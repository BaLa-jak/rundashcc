using UnityEngine;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>
    /// "Corre en su sitio": rebote, squash/stretch e inclinación leve de un Image (pivote abajo al centro).
    /// Opcional: un Text de polvo ASCII bajo los pies. Tiempo no escalado.
    /// </summary>
    public class UiBob : MonoBehaviour
    {
        static readonly string[] DustFrames = { ".  ,", " ,' .", "'.  ", " . ,'" };

        public float stepsPerSecond = 3.2f;
        public float height = 16f;
        public float squash = 0.08f;
        public float tilt = 4f;
        public Text dust;

        RectTransform rt;
        Vector2 basePos;

        void Awake()
        {
            rt = (RectTransform)transform;
            basePos = rt.anchoredPosition;
        }

        void Update()
        {
            float phase = Time.unscaledTime * stepsPerSecond * Mathf.PI;
            float b = Mathf.Abs(Mathf.Sin(phase));   // 0 = pisando, 1 = en el aire
            float s = Mathf.Lerp(-1f, 1f, b);        // -1 squash, +1 stretch
            rt.anchoredPosition = basePos + new Vector2(0f, b * height);
            rt.localScale = new Vector3(1f - squash * s * 0.6f, 1f + squash * s, 1f);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 0.5f) * tilt);
            if (dust != null) dust.text = DustFrames[(int)(Time.unscaledTime * stepsPerSecond) % DustFrames.Length];
        }
    }
}
