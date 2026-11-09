using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Estilo visual único del juego: terminal ASCII + líneas de código.
    /// Fuente VT323: usar solo ASCII + Latin-1 (á é í ó ú ñ · •). NO tiene █ ─ │ ↑ ni emojis.
    /// </summary>
    public static class AsciiTheme
    {
        public static readonly Color Bg = Hex("#0A0A0A");
        public static readonly Color Green = Hex("#33FF66");
        public static readonly Color GreenDim = Hex("#1A7F3A");
        public static readonly Color Cyan = Hex("#3EF1FF");
        public static readonly Color Red = Hex("#FF3344");
        public static readonly Color White = Hex("#E8E8E8");
        public static readonly Color Yellow = Hex("#FFD23F");

        public const string GameTitle = "RUNDASHC";
        public const string Practica = "PRACTICA #8";
        public const string CharacterName = "CAMARADA";

        public static readonly string[] Integrantes =
        {
            "Jonathan Ariel Lagarda Durazo",
            "Juan Carlos Gomez Ponce",
            "Yadid Elias Rosell Bañuelos",
            "Kedryck Sergio Noperi Vásquez",
        };

        /// <summary>Logo figlet de 5 líneas; todas miden lo mismo para que se alineen aunque el Text esté centrado.</summary>
        public static readonly string Logo = string.Join("\n",
            @" ____   _   _  _   _  ____      _     ____   _   _   ____ ",
            @"|  _ \ | | | || \ | ||  _ \    / \   / ___| | | | | / ___|",
            @"| |_) || | | ||  \| || | | |  / _ \  \___ \ | |_| || |    ",
            @"|  _ < | |_| || |\  || |_| | / ___ \  ___) ||  _  || |___ ",
            @"|_| \_\ \___/ |_| \_||____/ /_/   \_\|____/ |_| |_| \____|");

        /// <summary>Fragmentos de código para decorar plataformas, fondos y transiciones.</summary>
        public static readonly string[] CodeSnippets =
        {
            "if(rata.viva){correr();}", "while(true){esquivar();}", "int vidas = 3;",
            "for(;;){saltar();}", "return 0;", "bits++;", "malloc(queso);", "free(bug);",
            "try{run();}catch{}", "x = x / 0; // NO", "goto meta;", "#include <rata.h>",
            "camarada.saltar();", "if(bug){pisar();}", "printf(\"run\\n\");", "{;}{;}{;}",
        };

        static Font font;

        public static Font Font => font != null ? font : (font = Resources.Load<Font>("Fonts/VT323-Regular"));

        /// <summary>"RRGGBB" para usar en rich text: &lt;color=#RRGGBB&gt;.</summary>
        public static string ToHex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out var c);
            return c;
        }
    }
}
