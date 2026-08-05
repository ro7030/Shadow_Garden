using ShadowGarden.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Playing overlays: pause (screen button only — no Esc), settings, focus-return.
    /// </summary>
    public sealed class MainOverlayController : MonoBehaviour
    {
        private MainCompositionRoot _main;
        private GameObject _root;
        private GameObject _dimmer;
        private GameObject _pausePanel;
        private GameObject _settingsPanel;
        private GameObject _focusPanel;
        private TextMeshProUGUI _focusLabel;
        private TextMeshProUGUI _controlsLabel;
        private Slider _bgmSlider;
        private Slider _sfxSlider;
        private Toggle _reduceMotionToggle;
        private Toggle _fullscreenToggle;
        private bool _paused;
        private bool _settingsOpen;
        private bool _awaitingFocusClick;
        private AppState _settingsReturnState = AppState.Title;

        public bool IsPauseOpen => _paused;
        public bool IsSettingsOpen => _settingsOpen;
        public bool IsFocusOverlayVisible => _awaitingFocusClick;

        public void Bind(MainCompositionRoot main, Transform canvas)
        {
            _main = main;
            EnsureBuilt(canvas);
            HideAll();
        }

        public void OpenPause()
        {
            if (_main == null || _main.CurrentState != AppState.Playing)
            {
                return;
            }

            _paused = true;
            _settingsOpen = false;
            _main.SetPlayPaused(true);
            ShowOnly(_pausePanel);
            var resume = _pausePanel.transform.Find("ResumeButton")?.GetComponent<Button>();
            UiFactory.Select(resume);
        }

        public void ClosePause(bool resumePlay)
        {
            _paused = false;
            _settingsOpen = false;
            HideAll();
            if (resumePlay)
            {
                _main?.SetPlayPaused(false);
            }
        }

        public void OpenSettings(AppState returnState)
        {
            _settingsReturnState = returnState;
            _settingsOpen = true;
            if (_paused)
            {
                _pausePanel?.SetActive(false);
            }

            ShowOnly(_settingsPanel);
            SyncSettingsWidgets();
            var close = _settingsPanel.transform.Find("CloseButton")?.GetComponent<Button>();
            UiFactory.Select(close);
        }

        public void CloseSettings()
        {
            _settingsOpen = false;
            PersistSettings();
            if (_paused)
            {
                ShowOnly(_pausePanel);
                var resume = _pausePanel.transform.Find("ResumeButton")?.GetComponent<Button>();
                UiFactory.Select(resume);
            }
            else
            {
                HideAll();
            }
        }

        public void ShowFocusLost()
        {
            if (_main == null || _main.CurrentState != AppState.Playing || _paused)
            {
                return;
            }

            _awaitingFocusClick = true;
            _main.SetPlayPaused(true);
            ShowOnly(_focusPanel);
        }

        public void TryDismissFocusOverlay()
        {
            if (!_awaitingFocusClick)
            {
                return;
            }

            _awaitingFocusClick = false;
            HideAll();
            if (!_paused)
            {
                _main?.SetPlayPaused(false);
            }
        }

        public void HideAllForStateChange()
        {
            _paused = false;
            _settingsOpen = false;
            _awaitingFocusClick = false;
            HideAll();
        }

        private void EnsureBuilt(Transform canvas)
        {
            if (_root != null || canvas == null)
            {
                return;
            }

            _root = new GameObject("OverlayLayer", typeof(RectTransform));
            _root.transform.SetParent(canvas, false);
            UiFactory.StretchFull(_root);
            _root.transform.SetAsLastSibling();

            _dimmer = new GameObject("Dimmer", typeof(RectTransform), typeof(Image), typeof(Button));
            _dimmer.transform.SetParent(_root.transform, false);
            UiFactory.StretchFull(_dimmer);
            var dimImage = _dimmer.GetComponent<Image>();
            dimImage.color = new Color(0.02f, 0.04f, 0.08f, 0.72f);
            var dimButton = _dimmer.GetComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(OnDimmerClicked);

            _pausePanel = BuildPause();
            _settingsPanel = BuildSettings();
            _focusPanel = BuildFocus();
        }

        private GameObject BuildPause()
        {
            var panel = UiFactory.CreatePanel(_root.transform, "PausePanel", UiTheme.Panel,
                new Vector2(480f, 420f), Vector2.zero).gameObject;
            UiFactory.CreateLabel(panel.transform, "Title", "일시정지",
                new Vector2(0f, 150f), new Vector2(400f, 48f), UiTheme.TitleFont * 0.6f, true);
            UiFactory.CreateButton(panel.transform, "ResumeButton", "계속하기",
                new Vector2(0f, 60f), () => ClosePause(true));
            UiFactory.CreateButton(panel.transform, "RetryButton", "다시 도전",
                new Vector2(0f, 0f), () => _main?.RetryFromPause());
            UiFactory.CreateButton(panel.transform, "WorldMapButton", "레벨 선택",
                new Vector2(0f, -60f), () =>
                {
                    ClosePause(false);
                    _main?.ReturnToWorldMap();
                });
            UiFactory.CreateButton(panel.transform, "SettingsButton", "설정",
                new Vector2(0f, -120f), () => OpenSettings(AppState.Playing));
            return panel;
        }

        private GameObject BuildSettings()
        {
            var panel = UiFactory.CreatePanel(_root.transform, "SettingsPanel", UiTheme.Panel,
                new Vector2(640f, 620f), Vector2.zero).gameObject;
            UiFactory.CreateLabel(panel.transform, "Title", "설정",
                new Vector2(0f, 250f), new Vector2(520f, 48f), UiTheme.TitleFont * 0.55f, true);

            UiFactory.CreateLabel(panel.transform, "BgmLabel", "BGM",
                new Vector2(-200f, 170f), new Vector2(120f, 32f), UiTheme.BodyFontMin + 4, false,
                TextAlignmentOptions.MidlineLeft);
            _bgmSlider = CreateSlider(panel.transform, "BgmSlider", new Vector2(40f, 170f));
            _bgmSlider.value = 0.7f;

            UiFactory.CreateLabel(panel.transform, "SfxLabel", "효과음",
                new Vector2(-200f, 110f), new Vector2(120f, 32f), UiTheme.BodyFontMin + 4, false,
                TextAlignmentOptions.MidlineLeft);
            _sfxSlider = CreateSlider(panel.transform, "SfxSlider", new Vector2(40f, 110f));
            _sfxSlider.value = 0.8f;

            _fullscreenToggle = CreateToggle(panel.transform, "FullscreenToggle",
                new Vector2(-220f, 40f));
            UiFactory.CreateLabel(panel.transform, "FullscreenLabel", "전체화면",
                new Vector2(20f, 40f), new Vector2(360f, 44f), UiTheme.BodyFontMin + 2, false,
                TextAlignmentOptions.MidlineLeft);

            _reduceMotionToggle = CreateToggle(panel.transform, "ReduceMotionToggle",
                new Vector2(-220f, -20f));
            UiFactory.CreateLabel(panel.transform, "ReduceMotionLabel", "모션 완화 (점멸·맥동 제거)",
                new Vector2(40f, -20f), new Vector2(400f, 44f), UiTheme.BodyFontMin + 2, false,
                TextAlignmentOptions.MidlineLeft);

            _controlsLabel = UiFactory.CreateLabel(panel.transform, "ControlsLabel",
                "조작 확인\nWASD 이동 · Q/E 태양등 회전 · R 다시 도전\n방향키·Enter 메뉴 · 일시정지는 화면 버튼",
                new Vector2(0f, -110f), new Vector2(560f, 100f), UiTheme.BodyFontMin + 2, false);

            UiFactory.CreateButton(panel.transform, "ReplayOpeningButton", "오프닝 다시 보기",
                new Vector2(0f, -190f), () =>
                {
                    PersistSettings();
                    HideAll();
                    _paused = false;
                    _settingsOpen = false;
                    _main?.ReplayOpening();
                });
            UiFactory.CreateButton(panel.transform, "CloseButton", "닫기",
                new Vector2(0f, -260f), CloseSettings);
            return panel;
        }

        private GameObject BuildFocus()
        {
            var panel = UiFactory.CreatePanel(_root.transform, "FocusPanel", UiTheme.Panel,
                new Vector2(560f, 220f), Vector2.zero).gameObject;
            _focusLabel = UiFactory.CreateLabel(panel.transform, "Body",
                "게임 화면을 클릭해 계속합니다.\n포커스가 돌아오면 안전하게 이어서 플레이합니다.",
                new Vector2(0f, 20f), new Vector2(500f, 100f), UiTheme.SubtitleFont, false);
            UiFactory.CreateButton(panel.transform, "ResumeClickButton", "클릭하여 복귀",
                new Vector2(0f, -60f), TryDismissFocusOverlay);
            return panel;
        }

        private static Toggle CreateToggle(Transform parent, string name, Vector2 anchored)
        {
            var toggleGo = new GameObject(name, typeof(RectTransform), typeof(Toggle), typeof(Image));
            toggleGo.transform.SetParent(parent, false);
            var trt = toggleGo.GetComponent<RectTransform>();
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(UiTheme.ButtonMinHeight, UiTheme.ButtonMinHeight);
            trt.anchoredPosition = anchored;
            var background = toggleGo.GetComponent<Image>();
            background.sprite = PresentationAssetLibrary.Catalog?.buttonSecondary;
            background.type = background.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            background.color = Color.white;

            var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGo.transform.SetParent(toggleGo.transform, false);
            var checkRt = checkGo.GetComponent<RectTransform>();
            checkRt.anchorMin = checkRt.anchorMax = new Vector2(0.5f, 0.5f);
            checkRt.sizeDelta = new Vector2(28f, 28f);
            var check = checkGo.GetComponent<Image>();
            check.sprite = PresentationAssetLibrary.Catalog?.iconCheck;
            check.color = UiTheme.Mint;
            check.preserveAspect = true;
            check.raycastTarget = false;

            var toggle = toggleGo.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = check;
            var colors = toggle.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 1f, 0.98f, 1f);
            colors.selectedColor = new Color(0.92f, 1f, 0.98f, 1f);
            colors.pressedColor = new Color(0.82f, 0.92f, 0.9f, 1f);
            toggle.colors = colors;
            return toggle;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 anchored)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(280f, 36f);
            rt.anchoredPosition = anchored;

            var backgroundGo = new GameObject("Track", typeof(RectTransform), typeof(Image));
            backgroundGo.transform.SetParent(go.transform, false);
            var backgroundRt = backgroundGo.GetComponent<RectTransform>();
            backgroundRt.anchorMin = new Vector2(0f, 0.5f);
            backgroundRt.anchorMax = new Vector2(1f, 0.5f);
            backgroundRt.offsetMin = new Vector2(0f, -7f);
            backgroundRt.offsetMax = new Vector2(0f, 7f);
            var background = backgroundGo.GetComponent<Image>();
            background.sprite = PresentationAssetLibrary.Catalog?.buttonPrimary;
            background.type = background.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            background.color = new Color(1f, 1f, 1f, 0.78f);

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRt.offsetMin = new Vector2(5f, -5f);
            fillAreaRt.offsetMax = new Vector2(-5f, 5f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            UiFactory.StretchFull(fill);
            var fillImage = fill.GetComponent<Image>();
            fillImage.color = UiTheme.Mint;
            fillImage.raycastTarget = false;

            var handleArea = new GameObject("HandleSlideArea", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(10f, 0f);
            handleAreaRt.offsetMax = new Vector2(-10f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(28f, 34f);
            var handleImage = handle.GetComponent<Image>();
            handleImage.sprite = PresentationAssetLibrary.Catalog?.keyCap;
            handleImage.type = handleImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            handleImage.color = UiTheme.Ivory;

            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private void SyncSettingsWidgets()
        {
            var prefs = _main?.Save?.Preferences;
            if (prefs == null)
            {
                return;
            }

            if (_bgmSlider != null)
            {
                _bgmSlider.SetValueWithoutNotify(prefs.bgmVolume);
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.SetValueWithoutNotify(prefs.sfxVolume);
            }

            if (_reduceMotionToggle != null)
            {
                _reduceMotionToggle.SetIsOnWithoutNotify(prefs.reduceMotion);
            }

            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            }
        }

        private void PersistSettings()
        {
            var prefs = _main?.Save?.Preferences;
            if (prefs == null)
            {
                return;
            }

            if (_bgmSlider != null)
            {
                prefs.bgmVolume = _bgmSlider.value;
            }

            if (_sfxSlider != null)
            {
                prefs.sfxVolume = _sfxSlider.value;
            }

            if (_reduceMotionToggle != null)
            {
                prefs.reduceMotion = _reduceMotionToggle.isOn;
            }

            if (_fullscreenToggle != null)
            {
                Screen.fullScreen = _fullscreenToggle.isOn;
            }

            _main.Save.TrySavePreferences();
            _main.ApplyUiPreferences();
        }

        private void OnDimmerClicked()
        {
            if (_awaitingFocusClick)
            {
                TryDismissFocusOverlay();
            }
        }

        private void ShowOnly(GameObject panel)
        {
            _root.SetActive(true);
            _dimmer.SetActive(true);
            _pausePanel.SetActive(panel == _pausePanel);
            _settingsPanel.SetActive(panel == _settingsPanel);
            _focusPanel.SetActive(panel == _focusPanel);
        }

        private void HideAll()
        {
            if (_root == null)
            {
                return;
            }

            _pausePanel?.SetActive(false);
            _settingsPanel?.SetActive(false);
            _focusPanel?.SetActive(false);
            _dimmer?.SetActive(false);
            _root.SetActive(false);
        }
    }
}
