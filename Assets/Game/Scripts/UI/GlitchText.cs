using UnityEngine;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>
    /// Cada cierto tiempo cambia caracteres al azar por símbolos durante unos frames y desplaza el texto 1-3 px.
    /// Nunca toca saltos de línea ni espacios, así el arte ASCII multilínea no se desalinea. Sin rich text.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class GlitchText : MonoBehaviour
    {
        const string Symbols = "#$%&@!?/\\|*";

        public float minInterval = 1.5f;
        public float maxInterval = 4.5f;
        public float duration = 0.12f;
        [Range(0f, 1f)] public float amount = 0.06f;
        public float maxOffset = 3f;

        Text text;
        RectTransform rt;
        string original;
        Vector2 basePos;
        float next, until, burstOffset, burstStart, burstEnd;
        bool glitching;

        void Awake()
        {
            text = GetComponent<Text>();
            rt = text.rectTransform;
            original = text.text;
            basePos = rt.anchoredPosition;
            next = Time.unscaledTime + Random.Range(minInterval, maxInterval);
        }

        /// <summary>Glitch fuerte con sacudida de <paramref name="offset"/> px que se apaga en <paramref name="seconds"/>.</summary>
        public void Burst(float seconds, float offset)
        {
            burstStart = Time.unscaledTime;
            burstEnd = until = burstStart + seconds;
            burstOffset = offset;
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (now >= next)
            {
                until = Mathf.Max(until, now + duration);
                next = now + Random.Range(minInterval, maxInterval);
            }

            if (now < until)
            {
                glitching = true;
                float k = now < burstEnd ? 1f - (now - burstStart) / (burstEnd - burstStart) : 0f;
                float off = Mathf.Max(Random.Range(1f, maxOffset), burstOffset * k);
                text.text = Scramble(k > 0f ? Mathf.Max(amount, 0.25f * k) : amount);
                rt.anchoredPosition = basePos + new Vector2(Random.value < 0.5f ? -off : off, Random.Range(-1f, 1f) * off * 0.5f);
            }
            else if (glitching)
            {
                glitching = false;
                text.text = original;
                rt.anchoredPosition = basePos;
            }
        }

        string Scramble(float p)
        {
            var c = original.ToCharArray();
            for (int i = 0; i < c.Length; i++)
                if (c[i] != '\n' && c[i] != ' ' && Random.value < p) c[i] = Symbols[Random.Range(0, Symbols.Length)];
            return new string(c);
        }
    }
}
