using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ShadowGarden.Presentation;
using ShadowGarden.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ShadowGarden.Tests.PlayMode
{
    public class ProductionUiFlowTests
    {
        [UnityTest]
        public IEnumerator Canvas_Uses_Production_Scaler()
        {
            yield return LoadMain();
            var canvas = Object.FindFirstObjectByType<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.IsNotNull(scaler);
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
            Assert.AreEqual(1920f, scaler.referenceResolution.x, 0.1f);
            Assert.AreEqual(1080f, scaler.referenceResolution.y, 0.1f);
            Assert.AreEqual(0.5f, scaler.matchWidthOrHeight, 0.01f);
        }

        [UnityTest]
        public IEnumerator Title_Has_Settings_And_Continue_Branch()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.Preferences.openingSeen = true;
            root.Save.RecordStageCleared("1-1", 9000);
            var screens = Object.FindFirstObjectByType<MainFlowScreens>();
            screens.RefreshTitleBranch();
            yield return null;
            Assert.IsNotNull(GameObject.Find("SettingsButton"));
            Assert.IsNotNull(GameObject.Find("ContinueButton"));
            Assert.IsNotNull(GameObject.Find("NewGameButton"));
        }

        [UnityTest]
        public IEnumerator Refreshing_Title_Preserves_Continue_Button_Callback()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            var screens = Object.FindFirstObjectByType<MainFlowScreens>();
            Assert.IsNotNull(root);
            Assert.IsNotNull(screens);

            root.Save.Preferences.openingSeen = true;
            screens.RefreshTitleBranch();
            yield return null;

            var continueButton = GameObject.Find("ContinueButton")?.GetComponent<Button>();
            Assert.IsNotNull(continueButton);
            continueButton.onClick.Invoke();
            yield return null;

            Assert.AreEqual(AppState.WorldMap, root.CurrentState);
        }

        [UnityTest]
        public IEnumerator Pause_Opens_From_Screen_Button_Without_Esc()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.Preferences.openingSeen = true;
            root.ContinueFromTitle();
            yield return null;
            root.StartStage("1-1");
            yield return null;
            yield return null;
            Assert.AreEqual(AppState.Playing, root.CurrentState);
            root.OpenPause();
            yield return null;
            Assert.IsTrue(root.Overlay.IsPauseOpen);
            Assert.IsTrue(root.IsPlayPaused);
        }

        [UnityTest]
        public IEnumerator GameOver_Shows_Cause_Copy()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.Preferences.openingSeen = true;
            root.ContinueFromTitle();
            yield return null;
            root.StartStage("1-1");
            yield return null;
            yield return null;
            root.NotifyGameOver(ShadowGarden.Core.GameOverCause.CliffFall);
            yield return null;
            Assert.AreEqual(AppState.GameOver, root.CurrentState);
            var reason = GameObject.Find("ReasonLabel")?.GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(reason);
            StringAssert.Contains("절벽", reason.text);
        }

        [UnityTest]
        public IEnumerator GameOver_Navigation_Keeps_Highlight_And_Enter_Activates_It()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            var screens = Object.FindFirstObjectByType<MainFlowScreens>();
            root.Save.Preferences.openingSeen = true;
            root.ContinueFromTitle();
            yield return null;
            root.StartStage("1-1");
            yield return null;
            root.NotifyGameOver(ShadowGarden.Core.GameOverCause.CliffFall);
            yield return null;

            Assert.IsFalse(EventSystem.current.sendNavigationEvents,
                "Flow screens must have one keyboard-navigation owner.");
            var navigate = typeof(MainFlowScreens).GetMethod(
                "OnNavigate", BindingFlags.Instance | BindingFlags.NonPublic);
            var submit = typeof(MainFlowScreens).GetMethod(
                "OnSubmit", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(navigate);
            Assert.IsNotNull(submit);

            navigate.Invoke(screens, new object[] { Vector2.down });
            yield return null;
            Assert.AreEqual("WorldMapButton", EventSystem.current.currentSelectedGameObject?.name);

            submit.Invoke(screens, null);
            yield return null;
            Assert.AreEqual(AppState.WorldMap, root.CurrentState);
        }

        [UnityTest]
        public IEnumerator Gameplay_Input_Path_CliffDeath_Reaches_GameOver_Modal()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            Assert.IsNotNull(root);
            root.Save.Preferences.openingSeen = true;
            root.ContinueFromTitle();
            yield return null;
            root.StartStage("1-1");
            yield return null;
            yield return null;

            var moveHandler = typeof(MainGameplayHost).GetMethod(
                "OnMove",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(moveHandler);
            moveHandler.Invoke(root.Gameplay, new object[] { ShadowGarden.Core.CardinalDirection.North });

            var guard = 0f;
            while (root.CurrentState != AppState.GameOver && guard < 2f)
            {
                guard += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(AppState.GameOver, root.CurrentState);
            Assert.IsFalse(root.Gameplay.IsSequencing);
            var retry = GameObject.Find("RetryButton");
            var worldMap = GameObject.Find("WorldMapButton");
            Assert.IsNotNull(retry);
            Assert.IsNotNull(worldMap);
            Assert.IsTrue(retry.activeInHierarchy);
            Assert.IsTrue(worldMap.activeInHierarchy);
            Assert.AreEqual("GameOverNotePanel", retry.transform.parent.parent.name);
        }

        [UnityTest]
        public IEnumerator Gameplay_Input_Path_OverlapDeath_Reaches_GameOver_Modal()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.Preferences.openingSeen = true;
            root.ContinueFromTitle();
            yield return null;
            root.StartStage("1-1");
            yield return null;

            var stage = new ShadowGarden.Core.StageDefinition(
                "overlap-test",
                new ShadowGarden.Core.GridSize(4, 4),
                new ShadowGarden.Core.GridPosition(1, 2),
                new[]
                {
                    new ShadowGarden.Core.GridPosition(1, 2),
                    new ShadowGarden.Core.GridPosition(0, 3),
                    new ShadowGarden.Core.GridPosition(3, 3)
                },
                new[]
                {
                    new ShadowGarden.Core.LampDefinition(
                        new ShadowGarden.Core.GridPosition(0, 3),
                        ShadowGarden.Core.ChannelId.Circle,
                        ShadowGarden.Core.CardinalDirection.East),
                    new ShadowGarden.Core.LampDefinition(
                        new ShadowGarden.Core.GridPosition(3, 3),
                        ShadowGarden.Core.ChannelId.Triangle,
                        ShadowGarden.Core.CardinalDirection.South)
                },
                new[]
                {
                    new ShadowGarden.Core.PillarDefinition(
                        new ShadowGarden.Core.GridPosition(0, 1),
                        ShadowGarden.Core.ChannelId.Circle,
                        ShadowGarden.Core.PillarHeight.Low),
                    new ShadowGarden.Core.PillarDefinition(
                        new ShadowGarden.Core.GridPosition(1, 0),
                        ShadowGarden.Core.ChannelId.Triangle,
                        ShadowGarden.Core.PillarHeight.Low)
                },
                ShadowGarden.Core.ClearGoalType.ExitDoor,
                new ShadowGarden.Core.GridPosition(3, 0),
                120);
            root.Gameplay.BeginDefinition(stage);
            yield return null;

            var moveHandler = typeof(MainGameplayHost).GetMethod(
                "OnMove",
                BindingFlags.Instance | BindingFlags.NonPublic);
            moveHandler.Invoke(root.Gameplay, new object[] { ShadowGarden.Core.CardinalDirection.North });

            var guard = 0f;
            while (root.CurrentState != AppState.GameOver && guard < 2f)
            {
                guard += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(AppState.GameOver, root.CurrentState);
            Assert.IsFalse(root.Gameplay.IsSequencing);
            var reason = GameObject.Find("ReasonLabel")?.GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(reason);
            StringAssert.Contains("겹친 그림자", reason.text);
        }

        [UnityTest]
        public IEnumerator Gameplay_TimeExpired_Reaches_GameOver_Modal()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.Preferences.openingSeen = true;
            root.ContinueFromTitle();
            yield return null;
            root.StartStage("1-1");
            yield return null;
            yield return null;

            root.Gameplay.Session.Tick(121000);
            var guard = 0f;
            while (root.CurrentState != AppState.GameOver && guard < 1.5f)
            {
                guard += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(AppState.GameOver, root.CurrentState);
            Assert.IsFalse(root.Gameplay.IsSequencing);
            var reason = GameObject.Find("ReasonLabel")?.GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(reason);
            StringAssert.Contains("시간 안에", reason.text);
        }

        [UnityTest]
        public IEnumerator Opening_Content_Remains_Inside_Note_Panel()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.Preferences.openingSeen = false;
            Assert.IsTrue(root.RequestState(AppState.Opening).Accepted);
            yield return null;
            yield return null;

            var panel = GameObject.Find("OpeningNotePanel")?.GetComponent<RectTransform>();
            Assert.IsNotNull(panel);
            AssertInside(panel, GameObject.Find("OpeningPageLabel")?.GetComponent<RectTransform>());
            AssertInside(panel, GameObject.Find("OpeningBody")?.GetComponent<RectTransform>());
            AssertInside(panel, GameObject.Find("ContinueButton")?.GetComponent<RectTransform>());
            AssertInside(panel, GameObject.Find("SkipButton")?.GetComponent<RectTransform>());
        }

        [UnityTest]
        public IEnumerator Ending_To_Title_Reapplies_Title_Panel_Layout()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            Assert.IsTrue(root.RequestState(AppState.WorldMap).Accepted);
            Assert.IsTrue(root.RequestState(AppState.Playing).Accepted);
            Assert.IsTrue(root.RequestState(AppState.Cleared).Accepted);
            Assert.IsTrue(root.RequestState(AppState.Ending).Accepted);
            Assert.IsTrue(root.RequestState(AppState.Title).Accepted);
            yield return null;
            yield return null;

            var panel = GameObject.Find("TitleNotePanel")?.GetComponent<RectTransform>();
            Assert.IsNotNull(panel);
            AssertInside(panel, GameObject.Find("ConceptLabel")?.GetComponent<RectTransform>());
            AssertInside(panel, GameObject.Find("ContinueButton")?.GetComponent<RectTransform>());
            AssertInside(panel, GameObject.Find("ReplayOpeningButton")?.GetComponent<RectTransform>());
            AssertInside(panel, GameObject.Find("SettingsButton")?.GetComponent<RectTransform>());
        }

        [UnityTest]
        public IEnumerator Repeated_Play_GameOver_WorldMap_Cycle_Does_Not_Duplicate_Ui()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.Preferences.openingSeen = true;
            root.ContinueFromTitle();
            yield return null;

            for (var iteration = 0; iteration < 10; iteration++)
            {
                root.StartStage("1-1");
                yield return null;
                root.NotifyGameOver(ShadowGarden.Core.GameOverCause.CliffFall);
                yield return null;
                root.ReturnToWorldMap();
                yield return null;
            }

            Assert.AreEqual(AppState.WorldMap, root.CurrentState);
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, System.Linq.Enumerable.Count(transforms, item => item.name == "WorldMapHeader"));
            Assert.LessOrEqual(System.Linq.Enumerable.Count(transforms, item => item.name == "GameOverNotePanel"), 1);
            foreach (var parent in transforms)
            {
                if (!parent.name.EndsWith("Panel") && !parent.name.EndsWith("Root")) continue;
                var directNames = System.Linq.Enumerable.Range(0, parent.childCount)
                    .Select(index => parent.GetChild(index).name)
                    .ToArray();
                Assert.AreEqual(directNames.Length, directNames.Distinct().Count(),
                    $"Duplicate direct child under {parent.name}");
            }
        }

        [UnityTest]
        public IEnumerator Buttons_Meet_Min_Height()
        {
            yield return LoadMain();
            var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            Assert.Greater(buttons.Length, 0);
            foreach (var button in buttons)
            {
                if (!button.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var rt = button.GetComponent<RectTransform>();
                Assert.GreaterOrEqual(rt.sizeDelta.y, UiTheme.ButtonMinHeight - 0.1f, button.name);
            }
        }

        [UnityTest]
        public IEnumerator Capture_6_1_Flow_Screenshots()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            yield return Capture("6_1_Title");

            root.ReplayOpening();
            yield return null;
            yield return Capture("6_1_Opening");

            root.CompleteOpening();
            yield return null;
            root.StartStage("1-1");
            yield return null;
            root.NotifyGameOver(ShadowGarden.Core.GameOverCause.CliffFall);
            yield return null;
            yield return Capture("6_1_GameOver");
        }

        [UnityTest]
        public IEnumerator Capture_Key_Ui_Screenshots()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.Preferences.openingSeen = true;
            root.ContinueFromTitle();
            yield return null;
            yield return null;

            foreach (var res in new[]
                     {
                         new Vector2Int(1280, 720),
                         new Vector2Int(1366, 768),
                         new Vector2Int(1440, 900),
                         new Vector2Int(1920, 1080)
                     })
            {
                Screen.SetResolution(res.x, res.y, FullScreenMode.Windowed);
                yield return null;
                yield return Capture($"UI_WorldMap_{res.x}x{res.y}");
            }

            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            yield return null;
            yield return Capture("UI_WorldMap");
            root.StartStage("1-1");
            yield return null;
            yield return null;
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return null;
            yield return Capture("UI_Play_12x6_1280x720");
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            yield return null;
            yield return Capture("UI_Play_12x6");
            root.OpenPause();
            yield return null;
            yield return Capture("UI_Pause");
            root.Overlay.ClosePause(false);
            root.ReturnToWorldMap();
            yield return null;

            foreach (var id in new[] { "1-1", "1-2", "1-3", "1-4", "2-1", "2-2", "2-3", "2-4", "3-1", "3-2", "3-3" })
            {
                root.Save.RecordStageCleared(id, 12000);
            }

            root.StartStage("3-4");
            yield return null;
            yield return null;
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return null;
            yield return Capture("UI_Play_18x8_1280x720");
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            yield return null;
            yield return Capture("UI_Play_18x8");
            Assert.AreEqual("3-4", root.Gameplay.Definition.StageId);
        }

        [UnityTest]
        public IEnumerator Title_Has_Replay_Opening_Button()
        {
            yield return LoadMain();
            Assert.IsNotNull(GameObject.Find("ReplayOpeningButton"));
        }

        [UnityTest]
        public IEnumerator Channel_Glyphs_Are_Embedded_In_Objects_And_Lamp_Arrow_Is_Emphasized()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.Preferences.openingSeen = true;
            root.ContinueFromTitle();
            yield return null;
            root.StartStage("1-3");
            yield return null;

            foreach (var pillar in root.Gameplay.Definition.Pillars)
            {
                var cell = GameObject.Find($"Cell_{pillar.Position.X}_{pillar.Position.Y}");
                Assert.IsNotNull(cell);
                Assert.IsNull(cell.transform.Find("ChannelMark"), "No glyph may remain outside on the tile");
                var glyph = cell.transform.Find("GameplayObject/ChannelMark");
                Assert.IsNotNull(glyph);
                Assert.AreEqual(0f, glyph.localPosition.x, 0.01f);
                Assert.Greater(glyph.localPosition.y, 0.7f);
                Assert.GreaterOrEqual(glyph.localScale.x, 0.33f);
                Assert.GreaterOrEqual(glyph.childCount, 4, "Glyph must use the bold renderer treatment");
            }

            var lamp = root.Gameplay.Definition.Lamps[0];
            var lampCell = GameObject.Find($"Cell_{lamp.Position.X}_{lamp.Position.Y}");
            var lampGlyph = lampCell.transform.Find("GameplayObject/ChannelMark");
            var arrow = lampCell.transform.Find("GameplayObject/DirectionMark");
            Assert.IsNotNull(lampGlyph);
            Assert.IsNotNull(arrow);
            Assert.AreEqual(1.37f, lampGlyph.localPosition.y, 0.02f);
            Assert.GreaterOrEqual(lampGlyph.localScale.x, 0.37f);
            Assert.GreaterOrEqual(lampGlyph.childCount, 4, "Lamp glyph must use the bold renderer treatment");
            Assert.GreaterOrEqual(arrow.localScale.x, 0.45f);
            Assert.Greater(Vector3.Distance(arrow.localPosition, lampGlyph.localPosition), 0.5f);
        }

        [UnityTest]
        public IEnumerator Title_Uses_Garden_Enter_Copy()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.ResetProgressForNewGame();
            root.Save.Preferences.openingSeen = false;
            var screens = Object.FindFirstObjectByType<MainFlowScreens>();
            screens.RefreshTitleBranch();
            yield return null;
            var continueBtn = GameObject.Find("ContinueButton");
            Assert.IsNotNull(continueBtn);
            var label = continueBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            Assert.IsNotNull(label);
            StringAssert.Contains("정원 들어가기", label.text);
        }

        [UnityTest]
        public IEnumerator Pause_Retry_Restarts_Without_Leaving_Playing()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.Preferences.openingSeen = true;
            root.ContinueFromTitle();
            yield return null;
            root.StartStage("1-1");
            yield return null;
            yield return null;
            Assert.AreEqual(AppState.Playing, root.CurrentState);
            root.OpenPause();
            yield return null;
            root.RetryFromPause();
            yield return null;
            yield return null;
            Assert.AreEqual(AppState.Playing, root.CurrentState);
            Assert.IsFalse(root.Overlay.IsPauseOpen);
            Assert.AreEqual("1-1", root.Gameplay.Definition.StageId);
        }

        [UnityTest]
        public IEnumerator Hud_Timer_Is_Top_Center_Anchored()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.Preferences.openingSeen = true;
            root.ContinueFromTitle();
            yield return null;
            root.StartStage("1-1");
            yield return null;
            yield return null;
            var timer = GameObject.Find("TimerLabel");
            Assert.IsNotNull(timer);
            var rt = timer.GetComponent<RectTransform>();
            Assert.AreEqual(0.5f, rt.anchorMin.x, 0.01f);
            Assert.AreEqual(1f, rt.anchorMin.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator Hud_Side_Panels_Are_Mirrored_And_Content_Stays_Inside()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.Preferences.openingSeen = true;
            root.ContinueFromTitle();
            yield return null;
            root.StartStage("1-1");
            yield return null;
            yield return null;

            var stagePanel = GameObject.Find("StagePanel")?.GetComponent<RectTransform>();
            var goalPanel = GameObject.Find("GoalPanel")?.GetComponent<RectTransform>();
            Assert.IsNotNull(stagePanel);
            Assert.IsNotNull(goalPanel);
            Assert.AreEqual(stagePanel.rect.width, goalPanel.rect.width, 0.1f);
            Assert.AreEqual(stagePanel.rect.height, goalPanel.rect.height, 0.1f);
            Assert.AreEqual(stagePanel.anchoredPosition.y, goalPanel.anchoredPosition.y, 0.1f);
            Assert.AreEqual(stagePanel.anchoredPosition.x, -goalPanel.anchoredPosition.x, 0.1f);

            AssertInside(stagePanel, GameObject.Find("StageLabel")?.GetComponent<RectTransform>());
            AssertInside(goalPanel, GameObject.Find("GoalLabel")?.GetComponent<RectTransform>());
            AssertInside(goalPanel, GameObject.Find("PauseButton")?.GetComponent<RectTransform>());
        }

        private static IEnumerator Capture(string name)
        {
            var projectRoot = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, ".."));
            var dir = System.IO.Path.Combine(projectRoot, "Temp", "ShadowGardenQA");
            System.IO.Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();
            // CaptureScreenshot writes asynchronously; give the file a moment.
            yield return null;
        }

        private static void AssertInside(RectTransform panel, RectTransform child)
        {
            Assert.IsNotNull(child);
            Assert.IsTrue(child.IsChildOf(panel), $"{child.name} must be parented under {panel.name}.");
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, child);
            const float tolerance = 1f;
            Assert.GreaterOrEqual(bounds.min.x, panel.rect.xMin - tolerance, child.name);
            Assert.LessOrEqual(bounds.max.x, panel.rect.xMax + tolerance, child.name);
            Assert.GreaterOrEqual(bounds.min.y, panel.rect.yMin - tolerance, child.name);
            Assert.LessOrEqual(bounds.max.y, panel.rect.yMax + tolerance, child.name);
        }

        private static IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }
    }
}
