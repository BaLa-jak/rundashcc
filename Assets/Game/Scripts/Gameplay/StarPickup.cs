using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Estrella oculta 'E' (3 por nivel, las pone Tools/level/place_stars.py): gira sobre su eje y brilla.
    /// Al tocarla se guarda en <see cref="Stars"/> en el acto. Si ya se tenía, sale translúcida y no cuenta otra vez.
    /// </summary>
    public class StarPickup : MonoBehaviour
    {
        const float Size = 1.1f;

        /// <summary>Orden de la estrella en el mapa (lo pone LevelBuilder).</summary>
        public int index;

        Transform spin;
        float phase;
        bool taken, owned;

        void Start()
        {
            owned = Stars.Has(GameSession.Level, index);
            spin = new GameObject("spin").transform;
            spin.SetParent(transform, false);
            var code = ProdSkin.Split(spin, out var prod);
            var sr = WorldKit.AddSprite(code, "estrella", Size, 6);
            if (owned) sr.color = new Color(1f, 1f, 1f, 0.3f);
            if (prod != null)
            {
                var p = WorldKit.AddSprite(prod, "prod_estrella", Size * 0.85f, 6);
                if (owned) p.color = new Color(1f, 1f, 1f, 0.3f);
            }
            if (!owned) WorldKit.AddLight(transform, AsciiTheme.Cyan, 1.2f, 2.2f);
            var c = gameObject.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = 0.4f; // simulate.STAR_R = 0.35: valida con un poco menos para no contar roces dudosos
            phase = transform.position.x * 0.37f;
        }

        void Update()
        {
            float t = Time.time * 2.2f + phase;
            spin.localScale = new Vector3(Mathf.Cos(t), 1f, 1f); // giro sobre su propio eje (vertical)
            spin.localPosition = new Vector3(0f, 0.15f * Mathf.Sin(t * 0.7f), 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (taken || WorldKit.Player(other) == null) return;
            taken = true;
            var pos = transform.position;
            if (owned)
            {
                AsciiParticles.Burst(pos, "*.", AsciiTheme.GreenDim, 5, 2.5f, 0.4f, 0.35f, 0f);
                Destroy(gameObject);
                return;
            }
            Stars.Collect(GameSession.Level, index);
            AudioManager.Play(Sfx.Star);
            AsciiParticles.Burst(pos, "*+01", AsciiTheme.Cyan, 16, 6f, 0.7f, 0.5f, 4f);
            AsciiParticles.Popup(pos + Vector3.up * 0.9f, $"* estrella {Stars.CountIn(GameSession.Level)}/{Stars.PerLevel}", AsciiTheme.Yellow, 0.75f);
            Destroy(gameObject);
        }
    }
}
