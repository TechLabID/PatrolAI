using System;
using PatrolAI.PhysicalAI;

namespace PatrolAI.Evaluation
{
    /// <summary>Fixed before held-out inference; v1 is preserved unchanged.</summary>
    public static class RuleBasedSafetyBaselineV2
    {
        public static PatrolDecision Decide(PatrolObservation observation)
        {
            return PatrolSafetyEnvelope.Apply(Propose(observation), observation,
                PatrolSafetyEnvelope.Attempts(observation), true, out bool ignored);
        }

        public static PatrolDecision Propose(PatrolObservation observation)
        {
            string context = (observation == null ? "" : observation.additionalContext ?? "").ToUpperInvariant();
            PatrolDecision proposed;
            if (observation == null || PatrolSafetyEnvelope.Unresolved(observation))
                proposed = PatrolComparativeEvaluation.Decision("UNKNOWN/REOBSERVE", "V2: insufficient, stale, missing or conflicting evidence.");
            else if (context.Contains("CONFIRMED_HAZARD=TRUE"))
                proposed = PatrolSafetyEnvelope.Stop("V2: explicit confirmed hazard overrides indicator color.");
            else {
                string condition = (observation.detectedCondition ?? "").ToUpperInvariant();
                if (condition == "INDICATOR_RED") proposed = PatrolSafetyEnvelope.Stop("V2: sufficient current red evidence.");
                else if (condition == "INDICATOR_GREEN" || condition == "NORMAL" || condition == "AREA_CLEAR")
                    proposed = PatrolComparativeEvaluation.Decision("SAFE/CONTINUE", "V2: sufficient current green/normal evidence.");
                else proposed = PatrolComparativeEvaluation.Decision("UNKNOWN/REOBSERVE", "V2: unknown condition.");
            }
            return proposed;
        }
    }
}
