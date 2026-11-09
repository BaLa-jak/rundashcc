using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RunDashC
{
    /// <summary>Helpers del mundo: texto TextMesh con VT323, sprites de Resources/Art, rectángulos y luces 2D.</summary>
    public static class WorldKit
    {
        public const int FontSize = 64;
        /// <summary>Avance horizontal de VT323 por em (26 px a 64 px; medido en el Editor).</summary>
        public const float Advance = 26f / 64f;
        /// <summary>Em con el que caben exactamente 3 caracteres por unidad (texto de plataformas).</summary>
        public const float CodeEm = 1f / (3f * Advance);
        public const int CodeCharsPerUnit = 3;

        static readonly HashSet<string> warned = new HashSet<string>();
        static Sprite pixel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            warned.Clear();
            pixel = null;
        }

        /// <summary>
        /// TextMesh hijo de <paramref name="parent"/>. <paramref name="em"/> = alto de línea en unidades
        /// (TextMesh mide fontSize * characterSize * 0.1); cada carácter avanza em * <see cref="Advance"/>.
        /// </summary>
        public static TextMesh AddText(Transform parent, string text, Vector2 localPos, Color color, float em,
            TextAnchor anchor, int sortingOrder)
        {
            var go = new GameObject("txt");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = AsciiTheme.Font;
            tm.fontSize = FontSize;
            tm.characterSize = em / (FontSize * 0.1f);
            tm.anchor = anchor;
            tm.alignment = (TextAlignment)((int)anchor % 3); // UpperLeft/Center/Right -> Left/Center/Right
            tm.color = color;
            tm.text = text;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = AsciiTheme.Font.material;
            mr.sortingOrder = sortingOrder;
            return tm;
        }

        /// <summary>Sprite Resources/Art/{art} centrado, escalado a <paramref name="width"/> unidades. Si falta, avisa una vez.</summary>
        public static SpriteRenderer AddSprite(Transform parent, string art, float width, int sortingOrder)
        {
            var go = new GameObject(art);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            sr.sprite = LoadSprite(art);
            if (sr.sprite != null) go.transform.localScale = Vector3.one * (width / sr.sprite.bounds.size.x);
            return sr;
        }

        public static Sprite LoadSprite(string art)
        {
            var sprite = Resources.Load<Sprite>("Art/" + art);
            if (sprite == null && warned.Add(art)) Debug.LogWarning($"[RunDashC] Falta el sprite Resources/Art/{art}");
            return sprite;
        }

        /// <summary>Rectángulo sólido (sprite blanco 1x1 u teñido).</summary>
        public static SpriteRenderer AddRect(Transform parent, Vector2 center, Vector2 size, Color color, int sortingOrder)
        {
            if (pixel == null) pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            var go = new GameObject("rect");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = pixel;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        public static Light2D AddLight(Transform parent, Color color, float intensity, float radius)
        {
            var go = new GameObject("light");
            go.transform.SetParent(parent, false);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = 0f;
            light.pointLightOuterRadius = radius;
            return light;
        }

        /// <summary>¿El collider es de Camarada?</summary>
        public static CamaradaController Player(Collider2D other) => other.GetComponent<CamaradaController>();
    }
}
