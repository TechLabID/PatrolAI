using UnityEditor;
using UnityEngine;

namespace VisionGuard.Phase4.Editor
{
    [CustomEditor(typeof(VGPhase4FusionController))]
    public sealed class VGPhase4FusionControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var controller = (VGPhase4FusionController)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("State", controller.State);
            EditorGUILayout.LabelField("Fusion Status", controller.FusionStatus);
            EditorGUILayout.LabelField("Verification", controller.Verification);
            EditorGUILayout.LabelField("Action", controller.Action);
            EditorGUILayout.LabelField("Final Condition", controller.FinalCondition);
            EditorGUILayout.LabelField("Reason", controller.Reason);
            EditorGUILayout.LabelField("Result", controller.ResultPath);

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || controller.IsRunning))
            {
                if (GUILayout.Button("RUN MULTI-SOURCE FUSION", GUILayout.Height(40f)))
                    controller.RunMultiSourceFusion();
            }

            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to capture and fuse all three sources.",
                    MessageType.Info);
        }
    }
}
