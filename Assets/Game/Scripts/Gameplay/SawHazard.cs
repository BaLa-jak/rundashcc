using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RunDashC
{
    /// <summary>Sierra while(true) 'S'; la variante 's' (moving) oscila ±2 u en vertical.</summary>
    public class SawHazard : Hazard
    {
        const float Size = 1.6f;
        const float Radius = 0.6f; // dientes a ~0.75 u: el collider perdona un poco

        public bool moving;

        Transform spin;
        Light2D glow;
        Rigidbody2D body;
        Vector2 origin;
        float phase;

        void Start()
        {
            spin = new GameObject("spin").transform;
            spin.SetParent(transform, false);
            var code = ProdSkin.Split(spin, out var prod);
            if (prod != null) WorldKit.AddSprite(prod, "prod_sierra", Size * 0.9f, 10);
            WorldKit.AddSprite(code, "sierra", Size, 10);
            glow = WorldKit.AddLight(transform, AsciiTheme.Red, 1f, 2.6f);
            AddTrigger<CircleCollider2D>().radius = Radius;
            origin = transform.position;
            phase = origin.x * 0.37f;
            if (moving) body = AddKinematicBody();
        }

        void Update()
        {
            spin.Rotate(0f, 0f, -600f * Time.deltaTime);
            glow.intensity = 1f + 0.6f * Mathf.Sin(Time.time * 6f + phase);
        }

        void FixedUpdate()
        {
            if (body != null) body.MovePosition(origin + Vector2.up * (2f * Mathf.Sin(Time.time * 2f + phase)));
        }
    }
}
