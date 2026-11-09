using UnityEngine;
using UnityEngine.SceneManagement;

namespace RunDashC
{
    /// <summary>
    /// Navegación entre escenas. La pausa carga Help en modo aditivo encima de Game
    /// (timeScale = 0) para no perder el estado de la partida.
    /// </summary>
    public static class SceneFlow
    {
        public static bool IsPaused { get; private set; }

        /// <summary>
        /// Frame en que se reanudó. Quien lea la acción Pause debe ignorarla si
        /// Time.frameCount == ResumedFrame (si no, Esc reanuda y vuelve a pausar en el mismo frame).
        /// </summary>
        public static int ResumedFrame { get; private set; } = -1;

        static bool loading;
        static AsyncOperation pendingHelp;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsPaused = false;
            ResumedFrame = -1;
            loading = false;
            pendingHelp = null;
            Time.timeScale = 1f;
        }

        public static void Load(string scene)
        {
            if (loading) return; // evita doble clic
            IsPaused = false;
            Time.timeScale = 1f;
            AudioManager.SetMusicDucked(false);

            var fader = ScreenFader.Instance;
            if (fader == null)
            {
                SceneManager.LoadScene(scene);
                return;
            }
            loading = true;
            fader.FadeOutIn(() =>
            {
                loading = false;
                SceneManager.LoadScene(scene);
            });
        }

        public static void Pause()
        {
            if (IsPaused || loading) return;
            IsPaused = true;
            Time.timeScale = 0f;
            AudioManager.SetMusicDucked(true);
            AudioManager.Play(Sfx.Pause);
            if (!SceneManager.GetSceneByName(SceneNames.Help).isLoaded)
                pendingHelp = SceneManager.LoadSceneAsync(SceneNames.Help, LoadSceneMode.Additive);
        }

        public static void Resume()
        {
            // Help abierta sola (sin partida debajo): no hay nada que reanudar.
            if (!SceneManager.GetSceneByName(SceneNames.Game).isLoaded)
            {
                Load(SceneNames.Menu);
                return;
            }
            IsPaused = false;
            ResumedFrame = Time.frameCount;
            Time.timeScale = 1f;
            AudioManager.SetMusicDucked(false);

            if (pendingHelp != null && !pendingHelp.isDone)
                pendingHelp.completed += _ => UnloadHelp();
            else
                UnloadHelp();
            pendingHelp = null;
        }

        static void UnloadHelp()
        {
            var help = SceneManager.GetSceneByName(SceneNames.Help);
            if (help.isLoaded) SceneManager.UnloadSceneAsync(help);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
