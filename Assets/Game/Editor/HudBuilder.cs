using UnityEngine;
using UnityEngine.UI;

namespace RunDashC.EditorTools
{
    /// <summary>
    /// Agrega el HUD (canvas + HudController cableado) a la escena abierta.
    /// La llama GameSceneBuilder después de crear la Main Camera y antes de guardar.
    /// </summary>
    public static class HudBuilder
    {
        public static void Build()
        {
            var canvas = AsciiUi.CreateCanvas("HUD", 100);
            var hud = canvas.gameObject.AddComponent<HudController>();
            var t = canvas.transform;

            // Flash rojo de golpe (fuera de Root para que no tiemble con el glitch).
            hud.hitFlash = AsciiUi.CreatePanel(t, "HitFlash", Color.clear);
            AsciiUi.Stretch(hud.hitFlash.rectTransform);

            var root = new GameObject("Root", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(t, false);
            AsciiUi.Stretch(root);
            hud.rootGlitch = root.gameObject.AddComponent<HudGlitch>();

            // Barra superior como el mockup: franja oscura + línea de guiones.
            var bar = AsciiUi.CreatePanel(root, "TopBar", AsciiTheme.WithAlpha(Color.black, 0.7f));
            AsciiUi.Place(bar.rectTransform, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 76));
            var barFill = new Vector2(-96, 0);
            Label(bar.transform, "Title", $"{AsciiTheme.Practica} - {AsciiTheme.GameTitle}", 42, AsciiTheme.White,
                TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(0, 4), barFill);
            hud.infoText = Label(bar.transform, "Info", "JUGADOR: SYSADMIN    NIVEL 01", 42, AsciiTheme.White,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0, 4), barFill);
            Label(bar.transform, "PauseHint", "[ESC] pausa", 38, AsciiTheme.Cyan,
                TextAnchor.MiddleRight, Vector2.zero, Vector2.one, new Vector2(0, 4), barFill);
            Label(bar.transform, "Dashes", new string('-', 200).Replace("--", "- "), 28, AsciiTheme.WithAlpha(AsciiTheme.White, 0.45f),
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1, 0), new Vector2(0, -2), new Vector2(0, 20));

            var top = new Vector2(0.5f, 1f);
            hud.progressText = Label(root, "Progress", "[....................]   0%", 38, AsciiTheme.Green,
                TextAnchor.MiddleCenter, top, top, new Vector2(0, -84), new Vector2(900, 50));
            hud.toastText = Label(root, "Toast", "", 36, AsciiTheme.Cyan,
                TextAnchor.MiddleCenter, top, top, new Vector2(0, -132), new Vector2(900, 50));

            var center = new Vector2(0.5f, 0.5f);
            hud.countdownText = Label(root, "Countdown", "", 300, AsciiTheme.White,
                TextAnchor.MiddleCenter, center, center, new Vector2(0, 60), new Vector2(1400, 400));

            // Franja inferior oscura: sin ella, vidas y bits se enciman con el texto del suelo.
            var bottomBar = AsciiUi.CreatePanel(root, "BottomBar", AsciiTheme.WithAlpha(Color.black, 0.93f));
            AsciiUi.Place(bottomBar.rectTransform, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 110));

            hud.livesText = Label(root, "Lives", "[###] VIDAS = 3", 54, AsciiTheme.White,
                TextAnchor.LowerLeft, Vector2.zero, Vector2.zero, new Vector2(48, 30), new Vector2(800, 70));
            hud.livesGlitch = hud.livesText.gameObject.AddComponent<HudGlitch>();
            hud.livesGlitch.amplitude = 10f;
            hud.lifeLostText = Label(root, "LifeLost", "ERROR: vida perdida", 38, AsciiTheme.Red,
                TextAnchor.LowerLeft, Vector2.zero, Vector2.zero, new Vector2(48, 118), new Vector2(800, 50));
            hud.lifeLostText.gameObject.SetActive(false);

            // Centro de la franja: estrellas del nivel + iconos de habilidades (los arma HudAbilities en runtime).
            var abilities = new GameObject("Abilities", typeof(RectTransform)).GetComponent<RectTransform>();
            abilities.SetParent(root, false);
            AsciiUi.Place(abilities, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 55), Vector2.zero);
            abilities.gameObject.AddComponent<HudAbilities>();

            hud.bitsText = Label(root, "Bits", "bits: 000   record: 000", 44, AsciiTheme.Green,
                TextAnchor.LowerRight, Vector2.right, Vector2.right, new Vector2(-48, 30), new Vector2(1100, 70));

            // Overlay de nivel completado (el texto con marco lo tipea HudController).
            var panel = AsciiUi.CreatePanel(t, "LevelComplete", AsciiTheme.WithAlpha(AsciiTheme.Bg, 0.88f));
            AsciiUi.Place(panel.rectTransform, center, center, new Vector2(0, 40), new Vector2(1000, 420));
            hud.completePanel = panel.gameObject;
            hud.completeText = AsciiUi.CreateText(panel.transform, "Text", "", 44, AsciiTheme.Green);
            AsciiUi.Stretch(hud.completeText.rectTransform);
            panel.gameObject.SetActive(false);

            hud.fatalText = Label(t, "Fatal", "FATAL ERROR", 180, AsciiTheme.Red,
                TextAnchor.MiddleCenter, center, center, new Vector2(0, 40), new Vector2(1600, 420));
            hud.fatalGlitch = hud.fatalText.gameObject.AddComponent<HudGlitch>();
            hud.fatalGlitch.amplitude = 14f;
            hud.fatalText.gameObject.SetActive(false);

            AsciiUi.AddCrtOverlay(canvas);
        }

        /// <summary>Texto posicionado con un Shadow transparente que HudGlitch usa como aberración.</summary>
        static Text Label(Transform parent, string name, string text, int size, Color color, TextAnchor align,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 sizeDelta)
        {
            var label = AsciiUi.CreateText(parent, name, text, size, color, align);
            AsciiUi.Place(label.rectTransform, anchorMin, anchorMax, pos, sizeDelta);
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Color.clear;
            shadow.effectDistance = Vector2.zero;
            return label;
        }
    }
}
