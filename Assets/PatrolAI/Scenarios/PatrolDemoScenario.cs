using System;

namespace PatrolAI.Scenarios
{
    [Serializable]
    public sealed class PatrolDemoScenario
    {
        public string scenarioId;
        public string title;
        public PatrolObservation observation;
        public string expectedRiskLevel;
        public string expectedAction;
        public string expectedFinalState;

        public static PatrolDemoScenario Safe()
        {
            return Create("A", "Normal / Safe", "NORMAL", "SUFFICIENT", 0.98f,
                "SAFE", "CONTINUE", "CONTINUING");
        }

        public static PatrolDemoScenario InsufficientEvidence()
        {
            return Create("B", "Insufficient Evidence", "INDICATOR_UNCLEAR",
                "INSUFFICIENT", 0.35f, "UNKNOWN", "REOBSERVE", "REOBSERVING");
        }

        public static PatrolDemoScenario Hazard()
        {
            return Create("C", "Confirmed Hazard", "INDICATOR_RED", "SUFFICIENT",
                0.95f, "DANGER", "STOP", "STOPPED");
        }

        private static PatrolDemoScenario Create(
            string id,
            string title,
            string condition,
            string evidence,
            float confidence,
            string risk,
            string action,
            string finalState)
        {
            return new PatrolDemoScenario
            {
                scenarioId = id,
                title = title,
                observation = new PatrolObservation
                {
                    observationId = "phase5-scenario-" + id.ToLowerInvariant(),
                    timestamp = DateTime.UtcNow.ToString("O"),
                    source = "PatrolAI competition demo sensor",
                    detectedCondition = condition,
                    evidenceStatus = evidence,
                    confidence = confidence,
                    location = "Industrial inspection point A",
                    additionalContext = "Deterministic Phase 5 demo observation."
                },
                expectedRiskLevel = risk,
                expectedAction = action,
                expectedFinalState = finalState
            };
        }
    }
}
