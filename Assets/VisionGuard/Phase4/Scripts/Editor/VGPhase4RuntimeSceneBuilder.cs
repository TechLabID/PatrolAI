using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VisionGuard.Phase2;
using VisionGuard.Phase3;

namespace VisionGuard.Phase4.Editor
{
    [InitializeOnLoad]
    public static class VGPhase4RuntimeSceneBuilder
    {
        private const string Phase3Scene =
            "Assets/VisionGuard/Phase3/Scenes/VISIONGUARD_IndustrialRoom_Phase3_v01.unity";
        private const string Phase4Scene =
            "Assets/VisionGuard/Phase4/Scenes/VISIONGUARD_IndustrialRoom_Phase4_v01.unity";
        private static bool isConfiguring;

        static VGPhase4RuntimeSceneBuilder()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += EnsureOpenPhase4Scene;
        }

        [MenuItem("VISIONGUARD/Phase 4/Build Runtime Review Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase4Scene) == null &&
                !AssetDatabase.CopyAsset(Phase3Scene, Phase4Scene))
                throw new IOException("Could not create the additive Phase 4 scene copy.");

            Scene scene = EditorSceneManager.OpenScene(Phase4Scene, OpenSceneMode.Single);
            VGPhase4FusionController controller = EnsureConfigured(scene);
            Selection.activeObject = controller;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && scene.path == Phase4Scene)
                EnsureConfigured(scene);
        }

        private static void EnsureOpenPhase4Scene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.path == Phase4Scene)
                EnsureConfigured(scene);
        }

        private static VGPhase4FusionController EnsureConfigured(Scene scene)
        {
            if (isConfiguring || !scene.IsValid() || scene.path != Phase4Scene)
                return FindInScene<VGPhase4FusionController>(scene);

            isConfiguring = true;
            try
            {
                VGObservationExporter cctv01 = FindExporter(scene, "VG_CCTV_01");
                VGObservationExporter mobile01 = FindExporter(scene, "VG_MOBILE_01");
                if (cctv01 == null || mobile01 == null)
                    throw new MissingReferenceException(
                        "Sealed source exporters were not found in the Phase 4 scene copy.");

                bool changed = false;
                var mobileSerialized = new SerializedObject(mobile01);
                changed |= AssignRectIntIfDifferent(
                    mobileSerialized, "roi", new RectInt(592, 312, 96, 96));
                mobileSerialized.ApplyModifiedPropertiesWithoutUndo();

                VGObservationExporter cctv02 = FindExporter(scene, "VG_CCTV_02");
                if (cctv02 == null)
                {
                    GameObject cctv02Object = Object.Instantiate(cctv01.gameObject);
                    cctv02Object.name = "VG_CCTV_02";
                    cctv02Object.transform.SetParent(cctv01.transform.parent, true);
                    cctv02 = cctv02Object.GetComponent<VGObservationExporter>();
                    var cctv02Serialized = new SerializedObject(cctv02);
                    cctv02Serialized.FindProperty("cameraId").stringValue = "VG_CCTV_02";
                    cctv02Serialized.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                if (Vector3.Distance(cctv02.transform.position, cctv01.transform.position) < 0.001f &&
                    Quaternion.Angle(cctv02.transform.rotation, cctv01.transform.rotation) < 0.1f)
                {
                    cctv02.transform.position =
                        cctv01.transform.position + cctv01.transform.forward * 0.4f;
                    changed = true;
                }

                VGMobileInspectorMovement mobileMovement =
                    FindInScene<VGMobileInspectorMovement>(scene);
                if (mobileMovement == null)
                    throw new MissingReferenceException(
                        "Validated mobile movement component was not found in the Phase 4 scene copy.");

                VGPhase4FusionController controller =
                    FindInScene<VGPhase4FusionController>(scene);
                if (controller == null)
                {
                    GameObject controllerObject = new GameObject("VG_Phase4_FusionController");
                    SceneManager.MoveGameObjectToScene(controllerObject, scene);
                    controller = controllerObject.AddComponent<VGPhase4FusionController>();
                    changed = true;
                }

                var serialized = new SerializedObject(controller);
                changed |= AssignIfDifferent(serialized, "cctv01Exporter", cctv01);
                changed |= AssignIfDifferent(serialized, "cctv02Exporter", cctv02);
                changed |= AssignIfDifferent(serialized, "mobile01Exporter", mobile01);
                changed |= AssignIfDifferent(serialized, "mobileMovement", mobileMovement);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("VG_PHASE4_RUNTIME_SCENE_READY", controller);
                }
                return controller;
            }
            finally
            {
                isConfiguring = false;
            }
        }

        private static bool AssignIfDifferent(
            SerializedObject serialized,
            string propertyName,
            Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property.objectReferenceValue == value)
                return false;
            property.objectReferenceValue = value;
            return true;
        }

        private static bool AssignRectIntIfDifferent(
            SerializedObject serialized,
            string propertyName,
            RectInt value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property.rectIntValue == value)
                return false;
            property.rectIntValue = value;
            return true;
        }

        private static VGObservationExporter FindExporter(Scene scene, string cameraId)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (VGObservationExporter exporter in
                         root.GetComponentsInChildren<VGObservationExporter>(true))
                {
                    var serialized = new SerializedObject(exporter);
                    if (serialized.FindProperty("cameraId").stringValue == cameraId)
                        return exporter;
                }
            }
            return null;
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
