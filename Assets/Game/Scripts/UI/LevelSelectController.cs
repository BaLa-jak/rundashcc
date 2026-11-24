using UnityEngine;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>
    /// Selector de niveles ("ls -l" de ./niveles): una fila por nivel con barra ASCII del progreso máximo.
    /// El último nivel sale bloqueado hasta que los anteriores estén al 100% (GameSession.IsUnlocked).
    /// </summary>
    public class LevelSelectController : MonoBehaviour
    {
        /// <summary>Nombre "de archivo" de cada nivel, en el orden nivel1..nivelN.</summary>
        public static readonly string[] Files = { "main.c", "bugs.c", "sierras.c", "div_cero.c", "kernel.c", "deploy.c", "espejo.c" };
        public const string EndlessFile = "while(1).c";
        const int BarCells = 20;

        public Button[] levelButtons;
        public Text[] rowTexts;
        public Button endlessButton;
        public Text endlessRow;
        public Text playerLabel;
        public Button backButton;
        public Button shopButton;
        public Text walletLabel;

        GameObject firstChoice;
        bool leaving;

        void Awake()
        {
            playerLabel.text = "JUGADOR: " + GameSession.PlayerName;
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int level = i + 1;
                bool open = GameSession.IsUnlocked(level);
                levelButtons[i].interactable = open;
                levelButtons[i].GetComponent<AsciiButton>().Refresh();
                levelButtons[i].onClick.AddListener(() => Play(level));
                rowTexts[i].text = Row(level, open);
                if (!open)
                {
                    var blink = rowTexts[i].gameObject.AddComponent<Blink>();
                    blink.period = 1.2f;
                    blink.offAlpha = 0.35f;
                }
                // Por defecto: el primer nivel abierto que falte terminar.
                if (firstChoice == null && open && GameSession.LevelProgress(level) < 100) firstChoice = levelButtons[i].gameObject;
            }
            if (firstChoice == null) firstChoice = levelButtons[0].gameObject;
            endlessButton.onClick.AddListener(() => Play(GameSession.EndlessLevel));
            string C(Color c) => AsciiTheme.ToHex(c);
            endlessRow.text = $"<color=#{C(AsciiTheme.White)}>{EndlessFile.PadRight(12)}</color><color=#{C(AsciiTheme.Yellow)}>record: {GameSession.BestDistance:0000} m</color>  " +
                              $"<color=#{C(AsciiTheme.GreenDim)}>// infinito: cada vez más rápido</color>";
            backButton.onClick.AddListener(() => Leave(SceneNames.Menu));
            shopButton.onClick.AddListener(() => Leave(SceneNames.Shop));
            walletLabel.text = $"// cartera: {Cosmetics.Wallet} bits";
        }

        void Start()
        {
            AudioManager.PlayMusic(Music.Menu);
            UiKit.Select(firstChoice);
        }

        void Update() => UiKit.KeepSelection(firstChoice);

        static string Row(int level, bool open)
        {
            string C(Color c) => AsciiTheme.ToHex(c);
            string dim = C(AsciiTheme.GreenDim);
            string file = Files[level - 1].PadRight(12);
            if (!open)
                return $"<color=#{dim}>{file}</color><color=#{C(AsciiTheme.Red)}>-r--------  BLOQUEADO</color>  " +
                       $"<color=#{dim}>// completa 01-{GameSession.LevelCount - 1:00} al 100%</color>";

            int p = GameSession.LevelProgress(level);
            int filled = p * BarCells / 100;
            var color = p >= 100 ? AsciiTheme.Green : p > 0 ? AsciiTheme.Cyan : AsciiTheme.GreenDim;
            string note = p >= 100 ? "// ok" : p > 0 ? "// en progreso" : "// sin ejecutar";
            return $"<color=#{C(AsciiTheme.White)}>{file}</color><color=#{C(color)}>[{new string('#', filled)}{new string('.', BarCells - filled)}] {p,3}%</color>  " +
                   $"<color=#{dim}>{note}</color>";
        }

        void Play(int level)
        {
            if (leaving) return;
            AudioManager.Play(Sfx.Start);
            GameSession.StartLevel(level);
            Leave(SceneNames.Game);
        }

        void Leave(string scene)
        {
            if (leaving) return;
            leaving = true;
            SceneFlow.Load(scene);
        }
    }
}
