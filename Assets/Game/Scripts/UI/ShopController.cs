using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>
    /// Tienda del duende ("BLACK MARKET"): rejilla de 20 cosméticos (Cosmetics.All, mismo orden que itemButtons),
    /// cartera de bits, preview de Camarada vestida y diálogo del duende. Clic en un artículo: si no lo tienes
    /// y alcanza, se compra y se pone; si ya es tuyo, se pone o se quita.
    /// </summary>
    public class ShopController : MonoBehaviour
    {
        static readonly string[] Welcome =
        {
            "Psst... camarada. Bits limpios, ropa sin preguntas.",
            "Bienvenido al mercado negro. Aquí no hay garantía.",
            "Mira sin tocar... o paga y toca.",
        };
        static readonly string[] Bought =
        {
            "Trato hecho, camarada. Nada de reembolsos.",
            "Te queda de lujo. Bueno, mejor que antes.",
            "Excelente elección. Mis bits te lo agradecen.",
        };
        static readonly string[] Broke =
        {
            "Vuelve cuando tengas más bits, rata.",
            "Ja! Con eso no te alcanza ni para el cordón.",
            "Sin bits no hay trato. Ve a correr un rato.",
        };

        public Button[] itemButtons;
        public Image[] itemIcons;
        public Text[] priceTexts;
        public Image[] previewSlots; // índice = (int)CosmeticSlot
        public Text walletText;
        public Text detailText;
        public Text dialogText;
        public Button backButton;

        TypewriterText typer;
        GameObject lastSelected;
        bool leaving;

        void Awake()
        {
            typer = dialogText.GetComponent<TypewriterText>();
            for (int i = 0; i < itemButtons.Length; i++)
            {
                int index = i;
                itemButtons[i].onClick.AddListener(() => Click(index));
                // El mouse también mueve la selección (como AsciiButton), así el detalle sigue al puntero.
                var trigger = itemButtons[i].gameObject.AddComponent<EventTrigger>();
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                var go = itemButtons[i].gameObject;
                enter.callback.AddListener(_ => UiKit.Select(go));
                trigger.triggers.Add(enter);
            }
            backButton.onClick.AddListener(Leave);
            Cosmetics.Changed += Refresh;
            Refresh();
        }

        void OnDestroy() => Cosmetics.Changed -= Refresh;

        void Start()
        {
            AudioManager.PlayMusic(Music.Shop);
            UiKit.Select(itemButtons[0].gameObject);
            typer.Play(Pick(Welcome)); // en Start: TypewriterText ya tomó su texto en Awake/OnEnable
        }

        void Update()
        {
            UiKit.KeepSelection(itemButtons[0].gameObject);
            var cur = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (cur == lastSelected) return;
            // Sonido al moverse entre artículos (los AsciiButton ya suenan solos); no en la selección inicial.
            if (lastSelected != null && SelectedIndex() >= 0) AudioManager.Play(Sfx.Hover, 0.08f, 0.6f);
            lastSelected = cur;
            ShowDetail();
        }

        int SelectedIndex()
        {
            var cur = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return cur == null ? -1 : System.Array.IndexOf(itemButtons, cur.GetComponent<Button>());
        }

        void Click(int index)
        {
            if (leaving) return;
            var c = Cosmetics.All[index];
            if (Cosmetics.Owns(c))
            {
                bool on = Cosmetics.Toggle(c);
                AudioManager.Play(on ? Sfx.Checkpoint : Sfx.Click);
                Say(on ? $"Así que {c.Nombre.ToLowerInvariant()}... buen gusto." : "Guardado en tu inventario. Sin cargo extra.");
            }
            else if (Cosmetics.TryBuy(c))
            {
                AudioManager.Play(Sfx.LevelComplete, 0f, 0.6f);
                Say(Pick(Bought));
            }
            else
            {
                AudioManager.Play(Sfx.Hit, 0.05f, 0.6f);
                Say(Pick(Broke));
            }
        }

        void Refresh()
        {
            int wallet = Cosmetics.Wallet;
            walletText.text = $"BITS: {wallet:0000}";
            for (int i = 0; i < itemButtons.Length; i++)
            {
                var c = Cosmetics.All[i];
                bool owned = Cosmetics.Owns(c);
                if (Cosmetics.IsEquipped(c)) Set(priceTexts[i], "[PUESTO]", AsciiTheme.Cyan);
                else if (owned) Set(priceTexts[i], "TUYO", AsciiTheme.Green);
                else Set(priceTexts[i], c.Precio + " bits", wallet >= c.Precio ? AsciiTheme.Yellow : AsciiTheme.Red);
                // Lo que no alcanza se ve apagado (sigue seleccionable para ver el precio y oír al duende).
                itemIcons[i].color = owned || wallet >= c.Precio ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f);
            }
            foreach (var slot in Cosmetics.Slots) ShowPreview(slot);
            ShowDetail();
        }

        static void Set(Text t, string value, Color color)
        {
            t.text = value;
            t.color = color;
        }

        /// <summary>Como en CamaradaFx: la capa wear_* centrada sobre la rata, a la misma escala de pixel.</summary>
        void ShowPreview(CosmeticSlot slot)
        {
            var img = previewSlots[(int)slot];
            var c = Cosmetics.Equipped(slot);
            var sprite = c.HasValue ? Resources.Load<Sprite>("Art/" + c.Value.Wear) : null;
            img.enabled = sprite != null;
            if (sprite == null) return;
            img.sprite = sprite;
            var rat = img.transform.parent.GetComponent<Image>();
            float k = rat.rectTransform.rect.width / rat.sprite.rect.width; // px de UI por px de textura
            img.rectTransform.sizeDelta = sprite.rect.size * k;
        }

        void ShowDetail()
        {
            int i = SelectedIndex();
            if (i < 0)
            {
                detailText.text = "> selecciona un artículo";
                return;
            }
            var c = Cosmetics.All[i];
            string action = Cosmetics.IsEquipped(c) ? "[ENTER] quitar" : Cosmetics.Owns(c) ? "[ENTER] poner" :
                Cosmetics.Wallet >= c.Precio ? $"[ENTER] comprar por {c.Precio} bits" : $"te faltan {c.Precio - Cosmetics.Wallet} bits";
            detailText.text = $"> {c.Nombre}  // {action}";
        }

        void Say(string line)
        {
            typer.delay = 0f;
            typer.Play(line);
        }

        static string Pick(string[] lines) => lines[Random.Range(0, lines.Length)];

        void Leave()
        {
            if (leaving) return;
            leaving = true;
            SceneFlow.Load(SceneNames.LevelSelect);
        }
    }
}
