using System;

namespace PatrolAI.NVIDIA
{
    /// <summary>
    /// Converts untrusted parsed model output into a safety-validated decision.
    /// Invalid or inconsistent output always becomes UNKNOWN / REOBSERVE.
    /// </summary>
    public static class NemotronDecisionValidator
    {
        public static PatrolDecision Validate(
            NemotronReasoningResponse response,
            string observationId = null)
        {
            if (response == null)
            {
                return CreateSafeFallback(observationId, "Nemotron response is missing.");
            }

            string riskLevel = Normalize(response.riskLevel);
            string action = Normalize(response.action);

            if (!IsAllowedRiskLevel(riskLevel))
            {
                return CreateSafeFallback(observationId, "Nemotron risk level is unsupported.");
            }

            if (!IsAllowedAction(action))
            {
                return CreateSafeFallback(observationId, "Nemotron action is unsupported.");
            }

            if (float.IsNaN(response.confidence)
                || float.IsInfinity(response.confidence)
                || response.confidence < 0f
                || response.confidence > 1f)
            {
                return CreateSafeFallback(observationId, "Nemotron confidence is outside 0..1.");
            }

            if (string.IsNullOrWhiteSpace(response.reason))
            {
                return CreateSafeFallback(observationId, "Nemotron reason is missing.");
            }

            if (!IsRiskActionConsistent(riskLevel, action))
            {
                return CreateSafeFallback(
                    observationId,
                    "Nemotron risk level and action are inconsistent.");
            }

            if ((riskLevel == "UNKNOWN" || action == "REOBSERVE")
                && !response.requiresReobservation)
            {
                return CreateSafeFallback(
                    observationId,
                    "Unknown or re-observation output must require re-observation.");
            }

            return new PatrolDecision
            {
                decisionId = CreateDecisionId(observationId),
                riskLevel = riskLevel,
                action = action,
                reason = response.reason.Trim(),
                requiresReobservation = response.requiresReobservation,
                confidence = response.confidence
            };
        }

        public static PatrolDecision CreateSafeFallback(
            string observationId,
            string reason)
        {
            return new PatrolDecision
            {
                decisionId = CreateDecisionId(observationId),
                riskLevel = "UNKNOWN",
                action = "REOBSERVE",
                reason = string.IsNullOrWhiteSpace(reason)
                    ? "Reasoning output could not be validated."
                    : reason.Trim(),
                requiresReobservation = true,
                confidence = 0f
            };
        }

        private static bool IsAllowedRiskLevel(string value)
        {
            return value == "SAFE"
                || value == "WARNING"
                || value == "DANGER"
                || value == "UNKNOWN";
        }

        private static bool IsAllowedAction(string value)
        {
            return value == "CONTINUE"
                || value == "REOBSERVE"
                || value == "INSPECT"
                || value == "STOP"
                || value == "ALERT"
                || value == "REROUTE";
        }

        private static bool IsRiskActionConsistent(string riskLevel, string action)
        {
            switch (riskLevel)
            {
                case "SAFE":
                    return action == "CONTINUE";
                case "WARNING":
                    return action == "INSPECT"
                        || action == "REOBSERVE"
                        || action == "ALERT"
                        || action == "STOP"
                        || action == "REROUTE";
                case "DANGER":
                    return action == "STOP"
                        || action == "ALERT"
                        || action == "REROUTE";
                case "UNKNOWN":
                    return action == "REOBSERVE";
                default:
                    return false;
            }
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string CreateDecisionId(string observationId)
        {
            return string.IsNullOrWhiteSpace(observationId)
                ? "decision-" + Guid.NewGuid().ToString("N")
                : "decision-" + observationId.Trim();
        }
    }
}
