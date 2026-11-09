using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Partículas de texto: un anillo de TextMesh reciclados (si se llena, se reusa la más vieja).
    /// Polvo, chispas, explosiones y popups (+1, bug fixed!). Se crea al primer uso y vive con la escena.
    /// </summary>
    public class AsciiParticles : MonoBehaviour
    {
        const int Capacity = 192;

        struct Particle
        {
            public Transform tr;
            public TextMesh tm;
            public Color color;
            public Vector2 vel;
            public float gravity, life, age, spin, size;
            public bool unscaled, alive;
        }

        static AsciiParticles instance;

        readonly Particle[] particles = new Particle[Capacity];
        int next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        /// <summary>Crea el pool ahora (evita el tirón del primer uso).</summary>
        public static AsciiParticles Warm()
        {
            if (instance == null) instance = new GameObject("AsciiParticles").AddComponent<AsciiParticles>();
            return instance;
        }

        void Awake()
        {
            for (int i = 0; i < Capacity; i++)
            {
                var tm = WorldKit.AddText(transform, "", Vector2.zero, Color.white, 1f, TextAnchor.MiddleCenter, 30);
                tm.gameObject.SetActive(false);
                particles[i].tm = tm;
                particles[i].tr = tm.transform;
            }
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        /// <param name="unscaled">Usa tiempo real (la explosión de muerte corre durante el slow-mo).</param>
        public static void Emit(Vector3 pos, string text, Color color, Vector2 velocity, float life,
            float size = 0.5f, float gravity = 0f, float spin = 0f, bool unscaled = false)
        {
            var self = Warm();
            ref var p = ref self.particles[self.next];
            self.next = (self.next + 1) % Capacity;
            p.color = color;
            p.vel = velocity;
            p.life = Mathf.Max(0.01f, life);
            p.age = 0f;
            p.gravity = gravity;
            p.spin = spin;
            p.size = size;
            p.unscaled = unscaled;
            p.alive = true;
            p.tm.text = text;
            p.tm.color = color;
            p.tr.SetPositionAndRotation(new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
            p.tr.localScale = Vector3.one * size;
            p.tm.gameObject.SetActive(true);
        }

        /// <summary>Chispas: <paramref name="count"/> caracteres al azar de <paramref name="chars"/> en todas direcciones.</summary>
        public static void Burst(Vector3 pos, string chars, Color color, int count, float speed,
            float life = 0.6f, float size = 0.45f, float gravity = 12f, bool unscaled = false)
        {
            for (int i = 0; i < count; i++)
            {
                var v = Random.insideUnitCircle * speed + Vector2.up * (speed * 0.4f);
                Emit(pos, chars[Random.Range(0, chars.Length)].ToString(), color, v,
                    life * Random.Range(0.7f, 1.2f), size * Random.Range(0.8f, 1.2f), gravity, Random.Range(-400f, 400f), unscaled);
            }
        }

        /// <summary>Texto que sube y se desvanece.</summary>
        public static void Popup(Vector3 pos, string text, Color color, float size = 0.6f) =>
            Emit(pos, text, color, new Vector2(0f, 1.8f), 0.9f, size);

        void Update()
        {
            float dt = Time.deltaTime, udt = Time.unscaledDeltaTime;
            for (int i = 0; i < Capacity; i++)
            {
                ref var p = ref particles[i];
                if (!p.alive) continue;
                float d = p.unscaled ? udt : dt;
                p.age += d;
                if (p.age >= p.life)
                {
                    p.alive = false;
                    p.tm.gameObject.SetActive(false);
                    continue;
                }
                p.vel.y -= p.gravity * d;
                p.tr.position += (Vector3)(p.vel * d);
                if (p.spin != 0f) p.tr.Rotate(0f, 0f, p.spin * d);
                // Pop al nacer y fade en el último 40% de vida.
                p.tr.localScale = Vector3.one * (p.size * (1f + 0.4f * Mathf.Max(0f, 1f - p.age * 8f)));
                float k = p.age / p.life;
                var c = p.color;
                if (k > 0.6f) c.a *= 1f - (k - 0.6f) / 0.4f;
                p.tm.color = c;
            }
        }
    }
}
