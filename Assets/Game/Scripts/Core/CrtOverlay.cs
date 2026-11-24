using UnityEngine;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>Scanlines de monitor CRT (textura generada en runtime) con un parpadeo leve.</summary>
    [RequireComponent(typeof(RawImage))]
    public class CrtOverlay : MonoBehaviour
    {
        [Range(0f, 1f)] public float lineAlpha = 0.22f;
        [Range(0f, 0.2f)] public float flicker = 0.04f;

        RawImage image;
        Texture2D texture;

        void Awake()
        {
            image = GetComponent<RawImage>();
            image.raycastTarget = false;
            texture = new Texture2D(1, 4, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.DontSave,
            };
            var line = new Color(0f, 0f, 0f, 1f);
            var clear = new Color(0f, 0f, 0f, 0f);
            texture.SetPixels(new[] { clear, clear, line, line });
            texture.Apply();
            image.texture = texture;
        }

        void Update()
        {
            // Una scanline cada 4 px de pantalla.
            image.uvRect = new Rect(0f, 0f, 1f, Screen.height / 4f);
            float a = WorldMode.Production ? 0f : lineAlpha + (Random.value - 0.5f) * flicker; // producción: sin monitor CRT
            image.color = new Color(1f, 1f, 1f, a);
        }

        void OnDestroy()
        {
            if (texture != null) Destroy(texture);
        }
    }
}
