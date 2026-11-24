using System.Collections.Generic;
using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Fondo en dos capas con wrap infinito: "fondo" (3 copias, 12 u de alto, factor 0.85)
    /// y líneas de código tenues (factor 0.5). Corre después de CameraRig para no temblar.
    /// En niveles con portal 'D' arma también la piel "producción": cielo, colinas y nubes (ProdSkin).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ParallaxLoop : MonoBehaviour
    {
        const int MidLines = 16;
        const float MidSpan = 48f;
        const float ViewHeight = 12f;
        const int SkyBands = 6, Hills = 6, Clouds = 7;
        const float ProdSpan = 60f;

        public Transform cameraTransform;
        public float farFactor = 0.85f;
        public float midFactor = 0.5f;

        readonly Transform[] tiles = new Transform[3];
        readonly Transform[] lines = new Transform[MidLines];
        readonly Vector2[] lineOrigin = new Vector2[MidLines];
        float tileWidth;

        Transform sky;
        readonly List<(Transform t, Vector2 origin, float factor)> prodItems = new List<(Transform, Vector2, float)>();

        void Start()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

            var code = ProdSkin.Split(transform, out var prod);
            if (prod != null) BuildProd(prod);

            for (int i = 0; i < tiles.Length; i++)
            {
                var sr = WorldKit.AddSprite(code, "fondo", 1f, -100);
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
                lines[i] = WorldKit.AddText(code, text, Vector2.zero, color, Random.Range(0.3f, 0.42f), TextAnchor.MiddleLeft, -50).transform;
                lineOrigin[i] = new Vector2(Random.Range(0f, MidSpan), Random.Range(3.5f, ViewHeight - 1.5f));
            }
        }

        /// <summary>Cielo degradado (bandas pegadas a la cámara), colinas lejanas y nubes, todo con parallax.</summary>
        void BuildProd(Transform prod)
        {
            sky = new GameObject("sky").transform;
            sky.SetParent(prod, false);
            float band = ViewHeight / SkyBands;
            for (int i = 0; i < SkyBands; i++)
            {
                var c = Color.Lerp(AsciiTheme.ProdSkyBottom, AsciiTheme.ProdSkyTop, (i + 0.5f) / SkyBands);
                WorldKit.AddRect(sky, new Vector2(0f, -ViewHeight / 2f + (i + 0.5f) * band), new Vector2(80f, band + 0.02f), c, -100);
            }
            for (int i = 0; i < Hills; i++)
            {
                var sr = WorldKit.AddSprite(prod, "prod_colina", Random.Range(9f, 14f), -90);
                sr.color = Color.Lerp(Color.white, AsciiTheme.ProdSkyBottom, 0.35f); // lejanas: un poco desvaídas
                prodItems.Add((sr.transform, new Vector2(i * ProdSpan / Hills, 3.6f), 0.8f)); // asoman sobre el suelo (altura 2)
            }
            for (int i = 0; i < Clouds; i++)
            {
                var sr = WorldKit.AddSprite(prod, "prod_nube", Random.Range(2.2f, 3.6f), -80);
                prodItems.Add((sr.transform, new Vector2(Random.Range(0f, ProdSpan), Random.Range(7.5f, 10.5f)), Random.Range(0.6f, 0.75f)));
            }
        }

        void LateUpdate()
        {
            if (cameraTransform == null) return;
            float cx = cameraTransform.position.x, cy = cameraTransform.position.y;

            if (sky != null)
            {
                sky.position = new Vector3(cx, cy, 0f);
                float prodLeft = cx - ProdSpan / 2f;
                foreach (var (t, origin, factor) in prodItems)
                    t.position = new Vector3(prodLeft + Mathf.Repeat(cx * factor + origin.x - prodLeft, ProdSpan), origin.y, 0f);
            }

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
