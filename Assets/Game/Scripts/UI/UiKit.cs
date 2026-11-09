using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>Piezas de UI de terminal que comparten los builders de Menu, GameOver y Help.</summary>
    public static class UiKit
    {
        /// <summary>VT323 es monoespaciada: cada carácter avanza 0.4 em.</summary>
        public const float CharWidth = 0.4f;

        /// <summary>Ancho de un carácter en px de referencia (Unity redondea el avance a px enteros).</summary>
        public static float Cw(int fontSize) => Mathf.Round(fontSize * CharWidth);

        /// <summary>Línea horizontal ASCII "+-----+" de <paramref name="cols"/> caracteres.</summary>
        public static string Rule(int cols) => "+" + new string('-', Mathf.Max(0, cols - 2)) + "+";

        public static string IntegrantesComment() =>
            "/* EQUIPO:\n" + string.Join("\n", AsciiTheme.Integrantes.Select(n => " *  - " + n)) + "\n */";

        /// <summary>Texto posicionado con un ancla puntual.</summary>
        public static Text Label(Transform parent, string name, string text, int size, Color color,
            TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 rectSize)
        {
            var t = AsciiUi.CreateText(parent, name, text, size, color, align);
            AsciiUi.Place(t.rectTransform, anchor, pos, rectSize);
            return t;
        }

        /// <summary>Texto alineado a la izquierda con su borde izquierdo en x = <paramref name="left"/> (para typewriter y prompts).</summary>
        public static Text LeftLabel(Transform parent, string name, string text, int size, Color color, float left, float y)
        {
            float w = (text.Length + 1) * Cw(size);
            return Label(parent, name, text, size, color, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(left + w * 0.5f, y), new Vector2(w, size * 1.3f));
        }

        /// <summary>
        /// Marco ASCII en un solo Text (+---+ arriba/abajo, | a los lados), así esquinas y lados
        /// siempre coinciden a cualquier resolución. Interlineado de VT323 = 1 em, así que la fila r
        /// queda centrada en y = pos.y + alto/2 - (r + 0.5) * fontSize. <paramref name="ruleRow"/>: fila
        /// extra con "+---+" (separador de barra de título).
        /// </summary>
        public static Text Frame(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize, Color color, int ruleRow = -1)
        {
            int cols = Mathf.FloorToInt(size.x / Cw(fontSize));
            int rows = Mathf.FloorToInt(size.y / fontSize);
            string rule = Rule(cols), middle = "|" + new string(' ', cols - 2) + "|";
            var lines = Enumerable.Range(0, rows).Select(r => r == 0 || r == rows - 1 || r == ruleRow ? rule : middle);
            return Label(parent, name, string.Join("\n", lines), fontSize, color, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), pos, new Vector2(cols * Cw(fontSize), rows * fontSize));
        }

        /// <summary>Image con sprite (respeta proporción). Si falta el sprite no crea nada (evita el cuadro blanco).</summary>
        public static Image SpriteImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            if (sprite == null) return null;
            var img = AsciiUi.CreatePanel(parent, name, Color.white);
            img.sprite = sprite;
            img.preserveAspect = true;
            AsciiUi.Place(img.rectTransform, anchor, pos, size);
            return img;
        }

        /// <summary>Selecciona <paramref name="target"/> (para teclado y gamepad).</summary>
        public static void Select(GameObject target)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target);
        }

        /// <summary>Si un clic en el fondo dejó todo sin seleccionar, vuelve a <paramref name="fallback"/> para que el gamepad siga funcionando.</summary>
        public static void KeepSelection(GameObject fallback)
        {
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject == null) es.SetSelectedGameObject(fallback);
        }
    }
}
