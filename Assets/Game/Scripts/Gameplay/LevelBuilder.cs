using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Arma el nivel en runtime desde Resources/Levels/nivel{GameSession.Level}.txt (si no existe, nivel1).
    /// La celda (x, y) ocupa [x, x+1] × [y, y+1]; y = 0 es la fila de abajo.
    /// Infinito: encadena al azar tramos de Resources/Levels/Infinito/tNN_*.txt delante de Camarada (StreamTo);
    /// cada <see cref="PerTier"/> tramos sube un tier (más rápido y más enemigos/obstáculos).
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        const string Fill = "{;}";
        const int PerTier = 2;              // = ENDLESS_PER_TIER de generate_levels.py
        const float Lookahead = 60f, KeepBehind = 20f;
        const float OverflowSpeedUp = 1.03f; // por tramo después del último tier

        class Chunk
        {
            public int Start, End, Tier;
            public float Speed;
            public Transform Root;
        }

        readonly List<List<TextAsset>> tiers = new List<List<TextAsset>>();
        readonly List<Chunk> chunks = new List<Chunk>();
        int built, removed, lastPick = -1;

        public LevelData Data { get; private set; }
        /// <summary>Centro del collider de Camarada en la celda 'P'.</summary>
        public Vector2 StartPosition { get; private set; }
        /// <summary>Borde izquierdo de la meta (celda 'F').</summary>
        public float FinishX { get; private set; }

        CodeGlitch glitch;

        void Awake()
        {
            glitch = gameObject.AddComponent<CodeGlitch>();
            AsciiParticles.Warm();

            if (GameSession.IsEndless)
            {
                foreach (var asset in Resources.LoadAll<TextAsset>("Levels/Infinito"))
                {
                    int t = int.Parse(asset.name.Substring(1, 2)); // "t03_a" → 3
                    while (tiers.Count < t) tiers.Add(new List<TextAsset>());
                    tiers[t - 1].Add(asset);
                }
                AppendChunk();
                StartPosition = SpawnPoint(Data.StartCell);
                FinishX = float.PositiveInfinity;
                StreamTo(StartPosition.x, StartPosition.x);
                return;
            }

            Data = LevelData.Load(GameSession.Level);
            StartPosition = SpawnPoint(Data.StartCell);
            FinishX = Data.FinishCell.x;
            BuildColumns(0, Data.Width, transform);

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

        /// <summary>Infinito: agrega tramos hasta <see cref="Lookahead"/> delante de x, los construye y borra los que
        /// quedaron atrás de <paramref name="keepFrom"/> (el menor entre Camarada y su checkpoint).</summary>
        public void StreamTo(float x, float keepFrom)
        {
            if (chunks.Count == 0) return;
            while (chunks[chunks.Count - 1].Start < x + Lookahead) AppendChunk();
            // El último queda sin construir: así los remates >> / << de la costura ven al vecino real.
            for (; built < chunks.Count - 1; built++)
            {
                var c = chunks[built];
                c.Root = new GameObject("chunk_" + built).transform;
                c.Root.SetParent(transform, false);
                BuildColumns(c.Start, c.End, c.Root);
            }
            for (; removed < built && chunks[removed].End < keepFrom - KeepBehind; removed++)
                Destroy(chunks[removed].Root.gameObject);
        }

        /// <summary>Velocidad base en x: la del tramo en el infinito, la del archivo en un nivel normal.</summary>
        public float SpeedAt(float x) => chunks.Count == 0 ? Data.Speed : ChunkAt(x).Speed;

        /// <summary>Tier del tramo en x (1 en niveles normales).</summary>
        public int TierAt(float x) => chunks.Count == 0 ? 1 : ChunkAt(x).Tier;

        /// <summary>Velocidad en x respecto a la del primer tramo (1.0 en niveles normales).</summary>
        public float SpeedFactorAt(float x) => chunks.Count == 0 ? 1f : ChunkAt(x).Speed / chunks[0].Speed;

        Chunk ChunkAt(float x)
        {
            for (int i = chunks.Count - 1; i > 0; i--)
                if (x >= chunks[i].Start) return chunks[i];
            return chunks[0];
        }

        void AppendChunk()
        {
            int n = chunks.Count, tier = Mathf.Min(1 + n / PerTier, tiers.Count);
            var pool = tiers[tier - 1];
            int pick = Random.Range(0, pool.Count);
            if (pool.Count > 1 && pick == lastPick) pick = (pick + 1) % pool.Count; // no repetir el mismo seguido
            lastPick = pick;
            var chunk = LevelData.Parse(pool[pick].text);
            int start = Data == null ? 0 : Data.Append(chunk);
            if (Data == null) Data = chunk;
            // shortcut: después del último tier la velocidad sigue ×1.03 por tramo SIN validar en simulate.py (es el "muro"
            // que acaba la partida); si se siente injusto, generar más tiers en generate_levels.py.
            int overflow = Mathf.Max(0, n + 1 - tiers.Count * PerTier);
            chunks.Add(new Chunk { Start = start, End = Data.Width, Tier = tier, Speed = chunk.Speed * Mathf.Pow(OverflowSpeedUp, overflow) });
        }

        /// <summary>Plataformas, peligros y bits de las columnas [x0, x1) bajo <paramref name="parent"/>.</summary>
        void BuildColumns(int x0, int x1, Transform parent)
        {
            for (int y = 0; y < LevelData.Rows; y++)
            {
                for (int x = x0; x < x1;)
                {
                    if (Data.At(x, y) != LevelData.Block) { x++; continue; }
                    int s = x;
                    while (x < x1 && Data.At(x, y) == LevelData.Block) x++;
                    BuildPlatform(s, x, y, parent);
                }
            }

            for (int x = x0; x < x1; x++)
                for (int y = 0; y < LevelData.Rows; y++)
                    Spawn(Data.At(x, y), new Vector2(x + 0.5f, y + 0.5f), parent);
        }

        /// <summary>Punto de aparición de Camarada en una celda (apoyada en el suelo de la celda de abajo).</summary>
        public static Vector2 SpawnPoint(Vector2Int cell) => new Vector2(cell.x + 0.5f, cell.y + 0.32f);

        void Spawn(char c, Vector2 center, Transform parent)
        {
            switch (c)
            {
                case LevelData.Spikes: Create<SpikesHazard>("spikes", center, parent); break;
                case LevelData.Bug: Create<BugEnemy>("bug", center, parent).level = Data; break;
                case LevelData.Saw: Create<SawHazard>("saw", center, parent); break;
                case LevelData.SawMoving: Create<SawHazard>("saw_moving", center, parent).moving = true; break;
                case LevelData.DivZero: Create<DivZeroHazard>("div0", center, parent); break;
                case LevelData.DivZeroMoving: Create<DivZeroHazard>("div0_moving", center, parent).moving = true; break;
                case LevelData.Bit: Create<BitPickup>("bit", center, parent); break;
                case LevelData.Checkpoint: Create<Checkpoint>("checkpoint", center, parent); break;
            }
        }

        T Create<T>(string name, Vector2 center, Transform parent) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            return go.AddComponent<T>();
        }

        /// <summary>Las '=' contiguas [x0, x1) de la fila y: un collider, código arriba y relleno {;} en las filas tapadas.</summary>
        void BuildPlatform(int x0, int x1, int y, Transform parent)
        {
            int w = x1 - x0;
            var root = new GameObject($"plat_{x0}_{y}").transform;
            root.SetParent(parent, false);
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

            // Bordes laterales solo donde el suelo termina (en la costura entre tramos sigue).
            if (Data.At(x0 - 1, y) != LevelData.Block) WorldKit.AddRect(root, new Vector2(0.04f, 0.5f), new Vector2(0.08f, 1f), AsciiTheme.Green, 1);
            if (Data.At(x1, y) != LevelData.Block) WorldKit.AddRect(root, new Vector2(w - 0.04f, 0.5f), new Vector2(0.08f, 1f), AsciiTheme.Green, 1);

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
