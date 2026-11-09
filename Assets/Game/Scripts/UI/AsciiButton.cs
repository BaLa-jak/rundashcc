using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>
    /// Botón de terminal: "[ INICIAR ]" en verde; con hover o selección "> [ INICIAR ] <" invertido
    /// (fondo verde, texto negro) + glitch + Sfx.Hover. Al pulsar: Sfx.Click, flash cian y punch de escala.
    /// El onClick del Button lo conecta el controlador de la escena. Tiempo no escalado (funciona en pausa).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class AsciiButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler, IPointerClickHandler, ISubmitHandler
    {
        const string Symbols = "#$%&@!?/\\|*";
        const float GlitchTime = 0.12f, FlashTime = 0.14f, PunchTime = 0.18f;

        public string label = "BOTON";
        public Text text;
        public Image background;

        Button button;
        bool hovered, selected, animating;
        float glitchUntil, flashUntil, punchUntil, silentUntil;

        bool Highlighted => hovered || selected;

        void Awake() => button = GetComponent<Button>();

        // La selección inicial al abrir la escena no debe sonar.
        void OnEnable() => silentUntil = Time.unscaledTime + 0.2f;

        public void OnPointerEnter(PointerEventData e)
        {
            if (!button.IsInteractable()) return;
            SetState(true, selected);
            // El mouse también mueve la selección (así teclado y mouse no muestran dos botones activos),
            // salvo que se esté escribiendo en un InputField.
            var es = EventSystem.current;
            var cur = es != null ? es.currentSelectedGameObject : null;
            if (es != null && (cur == null || !cur.TryGetComponent<InputField>(out var field) || !field.isFocused))
                es.SetSelectedGameObject(gameObject);
        }

        public void OnPointerExit(PointerEventData e) => SetState(false, selected);
        public void OnSelect(BaseEventData e) => SetState(hovered, true);
        public void OnDeselect(BaseEventData e) => SetState(hovered, false);
        public void OnPointerClick(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) Press(); }
        public void OnSubmit(BaseEventData e) => Press();

        void SetState(bool hov, bool sel)
        {
            bool was = Highlighted;
            hovered = hov;
            selected = sel;
            if (!was && Highlighted)
            {
                glitchUntil = Time.unscaledTime + GlitchTime;
                if (Time.unscaledTime > silentUntil) AudioManager.Play(Sfx.Hover, 0.08f, 0.6f);
            }
            animating = true;
            Refresh();
        }

        void Press()
        {
            if (!button.IsInteractable()) return;
            AudioManager.Play(Sfx.Click, 0.05f);
            float now = Time.unscaledTime;
            flashUntil = now + FlashTime;
            punchUntil = now + PunchTime;
            glitchUntil = now + GlitchTime;
            animating = true;
        }

        void Update()
        {
            if (!animating) return;
            float now = Time.unscaledTime;
            animating = now < glitchUntil || now < flashUntil || now < punchUntil;
            Refresh();
        }

        /// <summary>Redibuja el estado actual (también lo usa el builder para que la escena guardada se vea bien).</summary>
        public void Refresh()
        {
            if (text == null) return;
            float now = Application.isPlaying ? Time.unscaledTime : 0f;
            bool flash = now < flashUntil;
            bool inverted = flash || Highlighted;

            var l = now < glitchUntil ? Glitch(label) : label;
            text.text = Highlighted ? $"> [ {l} ] <" : $"[ {l} ]";
            text.color = inverted ? AsciiTheme.Bg : AsciiTheme.Green;
            if (background != null)
                background.color = flash ? AsciiTheme.Cyan : Highlighted ? AsciiTheme.Green : Color.clear;

            float punch = Mathf.Clamp01((punchUntil - now) / PunchTime);
            transform.localScale = Vector3.one * (1f + 0.12f * punch * punch);
        }

        static string Glitch(string s)
        {
            var c = s.ToCharArray();
            for (int i = 0; i < c.Length; i++)
                if (c[i] != ' ' && Random.value < 0.35f) c[i] = Symbols[Random.Range(0, Symbols.Length)];
            return new string(c);
        }

        /// <summary>Crea el botón completo (Image de fondo + Button + Text hijo). El tamaño cabe en "> [ LABEL ] <".</summary>
        public static AsciiButton Create(Transform parent, string name, string label, int fontSize, Vector2 anchor, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            AsciiUi.Place(rt, anchor, pos, new Vector2((label.Length + 10) * UiKit.Cw(fontSize), fontSize * 1.2f));

            var img = go.GetComponent<Image>();
            img.color = Color.clear; // sigue recibiendo clics aunque sea transparente

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;

            var t = AsciiUi.CreateText(go.transform, "Label", "", fontSize, AsciiTheme.Green);
            AsciiUi.Stretch(t.rectTransform);
            t.supportRichText = false;

            var ab = go.AddComponent<AsciiButton>();
            ab.label = label;
            ab.text = t;
            ab.background = img;
            ab.Refresh();
            return ab;
        }
    }
}
