using System.Text;
using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Arma el nivel en runtime desde Resources/Levels/nivel{GameSession.Level}.txt (si no existe, nivel1).
    /// La celda (x, y) ocupa [x, x+1] × [y, y+1]; y = 0 es la fila de abajo.
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        const string Fill = "{;}";

        public LevelData Data { get; private set; }
        /// <summary>Centro del collider de Camarada en la celda 'P'.</summary>
        public Vector2 StartPosition { get; private set; }
        /// <summary>Borde izquierdo de la meta (celda 'F').</summary>
        public float FinishX { get; private set; }

        CodeGlitch glitch;

        void Awake()
        {
            Data = LevelData.Load(GameSession.Level);
            StartPosition = SpawnPoint(Data.StartCell);
            FinishX = Data.FinishCell.x;
            glitch = gameObject.AddComponent<CodeGlitch>();
            AsciiParticles.Warm();

            for (int y = 0; y < LevelData.Rows; y++)
            {
                for (int x = 0; x < Data.Width;)
                {
                    if (Data.At(x, y) != LevelData.Block) { x++; continue; }
                    int x0 = x;
                    while (Data.At(x, y) == LevelData.Block) x++;
                    BuildPlatform(x0, x, y);
                }
            }

            for (int x = 0; x < Data.Width; x++)
                for (int y = 0; y < LevelData.Rows; y++)
                    Spawn(Data.At(x, y), new Vector2(x + 0.5f, y + 0.5f));

            var gate = new GameObject("finish").AddComponent<FinishGate>();
            gate.transform.SetParent(transform, false);
            gate.transform.position = new Vector3(FinishX, 0f);
            gate.labelY = Data.FinishCell.y + 3f;

            string dim = AsciiTheme.ToHex(AsciiTheme.GreenDim);
            foreach (var h in Data.Hints)
            {
                var text = h.Text.StartsWith("//") ? $"<color=#{dim}>//</color>{h.Text.Substring(2)}" : h.Text;
                WorldKit.AddText(transform, text, new Vector2(h.Col, h.Y + 0.5f), AsciiTheme.Cyan, 0.9f, TextAnchor.MiddleLeft, 1);
            }
        }

        /// <summary>Punto de aparición de Camarada en una celda (apoyada en el suelo de la celda de abajo).</summary>
        public static Vector2 SpawnPoint(Vector2Int cell) => new Vector2(cell.x + 0.5f, cell.y + 0.32f);

        void Spawn(char c, Vector2 center)
        {
            switch (c)
            {
                case LevelData.Spikes: Create<SpikesHazard>("spikes", center); break;
                case LevelData.Bug: Create<BugEnemy>("bug", center).level = Data; break;
                case LevelData.Saw: Create<SawHazard>("saw", center); break;
                case LevelData.SawMoving: Create<SawHazard>("saw_moving", center).moving = true; break;
                case LevelData.DivZero: Create<DivZeroHazard>("div0", center); break;
                case LevelData.DivZeroMoving: Create<DivZeroHazard>("div0_moving", center).moving = true; break;
                case LevelData.Bit: Create<BitPickup>("bit", center); break;
                case LevelData.Checkpoint: Create<Checkpoint>("checkpoint", center); break;
            }
        }

        T Create<T>(string name, Vector2 center) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = center;
            return go.AddComponent<T>();
        }

        /// <summary>Las '=' contiguas [x0, x1) de la fila y: un collider, código arriba y relleno {;} en las filas tapadas.</summary>
        void BuildPlatform(int x0, int x1, int y)
        {
            int w = x1 - x0;
            var root = new GameObject($"plat_{x0}_{y}").transform;
            root.SetParent(transform, false);
            root.position = new Vector3(x0, y);

            var box = root.gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(w, 1f);
            box.offset = new Vector2(w / 2f, 0.5f);

            // Fondo opaco: tapa el parallax para que la plataforma se lea.
            WorldKit.AddRect(root, new Vector2(w / 2f, 0.5f), new Vector2(w, 1f), AsciiTheme.WithAlpha(AsciiTheme.Bg, 0.92f), -1);

            // Tramos con cara superior expuesta (código verde) o tapados por otra '=' (relleno tenue).
            for (int s = x0; s < x1;)
            {
                bool top = Data.At(s, y + 1) != LevelData.Block;
                int e = s;
                while (e < x1 && (Data.At(e, y + 1) != LevelData.Block) == top) e++;
                int chars = (e - s) * WorldKit.CodeCharsPerUnit;
                var tm = WorldKit.AddText(root, top ? Code(chars) : Repeat(Fill, chars), new Vector2(s - x0, 0.5f),
                    top ? AsciiTheme.Green : AsciiTheme.GreenDim, WorldKit.CodeEm, TextAnchor.MiddleLeft, 0);
                glitch.Add(tm);
                if (top) WorldKit.AddRect(root, new Vector2((s + e) / 2f - x0, 0.96f), new Vector2(e - s, 0.08f), AsciiTheme.Green, 1);
                s = e;
            }

            WorldKit.AddRect(root, new Vector2(0.04f, 0.5f), new Vector2(0.08f, 1f), AsciiTheme.Green, 1);
            WorldKit.AddRect(root, new Vector2(w - 0.04f, 0.5f), new Vector2(0.08f, 1f), AsciiTheme.Green, 1);

            // Remates cian >> / << en los bordes con cara superior, como en el mockup.
            if (Data.At(x1 - 1, y + 1) != LevelData.Block && Data.At(x1, y) == LevelData.Empty)
                WorldKit.AddText(root, ">>", new Vector2(w + 0.1f, 0.5f), AsciiTheme.Cyan, WorldKit.CodeEm, TextAnchor.MiddleLeft, 1);
            if (x0 > 0 && Data.At(x0, y + 1) != LevelData.Block && Data.At(x0 - 1, y) == LevelData.Empty)
                WorldKit.AddText(root, "<<", new Vector2(-0.1f, 0.5f), AsciiTheme.Cyan, WorldKit.CodeEm, TextAnchor.MiddleRight, 1);
        }

        /// <summary>Fragmentos de AsciiTheme.CodeSnippets al azar, recortados a <paramref name="n"/> caracteres.</summary>
        static string Code(int n)
        {
            var sb = new StringBuilder(n + 32);
            while (sb.Length < n) sb.Append(AsciiTheme.CodeSnippets[Random.Range(0, AsciiTheme.CodeSnippets.Length)]).Append(' ');
            sb.Length = n;
            return sb.ToString();
        }

        static string Repeat(string pattern, int n)
        {
            var sb = new StringBuilder(n + pattern.Length);
            while (sb.Length < n) sb.Append(pattern);
            sb.Length = n;
            return sb.ToString();
        }
    }
}
