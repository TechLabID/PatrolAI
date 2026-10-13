using System;

namespace PatrolAI.Scenarios
{
    public static class PatrolConflictDemoScenario
    {
        public static PatrolDemoScenario Create()
        {
            return new PatrolDemoScenario {
                scenarioId = "D", title = "Synthetic conflict: RED versus NORMAL",
                expectedRiskLevel = "UNKNOWN", expectedAction = "REOBSERVE", expectedFinalState = "REOBSERVING",
                observation = new PatrolObservation {
                    observationId = "phase7b-conflict-initial", timestamp = DateTime.UtcNow.ToString("O"),
                    source = "SYNTHETIC / SCENARIO-DEFINED: Source A + Source B",
                    location = "Industrial inspection point A", detectedCondition = "INDICATOR_RED",
                    evidenceStatus = "CONFLICT", confidence = .88f,
                    additionalContext = "SYNTHETIC / SCENARIO-DEFINED evidence; sources=2; agreement=conflict; "
                        + "Source A current camera indicates RED; Source B current context sensor reports NORMAL. "
                        + "Both initially appear sufficient; neither is authoritative. Their target association needs verification. "
                        + "Safety policy: unresolved conflicting sources require re-observation; do not invent a resolution."
                }
            };
        }

        public static PatrolObservation AddResolvedContext(PatrolObservation actual)
        {
            if (actual == null) throw new ArgumentNullException(nameof(actual));
            return new PatrolObservation {
                observationId = actual.observationId, timestamp = actual.timestamp,
                detectedCondition = actual.detectedCondition, evidenceStatus = actual.evidenceStatus,
                confidence = actual.confidence, location = actual.location,
                source = actual.source + " + SYNTHETIC scenario target-association check",
                additionalContext = actual.additionalContext + "; SYNTHETIC / SCENARIO-DEFINED context update: "
                    + "target-association check identifies Source B NORMAL reading as a different equipment target; "
                    + "exclude that mismatched source. Use the actual current primary/secondary verification above; "
                    + "do not assume that remaining evidence is sufficient or that its condition is RED."
            };
        }
    }
}
