using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VisionGuard.Phase2
{
    // Passive until the user invokes the component context menu in Play Mode.
    // No editor hooks, external control endpoint, polling, or movement.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class VGObservationExporter : MonoBehaviour
    {
        [SerializeField] private string cameraId = "VG_CCTV_01";
        [SerializeField] private string targetId = "SWG_05/Indicator_03";
        [SerializeField] private int width = 1280;
        [SerializeField] private int height = 720;
        [Tooltip("Top-left image pixels: x, y, width, height. Calibrate after first Unity render.")]
        [SerializeField] private RectInt roi = new RectInt(604, 324, 72, 72);

        public string LastManifestPath { get; private set; }
        public string LastError { get; private set; }

        [Serializable] private sealed class Pose
        {
            public float[] position;
            public float[] rotation_xyzw;
        }
        [Serializable] private sealed class Metadata
        {
            public string target_id;
            public int[] roi;
            public string provenance = "unity_render";
        }
        [Serializable] private sealed class Observation
        {
            public int schema_version = 1;
            public string observation_id, camera_id, timestamp;
            public string source_type = "unity";
            public string frame, frame_sha256;
            public int width, height;
            public Pose camera_pose;
            public Metadata metadata;
        }
        [Serializable] private sealed class Trace
        {
            public string @event, observation_id, manifest, message;
        }

        [ContextMenu("VISIONGUARD/Capture One Observation")]
        public void CaptureOneObservation()
        {
            LastManifestPath = null;
            LastError = null;
            if (!Application.isPlaying)
            {
                LastError = "Capture requires Play Mode.";
                Debug.LogWarning("Enter Play Mode first, then invoke Capture One Observation.", this);
                return;
            }
            var camera = GetComponent<Camera>();
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            float previousAspect = camera.aspect;
            RenderTexture target = null;
            Texture2D image = null;
            string pngPath = null, jsonPath = null;
            try
            {
                if (width < 16 || height < 16 || width > 4096 || height > 4096 ||
                    roi.x < 0 || roi.y < 0 || roi.width < 8 || roi.height < 8 ||
                    roi.xMax > width || roi.yMax > height)
                    throw new InvalidOperationException("Invalid image dimensions or calibration ROI.");
                target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB) { antiAliasing = 1 };
                if (!target.Create()) throw new InvalidOperationException("RenderTexture creation failed.");
                camera.targetTexture = target;
                camera.aspect = (float)width / height;
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request))
                    throw new InvalidOperationException("Active render pipeline does not support URP SingleCameraRequest.");
                string timestamp = DateTime.UtcNow.ToString("O");
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                image = new Texture2D(width, height, TextureFormat.RGB24, false, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                image.Apply(false, false);
                byte[] png = image.EncodeToPNG();
                if (png == null || png.Length == 0) throw new IOException("PNG encoding failed.");
                string id = "obs_" + Guid.NewGuid().ToString("N");
                Vector3 p = camera.transform.position;
                Quaternion q = camera.transform.rotation;
                string hash;
                using (var sha = SHA256.Create())
                    hash = BitConverter.ToString(sha.ComputeHash(png)).Replace("-", "").ToLowerInvariant();
                var observation = new Observation
                {
                    observation_id = id, camera_id = cameraId, timestamp = timestamp,
                    frame = id + ".png", frame_sha256 = hash, width = width, height = height,
                    camera_pose = new Pose { position = new[] { p.x, p.y, p.z },
                        rotation_xyzw = new[] { q.x, q.y, q.z, q.w } },
                    metadata = new Metadata { target_id = targetId,
                        roi = new[] { roi.x, roi.y, roi.width, roi.height } }
                };
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "exchange", "observations"));
                Directory.CreateDirectory(directory);
                pngPath = Path.Combine(directory, observation.frame);
                jsonPath = Path.Combine(directory, id + ".json");
                // Same-volume rename; unique IDs prevent overwriting earlier observations.
                Publish(pngPath, png);
                Publish(jsonPath, new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(observation, true)));
                LastManifestPath = jsonPath;
                Debug.Log(JsonUtility.ToJson(new Trace { @event = "OBSERVATION_CAPTURED",
                    observation_id = id, manifest = jsonPath }), this);
            }
            catch (Exception error)
            {
                LastError = error.Message;
                // Do not leave an apparently complete observation after a failed publication.
                if (pngPath != null && !File.Exists(jsonPath))
                {
                    try { File.Delete(pngPath); } catch (IOException) { /* retain original failure */ }
                }
                Debug.LogError(JsonUtility.ToJson(new Trace { @event = "PERCEPTION_FAILED",
                    message = error.Message }), this);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
                RenderTexture.active = previousActive;
                if (image != null) Destroy(image);
                if (target != null) { target.Release(); Destroy(target); }
            }
        }

        private static void Publish(string path, byte[] bytes)
        {
            string temporary = path + ".tmp";
            try
            {
                File.WriteAllBytes(temporary, bytes);
                File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }
}
