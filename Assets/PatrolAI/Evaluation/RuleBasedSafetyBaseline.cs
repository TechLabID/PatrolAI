using System;

namespace PatrolAI.Evaluation
{
    /// <summary>
    /// A reasonable safety-first deterministic baseline. It uses explicit condition,
    /// evidence quality, confidence, and structured source agreement tokens. It does
    /// not attempt open-ended interpretation of operational context.
    /// </summary>
    public static class RuleBasedSafetyBaseline
    {
        public const float SufficientConfidence = 0.75f;

        public static PatrolDecision Decide(PatrolObservation observation)
        {
            if (observation == null)
                return Reobserve("Observation is missing.");

            string condition = Normalize(observation.detectedCondition);
            string evidence = Normalize(observation.evidenceStatus);
            string context = Normalize(observation.additionalContext);

            if (evidence.Contains("MISSING") || evidence.Contains("DEGRADED")
                || evidence.Contains("INSUFFICIENT") || evidence.Contains("UNCLEAR")
                || observation.confidence < SufficientConfidence)
            {
                return Reobserve("Evidence is insufficient, degraded, or below threshold.");
            }

            bool conflict = evidence.Contains("CONFLICT")
                || context.Contains("AGREEMENT=CONFLICT")
                || context.Contains("AGREEMENT=DISAGREE");
            if (conflict)
            {
                // Conservative rule-table policy: stop when any current source reports red;
                // otherwise obtain more evidence rather than guessing.
                if (condition.Contains("RED") || context.Contains("RED"))
                    return Stop("Conflicting evidence includes a red indication.");
                return Reobserve("Conflicting sources require another observation.");
            }

            if (condition.Contains("RED"))
                return Stop("Sufficient evidence reports a red indication.");
            if (condition.Contains("GREEN") || condition == "NORMAL"
                || condition.Contains("CLEAR"))
                return Continue("Sufficient evidence reports a safe condition.");

            return Reobserve("Condition is not covered by the bounded rule table.");
        }

        private static PatrolDecision Continue(string reason)
        {
            return Decision("SAFE", "CONTINUE", reason, false, 1f);
        }

        private static PatrolDecision Stop(string reason)
        {
            return Decision("DANGER", "STOP", reason, false, 1f);
        }

        private static PatrolDecision Reobserve(string reason)
        {
            return Decision("UNKNOWN", "REOBSERVE", reason, true, 1f);
        }

        private static PatrolDecision Decision(
            string risk, string action, string reason, bool reobserve, float confidence)
        {
            return new PatrolDecision
            {
                decisionId = "rule-baseline",
                riskLevel = risk,
                action = action,
                reason = reason,
                requiresReobservation = reobserve,
                confidence = confidence
            };
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }
    }
}
