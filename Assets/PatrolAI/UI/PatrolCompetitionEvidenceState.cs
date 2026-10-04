using System;
using PatrolAI.PhysicalAI;

namespace PatrolAI.UI
{
    /// <summary>
    /// Presentation state derived from an actual PatrolAI observation and closed-loop result.
    /// A new run clears prior success before any result is displayed.
    /// </summary>
    [Serializable]
    public sealed class PatrolCompetitionEvidenceState
    {
        public const string ProviderName = "Nebius Token Factory";
        public const string ModelName = "nvidia/Nemotron-3_5-Lightning";

        public string RuntimeStatus { get; private set; } = "NOT VERIFIED";
        public string Provider { get; private set; } = ProviderName;
        public string Model { get; private set; } = ModelName;
        public string Observation { get; private set; } = "NOT VERIFIED";
        public string Evidence { get; private set; } = "NOT VERIFIED";
        public string PerceptionConfidence { get; private set; } = "NOT VERIFIED";
        public string PatrolDecision { get; private set; } = "NOT VERIFIED";
        public string PatrolAction { get; private set; } = "NOT EXECUTED";
        public string FinalState { get; private set; } = "NOT VERIFIED";
        public string Latency { get; private set; } = "NOT VERIFIED";
        public string Detail { get; private set; } = "Awaiting scenario run.";

        public void Begin(PatrolObservation observation)
        {
            RuntimeStatus = "NOT VERIFIED";
            Observation = ValueOrUnknown(observation == null
                ? null
                : observation.detectedCondition);
            Evidence = ValueOrUnknown(observation == null
                ? null
                : observation.evidenceStatus);
            PerceptionConfidence = observation == null
                ? "NOT VERIFIED"
                : observation.confidence.ToString("0.00");
            PatrolDecision = "NOT VERIFIED";
            PatrolAction = "NOT EXECUTED";
            FinalState = "NOT VERIFIED";
            Latency = "NOT VERIFIED";
            Detail = "Reasoning in progress.";
        }

        public void Apply(PatrolObservation observation, PatrolClosedLoopResult result)
        {
            Begin(observation);
            if (result == null)
            {
                SetFailed("No closed-loop result was returned.");
                return;
            }

            string reportedStatus = NormalizeRuntimeStatus(result.runtimeStatus);
            bool verifiedReal = reportedStatus == "REAL"
                && result.success
                && result.httpSuccess
                && result.parserSuccess
                && result.validatorSuccess
                && !result.usedSafeFallback;

            RuntimeStatus = reportedStatus == "REAL" && !verifiedReal
                ? "FAILED"
                : reportedStatus;
            Latency = Math.Max(0L, result.elapsedMilliseconds) + " ms";

            if (RuntimeStatus == "FAILED" || RuntimeStatus == "NOT VERIFIED")
            {
                PatrolDecision = "NOT VERIFIED";
                PatrolAction = "NOT EXECUTED";
                FinalState = "NOT VERIFIED";
                Detail = ValueOrUnknown(result.message);
                return;
            }

            PatrolDecision = FormatDecision(result.decision);
            PatrolAction = result.action == null
                ? "NOT EXECUTED"
                : ValueOrUnknown(result.action.actionType);
            FinalState = ValueOrUnknown(result.finalState);
            Detail = RuntimeStatus == "FALLBACK"
                ? "Fallback source: PatrolAI safe re-observation policy."
                : ValueOrUnknown(result.message);
        }

        public void SetPhysicalWorkflowStatus(string status)
        {
            Detail = string.IsNullOrWhiteSpace(status)
                ? "PHYSICAL RE-OBSERVATION: NOT VERIFIED"
                : status.Trim().ToUpperInvariant();
        }

        private void SetFailed(string detail)
        {
            RuntimeStatus = "FAILED";
            PatrolDecision = "NOT VERIFIED";
            PatrolAction = "NOT EXECUTED";
            FinalState = "NOT VERIFIED";
            Detail = detail;
        }

        private static string NormalizeRuntimeStatus(string value)
        {
            switch ((value ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "REAL":
                case "MOCK":
                case "FALLBACK":
                case "FAILED":
                case "LOCAL SAMPLE":
                    return value.Trim().ToUpperInvariant();
                default:
                    return "NOT VERIFIED";
            }
        }

        private static string FormatDecision(PatrolDecision decision)
        {
            return decision == null
                ? "NOT VERIFIED"
                : ValueOrUnknown(decision.riskLevel) + " / "
                    + ValueOrUnknown(decision.action);
        }

        private static string ValueOrUnknown(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "NOT VERIFIED"
                : value.Trim().ToUpperInvariant();
        }
    }
}
