using ShadowGarden.Core;
using ShadowGarden.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowGarden.Presentation
{
    /// <summary>Board-safe production HUD with dedicated icon sprites and compact 18x8 layout.</summary>
    public sealed class MainPlayHud : MonoBehaviour
    {
        private static readonly Vector2 TopSidePanelSize = new(500f, 76f);

        [SerializeField] private TextMeshProUGUI stageLabel;
        [SerializeField] private TextMeshProUGUI timerLabel;
        [SerializeField] private TextMeshProUGUI goalLabel;
        [SerializeField] private TextMeshProUGUI warningLabel;
        [SerializeField] private TextMeshProUGUI progressLabel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject root;

        private Image _stagePanel;
        private Image _timerPanel;
        private Image _goalPanel;
        private Image _progressPanel;
        private Image _goalIcon;
        private Image[] _progressNodes;
        private MainCompositionRoot _main;
        private bool _reduceMotion;
        private float _blinkTimer;
        private bool _blinkOn = true;
        private bool _compact;

        public void Bind(MainCompositionRoot main)
        {
            _main = main;
            ApplyPreferences();
        }

        public void ApplyPreferences()
        {
            _reduceMotion = _main?.Save?.Preferences != null && _main.Save.Preferences.reduceMotion;
        }

        public void EnsureBuilt(Transform parent)
        {
            if (root != null)
            {
                WirePause();
                return;
            }

            root = new GameObject("PlayHud", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            UiFactory.StretchFull(root);
            var skin = PresentationAssetLibrary.Catalog;

            _stagePanel = CreateAnchoredPanel("StagePanel", new Vector2(0f, 1f),
                new Vector2(UiTheme.SafeMargin, -UiTheme.SafeMargin), TopSidePanelSize, new Vector2(0f, 1f));
            _timerPanel = CreateAnchoredPanel("TimerPanel", new Vector2(0.5f, 1f),
                new Vector2(0f, -UiTheme.SafeMargin), new Vector2(238f, 74f), new Vector2(0.5f, 1f));
            _goalPanel = CreateAnchoredPanel("GoalPanel", new Vector2(1f, 1f),
                new Vector2(-UiTheme.SafeMargin, -UiTheme.SafeMargin), TopSidePanelSize, new Vector2(1f, 1f));
            _progressPanel = CreateAnchoredPanel("ProgressPanel", new Vector2(1f, 0f),
                new Vector2(-UiTheme.SafeMargin, UiTheme.SafeMargin), new Vector2(430f, 76f), new Vector2(1f, 0f));

            stageLabel = CreateAnchorLabel(_stagePanel.transform, "StageLabel", new Vector2(0f, 0.5f),
                new Vector2(22f, 0f), new Vector2(455f, 52f),
                TextAlignmentOptions.MidlineLeft, UiTheme.HudFont, true);
            timerLabel = CreateAnchorLabel(root.transform, "TimerLabel", new Vector2(0.5f, 1f),
                new Vector2(0f, -UiTheme.SafeMargin - 12f), new Vector2(210f, 52f),
                TextAlignmentOptions.Top, UiTheme.TimerFont, true);
            goalLabel = CreateAnchorLabel(_goalPanel.transform, "GoalLabel", new Vector2(1f, 0.5f),
                new Vector2(-78f, 0f), new Vector2(330f, 52f),
                TextAlignmentOptions.MidlineRight, UiTheme.SubtitleFont, true);
            progressLabel = CreateAnchorLabel(root.transform, "ProgressLabel", new Vector2(1f, 0f),
                new Vector2(-UiTheme.SafeMargin - 22f, UiTheme.SafeMargin + 14f), new Vector2(202f, 48f),
                TextAlignmentOptions.BottomRight, UiTheme.BodyFontMin + 1, false);
            warningLabel = CreateAnchorLabel(root.transform, "WarningLabel", new Vector2(0.5f, 1f),
                new Vector2(0f, -112f), new Vector2(620f, 42f), TextAlignmentOptions.Top,
                UiTheme.BodyFontMin + 4, true);
            warningLabel.gameObject.SetActive(false);

            _goalIcon = UiFactory.EnsureIcon(_goalPanel.transform, "GoalIcon", skin?.iconDoor,
                new Vector2(36f, 36f));
            var goalIconRt = _goalIcon.rectTransform;
            goalIconRt.anchorMin = goalIconRt.anchorMax = new Vector2(0f, 0.5f);
            goalIconRt.pivot = new Vector2(0.5f, 0.5f);
            goalIconRt.anchoredPosition = new Vector2(30f, 0f);
            _progressNodes = new Image[4];
            for (var i = 0; i < 4; i++)
            {
                _progressNodes[i] = UiFactory.EnsureIcon(_progressPanel.transform, $"ProgressNode_{i + 1}",
                    skin?.iconLock, new Vector2(30f, 30f));
                var nodeRt = _progressNodes[i].rectTransform;
                nodeRt.anchorMin = nodeRt.anchorMax = new Vector2(0f, 0.5f);
                nodeRt.pivot = new Vector2(0.5f, 0.5f);
                nodeRt.anchoredPosition = new Vector2(34f + i * 42f, 0f);
            }

            pauseButton = UiFactory.CreateButton(_goalPanel.transform, "PauseButton", string.Empty, Vector2.zero,
                () => _main?.OpenPause(), width: 56f, height: 56f);
            var pauseRt = pauseButton.GetComponent<RectTransform>();
            pauseRt.anchorMin = pauseRt.anchorMax = new Vector2(1f, 0.5f);
            pauseRt.pivot = new Vector2(1f, 0.5f);
            pauseRt.anchoredPosition = new Vector2(-10f, 0f);
            UiFactory.EnsureIcon(pauseButton.transform, "PauseIcon", skin?.iconPause, new Vector2(24f, 24f));
            WirePause();
        }

        public void SetVisible(bool visible)
        {
            if (root != null) root.SetActive(visible);
        }

        public void Render(StageDefinition stage, StageRuntimeState state)
        {
            if (stage == null || state == null || stageLabel == null) return;
            ApplyCompactLayout(stage.BoardSize);
            var world = MockupPalette.WorldName(stage.StageId);
            stageLabel.text = $"{stage.StageId}   {world}";
            goalLabel.text = stage.ClearGoalType == ClearGoalType.NightFlower ? "밤꽃에 도달하기" : "출구로 나가기";
            timerLabel.text = FormatTimer(state.RemainingMilliseconds);
            UpdateGoalIcon(stage.ClearGoalType);
            UpdateProgress(stage);

            var ms = state.RemainingMilliseconds;
            if (ms <= 10_000)
                ApplyTimerWarning(UiTheme.Coral, "곧 어둠이 닥쳐옵니다", !_reduceMotion);
            else if (ms <= 30_000)
                ApplyTimerWarning(UiTheme.Brass, "남은 시간 30초", false);
            else
            {
                timerLabel.color = UiTheme.Ivory;
                warningLabel.gameObject.SetActive(false);
            }
        }

        public void ShowTransientWarning(string message, Color color)
        {
            if (warningLabel == null) return;
            warningLabel.gameObject.SetActive(true);
            warningLabel.text = message;
            warningLabel.color = color;
        }

        private Image CreateAnchoredPanel(
            string name,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Vector2 pivot)
        {
            var image = UiFactory.CreatePanel(root.transform, name, new Color(1f, 1f, 1f, 0.94f), size, position);
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            return image;
        }

        private void UpdateGoalIcon(ClearGoalType type)
        {
            if (_goalIcon == null) return;
            var catalog = PresentationAssetLibrary.Catalog;
            _goalIcon.sprite = type == ClearGoalType.NightFlower ? catalog?.iconFlower : catalog?.iconDoor;
        }

        private void UpdateProgress(StageDefinition stage)
        {
            if (!TryParse(stage.StageId, out var world, out var slot))
            {
                progressLabel.text = string.Empty;
                return;
            }

            var catalog = PresentationAssetLibrary.Catalog;
            for (var i = 1; i <= 4; i++)
            {
                var image = _progressNodes[i - 1];
                if (image == null) continue;
                image.sprite = i < slot ? catalog?.iconCheck : i == slot ? catalog?.iconFlower : catalog?.iconLock;
                image.color = i < slot ? UiTheme.Mint : i == slot ? UiTheme.Brass : new Color(1f, 1f, 1f, 0.42f);
            }

            var best = ProgressTimeFormat.Incomplete;
            if (_main?.Save?.Progress != null)
                best = ProgressTimeFormat.FormatBestClear(_main.Save.Progress, stage.StageId);
            progressLabel.text = best == ProgressTimeFormat.Incomplete ? $"WORLD {world}" : $"BEST  {best}";
        }

        private void ApplyCompactLayout(GridSize size)
        {
            var wantCompact = size.Width > 12 || size.Height > 6;
            if (wantCompact == _compact || stageLabel == null)
            {
                _compact = wantCompact;
                return;
            }

            _compact = wantCompact;
            var margin = _compact ? 20f : UiTheme.SafeMargin;
            stageLabel.fontSize = _compact ? 21f : UiTheme.HudFont;
            timerLabel.fontSize = _compact ? 30f : UiTheme.TimerFont;
            goalLabel.fontSize = _compact ? 19f : UiTheme.SubtitleFont;
            progressLabel.fontSize = _compact ? UiTheme.BodyFontMin : UiTheme.BodyFontMin + 1;
            SetTopLayout(margin);
        }

        private void SetTopLayout(float margin)
        {
            SetAnchor(_stagePanel.rectTransform, new Vector2(0f, 1f), new Vector2(margin, -margin));
            SetAnchor(_timerPanel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -margin));
            SetAnchor(_goalPanel.rectTransform, new Vector2(1f, 1f), new Vector2(-margin, -margin));
            SetAnchor(timerLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -margin - 12f));
        }

        private void ApplyTimerWarning(Color color, string message, bool blink)
        {
            warningLabel.gameObject.SetActive(true);
            warningLabel.text = message;
            warningLabel.color = color;
            if (blink)
            {
                _blinkTimer += Time.unscaledDeltaTime;
                if (_blinkTimer >= 0.5f)
                {
                    _blinkTimer = 0f;
                    _blinkOn = !_blinkOn;
                }
                timerLabel.color = _blinkOn ? color : UiTheme.Ivory;
            }
            else timerLabel.color = color;
        }

        private void WirePause()
        {
            if (pauseButton == null) return;
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(() => _main?.OpenPause());
        }

        private static string FormatTimer(long remainingMs)
        {
            var total = Mathf.Max(0, Mathf.CeilToInt(remainingMs / 1000f));
            return $"{total / 60:0}:{total % 60:00}";
        }

        private static bool TryParse(string stageId, out int world, out int slot)
        {
            world = 0;
            slot = 0;
            if (string.IsNullOrWhiteSpace(stageId)) return false;
            var parts = stageId.Split('-');
            return parts.Length == 2 && int.TryParse(parts[0], out world) && int.TryParse(parts[1], out slot);
        }

        private static void SetAnchor(RectTransform rt, Vector2 anchor, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
        }

        private static TextMeshProUGUI CreateAnchorLabel(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 anchoredPos,
            Vector2 size,
            TextAlignmentOptions align,
            int fontSize,
            bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.alignment = align;
            text.fontSize = Mathf.Max(fontSize, UiTheme.BodyFontMin);
            text.color = UiTheme.Ivory;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            UiTypography.Apply(text, bold);
            return text;
        }
    }
}
