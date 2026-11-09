using UnityEngine;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>
    /// Escribe el texto del Text carácter por carácter con Sfx.Type y un cursor "_" al final.
    /// Usar alineación a la izquierda (si no, el texto "baila" mientras crece) y sin rich text.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class TypewriterText : MonoBehaviour
    {
        public float delay = 0.4f;
        public float charsPerSecond = 32f;
        public int soundEvery = 3;
        public bool cursor = true;

        Text text;
        string full;
        float t;
        int shown;

        void Awake()
        {
            text = GetComponent<Text>();
            full = text.text;
            text.text = "";
        }

        void OnEnable() => Play(full);

        /// <summary>Vuelve a escribir desde cero (con el retraso inicial).</summary>
        public void Play(string value)
        {
            full = value ?? "";
            t = -delay;
            shown = 0;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            int n = Mathf.Clamp(Mathf.FloorToInt(t * charsPerSecond), 0, full.Length);
            for (int i = shown; i < n; i++)
                if (i % soundEvery == 0 && !char.IsWhiteSpace(full[i])) AudioManager.Play(Sfx.Type, 0.15f, 0.3f);
            shown = n;

            bool cursorOn = cursor && (n < full.Length || Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f);
            text.text = cursorOn ? full.Substring(0, n) + "_" : full.Substring(0, n);
        }
    }
}
