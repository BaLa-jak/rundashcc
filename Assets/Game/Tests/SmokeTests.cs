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
        int savedBest;

        [OneTimeSetUp]
        public void SavePrefs()
        {
            // Los tests usan StartNewRun: no queremos pisar el nombre ni el récord del jugador real.
            savedName = PlayerPrefs.GetString("rdc.player", GameSession.DefaultName);
            savedBest = PlayerPrefs.GetInt("rdc.best", 0);
        }

        [OneTimeTearDown]
        public void RestorePrefs()
        {
            PlayerPrefs.SetString("rdc.player", savedName);
            PlayerPrefs.SetInt("rdc.best", savedBest);
            PlayerPrefs.Save();
        }

        [SetUp]
        public void ResetTime() => Time.timeScale = 1f;

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
        }

        [UnityTest]
        public IEnumerator SceneLoadsWithoutErrors(
            [Values(SceneNames.Menu, SceneNames.Game, SceneNames.GameOver, SceneNames.Help)] string scene)
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

        /// <summary>Nueva partida en Game y espera a que termine la cuenta regresiva.</summary>
        static IEnumerator StartRun()
        {
            bool go = false;
            Action<int> onTick = n => { if (n == 0) go = true; };
            RunEvents.CountdownTick += onTick;
            GameSession.StartNewRun("TEST");
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
