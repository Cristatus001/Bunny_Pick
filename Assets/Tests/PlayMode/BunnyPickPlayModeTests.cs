using System.Collections;
using BunnyPick.Gameplay;
using BunnyPick.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BunnyPick.Tests
{
    public class BunnyPickPlayModeTests
    {
        static IEnumerator WaitForState(GameManager gm, GameState target, float maxSeconds)
        {
            float start = Time.realtimeSinceStartup;
            while (gm.State != target && Time.realtimeSinceStartup - start < maxSeconds) yield return null;
        }

        static IEnumerator WaitForAnyBunny(GameManager gm, float maxSeconds, System.Func<Bunny, bool> predicate, System.Action<int, Bunny> onFound)
        {
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < maxSeconds)
            {
                for (int i = 0; i < gm.Holes.Length; i++)
                {
                    var b = gm.Holes[i].Bunny;
                    if (b != null && predicate(b))
                    {
                        onFound(i, b);
                        yield break;
                    }
                }
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator MainMenuAndHud_ToggleVisibility_ThroughLoadingIntoPlaying()
        {
            yield return SceneManager.LoadSceneAsync("BunnyPick", LoadSceneMode.Single);
            yield return null;

            var gm = Object.FindFirstObjectByType<GameManager>();
            var menuUi = Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
            var hudUi = Object.FindFirstObjectByType<HudUI>(FindObjectsInactive.Include);

            Assert.IsNotNull(gm, "GameManager missing from scene.");
            Assert.IsNotNull(menuUi, "MainMenuUI missing from scene.");
            Assert.IsNotNull(hudUi, "HudUI missing from scene.");
            Assert.AreEqual(8, gm.Holes.Length, "Expected 8 fixed holes.");

            Assert.Greater(menuUi.root.alpha, 0.5f, "MainMenuPanel should be visible before Play is pressed.");
            Assert.Less(hudUi.root.alpha, 0.5f, "HudPanel should be hidden before Play is pressed.");

            gm.StartGame();
            yield return null;
            Assert.AreEqual(GameState.Loading, gm.State, "StartGame should enter a brief Loading state first.");
            Assert.Less(menuUi.root.alpha, 0.5f, "MainMenuPanel should hide as soon as loading starts.");

            yield return WaitForState(gm, GameState.Playing, 5f);
            Assert.AreEqual(GameState.Playing, gm.State, "Should transition to Playing after the loading screen.");
            Assert.Greater(hudUi.root.alpha, 0.5f, "HudPanel should show once gameplay starts.");

            gm.ReturnToMenu();
        }

        [UnityTest]
        public IEnumerator CorrectTap_FadesBunnyOut_AndAdvancesProgress()
        {
            yield return SceneManager.LoadSceneAsync("BunnyPick", LoadSceneMode.Single);
            yield return null;

            var gm = Object.FindFirstObjectByType<GameManager>();
            gm.StartGame();
            yield return WaitForState(gm, GameState.Playing, 5f);

            int targetHole = -1;
            yield return WaitForAnyBunny(gm, 10f, b => b.IsTarget, (i, b) => targetHole = i);
            Assert.GreaterOrEqual(targetHole, 0, "No target bunny appeared within 10 real seconds.");

            int progressBefore = gm.Progress;
            bool tapped = gm.TapHole(targetHole);
            Assert.IsTrue(tapped, "Tapping the target bunny should be accepted.");
            Assert.AreEqual(BunnyState.Caught, gm.Holes[targetHole].Bunny.State, "Correct tap should mark the bunny Caught.");

            float start = Time.realtimeSinceStartup;
            while (gm.Holes[targetHole].Bunny != null && Time.realtimeSinceStartup - start < 3f) yield return null;

            Assert.AreEqual(progressBefore + 1, gm.Progress, "Progress should advance by one after the catch fades out.");
            Assert.AreEqual(GameState.Playing, gm.State, "Game should still be playing (unless that was the 20th question).");

            gm.ReturnToMenu();
        }

        [UnityTest]
        public IEnumerator WrongTap_CostsALife_AndBunnyRetreatsInsteadOfVanishing()
        {
            yield return SceneManager.LoadSceneAsync("BunnyPick", LoadSceneMode.Single);
            yield return null;

            var gm = Object.FindFirstObjectByType<GameManager>();
            gm.StartGame();
            yield return WaitForState(gm, GameState.Playing, 5f);

            int wrongHole = -1;
            yield return WaitForAnyBunny(gm, 10f, b => !b.IsTarget, (i, b) => wrongHole = i);
            Assert.GreaterOrEqual(wrongHole, 0, "No distractor bunny appeared within 10 real seconds.");

            int livesBefore = gm.Lives;
            bool tapped = gm.TapHole(wrongHole);
            Assert.IsTrue(tapped, "Tapping a wrong bunny should be accepted.");
            Assert.AreEqual(livesBefore - 1, gm.Lives, "Wrong tap should cost exactly one life.");
            Assert.AreEqual(BunnyState.Retreating, gm.Holes[wrongHole].Bunny.State, "Wrong bunny should retreat, not vanish instantly.");

            float start = Time.realtimeSinceStartup;
            while (gm.Holes[wrongHole].Bunny != null && Time.realtimeSinceStartup - start < 2f) yield return null;
            Assert.IsNull(gm.Holes[wrongHole].Bunny, "Bunny should be gone once its retreat animation finishes.");

            gm.ReturnToMenu();
        }

        [UnityTest]
        public IEnumerator TapTeddy_Freeze_ActivatesTheFreezePowerup()
        {
            yield return SceneManager.LoadSceneAsync("BunnyPick", LoadSceneMode.Single);
            yield return null;

            var gm = Object.FindFirstObjectByType<GameManager>();
            gm.StartGame();
            yield return WaitForState(gm, GameState.Playing, 5f);

            const int idx = 0;
            gm.Holes[idx].Bunny = null;
            gm.Holes[idx].Teddy = new Teddy { HoleIndex = idx, EffectType = PowerupType.Freeze, Lifetime = 10f, State = TeddyState.Active };

            bool tapped = gm.TapHole(idx);
            Assert.IsTrue(tapped, "Tapping an active teddy should be accepted.");
            Assert.AreEqual(TeddyState.Collected, gm.Holes[idx].Teddy.State, "Teddy should enter the Collected (poof) state.");
            Assert.Greater(gm.Powerups.FreezeUntil, 0f, "Freeze should now be active.");

            gm.ReturnToMenu();
        }

        [UnityTest]
        public IEnumerator ChatBubbleAndBadgeFollowers_AreWiredToAllEightHoles()
        {
            yield return SceneManager.LoadSceneAsync("BunnyPick", LoadSceneMode.Single);
            yield return null;

            var bubbles = Object.FindObjectsByType<ChatBubbleFollower>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var badges = Object.FindObjectsByType<BadgeChipFollower>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.AreEqual(8, bubbles.Length, "Expected one chat bubble per hole.");
            Assert.AreEqual(8, badges.Length, "Expected one badge chip per hole.");
            foreach (var b in bubbles)
            {
                Assert.IsTrue(b.gameObject.activeSelf, "Chat bubble GameObjects must start active (CanvasGroup alpha drives visibility).");
                Assert.IsNotNull(b.visibilityGroup);
                Assert.IsNotNull(b.text);
            }
            foreach (var b in badges)
            {
                Assert.IsTrue(b.gameObject.activeSelf, "Badge chip GameObjects must start active.");
                Assert.IsNotNull(b.visibilityGroup);
                Assert.IsNotNull(b.badgeImage);
            }
        }
    }
}
