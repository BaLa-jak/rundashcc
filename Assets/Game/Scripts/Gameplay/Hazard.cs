using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Base de los peligros: un trigger que golpea a Camarada una vez por contacto
    /// (la invulnerabilidad del RunManager evita golpes repetidos).
    /// Los hijos arman su visual en Start: así LevelBuilder alcanza a configurar sus campos tras AddComponent.
    /// </summary>
    public abstract class Hazard : MonoBehaviour
    {
        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            var player = WorldKit.Player(other);
            if (player != null) OnPlayer(player);
        }

        protected virtual void OnPlayer(CamaradaController player)
        {
            var run = RunManager.Instance;
            if (run != null) run.HitPlayer(false);
        }

        /// <summary>Rigidbody cinemático para los peligros que se mueven (mover un collider estático es caro).</summary>
        protected Rigidbody2D AddKinematicBody()
        {
            var body = gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            return body;
        }

        protected T AddTrigger<T>() where T : Collider2D
        {
            var c = gameObject.AddComponent<T>();
            c.isTrigger = true;
            return c;
        }
    }
}
