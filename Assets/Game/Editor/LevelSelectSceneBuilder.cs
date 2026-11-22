using UnityEngine;
using UnityEngine.UI;

namespace RunDashC.EditorTools
{
    /// <summary>
    /// Escena Niveles: misma ventana de terminal que el menú, "ls -l" de ./niveles con una fila por nivel
    /// ([ NIVEL 0N ] + archivo + barra de progreso; el texto lo pone LevelSelectController) y [ VOLVER ].
    /// </summary>
    public static class LevelSelectSceneBuilder
    {
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        public static void Build()
        {
            var scene = SceneBuilder.NewScene(withCamera: true);
            var canvas = AsciiUi.CreateCanvas("LevelSelectCanvas", 0);
            var root = canvas.transform;

            // Fondo atenuado + lluvia (igual que el menú).
            var fondo = SceneBuilder.LoadResource<Sprite>("Art/fondo");
            var bg = UiKit.SpriteImage(root, "Fondo", fondo, Center, Vector2.zero, AsciiUi.Reference);
            if (bg != null)
            {
                bg.color = new Color(0.25f, 0.25f, 0.25f, 1f);
                var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = fondo.rect.width / fondo.rect.height;
            }
            var rain = AsciiUi.CreateText(root, "Rain", "", 26, AsciiTheme.Green, TextAnchor.UpperLeft);
            AsciiUi.Stretch(rain.rectTransform);
            rain.gameObject.AddComponent<AsciiRain>().density = 0.4f;

            // Ventana con barra de título.
            const int frameFs = 30;
            var frame = UiKit.Frame(root, "Frame", Vector2.zero, new Vector2(1888, 1048), frameFs, AsciiTheme.Green, ruleRow: 2);
            float barY = frame.rectTransform.sizeDelta.y * 0.5f - 1.5f * frameFs;
            UiKit.Label(root, "BarTitle", $"{AsciiTheme.Practica} - {AsciiTheme.GameTitle}", 36, AsciiTheme.White, TextAnchor.MiddleLeft, Center, new Vector2(0, barY), new Vector2(1800, 50));
            var player = UiKit.Label(root, "BarPlayer", "JUGADOR: " + GameSession.DefaultName, 36, AsciiTheme.White, TextAnchor.MiddleRight, Center, new Vector2(0, barY), new Vector2(1800, 50));

            UiKit.Label(root, "Logo", AsciiTheme.Logo, 32, AsciiTheme.Green, TextAnchor.MiddleCenter, Center, new Vector2(0, 320), new Vector2(58 * UiKit.Cw(32) + 20, 32 * 5 + 10))
                .gameObject.AddComponent<GlitchText>();

            const float left = -780f;
            UiKit.LeftLabel(root, "Prompt", "> cd ~/camarada/niveles && ls -l", 36, AsciiTheme.Green, left, 185)
                .gameObject.AddComponent<TypewriterText>();
            UiKit.LeftLabel(root, "Header", "// SELECCIONA UN NIVEL", 32, AsciiTheme.GreenDim, left, 135);

            // Filas: botón a la izquierda, texto (archivo + barra) a la derecha.
            const int btnFs = 40, rowFs = 36;
            float btnW = ("NIVEL 00".Length + 10) * UiKit.Cw(btnFs);
            var buttons = new Button[GameSession.LevelCount];
            var rows = new Text[GameSession.LevelCount];
            for (int i = 0; i < GameSession.LevelCount; i++)
            {
                float y = 65f - i * 78f;
                buttons[i] = AsciiButton.Create(root, $"Level{i + 1:00}", $"NIVEL {i + 1:00}", btnFs, Center, new Vector2(left + btnW * 0.5f, y)).GetComponent<Button>();
                buttons[i].navigation = new Navigation { mode = Navigation.Mode.Vertical };
                float textLeft = left + btnW + 20f;
                rows[i] = UiKit.Label(root, $"Row{i + 1:00}", LevelSelectController.Files[i], rowFs, AsciiTheme.White, TextAnchor.MiddleLeft, Center,
                    new Vector2(textLeft + 560f, y), new Vector2(1120f, rowFs * 1.3f));
            }

            // Tienda (con el icono SHOP pegado al botón para que haga el punch con él) y Volver.
            var shop = AsciiButton.Create(root, "ShopButton", "TIENDA", 44, Center, new Vector2(-220, -350)).GetComponent<Button>();
            var shopIcon = UiKit.SpriteImage(shop.transform, "Icono", SceneBuilder.LoadResource<Sprite>("Art/tienda_icono"), Center, new Vector2(-190, 0), new Vector2(84, 74));
            if (shopIcon != null) shopIcon.raycastTarget = false;
            var back = AsciiButton.Create(root, "BackButton", "VOLVER", 44, Center, new Vector2(220, -350)).GetComponent<Button>();
            shop.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            back.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            var walletLabel = UiKit.Label(root, "Wallet", "// cartera: 0 bits", 32, AsciiTheme.Yellow, TextAnchor.MiddleRight, Center, new Vector2(330, 135), new Vector2(900, 44));
            UiKit.Label(root, "Hint", "// [ESPACIO] saltar   [ESC] pausa   // el % es lo más lejos que llegaste", 28, AsciiTheme.GreenDim, TextAnchor.MiddleCenter, Center, new Vector2(0, -430), new Vector2(1500, 40));

            AsciiUi.AddCrtOverlay(canvas);

            var ctrl = canvas.gameObject.AddComponent<LevelSelectController>();
            ctrl.levelButtons = buttons;
            ctrl.rowTexts = rows;
            ctrl.playerLabel = player;
            ctrl.backButton = back;
            ctrl.shopButton = shop;
            ctrl.walletLabel = walletLabel;

            SceneBuilder.Save(scene, SceneNames.LevelSelect);
        }
    }
}
