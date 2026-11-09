using UnityEngine;
using UnityEngine.UI;

namespace RunDashC.EditorTools
{
    /// <summary>
    /// Escena Menu: ventana de terminal (como Assets/ejemplo.jpeg) con logo, Camarada corriendo,
    /// prompt de nombre, [ INICIAR ] / [ SALIR ], integrantes y récord.
    /// </summary>
    public static class MenuSceneBuilder
    {
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        public static void Build()
        {
            var scene = SceneBuilder.NewScene(withCamera: true);
            var canvas = AsciiUi.CreateCanvas("MenuCanvas", 0);
            var root = canvas.transform;

            // Fondo atenuado + lluvia sutil.
            var fondo = SceneBuilder.LoadResource<Sprite>("Art/fondo");
            var bg = UiKit.SpriteImage(root, "Fondo", fondo, Center, Vector2.zero, AsciiUi.Reference);
            if (bg != null)
            {
                bg.color = new Color(0.32f, 0.32f, 0.32f, 1f);
                var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = fondo.rect.width / fondo.rect.height;
            }
            var rain = AsciiUi.CreateText(root, "Rain", "", 26, AsciiTheme.Green, TextAnchor.UpperLeft);
            AsciiUi.Stretch(rain.rectTransform);
            rain.gameObject.AddComponent<AsciiRain>();

            // Marco de ventana con barra de título.
            const int frameFs = 30;
            var frame = UiKit.Frame(root, "Frame", Vector2.zero, new Vector2(1888, 1048), frameFs, AsciiTheme.Green, ruleRow: 2);
            float barY = frame.rectTransform.sizeDelta.y * 0.5f - 1.5f * frameFs; // fila 1 del marco
            UiKit.Label(root, "BarTitle", $"{AsciiTheme.Practica} - {AsciiTheme.GameTitle}", 36, AsciiTheme.White, TextAnchor.MiddleLeft, Center, new Vector2(0, barY), new Vector2(1800, 50));
            var player = UiKit.Label(root, "BarPlayer", "JUGADOR: " + GameSession.DefaultName, 36, AsciiTheme.White, TextAnchor.MiddleCenter, Center, new Vector2(60, barY), new Vector2(700, 50));
            var close = AsciiButton.Create(root, "CloseButton", "X", 36, Center, new Vector2(820, barY));
            close.GetComponent<Button>().navigation = new Navigation { mode = Navigation.Mode.None };

            // Logo con glitch + subtítulo con typewriter, alineado a la izquierda del logo.
            const int logoFs = 48;
            const float logoX = -170f, logoY = 300f;
            float logoW = 58 * UiKit.Cw(logoFs);
            float left = logoX - logoW * 0.5f;
            var logo = UiKit.Label(root, "Logo", AsciiTheme.Logo, logoFs, AsciiTheme.Green, TextAnchor.MiddleCenter, Center, new Vector2(logoX, logoY), new Vector2(logoW + 20, logoFs * 5 + 20));
            logo.gameObject.AddComponent<GlitchText>();
            UiKit.LeftLabel(root, "Subtitle", "> while(camarada.viva) { correr(); }", 36, AsciiTheme.Green, left, 145)
                .gameObject.AddComponent<TypewriterText>();

            // Camarada corriendo en su sitio (pivote en los pies) + polvo.
            var rat = UiKit.SpriteImage(root, "Camarada", SceneBuilder.LoadResource<Sprite>("Art/camarada"), Center, Vector2.zero, new Vector2(300, 300));
            if (rat != null)
            {
                rat.rectTransform.pivot = new Vector2(0.5f, 0f);
                rat.rectTransform.anchoredPosition = new Vector2(560, 110);
                var dust = UiKit.Label(root, "Dust", "", 34, AsciiTheme.GreenDim, TextAnchor.MiddleRight, Center, new Vector2(420, 110), new Vector2(120, 40));
                dust.transform.SetSiblingIndex(rat.transform.GetSiblingIndex());
                rat.gameObject.AddComponent<UiBob>().dust = dust;
            }
            UiKit.Label(root, "CamaradaTag", "// " + AsciiTheme.CharacterName.ToLowerInvariant() + ".exe", 28, AsciiTheme.GreenDim, TextAnchor.MiddleCenter, Center, new Vector2(560, 70), new Vector2(400, 40));

            // Prompt de nombre: > nombre_jugador: [____________]_
            const int fs = 40;
            float cw = UiKit.Cw(fs);
            const string prefix = "> nombre_jugador: [";
            const float promptY = 50f;
            UiKit.LeftLabel(root, "NamePrompt", prefix, fs, AsciiTheme.Green, left, promptY);
            float inputLeft = left + prefix.Length * cw;
            float inputW = GameSession.MaxNameLength * cw + 8;
            var input = CreateNameInput(root, new Vector2(inputLeft + inputW * 0.5f, promptY), new Vector2(inputW, fs * 1.3f), fs);
            UiKit.LeftLabel(root, "NameClose", "]", fs, AsciiTheme.Green, inputLeft + inputW, promptY);
            UiKit.LeftLabel(root, "NameCursor", "_", fs, AsciiTheme.Green, inputLeft + inputW + cw, promptY)
                .gameObject.AddComponent<Blink>();

            // Botones.
            var start = AsciiButton.Create(root, "StartButton", "INICIAR", 48, Center, new Vector2(logoX, -70)).GetComponent<Button>();
            var quit = AsciiButton.Create(root, "QuitButton", "SALIR", 48, Center, new Vector2(logoX, -160)).GetComponent<Button>();
            input.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = start };
            start.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = input, selectOnDown = quit };
            quit.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = start };

            // Integrantes (abajo a la izquierda) y récord + ayuda rápida (abajo a la derecha).
            UiKit.Label(root, "Integrantes", UiKit.IntegrantesComment(), 30, AsciiTheme.Green, TextAnchor.LowerLeft, Center, new Vector2(-440, -340), new Vector2(880, 260));
            UiKit.Label(root, "Hint", "// [ESPACIO] saltar   [ESC] pausa", 28, AsciiTheme.GreenDim, TextAnchor.LowerRight, Center, new Vector2(440, -394), new Vector2(880, 40));
            var record = UiKit.Label(root, "Record", "// record: 0 bits", 36, AsciiTheme.Yellow, TextAnchor.LowerRight, Center, new Vector2(440, -440), new Vector2(880, 50));

            AsciiUi.AddCrtOverlay(canvas);

            var ctrl = canvas.gameObject.AddComponent<MenuController>();
            ctrl.nameInput = input;
            ctrl.playerLabel = player;
            ctrl.recordText = record;
            ctrl.startButton = start;
            ctrl.quitButton = quit;
            ctrl.closeButton = close.GetComponent<Button>();

            SceneBuilder.Save(scene, SceneNames.Menu);
        }

        /// <summary>InputField clásico de terminal: "____________" tenue detrás y el nombre en cian encima.</summary>
        static InputField CreateNameInput(Transform parent, Vector2 pos, Vector2 size, int fs)
        {
            var go = new GameObject("NameInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            AsciiUi.Place((RectTransform)go.transform, Center, pos, size);
            var img = go.GetComponent<Image>();
            img.color = AsciiTheme.WithAlpha(AsciiTheme.Green, 0.07f);

            var underline = AsciiUi.CreateText(go.transform, "Underline", new string('_', GameSession.MaxNameLength), fs, AsciiTheme.GreenDim, TextAnchor.MiddleLeft);
            AsciiUi.Stretch(underline.rectTransform);
            var text = AsciiUi.CreateText(go.transform, "Text", "", fs, AsciiTheme.Cyan, TextAnchor.MiddleLeft);
            AsciiUi.Stretch(text.rectTransform);
            text.supportRichText = false;

            var input = go.GetComponent<InputField>();
            input.textComponent = text;
            input.targetGraphic = img;
            input.transition = Selectable.Transition.None;
            input.characterLimit = GameSession.MaxNameLength;
            input.lineType = InputField.LineType.SingleLine;
            input.customCaretColor = true;
            input.caretColor = AsciiTheme.Green;
            input.caretWidth = 3;
            input.selectionColor = AsciiTheme.WithAlpha(AsciiTheme.Cyan, 0.35f);
            return input;
        }
    }
}
