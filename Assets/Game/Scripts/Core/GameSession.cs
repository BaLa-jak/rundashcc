using System;
using System.Text;
using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Estado de la partida que sobrevive entre escenas: jugador, nivel, vidas y bits.
    /// Cualquier cambio dispara <see cref="Changed"/> (el HUD y el RunManager lo escuchan).
    /// </summary>
    public static class GameSession
    {
        public const int MaxLives = 3;
        public const int MaxNameLength = 12;
        public const string DefaultName = "SYSADMIN";

        const string PrefName = "rdc.player";
        const string PrefBest = "rdc.best";

        public static event Action Changed;

        public static string PlayerName { get; private set; } = DefaultName;
        public static int Level { get; private set; } = 1;
        public static int Lives { get; private set; } = MaxLives;
        public static int Bits { get; private set; }
        public static bool IsNewRecord { get; private set; }
        public static int BestBits => PlayerPrefs.GetInt(PrefBest, 0);
        public static string SavedName => PlayerPrefs.GetString(PrefName, DefaultName);

        /// <summary>Nivel 1 = 1.0, cada nivel siguiente +15% de velocidad.</summary>
        public static float SpeedMultiplier => 1f + 0.15f * (Level - 1);

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
            bitsAtLevelStart = 0;
            IsNewRecord = false;
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
            bitsAtLevelStart = 0;
            IsNewRecord = false;
            Notify();
        }

        /// <summary>Reintentar tras Game Over: mismo nivel, 3 vidas, bits como al empezar el nivel.</summary>
        public static void RetryLevel()
        {
            Lives = MaxLives;
            Bits = bitsAtLevelStart;
            IsNewRecord = false;
            Notify();
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
            if (Bits > BestBits)
            {
                PlayerPrefs.SetInt(PrefBest, Bits);
                IsNewRecord = true;
            }
            Notify();
        }

        public static void NextLevel()
        {
            Level++;
            bitsAtLevelStart = Bits;
            Notify();
        }

        static void Notify() => Changed?.Invoke();
    }
}
