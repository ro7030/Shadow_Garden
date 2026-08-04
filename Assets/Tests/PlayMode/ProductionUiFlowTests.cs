using System.Collections;
using NUnit.Framework;
using ShadowGarden.Presentation;
using ShadowGarden.Runtime;
using TMPro;
using UnityEngine;
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
            yield return Capture("UI_Play_18x8");
            Assert.AreEqual("3-4", root.Gameplay.Definition.StageId);
        }

        [UnityTest]
        public IEnumerator Title_Has_Replay_Opening_Button()
        {
            yield return LoadMain();
            Assert.IsNotNull(GameObject.Find("ReplayOpeningButton"));
        }

        private static IEnumerator Capture(string name)
        {
            var dir = System.IO.Path.Combine(Application.dataPath, "Screenshots", "Stage5");
            System.IO.Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();
            // CaptureScreenshot writes asynchronously; give the file a moment.
            yield return null;
        }

        private static IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }
    }
}
