using UnityEngine;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>
    /// Fábrica de UI con el estilo del juego. Sirve en runtime y en los builders del Editor.
    /// Referencia de diseño: 1920x1080.
    /// </summary>
    public static class AsciiUi
    {
        public static readonly Vector2 Reference = new Vector2(1920, 1080);

        /// <summary>
        /// Canvas Screen Space - Camera (así le afecta el Bloom). La cámara se asigna en runtime
        /// con CanvasCameraBinder (Help no tiene cámara propia: usa la de Game).
        /// </summary>
        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<CanvasCameraBinder>();
            return canvas;
        }

        public static Text CreateText(Transform parent, string name, string text, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = AsciiTheme.Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = alignment;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Panel de color sólido (sin sprite).</summary>
        public static Image CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Posiciona con anclas; el pivote queda en el centro de las anclas.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = (anchorMin + anchorMax) * 0.5f;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Atajo: ancla puntual (anchor) + posición + tamaño.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
            => Place(rt, anchor, anchor, position, size);

        /// <summary>Ocupa todo el padre con un margen opcional.</summary>
        public static RectTransform Stretch(RectTransform rt, float margin = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
            return rt;
        }

        /// <summary>Scanlines CRT + parpadeo sutil por encima de todo el canvas.</summary>
        public static CrtOverlay AddCrtOverlay(Canvas canvas)
        {
            var go = new GameObject("CRT", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsLastSibling();
            Stretch((RectTransform)go.transform);
            var raw = go.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.color = Color.clear; // invisible en el Editor; CrtOverlay le pone textura y alfa en runtime
            return go.AddComponent<CrtOverlay>();
        }
    }
}
