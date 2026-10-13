using System;
using System.Text.RegularExpressions;

namespace PatrolAI.PhysicalAI
{
    /// <summary>Shared bounded operational policy; preserves the unmodified model result separately.</summary>
    public static class PatrolSafetyEnvelope
    {
        public const int MaximumReobservations = 2;
        public const int DemoMaximumReobservations = 1; // two total inference calls

        public static int Attempts(PatrolObservation observation)
        {
            var match = Regex.Match(observation == null ? "" : observation.additionalContext ?? "",
                @"(?:^|;)\s*reobservation_attempts=(\d+)(?:;|$)", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out int count) ? count : 0;
        }

        public static bool Unresolved(PatrolObservation observation)
        {
            if (observation == null) return true;
            string status = (observation.evidenceStatus ?? "").ToUpperInvariant();
            string condition = (observation.detectedCondition ?? "").ToUpperInvariant();
            return status.Contains("CONFLICT") || status.Contains("INSUFFICIENT")
                || status.Contains("MISSING") || status.Contains("DEGRADED") || status.Contains("STALE")
                || status.Contains("UNCLEAR") || condition == "UNKNOWN" || condition.Contains("UNCLEAR")
                || float.IsNaN(observation.confidence) || float.IsInfinity(observation.confidence)
                || observation.confidence < .75f;
        }

        public static PatrolDecision Apply(PatrolDecision proposed, PatrolObservation observation,
            int attempts, bool inferenceValid, out bool escalated, int maximum = MaximumReobservations)
        {
            escalated = false;
            string action = PatrolActionMapper.Map(proposed, out bool unsupported).actionType;
            if (!inferenceValid || unsupported || (action != "CONTINUE" && action != "STOP" && action != "REOBSERVE"))
            {
                escalated = true;
                return Stop("Deterministic safety envelope: unavailable/invalid/unsupported decision; STOP for human review.");
            }
            if (attempts >= maximum && (Unresolved(observation) || action == "REOBSERVE"))
            {
                escalated = true;
                return Stop("Deterministic safety envelope: re-observation budget exhausted; STOP for human review.");
            }
            if (action == "CONTINUE" && Unresolved(observation))
                return new PatrolDecision { riskLevel = "UNKNOWN", action = "REOBSERVE", confidence = 1f,
                    requiresReobservation = true, reason = "Safety envelope: unresolved evidence cannot authorize CONTINUE." };
            return proposed;
        }

        public static PatrolDecision Stop(string reason)
        {
            return new PatrolDecision { riskLevel = "DANGER", action = "STOP", reason = reason,
                requiresReobservation = false, confidence = 1f };
        }
    }
}
