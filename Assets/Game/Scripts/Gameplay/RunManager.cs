using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RunDashC
{
    public enum RunState { Countdown, Running, Dying, Completed }

    /// <summary>
    /// Máquina de estados de la partida: Countdown → Running → (Dying | Completed).
    /// Golpes, vidas, checkpoint, muerte, meta y pausa. Los peligros y Camarada llaman a <see cref="HitPlayer"/>.
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        const float FadeInWait = 0.35f;
        const float CountdownStep = 0.6f;
        const float HitstopScale = 0.05f, HitstopSeconds = 0.06f;
        const float DeathSlowMo = 0.3f, DeathDelay = 1.2f;
        const float CompleteDelay = 3f;

        public LevelBuilder level;
        public CamaradaController player;
        public CameraRig cameraRig;

        public static RunManager Instance { get; private set; }
        public RunState State { get; private set; } = RunState.Countdown;
        public bool IsInvulnerable => Time.time < invulnerableUntil;
        /// <summary>Último checkpoint (o el inicio).</summary>
        public Vector2 RespawnPoint { get; private set; }

        float invulnerableUntil = -1f;
        InputAction pauseAction;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake()
        {
            Instance = this;
            if (InputSystem.actions != null) pauseAction = InputSystem.actions.FindAction("Player/Pause");
        }

        void OnEnable() => GameSession.Changed += OnSessionChanged;
        void OnDisable() => GameSession.Changed -= OnSessionChanged;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        IEnumerator Start()
        {
            AudioManager.PlayMusic(Music.Level1);
            RespawnPoint = level.StartPosition;
            player.levelSpeed = level.Data.Speed;
            player.Respawn(RespawnPoint);

            yield return new WaitForSeconds(FadeInWait);
            for (int n = 3; n >= 1; n--)
            {
                if (State != RunState.Countdown) yield break;
                RunEvents.RaiseCountdown(n);
                AudioManager.Play(Sfx.Countdown);
                yield return new WaitForSeconds(CountdownStep); // escalado: se congela en pausa
            }
            if (State != RunState.Countdown) yield break;
            RunEvents.RaiseCountdown(0);
            AudioManager.Play(Sfx.Go);
            State = RunState.Running;
        }

        void Update()
        {
            if (pauseAction != null && pauseAction.WasPressedThisFrame() && !SceneFlow.IsPaused &&
                Time.frameCount != SceneFlow.ResumedFrame && (State == RunState.Countdown || State == RunState.Running))
                SceneFlow.Pause();

            if (State == RunState.Running)
                RunEvents.RaiseProgress((player.transform.position.x - level.StartPosition.x) / Mathf.Max(0.01f, level.FinishX - level.StartPosition.x));
        }

        /// <summary>Golpe a Camarada. <paramref name="respawn"/> = caída o pared: vuelve al checkpoint.</summary>
        public void HitPlayer(bool respawn)
        {
            if (State != RunState.Running) return;
            if (IsInvulnerable)
            {
                if (respawn) player.Respawn(RespawnPoint); // sin vida extra, pero nunca se queda trabada
                return;
            }

            invulnerableUntil = Time.time + RunTuning.InvulnerableTime;
            AsciiParticles.Burst(player.transform.position, "!#x*", AsciiTheme.Red, 10, 5f, 0.5f, 0.45f, 8f);
            RunEvents.RaisePlayerHit();
            AudioManager.Play(Sfx.Hit, 0.05f);
            if (cameraRig != null) cameraRig.Shake(0.3f, 0.3f);
            GameSession.LoseLife(); // con 0 vidas, OnSessionChanged pasa a Dying
            if (State == RunState.Dying) return;

            if (respawn) player.Respawn(RespawnPoint);
            StartCoroutine(Hitstop());
        }

        public void SetCheckpoint(Vector2 position) => RespawnPoint = position;

        /// <summary>Llamado por la meta.</summary>
        public void Complete()
        {
            if (State != RunState.Running) return;
            State = RunState.Completed;
            RunEvents.RaiseProgress(1f);
            AudioManager.Play(Sfx.LevelComplete);
            RunEvents.RaiseLevelCompleted();
            if (cameraRig != null) cameraRig.follow = false; // Camarada sale corriendo de la pantalla
            StartCoroutine(LoadNextLevel());
        }

        IEnumerator LoadNextLevel()
        {
            yield return new WaitForSecondsRealtime(CompleteDelay);
            GameSession.NextLevel();
            SceneFlow.Load(SceneNames.Game);
        }

        IEnumerator Hitstop()
        {
            Time.timeScale = HitstopScale;
            yield return new WaitForSecondsRealtime(HitstopSeconds);
            if (!SceneFlow.IsPaused && State != RunState.Dying) Time.timeScale = 1f;
        }

        void OnSessionChanged()
        {
            if (GameSession.Lives <= 0 && (State == RunState.Countdown || State == RunState.Running))
                StartCoroutine(Die());
        }

        IEnumerator Die()
        {
            State = RunState.Dying;
            RunEvents.RaisePlayerDied();
            if (player != null) player.Explode();
            if (cameraRig != null) cameraRig.Shake(0.5f, 0.5f);
            Time.timeScale = DeathSlowMo;
            yield return new WaitForSecondsRealtime(DeathDelay);
            SceneFlow.Load(SceneNames.GameOver);
        }
    }
}
