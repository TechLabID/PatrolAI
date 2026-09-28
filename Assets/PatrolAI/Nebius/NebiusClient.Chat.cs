using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace PatrolAI.Nebius
{
    public sealed partial class NebiusClient
    {
        private const string ChatCompletionsEndpoint = "v1/chat/completions";
        private const string NonThinkingModelId = "nvidia/Nemotron-3_5-Lightning";

        public async Task<NebiusChatCompletionResult> SendChatCompletionAsync(
            string modelId,
            string systemPrompt,
            string userPrompt,
            float temperature = 0.1f,
            int maxTokens = 180)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string apiKey = string.Empty;

            try
            {
                if (!config.TryGetApiKey(out apiKey, out string configurationError))
                {
                    return CreateChatResult(false, 0, configurationError, string.Empty, stopwatch);
                }

                if (string.IsNullOrWhiteSpace(modelId))
                {
                    return CreateChatResult(
                        false,
                        0,
                        "Nebius chat model ID is required.",
                        string.Empty,
                        stopwatch);
                }

                if (string.IsNullOrWhiteSpace(systemPrompt)
                    || string.IsNullOrWhiteSpace(userPrompt))
                {
                    return CreateChatResult(
                        false,
                        0,
                        "Nebius chat system and user prompts are required.",
                        string.Empty,
                        stopwatch);
                }

                if (!TryCreateChatUri(out Uri chatUri, out configurationError))
                {
                    return CreateChatResult(false, 0, configurationError, string.Empty, stopwatch);
                }

                var payload = new ChatCompletionRequest
                {
                    Model = modelId,
                    Messages = new[]
                    {
                        new ChatMessage { Role = "system", Content = systemPrompt },
                        new ChatMessage { Role = "user", Content = userPrompt }
                    },
                    Temperature = Math.Max(0f, Math.Min(temperature, 1f)),
                    MaxTokens = Math.Max(1, maxTokens),
                    Stream = false,
                    ResponseFormat = CreatePatrolDecisionResponseFormat(),
                    ExtraBody = string.Equals(
                        modelId,
                        NonThinkingModelId,
                        StringComparison.Ordinal)
                        ? new ChatExtraBody
                        {
                            ChatTemplateKwargs = new ChatTemplateKwargs
                            {
                                EnableThinking = false
                            }
                        }
                        : null
                };

                string requestJson = SerializeChatRequest(payload);
                using (var request = new HttpRequestMessage(HttpMethod.Post, chatUri))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                    using (HttpResponseMessage response = await httpClient
                        .SendAsync(request)
                        .ConfigureAwait(false))
                    {
                        string responseBody = await response.Content
                            .ReadAsStringAsync()
                            .ConfigureAwait(false);
                        responseBody = RedactSecret(responseBody, apiKey);
                        int statusCode = (int)response.StatusCode;

                        if (!response.IsSuccessStatusCode)
                        {
                            return CreateChatResult(
                                false,
                                statusCode,
                                "Nebius chat completion returned HTTP " + statusCode + ".",
                                string.Empty,
                                stopwatch);
                        }

                        if (!TryReadModelOutput(responseBody, out string modelOutput, out string parseError))
                        {
                            return CreateChatResult(
                                false,
                                statusCode,
                                "Nebius chat response was invalid: " + parseError,
                                string.Empty,
                                stopwatch);
                        }

                        return CreateChatResult(
                            true,
                            statusCode,
                            "Nebius chat completion succeeded.",
                            modelOutput,
                            stopwatch);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return CreateChatResult(
                    false,
                    0,
                    "Nebius chat request timed out after "
                    + config.GetEffectiveTimeoutSeconds()
                    + " seconds.",
                    string.Empty,
                    stopwatch);
            }
            catch (HttpRequestException exception)
            {
                return CreateChatResult(
                    false,
                    0,
                    RedactSecret("Nebius chat network request failed: " + exception.Message, apiKey),
                    string.Empty,
                    stopwatch);
            }
            catch (Exception exception)
            {
                return CreateChatResult(
                    false,
                    0,
                    RedactSecret("Nebius chat request failed safely: " + exception.Message, apiKey),
                    string.Empty,
                    stopwatch);
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        private bool TryCreateChatUri(out Uri chatUri, out string errorMessage)
        {
            chatUri = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(config.baseUrl)
                || !Uri.TryCreate(config.baseUrl, UriKind.Absolute, out Uri baseUri))
            {
                errorMessage = "Nebius base URL is missing or invalid.";
                return false;
            }

            if (baseUri.Scheme != Uri.UriSchemeHttps)
            {
                errorMessage = "Nebius base URL must use HTTPS.";
                return false;
            }

            string normalizedBaseUrl = baseUri.AbsoluteUri.TrimEnd('/') + "/";
            chatUri = new Uri(new Uri(normalizedBaseUrl), ChatCompletionsEndpoint);
            return true;
        }

        private static string SerializeChatRequest(ChatCompletionRequest payload)
        {
            var serializer = new DataContractJsonSerializer(typeof(ChatCompletionRequest));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, payload);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static ChatResponseFormat CreatePatrolDecisionResponseFormat()
        {
            return new ChatResponseFormat
            {
                Type = "json_schema",
                JsonSchema = new ChatJsonSchema
                {
                    Name = "patrol_decision",
                    Strict = true,
                    Schema = new ChatJsonObjectSchema
                    {
                        Type = "object",
                        Properties = new ChatDecisionProperties
                        {
                            RiskLevel = ChatJsonProperty.EnumString(
                                "SAFE", "WARNING", "DANGER", "UNKNOWN"),
                            Action = ChatJsonProperty.EnumString(
                                "CONTINUE", "REOBSERVE", "INSPECT", "STOP", "ALERT", "REROUTE"),
                            Reason = ChatJsonProperty.String(),
                            RequiresReobservation = ChatJsonProperty.Boolean(),
                            Confidence = ChatJsonProperty.Number(0f, 1f)
                        },
                        Required = new[]
                        {
                            "riskLevel",
                            "action",
                            "reason",
                            "requiresReobservation",
                            "confidence"
                        },
                        AdditionalProperties = false
                    }
                }
            };
        }

        private static bool TryReadModelOutput(
            string responseBody,
            out string modelOutput,
            out string errorMessage)
        {
            modelOutput = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                errorMessage = "response body is empty.";
                return false;
            }

            try
            {
                var serializer = new DataContractJsonSerializer(typeof(ChatCompletionResponse));
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseBody)))
                {
                    var parsed = serializer.ReadObject(stream) as ChatCompletionResponse;
                    if (parsed == null
                        || parsed.Choices == null
                        || parsed.Choices.Length == 0
                        || parsed.Choices[0] == null
                        || parsed.Choices[0].Message == null
                        || string.IsNullOrWhiteSpace(parsed.Choices[0].Message.Content))
                    {
                        errorMessage = "first choice message content is missing.";
                        return false;
                    }

                    modelOutput = parsed.Choices[0].Message.Content.Trim();
                    return true;
                }
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private static NebiusChatCompletionResult CreateChatResult(
            bool success,
            int statusCode,
            string message,
            string modelOutput,
            Stopwatch stopwatch)
        {
            return new NebiusChatCompletionResult
            {
                success = success,
                statusCode = statusCode,
                message = message ?? string.Empty,
                modelOutput = modelOutput ?? string.Empty,
                elapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                timestamp = DateTime.UtcNow.ToString("o")
            };
        }

        [DataContract]
        private sealed class ChatCompletionRequest
        {
            [DataMember(Name = "model", Order = 1)]
            public string Model { get; set; }

            [DataMember(Name = "messages", Order = 2)]
            public ChatMessage[] Messages { get; set; }

            [DataMember(Name = "temperature", Order = 3)]
            public float Temperature { get; set; }

            [DataMember(Name = "max_tokens", Order = 4)]
            public int MaxTokens { get; set; }

            [DataMember(Name = "stream", Order = 5)]
            public bool Stream { get; set; }

            [DataMember(Name = "response_format", Order = 6)]
            public ChatResponseFormat ResponseFormat { get; set; }

            [DataMember(Name = "extra_body", Order = 7, EmitDefaultValue = false)]
            public ChatExtraBody ExtraBody { get; set; }
        }

        [DataContract]
        private sealed class ChatExtraBody
        {
            [DataMember(Name = "chat_template_kwargs", Order = 1)]
            public ChatTemplateKwargs ChatTemplateKwargs { get; set; }
        }

        [DataContract]
        private sealed class ChatTemplateKwargs
        {
            [DataMember(Name = "enable_thinking", Order = 1)]
            public bool EnableThinking { get; set; }
        }

        [DataContract]
        private sealed class ChatResponseFormat
        {
            [DataMember(Name = "type", Order = 1)]
            public string Type { get; set; }

            [DataMember(Name = "json_schema", Order = 2)]
            public ChatJsonSchema JsonSchema { get; set; }
        }

        [DataContract]
        private sealed class ChatJsonSchema
        {
            [DataMember(Name = "name", Order = 1)]
            public string Name { get; set; }

            [DataMember(Name = "strict", Order = 2)]
            public bool Strict { get; set; }

            [DataMember(Name = "schema", Order = 3)]
            public ChatJsonObjectSchema Schema { get; set; }
        }

        [DataContract]
        private sealed class ChatJsonObjectSchema
        {
            [DataMember(Name = "type", Order = 1)]
            public string Type { get; set; }

            [DataMember(Name = "properties", Order = 2)]
            public ChatDecisionProperties Properties { get; set; }

            [DataMember(Name = "required", Order = 3)]
            public string[] Required { get; set; }

            [DataMember(Name = "additionalProperties", Order = 4)]
            public bool AdditionalProperties { get; set; }
        }

        [DataContract]
        private sealed class ChatDecisionProperties
        {
            [DataMember(Name = "riskLevel", Order = 1)]
            public ChatJsonProperty RiskLevel { get; set; }

            [DataMember(Name = "action", Order = 2)]
            public ChatJsonProperty Action { get; set; }

            [DataMember(Name = "reason", Order = 3)]
            public ChatJsonProperty Reason { get; set; }

            [DataMember(Name = "requiresReobservation", Order = 4)]
            public ChatJsonProperty RequiresReobservation { get; set; }

            [DataMember(Name = "confidence", Order = 5)]
            public ChatJsonProperty Confidence { get; set; }
        }

        [DataContract]
        private sealed class ChatJsonProperty
        {
            [DataMember(Name = "type", Order = 1)]
            public string Type { get; set; }

            [DataMember(Name = "enum", Order = 2, EmitDefaultValue = false)]
            public string[] Enum { get; set; }

            [DataMember(Name = "minimum", Order = 3, EmitDefaultValue = false)]
            public float? Minimum { get; set; }

            [DataMember(Name = "maximum", Order = 4, EmitDefaultValue = false)]
            public float? Maximum { get; set; }

            public static ChatJsonProperty String()
            {
                return new ChatJsonProperty { Type = "string" };
            }

            public static ChatJsonProperty EnumString(params string[] values)
            {
                return new ChatJsonProperty { Type = "string", Enum = values };
            }

            public static ChatJsonProperty Boolean()
            {
                return new ChatJsonProperty { Type = "boolean" };
            }

            public static ChatJsonProperty Number(float minimum, float maximum)
            {
                return new ChatJsonProperty
                {
                    Type = "number",
                    Minimum = minimum,
                    Maximum = maximum
                };
            }
        }

        [DataContract]
        private sealed class ChatMessage
        {
            [DataMember(Name = "role", Order = 1)]
            public string Role { get; set; }

            [DataMember(Name = "content", Order = 2)]
            public string Content { get; set; }
        }

        [DataContract]
        private sealed class ChatCompletionResponse
        {
            [DataMember(Name = "choices")]
            public ChatChoice[] Choices { get; set; }
        }

        [DataContract]
        private sealed class ChatChoice
        {
            [DataMember(Name = "message")]
            public ChatResponseMessage Message { get; set; }
        }

        [DataContract]
        private sealed class ChatResponseMessage
        {
            [DataMember(Name = "content")]
            public string Content { get; set; }
        }
    }
}
