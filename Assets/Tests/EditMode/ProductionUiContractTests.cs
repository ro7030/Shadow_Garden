using NUnit.Framework;
using ShadowGarden.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowGarden.Tests.EditMode
{
    public class ProductionUiContractTests
    {
        [Test]
        public void Theme_Tokens_Match_Ux_Spec()
        {
            Assert.AreEqual(44f, UiTheme.ButtonMinHeight);
            Assert.AreEqual(3f, UiTheme.FocusOutline);
            Assert.AreEqual(16, UiTheme.BodyFontMin);
            Assert.AreEqual(1920f, UiTheme.ReferenceWidth);
            Assert.AreEqual(1080f, UiTheme.ReferenceHeight);
            Assert.AreEqual(0.5f, UiTheme.Match);
            Assert.AreEqual(new Color(0x78 / 255f, 0xCD / 255f, 0xB8 / 255f, 1f).r, UiTheme.Mint.r, 0.01f);
        }

        [Test]
        public void Preference_Defaults_Match_Ux_Mix()
        {
            var prefs = ShadowGarden.Infrastructure.UiPreferencesData.CreateDefault();
            Assert.AreEqual(0.7f, prefs.bgmVolume, 0.001f);
            Assert.AreEqual(0.8f, prefs.sfxVolume, 0.001f);
        }

        [Test]
        public void Presentation_Timings_Match_Ux_Contracts()
        {
            Assert.AreEqual(1f, PresentationTiming.OpeningSkipHoldSeconds, 0.01f);
            Assert.AreEqual(1.5f, PresentationTiming.NightFlowerBloomSeconds, 0.01f);
            Assert.AreEqual(0.55f, PresentationTiming.OverlapSinkSeconds, 0.01f);
            Assert.AreEqual(0.65f, PresentationTiming.TimeVacuumSeconds, 0.01f);
            Assert.AreEqual(0.35f, PresentationTiming.CliffApproachCells, 0.01f);
            Assert.AreEqual(0.5f, PresentationTiming.CliffFallSeconds, 0.01f);
            Assert.AreEqual(0.45f, PresentationTiming.DoorOpenSeconds, 0.01f);
            Assert.AreEqual(0.35f, PresentationTiming.GoalPassSeconds, 0.01f);
            Assert.AreEqual(10f, PresentationTiming.EndingBeatSeconds, 0.01f);
            Assert.AreEqual(10f,
                PresentationTiming.EndingWorldRecoverSeconds + PresentationTiming.EndingCelebrateSeconds, 0.01f);
        }

        [Test]
        public void MainCompositionRoot_Exposes_Pause_Retry()
        {
            Assert.IsNotNull(typeof(MainCompositionRoot).GetMethod("RetryFromPause"));
        }

        [Test]
        public void Secondary_Button_Tint_Is_Visible_On_White()
        {
            var go = new GameObject("SecondaryTintButton", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var button = go.GetComponent<Button>();
                UiFactory.ApplyButtonColorTint(button, secondary: true);
                Assert.Less(button.colors.highlightedColor.maxColorComponent, 1f);
                Assert.Less(button.colors.selectedColor.maxColorComponent, 1f);
                Assert.Less(button.colors.pressedColor.maxColorComponent, 1f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ConfigureCanvas_Applies_Overlay_Scaler()
        {
            var go = new GameObject("Canvas");
            try
            {
                var canvas = go.AddComponent<Canvas>();
                var scaler = UiFactory.ConfigureCanvas(canvas);
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
                Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
                Assert.AreEqual(0.5f, scaler.matchWidthOrHeight);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Main_Presentation_Types_Do_Not_Use_OnGUI_Methods()
        {
            var types = new[]
            {
                typeof(MainPlayHud),
                typeof(MainFlowScreens),
                typeof(MainCompositionRoot),
                typeof(MainOverlayController)
            };

            foreach (var type in types)
            {
                var method = type.GetMethod("OnGUI",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                Assert.IsNull(method, type.Name + " must not define OnGUI");
            }
        }
    }
}
