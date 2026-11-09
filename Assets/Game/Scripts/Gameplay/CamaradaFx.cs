using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RunDashC
{
    /// <summary>
    /// Animación procedural de Camarada sobre el hijo visual (no toca el Rigidbody): bob al correr,
    /// squash/stretch con resorte, inclinación según vy, giro de 360° con estela ASCII en el doble salto,
    /// polvo en los pies, parpadeo rojo con jitter en invulnerabilidad y explosión de caracteres al morir.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CamaradaFx : MonoBehaviour
    {
        const float Width = 1.5f;
        const float SpinSeconds = 0.38f;
        const float FeetSink = 0.04f; // margen transparente del PNG bajo las patas
        const string DustChars = ".,'`";
        const string TrailChars = "@%&#";
        const string DeathChars = "01{};#/*";

        SpriteRenderer sprite;
        CamaradaController body;
        Light2D glow;
        float baseScale = 1f, halfHeight = 0.37f;
        float squash, squashVel, bobPhase, spinLeft, dustTimer, trailTimer;
        bool exploded;

        void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            body = GetComponentInParent<CamaradaController>();
            if (sprite.sprite == null) sprite.sprite = WorldKit.LoadSprite("camarada");
            if (sprite.sprite != null)
            {
                baseScale = Width / sprite.sprite.bounds.size.x;
                halfHeight = sprite.sprite.bounds.size.y * baseScale / 2f;
            }
            sprite.sortingOrder = 20;
            if (transform.parent != null) glow = WorldKit.AddLight(transform.parent, AsciiTheme.White, 0.35f, 2.5f);
        }

        void Update()
        {
            if (exploded || body == null) return;
            float dt = Time.deltaTime;
            var run = RunManager.Instance;
            var state = run != null ? run.State : RunState.Countdown;
            var v = body.Velocity;

            // Resorte del squash (positivo = aplastado, negativo = estirado).
            squashVel += (-220f * squash - 16f * squashVel) * dt;
            squash += squashVel * dt;
            float sx = 1f + squash, sy = 1f - squash, bob = 0f, tilt = 0f;

            if (state == RunState.Countdown)
            {
                float b = Mathf.Sin(Time.time * 5f);
                sx *= 1f - 0.03f * b;
                sy *= 1f + 0.04f * b;
            }
            else if (body.Grounded && v.x > 0.1f)
            {
                bobPhase += dt * v.x * 0.8f; // frecuencia proporcional a la velocidad
                bob = 0.09f * Mathf.Abs(Mathf.Sin(bobPhase * Mathf.PI));
                tilt = 3f * Mathf.Sin(bobPhase * Mathf.PI * 2f);
                dustTimer -= dt;
                if (dustTimer <= 0f)
                {
                    dustTimer = 0.07f;
                    Dust(new Vector2(-Random.Range(0.6f, 2f), Random.Range(0.4f, 1.4f)));
                }
            }
            else if (!body.Grounded)
            {
                tilt = Mathf.Clamp(v.y * 2.2f, -30f, 30f);
            }

            if (spinLeft > 0f)
            {
                spinLeft -= dt;
                float p = 1f - Mathf.Clamp01(spinLeft / SpinSeconds);
                tilt -= 360f * (1f - (1f - p) * (1f - p)); // ease-out, hacia adelante
                trailTimer -= dt;
                if (trailTimer <= 0f)
                {
                    trailTimer = 0.03f;
                    AsciiParticles.Emit(transform.position, TrailChars[Random.Range(0, TrailChars.Length)].ToString(),
                        AsciiTheme.WithAlpha(AsciiTheme.White, 0.5f), Vector2.zero, 0.25f, 0.6f);
                }
            }

            var jitter = Vector3.zero;
            bool invulnerable = run != null && run.IsInvulnerable && state != RunState.Dying;
            if (invulnerable)
            {
                sprite.enabled = Mathf.Repeat(Time.time, 0.12f) < 0.08f;
                sprite.color = Color.Lerp(Color.white, AsciiTheme.Red, 0.6f);
                if (Random.value < 0.25f) jitter = Random.insideUnitCircle * 0.07f;
            }
            else
            {
                sprite.enabled = true;
                sprite.color = Color.white;
            }

            // Pies fijos en el borde del collider aunque haya squash; rota sobre el centro.
            transform.localPosition = new Vector3(0f, -CamaradaController.HalfHeight - FeetSink + halfHeight * sy + bob, 0f) + jitter;
            transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            transform.localScale = new Vector3(baseScale * sx, baseScale * sy, 1f);
        }

        Vector3 Feet => body.transform.position + new Vector3(-0.35f, -CamaradaController.HalfHeight + 0.05f, 0f);

        void Dust(Vector2 velocity) =>
            AsciiParticles.Emit(Feet, DustChars[Random.Range(0, DustChars.Length)].ToString(),
                AsciiTheme.WithAlpha(AsciiTheme.White, 0.6f), velocity, 0.35f, 0.4f, 3f);

        public void OnJump()
        {
            squash = -0.25f;
            squashVel = 0f;
            for (int i = 0; i < 3; i++) Dust(new Vector2(Random.Range(-1.5f, 0.5f), Random.Range(0.2f, 1f)));
        }

        public void OnDoubleJump()
        {
            squash = -0.15f;
            squashVel = 0f;
            spinLeft = SpinSeconds;
            AsciiParticles.Burst(Feet, "*+", AsciiTheme.Cyan, 5, 3f, 0.35f, 0.4f, 4f);
        }

        public void OnLand(float impactSpeed)
        {
            squash = Mathf.Clamp(impactSpeed * 0.018f, 0.08f, 0.35f);
            squashVel = 0f;
            spinLeft = 0f;
            int n = impactSpeed > 6f ? 5 : 2;
            for (int i = 0; i < n; i++) Dust(new Vector2(Random.Range(-2f, 2f), Random.Range(0.3f, 1.2f)));
        }

        public void OnRespawn()
        {
            squash = -0.3f;
            squashVel = 0f;
            spinLeft = 0f;
            if (body != null) AsciiParticles.Burst(body.transform.position, "01#", AsciiTheme.Cyan, 8, 3f, 0.4f, 0.4f, 0f);
        }

        public void Explode()
        {
            exploded = true;
            sprite.enabled = false;
            if (glow != null) glow.enabled = false;
            var p = transform.position;
            AsciiParticles.Burst(p, DeathChars, AsciiTheme.White, 26, 9f, 1.3f, 0.5f, 10f, true);
            AsciiParticles.Burst(p, DeathChars, AsciiTheme.Red, 18, 7f, 1.3f, 0.5f, 10f, true);
        }
    }
}
