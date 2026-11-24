using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>Menú principal: nombre del jugador, INICIAR (→ selector de niveles) / HABILIDADES / SALIR, récord, estrellas y música.</summary>
    public class MenuController : MonoBehaviour
    {
        public InputField nameInput;
        public Text playerLabel;
        public Text recordText;
        public Button startButton;
        public Button quitButton;
        public Button abilitiesButton;
        public Text starsText;
        public Button closeButton;

        bool started;

        void Awake()
        {
            nameInput.characterLimit = GameSession.MaxNameLength;
            nameInput.text = GameSession.SavedName;
            // Mayúsculas al escribir y sin '<' '>' (romperían el rich text de la barra).
            nameInput.onValidateInput = (_, __, c) => c == '<' || c == '>' ? '\0' : char.ToUpperInvariant(c);
            nameInput.onValueChanged.AddListener(OnNameChanged);
            nameInput.onSubmit.AddListener(_ => StartGame());

            startButton.onClick.AddListener(StartGame);
            quitButton.onClick.AddListener(SceneFlow.Quit);
            abilitiesButton.onClick.AddListener(OpenAbilities);
            if (closeButton != null) closeButton.onClick.AddListener(SceneFlow.Quit);

            recordText.text = $"// record: {GameSession.BestBits} bits";
            starsText.text = $"// estrellas: {Stars.Total}/{Stars.Max}   habilidades: {Abilities.UnlockedCount(Stars.Total)}/{Abilities.All.Length}";
            ShowPlayer(nameInput.text);
        }

        void Start()
        {
            AudioManager.PlayMusic(Music.Menu);
            UiKit.Select(startButton.gameObject);
        }

        void Update()
        {
            UiKit.KeepSelection(startButton.gameObject);
            // Con el campo activo, InputField se come la navegación: flecha abajo o dpad regresan a INICIAR.
            if (!nameInput.isFocused) return;
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            if ((kb != null && kb.downArrowKey.wasPressedThisFrame) || (pad != null && pad.dpad.down.wasPressedThisFrame))
                UiKit.Select(startButton.gameObject);
        }

        void OnNameChanged(string value)
        {
            AudioManager.Play(Sfx.Type, 0.15f, 0.5f);
            ShowPlayer(value);
        }

        void ShowPlayer(string value) => playerLabel.text = "JUGADOR: " + GameSession.SanitizeName(value);

        void OpenAbilities()
        {
            if (started) return;
            started = true;
            AbilitiesController.ReturnTo = SceneNames.Menu;
            SceneFlow.Load(SceneNames.Abilities);
        }

        void StartGame()
        {
            if (started) return;
            started = true;
            AudioManager.Play(Sfx.Start);
            GameSession.StartNewRun(nameInput.text);
            SceneFlow.Load(SceneNames.LevelSelect);
        }
    }
}
