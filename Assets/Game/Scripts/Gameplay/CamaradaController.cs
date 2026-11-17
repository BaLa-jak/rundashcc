using UnityEngine;
using UnityEngine.InputSystem;

namespace RunDashC
{
    /// <summary>
    /// Camarada corre sola (auto-run) y el jugador solo salta: buffer, coyote time, salto variable y un doble salto.
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
        }

        static RunState State => RunManager.Instance != null ? RunManager.Instance.State : RunState.Countdown;

        void Update()
        {
            if (jumpAction == null || SceneFlow.IsPaused || Time.frameCount == SceneFlow.ResumedFrame || State != RunState.Running) return;
            if (jumpAction.WasPressedThisFrame())
            {
                bufferTimer = RunTuning.JumpBufferTime;
                freshPress = true;
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
                airJumps = 1;
                jumping = false;
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

                bool coyote = !jumping && Time.time - lastGroundedTime <= RunTuning.CoyoteTime;
                if (bufferTimer > 0f && (Grounded || coyote)) Jump(ref v, RunTuning.JumpVelocity, false);
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
            v.x = commandedRun ? RunSpeed : 0f;
            Body.linearVelocity = v;
            prevVy = v.y;
            lastX = Body.position.x;
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
            airJumps = 1;
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
            jumping = cutDone = freshPress = false;
            airJumps = 1;
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
