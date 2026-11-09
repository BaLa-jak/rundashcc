using UnityEngine;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>Game Over: estadísticas de la partida, REANUDAR (mismo nivel) o SALIR al menú.</summary>
    public class GameOverController : MonoBehaviour
    {
        public Text statsText;
        public GameObject newRecord;
        public GlitchText title;
        public Button retryButton;
        public Button exitButton;

        bool leaving;

        void Awake()
        {
            retryButton.onClick.AddListener(Retry);
            exitButton.onClick.AddListener(() => Leave(SceneNames.Menu));

            string num = AsciiTheme.ToHex(AsciiTheme.Cyan), dim = AsciiTheme.ToHex(AsciiTheme.GreenDim);
            string Line(string name, string value) =>
                $"{name} <color=#{dim}>=</color> <color=#{num}>{value}</color><color=#{dim}>;</color>";
            statsText.text = string.Join("\n",
                Line("nivel_alcanzado", GameSession.Level.ToString("00")),
                Line("bits", GameSession.Bits.ToString("000")),
                Line("record", GameSession.BestBits.ToString("000")));
            newRecord.SetActive(GameSession.IsNewRecord);
        }

        void Start()
        {
            AudioManager.PlayMusic(Music.None);
            AudioManager.Play(Sfx.GameOver);
            if (title != null) title.Burst(0.7f, 18f);
            UiKit.Select(retryButton.gameObject);
        }

        void Update() => UiKit.KeepSelection(retryButton.gameObject);

        void Retry()
        {
            if (leaving) return;
            GameSession.RetryLevel();
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
