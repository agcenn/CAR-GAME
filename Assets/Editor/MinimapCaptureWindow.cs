using System.IO;
using UnityEditor;
using UnityEngine;

namespace OriginalKartRacer.EditorTools
{
    /// <summary>从 Editor 菜单生成赛道航拍图，适合作为手工描边小地图的底稿。</summary>
    public sealed class MinimapCaptureWindow : EditorWindow
    {
        private Vector3 center = Vector3.zero;
        private float height = 120f;
        private float size = 75f;
        private int resolution = 1024;
        private string outputPath = "Assets/Art/UI/Minimap_Aerial.png";

        [MenuItem("Neon Harbor/Minimap Capture")]
        private static void Open() => GetWindow<MinimapCaptureWindow>("Minimap Capture");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("生成赛道顶部航拍图。输出后可在 Photoshop、Figma 或 Unity 中描边成正式小地图。", MessageType.Info);
            center = EditorGUILayout.Vector3Field("Center", center);
            height = EditorGUILayout.FloatField("Camera Height", height);
            size = EditorGUILayout.FloatField("Orthographic Size", size);
            resolution = EditorGUILayout.IntPopup("Resolution", resolution,
                new[] { "512", "1024", "2048" }, new[] { 512, 1024, 2048 });
            outputPath = EditorGUILayout.TextField("Asset Path", outputPath);
            if (GUILayout.Button("Capture Aerial Image"))
                Capture();
        }

        private void Capture()
        {
            if (!outputPath.StartsWith("Assets/"))
            {
                EditorUtility.DisplayDialog("Invalid output path", "输出路径必须位于 Assets/ 内。", "OK");
                return;
            }

            GameObject cameraObject = new GameObject("__MinimapCaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            RenderTexture renderTexture = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.transform.SetPositionAndRotation(center + Vector3.up * height, Quaternion.Euler(90f, 0, 0));
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            image.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            image.Apply();

            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllBytes(absolutePath, image.EncodeToPNG());
            RenderTexture.active = null;
            DestroyImmediate(cameraObject);
            DestroyImmediate(renderTexture);
            DestroyImmediate(image);
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
        }
    }
}
