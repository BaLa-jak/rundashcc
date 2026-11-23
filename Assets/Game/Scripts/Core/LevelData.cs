using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Nivel en texto plano (Resources/Levels/nivelN.txt). Formato:
    /// <code>
    /// # comentario (fuera del mapa)
    /// speed 8                      (opcional, unidades/seg a nivel 1)
    /// map
    /// ...12 filas exactas, de ARRIBA hacia ABAJO; la última fila es y = 0. 1 carácter = 1 unidad...
    /// end
    /// hint &lt;col&gt; &lt;y&gt; &lt;texto&gt;    (texto flotante en el mundo; y = 0 es la fila de abajo)
    /// </code>
    /// Leyenda: '=' bloque/suelo · '^' púas · 'B' bug · 'S' sierra · 's' sierra móvil ·
    /// 'Z' /0 · 'z' /0 móvil · '*' bit · 'C' checkpoint · 'F' meta · 'P' inicio · ' ' vacío.
    /// </summary>
    public sealed class LevelData
    {
        public const int Rows = 12;

        public const char Block = '=';
        public const char Spikes = '^';
        public const char Bug = 'B';
        public const char Saw = 'S';
        public const char SawMoving = 's';
        public const char DivZero = 'Z';
        public const char DivZeroMoving = 'z';
        public const char Bit = '*';
        public const char Checkpoint = 'C';
        public const char Finish = 'F';
        public const char Start = 'P';
        public const char Empty = ' ';

        const string Legend = "=^BSsZz*CFP ";

        public struct Hint
        {
            public int Col;
            public int Y;
            public string Text;
        }

        public float Speed = RunTuning.RunSpeed;
        public int Width { get; private set; }
        public Vector2Int StartCell { get; private set; }
        public Vector2Int FinishCell { get; private set; }
        public readonly List<Hint> Hints = new List<Hint>();

        char[,] cells; // [x, y], y = 0 abajo

        /// <summary>Carácter en (x, y); fuera del mapa devuelve ' '.</summary>
        public char At(int x, int y) =>
            x < 0 || y < 0 || x >= Width || y >= Rows ? Empty : cells[x, y];

        /// <summary>Modo infinito: pega <paramref name="chunk"/> a la derecha y devuelve su offset x en el mundo.</summary>
        public int Append(LevelData chunk)
        {
            int offset = Width;
            var grown = new char[Width + chunk.Width, Rows];
            for (int x = 0; x < grown.GetLength(0); x++)
                for (int y = 0; y < Rows; y++)
                    grown[x, y] = x < offset ? cells[x, y] : chunk.cells[x - offset, y];
            cells = grown;
            Width += chunk.Width;
            return offset;
        }

        /// <summary>Carga Resources/Levels/nivel{level}.txt; si no existe, usa nivel1.</summary>
        public static LevelData Load(int level)
        {
            var asset = Resources.Load<TextAsset>($"Levels/nivel{level}");
            if (asset == null) asset = Resources.Load<TextAsset>("Levels/nivel1");
            if (asset == null) throw new InvalidOperationException("Falta Resources/Levels/nivel1.txt");
            return Parse(asset.text);
        }

        /// <exception cref="FormatException">Con el número de línea si el texto no es válido.</exception>
        public static LevelData Parse(string source)
        {
            var data = new LevelData();
            var mapLines = new List<string>();
            var lines = source.Replace("\r", "").Split('\n');
            bool inMap = false, mapDone = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                int lineNo = i + 1;

                if (inMap)
                {
                    if (line.Trim() == "end") { inMap = false; mapDone = true; continue; }
                    mapLines.Add(line);
                    continue;
                }

                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#")) continue;

                var parts = trimmed.Split(new[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                switch (parts[0])
                {
                    case "map":
                        if (mapDone) throw new FormatException($"Línea {lineNo}: solo se permite un bloque 'map'.");
                        inMap = true;
                        break;
                    case "speed":
                        if (parts.Length < 2 || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out data.Speed) || data.Speed <= 0f)
                            throw new FormatException($"Línea {lineNo}: 'speed' necesita un número > 0.");
                        break;
                    case "hint":
                        if (parts.Length < 4 || !int.TryParse(parts[1], out var col) || !int.TryParse(parts[2], out var y))
                            throw new FormatException($"Línea {lineNo}: formato 'hint <col> <y> <texto>'.");
                        data.Hints.Add(new Hint { Col = col, Y = y, Text = parts[3] });
                        break;
                    default:
                        throw new FormatException($"Línea {lineNo}: directiva desconocida '{parts[0]}'.");
                }
            }

            if (inMap) throw new FormatException("Falta 'end' al final del mapa.");
            if (mapLines.Count != Rows) throw new FormatException($"El mapa debe tener {Rows} filas y tiene {mapLines.Count}.");

            foreach (var l in mapLines) data.Width = Mathf.Max(data.Width, l.Length);
            data.cells = new char[data.Width, Rows];
            int starts = 0, finishes = 0;

            for (int row = 0; row < Rows; row++)
            {
                int y = Rows - 1 - row;
                var l = mapLines[row];
                for (int x = 0; x < data.Width; x++)
                {
                    char c = x < l.Length ? l[x] : Empty;
                    if (Legend.IndexOf(c) < 0)
                        throw new FormatException($"Mapa fila {row + 1}, columna {x}: carácter desconocido '{c}'.");
                    data.cells[x, y] = c;
                    if (c == Start) { starts++; data.StartCell = new Vector2Int(x, y); }
                    if (c == Finish && finishes++ == 0) data.FinishCell = new Vector2Int(x, y);
                }
            }

            if (starts != 1) throw new FormatException($"El mapa necesita exactamente un 'P' (tiene {starts}).");
            if (finishes == 0) throw new FormatException("El mapa necesita al menos una 'F'.");
            return data;
        }
    }
}
