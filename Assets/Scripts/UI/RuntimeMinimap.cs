using OriginalKartRacer.Race;
using UnityEngine;

namespace OriginalKartRacer.UI
{
    /// <summary>运行时俯视相机。其 RenderTexture 可直接接到 UI RawImage 或 DemoRuntimeHUD。</summary>
    public sealed class RuntimeMinimap : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private RacingPath path;
        [SerializeField, Min(20)] private int textureSize = 512;
        [SerializeField, Min(10f)] private float orthographicSize = 63f;
        [SerializeField, Min(1f)] private float height = 90f;
        [SerializeField] private Color clearColor = new Color(0.05f, 0.11f, 0.16f, 1f);

        private Camera minimapCamera;
        public RenderTexture Texture { get; private set; }

        private void Awake()
        {
            minimapCamera = GetComponent<Camera>();
            if (minimapCamera == null)
                minimapCamera = gameObject.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = orthographicSize;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = clearColor;
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            minimapCamera.nearClipPlane = 0.1f;
            minimapCamera.farClipPlane = 180f;
            Texture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = "RuntimeMinimap"
            };
            minimapCamera.targetTexture = Texture;
            if (path != null)
                minimapCamera.transform.position = path.transform.position + Vector3.up * height;
        }

        private void LateUpdate()
        {
            if (path == null && target != null)
                minimapCamera.transform.position = new Vector3(target.position.x, height, target.position.z);
        }

        private void OnDestroy()
        {
            if (Texture != null)
                Texture.Release();
        }
    }
}
