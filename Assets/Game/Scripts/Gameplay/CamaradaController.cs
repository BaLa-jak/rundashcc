using UnityEngine;
using UnityEngine.InputSystem;

namespace RunDashC
{
    /// <summary>
    /// Camarada corre sola (auto-run) y el jugador solo salta: buffer, coyote time, salto variable y un doble salto.
    /// Habilidades (estrellas): TRIPLE SALTO = otro salto en el aire; DASH = mantener el botón <see cref="DashHold"/> s
    /// (justo pasado el ápice del salto completo, 0.36 s, así no se come el salto largo).
    /// El suelo y las paredes se detectan por las normales de contacto (sin layers).
    /// La física se configura en Awake (el PhysicsMaterial2D no se puede guardar en la escena sin asset).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class CamaradaController : MonoBehaviour
    {
        const float MinJumpTimeBeforeCut = 0.08f; // un toque corto da un salto bajo pero útil (~1.4 u)
        const float RespawnHold = 0.4f;           // al reaparecer espera un instante para ubicarse
        const float StuckSeconds = 0.2f;          // si no avanza, cuenta como pared (esquinas raras)
        const float LandSfxSpeed = 6f;
        public const float DashHold = 0.4f;
        const float DashSpeedFactor = 2.2f;
        const float DashSeconds = 0.3f, DashCooldown = 1f;
        const float SudoDashSeconds = 0.45f, SudoDashCooldown = 0.5f;

        /// <summary>Animación procedural (hijo "Visual"). La cablea GameSceneBuilder.</summary>
        public CamaradaFx fx;
        /// <summary>Velocidad base del nivel (LevelData.Speed). La pone RunManager.</summary>
        public float levelSpeed = RunTuning.RunSpeed;

        public Rigidbody2D Body { get; private set; }
        public BoxCollider2D Box { get; private set; }
        public bool Grounded { get; private set; }
        public Vector2 Velocity => Body.linearVelocity;
        public float FeetY => Box.bounds.min.y;
        public float RunSpeed => levelSpeed * GameSession.SpeedMultiplier;
        /// <summary>Saltos en el aire disponibles al despegar: 1, o 2 con TRIPLE SALTO.</summary>
        public int AirJumpsMax { get; private set; } = 1;
        /// <summary>Embestida en curso: horizontal, sin gravedad e inmortal (RunManager.IsInvulnerable).</summary>
        public bool Dashing => Time.time < dashUntil;
        /// <summary>Para el HUD: desbloqueado, recargado y sin gastar en este salto.</summary>
        public bool DashReady => hasDash && dashCharge && Time.time >= dashReadyAt;

        /// <summary>Distancia del centro del cuerpo a los pies (collider 0.8x0.5 + edgeRadius 0.05 = 0.9x0.6).</summary>
        public const float HalfHeight = 0.3f;

        readonly ContactPoint2D[] contacts = new ContactPoint2D[16];
        InputAction jumpAction;
        float bufferTimer;
        bool freshPress;
        float lastGroundedTime = -999f;
        bool jumping, cutDone, wasGrounded;
        float jumpTime;
        int airJumps;
        float holdUntil;
        float stuckTimer, lastX, prevVy;
        bool commandedRun;
        bool hasDash, hasSudoDash, holdArmed, dashCharge = true;
        float pressTime, dashUntil = -1f, dashReadyAt;

        void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Box = GetComponent<BoxCollider2D>();
            Body.bodyType = RigidbodyType2D.Dynamic;
            Body.gravityScale = RunTuning.GravityScale;
            Body.freezeRotation = true;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.sharedMaterial = new PhysicsMaterial2D("CamaradaSinFriccion") { friction = 0f, bounciness = 0f };
            Box.size = new Vector2(0.8f, 0.5f);
            Box.edgeRadius = 0.05f;
            Box.offset = Vector2.zero;
            if (InputSystem.actions != null) jumpAction = InputSystem.actions.FindAction("Player/Jump");
            if (jumpAction == null) Debug.LogWarning("[RunDashC] No encuentro la acción Player/Jump");
            RefreshAbilities();
        }

        void OnEnable() => Stars.Collected += OnStarCollected;
        void OnDisable() => Stars.Collected -= OnStarCollected;
        void OnStarCollected(int level, int index, int before) => RefreshAbilities();

        /// <summary>Las habilidades se leen una vez (PlayerPrefs) y otra al recoger una estrella: sirven al instante.</summary>
        void RefreshAbilities()
        {
            AirJumpsMax = Abilities.Has(Ability.TripleSalto) ? 2 : 1;
            hasDash = Abilities.Has(Ability.Dash);
            hasSudoDash = Abilities.Has(Ability.SudoDash);
        }

        static RunState State => RunManager.Instance != null ? RunManager.Instance.State : RunState.Countdown;

        void Update()
        {
            if (jumpAction == null || SceneFlow.IsPaused || Time.frameCount == SceneFlow.ResumedFrame || State != RunState.Running) return;
            if (jumpAction.WasPressedThisFrame())
            {
                bufferTimer = RunTuning.JumpBufferTime;
                freshPress = true;
                pressTime = Time.time;
                holdArmed = true;
            }
        }

        void FixedUpdate()
        {
            var state = State;
            if (state == RunState.Dying) return;
            float dt = Time.fixedDeltaTime;

            ReadContacts(out bool ground, out bool wall);
            var v = Body.linearVelocity;
            Grounded = ground && v.y < 0.5f;

            if (Grounded)
            {
                if (!wasGrounded && prevVy < -2f)
                {
                    if (prevVy < -LandSfxSpeed) AudioManager.Play(Sfx.Land, 0.1f, 0.7f);
                    if (fx != null) fx.OnLand(-prevVy);
                }
                lastGroundedTime = Time.time;
                airJumps = AirJumpsMax;
                jumping = false;
                dashCharge = true;
            }
            wasGrounded = Grounded;

            if (state == RunState.Running)
            {
                bool stuck = commandedRun && Body.position.x - lastX < RunSpeed * dt * 0.25f;
                stuckTimer = stuck ? stuckTimer + dt : 0f;
                if (wall || stuckTimer > StuckSeconds || Body.position.y < RunTuning.FallDeathY)
                {
                    stuckTimer = 0f;
                    RunManager.Instance.HitPlayer(true); // reaparece en el checkpoint (o explota si era la última vida)
                    return;
                }

                // DASH: el mismo toque, sostenido. Soltar antes lo cancela.
                if (holdArmed && !jumpAction.IsPressed()) holdArmed = false;
                if (holdArmed && Time.time - pressTime >= DashHold)
                {
                    holdArmed = false;
                    StartDash();
                }

                bool coyote = !jumping && Time.time - lastGroundedTime <= RunTuning.CoyoteTime;
                if (Dashing) v.y = 0f; // durante el dash no se salta: el toque queda en el buffer
                else if (bufferTimer > 0f && (Grounded || coyote)) Jump(ref v, RunTuning.JumpVelocity, false);
                else if (freshPress && airJumps > 0)
                {
                    airJumps--;
                    Jump(ref v, RunTuning.DoubleJumpVelocity, true);
                }

                // Salto variable: soltar mientras sube recorta la velocidad.
                if (jumping && !cutDone && v.y > 0f && Time.time - jumpTime >= MinJumpTimeBeforeCut && !jumpAction.IsPressed())
                {
                    v.y *= RunTuning.JumpCutMultiplier;
                    cutDone = true;
                }
            }
            freshPress = false;
            bufferTimer -= dt;

            commandedRun = state == RunState.Running && Time.time >= holdUntil; // en la meta frena para bailar
            bool dashing = Dashing && commandedRun;
            Body.gravityScale = dashing ? 0f : RunTuning.GravityScale;
            v.x = commandedRun ? RunSpeed * (dashing ? DashSpeedFactor : 1f) : 0f;
            Body.linearVelocity = v;
            prevVy = v.y;
            lastX = Body.position.x;
        }

        /// <summary>Embestida hacia adelante (DASH). Una por salto, con recarga. False si no se puede ahora.</summary>
        public bool StartDash()
        {
            if (!DashReady || State != RunState.Running || Time.time < holdUntil) return false;
            dashCharge = false;
            dashUntil = Time.time + (hasSudoDash ? SudoDashSeconds : DashSeconds);
            dashReadyAt = dashUntil + (hasSudoDash ? SudoDashCooldown : DashCooldown);
            AudioManager.Play(Sfx.DoubleJump, 0.1f, 0.6f);
            if (fx != null) fx.OnDash();
            return true;
        }

        void ReadContacts(out bool ground, out bool wall)
        {
            ground = wall = false;
            float feet = FeetY;
            int n = Body.GetContacts(contacts);
            for (int i = 0; i < n; i++)
            {
                var c = contacts[i];
                if (c.collider.isTrigger || c.otherCollider.isTrigger) continue;
                // Las normales apuntan hacia Camarada: piso = arriba; pared de frente = hacia la izquierda.
                if (c.normal.y > 0.6f) ground = true;
                else if (c.normal.x < -0.7f && c.point.y > feet + 0.15f) wall = true; // no cuenta rozar una esquina con los pies
            }
        }

        void Jump(ref Vector2 v, float speed, bool air)
        {
            v.y = speed;
            jumping = true;
            cutDone = false;
            jumpTime = Time.time;
            bufferTimer = 0f;
            lastGroundedTime = -999f;
            Grounded = false;
            AudioManager.Play(air ? Sfx.DoubleJump : Sfx.Jump, 0.06f);
            if (fx == null) return;
            if (air) fx.OnDoubleJump();
            else fx.OnJump();
        }

        /// <summary>Rebote al pisar un bug: recupera el doble salto.</summary>
        public void Bounce()
        {
            var v = Body.linearVelocity;
            v.y = RunTuning.StompBounceVelocity;
            Body.linearVelocity = v;
            jumping = true;
            cutDone = true;
            airJumps = AirJumpsMax;
            lastGroundedTime = -999f;
            if (fx != null) fx.OnJump();
        }

        /// <summary>Teletransporta sin moverse durante un instante (inicio y reaparición).</summary>
        public void Respawn(Vector2 position)
        {
            Body.position = position;
            transform.position = position;
            Body.linearVelocity = Vector2.zero;
            holdUntil = Time.time + RespawnHold;
            jumping = cutDone = freshPress = holdArmed = false;
            airJumps = AirJumpsMax;
            dashUntil = -1f;
            dashCharge = true;
            Body.gravityScale = RunTuning.GravityScale;
            bufferTimer = stuckTimer = 0f;
            lastGroundedTime = -999f;
            lastX = position.x;
            prevVy = 0f;
            if (fx != null) fx.OnRespawn();
        }

        /// <summary>Muerte: congela la física y explota en caracteres (visible aunque haya caído al vacío).</summary>
        public void Explode()
        {
            Body.simulated = false;
            if (transform.position.y < 0.4f) transform.position = new Vector3(transform.position.x, 0.4f, 0f);
            if (fx != null) fx.Explode();
        }
    }
}
