using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SticklersGate.Combat;
using SticklersGate.Data;
using SticklersGate.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SticklersGate.Tests.PlayMode
{
    /// <summary>
    /// Autoplay harness: builds the real combat scene (CombatBootstrapper, UI,
    /// grid, AI) and plays the player side with <see cref="AutoPilot"/>.
    /// Writes screenshots, a combat log per battle, and a summary CSV to
    /// SticklersGate/Autoplay/&lt;timestamp&gt;/ so a person or an agent can
    /// review what actually happened on screen. Fails on exceptions/error logs,
    /// battles that never end, or player commands the game rejects.
    ///
    /// Run headless (needs graphics, so no -nographics):
    /// Unity.exe -batchmode -projectPath SticklersGate -runTests -testPlatform PlayMode
    ///   -testCategory Autoplay -testResults results.xml
    /// Balance sims: -testCategory Balance, with AUTOPLAY_SIMS (default 20) and
    /// AUTOPLAY_LEVEL (default 1, the Combat scene's party level).
    /// </summary>
    public class AutoplayTests
    {
        private const int MAX_TURNS = 200;
        private const float TIME_SCALE = 20f;
        /// <summary>Recorded runs use level 5 so every ability (incl. areas) shows up on screen.</summary>
        private const int SHOWCASE_LEVEL = 5;
        private const int SHOT_WIDTH = 1920;
        private const int SHOT_HEIGHT = 1080;
        private const int SCREENSHOT_EVERY_N_TURNS = 12;

        private static readonly string[] DEFAULT_PARTY =
            { "SticknightClass", "StickwizardClass", "StickrogueClass", "SticklericClass" };

        /// <summary>The full roster split into three parties so every class fights every encounter.</summary>
        private static readonly string[][] ROSTER_PARTIES =
        {
            new[] { "StickbarbarianClass", "SticksorcererClass", "StickmonkClass", "SticklericClass" },
            new[] { "StickpaladinClass", "StickwarlockClass", "StickrangerClass", "StickbardClass" },
            new[] { "SticknightClass", "StickdruidClass", "StickrogueClass", "StickwizardClass" },
        };

        private static string _runDir;
        private RenderTexture _rt;

        private struct BattleResult
        {
            public string Encounter;
            public string Party;
            public bool Finished;
            public bool Victory;
            public int Turns;
            public int PartySurvivors;
            public int Rejected;
            public int AreaCasts;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _runDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Autoplay",
                DateTime.Now.ToString("yyyy-MM-dd_HHmmss")));
            Directory.CreateDirectory(_runDir);
            File.WriteAllText(Path.Combine(_runDir, "summary.csv"),
                "encounter,party,finished,victory,turns,party_survivors,rejected_commands,area_casts\n");
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (_rt != null) _rt.Release();
        }

        [UnityTest, Category("Autoplay"), Timeout(1800000)]
        public IEnumerator Autoplay_EveryEncounter_DefaultParty()
        {
            var results = new List<BattleResult>();
            foreach (var encounter in LoadEncounters())
            {
                yield return RunBattle(encounter, DEFAULT_PARTY, SHOWCASE_LEVEL, screenshots: true, r => results.Add(r));
            }
            AssertHealthy(results);
        }

        [UnityTest, Category("Autoplay"), Timeout(3600000)]
        public IEnumerator Autoplay_FullRoster_EveryEncounter()
        {
            var results = new List<BattleResult>();
            foreach (var encounter in LoadEncounters())
            {
                foreach (var party in ROSTER_PARTIES)
                {
                    yield return RunBattle(encounter, party, SHOWCASE_LEVEL, screenshots: true, r => results.Add(r));
                }
            }
            AssertHealthy(results);
        }

        /// <summary>Many unrecorded battles per encounter × party for win-rate numbers. Explicit: slow.</summary>
        [UnityTest, Explicit("Balance simulation; run with -testCategory Balance"), Category("Balance"), Timeout(14400000)]
        public IEnumerator Balance_WinRates()
        {
            int sims = int.TryParse(Environment.GetEnvironmentVariable("AUTOPLAY_SIMS"), out var n) ? n : 20;
            int level = int.TryParse(Environment.GetEnvironmentVariable("AUTOPLAY_LEVEL"), out var l) ? l : 1;
            var results = new List<BattleResult>();
            var parties = new List<string[]> { DEFAULT_PARTY };
            parties.AddRange(ROSTER_PARTIES);

            foreach (var encounter in LoadEncounters())
                foreach (var party in parties)
                    for (int i = 0; i < sims; i++)
                        yield return RunBattle(encounter, party, level, screenshots: false, r => results.Add(r));

            var report = new StringBuilder($"# party level {level}, {sims} battles per row\nencounter,party,battles,win_rate,avg_turns,avg_survivors\n");
            foreach (var g in results.GroupBy(r => (r.Encounter, r.Party)))
            {
                report.AppendLine($"{g.Key.Encounter},{g.Key.Party},{g.Count()}," +
                                  $"{g.Average(r => r.Victory ? 1.0 : 0.0):0.00},{g.Average(r => r.Turns):0.0},{g.Average(r => r.PartySurvivors):0.00}");
            }
            File.WriteAllText(Path.Combine(_runDir, "balance.csv"), report.ToString());
            AssertHealthy(results);
        }

        // ----------------------------------------------------------------

        private IEnumerator RunBattle(EncounterSO encounter, string[] partyAssets, int partyLevel, bool screenshots, Action<BattleResult> onDone)
        {
            var before = new HashSet<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
            Time.timeScale = TIME_SCALE;

            string partyLabel = string.Join("+", partyAssets.Select(p => p.Replace("Class", "")));
            string battleId = $"{encounter.name}__{partyLabel}";

            // Camera renders into a fixed 1920x1080 texture so layout and screenshots are deterministic
            if (_rt == null)
            {
                _rt = new RenderTexture(SHOT_WIDTH, SHOT_HEIGHT, 24);
                _rt.Create();
            }
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.95f, 0.93f, 0.88f);
            cam.targetTexture = _rt;

            // Configure the real bootstrapper before its Start() runs
            var bootGo = new GameObject("CombatBootstrapper");
            bootGo.SetActive(false);
            var boot = bootGo.AddComponent<CombatBootstrapper>();
            var classes = partyAssets.Select(LoadClass).ToArray();
            SetField(boot, "_encounter", encounter);
            SetField(boot, "_partyClasses", classes);
            SetField(boot, "_partyNames", classes.Select(c => c.ClassName).ToArray());
            SetField(boot, "_partyLevel", partyLevel);
            SetField(boot, "_starterItems", LoadItems());
            SetField(boot, "_starterItemQuantities", new[] { 2, 1, 1 });
            bootGo.SetActive(true);

            yield return null; // Start(): builds UI, grid, units
            var sm = UnityEngine.Object.FindAnyObjectByType<CombatStateMachine>();
            Assert.IsNotNull(sm, $"{battleId}: combat did not start.");
            RouteCanvasesTo(cam);

            var driver = camGo.AddComponent<AutoplayDriver>();
            driver.Attach(sm);

            bool finished = false, victory = false;
            sm.OnCombatResult += v => { finished = true; victory = v; };

            int nextShotTurn = SCREENSHOT_EVERY_N_TURNS;
            while (!finished && sm.TurnCount <= MAX_TURNS)
            {
                if (screenshots && driver.PendingCapture != null)
                {
                    Capture(cam, $"{battleId}__{driver.PendingCapture}");
                    driver.PendingCapture = null;
                }
                if (screenshots && sm.TurnCount >= nextShotTurn)
                {
                    Capture(cam, $"{battleId}__turn{sm.TurnCount:000}");
                    nextShotTurn += SCREENSHOT_EVERY_N_TURNS;
                }
                yield return null;
            }

            // Let the result panel appear, then record the end state
            for (int i = 0; i < 5; i++) yield return null;
            if (screenshots) Capture(cam, $"{battleId}__end");

            var result = new BattleResult
            {
                Encounter = encounter.name,
                Party = partyLabel,
                Finished = finished,
                Victory = victory,
                Turns = sm.TurnCount,
                PartySurvivors = sm.PlayerUnits.Count(u => u.IsAlive && u.IsConscious),
                Rejected = driver.RejectedCommands,
                AreaCasts = driver.AreaCasts
            };

            File.WriteAllText(Path.Combine(_runDir, $"{battleId}.log.txt"), driver.Log.ToString());
            File.AppendAllText(Path.Combine(_runDir, "summary.csv"),
                $"{result.Encounter},{result.Party},{result.Finished},{result.Victory},{result.Turns}," +
                $"{result.PartySurvivors},{result.Rejected},{result.AreaCasts}\n");
            Debug.Log($"[Autoplay] {battleId}: {(finished ? (victory ? "VICTORY" : "DEFEAT") : "UNFINISHED")} " +
                      $"in {result.Turns} turns, rejected={result.Rejected}, output={_runDir}");

            // Tear down everything this battle created
            Time.timeScale = 1f;
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!before.Contains(go)) UnityEngine.Object.Destroy(go);
            }
            yield return null;

            onDone(result);
        }

        private static void AssertHealthy(List<BattleResult> results)
        {
            var unfinished = results.Where(r => !r.Finished).Select(r => $"{r.Encounter}/{r.Party}").ToList();
            var rejected = results.Where(r => r.Rejected > 0).Select(r => $"{r.Encounter}/{r.Party} ({r.Rejected})").ToList();

            Assert.IsEmpty(unfinished, $"Battles that hit {MAX_TURNS} turns without ending: {string.Join(", ", unfinished)}. See {_runDir}");
            Assert.IsEmpty(rejected, $"Battles where the game rejected autopilot commands: {string.Join(", ", rejected)}. See logs in {_runDir}");
        }

        /// <summary>Switch overlay canvases to camera space so the UI lands in the render texture.</summary>
        private static void RouteCanvasesTo(Camera cam)
        {
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }
        }

        private void Capture(Camera cam, string name)
        {
            Canvas.ForceUpdateCanvases();
            cam.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(SHOT_WIDTH, SHOT_HEIGHT, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, SHOT_WIDTH, SHOT_HEIGHT), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;

            File.WriteAllBytes(Path.Combine(_runDir, name + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
        }

        // --- Asset loading (editor-only; the harness runs in the editor's play mode) ---

        private static List<EncounterSO> LoadEncounters()
        {
#if UNITY_EDITOR
            return AssetDatabase.FindAssets("t:EncounterSO", new[] { "Assets/_Project/Data/Encounters" })
                .Select(g => AssetDatabase.LoadAssetAtPath<EncounterSO>(AssetDatabase.GUIDToAssetPath(g)))
                .OrderBy(e => e.name)
                .ToList();
#else
            Assert.Ignore("Autoplay loads content via AssetDatabase; run it in the editor.");
            return null;
#endif
        }

        private static CharacterClassSO LoadClass(string assetName)
        {
#if UNITY_EDITOR
            var cls = AssetDatabase.LoadAssetAtPath<CharacterClassSO>($"Assets/_Project/Data/Classes/{assetName}.asset");
            Assert.IsNotNull(cls, $"Class asset {assetName} not found.");
            return cls;
#else
            return null;
#endif
        }

        private static ItemSO[] LoadItems()
        {
#if UNITY_EDITOR
            return new[] { "HealthPotion", "Antidote", "AlchemistFire" }
                .Select(n => AssetDatabase.LoadAssetAtPath<ItemSO>($"Assets/_Project/Data/Items/{n}.asset"))
                .Where(i => i != null)
                .ToArray();
#else
            return new ItemSO[0];
#endif
        }

        private static void SetField(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(info, $"{target.GetType().Name}.{field} not found.");
            info.SetValue(target, value);
        }
    }
}
