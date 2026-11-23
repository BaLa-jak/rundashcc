using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>
    /// HUD de la escena Game (lo arma y cablea HudBuilder). Escucha GameSession.Changed y RunEvents.
    /// Sin botones: el clic también es saltar. Todo anima en tiempo no escalado (pausa y slow-mo).
    /// </summary>
    public class HudController : MonoBehaviour
    {
        public Text infoText;
        public Text livesText;
        public Text lifeLostText;
        public Text bitsText;
        public Text progressText;
        public Text countdownText;
        public Text toastText;
        public Image hitFlash;
        public GameObject completePanel;
        public Text completeText;
        public Text fatalText;
        public HudGlitch rootGlitch;
        public HudGlitch livesGlitch;
        public HudGlitch fatalGlitch;

        const int BarWidth = 20;
        const float BlinkTime = 0.9f, LifeLostTime = 1.1f, PunchTime = 0.25f;
        const float FlashTime = 0.35f, FlashAlpha = 0.35f, CountdownTime = 0.95f;
        const string FatalTitle = "FATAL ERROR";

        int lives, bits, levelAtStart, lostIndex = -1, lastPct = -1, shownTier;
        float blinkUntil, lifeLostUntil, punchStart, flashStart = -10f, countdownStart = -10f, nextScramble;
        bool punching;
        Color countdownColor;
        Coroutine toastRoutine;

        void OnEnable()
        {
            GameSession.Changed += Refresh;
            RunEvents.CountdownTick += OnCountdown;
            RunEvents.ProgressChanged += OnProgress;
            RunEvents.PlayerHit += OnPlayerHit;
            RunEvents.Checkpoint += OnCheckpoint;
            RunEvents.LevelCompleted += OnLevelCompleted;
            RunEvents.PlayerDied += OnPlayerDied;

            // Estado inicial sin animaciones de "vida perdida" ni punch.
            levelAtStart = GameSession.Level;
            lives = GameSession.Lives;
            bits = GameSession.Bits;
            lastPct = -1;
            OnProgress(0f);
            countdownText.color = Color.clear;
            hitFlash.color = Color.clear;
            toastText.text = "";
            lifeLostText.gameObject.SetActive(false);
            completePanel.SetActive(false);
            fatalText.gameObject.SetActive(false);
            Refresh();
        }

        void OnDisable()
        {
            GameSession.Changed -= Refresh;
            RunEvents.CountdownTick -= OnCountdown;
            RunEvents.ProgressChanged -= OnProgress;
            RunEvents.PlayerHit -= OnPlayerHit;
            RunEvents.Checkpoint -= OnCheckpoint;
            RunEvents.LevelCompleted -= OnLevelCompleted;
            RunEvents.PlayerDied -= OnPlayerDied;
        }

        void Refresh()
        {
            float now = Time.unscaledTime;
            if (GameSession.Lives < lives)
            {
                lostIndex = GameSession.Lives;
                blinkUntil = now + BlinkTime;
                lifeLostUntil = now + LifeLostTime;
                lifeLostText.gameObject.SetActive(true);
                livesGlitch.Kick(0.45f);
            }
            if (GameSession.Bits > bits)
            {
                punchStart = now;
                punching = true;
            }
            lives = GameSession.Lives;
            bits = GameSession.Bits;

            infoText.text = $"JUGADOR: {GameSession.PlayerName}    NIVEL {GameSession.Level:00}";
            if (GameSession.IsEndless) RefreshEndless();
            livesText.text = LivesString(true);
            livesText.color = lives > 1 ? AsciiTheme.White : AsciiTheme.Red;
            livesText.rectTransform.localScale = Vector3.one;
            bitsText.text = BitsString(0f);
        }

        void Update()
        {
            float now = Time.unscaledTime;

            if (lostIndex >= 0)
            {
                bool done = now >= blinkUntil;
                livesText.text = LivesString(done || Mathf.Repeat(now, 0.16f) < 0.08f);
                if (done) lostIndex = -1;
            }
            if (lives == 1) // latido rojo con la última vida
            {
                float k = 0.5f + 0.5f * Mathf.Sin(now * 7f);
                livesText.color = AsciiTheme.WithAlpha(AsciiTheme.Red, Mathf.Lerp(1f, 0.45f, k));
                livesText.rectTransform.localScale = Vector3.one * (1f + 0.06f * k);
            }
            if (lifeLostText.gameObject.activeSelf && now >= lifeLostUntil)
                lifeLostText.gameObject.SetActive(false);

            if (punching)
            {
                float t = (now - punchStart) / PunchTime;
                punching = t < 1f;
                bitsText.text = BitsString(punching ? 1f - t : 0f);
            }

            float f = (now - flashStart) / FlashTime;
            if (f < 1f || hitFlash.color.a > 0f)
                hitFlash.color = AsciiTheme.WithAlpha(AsciiTheme.Red, f < 1f ? FlashAlpha * (1f - f) : 0f);

            float c = now - countdownStart;
            if (c < CountdownTime)
            {
                float p = Mathf.Clamp01(1f - c / 0.2f);
                countdownText.rectTransform.localScale = Vector3.one * (1f + 0.9f * p * p);
                countdownText.color = AsciiTheme.WithAlpha(countdownColor, Mathf.Clamp01((CountdownTime - c) / 0.35f));
            }
            else if (countdownText.color.a > 0f)
                countdownText.color = Color.clear;

            if (fatalText.gameObject.activeSelf && now >= nextScramble)
            {
                nextScramble = now + 0.07f;
                fatalText.text = Scramble(FatalTitle) + "\n<size=44>// segmentation fault (core dumped)</size>";
            }
        }

        string LivesString(bool blinkOn)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < GameSession.MaxLives; i++)
            {
                if (i < lives) sb.Append(lives == 1 ? "#" : Paint("#", AsciiTheme.Green)); // con 1 vida hereda el latido rojo
                else if (i == lostIndex) sb.Append(Paint(blinkOn ? "#" : "-", AsciiTheme.Red));
                else sb.Append(Paint("-", AsciiTheme.GreenDim));
            }
            return sb.Append("] VIDAS = ").Append(lives).ToString();
        }

        string BitsString(float punch)
        {
            int size = Mathf.RoundToInt(bitsText.fontSize * (1f + 0.6f * punch));
            string number = $"<size={size}>{Paint(bits.ToString("000"), punch > 0f ? AsciiTheme.Cyan : AsciiTheme.White)}</size>";
            string record = GameSession.IsNewRecord
                ? Paint($"record: {GameSession.BestBits:000} // NUEVO RECORD!", AsciiTheme.Yellow)
                : Paint($"record: {GameSession.BestBits:000}", AsciiTheme.GreenDim);
            return $"bits: {number}   {record}";
        }

        /// <summary>Infinito: la barra de progreso pasa a ser distancia + récord; arriba, el multiplicador de velocidad.</summary>
        void RefreshEndless()
        {
            var run = RunManager.Instance;
            float x = run != null && run.player != null ? run.player.transform.position.x : 0f;
            float factor = run != null && run.level != null ? run.level.SpeedFactorAt(x) : 1f;
            int tier = run != null && run.level != null ? run.level.TierAt(x) : 1;
            infoText.text = $"JUGADOR: {GameSession.PlayerName}    INFINITO x{factor:0.00}";
            string record = GameSession.IsNewDistanceRecord
                ? Paint($"record: {GameSession.BestDistance:0000} m // NUEVO RECORD!", AsciiTheme.Yellow)
                : Paint($"record: {GameSession.BestDistance:0000} m", AsciiTheme.GreenDim);
            progressText.text = $"distancia: {GameSession.Distance:0000} m   {record}";
            if (tier > shownTier)
            {
                if (shownTier > 0)
                {
                    if (toastRoutine != null) StopCoroutine(toastRoutine);
                    toastRoutine = StartCoroutine(Toast($"> dificultad++ // tier {tier:00}"));
                }
                shownTier = tier;
            }
        }

        void OnProgress(float t)
        {
            if (GameSession.IsEndless) return;
            int pct = Mathf.RoundToInt(t * 100f);
            if (pct == lastPct) return;
            lastPct = pct;
            int filled = Mathf.RoundToInt(t * BarWidth);
            string done = new string('=', Mathf.Max(0, filled - 1)) + (filled > 0 ? ">" : "");
            progressText.text = $"[{done}{Paint(new string('.', BarWidth - filled), AsciiTheme.GreenDim)}] {pct,3}%";
        }

        void OnCountdown(int n)
        {
            countdownText.text = n > 0 ? n.ToString() : "run();";
            countdownColor = n > 0 ? AsciiTheme.White : AsciiTheme.Green;
            countdownStart = Time.unscaledTime;
        }

        void OnPlayerHit()
        {
            flashStart = Time.unscaledTime;
            rootGlitch.Kick(0.3f);
        }

        void OnCheckpoint()
        {
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(Toast("> progreso guardado"));
        }

        IEnumerator Toast(string msg)
        {
            toastText.color = AsciiTheme.Cyan;
            for (int i = 1; i <= msg.Length; i++)
            {
                toastText.text = msg.Substring(0, i) + "_";
                yield return new WaitForSecondsRealtime(0.02f);
            }
            toastText.text = msg;
            yield return new WaitForSecondsRealtime(1.2f);
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                toastText.color = AsciiTheme.WithAlpha(AsciiTheme.Cyan, 1f - t / 0.4f);
                yield return null;
            }
            toastText.text = "";
        }

        void OnLevelCompleted()
        {
            OnProgress(1f);
            countdownStart = -10f;
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastText.text = "";
            completePanel.SetActive(true);
            StartCoroutine(TypeComplete());
        }

        IEnumerator TypeComplete()
        {
            string[] lines =
            {
                $"NIVEL {levelAtStart:00} COMPLETADO",
                "// compilado sin errores (0 warnings)",
                $"bits: {GameSession.Bits:000}",
                "> return 0; // volviendo a ./niveles",
            };
            Color[] colors = { AsciiTheme.Green, AsciiTheme.GreenDim, AsciiTheme.White, AsciiTheme.Cyan };
            int total = 0;
            foreach (var l in lines) total += l.Length;

            // ~2 s de tipeo: el RunManager recarga la escena ~2.5 s después de LevelCompleted (tras el baile).
            for (int n = 0; n <= total; n++)
            {
                completeText.text = Framed(lines, colors, n, true);
                if (n % 3 == 0) AudioManager.Play(Sfx.Type, 0.1f, 0.5f);
                yield return new WaitForSecondsRealtime(0.018f);
            }
            for (bool on = false; ; on = !on)
            {
                completeText.text = Framed(lines, colors, total, on);
                yield return new WaitForSecondsRealtime(0.4f);
            }
        }

        /// <summary>Marco ASCII fijo; dentro se ven los primeros <paramref name="visible"/> caracteres y el cursor.</summary>
        static string Framed(string[] lines, Color[] colors, int visible, bool cursor)
        {
            int width = 0;
            foreach (var l in lines) width = Mathf.Max(width, l.Length);
            width += 1; // hueco para el cursor
            string border = "+" + new string('-', width + 4) + "+";
            string blank = "|" + new string(' ', width + 4) + "|";

            var sb = new StringBuilder(border).Append('\n').Append(blank).Append('\n');
            bool cursorPlaced = false;
            for (int i = 0; i < lines.Length; i++)
            {
                int k = Mathf.Clamp(visible, 0, lines[i].Length);
                visible -= lines[i].Length;
                bool here = !cursorPlaced && (k < lines[i].Length || i == lines.Length - 1);
                cursorPlaced |= here;
                sb.Append("|  ").Append(Paint(lines[i].Substring(0, k), colors[i]))
                  .Append(here && cursor ? Paint("_", AsciiTheme.Green) : " ")
                  .Append(' ', width - k - 1).Append("  |\n");
            }
            return sb.Append(blank).Append('\n').Append(border).ToString();
        }

        void OnPlayerDied()
        {
            flashStart = Time.unscaledTime;
            countdownStart = -10f;
            rootGlitch.Kick(0.6f);
            completePanel.SetActive(false);
            fatalText.gameObject.SetActive(true);
            fatalGlitch.Kick(60f); // hasta que cargue GameOver
        }

        static string Scramble(string s)
        {
            const string junk = "#$%&!?/\\01";
            var chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (chars[i] != ' ' && Random.value < 0.12f) chars[i] = junk[Random.Range(0, junk.Length)];
            return new string(chars);
        }

        static string Paint(string s, Color c) => $"<color=#{AsciiTheme.ToHex(c)}>{s}</color>";
    }
}
