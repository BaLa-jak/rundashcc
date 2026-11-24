using System;
using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Estado de los portales en la partida: mundo al revés (portal azul 'V', solo visual: CameraRig voltea la
    /// cámara) y dimensión "producción" (portal morado 'D': ProdSkin cambia el arte durante
    /// <see cref="ProductionSeconds"/>). RunManager hace las transiciones; aquí solo vive el estado.
    /// </summary>
    public static class WorldMode
    {
        /// <summary>= PROD_SECONDS de generate_levels.py (la meta queda fuera de ese tramo).</summary>
        public const float ProductionSeconds = 10f;

        /// <summary>El nivel tiene portales 'D': los visuales arman también su piel "prod". Lo pone LevelBuilder.</summary>
        public static bool HasProduction;
        public static bool Flipped { get; private set; }
        public static bool Production { get; private set; }
        public static float ProductionLeft { get; private set; }

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Changed = null;
            HasProduction = false;
            Reset();
        }

        /// <summary>Mundo normal, sin avisar (se llama al empezar y al salir de la partida).</summary>
        public static void Reset()
        {
            Flipped = Production = false;
            ProductionLeft = 0f;
        }

        public static void SetFlipped(bool on)
        {
            if (Flipped == on) return;
            Flipped = on;
            Changed?.Invoke();
        }

        /// <summary>Entrar (o volver a entrar: reinicia el tiempo) o salir de producción.</summary>
        public static void SetProduction(bool on)
        {
            ProductionLeft = on ? ProductionSeconds : 0f;
            if (Production == on) return;
            Production = on;
            Changed?.Invoke();
        }

        /// <summary>Descuenta tiempo de producción; true solo el frame en que se acaba (quien llama hace el regreso).</summary>
        public static bool Tick(float dt)
        {
            if (!Production || ProductionLeft <= 0f) return false;
            ProductionLeft -= dt;
            return ProductionLeft <= 0f;
        }
    }
}
