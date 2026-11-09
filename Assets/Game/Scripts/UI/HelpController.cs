using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>
    /// Pantalla de pausa (escena Help, aditiva sobre Game con timeScale 0).
    /// REANUDAR o la acción Player/Pause (Esc/P/Start) llaman a SceneFlow.Resume().
    /// </summary>
    public class HelpController : MonoBehaviour
    {
        public Button resumeButton;

        InputAction pause;
        int openedFrame;
        bool resumed;

        void Awake()
        {
            openedFrame = Time.frameCount;
            pause = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Pause") : null;
            resumeButton.onClick.AddListener(Resume);
        }

        void Start() => UiKit.Select(resumeButton.gameObject);

        void Update()
        {
            UiKit.KeepSelection(resumeButton.gameObject);
            // En el primer frame la tecla que abrió la pausa todavía cuenta como "presionada".
            if (Time.frameCount != openedFrame && pause != null && pause.WasPressedThisFrame()) Resume();
        }

        void Resume()
        {
            if (resumed) return;
            resumed = true;
            SceneFlow.Resume();
        }
    }
}
