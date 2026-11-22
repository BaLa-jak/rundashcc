using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RunDashC
{
    /// <summary>
    /// Animación procedural de Camarada sobre el hijo visual (no toca el Rigidbody): bob al correr,
    /// squash/stretch con resorte, inclinación según vy, giro de 360° con estela ASCII en el doble salto,
    /// polvo en los pies, parpadeo rojo con jitter en invulnerabilidad, explosión de caracteres al morir
    /// y baile de victoria en la meta (de pie sobre las patas traseras, meciéndose al ritmo).
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
        // Baile: balanceo lado a lado a 1 Hz = 2 beats/s (≈120 BPM), de pie sobre las patas traseras.
        const float DanceUpright = 70f, DanceSway = 14f, DanceHop = 0.12f, DanceRamp = 0.25f;
        const float DanceRear = Width * 0.3f; // de la pata trasera al centro del sprite
        const string PawChars = "*+~o";

        SpriteRenderer sprite;
        CamaradaController body;
        Light2D glow;
        readonly System.Collections.Generic.List<SpriteRenderer> outfit = new System.Collections.Generic.List<SpriteRenderer>();
        float baseScale = 1f, halfHeight = 0.37f;
        float squash, squashVel, bobPhase, spinLeft, dustTimer, trailTimer;
        float danceWeight, danceTime;
        int danceBeat;
        bool exploded, dancing;

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
            DressUp();
            if (transform.parent != null) glow = WorldKit.AddLight(transform.parent, AsciiTheme.White, 0.35f, 2.5f);
        }

        /// <summary>
        /// Cosméticos equipados en la tienda: capas wear_* centradas sobre la rata (hijas del visual, así heredan
        /// squash, giro y baile; con flipX se reflejan sobre el mismo centro).
        /// </summary>
        void DressUp()
        {
            foreach (var slot in Cosmetics.Slots)
            {
                if (!(Cosmetics.Equipped(slot) is Cosmetic c)) continue;
                var art = WorldKit.LoadSprite(c.Wear);
                if (art == null) continue;
                var go = new GameObject(c.Wear);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = art;
                sr.sortingOrder = 21 + (int)slot;
                outfit.Add(sr);
            }
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

            // Baile: mezcla con rampa suave para que no "salte" a parado ni al volver a 4 patas.
            danceWeight = Mathf.MoveTowards(danceWeight, dancing ? 1f : 0f, dt / DanceRamp);
            float danceW = Mathf.SmoothStep(0f, 1f, danceWeight), dir = 1f;
            if (danceWeight > 0f)
            {
                danceTime += dt;
                dir = Mathf.Repeat(danceTime, 2f) < 1f ? 1f : -1f; // voltea cada 2 beats, en lo alto del brinco
                float phase = danceTime * Mathf.PI * 2f;
                tilt = Mathf.Lerp(tilt, DanceUpright * dir + DanceSway * Mathf.Sin(phase), danceW);
                bob = Mathf.Lerp(bob, DanceHop * Mathf.Abs(Mathf.Cos(phase)), danceW);
                int beat = Mathf.FloorToInt((danceTime + 0.25f) * 2f); // pisada en cada extremo del balanceo
                if (beat != danceBeat && dancing)
                {
                    danceBeat = beat;
                    squash = 0.18f;
                    squashVel = 0f;
                    Paws(dir, beat % 2 == 0 ? 1f : -1f);
                }
            }
            sprite.flipX = danceWeight > 0f && dir < 0f;

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

            foreach (var sr in outfit)
            {
                sr.enabled = sprite.enabled;
                sr.color = sprite.color;
                sr.flipX = sprite.flipX;
            }

            // Pies fijos en el borde del collider aunque haya squash; rota sobre el centro.
            float feet = -CamaradaController.HalfHeight - FeetSink + bob;
            var rot = Quaternion.Euler(0f, 0f, tilt);
            var pos = new Vector3(0f, feet + halfHeight * sy, 0f);
            // Bailando rota sobre la pata trasera (puesta en el centro del cuerpo) para pararse en 2 patas.
            if (danceWeight > 0f) pos = new Vector3(0f, feet, 0f) + rot * new Vector3(DanceRear * dir * danceW, halfHeight * sy, 0f);
            transform.localPosition = pos + jitter;
            transform.localRotation = rot;
            transform.localScale = new Vector3(baseScale * sx, baseScale * sy, 1f);
        }

        Vector3 Feet => body.transform.position + new Vector3(-0.35f, -CamaradaController.HalfHeight + 0.05f, 0f);

        void Dust(Vector2 velocity) =>
            AsciiParticles.Emit(Feet, DustChars[Random.Range(0, DustChars.Length)].ToString(),
                AsciiTheme.WithAlpha(AsciiTheme.White, 0.6f), velocity, 0.35f, 0.4f, 3f);

        /// <summary>Baile de victoria (lo controla RunManager al llegar a la meta).</summary>
        public void StartDance()
        {
            dancing = true;
            danceTime = 0f;
            danceBeat = 0;
        }

        public void StopDance() => dancing = false;

        /// <summary>"Patitas" al ritmo: chispas ASCII desde el pecho, alternando de lado.</summary>
        void Paws(float dir, float side)
        {
            var chest = transform.position + transform.rotation * new Vector3(0.35f * dir, 0f, 0f);
            for (int i = 0; i < 2; i++)
                AsciiParticles.Emit(chest, PawChars[Random.Range(0, PawChars.Length)].ToString(),
                    i == 0 ? AsciiTheme.Cyan : AsciiTheme.Green,
                    new Vector2(side * Random.Range(1f, 2.2f), Random.Range(1f, 2.2f)), 0.5f, 0.4f, 2f);
        }

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
            foreach (var sr in outfit) sr.enabled = false;
            if (glow != null) glow.enabled = false;
            var p = transform.position;
            AsciiParticles.Burst(p, DeathChars, AsciiTheme.White, 26, 9f, 1.3f, 0.5f, 10f, true);
            AsciiParticles.Burst(p, DeathChars, AsciiTheme.Red, 18, 7f, 1.3f, 0.5f, 10f, true);
        }
    }
}
