using System;
using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Eventos de gameplay (RunManager/Camarada) que escucha el HUD.
    /// Quien se suscriba debe desuscribirse en OnDisable/OnDestroy.
    /// </summary>
    public static class RunEvents
    {
        /// <summary>3, 2, 1 y 0 = "run();" (arranca la carrera).</summary>
        public static event Action<int> CountdownTick;
        /// <summary>Progreso del nivel, de 0 a 1.</summary>
        public static event Action<float> ProgressChanged;
        public static event Action PlayerHit;
        public static event Action Checkpoint;
        public static event Action LevelCompleted;
        /// <summary>Sin vidas: empieza la animación de muerte, luego se carga GameOver.</summary>
        public static event Action PlayerDied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            CountdownTick = null;
            ProgressChanged = null;
            PlayerHit = null;
            Checkpoint = null;
            LevelCompleted = null;
            PlayerDied = null;
        }

        public static void RaiseCountdown(int n) => CountdownTick?.Invoke(n);
        public static void RaiseProgress(float t) => ProgressChanged?.Invoke(Mathf.Clamp01(t));
        public static void RaisePlayerHit() => PlayerHit?.Invoke();
        public static void RaiseCheckpoint() => Checkpoint?.Invoke();
        public static void RaiseLevelCompleted() => LevelCompleted?.Invoke();
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
    }
}
