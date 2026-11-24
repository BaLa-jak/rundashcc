using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RunDashC
{
    public enum PortalKind { Flip, Production }

    /// <summary>
    /// Portal 'V' (azul: mundo al revés) o 'D' (morado: dimensión producción). Compuerta vista de lado, apoyada
    /// en el suelo: el aro de atrás va detrás de Camarada y el borde derecho delante, así parece que lo cruza.
    /// Trigger de toda la columna: saltar por encima también cuenta.
    /// </summary>
    public class PortalGate : MonoBehaviour
    {
        const float Height = 3.4f;

        public PortalKind kind;

        Transform visual;
        Light2D glow;
        float phase;

        void Start()
        {
            string art = kind == PortalKind.Flip ? "portal_azul" : "portal_morado";
            var color = kind == PortalKind.Flip ? AsciiTheme.Cyan : AsciiTheme.Magenta;
            visual = new GameObject("visual").transform; // pivote en el suelo: el pulso no lo despega
            visual.SetParent(transform, false);
            visual.localPosition = new Vector3(0f, -0.5f, 0f);
            AddLayer(art, 15);           // Camarada = 20
            AddLayer(art + "_frente", 25);
            glow = WorldKit.AddLight(visual, color, 1.3f, 3.6f);
            glow.transform.localPosition = new Vector3(0f, Height / 2f, 0f);

            var box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.6f, 40f);
            phase = transform.position.x * 0.31f;
        }

        void AddLayer(string art, int order)
        {
            var sr = WorldKit.AddSprite(visual, art, 1f, order);
            if (sr.sprite == null) return;
            sr.transform.localScale = Vector3.one * (Height / sr.sprite.bounds.size.y);
            sr.transform.localPosition = new Vector3(0f, Height / 2f, 0f);
        }

        void Update()
        {
            float t = Time.time * 4f + phase;
            visual.localScale = new Vector3(1f + 0.06f * Mathf.Sin(t), 1f + 0.03f * Mathf.Sin(t * 0.7f), 1f);
            glow.intensity = 1.3f + 0.5f * Mathf.Sin(t * 1.5f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (WorldKit.Player(other) == null) return;
            var color = kind == PortalKind.Flip ? AsciiTheme.Cyan : AsciiTheme.Magenta;
            AsciiParticles.Burst(transform.position + Vector3.up, "01{}#*%", color, 16, 6f, 0.6f, 0.45f, 0f);
            var run = RunManager.Instance;
            if (run != null) run.EnterPortal(kind);
        }
    }
}
