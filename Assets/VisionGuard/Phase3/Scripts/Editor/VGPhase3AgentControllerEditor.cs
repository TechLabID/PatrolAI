using UnityEditor;
using UnityEngine;

namespace VisionGuard.Phase3.Editor
{
    [CustomEditor(typeof(VGPhase3AgentController))]
    public sealed class VGPhase3AgentControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10f);
            var controller = (VGPhase3AgentController)target;
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || controller.IsRunning))
            {
                if (GUILayout.Button("RUN AGENTIC INSPECTION", GUILayout.Height(40f)))
                {
                    // The failed live manifest retained the exporter's 72px default
                    // despite the Phase 3 scene's 96px calibration. Apply that same
                    // measured calibration to the exact referenced live exporter.
                    serializedObject.Update();
                    var secondary = serializedObject.FindProperty("secondaryExporter").objectReferenceValue;
                    if (secondary != null)
                    {
                        var exporter = new SerializedObject(secondary);
                        exporter.FindProperty("roi").rectIntValue = new RectInt(592, 312, 96, 96);
                        exporter.ApplyModifiedPropertiesWithoutUndo();
                        exporter.Update();
                        Debug.Log($"Phase 3 secondary calibration: {secondary.name}, " +
                            $"entity={secondary.GetEntityId()}, " +
                            $"ROI={exporter.FindProperty("roi").rectIntValue}", secondary);
                    }
                    controller.RunAgenticInspection();
                }
            }
            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to run the active re-observation loop.", MessageType.Info);
        }
    }
}
