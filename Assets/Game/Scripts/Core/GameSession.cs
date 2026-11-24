using System;
using System.Text;
using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Estado de la partida que sobrevive entre escenas: jugador, nivel, vidas y bits.
    /// También guarda en PlayerPrefs el progreso máximo (0..100) de cada nivel y decide qué niveles están desbloqueados.
    /// Cualquier cambio dispara <see cref="Changed"/> (el HUD y el RunManager lo escuchan).
    /// </summary>
    public static class GameSession
    {
        public const int BaseLives = 3;
        /// <summary>3, o 4 con la habilidad VIDA EXTRA.</summary>
        public static int MaxLives => BaseLives + (Abilities.Has(Ability.VidaExtra) ? 1 : 0);
        public const int MaxNameLength = 12;
        public const string DefaultName = "SYSADMIN";
        /// <summary>Niveles en Resources/Levels (nivel1..nivelN). El último se desbloquea con los demás al 100%.</summary>
        public const int LevelCount = 7;
        /// <summary>Nivel infinito (después de los normales): tramos de Resources/Levels/Infinito, puntaje = distancia.</summary>
        public const int EndlessLevel = LevelCount + 1;

        const string PrefName = "rdc.player";
        const string PrefBest = "rdc.best";
        const string PrefProgress = "rdc.lvl"; // + número de nivel
        const string PrefBestDistance = "rdc.inf.best";

        public static event Action Changed;

        public static string PlayerName { get; private set; } = DefaultName;
        public static int Level { get; private set; } = 1;
        public static int Lives { get; private set; } = BaseLives;
        public static int Bits { get; private set; }
        public static bool IsNewRecord { get; private set; }
        public static bool IsNewDistanceRecord { get; private set; }
        public static int BestBits => PlayerPrefs.GetInt(PrefBest, 0);
        /// <summary>Metros recorridos en el infinito y su récord.</summary>
        public static int Distance { get; private set; }
        public static int BestDistance => PlayerPrefs.GetInt(PrefBestDistance, 0);
        public static bool IsEndless => Level == EndlessLevel;
        public static string SavedName => PlayerPrefs.GetString(PrefName, DefaultName);

        /// <summary>Nivel 1 = 1.0, cada nivel siguiente +15% de velocidad. El infinito trae la velocidad absoluta en cada tramo.</summary>
        public static float SpeedMultiplier => IsEndless ? 1f : 1f + 0.15f * (Level - 1);

        static int bitsAtLevelStart;

        // El proyecto tiene desactivado el domain reload: hay que limpiar los estáticos a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Changed = null;
            PlayerName = SavedName;
            Level = 1;
            Lives = MaxLives;
            Bits = 0;
            Distance = 0;
            bitsAtLevelStart = 0;
            IsNewRecord = IsNewDistanceRecord = false;
        }

        /// <summary>Limpia el nombre: sin espacios extremos ni caracteres de control, máx. 12, en mayúsculas.</summary>
        public static string SanitizeName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return DefaultName;
            var sb = new StringBuilder();
            foreach (var c in raw.Trim())
            {
                if (char.IsControl(c) || c == '<' || c == '>') continue; // '<' '>' romperían el rich text
                sb.Append(c);
                if (sb.Length == MaxNameLength) break;
            }
            var name = sb.ToString().Trim().ToUpperInvariant();
            return name.Length == 0 ? DefaultName : name;
        }

        public static void StartNewRun(string playerName)
        {
            PlayerName = SanitizeName(playerName);
            PlayerPrefs.SetString(PrefName, PlayerName);
            PlayerPrefs.Save();
            Level = 1;
            Lives = MaxLives;
            Bits = 0;
            Distance = 0;
            bitsAtLevelStart = 0;
            IsNewRecord = IsNewDistanceRecord = false;
            Notify();
        }

        /// <summary>Partida nueva en el nivel elegido en el selector: 3 vidas y bits en 0.</summary>
        public static void StartLevel(int level)
        {
            Level = Mathf.Clamp(level, 1, EndlessLevel);
            Lives = MaxLives;
            Bits = 0;
            Distance = 0;
            bitsAtLevelStart = 0;
            IsNewRecord = IsNewDistanceRecord = false;
            Notify();
        }

        /// <summary>Progreso máximo guardado del nivel (0..100). 100 = llegó a la meta.</summary>
        public static int LevelProgress(int level) => PlayerPrefs.GetInt(PrefProgress + level, 0);

        /// <summary>Guarda el avance (0..1) del nivel actual solo si supera lo guardado. Lo escribe a disco SceneFlow.Load.</summary>
        public static void RecordProgress(float progress)
        {
            int p = Mathf.Clamp(Mathf.FloorToInt(progress * 100f), 0, 100);
            if (p > LevelProgress(Level)) PlayerPrefs.SetInt(PrefProgress + Level, p);
        }

        /// <summary>Todos libres salvo el último, que pide los anteriores al 100%.</summary>
        public static bool IsUnlocked(int level)
        {
            if (level < LevelCount) return true;
            for (int i = 1; i < LevelCount; i++)
                if (LevelProgress(i) < 100) return false;
            return true;
        }

        /// <summary>Reintentar tras Game Over: mismo nivel, 3 vidas, bits como al empezar el nivel.</summary>
        public static void RetryLevel()
        {
            Lives = MaxLives;
            Bits = bitsAtLevelStart;
            Distance = 0;
            IsNewRecord = IsNewDistanceRecord = false;
            Notify();
        }

        /// <summary>HOT RELOAD: +1 vida sin pasar de <see cref="MaxLives"/>. True si sumó.</summary>
        public static bool GainLife()
        {
            if (Lives <= 0 || Lives >= MaxLives) return false;
            Lives++;
            Notify();
            return true;
        }

        /// <returns>Vidas restantes.</returns>
        public static int LoseLife()
        {
            if (Lives > 0)
            {
                Lives--;
                Notify();
            }
            return Lives;
        }

        public static void AddBits(int amount)
        {
            if (amount <= 0) return;
            Bits += amount;
            Cosmetics.AddToWallet(amount);
            if (Bits > BestBits)
            {
                PlayerPrefs.SetInt(PrefBest, Bits);
                IsNewRecord = true;
            }
            Notify();
        }

        /// <summary>Infinito: metros recorridos (solo sube). Superar el récord lo guarda y marca IsNewDistanceRecord.</summary>
        public static void SetDistance(int meters)
        {
            if (meters <= Distance) return;
            Distance = meters;
            if (Distance > BestDistance)
            {
                PlayerPrefs.SetInt(PrefBestDistance, Distance);
                IsNewDistanceRecord = true;
            }
            Notify();
        }

        static void Notify() => Changed?.Invoke();
    }
}
