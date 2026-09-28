using UnityEditor;
using UnityEngine;

namespace VisionGuard.Phase2.Editor
{
    [CustomEditor(typeof(VGObservationExporter))]
    public sealed class VGObservationExporterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10f);

            var exporter = (VGObservationExporter)target;
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || !exporter.isActiveAndEnabled))
            {
                if (GUILayout.Button("CAPTURE ONE OBSERVATION", GUILayout.Height(36f)))
                    exporter.CaptureOneObservation();
            }

            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to capture an observation.", MessageType.Info);
        }
    }
}
