using UnityEngine;
using UnityEngine.UI;

namespace RunDashC.EditorTools
{
    /// <summary>
    /// Escena Tienda: misma ventana de terminal que Niveles. Izquierda: el duende (Art/duende) con su diálogo.
    /// Derecha: Camarada vestida (preview), rejilla 4x5 de cosméticos con precio y línea de detalle; [ VOLVER ].
    /// </summary>
    public static class ShopSceneBuilder
    {
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        static readonly Vector2 ItemSize = new Vector2(112, 84);
        static readonly string[] RowNames = { "tenis", "camisetas", "guantes", "sombreros" }; // índice = CosmeticSlot

        public static void Build()
        {
            var scene = SceneBuilder.NewScene(withCamera: true);
            var canvas = AsciiUi.CreateCanvas("ShopCanvas", 0);
            var root = canvas.transform;

            // Fondo atenuado + lluvia (igual que el menú).
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

            // Ventana con barra de título: nombre a la izquierda, cartera a la derecha.
            const int frameFs = 30;
            var frame = UiKit.Frame(root, "Frame", Vector2.zero, new Vector2(1888, 1048), frameFs, AsciiTheme.Green, ruleRow: 2);
            float barY = frame.rectTransform.sizeDelta.y * 0.5f - 1.5f * frameFs;
            UiKit.Label(root, "BarTitle", "BLACK_MARKET.exe - tienda del duende", 36, AsciiTheme.White, TextAnchor.MiddleLeft, Center, new Vector2(0, barY), new Vector2(1800, 50));
            var wallet = UiKit.Label(root, "Wallet", "BITS: 0000", 36, AsciiTheme.Yellow, TextAnchor.MiddleRight, Center, new Vector2(0, barY), new Vector2(1800, 50));

            // Duende enmarcado + su diálogo.
            var duendePos = new Vector2(-460, 40);
            UiKit.Frame(root, "DuendeFrame", duendePos, new Vector2(880, 680), 24, AsciiTheme.GreenDim);
            UiKit.SpriteImage(root, "Duende", SceneBuilder.LoadResource<Sprite>("Art/duende"), Center, duendePos, new Vector2(840, 627));
            UiKit.LeftLabel(root, "DuendeTag", "// duende.exe dice:", 28, AsciiTheme.GreenDim, -900, -322);
            var dialog = UiKit.Label(root, "Dialog", "", 32, AsciiTheme.Green, TextAnchor.MiddleLeft, Center, new Vector2(-460, -362), new Vector2(880, 44));
            dialog.supportRichText = false;
            dialog.gameObject.AddComponent<TypewriterText>();

            // Preview: Camarada corriendo en su sitio con una Image hija por categoría (mismas anclas que en el juego).
            var ratSprite = SceneBuilder.LoadResource<Sprite>("Art/camarada");
            var slots = new Image[Cosmetics.Slots.Length];
            if (ratSprite != null)
            {
                float w = 330f, h = w * ratSprite.rect.height / ratSprite.rect.width;
                var rat = UiKit.SpriteImage(root, "Camarada", ratSprite, Center, Vector2.zero, new Vector2(w, h));
                rat.rectTransform.pivot = new Vector2(0.5f, 0f);
                rat.rectTransform.anchoredPosition = new Vector2(460, 215);
                rat.gameObject.AddComponent<UiBob>().height = 10f;
                for (int s = 0; s < slots.Length; s++) // orden del enum = orden de dibujo
                {
                    var slot = (CosmeticSlot)s;
                    var img = AsciiUi.CreatePanel(rat.transform, slot.ToString(), Color.white);
                    img.preserveAspect = true;
                    img.enabled = false;
                    AsciiUi.Place(img.rectTransform, Center, Vector2.zero, Vector2.zero); // ShopController le da el tamaño
                    slots[s] = img;
                }
            }
            UiKit.Label(root, "CamaradaTag", "// " + AsciiTheme.CharacterName.ToLowerInvariant() + ".exe", 26, AsciiTheme.GreenDim, TextAnchor.MiddleCenter, Center, new Vector2(460, 192), new Vector2(400, 36));

            // Rejilla: una fila por categoría (orden de Cosmetics.All).
            int n = Cosmetics.All.Length;
            var buttons = new Button[n];
            var icons = new Image[n];
            var prices = new Text[n];
            for (int i = 0; i < n; i++)
            {
                int row = i / 5, col = i % 5;
                if (col == 0)
                    UiKit.LeftLabel(root, "RowLabel" + row, "// " + RowNames[(int)Cosmetics.All[i].Slot], 28, AsciiTheme.GreenDim, 30, 95 - row * 130);
                var pos = new Vector2(300 + col * 125, 95 - row * 130);
                CreateItem(root, Cosmetics.All[i], pos, out buttons[i], out icons[i], out prices[i]);
            }

            var detail = UiKit.Label(root, "Detail", "> selecciona un artículo", 30, AsciiTheme.White, TextAnchor.MiddleCenter, Center, new Vector2(460, -378), new Vector2(900, 40));
            var back = AsciiButton.Create(root, "BackButton", "VOLVER", 44, Center, new Vector2(460, -440)).GetComponent<Button>();
            UiKit.Label(root, "Hint", "// [ENTER] comprar / poner / quitar", 28, AsciiTheme.GreenDim, TextAnchor.MiddleLeft, Center, new Vector2(-460, -440), new Vector2(880, 40));

            AsciiUi.AddCrtOverlay(canvas);

            var ctrl = canvas.gameObject.AddComponent<ShopController>();
            ctrl.itemButtons = buttons;
            ctrl.itemIcons = icons;
            ctrl.priceTexts = prices;
            ctrl.previewSlots = slots;
            ctrl.walletText = wallet;
            ctrl.detailText = detail;
            ctrl.dialogText = dialog;
            ctrl.backButton = back;

            SceneBuilder.Save(scene, SceneNames.Shop);
        }

        /// <summary>Casilla: caja ASCII + icono + precio debajo. Resalte verde al seleccionar (ColorTint sobre el fondo).</summary>
        static void CreateItem(Transform root, Cosmetic c, Vector2 pos, out Button button, out Image icon, out Text price)
        {
            var go = new GameObject("Item_" + c.Id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(root, false);
            AsciiUi.Place((RectTransform)go.transform, Center, pos, ItemSize);
            var bg = go.GetComponent<Image>();
            bg.color = AsciiTheme.Green;

            button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.ColorTint;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.clear;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.3f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0.3f);
            colors.pressedColor = AsciiTheme.WithAlpha(AsciiTheme.Cyan, 0.7f);
            colors.disabledColor = Color.clear;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            UiKit.Frame(go.transform, "Box", Vector2.zero, ItemSize, 16, AsciiTheme.GreenDim);
            icon = AsciiUi.CreatePanel(go.transform, "Icon", Color.white);
            icon.preserveAspect = true;
            icon.sprite = SceneBuilder.LoadResource<Sprite>("Art/" + c.Art);
            AsciiUi.Place(icon.rectTransform, Center, Vector2.zero, ItemSize - new Vector2(20, 20));
            price = UiKit.Label(go.transform, "Price", "", 24, AsciiTheme.Yellow, TextAnchor.MiddleCenter, Center, new Vector2(0, -56), new Vector2(124, 28));
        }
    }
}
