using UnityEngine;

namespace RunDashC
{
    /// <summary>Bit '*': "{1}" o "{0}" cian que gira y flota. Al recogerlo: +1 bit, sonido, chispas y popup.</summary>
    public class BitPickup : MonoBehaviour
    {
        Transform spin;
        float phase;
        bool taken;

        void Start()
        {
            spin = new GameObject("spin").transform;
            spin.SetParent(transform, false);
            var code = ProdSkin.Split(spin, out var prod);
            if (prod != null) WorldKit.AddSprite(prod, "prod_moneda", 0.7f, 5);
            WorldKit.AddText(code, Random.value < 0.5f ? "{1}" : "{0}", Vector2.zero, AsciiTheme.Cyan, 0.8f, TextAnchor.MiddleCenter, 5);
            var c = gameObject.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = 0.45f;
            phase = transform.position.x * 0.7f;
        }

        void Update()
        {
            float t = Time.time * 3f + phase;
            spin.localScale = new Vector3(Mathf.Cos(t), 1f, 1f);
            spin.localPosition = new Vector3(0f, 0.12f * Mathf.Sin(t * 0.8f), 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (taken || WorldKit.Player(other) == null) return;
            taken = true;
            GameSession.AddBits(1);
            AudioManager.Play(Sfx.Bit, 0.08f);
            AsciiParticles.Burst(transform.position, "01+*.", AsciiTheme.Cyan, 6, 3.5f, 0.4f, 0.35f, 0f);
            AsciiParticles.Popup(transform.position + Vector3.up * 0.5f, "+1", AsciiTheme.Cyan);
            Destroy(gameObject);
        }
    }
}
