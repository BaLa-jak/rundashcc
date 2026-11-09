using UnityEngine;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>Hace parpadear un Graphic (cursor "_", "// PAUSA", "NUEVO RECORD!"). Tiempo no escalado.</summary>
    [RequireComponent(typeof(Graphic))]
    public class Blink : MonoBehaviour
    {
        public float period = 1f;
        [Range(0f, 1f)] public float onFraction = 0.55f;
        [Range(0f, 1f)] public float offAlpha = 0f;

        Graphic graphic;

        void Awake() => graphic = GetComponent<Graphic>();

        void Update()
        {
            bool on = Mathf.Repeat(Time.unscaledTime, period) < period * onFraction;
            graphic.canvasRenderer.SetAlpha(on ? 1f : offAlpha);
        }
    }
}
