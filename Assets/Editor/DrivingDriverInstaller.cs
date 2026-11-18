using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OriginalKartRacer.Vehicle;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OriginalKartRacer.EditorTools
{
    /// <summary>
    /// Imports the supplied Mixamo driving character, builds a reusable prefab and
    /// installs it on every kart in the Coast scene. The operation is idempotent and
    /// is also available from Neon Harbor/Install Driving Character.
    /// </summary>
    [InitializeOnLoad]
    public static class DrivingDriverInstaller
    {
        public const string ModelPath = "Assets/Art/Characters/Driver/Driving.fbx";
        public const string ControllerPath = "Assets/Art/Characters/Driver/DrivingDriver.controller";
        public const string PrefabPath = "Assets/Prefabs/Characters/DrivingDriver.prefab";
        public const string ScenePath = "Assets/Scenes/Race_Coast_02.unity";
        private const string GeneratedMaterialPath = "Assets/Materials/Generated/ImportedDriverSuit.mat";
        private static readonly HashSet<string> PlaceholderNames = new HashSet<string>
        {
            "DriverTorso", "SuitChest", "UpperArm", "ForeArm", "Glove",
            "Thigh", "Shin", "Boot", "Neck", "HelmetShell", "Visor",
            "HelmetChin", "HelmetStripe"
        };
        private static bool running;

        static DrivingDriverInstaller()
        {
            EditorApplication.delayCall += AutoInstall;
        }

        private static void AutoInstall()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
            Install();
        }

        [MenuItem("Neon Harbor/Install Driving Character")]
        public static void Install()
        {
            if (running) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Install;
                return;
            }

            running = true;
            try
            {
                EnsureFolders();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                if (model == null)
                {
                    Debug.LogWarning("[DrivingDriver] Waiting for Driving.fbx to finish importing.");
                    EditorApplication.delayCall += Install;
                    return;
                }

                if (ConfigureImporter())
                {
                    // SaveAndReimport refreshes the Avatar and animation clip. Continue
                    // on the next editor tick/domain reload instead of using stale data.
                    EditorApplication.delayCall += Install;
                    return;
                }

                var clip = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                    .OfType<AnimationClip>()
                    .Where(candidate => !candidate.name.StartsWith("__preview__", StringComparison.Ordinal))
                    .OrderByDescending(candidate => candidate.length)
                    .FirstOrDefault();
                if (clip == null) throw new InvalidOperationException("Driving.fbx contains no importable animation clip.");

                var controller = EnsureController(clip);
                var material = EnsureDriverMaterial();
                var prefab = EnsureDriverPrefab(model, controller, material);
                InstallIntoCoastScene(prefab);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[DrivingDriver] Installed Humanoid driving character, looping animation, prefab and Coast scene instances.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                running = false;
            }
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Art/Characters/Driver");
            Directory.CreateDirectory("Assets/Prefabs/Characters");
            Directory.CreateDirectory("Assets/Materials/Generated");
        }

        /// <returns>True when the model was reimported and installation should resume later.</returns>
        private static bool ConfigureImporter()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) return false;
            bool changed = false;

            if (!importer.importAnimation) { importer.importAnimation = true; changed = true; }
            if (importer.animationType != ModelImporterAnimationType.Human) { importer.animationType = ModelImporterAnimationType.Human; changed = true; }
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel) { importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; changed = true; }
            if (importer.importCameras) { importer.importCameras = false; changed = true; }
            if (importer.importLights) { importer.importLights = false; changed = true; }
            if (importer.materialImportMode != ModelImporterMaterialImportMode.None) { importer.materialImportMode = ModelImporterMaterialImportMode.None; changed = true; }
            if (importer.animationCompression != ModelImporterAnimationCompression.Optimal) { importer.animationCompression = ModelImporterAnimationCompression.Optimal; changed = true; }

            var clips = importer.defaultClipAnimations;
            if (clips != null && clips.Length > 0)
            {
                for (int index = 0; index < clips.Length; index++)
                {
                    var clip = clips[index];
                    if (clip.name != "Driving_Loop") { clip.name = "Driving_Loop"; changed = true; }
                    if (!clip.loopTime) { clip.loopTime = true; changed = true; }
                    if (!clip.loopPose) { clip.loopPose = true; changed = true; }
                    if (!clip.lockRootRotation) { clip.lockRootRotation = true; changed = true; }
                    if (!clip.lockRootHeightY) { clip.lockRootHeightY = true; changed = true; }
                    if (!clip.lockRootPositionXZ) { clip.lockRootPositionXZ = true; changed = true; }
                    clip.keepOriginalOrientation = true;
                    clip.keepOriginalPositionY = true;
                    clip.keepOriginalPositionXZ = true;
                    clips[index] = clip;
                }
                importer.clipAnimations = clips;
            }

            if (!changed) return false;
            importer.SaveAndReimport();
            return true;
        }

        private static AnimatorController EnsureController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states.Select(child => child.state).FirstOrDefault(candidate => candidate.name == "Driving_Loop");
            if (state == null) state = stateMachine.AddState("Driving_Loop");
            state.motion = clip;
            state.speed = 1f;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Material EnsureDriverMaterial()
        {
            var existingSuit = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Generated/RaceSuit.mat");
            if (existingSuit != null) return existingSuit;

            var material = AssetDatabase.LoadAssetAtPath<Material>(GeneratedMaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = "ImportedDriverSuit" };
                AssetDatabase.CreateAsset(material, GeneratedMaterialPath);
            }
            var color = new Color(.055f, .13f, .24f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .08f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .32f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject EnsureDriverPrefab(GameObject model, RuntimeAnimatorController controller, Material material)
        {
            var temporary = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (temporary == null) temporary = UnityEngine.Object.Instantiate(model);
            temporary.name = "DrivingDriver";
            temporary.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            temporary.transform.localScale = Vector3.one;

            foreach (var camera in temporary.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(camera.gameObject);
            foreach (var light in temporary.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(light.gameObject);
            foreach (var renderer in temporary.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++) materials[index] = material;
                renderer.sharedMaterials = materials;
                renderer.updateWhenOffscreen = true;
            }

            var animator = temporary.GetComponentInChildren<Animator>(true);
            if (animator == null) animator = temporary.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var prefab = PrefabUtility.SaveAsPrefabAsset(temporary, PrefabPath);
            UnityEngine.Object.DestroyImmediate(temporary);
            if (prefab == null) throw new InvalidOperationException("Could not create DrivingDriver prefab.");
            return prefab;
        }

        private static void InstallIntoCoastScene(GameObject prefab)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                bool safeUntitled = string.IsNullOrEmpty(scene.path) && IsDefaultUntitled(scene);
                if (!safeUntitled && scene.isDirty)
                {
                    Debug.LogWarning("[DrivingDriver] Assets are ready, but the current scene has unsaved changes. Open Race_Coast_02 and run Neon Harbor/Install Driving Character.");
                    return;
                }
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var karts = UnityEngine.Object.FindObjectsOfType<KartController>(true);
            GameObject selectedDriver = null;
            foreach (var kart in karts)
            {
                RemovePlaceholderDriver(kart.transform);
                var existing = kart.transform.Find("DriverAnchor");
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

                var anchor = new GameObject("DriverAnchor").transform;
                anchor.SetParent(kart.transform, false);
                anchor.localPosition = new Vector3(0f, 0f, -.35f);
                anchor.localRotation = Quaternion.identity;
                anchor.localScale = Vector3.one * 1.85f;

                var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null) continue;
                instance.name = "DrivingDriver";
                instance.transform.SetParent(anchor, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                var animator = instance.GetComponentInChildren<Animator>(true);
                if (animator != null) animator.applyRootMotion = false;
                if (kart.name == "PlayerKart") selectedDriver = instance;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (selectedDriver != null)
            {
                Selection.activeGameObject = selectedDriver;
                if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        private static bool IsDefaultUntitled(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            if (roots.Length > 2) return false;
            return roots.All(root => root.name == "Main Camera" || root.name == "Directional Light");
        }

        private static void RemovePlaceholderDriver(Transform kart)
        {
            for (int index = kart.childCount - 1; index >= 0; index--)
            {
                var child = kart.GetChild(index);
                if (PlaceholderNames.Contains(child.name)) UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    internal sealed class DrivingDriverPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (importedAssets.Contains(DrivingDriverInstaller.ModelPath)) EditorApplication.delayCall += DrivingDriverInstaller.Install;
        }
    }
}
