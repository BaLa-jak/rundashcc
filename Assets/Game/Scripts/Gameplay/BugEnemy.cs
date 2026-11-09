using System.Collections;
using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Bug 'B': camina hacia la izquierda cuando Camarada está cerca y gira al borde de su plataforma.
    /// Si Camarada le cae encima (bajando y con los pies sobre su centro) se aplasta: rebote, +5 bits.
    /// </summary>
    public class BugEnemy : Hazard
    {
        const float Size = 1.1f;
        const float Speed = 1.5f;
        const float WakeDistance = 16f;

        /// <summary>Mapa del nivel: el borde y las paredes se leen de la grilla (equivale al raycast, sin falsos positivos con triggers).</summary>
        public LevelData level;

        Transform visual;
        SpriteRenderer sprite;
        Rigidbody2D body;
        BoxCollider2D box;
        float dir = -1f;
        float walkPhase;
        bool walking, dead;

        void Start()
        {
            visual = new GameObject("visual").transform; // pivote en los pies: el squash no lo despega del piso
            visual.SetParent(transform, false);
            visual.localPosition = new Vector3(0f, -0.5f, 0f);
            sprite = WorldKit.AddSprite(visual, "bug", Size, 10);
            if (sprite.sprite != null)
                sprite.transform.localPosition = new Vector3(0f, sprite.sprite.bounds.size.y * sprite.transform.localScale.y / 2f - 0.03f, 0f);

            box = AddTrigger<BoxCollider2D>();
            box.size = new Vector2(0.75f, 0.55f);
            box.offset = new Vector2(0f, -0.5f + 0.3f);
            body = AddKinematicBody();
        }

        void FixedUpdate()
        {
            walking = false;
            if (dead || level == null) return;
            var run = RunManager.Instance;
            if (run == null || run.player == null || Mathf.Abs(run.player.transform.position.x - body.position.x) > WakeDistance) return;

            var p = body.position;
            int cx = Mathf.FloorToInt(p.x + dir * 0.45f), cy = Mathf.FloorToInt(p.y);
            if (level.At(cx, cy - 1) != LevelData.Block || level.At(cx, cy) == LevelData.Block)
            {
                dir = -dir;
                return;
            }
            walking = true;
            body.MovePosition(p + new Vector2(dir * Speed * Time.fixedDeltaTime, 0f));
        }

        void Update()
        {
            if (dead) return;
            sprite.flipX = dir < 0f; // el PNG mira a la derecha
            walkPhase += Time.deltaTime * (walking ? 14f : 5f);
            float s = Mathf.Sin(walkPhase);
            // "Patas": squash alterno + bob + wobble.
            visual.localScale = new Vector3(1f + 0.07f * s, 1f - 0.07f * s, 1f);
            visual.localRotation = Quaternion.Euler(0f, 0f, 6f * Mathf.Sin(walkPhase * 0.5f));
            visual.localPosition = new Vector3(0f, -0.5f + (walking ? 0.06f * Mathf.Abs(s) : 0f), 0f);
        }

        protected override void OnPlayer(CamaradaController player)
        {
            if (dead) return;
            if (player.Velocity.y < 0f && player.FeetY > box.bounds.center.y) StartCoroutine(Squash(player));
            else base.OnPlayer(player);
        }

        IEnumerator Squash(CamaradaController player)
        {
            dead = true;
            box.enabled = false;
            player.Bounce();
            AudioManager.Play(Sfx.Stomp, 0.05f);
            GameSession.AddBits(RunTuning.StompBits);

            var center = box.bounds.center;
            AsciiParticles.Burst(center, "#*{};/01!", AsciiTheme.Red, 16, 6f, 0.7f, 0.45f, 14f);
            AsciiParticles.Popup(center + Vector3.up * 0.9f, "bug fixed! +" + RunTuning.StompBits, AsciiTheme.Green, 0.7f);

            visual.localRotation = Quaternion.identity;
            visual.localScale = new Vector3(1.4f, 0.25f, 1f);
            yield return new WaitForSeconds(0.25f);
            Destroy(gameObject);
        }
    }
}
