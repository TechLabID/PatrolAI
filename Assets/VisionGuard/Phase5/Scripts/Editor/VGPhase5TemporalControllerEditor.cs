using UnityEditor;
using UnityEngine;

namespace VisionGuard.Phase5.Editor
{
    [CustomEditor(typeof(VGPhase5TemporalController))]
    public sealed class VGPhase5TemporalControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var controller = (VGPhase5TemporalController)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Evaluation Run", controller.EvaluationRun.ToString());
            EditorGUILayout.LabelField("State", controller.State);
            EditorGUILayout.LabelField("Outcome", controller.Outcome);
            EditorGUILayout.LabelField("Previous", controller.PreviousCondition);
            EditorGUILayout.LabelField("Current", controller.CurrentCondition);
            EditorGUILayout.LabelField("Change ID", controller.ChangeId);
            EditorGUILayout.LabelField("Incident ID", controller.IncidentId);
            EditorGUILayout.LabelField("Reason", controller.Reason);
            EditorGUILayout.LabelField("Result", controller.ResultPath);

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(
                       !EditorApplication.isPlaying || controller.IsRunning))
            {
                if (GUILayout.Button(
                        "EVALUATE LATEST VERIFIED FUSION", GUILayout.Height(40f)))
                    controller.EvaluateLatestFusion();
            }

            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox(
                    "Enter Play Mode, run Phase 4 fusion, then evaluate Phase 5.",
                    MessageType.Info);
        }
    }
}
