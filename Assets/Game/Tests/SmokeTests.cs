using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RunDashC.Tests
{
    /// <summary>
    /// Smoke tests de PlayMode: Tools/unity.sh test
    /// Cualquier Debug.LogError o excepción durante un test lo hace fallar.
    /// </summary>
    public class SmokeTests
    {
        string savedName;
        int savedBest, savedBestDistance;
        readonly int[] savedProgress = new int[GameSession.LevelCount + 1];
        int savedWallet;
        readonly int[] savedOwn = new int[Cosmetics.All.Length];
        readonly string[] savedEquip = new string[Cosmetics.Slots.Length];
        readonly int[] savedStars = new int[GameSession.LevelCount + 1];

        [OneTimeSetUp]
        public void SavePrefs()
        {
            // Los tests usan StartNewRun: no queremos pisar el nombre ni el récord del jugador real.
            savedName = PlayerPrefs.GetString("rdc.player", GameSession.DefaultName);
            savedBest = PlayerPrefs.GetInt("rdc.best", 0);
            savedBestDistance = GameSession.BestDistance;
            for (int n = 1; n <= GameSession.LevelCount; n++) savedProgress[n] = GameSession.LevelProgress(n);
            // La tienda también: cartera, compras y lo equipado.
            savedWallet = Cosmetics.Wallet;
            for (int i = 0; i < Cosmetics.All.Length; i++) savedOwn[i] = PlayerPrefs.GetInt("rdc.own." + Cosmetics.All[i].Id, 0);
            for (int s = 0; s < savedEquip.Length; s++) savedEquip[s] = PlayerPrefs.GetString("rdc.eq." + (CosmeticSlot)s, "");
            for (int n = 1; n <= GameSession.LevelCount; n++) savedStars[n] = PlayerPrefs.GetInt("rdc.star" + n, 0);
        }

        [OneTimeTearDown]
        public void RestorePrefs()
        {
            PlayerPrefs.SetString("rdc.player", savedName);
            PlayerPrefs.SetInt("rdc.best", savedBest);
            PlayerPrefs.SetInt("rdc.inf.best", savedBestDistance);
            for (int n = 1; n <= GameSession.LevelCount; n++) PlayerPrefs.SetInt("rdc.lvl" + n, savedProgress[n]);
            PlayerPrefs.SetInt("rdc.wallet", savedWallet);
            for (int i = 0; i < Cosmetics.All.Length; i++) PlayerPrefs.SetInt("rdc.own." + Cosmetics.All[i].Id, savedOwn[i]);
            for (int s = 0; s < savedEquip.Length; s++) PlayerPrefs.SetString("rdc.eq." + (CosmeticSlot)s, savedEquip[s]);
            for (int n = 1; n <= GameSession.LevelCount; n++) PlayerPrefs.SetInt("rdc.star" + n, savedStars[n]);
            PlayerPrefs.Save();
        }

        /// <summary>Cada test empieza sin estrellas (sin habilidades): 3 vidas, sin dash, etc.</summary>
        [SetUp]
        public void ResetTimeAndStars()
        {
            Time.timeScale = 1f;
            SetStars(0);
        }

        /// <summary>Las primeras <paramref name="n"/> estrellas del juego (nivel 1 primero).</summary>
        static void SetStars(int n)
        {
            for (int level = 1; level <= GameSession.LevelCount; level++)
            {
                int k = Mathf.Clamp(n - (level - 1) * Stars.PerLevel, 0, Stars.PerLevel);
                PlayerPrefs.SetInt("rdc.star" + level, (1 << k) - 1);
            }
        }

        [Test]
        public void EveryLevelHasThreeStars()
        {
            for (int n = 1; n <= GameSession.LevelCount; n++)
            {
                var level = LevelData.Load(n);
                int count = 0;
                for (int x = 0; x < level.Width; x++)
                    for (int y = 0; y < LevelData.Rows; y++)
                        if (level.At(x, y) == LevelData.Star) count++;
                Assert.AreEqual(Stars.PerLevel, count, $"nivel{n}");
            }
        }

        [Test]
        public void AbilitiesTableIsSortedAndHasArt()
        {
            Assert.AreEqual(10, Abilities.All.Length);
            for (int i = 0; i < Abilities.All.Length; i++)
            {
                var a = Abilities.All[i];
                Assert.AreEqual(i, (int)a.Kind, a.Id);
                if (i > 0) Assert.Greater(a.Stars, Abilities.All[i - 1].Stars, a.Id);
                Assert.LessOrEqual(a.Stars, Stars.Max, a.Id);
                Assert.IsNotNull(Resources.Load<Sprite>("Art/" + a.Icon), a.Icon);
            }
            foreach (var art in new[] { "estrella", "prod_estrella", "cos_corona", "wear_corona" })
                Assert.IsNotNull(Resources.Load<Sprite>("Art/" + art), art);
        }

        [Test]
        public void StarsUnlockAbilities()
        {
            Assert.AreEqual(0, Stars.Total);
            Assert.AreEqual(GameSession.BaseLives, GameSession.MaxLives);
            Assert.IsFalse(Cosmetics.Owns(Cosmetics.Corona));
            Stars.Collect(2, 1);
            Stars.Collect(2, 1); // repetida no cuenta
            Assert.AreEqual(1, Stars.Total);
            Assert.IsTrue(Stars.Has(2, 1) && !Stars.Has(2, 0));
            Assert.IsTrue(Abilities.Has(Ability.TripleSalto) && !Abilities.Has(Ability.Dash));
            SetStars(3);
            Assert.AreEqual(GameSession.BaseLives + 1, GameSession.MaxLives);
            SetStars(4);
            Assert.IsTrue(Cosmetics.Owns(Cosmetics.Corona));
            Assert.IsTrue(Cosmetics.Toggle(Cosmetics.Corona));
            Assert.AreEqual("corona", Cosmetics.Equipped(CosmeticSlot.Sombrero)?.Id);
            SetStars(Stars.Max);
            Assert.AreEqual(Abilities.All.Length, Abilities.UnlockedCount(Stars.Total));
        }

        [UnityTest]
        public IEnumerator DashIsInvulnerableAndTripleJumpUnlocks()
        {
            SetStars(2);
            yield return StartRun();
            var run = RunManager.Instance;
            var player = run.player;
            Assert.AreEqual(2, player.AirJumpsMax, "1 estrella = triple salto");
            yield return new WaitForSeconds(0.5f); // pausa de reaparición
            Assert.IsTrue(player.StartDash(), "con 2 estrellas el dash debe estar listo");
            Assert.IsTrue(player.Dashing && run.IsInvulnerable);
            Assert.IsFalse(player.StartDash(), "un dash por salto");
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(player.Dashing);
            Assert.IsFalse(run.IsInvulnerable);
            Assert.AreEqual(GameSession.BaseLives, GameSession.Lives);
        }

        [Test]
        public void Level1ParsesWithStartAndFinish()
        {
            var level = LevelData.Load(1);
            Assert.Greater(level.Width, 200);
            Assert.Greater(level.FinishCell.x, level.StartCell.x);
        }

        [Test]
        public void NameIsSanitized()
        {
            Assert.AreEqual(GameSession.DefaultName, GameSession.SanitizeName("   "));
            Assert.AreEqual("ABCDEFGHIJKL", GameSession.SanitizeName("  abcdefghijklmnop "));
            Assert.AreEqual("BX", GameSession.SanitizeName("<b>x"));
        }

        [Test]
        public void EveryAudioClipExists()
        {
            Assert.AreEqual("sfx_double_jump", AudioManager.FileName(Sfx.DoubleJump));
            foreach (Sfx s in Enum.GetValues(typeof(Sfx)))
                Assert.IsNotNull(Resources.Load<AudioClip>("Audio/" + AudioManager.FileName(s)), s.ToString());
            Assert.IsNotNull(Resources.Load<AudioClip>("Audio/" + AudioManager.FileName(Music.Menu)));
            Assert.IsNotNull(Resources.Load<AudioClip>("Audio/" + AudioManager.FileName(Music.Level1)));
            Assert.IsNotNull(Resources.Load<AudioClip>("Audio/" + AudioManager.FileName(Music.Shop)));
            Assert.IsNotNull(Resources.Load<AudioClip>("Audio/" + AudioManager.FileName(Music.Produccion)));
        }

        [Test]
        public void PortalLevelsHavePortalsAndArt()
        {
            Assert.IsTrue(LevelData.Load(6).Contains(LevelData.PortalProduction), "nivel6 sin portal morado");
            var espejo = LevelData.Load(7);
            Assert.IsTrue(espejo.Contains(LevelData.PortalFlip) && espejo.Contains(LevelData.PortalProduction), "nivel7 sin portales");
            Assert.IsFalse(LevelData.Load(3).Contains(LevelData.PortalFlip));
            foreach (var art in new[] { "portal_azul", "portal_azul_frente", "portal_morado", "portal_morado_frente",
                         "prod_pinchos", "prod_moneda", "prod_sierra", "prod_fuego", "prod_nube", "prod_colina" })
                Assert.IsNotNull(Resources.Load<Sprite>("Art/" + art), art);
        }

        [Test]
        public void ProductionLastsTenSecondsAndFlipToggles()
        {
            WorldMode.Reset();
            int changes = 0;
            Action count = () => changes++;
            WorldMode.Changed += count;
            try
            {
                WorldMode.SetProduction(true);
                Assert.IsFalse(WorldMode.Tick(6f));
                WorldMode.SetProduction(true); // otro portal reinicia el tiempo
                Assert.IsFalse(WorldMode.Tick(6f));
                Assert.IsTrue(WorldMode.Tick(4.1f), "La producción debe acabarse a los 10 s");
                Assert.IsFalse(WorldMode.Tick(1f), "Tick avisa una sola vez");
                WorldMode.SetProduction(false);
                Assert.IsFalse(WorldMode.Production);
                WorldMode.SetFlipped(true);
                WorldMode.SetFlipped(true);
                Assert.IsTrue(WorldMode.Flipped);
                Assert.AreEqual(3, changes, "Changed solo cuando el estado cambia");
            }
            finally
            {
                WorldMode.Changed -= count;
                WorldMode.Reset();
            }
        }

        [UnityTest]
        public IEnumerator PortalsSwitchSkinAndFlipCameraButNotHud()
        {
            yield return StartRun(6);
            RunManager.Instance.player.levelSpeed = 0f; // quieta en el inicio: una caída (respawn) apagaría la producción
            Assert.IsNull(GameObject.Find("prod"), "La piel prod debe empezar apagada");
            Assert.IsNotNull(GameObject.Find("code"));
            RunManager.Instance.EnterPortal(PortalKind.Production);
            yield return WaitFor(() => WorldMode.Production, 2f);
            Assert.IsTrue(WorldMode.Production);
            Assert.IsNotNull(GameObject.Find("prod"), "No se prendió la piel de producción");

            RunManager.Instance.EnterPortal(PortalKind.Flip);
            yield return new WaitForSeconds(1f);
            var cam = Camera.main;
            Assert.Less(cam.projectionMatrix.m11, 0f, "La cámara no quedó volteada");
            var hud = GameObject.Find("HUD").GetComponent<Canvas>();
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, hud.renderMode, "El HUD debe seguir derecho");

            RunManager.Instance.EnterPortal(PortalKind.Flip);
            yield return new WaitForSeconds(1f);
            Assert.Greater(cam.projectionMatrix.m11, 0f);
            Assert.AreEqual(RenderMode.ScreenSpaceCamera, hud.renderMode);
        }

        [UnityTest]
        public IEnumerator SceneLoadsWithoutErrors(
            [Values(SceneNames.Menu, SceneNames.LevelSelect, SceneNames.Game, SceneNames.GameOver, SceneNames.Help, SceneNames.Shop, SceneNames.Abilities)] string scene)
        {
            SceneManager.LoadScene(scene);
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(scene, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator CamaradaRunsRightAfterCountdown()
        {
            yield return StartRun();
            var rata = GameObject.Find("Camarada");
            Assert.IsNotNull(rata, "No hay un objeto 'Camarada' en Game");
            float x0 = rata.transform.position.x;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.Greater(rata.transform.position.x, x0 + 1f, "Camarada no avanza sola");
        }

        [UnityTest]
        public IEnumerator LosingAllLivesLoadsGameOver()
        {
            yield return StartRun();
            for (int i = 0; i < GameSession.MaxLives; i++) GameSession.LoseLife();
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.GameOver, 6f);
            Assert.AreEqual(SceneNames.GameOver, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator PauseLoadsHelpAndResumeUnloadsIt()
        {
            yield return StartRun();
            SceneFlow.Pause();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(SceneManager.GetSceneByName(SceneNames.Help).isLoaded, "Help no se cargó");
            Assert.AreEqual(0f, Time.timeScale);

            SceneFlow.Resume();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(SceneManager.GetSceneByName(SceneNames.Help).isLoaded, "Help no se descargó");
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator CompletingLevelDancesSavesAndReturnsToLevelSelect()
        {
            yield return StartRun();
            var cam = Camera.main;
            float size = cam.orthographicSize;
            PlayerPrefs.SetInt("rdc.lvl1", 0);
            RunManager.Instance.Complete();

            yield return WaitFor(() => cam.orthographicSize < size * 0.5f, 3f);
            Assert.Less(cam.orthographicSize, size * 0.5f, "No hizo zoom hacia Camarada");
            yield return WaitFor(() => cam != null && Mathf.Approximately(cam.orthographicSize, size), 6f);
            Assert.AreEqual(size, cam.orthographicSize, 0.001f, "El zoom no regresó");
            Assert.AreEqual(100, GameSession.LevelProgress(1), "La meta no guardó el 100%");
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.LevelSelect, 6f);
            Assert.AreEqual(SceneNames.LevelSelect, SceneManager.GetActiveScene().name, "No volvió al selector de niveles");
        }

        [Test]
        public void EveryCosmeticHasArt()
        {
            Assert.AreEqual(20, Cosmetics.All.Length);
            foreach (var c in Cosmetics.All)
            {
                Assert.IsNotNull(Resources.Load<Sprite>("Art/" + c.Art), c.Art);
                Assert.IsNotNull(Resources.Load<Sprite>("Art/" + c.Wear), c.Wear);
            }
            Assert.IsNotNull(Resources.Load<Sprite>("Art/duende"));
            Assert.IsNotNull(Resources.Load<Sprite>("Art/tienda_icono"));
        }

        [Test]
        public void BuyingSpendsWalletAndEquips()
        {
            var hat = Cosmetics.All[0];
            var other = Cosmetics.All[1];
            PlayerPrefs.SetInt("rdc.own." + hat.Id, 0);
            PlayerPrefs.SetInt("rdc.own." + other.Id, 0);
            PlayerPrefs.SetInt("rdc.wallet", hat.Precio - 1);
            Assert.IsFalse(Cosmetics.TryBuy(hat), "Compró sin bits suficientes");

            Cosmetics.AddToWallet(1);
            Assert.IsTrue(Cosmetics.TryBuy(hat));
            Assert.AreEqual(0, Cosmetics.Wallet);
            Assert.IsTrue(Cosmetics.Owns(hat));
            Assert.IsTrue(Cosmetics.IsEquipped(hat));
            Assert.IsFalse(Cosmetics.TryBuy(hat), "Lo compró dos veces");

            Assert.IsFalse(Cosmetics.Toggle(other), "Equipó algo que no compró");
            Assert.IsFalse(Cosmetics.Toggle(hat));
            Assert.IsNull(Cosmetics.Equipped(hat.Slot));
            Assert.IsTrue(Cosmetics.Toggle(hat));

            GameSession.StartLevel(1);
            GameSession.AddBits(3);
            Assert.AreEqual(3, Cosmetics.Wallet, "Los bits recogidos no llegan a la cartera");
        }

        [Test]
        public void EndlessChunksExistForEveryTierAndParse()
        {
            var assets = Resources.LoadAll<TextAsset>("Levels/Infinito");
            Assert.GreaterOrEqual(assets.Length, 2, "Faltan tramos en Resources/Levels/Infinito");
            var tiers = new System.Collections.Generic.HashSet<int>();
            foreach (var a in assets)
            {
                var chunk = LevelData.Parse(a.text);
                Assert.Greater(chunk.Speed, 0f, a.name);
                tiers.Add(int.Parse(a.name.Substring(1, 2)));
            }
            for (int t = 1; t <= tiers.Count; t++) Assert.IsTrue(tiers.Contains(t), $"Falta el tier {t} del infinito");

            foreach (var portal in new[] { LevelData.PortalFlip, LevelData.PortalProduction })
            {
                var pc = LevelData.Parse(LevelBuilder.PortalChunk(portal));
                Assert.IsTrue(pc.Contains(portal) && pc.At(4, 2) == portal && pc.At(0, 1) == LevelData.Block, "tramo de portal mal armado");
                Assert.AreEqual(pc.Width, pc.FinishCell.x + 1);
            }
        }

        [Test]
        public void EndlessDistanceRecordAndRetry()
        {
            PlayerPrefs.SetInt("rdc.inf.best", 10);
            GameSession.StartLevel(GameSession.EndlessLevel);
            Assert.IsTrue(GameSession.IsEndless);
            Assert.AreEqual(1f, GameSession.SpeedMultiplier, "El infinito trae la velocidad absoluta en cada tramo");
            GameSession.SetDistance(8);
            Assert.AreEqual(10, GameSession.BestDistance);
            Assert.IsFalse(GameSession.IsNewDistanceRecord);
            GameSession.SetDistance(25);
            GameSession.SetDistance(20); // la distancia nunca baja
            Assert.AreEqual(25, GameSession.Distance);
            Assert.AreEqual(25, GameSession.BestDistance);
            Assert.IsTrue(GameSession.IsNewDistanceRecord);
            GameSession.RetryLevel();
            Assert.AreEqual(0, GameSession.Distance);
            Assert.AreEqual(25, GameSession.BestDistance);
        }

        [UnityTest]
        public IEnumerator EndlessRunStreamsChunksAndCountsDistance()
        {
            yield return StartRun(GameSession.EndlessLevel);
            Assert.IsNull(GameObject.Find("finish"), "El infinito no tiene meta");
            Assert.IsNotNull(GameObject.Find("chunk_0"), "No se construyó el primer tramo");
            yield return new WaitForSecondsRealtime(1.5f);
            // Sin saltar choca con el primer obstáculo del tramo (al azar, ~6 columnas después del inicio).
            Assert.Greater(GameSession.Distance, 2, "La distancia no sube al correr");
        }

        [Test]
        public void AllLevelsExistAndParse()
        {
            Assert.AreEqual(GameSession.LevelCount, LevelSelectController.Files.Length);
            for (int n = 1; n <= GameSession.LevelCount; n++)
            {
                Assert.IsNotNull(Resources.Load<TextAsset>($"Levels/nivel{n}"), $"Falta nivel{n}.txt");
                var level = LevelData.Load(n);
                Assert.Greater(level.FinishCell.x, level.StartCell.x, $"nivel{n}: la meta no está después del inicio");
            }
        }

        [Test]
        public void LastLevelUnlocksOnlyWhenOthersAt100()
        {
            for (int n = 1; n < GameSession.LevelCount; n++) PlayerPrefs.SetInt("rdc.lvl" + n, 100);
            PlayerPrefs.SetInt("rdc.lvl" + (GameSession.LevelCount - 1), 99);
            Assert.IsTrue(GameSession.IsUnlocked(1));
            Assert.IsFalse(GameSession.IsUnlocked(GameSession.LevelCount), "Se desbloqueó con un nivel al 99%");

            GameSession.StartLevel(GameSession.LevelCount - 1);
            GameSession.RecordProgress(0.5f);
            Assert.AreEqual(99, GameSession.LevelProgress(GameSession.LevelCount - 1), "RecordProgress bajó el progreso");
            GameSession.RecordProgress(1f);
            Assert.IsTrue(GameSession.IsUnlocked(GameSession.LevelCount));
        }

        [UnityTest]
        public IEnumerator PauseExitGoesToLevelSelect()
        {
            yield return StartRun();
            SceneFlow.Pause();
            yield return WaitFor(() => UnityEngine.Object.FindFirstObjectByType<HelpController>() != null, 2f);
            UnityEngine.Object.FindFirstObjectByType<HelpController>().exitButton.onClick.Invoke();
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.LevelSelect, 4f);
            Assert.AreEqual(SceneNames.LevelSelect, SceneManager.GetActiveScene().name);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(SceneFlow.IsPaused);
        }

        /// <summary>Nueva partida en Game y espera a que termine la cuenta regresiva.</summary>
        static IEnumerator StartRun(int level = 1)
        {
            bool go = false;
            Action<int> onTick = n => { if (n == 0) go = true; };
            RunEvents.CountdownTick += onTick;
            GameSession.StartNewRun("TEST");
            if (level != 1) GameSession.StartLevel(level);
            SceneManager.LoadScene(SceneNames.Game);
            yield return WaitFor(() => go, 6f);
            RunEvents.CountdownTick -= onTick;
            Assert.IsTrue(go, "La cuenta regresiva nunca llegó a 0");
        }

        static IEnumerator WaitFor(Func<bool> condition, float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        }
    }
}
