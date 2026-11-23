using System.Collections.Generic;
using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Glitch barato: cada instante un carácter de un texto visible cambia a blanco por ~0.1 s.
    /// Con muchos textos en pantalla, cada uno parpadea cada pocos segundos.
    /// </summary>
    public class CodeGlitch : MonoBehaviour
    {
        const string Glyphs = "01#%&$@!?/\\*";

        readonly List<TextMesh> texts = new List<TextMesh>();
        TextMesh current;
        string original;
        float timer;

        public void Add(TextMesh tm) => texts.Add(tm);

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            if (current != null)
            {
                current.text = original;
                current = null;
                timer = Random.Range(0.05f, 0.3f);
                return;
            }

            timer = 0.1f;
            for (int tries = 0; tries < 6 && texts.Count > 0; tries++)
            {
                var tm = texts[Random.Range(0, texts.Count)];
                if (tm == null) { texts.RemoveAll(t => t == null); continue; } // tramos del infinito ya borrados
                if (tm.text.Length == 0 || !tm.GetComponent<MeshRenderer>().isVisible) continue;
                original = tm.text;
                int i = Random.Range(0, original.Length);
                tm.text = original.Substring(0, i) + "<color=#FFFFFF>" + Glyphs[Random.Range(0, Glyphs.Length)] + "</color>" + original.Substring(i + 1);
                current = tm;
                return;
            }
        }
    }
}
