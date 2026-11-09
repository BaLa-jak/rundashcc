using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>
    /// Lluvia estilo Matrix sobre un Text a pantalla completa: columnas de código que caen,
    /// cabeza brillante y estela que se apaga. Agrupa los caracteres del mismo color en un solo
    /// tag &lt;color&gt; por tramo. Tiempo no escalado.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class AsciiRain : MonoBehaviour
    {
        // Sin '<' ni '>': juntos podrían formar un tag de rich text (<b>, <i>).
        const string Glyphs = "01{}[]();=+-*/#$%&!?:.|_abcdefijnrstuvxyzABCDEF0101";

        public Color headColor = new Color(0.85f, 1f, 0.88f, 0.55f);
        public Color trailColor = new Color(0.2f, 1f, 0.4f, 0.32f);
        public Color fadeColor = new Color(0.1f, 0.5f, 0.23f, 0.22f);
        public float step = 0.07f;
        [Range(0.05f, 1f)] public float density = 0.4f;
        public int minTrail = 5;
        public int maxTrail = 16;

        Text text;
        int cols, rows;
        char[] glyphs;
        int[] head, len;
        string[] hex;
        float timer;
        readonly StringBuilder sb = new StringBuilder(16384);

        void Start()
        {
            text = GetComponent<Text>();
            text.supportRichText = true;
            text.alignment = TextAnchor.UpperLeft; // a la izquierda: los espacios finales no mueven las columnas
            // Se dimensiona con la resolución de referencia (+30% para otras proporciones de pantalla).
            cols = Mathf.CeilToInt(AsciiUi.Reference.x * 1.3f / UiKit.Cw(text.fontSize));
            rows = Mathf.CeilToInt(AsciiUi.Reference.y * 1.3f / (text.fontSize * text.lineSpacing));

            glyphs = new char[cols * rows];
            for (int i = 0; i < glyphs.Length; i++) glyphs[i] = RandomGlyph();
            head = new int[cols];
            len = new int[cols];
            for (int c = 0; c < cols; c++) Respawn(c, Random.Range(-rows, rows));
            hex = new[] { null, ColorUtility.ToHtmlStringRGBA(headColor), ColorUtility.ToHtmlStringRGBA(trailColor), ColorUtility.ToHtmlStringRGBA(fadeColor) };
            Render();
        }

        static char RandomGlyph() => Glyphs[Random.Range(0, Glyphs.Length)];

        void Respawn(int c, int startHead)
        {
            head[c] = startHead;
            len[c] = Random.Range(minTrail, maxTrail + 1);
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < step) return;
            timer = 0f;

            // Retraso medio ≈ rows * (1/density - 1): así ~density de las columnas tienen gota.
            int maxDelay = Mathf.CeilToInt(rows * (1f / density - 1f) * 2f) + 1;
            for (int c = 0; c < cols; c++)
            {
                head[c]++;
                if (head[c] - len[c] > rows) Respawn(c, -Random.Range(0, maxDelay));
                else if (head[c] >= 0 && head[c] < rows) glyphs[head[c] * cols + c] = RandomGlyph();
            }
            for (int i = glyphs.Length / 30; i > 0; i--) glyphs[Random.Range(0, glyphs.Length)] = RandomGlyph();
            Render();
        }

        void Render()
        {
            sb.Clear();
            for (int r = 0; r < rows; r++)
            {
                int open = 0;
                for (int c = 0; c < cols; c++)
                {
                    int d = head[c] - r; // 0 = cabeza; la estela queda arriba
                    int cls = d < 0 || d >= len[c] ? 0 : d == 0 ? 1 : d * 2 < len[c] ? 2 : 3;
                    if (cls == 0) { sb.Append(' '); continue; }
                    if (cls != open)
                    {
                        if (open != 0) sb.Append("</color>");
                        sb.Append("<color=#").Append(hex[cls]).Append('>');
                        open = cls;
                    }
                    sb.Append(glyphs[r * cols + c]);
                }
                if (open != 0) sb.Append("</color>");
                if (r < rows - 1) sb.Append('\n');
            }
            text.text = sb.ToString();
        }
    }
}
