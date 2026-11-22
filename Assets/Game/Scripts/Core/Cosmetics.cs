using System;
using UnityEngine;

namespace RunDashC
{
    /// <summary>Categoría de cosmético; el orden es también el orden de dibujo (lo de abajo primero).</summary>
    public enum CosmeticSlot { Tenis, Camiseta, Guantes, Sombrero }

    public readonly struct Cosmetic
    {
        public readonly string Id, Nombre;
        public readonly CosmeticSlot Slot;
        public readonly int Precio;

        public Cosmetic(string id, string nombre, CosmeticSlot slot, int precio)
        {
            Id = id;
            Nombre = nombre;
            Slot = slot;
            Precio = precio;
        }

        /// <summary>Icono de la tienda en Resources/Art (lo genera Tools/art/process_sprites.py).</summary>
        public string Art => "cos_" + Id;

        /// <summary>
        /// Capa "puesta" ajustada a camarada.png: mismo tamaño de pixel y mismo centro (lienzo más grande y
        /// simétrico), así va encima de la rata en (0,0) con escala 1.
        /// </summary>
        public string Wear => "wear_" + Id;
    }

    /// <summary>
    /// Tienda del duende: catálogo, cartera persistente de bits y lo que Camarada tiene puesto.
    /// PlayerPrefs: rdc.wallet, rdc.own.&lt;id&gt; (1 = comprado), rdc.eq.&lt;slot&gt; (id equipado o "").
    /// </summary>
    public static class Cosmetics
    {
        const string PrefWallet = "rdc.wallet";
        const string PrefOwn = "rdc.own.";
        const string PrefEquip = "rdc.eq.";

        public static readonly int[] Prices = { 25, 50, 80, 120, 200 };
        public static readonly CosmeticSlot[] Slots = { CosmeticSlot.Sombrero, CosmeticSlot.Camiseta, CosmeticSlot.Guantes, CosmeticSlot.Tenis };

        /// <summary>20 artículos: 5 por categoría, en el mismo orden que la hoja cosmeticos.jpeg.</summary>
        public static readonly Cosmetic[] All = Build(
            (CosmeticSlot.Sombrero, new[] { ("gorra", "GORRA ROJA"), ("copa", "SOMBRERO DE COPA"), ("pirata", "TRICORNIO PIRATA"), ("mago", "GORRO DE MAGO"), ("fiesta", "GORRO DE FIESTA") }),
            (CosmeticSlot.Camiseta, new[] { ("estrella", "CAMISETA ESTRELLA"), ("marinera", "CAMISETA MARINERA"), ("calavera", "CAMISETA CALAVERA"), ("jersey7", "JERSEY #7"), ("glitch", "CAMISETA GLITCH") }),
            (CosmeticSlot.Guantes, new[] { ("box_rojo", "GUANTES DE BOX"), ("box_oro", "GUANTES DE ORO"), ("sin_dedos", "GUANTES SIN DEDOS"), ("blancos", "GUANTES CARTOON"), ("cyber", "GUANTES CYBER") }),
            (CosmeticSlot.Tenis, new[] { ("rojos", "TENIS ROJOS"), ("turbo", "TENIS TURBO"), ("camuflaje", "TENIS CAMUFLAJE"), ("morados", "TENIS MORADOS"), ("alados", "TENIS ALADOS") }));

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Changed = null;

        static Cosmetic[] Build(params (CosmeticSlot slot, (string id, string nombre)[] items)[] rows)
        {
            var list = new System.Collections.Generic.List<Cosmetic>();
            foreach (var (slot, items) in rows)
                for (int i = 0; i < items.Length; i++)
                    list.Add(new Cosmetic(items[i].id, items[i].nombre, slot, Prices[i]));
            return list.ToArray();
        }

        public static int Wallet => PlayerPrefs.GetInt(PrefWallet, 0);

        /// <summary>Lo llama GameSession.AddBits: cada bit recogido también va a la cartera.</summary>
        public static void AddToWallet(int amount)
        {
            if (amount <= 0) return;
            PlayerPrefs.SetInt(PrefWallet, Wallet + amount);
            Changed?.Invoke();
        }

        public static bool Owns(Cosmetic c) => PlayerPrefs.GetInt(PrefOwn + c.Id, 0) == 1;

        public static bool IsEquipped(Cosmetic c) => EquippedId(c.Slot) == c.Id;

        static string EquippedId(CosmeticSlot slot) => PlayerPrefs.GetString(PrefEquip + slot, "");

        /// <summary>Artículo puesto en esa categoría, o null.</summary>
        public static Cosmetic? Equipped(CosmeticSlot slot)
        {
            var id = EquippedId(slot);
            foreach (var c in All)
                if (c.Id == id) return c;
            return null;
        }

        /// <summary>Compra y equipa. False si ya lo tenía o no alcanzan los bits.</summary>
        public static bool TryBuy(Cosmetic c)
        {
            if (Owns(c) || Wallet < c.Precio) return false;
            PlayerPrefs.SetInt(PrefWallet, Wallet - c.Precio);
            PlayerPrefs.SetInt(PrefOwn + c.Id, 1);
            PlayerPrefs.SetString(PrefEquip + c.Slot, c.Id);
            PlayerPrefs.Save(); // gastar bits no debe perderse si el juego se cierra
            Changed?.Invoke();
            return true;
        }

        /// <summary>Equipa o quita un artículo comprado. Devuelve si quedó puesto.</summary>
        public static bool Toggle(Cosmetic c)
        {
            if (!Owns(c)) return false;
            bool on = !IsEquipped(c);
            PlayerPrefs.SetString(PrefEquip + c.Slot, on ? c.Id : "");
            Changed?.Invoke();
            return on;
        }
    }
}
