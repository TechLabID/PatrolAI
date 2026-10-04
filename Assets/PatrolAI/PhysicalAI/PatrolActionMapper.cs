using System;

namespace PatrolAI.PhysicalAI
{
    /// <summary>
    /// Whitelists decision-to-action mappings and safely falls back to REOBSERVE.
    /// </summary>
    public static class PatrolActionMapper
    {
        public static PatrolAction Map(PatrolDecision decision, out bool usedSafeFallback)
        {
            usedSafeFallback = false;
            string actionType = ResolveAction(decision, out usedSafeFallback);
            string reason = decision == null || string.IsNullOrWhiteSpace(decision.reason)
                ? "Safe fallback: decision is missing or unsupported."
                : decision.reason.Trim();

            if (usedSafeFallback)
            {
                reason = "Safe fallback to re-observation. " + reason;
            }

            return new PatrolAction
            {
                actionType = actionType,
                reason = reason,
                sourceDecisionId = decision == null ? string.Empty : decision.decisionId,
                confidence = ClampConfidence(decision == null ? 0f : decision.confidence),
                timestamp = DateTime.UtcNow.ToString("o")
            };
        }

        private static string ResolveAction(
            PatrolDecision decision,
            out bool usedSafeFallback)
        {
            usedSafeFallback = false;
            if (decision == null)
            {
                usedSafeFallback = true;
                return "REOBSERVE";
            }

            string riskLevel = Normalize(decision.riskLevel);
            string action = Normalize(decision.action);
            switch (riskLevel + "/" + action)
            {
                case "SAFE/CONTINUE":
                    return "CONTINUE";
                case "WARNING/INSPECT":
                    return "INSPECT";
                case "WARNING/REOBSERVE":
                    return "REOBSERVE";
                case "DANGER/STOP":
                    return "STOP";
                case "DANGER/ALERT":
                    return "ALERT";
                case "UNKNOWN/REOBSERVE":
                    return "REOBSERVE";
                default:
                    usedSafeFallback = true;
                    return "REOBSERVE";
            }
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static float ClampConfidence(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return Math.Max(0f, Math.Min(value, 1f));
        }
    }
}
