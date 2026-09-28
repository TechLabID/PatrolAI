using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using VisionGuard.Phase2;
using Debug = UnityEngine.Debug;

#pragma warning disable 0649 // Unity/JsonUtility populate these serialized fields.

namespace VisionGuard.Phase3
{
    [DisallowMultipleComponent]
    public sealed class VGPhase3AgentController : MonoBehaviour
    {
        [SerializeField] private VGObservationExporter primaryExporter;
        [SerializeField] private VGObservationExporter secondaryExporter;
        [SerializeField] private VGMobileInspectorMovement movementController;
        [SerializeField] private bool showStatusOverlay = true;

        public bool IsRunning { get; private set; }
        public string State { get; private set; } = "IDLE";

        private string primarySummary = "—";
        private string decisionSummary = "—";
        private string inspectorSummary = "VG_MOBILE_01 — HOME";
        private string secondarySummary = "—";
        private string finalSummary = "—";
        private string sessionId;

        [Serializable] private sealed class AgentResponse
        {
            public bool ok;
            public string session_id, state, action, reason, candidate_condition;
            public string verification, final_condition, primary_quality, secondary_quality, trace_path;
        }

        [Serializable] private sealed class TraceEvent
        {
            public string @event, state, action, reason, verification;
        }

        public void RunAgenticInspection()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before running the Phase 3 inspection.", this);
                return;
            }
            if (IsRunning)
            {
                Debug.LogWarning("An agentic inspection is already running.", this);
                return;
            }
            StartCoroutine(RunInspection());
        }

        private IEnumerator RunInspection()
        {
            IsRunning = true;
            sessionId = "inspection_" + Guid.NewGuid().ToString("N");
            primarySummary = decisionSummary = secondarySummary = finalSummary = "—";
            inspectorSummary = "VG_MOBILE_01 — RETURNING HOME";

            if (primaryExporter == null || secondaryExporter == null || movementController == null)
            {
                Fail("phase3_references_missing");
                yield break;
            }

            yield return movementController.MoveHomeIfNeeded();
            inspectorSummary = "VG_MOBILE_01 — HOME";

            State = "OBSERVE_PRIMARY";
            primaryExporter.CaptureOneObservation();
            if (string.IsNullOrEmpty(primaryExporter.LastManifestPath))
            {
                Fail("primary_capture_failed: " + primaryExporter.LastError);
                yield break;
            }

            State = "ASSESS_PRIMARY";
            AgentResponse primary;
            try
            {
                primary = RunPython("primary", primaryExporter.LastManifestPath, null);
            }
            catch (Exception error)
            {
                Fail("primary_perception_failed: " + error.Message);
                yield break;
            }
            primarySummary = primary.candidate_condition + " / " +
                             (string.IsNullOrEmpty(primary.primary_quality) ? "ASSESSED" : primary.primary_quality);

            if (primary.state != "REQUEST_REOBSERVATION")
            {
                State = "COMPLETE";
                decisionSummary = "PRIMARY EVIDENCE SUFFICIENT — NO RE-OBSERVATION";
                finalSummary = primary.verification + " / " + primary.final_condition;
                Log("AGENT_DECISION", State, primary.action, primary.reason, primary.verification);
                IsRunning = false;
                yield break;
            }

            State = "REQUEST_REOBSERVATION";
            decisionSummary = "REQUEST_REOBSERVATION — " + primary.reason;
            Log("AGENT_DECISION", State, "REQUEST_REOBSERVATION", primary.reason, "");
            Log("REOBSERVATION_REQUESTED", State, "MOVE_TO_INSPECTION_POSE", primary.reason, "");
            Log("ACTION_REQUESTED", State, "MOVE_TO_INSPECTION_POSE", "VG_MobileInspector_01", "");
            yield return null;

            State = "MOVE_INSPECTOR";
            inspectorSummary = "VG_MOBILE_01 — MOVING";
            yield return movementController.MoveToInspectionPose();
            if (!movementController.IsAtInspectionPose)
            {
                Fail("inspection_pose_not_reached");
                yield break;
            }
            inspectorSummary = "VG_MOBILE_01 — INSPECTION POSE";
            Log("ACTION_COMPLETED", State, "MOVE_TO_INSPECTION_POSE", "pose_reached", "");

            State = "OBSERVE_SECONDARY";
            secondaryExporter.CaptureOneObservation();
            if (string.IsNullOrEmpty(secondaryExporter.LastManifestPath))
            {
                Fail("secondary_capture_failed: " + secondaryExporter.LastError);
                yield break;
            }
            Log("SECONDARY_OBSERVATION_CAPTURED", State, "OBSERVE_AGAIN",
                secondaryExporter.LastManifestPath, "");

            State = "ASSESS_SECONDARY";
            AgentResponse secondary;
            try
            {
                secondary = RunPython("secondary", secondaryExporter.LastManifestPath, null);
            }
            catch (Exception error)
            {
                Fail("secondary_perception_failed: " + error.Message);
                yield break;
            }
            secondarySummary = secondary.final_condition + " / " + secondary.secondary_quality;
            State = "COMPLETE";
            finalSummary = secondary.verification + " / " + secondary.final_condition;
            Log("VERIFICATION_RESULT", State, "RECORD", secondary.reason, secondary.verification);
            IsRunning = false;
        }

        private AgentResponse RunPython(string stage, string manifest, string reason)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string python = Path.Combine(root, ".venv-phase2", "Scripts", "python.exe");
            if (!File.Exists(python))
                throw new FileNotFoundException("Phase 2 Python environment is missing", python);
            string arguments = "-m agent.run " + stage + " --session " + sessionId;
            if (!string.IsNullOrEmpty(manifest)) arguments += " --manifest " + Quote(manifest);
            if (!string.IsNullOrEmpty(reason)) arguments += " --reason " + Quote(reason);
            var info = new ProcessStartInfo
            {
                FileName = python,
                Arguments = arguments,
                WorkingDirectory = root,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(info))
            {
                if (process == null) throw new InvalidOperationException("Python process did not start.");
                string output = process.StandardOutput.ReadToEnd();
                string diagnostic = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (!string.IsNullOrWhiteSpace(diagnostic)) Debug.Log(diagnostic.Trim(), this);
                AgentResponse result = JsonUtility.FromJson<AgentResponse>(output.Trim());
                if (process.ExitCode != 0 || result == null || !result.ok)
                    throw new InvalidOperationException(result != null ? result.reason : "Invalid Python response");
                return result;
            }
        }

        private void Fail(string reason)
        {
            State = "FAILED";
            decisionSummary = reason;
            finalSummary = "UNRESOLVED / UNKNOWN";
            try
            {
                if (!string.IsNullOrEmpty(sessionId)) RunPython("fail", null, reason);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Could not persist Python failure result: " + error.Message, this);
            }
            Log("VERIFICATION_RESULT", State, "RECORD", reason, "UNRESOLVED");
            IsRunning = false;
        }

        private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

        private void Log(string eventName, string state, string action, string reason, string verification)
        {
            Debug.Log(JsonUtility.ToJson(new TraceEvent { @event = eventName, state = state,
                action = action, reason = reason, verification = verification }), this);
        }

        private void OnGUI()
        {
            if (!showStatusOverlay || !Application.isPlaying) return;
            const float width = 530f;
            GUI.Box(new Rect(18f, 18f, width, 190f), "VISIONGUARD AI — ACTIVE RE-OBSERVATION");
            GUI.Label(new Rect(34f, 48f, width - 30f, 22f), "State: " + State);
            GUI.Label(new Rect(34f, 72f, width - 30f, 22f), "Primary: " + primarySummary);
            GUI.Label(new Rect(34f, 96f, width - 30f, 22f), "Decision: " + decisionSummary);
            GUI.Label(new Rect(34f, 120f, width - 30f, 22f), "Inspector: " + inspectorSummary);
            GUI.Label(new Rect(34f, 144f, width - 30f, 22f), "Secondary: " + secondarySummary);
            GUI.Label(new Rect(34f, 168f, width - 30f, 22f), "Final: " + finalSummary);
        }
    }
}

#pragma warning restore 0649
