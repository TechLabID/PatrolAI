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
        public string ObservationSource { get; private set; } = "SOURCE: NOT VERIFIED";
        public string ObservationContext { get; private set; } = "";
        public string ModelRationale { get; private set; } = "";
        public string Evidence { get; private set; } = "NOT VERIFIED";
        public string PerceptionConfidence { get; private set; } = "NOT VERIFIED";
        public string PatrolDecision { get; private set; } = "NOT VERIFIED";
        public string PatrolAction { get; private set; } = "NOT EXECUTED";
        public string FinalState { get; private set; } = "NOT VERIFIED";
        public string Latency { get; private set; } = "NOT VERIFIED";
        public string FirstDecision { get; private set; } = "NOT VERIFIED";
        public string SecondDecision { get; private set; } = "NOT RUN";
        public string ReobservationStatus { get; private set; } = "NOT RUN";
        public string DecisionChange { get; private set; } = "NOT VERIFIED";
        public string Detail { get; private set; } = "Awaiting scenario run.";
        public string Comparison { get; private set; } = "";
        public void SetComparison(string text) { Comparison = text ?? ""; }
        public void SetBoundedOutcome(PatrolAI.PhysicalAI.PatrolAction action, bool escalated)
        {
            PatrolAction = action == null ? "NOT EXECUTED" : action.actionType;
            FinalState = PatrolAction == "STOP" ? "STOPPED" : PatrolAction == "CONTINUE" ? "CONTINUING" : "REOBSERVING";
            Detail = escalated ? "DETERMINISTIC SAFETY ENVELOPE: STOP / HUMAN REVIEW"
                : "BOUNDED ACTION: " + PatrolAction;
        }

        public void Begin(PatrolObservation observation)
        {
            RuntimeStatus = "NOT VERIFIED";
            ObservationSource = FormatObservationSource(observation);
            ObservationContext = observation == null ? "" : observation.additionalContext ?? "";
            ModelRationale = "";
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
            FirstDecision = "NOT VERIFIED";
            SecondDecision = "NOT RUN";
            ReobservationStatus = "NOT RUN";
            DecisionChange = "NOT VERIFIED";
            Detail = "Reasoning in progress.";
            Comparison = "";
        }

        public void Apply(PatrolObservation observation, PatrolClosedLoopResult result)
        {
            string previousSource = ObservationSource;
            string previousContext = ObservationContext;
            Begin(observation);
            // Failure paths can clear result data without losing the sample's provenance.
            if (observation == null) ObservationSource = previousSource;
            if (observation == null) ObservationContext = previousContext;
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
            ModelRationale = RuntimeStatus == "REAL" && result.decision != null
                ? result.decision.reason ?? "" : "";
            FirstDecision = PatrolDecision;
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

        public void SetReobservationComplete(PatrolObservation observation)
        {
            ReobservationStatus = "COMPLETE";
            Observation = ValueOrUnknown(observation == null
                ? null : observation.detectedCondition);
            Evidence = ValueOrUnknown(observation == null
                ? null : observation.evidenceStatus);
            PerceptionConfidence = observation == null
                ? "NOT VERIFIED" : observation.confidence.ToString("0.00");
            Detail = "RE-OBSERVATION COMPLETE — NEMOTRON REASONING AGAIN";
        }

        public void BeginSecondReasoning()
        {
            RuntimeStatus = "NOT VERIFIED";
            SecondDecision = "NOT RUN";
            ModelRationale = "";
            PatrolAction = "NOT EXECUTED";
            FinalState = "NOT VERIFIED";
            Latency = "NOT VERIFIED";
            Detail = "SECOND NEMOTRON REASONING IN PROGRESS";
        }

        public void ApplySecond(PatrolObservation observation, PatrolClosedLoopResult result)
        {
            string first = FirstDecision;
            Apply(observation, result);
            FirstDecision = first;
            SecondDecision = PatrolDecision;
            ReobservationStatus = "COMPLETE";
            DecisionChange = RuntimeStatus != "REAL" ? "NOT VERIFIED"
                : first == SecondDecision ? "UNCHANGED" : first + " → " + SecondDecision;
            Detail = RuntimeStatus == "REAL"
                ? "SECOND REAL NEMOTRON DECISION COMPLETE"
                : "SECOND NEMOTRON DECISION " + RuntimeStatus;
        }

        public void SetReobservationFailed(string detail)
        {
            ReobservationStatus = "FAILED";
            SecondDecision = "NOT RUN";
            DecisionChange = "NOT VERIFIED";
            Detail = string.IsNullOrWhiteSpace(detail)
                ? "PHYSICAL RE-OBSERVATION FAILED"
                : detail.Trim().ToUpperInvariant();
        }

        private static string FormatObservationSource(PatrolObservation observation)
        {
            if (observation == null) return "SOURCE: NOT VERIFIED";
            string id = observation.observationId ?? "";
            if ((observation.additionalContext ?? "").Contains("Original SYNTHETIC HC09 context:"))
                return "LOCAL SAMPLE: HELD-OUT HC09 + UNITY RE-OBSERVATION";
            if (id.StartsWith("heldout-HC", StringComparison.Ordinal))
                return "LOCAL SAMPLE: HELD-OUT " + id.Substring("heldout-".Length);
            if (id == "phase7b-conflict-initial") return "LOCAL SAMPLE: SYNTHETIC CONFLICT";
            if (id.StartsWith("phase5-scenario-", StringComparison.Ordinal))
                return "LOCAL SAMPLE: SCENARIO " + id.Substring("phase5-scenario-".Length).ToUpperInvariant();
            return "SOURCE: " + ValueOrUnknown(observation.source);
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
