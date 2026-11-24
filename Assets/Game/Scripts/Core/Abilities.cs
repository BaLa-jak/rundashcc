namespace RunDashC
{
    public enum Ability { TripleSalto, Dash, VidaExtra, Corona, Iman, TryCatch, Overclock, HotReload, SudoDash, Root }

    /// <summary>
    /// Habilidades permanentes de Camarada: se desbloquean con el total de <see cref="Stars"/> (no hay estado propio).
    /// Icono Resources/Art/hab_&lt;Id&gt; (Tools/art/process_sprites.py → HAB_TPL, mismo orden).
    /// </summary>
    public static class Abilities
    {
        public readonly struct Info
        {
            public readonly Ability Kind;
            public readonly int Stars;
            public readonly string Id, Nombre, Desc;

            public Info(Ability kind, int stars, string id, string nombre, string desc)
            {
                Kind = kind;
                Stars = stars;
                Id = id;
                Nombre = nombre;
                Desc = desc;
            }

            public string Icon => "hab_" + Id;
        }

        /// <summary>Ordenadas por estrellas necesarias.</summary>
        public static readonly Info[] All =
        {
            new Info(Ability.TripleSalto, 1, "triple_salto", "TRIPLE SALTO", "un salto más en el aire"),
            new Info(Ability.Dash, 2, "dash", "DASH", "mantén [ESPACIO]: embestida, inmortal mientras dura"),
            new Info(Ability.VidaExtra, 3, "vida_extra", "VIDA EXTRA", "empiezas cada nivel con 4 vidas"),
            new Info(Ability.Corona, 4, "corona", "CORONA ESTELAR", "cosmético exclusivo: póntela en HABILIDADES"),
            new Info(Ability.Iman, 6, "iman", "GARBAGE COLLECTOR", "los bits cercanos vuelan hacia ti"),
            new Info(Ability.TryCatch, 8, "try_catch", "TRY / CATCH", "escudo: absorbe 1 golpe, se recarga en cada checkpoint"),
            new Info(Ability.Overclock, 11, "overclock", "OVERCLOCK", "cada bit vale 2"),
            new Info(Ability.HotReload, 14, "hot_reload", "HOT RELOAD", "cada checkpoint te devuelve 1 vida"),
            new Info(Ability.SudoDash, 17, "sudo_dash", "SUDO DASH", "dash más largo, recarga más rápida y aplasta bugs"),
            new Info(Ability.Root, 21, "root", "ROOT", "los bugs ya no te dañan: tocarlos los aplasta"),
        };

        public static Info Get(Ability a) => All[(int)a];

        public static bool Has(Ability a) => global::RunDashC.Stars.Total >= Get(a).Stars;

        public static int UnlockedCount(int stars)
        {
            int n = 0;
            foreach (var a in All)
                if (stars >= a.Stars) n++;
            return n;
        }
    }
}
