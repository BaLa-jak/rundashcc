using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>Fundido a negro entre escenas (persistente, en un canvas overlay por encima de todo).</summary>
    public class ScreenFader : MonoBehaviour
    {
        const float OutSeconds = 0.2f;
        const float InSeconds = 0.25f;

        public static ScreenFader Instance { get; private set; }

        Image image;

        internal static ScreenFader Create(Transform parent)
        {
            var go = new GameObject("ScreenFader", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var img = new GameObject("Black", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            img.transform.SetParent(go.transform, false);
            AsciiUi.Stretch((RectTransform)img.transform);

            var fader = go.AddComponent<ScreenFader>();
            fader.image = img.GetComponent<Image>();
            fader.image.color = new Color(0, 0, 0, 0);
            fader.image.raycastTarget = false;
            return fader;
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Oscurece, ejecuta <paramref name="atBlack"/> (p. ej. cargar escena) y vuelve a aclarar.</summary>
        public void FadeOutIn(Action atBlack) => StartCoroutine(Run(atBlack));

        IEnumerator Run(Action atBlack)
        {
            image.raycastTarget = true; // bloquea clics durante la transición
            yield return Fade(0f, 1f, OutSeconds);
            atBlack?.Invoke();
            yield return null; // deja que la escena nueva cargue
            yield return Fade(1f, 0f, InSeconds);
            image.raycastTarget = false;
        }

        IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            SetAlpha(to);
        }

        void SetAlpha(float a) => image.color = new Color(0f, 0f, 0f, a);
    }
}
