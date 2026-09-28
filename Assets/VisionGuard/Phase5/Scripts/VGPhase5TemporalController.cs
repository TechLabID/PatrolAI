using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using VisionGuard.Phase4;
using Debug = UnityEngine.Debug;

#pragma warning disable 0649 // Unity JsonUtility populates response fields.

namespace VisionGuard.Phase5
{
    [DisallowMultipleComponent]
    public sealed class VGPhase5TemporalController : MonoBehaviour
    {
        [SerializeField] private VGPhase4FusionController fusionController;

        public bool IsRunning { get; private set; }
        public int EvaluationRun { get; private set; }
        public string State { get; private set; } = "IDLE";
        public string Outcome { get; private set; } = "—";
        public string PreviousCondition { get; private set; } = "—";
        public string CurrentCondition { get; private set; } = "—";
        public string ChangeId { get; private set; } = "—";
        public string IncidentId { get; private set; } = "—";
        public string Reason { get; private set; } = "—";
        public string ResultPath { get; private set; } = "—";

        [Serializable]
        private sealed class TemporalResponse
        {
            public bool ok;
            public string outcome;
            public string previous_verified_condition;
            public string current_verified_condition;
            public string change_id;
            public string incident_id;
            public string reason;
            public string result_path;
        }

        [ContextMenu("VISIONGUARD/Evaluate Latest Verified Fusion")]
        public void EvaluateLatestFusion()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before evaluating Phase 5 temporal state.", this);
                return;
            }
            if (IsRunning)
                return;
            if (fusionController == null ||
                string.IsNullOrWhiteSpace(fusionController.ResultPath) ||
                fusionController.ResultPath == "—" ||
                !File.Exists(fusionController.ResultPath))
            {
                Fail("verified_fusion_result_required");
                return;
            }

            IsRunning = true;
            State = "EVALUATE_TEMPORAL_STATE";
            try
            {
                EvaluationRun++;
                TemporalResponse response = RunPython(fusionController.ResultPath);
                Outcome = ValueOrDash(response.outcome);
                PreviousCondition = ValueOrDash(response.previous_verified_condition);
                CurrentCondition = ValueOrDash(response.current_verified_condition);
                ChangeId = ValueOrDash(response.change_id);
                IncidentId = ValueOrDash(response.incident_id);
                Reason = ValueOrDash(response.reason);
                ResultPath = ValueOrDash(response.result_path);
                State = "COMPLETE";
                Debug.Log(JsonUtility.ToJson(response, true), this);
            }
            catch (Exception error)
            {
                Fail("temporal_evaluation_failed: " + error.Message);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private static TemporalResponse RunPython(string fusionPath)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string python = Path.Combine(root, ".venv-phase2", "Scripts", "python.exe");
            if (!File.Exists(python))
                throw new FileNotFoundException("Phase 2 Python environment is missing", python);

            var info = new ProcessStartInfo
            {
                FileName = python,
                Arguments = "-m agent.temporal.run --fusion " + Quote(fusionPath),
                WorkingDirectory = root,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(info))
            {
                if (process == null)
                    throw new InvalidOperationException("Phase 5 Python process did not start.");
                string output = process.StandardOutput.ReadToEnd();
                string diagnostic = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (!string.IsNullOrWhiteSpace(diagnostic))
                    Debug.Log(diagnostic.Trim());
                TemporalResponse response =
                    JsonUtility.FromJson<TemporalResponse>(output.Trim());
                if (process.ExitCode != 0 || response == null || !response.ok)
                    throw new InvalidOperationException(
                        response != null ? response.reason : "Invalid Phase 5 response");
                return response;
            }
        }

        private void Fail(string reason)
        {
            State = "FAILED";
            Outcome = "UNRESOLVED";
            Reason = reason;
            IsRunning = false;
            Debug.LogError(reason, this);
        }

        private static string ValueOrDash(string value)
        {
            return string.IsNullOrEmpty(value) ? "—" : value;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}

#pragma warning restore 0649
