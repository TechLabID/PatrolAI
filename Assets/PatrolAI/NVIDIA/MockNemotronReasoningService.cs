using System;
using System.Threading.Tasks;

namespace PatrolAI.NVIDIA
{
    /// <summary>
    /// OFFLINE TEST ONLY. Deterministic mock; never a Nebius or NVIDIA response.
    /// </summary>
    public sealed class MockNemotronReasoningService : INemotronReasoningService
    {
        public const string StatusLabel = "OFFLINE TEST ONLY";

        public Task<PatrolDecision> ReasonAsync(PatrolObservation observation)
        {
            if (observation == null)
            {
                return Task.FromResult(
                    NemotronDecisionValidator.CreateSafeFallback(
                        null,
                        "OFFLINE TEST ONLY: observation is missing."));
            }

            string condition = Normalize(observation.detectedCondition);
            string evidenceStatus = Normalize(observation.evidenceStatus);

            if (IsEvidenceInsufficient(condition, evidenceStatus, observation.confidence))
            {
                return Task.FromResult(
                    NemotronDecisionValidator.CreateSafeFallback(
                        observation.observationId,
                        "OFFLINE TEST ONLY: evidence is insufficient."));
            }

            NemotronReasoningResponse response;
            switch (condition)
            {
                case "NORMAL":
                    response = new NemotronReasoningResponse
                    {
                        riskLevel = "SAFE",
                        action = "CONTINUE",
                        reason = "OFFLINE TEST ONLY: normal condition mapping.",
                        requiresReobservation = false,
                        confidence = 0.95f
                    };
                    break;

                case "INDICATOR_RED":
                    response = new NemotronReasoningResponse
                    {
                        riskLevel = "DANGER",
                        action = "STOP",
                        reason = "OFFLINE TEST ONLY: red indicator mapping.",
                        requiresReobservation = false,
                        confidence = 0.9f
                    };
                    break;

                default:
                    return Task.FromResult(
                        NemotronDecisionValidator.CreateSafeFallback(
                            observation.observationId,
                            "OFFLINE TEST ONLY: condition has no deterministic mapping."));
            }

            return Task.FromResult(
                NemotronDecisionValidator.Validate(response, observation.observationId));
        }

        private static bool IsEvidenceInsufficient(
            string condition,
            string evidenceStatus,
            float confidence)
        {
            return string.IsNullOrEmpty(condition)
                || confidence < 0.5f
                || evidenceStatus == "INSUFFICIENT"
                || evidenceStatus == "INCOMPLETE"
                || evidenceStatus == "UNCERTAIN"
                || evidenceStatus == "UNKNOWN";
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }
    }
}
