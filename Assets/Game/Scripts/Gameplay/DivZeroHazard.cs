using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RunDashC
{
    /// <summary>"/0" 'Z': pulso de escala, flicker, bob, luz roja y '0' '/' orbitando. 'z' (moving) oscila en vertical.</summary>
    public class DivZeroHazard : Hazard
    {
        const float Size = 1.2f;
        const string OrbitChars = "0/0/";

        public bool moving;

        Transform visual;
        SpriteRenderer sprite;
        Light2D glow;
        Rigidbody2D body;
        readonly TextMesh[] orbit = new TextMesh[OrbitChars.Length];
        Vector2 origin;
        float phase;

        void Start()
        {
            visual = new GameObject("visual").transform;
            visual.SetParent(transform, false);
            sprite = WorldKit.AddSprite(visual, "obstaculo", Size, 10);
            glow = WorldKit.AddLight(transform, AsciiTheme.Red, 1.2f, 2.4f);
            for (int i = 0; i < orbit.Length; i++)
                orbit[i] = WorldKit.AddText(transform, OrbitChars[i].ToString(), Vector2.zero, AsciiTheme.Red, 0.55f, TextAnchor.MiddleCenter, 11);
            AddTrigger<CircleCollider2D>().radius = 0.42f;
            origin = transform.position;
            phase = origin.x * 0.53f;
            if (moving) body = AddKinematicBody();
        }

        void Update()
        {
            float t = Time.time + phase;
            visual.localScale = Vector3.one * (1f + 0.1f * Mathf.Sin(t * 8f));
            visual.localPosition = new Vector3(0f, 0.08f * Mathf.Sin(t * 3f), 0f);
            sprite.color = Random.value < 0.07f ? AsciiTheme.WithAlpha(AsciiTheme.White, 0.5f) : Color.white;
            glow.intensity = 1.2f + 0.5f * Mathf.Sin(t * 8f);
            for (int i = 0; i < orbit.Length; i++)
            {
                float a = t * 2.5f + i * Mathf.PI * 2f / orbit.Length;
                orbit[i].transform.localPosition = new Vector3(Mathf.Cos(a) * 0.85f, Mathf.Sin(a) * 0.85f, 0f);
            }
        }

        void FixedUpdate()
        {
            if (body != null) body.MovePosition(origin + Vector2.up * (2f * Mathf.Sin(Time.time * 1.6f + phase)));
        }
    }
}
