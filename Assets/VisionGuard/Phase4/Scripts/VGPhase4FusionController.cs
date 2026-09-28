using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using VisionGuard.Phase2;
using VisionGuard.Phase3;
using Debug = UnityEngine.Debug;

#pragma warning disable 0649 // Unity JsonUtility populates response fields.

namespace VisionGuard.Phase4
{
    [DisallowMultipleComponent]
    public sealed class VGPhase4FusionController : MonoBehaviour
    {
        private static readonly RectInt MobileInspectionRoi = new RectInt(592, 312, 96, 96);
        private static readonly FieldInfo ExporterRoiField =
            typeof(VGObservationExporter).GetField(
                "roi", BindingFlags.Instance | BindingFlags.NonPublic);

        [SerializeField] private VGObservationExporter cctv01Exporter;
        [SerializeField] private VGObservationExporter cctv02Exporter;
        [SerializeField] private VGObservationExporter mobile01Exporter;
        [SerializeField] private VGMobileInspectorMovement mobileMovement;

        public bool IsRunning { get; private set; }
        public string State { get; private set; } = "IDLE";
        public string FusionStatus { get; private set; } = "—";
        public string Verification { get; private set; } = "—";
        public string Action { get; private set; } = "—";
        public string FinalCondition { get; private set; } = "—";
        public string Reason { get; private set; } = "—";
        public string ResultPath { get; private set; } = "—";

        [Serializable]
        private sealed class FusionResponse
        {
            public bool ok;
            public string fusion_status;
            public string verification;
            public string action;
            public string final_condition;
            public string reason;
            public string result_path;
            public string trace_path;
        }

        [ContextMenu("VISIONGUARD/Run Multi-Source Fusion")]
        public void RunMultiSourceFusion()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before running Phase 4 fusion.", this);
                return;
            }
            if (IsRunning)
            {
                Debug.LogWarning("A Phase 4 fusion run is already active.", this);
                return;
            }
            StartCoroutine(RunFusion());
        }

        private IEnumerator RunFusion()
        {
            IsRunning = true;
            State = "MOVE_MOBILE_HOME";
            FusionStatus = Verification = Action = FinalCondition = Reason = ResultPath = "—";

            if (cctv01Exporter == null || cctv02Exporter == null ||
                mobile01Exporter == null || mobileMovement == null)
            {
                Fail("phase4_reference_missing");
                yield break;
            }

            yield return mobileMovement.MoveHomeIfNeeded();
            if (mobileMovement.IsMoving || mobileMovement.IsAtInspectionPose)
            {
                Fail("mobile_home_pose_not_reached");
                yield break;
            }

            State = "CAPTURE_CCTV_EVIDENCE";
            string[] manifests = new string[3];
            VGObservationExporter[] cctvExporters = { cctv01Exporter, cctv02Exporter };

            for (int index = 0; index < cctvExporters.Length; index++)
            {
                VGObservationExporter exporter = cctvExporters[index];
                exporter.CaptureOneObservation();
                if (string.IsNullOrEmpty(exporter.LastManifestPath))
                {
                    Fail("capture_failed: " + exporter.LastError);
                    yield break;
                }
                manifests[index] = exporter.LastManifestPath;
                yield return null;
            }

            State = "MOVE_MOBILE_TO_INSPECTION_POSE";
            yield return mobileMovement.MoveToInspectionPose();
            if (!mobileMovement.IsAtInspectionPose)
            {
                Fail("mobile_inspection_pose_not_reached");
                yield break;
            }

            State = "CAPTURE_MOBILE_EVIDENCE";
            if (ExporterRoiField == null)
            {
                Fail("mobile_roi_configuration_unavailable");
                yield break;
            }
            ExporterRoiField.SetValue(mobile01Exporter, MobileInspectionRoi);
            RectInt effectiveRoi = (RectInt)ExporterRoiField.GetValue(mobile01Exporter);
            Debug.Log(
                $"VG_PHASE4_CAPTURE_CONFIG source=VG_MOBILE_01 " +
                $"roi=[{effectiveRoi.x},{effectiveRoi.y},{effectiveRoi.width},{effectiveRoi.height}]",
                this);
            mobile01Exporter.CaptureOneObservation();
            if (string.IsNullOrEmpty(mobile01Exporter.LastManifestPath))
            {
                Fail("capture_failed: " + mobile01Exporter.LastError);
                yield break;
            }
            manifests[2] = mobile01Exporter.LastManifestPath;
            yield return null;

            State = "FUSE_EVIDENCE";
            FusionResponse response;
            try
            {
                response = RunPython(manifests);
            }
            catch (Exception error)
            {
                Fail("fusion_failed: " + error.Message);
                yield break;
            }

            FusionStatus = response.fusion_status;
            Verification = response.verification;
            Action = response.action;
            FinalCondition = response.final_condition;
            Reason = response.reason;
            ResultPath = response.result_path;
            State = "COMPLETE";
            IsRunning = false;

            Debug.Log(JsonUtility.ToJson(response, true), this);
        }

        private FusionResponse RunPython(string[] manifests)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string python = Path.Combine(root, ".venv-phase2", "Scripts", "python.exe");
            if (!File.Exists(python))
                throw new FileNotFoundException("Phase 2 Python environment is missing", python);

            string session = "fusion_" + Guid.NewGuid().ToString("N");
            var arguments = new StringBuilder("-m agent.fusion.run --session ");
            arguments.Append(session);
            foreach (string manifest in manifests)
                arguments.Append(" --manifest ").Append(Quote(manifest));

            var info = new ProcessStartInfo
            {
                FileName = python,
                Arguments = arguments.ToString(),
                WorkingDirectory = root,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = Process.Start(info))
            {
                if (process == null)
                    throw new InvalidOperationException("Phase 4 Python process did not start.");
                string output = process.StandardOutput.ReadToEnd();
                string diagnostic = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (!string.IsNullOrWhiteSpace(diagnostic))
                    Debug.Log(diagnostic.Trim(), this);
                FusionResponse response = JsonUtility.FromJson<FusionResponse>(output.Trim());
                if (process.ExitCode != 0 || response == null || !response.ok)
                    throw new InvalidOperationException(
                        response != null ? response.reason : "Invalid Phase 4 response");
                return response;
            }
        }

        private void Fail(string reason)
        {
            State = "FAILED";
            FusionStatus = "INSUFFICIENT";
            Verification = "UNRESOLVED";
            Action = "REQUEST_ADDITIONAL_EVIDENCE";
            FinalCondition = "UNKNOWN";
            Reason = reason;
            IsRunning = false;
            Debug.LogError(reason, this);
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}

#pragma warning restore 0649
