using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using PatrolAI.Nebius;

namespace PatrolAI.NVIDIA
{
    /// <summary>
    /// Live reasoning adapter for NVIDIA Nemotron through Nebius Token Factory.
    /// It returns data only and has no scene, movement, controller, OpenCV, or UI dependency.
    /// </summary>
    public sealed class NebiusNemotronReasoningService : INemotronReasoningService, IDisposable
    {
        public const string ModelId = "nvidia/Nemotron-3_5-Lightning";

        private const float Temperature = 0.1f;
        private const int MaxTokens = 512;
        private const string InvalidLiveReason =
            "Live reasoning unavailable or response invalid.";
        private const string SystemPrompt =
            "You are the PatrolAI industrial safety reasoning component. "
            + "Use only the supplied observation; do not invent evidence. "
            + "Return exactly one JSON object matching the response schema. "
            + "Do not return markdown, code fences, analysis, reasoning traces, "
            + "or prose outside the JSON object. "
            + "Required fields: riskLevel, action, reason, requiresReobservation, confidence. "
            + "riskLevel must be SAFE, WARNING, DANGER, or UNKNOWN. "
            + "action must be CONTINUE, REOBSERVE, INSPECT, STOP, ALERT, or REROUTE. "
            + "Use UNKNOWN and prefer REOBSERVE when evidence is insufficient. "
            + "Keep reason short and confidence between 0.0 and 1.0.";

        private readonly NebiusClient client;

        public NebiusNemotronReasoningService(NebiusConfig config = null)
        {
            client = new NebiusClient(config);
        }

        public async Task<PatrolDecision> ReasonAsync(PatrolObservation observation)
        {
            NemotronLiveReasoningResult result = await ReasonWithDiagnosticsAsync(observation)
                .ConfigureAwait(false);
            return result.decision;
        }

        public async Task<NemotronLiveReasoningResult> ReasonWithDiagnosticsAsync(
            PatrolObservation observation)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var result = new NemotronLiveReasoningResult
            {
                modelId = ModelId,
                message = string.Empty
            };

            try
            {
                if (!NemotronReasoningRequest.TryCreate(
                    observation,
                    out NemotronReasoningRequest reasoningRequest,
                    out string requestError))
                {
                    result.message = requestError;
                    result.decision = CreateLiveFallback(null);
                    return result;
                }

                string userPrompt =
                    "Return only the required PatrolDecision JSON for this observation: "
                    + SerializeObservation(reasoningRequest);
                result.requestStarted = true;

                NebiusChatCompletionResult chatResult = await client
                    .SendChatCompletionAsync(
                        ModelId,
                        SystemPrompt,
                        userPrompt,
                        Temperature,
                        MaxTokens)
                    .ConfigureAwait(false);

                result.httpSuccess = chatResult.success;
                result.statusCode = chatResult.statusCode;
                result.message = chatResult.message;

                if (!chatResult.success)
                {
                    result.decision = CreateLiveFallback(observation.observationId);
                    return result;
                }

                if (!NemotronDecisionParser.TryParse(
                    chatResult.modelOutput,
                    out NemotronReasoningResponse parsedResponse,
                    out string parseError))
                {
                    result.message = "Live Nemotron output parse failed: " + parseError;
                    result.decision = CreateLiveFallback(observation.observationId);
                    return result;
                }

                result.parserSuccess = true;
                PatrolDecision decision = NemotronDecisionValidator.Validate(
                    parsedResponse,
                    observation.observationId);
                result.decision = decision;
                result.validatorSuccess = IsValidDecision(decision);
                result.validatorUsedSafeNormalization =
                    result.validatorSuccess && WasSafelyNormalized(parsedResponse, decision);
                result.message = result.validatorSuccess
                    ? "Live Nemotron response parsed and validated."
                    : InvalidLiveReason;

                if (!result.validatorSuccess)
                {
                    result.decision = CreateLiveFallback(observation.observationId);
                }

                return result;
            }
            catch (Exception)
            {
                result.message = InvalidLiveReason;
                result.decision = CreateLiveFallback(observation == null
                    ? null
                    : observation.observationId);
                return result;
            }
            finally
            {
                stopwatch.Stop();
                result.elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        public void Dispose()
        {
            client.Dispose();
        }

        private static string SerializeObservation(NemotronReasoningRequest request)
        {
            var payload = new ObservationPayload
            {
                ObservationId = request.observationId,
                Source = request.source,
                DetectedCondition = request.detectedCondition,
                EvidenceStatus = request.evidenceStatus,
                Confidence = request.confidence,
                Location = request.location,
                AdditionalContext = request.additionalContext
            };

            var serializer = new DataContractJsonSerializer(typeof(ObservationPayload));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, payload);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static PatrolDecision CreateLiveFallback(string observationId)
        {
            return NemotronDecisionValidator.CreateSafeFallback(
                observationId,
                InvalidLiveReason);
        }

        private static bool IsValidDecision(PatrolDecision decision)
        {
            if (decision == null
                || string.IsNullOrWhiteSpace(decision.reason)
                || float.IsNaN(decision.confidence)
                || float.IsInfinity(decision.confidence)
                || decision.confidence < 0f
                || decision.confidence > 1f)
            {
                return false;
            }

            switch (decision.riskLevel + "/" + decision.action)
            {
                case "SAFE/CONTINUE":
                case "WARNING/INSPECT":
                case "WARNING/REOBSERVE":
                case "WARNING/ALERT":
                case "WARNING/STOP":
                case "WARNING/REROUTE":
                case "DANGER/STOP":
                case "DANGER/ALERT":
                case "DANGER/REROUTE":
                    return decision.action != "REOBSERVE" || decision.requiresReobservation;
                case "UNKNOWN/REOBSERVE":
                    return decision.requiresReobservation;
                default:
                    return false;
            }
        }

        private static bool WasSafelyNormalized(
            NemotronReasoningResponse parsed,
            PatrolDecision decision)
        {
            return parsed == null
                || decision.riskLevel != parsed.riskLevel
                || decision.action != parsed.action
                || decision.requiresReobservation != parsed.requiresReobservation
                || Math.Abs(decision.confidence - parsed.confidence) > 0.0001f;
        }

        [DataContract]
        private sealed class ObservationPayload
        {
            [DataMember(Name = "observationId", Order = 1)]
            public string ObservationId { get; set; }

            [DataMember(Name = "source", Order = 2)]
            public string Source { get; set; }

            [DataMember(Name = "detectedCondition", Order = 3)]
            public string DetectedCondition { get; set; }

            [DataMember(Name = "evidenceStatus", Order = 4)]
            public string EvidenceStatus { get; set; }

            [DataMember(Name = "confidence", Order = 5)]
            public float Confidence { get; set; }

            [DataMember(Name = "location", Order = 6)]
            public string Location { get; set; }

            [DataMember(Name = "additionalContext", Order = 7)]
            public string AdditionalContext { get; set; }
        }
    }
}
