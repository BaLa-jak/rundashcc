using UnityEngine;
using UnityEngine.UI;

namespace RunDashC.EditorTools
{
    /// <summary>
    /// Escena Help (pausa). Se carga en aditivo encima de Game: SIN cámara, luz, Volume,
    /// EventSystem ni AudioListener. El canvas toma Camera.main en runtime (CanvasCameraBinder).
    /// </summary>
    public static class HelpSceneBuilder
    {
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        static readonly string[,] Controls =
        {
            { "SALTAR ........", "[ESPACIO] [W] [ARRIBA] [CLIC] (A)" },
            { "DOBLE SALTO ...", "salta otra vez en el aire" },
            { "SALTO LARGO ...", "mantén presionado" },
            { "PISAR BUG .....", "cae encima de un bug (+5 bits)" },
            { "PAUSA .........", "[ESC] [P] (START)" },
        };

        public static void Build()
        {
            var scene = SceneBuilder.NewScene(withCamera: false);
            var canvas = AsciiUi.CreateCanvas("HelpCanvas", 200);
            var root = canvas.transform;

            // Oscurece el juego y bloquea los clics hacia el HUD.
            var dim = AsciiUi.CreatePanel(root, "Dim", new Color(0f, 0f, 0f, 0.85f));
            AsciiUi.Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var window = AsciiUi.CreatePanel(root, "Window", AsciiTheme.WithAlpha(AsciiTheme.Bg, 0.92f));
            AsciiUi.Place(window.rectTransform, Center, Vector2.zero, new Vector2(1720, 980));
            UiKit.Frame(root, "Frame", Vector2.zero, new Vector2(1720, 980), 28, AsciiTheme.Green);

            UiKit.Label(root, "Logo", AsciiTheme.Logo, 36, AsciiTheme.Green, TextAnchor.MiddleCenter, Center, new Vector2(0, 345), new Vector2(58 * UiKit.Cw(36) + 20, 36 * 5 + 10));
            UiKit.Label(root, "Pausa", "// PAUSA", 60, AsciiTheme.Green, TextAnchor.MiddleCenter, Center, new Vector2(0, 205), new Vector2(600, 70))
                .gameObject.AddComponent<Blink>();
            const string sigstop = "> proceso suspendido (SIGSTOP)";
            var sig = UiKit.LeftLabel(root, "Sigstop", sigstop, 32, AsciiTheme.White, -(sigstop.Length + 1) * UiKit.Cw(32) * 0.5f, 150)
                .gameObject.AddComponent<TypewriterText>();
            sig.delay = 0.15f;

            // Columna izquierda: controles con puntos guía.
            const float colTop = 80f;
            const float leftX = -800f, rightX = 20f;
            UiKit.LeftLabel(root, "ControlsTitle", "// CONTROLES", 34, AsciiTheme.Green, leftX, colTop);
            string white = AsciiTheme.ToHex(AsciiTheme.White), cyan = AsciiTheme.ToHex(AsciiTheme.Cyan), dots = AsciiTheme.ToHex(AsciiTheme.GreenDim);
            var lines = new string[Controls.GetLength(0)];
            for (int i = 0; i < lines.Length; i++)
            {
                var name = Controls[i, 0];
                int cut = name.IndexOf(" .");
                lines[i] = $"<color=#{white}>{name.Substring(0, cut)}</color><color=#{dots}>{name.Substring(cut)}</color> <color=#{cyan}>{Controls[i, 1]}</color>";
            }
            var table = UiKit.Label(root, "Controls", string.Join("\n", lines), 30, AsciiTheme.White, TextAnchor.UpperLeft, Center, new Vector2(leftX + 380, colTop - 40 - 140), new Vector2(760, 280));
            table.lineSpacing = 1.3f;

            // Columna derecha: leyenda de peligros y objetos con sus sprites.
            UiKit.LeftLabel(root, "LegendTitle", "// PELIGROS Y OBJETOS", 34, AsciiTheme.Green, rightX, colTop);
            LegendRow(root, 0, rightX, "Art/bug", "bug", AsciiTheme.Red, "te quita 1 vida, pero puedes pisarlo");
            LegendRow(root, 1, rightX, "Art/obstaculo", "/0", AsciiTheme.Red, "división entre cero: evítala");
            LegendRow(root, 2, rightX, "Art/sierra", "while(true)", AsciiTheme.Red, "sierra: no la toques");
            LegendRow(root, 3, rightX, null, "^^^", AsciiTheme.Red, "púas");
            LegendRow(root, 4, rightX, null, "{1}", AsciiTheme.Cyan, "bit: recógelos");

            var resume = AsciiButton.Create(root, "ResumeButton", "REANUDAR", 48, Center, new Vector2(0, -345)).GetComponent<Button>();
            resume.navigation = new Navigation { mode = Navigation.Mode.None };
            UiKit.Label(root, "Hint", "// [ESC] o [P] también reanudan", 28, AsciiTheme.GreenDim, TextAnchor.MiddleCenter, Center, new Vector2(0, -420), new Vector2(900, 40));

            canvas.gameObject.AddComponent<HelpController>().resumeButton = resume;

            SceneBuilder.Save(scene, SceneNames.Help);
        }

        /// <summary>Fila: [sprite] clave ...... descripción (^^^ y {1} no tienen sprite: la clave ya es el dibujo).</summary>
        static void LegendRow(Transform root, int i, float left, string spritePath, string key, Color keyColor, string desc)
        {
            float y = 25f - i * 64f;
            var iconPos = new Vector2(left + 30, y);
            if (spritePath != null)
                UiKit.SpriteImage(root, "Icon_" + key, SceneBuilder.LoadResource<Sprite>(spritePath), Center, iconPos, new Vector2(56, 56));
            string text = $"<color=#{AsciiTheme.ToHex(keyColor)}>{key}</color> <color=#{AsciiTheme.ToHex(AsciiTheme.GreenDim)}>{new string('.', Mathf.Max(2, 12 - key.Length))}</color> {desc}";
            UiKit.LeftLabel(root, "Legend_" + key, text, 30, AsciiTheme.White, left + 76, y);
        }
    }
}
