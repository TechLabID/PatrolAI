using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using PatrolAI.Runtime;
using UnityEngine;
using VisionGuard.Phase3;

namespace PatrolAI.Scenarios
{
    /// <summary>
    /// Reads the actual ignored Phase 3 agent result produced after a known start time.
    /// It exposes only structured decision inputs; no image, absolute path, or private log.
    /// </summary>
    public static class PatrolReobservationEvidenceReader
    {
        // Read-only compatibility bridge: no inherited controller changes.
        public static string CurrentSessionId()
        {
            var controller = UnityEngine.Object.FindAnyObjectByType<VGPhase3AgentController>();
            var field = typeof(VGPhase3AgentController).GetField("sessionId",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return controller == null || field == null ? null : field.GetValue(controller) as string;
        }

        public static bool TryReadLatestSince(
            DateTime startedUtc,
            string sessionId,
            PatrolObservation initialObservation,
            out PatrolObservation updatedObservation,
            out string error)
        {
            updatedObservation = null;
            error = string.Empty;
            try
            {
                PatrolPythonRuntime.ResolveOrThrow(out string root, out string ignoredPython);
                string directory = Path.Combine(root, "exchange", "agent");
                if (string.IsNullOrWhiteSpace(sessionId)
                    || !System.Text.RegularExpressions.Regex.IsMatch(sessionId, "^inspection_[a-f0-9]{32}$"))
                    throw new InvalidOperationException("Inspection session identity unavailable.");
                var file = new FileInfo(Path.Combine(directory, sessionId + ".json"));
                if (!file.Exists || file.LastWriteTimeUtc < startedUtc)
                {
                    error = "No new agentic re-observation result was produced.";
                    return false;
                }

                return TryParseObservation(File.ReadAllText(file.FullName), startedUtc,
                    initialObservation, out updatedObservation, out error);
            }
            catch (Exception exception)
            {
                error = "Re-observation evidence read failed: " + exception.GetType().Name;
                return false;
            }
        }

        public static bool TryParseObservation(string json, DateTime startedUtc,
            PatrolObservation initialObservation, out PatrolObservation updatedObservation,
            out string error)
        {
            updatedObservation = null;
            error = string.Empty;
            try
            {
                AgentSession session;
                var serializer = new DataContractJsonSerializer(typeof(AgentSession));
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
                    session = serializer.ReadObject(stream) as AgentSession;

                VerificationResult result = session == null || session.final_plan == null
                    ? null : session.final_plan.result;
                Evidence secondary = session == null ? null : session.secondary_evidence;
                if (result == null || secondary == null)
                {
                    error = "Agentic result did not contain secondary evidence.";
                    return false;
                }
                if (!DateTime.TryParse(secondary.timestamp, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out DateTime captured)
                    || captured.ToUniversalTime() < startedUtc
                    || float.IsNaN(secondary.confidence) || float.IsInfinity(secondary.confidence)
                    || secondary.confidence < 0f || secondary.confidence > 1f)
                    throw new InvalidOperationException("Secondary evidence is stale or invalid.");

                string evidenceStatus = result.verification == "VERIFIED"
                    && secondary.evidence_quality == "SUFFICIENT"
                    ? "VERIFIED"
                    : result.verification == "REJECTED" ? "CONFLICT" : "INSUFFICIENT";
                string condition = evidenceStatus != "VERIFIED" ? "UNKNOWN"
                    : string.IsNullOrWhiteSpace(result.final_condition)
                    ? "UNKNOWN" : result.final_condition;

                updatedObservation = new PatrolObservation
                {
                    observationId = "phase7-reobserve-" + secondary.observation_id,
                    timestamp = secondary.timestamp,
                    source = "VG_CCTV_01 + VG_MOBILE_01 agentic re-observation",
                    detectedCondition = condition,
                    evidenceStatus = evidenceStatus,
                    confidence = secondary.confidence,
                    location = initialObservation == null
                        ? "Industrial inspection point A"
                        : initialObservation.location,
                    additionalContext = "sources=2; physical re-observation complete; "
                        + "prior_condition=" + Safe(initialObservation == null
                            ? null : initialObservation.detectedCondition)
                        + "; secondary_condition=" + Safe(secondary.condition)
                        + "; secondary_quality=" + Safe(secondary.evidence_quality)
                        + "; verification=" + Safe(result.verification)
                        + "; fusion_reason=" + Safe(result.reason)
                        + "; reason only from the stated evidence quality and verification."
                };
                return true;
            }
            catch (Exception exception)
            {
                error = "Re-observation evidence read failed: " + exception.GetType().Name;
                return false;
            }
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "UNKNOWN" : value.Trim();
        }

        [DataContract]
        private sealed class AgentSession
        {
            [DataMember] public Evidence secondary_evidence;
            [DataMember] public FinalPlan final_plan;
        }

        [DataContract]
        private sealed class Evidence
        {
            [DataMember] public string observation_id;
            [DataMember] public string timestamp;
            [DataMember] public string condition;
            [DataMember] public float confidence;
            [DataMember] public string evidence_quality;
        }

        [DataContract]
        private sealed class FinalPlan
        {
            [DataMember] public VerificationResult result;
        }

        [DataContract]
        private sealed class VerificationResult
        {
            [DataMember] public string final_condition;
            [DataMember] public string verification;
            [DataMember] public string reason;
        }
    }
}
