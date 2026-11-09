using UnityEngine;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>
    /// Glitch mínimo del HUD: sacude este RectTransform y separa los textos en rojo/cian
    /// (aberración) usando los <see cref="Shadow"/> de sus hijos. Tiempo no escalado.
    /// </summary>
    public class HudGlitch : MonoBehaviour
    {
        public float amplitude = 6f;

        RectTransform rect;
        Vector2 basePos;
        Shadow[] shadows;
        float until;
        bool active;

        void Awake()
        {
            rect = (RectTransform)transform;
            basePos = rect.anchoredPosition;
            shadows = GetComponentsInChildren<Shadow>(true);
        }

        public void Kick(float seconds) => until = Mathf.Max(until, Time.unscaledTime + seconds);

        void Update()
        {
            bool on = Time.unscaledTime < until;
            if (!on && !active) return; // en reposo no toca nada (evita reconstruir mallas cada frame)
            active = on;

            rect.anchoredPosition = on
                ? basePos + new Vector2(Random.Range(-amplitude, amplitude), Random.Range(-amplitude, amplitude) * 0.4f)
                : basePos;
            foreach (var s in shadows)
            {
                s.effectColor = on ? AsciiTheme.WithAlpha(Random.value < 0.5f ? AsciiTheme.Red : AsciiTheme.Cyan, 0.75f) : Color.clear;
                s.effectDistance = on ? new Vector2(Random.Range(-amplitude, amplitude), Random.Range(-2f, 2f)) : Vector2.zero;
            }
        }
    }
}
