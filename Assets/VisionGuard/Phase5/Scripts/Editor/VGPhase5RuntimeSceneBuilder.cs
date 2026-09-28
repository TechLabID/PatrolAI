using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VisionGuard.Phase4;

namespace VisionGuard.Phase5.Editor
{
    [InitializeOnLoad]
    public static class VGPhase5RuntimeSceneBuilder
    {
        private const string Phase4Scene =
            "Assets/VisionGuard/Phase4/Scenes/VISIONGUARD_IndustrialRoom_Phase4_v01.unity";
        private const string Phase5Scene =
            "Assets/VisionGuard/Phase5/Scenes/VISIONGUARD_IndustrialRoom_Phase5_v01.unity";
        private static bool isConfiguring;

        static VGPhase5RuntimeSceneBuilder()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += EnsureOpenPhase5Scene;
        }

        [MenuItem("VISIONGUARD/Phase 5/Build Runtime Review Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase5Scene) == null &&
                !AssetDatabase.CopyAsset(Phase4Scene, Phase5Scene))
                throw new IOException("Could not create the additive Phase 5 scene copy.");

            Scene scene = EditorSceneManager.OpenScene(Phase5Scene, OpenSceneMode.Single);
            VGPhase5TemporalController controller = EnsureConfigured(scene);
            Selection.activeObject = controller;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode &&
                scene.path == Phase5Scene)
                EnsureConfigured(scene);
        }

        private static void EnsureOpenPhase5Scene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.path == Phase5Scene)
                EnsureConfigured(scene);
        }

        private static VGPhase5TemporalController EnsureConfigured(Scene scene)
        {
            if (isConfiguring || !scene.IsValid() || scene.path != Phase5Scene)
                return FindInScene<VGPhase5TemporalController>(scene);

            isConfiguring = true;
            try
            {
                VGPhase4FusionController fusion =
                    FindInScene<VGPhase4FusionController>(scene);
                if (fusion == null)
                    throw new MissingReferenceException(
                        "Validated Phase 4 fusion controller was not found in the Phase 5 copy.");

                bool changed = false;
                VGPhase5TemporalController temporal =
                    FindInScene<VGPhase5TemporalController>(scene);
                if (temporal == null)
                {
                    var temporalObject = new GameObject("VG_Phase5_TemporalController");
                    SceneManager.MoveGameObjectToScene(temporalObject, scene);
                    temporal = temporalObject.AddComponent<VGPhase5TemporalController>();
                    changed = true;
                }

                var serialized = new SerializedObject(temporal);
                SerializedProperty reference =
                    serialized.FindProperty("fusionController");
                if (reference.objectReferenceValue != fusion)
                {
                    reference.objectReferenceValue = fusion;
                    changed = true;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("VG_PHASE5_RUNTIME_SCENE_READY", temporal);
                }
                return temporal;
            }
            finally
            {
                isConfiguring = false;
            }
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid())
                return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }
            return null;
        }
    }
}
