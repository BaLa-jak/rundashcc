using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace RunDashC.EditorTools
{
    /// <summary>
    /// Genera las escenas del juego. Menú: RunDashC > Construir escenas.
    /// Batchmode: -executeMethod RunDashC.EditorTools.SceneBuilder.BuildAll
    /// OJO: sobrescribe las escenas; los cambios hechos a mano en ellas se pierden.
    /// </summary>
    public static class SceneBuilder
    {
        public const string SceneDir = "Assets/Game/Scenes";
        public const string VolumePath = "Assets/Game/Settings/RunDashVolume.asset";

        static readonly string[] Order = { SceneNames.Menu, SceneNames.LevelSelect, SceneNames.Game, SceneNames.GameOver, SceneNames.Help, SceneNames.Shop };

        [MenuItem("RunDashC/Construir escenas")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh();
            // Cada builder por separado: si uno falla, los demás se siguen generando.
            Run(SceneNames.Menu, MenuSceneBuilder.Build);
            Run(SceneNames.LevelSelect, LevelSelectSceneBuilder.Build);
            Run(SceneNames.Game, GameSceneBuilder.Build);
            Run(SceneNames.GameOver, GameOverSceneBuilder.Build);
            Run(SceneNames.Help, HelpSceneBuilder.Build);
            Run(SceneNames.Shop, ShopSceneBuilder.Build);

            EditorBuildSettings.scenes = Order
                .Select(n => new EditorBuildSettingsScene(ScenePath(n), true))
                .ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePath(SceneNames.Menu));
            Debug.Log("[RunDashC] Escenas construidas: " + string.Join(", ", Order));
        }

        static void Run(string name, System.Action build)
        {
            try { build(); }
            catch (System.Exception e) { Debug.LogError($"[RunDashC] Falló el builder de {name}: {e}"); }
        }

        public static string ScenePath(string name) => $"{SceneDir}/{name}.unity";

        /// <summary>Escena vacía con cámara ortográfica (tamaño 6, fondo negro, post-proceso), Volume global y Global Light 2D.</summary>
        public static Scene NewScene(bool withCamera = true)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (withCamera)
            {
                CreateCamera();
                CreateVolume();
                CreateGlobalLight();
            }
            return scene;
        }

        public static void Save(Scene scene, string name)
        {
            Directory.CreateDirectory(SceneDir);
            if (!EditorSceneManager.SaveScene(scene, ScenePath(name)))
                throw new IOException("No se pudo guardar " + ScenePath(name));
        }

        public static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = AsciiTheme.Bg;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            go.AddComponent<AudioListener>();
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            return cam;
        }

        public static Volume CreateVolume()
        {
            var go = new GameObject("Global Volume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = GetOrCreateVolumeProfile();
            return volume;
        }

        public static Light2D CreateGlobalLight(float intensity = 1f)
        {
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = intensity;
            light.color = Color.white;
            return light;
        }

        /// <summary>Perfil compartido: Bloom (glow neón) + Vignette.</summary>
        public static VolumeProfile GetOrCreateVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);
            if (profile != null) return profile;

            Directory.CreateDirectory(Path.GetDirectoryName(VolumePath));
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumePath);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.7f);
            bloom.intensity.Override(1.3f);
            bloom.scatter.Override(0.65f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.45f);

            foreach (var component in profile.components)
                AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        /// <summary>Carga un asset de Resources (p. ej. "Art/camarada") para asignarlo en escenas.</summary>
        public static T LoadResource<T>(string path) where T : Object
        {
            var asset = Resources.Load<T>(path);
            if (asset == null) Debug.LogWarning($"[RunDashC] Falta Resources/{path} ({typeof(T).Name})");
            return asset;
        }
    }
}
