using UnityEngine;

namespace RunDashC
{
    /// <summary>Púas '^': "^^" rojo apoyado en la plataforma de abajo, con flicker. Collider bajo.</summary>
    public class SpikesHazard : Hazard
    {
        const float Em = 1.2f;            // "^^" mide 2 * 0.406 * 1.2 ≈ 0.98 u
        const float StretchY = 1.6f;
        const float CaretInkBottom = 0.44f; // tinta de '^' en VT323: 0.44..0.76 em sobre el borde inferior de la línea

        TextMesh text;

        void Start()
        {
            text = WorldKit.AddText(transform, "^^", Vector2.zero, AsciiTheme.Red, Em, TextAnchor.LowerCenter, 10);
            text.transform.localScale = new Vector3(1f, StretchY, 1f);
            text.transform.localPosition = new Vector3(0f, -0.5f - CaretInkBottom * Em * StretchY, 0f);

            var box = AddTrigger<BoxCollider2D>();
            box.size = new Vector2(0.75f, 0.42f);
            box.offset = new Vector2(0f, -0.5f + 0.21f);
        }

        void Update()
        {
            text.color = Random.value < 0.05f ? AsciiTheme.WithAlpha(AsciiTheme.Red, 0.45f) : AsciiTheme.Red;
        }
    }
}
