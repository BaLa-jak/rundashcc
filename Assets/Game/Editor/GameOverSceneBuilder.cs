using UnityEngine;
using UnityEngine.UI;

namespace RunDashC.EditorTools
{
    /// <summary>Escena GameOver: lluvia roja, GAME OVER con glitch, segfault, estadísticas e integrantes.</summary>
    public static class GameOverSceneBuilder
    {
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        /// <summary>"GAME OVER" en figlet: 5 líneas de 71 caracteres.</summary>
        static readonly string GameOverArt = string.Join("\n",
            @"  ____      _      __  __   _____       ___   __     __  _____   ____  ",
            @" / ___|    / \    |  \/  | | ____|     / _ \  \ \   / / | ____| |  _ \ ",
            @"| |  _    / _ \   | |\/| | |  _|      | | | |  \ \ / /  |  _|   | |_) |",
            @"| |_| |  / ___ \  | |  | | | |___     | |_| |   \ V /   | |___  |  _ < ",
            @" \____| /_/   \_\ |_|  |_| |_____|     \___/     \_/    |_____| |_| \_\");

        public static void Build()
        {
            var scene = SceneBuilder.NewScene(withCamera: true);
            var canvas = AsciiUi.CreateCanvas("GameOverCanvas", 0);
            var root = canvas.transform;

            var rain = AsciiUi.CreateText(root, "Rain", "", 26, AsciiTheme.Red, TextAnchor.UpperLeft);
            AsciiUi.Stretch(rain.rectTransform);
            var drops = rain.gameObject.AddComponent<AsciiRain>();
            drops.headColor = new Color(1f, 0.62f, 0.66f, 0.5f);
            drops.trailColor = AsciiTheme.WithAlpha(AsciiTheme.Red, 0.28f);
            drops.fadeColor = AsciiTheme.WithAlpha(AsciiTheme.Red, 0.13f);
            drops.density = 0.3f;

            UiKit.Frame(root, "Frame", Vector2.zero, new Vector2(1888, 1048), 30, AsciiTheme.WithAlpha(AsciiTheme.Red, 0.75f));

            // Logo pequeño + GAME OVER enorme con glitch (el shake inicial lo dispara el controlador).
            UiKit.Label(root, "Logo", AsciiTheme.Logo, 26, AsciiTheme.Green, TextAnchor.MiddleCenter, Center, new Vector2(0, 410), new Vector2(58 * UiKit.Cw(26) + 20, 26 * 5 + 10));
            var title = UiKit.Label(root, "GameOverTitle", GameOverArt, 56, AsciiTheme.Red, TextAnchor.MiddleCenter, Center, new Vector2(0, 200), new Vector2(71 * UiKit.Cw(56) + 20, 56 * 5 + 10));
            var glitch = title.gameObject.AddComponent<GlitchText>();
            glitch.amount = 0.08f;
            glitch.maxInterval = 3f;

            // Mensajes de error con typewriter (alineados a la izquierda para que no "bailen").
            const float msgLeft = -290f;
            var segfault = UiKit.LeftLabel(root, "Segfault", "Segmentation fault (core dumped)", 38, AsciiTheme.Red, msgLeft, 30).gameObject.AddComponent<TypewriterText>();
            segfault.delay = 0.6f;
            segfault.cursor = false;
            var exit = UiKit.LeftLabel(root, "ExitCode", "> proceso 'camarada' terminado con código 1", 32, AsciiTheme.White, msgLeft, -16).gameObject.AddComponent<TypewriterText>();
            exit.delay = 0.6f + 32 / segfault.charsPerSecond + 0.3f;

            // Estadísticas (el texto lo pone el controlador) e integrantes.
            const float blockTop = -80f;
            var stats = UiKit.Label(root, "Stats", "nivel_alcanzado = 01;\nbits = 000;\nrecord = 000;", 38, AsciiTheme.White, TextAnchor.UpperLeft, Center, new Vector2(-520 + 280, blockTop - 75), new Vector2(560, 150));
            var record = UiKit.LeftLabel(root, "NewRecord", "// NUEVO RECORD!", 36, AsciiTheme.Yellow, -520, blockTop - 140);
            record.gameObject.AddComponent<Blink>().period = 0.6f;
            UiKit.Label(root, "Integrantes", UiKit.IntegrantesComment(), 28, AsciiTheme.Green, TextAnchor.UpperLeft, Center, new Vector2(60 + 290, blockTop - 100), new Vector2(580, 200));

            var retry = AsciiButton.Create(root, "RetryButton", "REANUDAR", 48, Center, new Vector2(-240, -390)).GetComponent<Button>();
            var leave = AsciiButton.Create(root, "ExitButton", "SALIR", 48, Center, new Vector2(240, -390)).GetComponent<Button>();
            retry.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = leave };
            leave.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = retry };

            AsciiUi.AddCrtOverlay(canvas);

            var ctrl = canvas.gameObject.AddComponent<GameOverController>();
            ctrl.statsText = stats;
            ctrl.newRecord = record.gameObject;
            ctrl.title = glitch;
            ctrl.retryButton = retry;
            ctrl.exitButton = leave;

            SceneBuilder.Save(scene, SceneNames.GameOver);
        }
    }
}
