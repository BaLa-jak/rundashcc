using System;
using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Estrellas ocultas ('E' en los niveles, 3 por nivel). Se guardan al recogerlas en PlayerPrefs rdc.star&lt;N&gt;
    /// (máscara de bits; índice = orden de la estrella por columna en el mapa). El total desbloquea <see cref="Abilities"/>.
    /// </summary>
    public static class Stars
    {
        public const int PerLevel = 3;
        public static int Max => PerLevel * GameSession.LevelCount;

        const string Pref = "rdc.star";

        /// <summary>Cambió alguna estrella: (nivel, índice, total anterior).</summary>
        public static event Action<int, int, int> Collected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Collected = null;

        static int Mask(int level) => PlayerPrefs.GetInt(Pref + level, 0);

        public static bool Has(int level, int index) => (Mask(level) & (1 << index)) != 0;

        public static int CountIn(int level)
        {
            int n = 0;
            for (int m = Mask(level); m != 0; m &= m - 1) n++;
            return n;
        }

        public static int Total
        {
            get
            {
                int n = 0;
                for (int l = 1; l <= GameSession.LevelCount; l++) n += CountIn(l);
                return n;
            }
        }

        /// <summary>Guarda la estrella (y escribe a disco: es un logro difícil, no debe perderse).</summary>
        public static void Collect(int level, int index)
        {
            if (Has(level, index)) return;
            int before = Total;
            PlayerPrefs.SetInt(Pref + level, Mask(level) | (1 << index));
            PlayerPrefs.Save();
            Collected?.Invoke(level, index, before);
        }
    }
}
