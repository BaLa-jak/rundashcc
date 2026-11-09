using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Fondo en dos capas con wrap infinito: "fondo" (3 copias, 12 u de alto, factor 0.85)
    /// y líneas de código tenues (factor 0.5). Corre después de CameraRig para no temblar.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ParallaxLoop : MonoBehaviour
    {
        const int MidLines = 16;
        const float MidSpan = 48f;
        const float ViewHeight = 12f;

        public Transform cameraTransform;
        public float farFactor = 0.85f;
        public float midFactor = 0.5f;

        readonly Transform[] tiles = new Transform[3];
        readonly Transform[] lines = new Transform[MidLines];
        readonly Vector2[] lineOrigin = new Vector2[MidLines];
        float tileWidth;

        void Start()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

            for (int i = 0; i < tiles.Length; i++)
            {
                var sr = WorldKit.AddSprite(transform, "fondo", 1f, -100);
                tiles[i] = sr.transform;
                if (sr.sprite == null) continue;
                float scale = ViewHeight / sr.sprite.bounds.size.y;
                tiles[i].localScale = Vector3.one * scale;
                tileWidth = sr.sprite.bounds.size.x * scale;
                sr.color = new Color(0.55f, 0.55f, 0.55f); // más oscuro: el primer plano manda
            }

            // Muy tenue y chico: es decorado, no debe competir con hints ni peligros.
            var color = AsciiTheme.WithAlpha(AsciiTheme.GreenDim, 0.14f);
            for (int i = 0; i < MidLines; i++)
            {
                string text = AsciiTheme.CodeSnippets[Random.Range(0, AsciiTheme.CodeSnippets.Length)];
                if (Random.value < 0.5f) text += "  " + AsciiTheme.CodeSnippets[Random.Range(0, AsciiTheme.CodeSnippets.Length)];
                lines[i] = WorldKit.AddText(transform, text, Vector2.zero, color, Random.Range(0.3f, 0.42f), TextAnchor.MiddleLeft, -50).transform;
                lineOrigin[i] = new Vector2(Random.Range(0f, MidSpan), Random.Range(3.5f, ViewHeight - 1.5f));
            }
        }

        void LateUpdate()
        {
            if (cameraTransform == null) return;
            float cx = cameraTransform.position.x, cy = cameraTransform.position.y;

            if (tileWidth > 0f)
            {
                float origin = cx * farFactor;
                float n0 = Mathf.Round((cx - origin) / tileWidth);
                for (int i = 0; i < tiles.Length; i++)
                    tiles[i].position = new Vector3(origin + (n0 + i - 1) * tileWidth, cy, 0f);
            }

            float o = cx * midFactor, left = cx - MidSpan / 2f;
            for (int i = 0; i < MidLines; i++)
                lines[i].position = new Vector3(left + Mathf.Repeat(o + lineOrigin[i].x - left, MidSpan), lineOrigin[i].y, 0f);
        }
    }
}
