using UnityEngine;
using UnityEngine.UI;

namespace RunDashC.EditorTools
{
    /// <summary>
    /// Escena Habilidades: ventana de terminal como Niveles, contador de estrellas y las 10 habilidades en 2 columnas
    /// (AbilityList las arma en runtime: dependen de las estrellas). PONER/QUITAR de la corona junto a su tarjeta.
    /// </summary>
    public static class AbilitiesSceneBuilder
    {
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        public static void Build()
        {
            var scene = SceneBuilder.NewScene(withCamera: true);
            var canvas = AsciiUi.CreateCanvas("AbilitiesCanvas", 0);
            var root = canvas.transform;

            var fondo = SceneBuilder.LoadResource<Sprite>("Art/fondo");
            var bg = UiKit.SpriteImage(root, "Fondo", fondo, Center, Vector2.zero, AsciiUi.Reference);
            if (bg != null)
            {
                bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = fondo.rect.width / fondo.rect.height;
            }
            var rain = AsciiUi.CreateText(root, "Rain", "", 26, AsciiTheme.Green, TextAnchor.UpperLeft);
            AsciiUi.Stretch(rain.rectTransform);
            rain.gameObject.AddComponent<AsciiRain>().density = 0.3f;

            const int frameFs = 30;
            var frame = UiKit.Frame(root, "Frame", Vector2.zero, new Vector2(1888, 1048), frameFs, AsciiTheme.Green, ruleRow: 2);
            float barY = frame.rectTransform.sizeDelta.y * 0.5f - 1.5f * frameFs;
            UiKit.Label(root, "BarTitle", $"{AsciiTheme.Practica} - {AsciiTheme.GameTitle}", 36, AsciiTheme.White, TextAnchor.MiddleLeft, Center, new Vector2(0, barY), new Vector2(1800, 50));

            const float left = -860f;
            UiKit.LeftLabel(root, "Prompt", "> ./camarada --habilidades", 40, AsciiTheme.Green, left, 395)
                .gameObject.AddComponent<TypewriterText>();
            UiKit.SpriteImage(root, "StarIcon", SceneBuilder.LoadResource<Sprite>("Art/estrella"), Center, new Vector2(left + 28, 325), new Vector2(56, 56));
            var stars = UiKit.Label(root, "Stars", "estrellas: 0/21", 36, AsciiTheme.Yellow, TextAnchor.MiddleLeft, Center, new Vector2(left + 70 + 600, 325), new Vector2(1200, 50));
            UiKit.Label(root, "Header", "// 3 estrellas ocultas por nivel: difíciles, pero posibles", 28, AsciiTheme.GreenDim, TextAnchor.MiddleRight, Center, new Vector2(230, 325), new Vector2(1100, 40));

            var listRt = new GameObject("Abilities", typeof(RectTransform)).GetComponent<RectTransform>();
            listRt.SetParent(root, false);
            var listPos = new Vector2(left, 255);
            AsciiUi.Place(listRt, Center, listPos, Vector2.zero);
            var list = listRt.gameObject.AddComponent<AbilityList>();
            list.columnWidth = 900f;
            list.rowStep = 112f;
            list.iconSize = 88f;
            list.nameSize = 34;
            list.descSize = 28;

            // PONER/QUITAR a la derecha de la tarjeta de la corona (la oculta el controlador si aún no se gana).
            int corona = (int)Ability.Corona;
            var coronaPos = listPos + list.IconCenter(corona) + new Vector2(700, 0);
            var coronaBtn = AsciiButton.Create(root, "CoronaButton", "PONER", 32, Center, coronaPos).GetComponent<Button>();

            var back = AsciiButton.Create(root, "BackButton", "VOLVER", 44, Center, new Vector2(0, -420)).GetComponent<Button>();
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = coronaBtn };
            coronaBtn.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = back };
            UiKit.Label(root, "Hint", "// las habilidades son permanentes: se activan solas al conseguir las estrellas", 28, AsciiTheme.GreenDim, TextAnchor.MiddleCenter, Center, new Vector2(0, -470), new Vector2(1600, 40));

            AsciiUi.AddCrtOverlay(canvas);

            var ctrl = canvas.gameObject.AddComponent<AbilitiesController>();
            ctrl.starsText = stars;
            ctrl.backButton = back;
            ctrl.coronaButton = coronaBtn;

            SceneBuilder.Save(scene, SceneNames.Abilities);
        }
    }
}
