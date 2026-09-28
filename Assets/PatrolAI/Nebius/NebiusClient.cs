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
    /// <summary>
    /// Isolated Nebius Token Factory connectivity client. It is not connected to
    /// perception, decisions, movement, scenes, controllers, or UI.
    /// </summary>
    public sealed class NebiusClient : IDisposable
    {
        private const string ModelsEndpoint = "v1/models";

        private readonly NebiusConfig config;
        private readonly HttpClient httpClient;

        public NebiusClient(NebiusConfig config = null)
        {
            this.config = config ?? new NebiusConfig();
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(this.config.GetEffectiveTimeoutSeconds())
            };
        }

        public async Task<NebiusConnectionResult> TestConnectionAsync()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string apiKey = string.Empty;

            try
            {
                if (!config.TryGetApiKey(out apiKey, out string configurationError))
                {
                    return CreateResult(false, 0, configurationError, string.Empty, stopwatch);
                }

                if (!TryCreateModelsUri(out Uri modelsUri, out configurationError))
                {
                    return CreateResult(false, 0, configurationError, string.Empty, stopwatch);
                }

                using (var request = new HttpRequestMessage(HttpMethod.Get, modelsUri))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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
                            return CreateResult(
                                false,
                                statusCode,
                                "Nebius returned a non-success HTTP status: " + statusCode + ".",
                                responseBody,
                                stopwatch);
                        }

                        if (string.IsNullOrWhiteSpace(responseBody))
                        {
                            return CreateResult(
                                false,
                                statusCode,
                                "Nebius returned an empty response body.",
                                string.Empty,
                                stopwatch);
                        }

                        if (!TryParseModelsResponse(responseBody, out int modelCount, out string parseError))
                        {
                            return CreateResult(
                                false,
                                statusCode,
                                "Nebius returned HTTP success, but the models response was invalid: " + parseError,
                                responseBody,
                                stopwatch);
                        }

                        return CreateResult(
                            true,
                            statusCode,
                            "Nebius connectivity succeeded. Models received: " + modelCount + ".",
                            responseBody,
                            stopwatch);
                    }
                }
            }
            catch (TaskCanceledException exception)
            {
                return CreateResult(
                    false,
                    0,
                    RedactSecret(
                        "Nebius request timed out after "
                        + config.GetEffectiveTimeoutSeconds()
                        + " seconds. "
                        + exception.Message,
                        apiKey),
                    string.Empty,
                    stopwatch);
            }
            catch (HttpRequestException exception)
            {
                return CreateResult(
                    false,
                    0,
                    RedactSecret("Nebius network request failed: " + exception.Message, apiKey),
                    string.Empty,
                    stopwatch);
            }
            catch (Exception exception)
            {
                return CreateResult(
                    false,
                    0,
                    RedactSecret("Nebius connectivity test failed safely: " + exception.Message, apiKey),
                    string.Empty,
                    stopwatch);
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        private bool TryCreateModelsUri(out Uri modelsUri, out string errorMessage)
        {
            modelsUri = null;
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
            modelsUri = new Uri(new Uri(normalizedBaseUrl), ModelsEndpoint);
            return true;
        }

        private static bool TryParseModelsResponse(
            string responseBody,
            out int modelCount,
            out string errorMessage)
        {
            modelCount = 0;
            errorMessage = string.Empty;

            try
            {
                var serializer = new DataContractJsonSerializer(typeof(NebiusModelsResponse));
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseBody)))
                {
                    var parsedResponse = serializer.ReadObject(stream) as NebiusModelsResponse;
                    if (parsedResponse == null || parsedResponse.Data == null)
                    {
                        errorMessage = "required data array is missing.";
                        return false;
                    }

                    modelCount = parsedResponse.Data.Length;
                    return true;
                }
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private static NebiusConnectionResult CreateResult(
            bool success,
            int statusCode,
            string message,
            string responseBody,
            Stopwatch stopwatch)
        {
            return new NebiusConnectionResult
            {
                success = success,
                statusCode = statusCode,
                message = message ?? string.Empty,
                responseBody = responseBody ?? string.Empty,
                elapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                timestamp = DateTime.UtcNow.ToString("o")
            };
        }

        private static string RedactSecret(string value, string secret)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(secret))
            {
                return value ?? string.Empty;
            }

            return value.Replace(secret, "[REDACTED]");
        }

        [DataContract]
        private sealed class NebiusModelsResponse
        {
            [DataMember(Name = "data")]
            public NebiusModelInfo[] Data { get; set; }
        }

        [DataContract]
        private sealed class NebiusModelInfo
        {
        }
    }
}
