using ShadowGarden.Core;
using ShadowGarden.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Production play HUD per UI/UX §21–24 / §39.
    /// Left: stage+world · Center: timer · Right: goal+pause · Bottom-right: nodes+BEST.
    /// </summary>
    public sealed class MainPlayHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI stageLabel;
        [SerializeField] private TextMeshProUGUI timerLabel;
        [SerializeField] private TextMeshProUGUI goalLabel;
        [SerializeField] private TextMeshProUGUI warningLabel;
        [SerializeField] private TextMeshProUGUI progressLabel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject root;

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

            // §22: 좌상단 월드·스테이지·목표 아이콘 / 상단 중앙 타이머 / 우상단 목표·일시정지
            stageLabel = CreateAnchorLabel(root.transform, "StageLabel",
                new Vector2(0f, 1f), new Vector2(UiTheme.SafeMargin, -UiTheme.SafeMargin),
                new Vector2(560f, 48f), TextAlignmentOptions.TopLeft, UiTheme.HudFont, true);
            timerLabel = CreateAnchorLabel(root.transform, "TimerLabel",
                new Vector2(0.5f, 1f), new Vector2(0f, -UiTheme.SafeMargin),
                new Vector2(220f, 56f), TextAlignmentOptions.Top, UiTheme.TimerFont, true);
            goalLabel = CreateAnchorLabel(root.transform, "GoalLabel",
                new Vector2(1f, 1f), new Vector2(-UiTheme.SafeMargin - 72f, -UiTheme.SafeMargin),
                new Vector2(360f, 48f), TextAlignmentOptions.TopRight, UiTheme.SubtitleFont, false);
            warningLabel = CreateAnchorLabel(root.transform, "WarningLabel",
                new Vector2(0.5f, 1f), new Vector2(0f, -96f),
                new Vector2(640f, 40f), TextAlignmentOptions.Top, UiTheme.BodyFontMin + 4, true);
            warningLabel.gameObject.SetActive(false);
            progressLabel = CreateAnchorLabel(root.transform, "ProgressLabel",
                new Vector2(1f, 0f), new Vector2(-UiTheme.SafeMargin, UiTheme.SafeMargin),
                new Vector2(420f, 48f), TextAlignmentOptions.BottomRight, UiTheme.BodyFontMin + 2, false);

            pauseButton = UiFactory.CreateButton(root.transform, "PauseButton", "Ⅱ",
                Vector2.zero, () => _main?.OpenPause(), width: 56f, height: 48f);
            var prt = pauseButton.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(1f, 1f);
            prt.pivot = new Vector2(1f, 1f);
            prt.anchoredPosition = new Vector2(-UiTheme.SafeMargin, -UiTheme.SafeMargin);
            WirePause();
        }

        public void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }
        }

        public void Render(StageDefinition stage, StageRuntimeState state)
        {
            if (stage == null || state == null || stageLabel == null)
            {
                return;
            }

            ApplyCompactLayout(stage.BoardSize);
            var goalIcon = stage.ClearGoalType == ClearGoalType.NightFlower ? "❀" : "⌂";
            stageLabel.text = $"{stage.StageId}  |  {MockupPalette.WorldName(stage.StageId)}  {goalIcon}";
            goalLabel.text = $"목표  ·  {goalIcon} {MockupPalette.GoalLabel(stage.ClearGoalType)}";
            timerLabel.text = FormatTimer(state.RemainingMilliseconds);
            progressLabel.text = BuildProgress(stage);

            var ms = state.RemainingMilliseconds;
            if (ms <= 10_000)
            {
                ApplyTimerWarning(UiTheme.Coral, "남은 시간 10초!", blink: !_reduceMotion);
            }
            else if (ms <= 30_000)
            {
                ApplyTimerWarning(UiTheme.Brass, "남은 시간 30초", blink: false);
            }
            else
            {
                timerLabel.color = UiTheme.Ivory;
                warningLabel.gameObject.SetActive(false);
            }
        }

        public void ShowTransientWarning(string message, Color color)
        {
            if (warningLabel == null)
            {
                return;
            }

            warningLabel.gameObject.SetActive(true);
            warningLabel.text = message;
            warningLabel.color = color;
        }

        private void ApplyCompactLayout(GridSize size)
        {
            // §39: 18×8는 HUD를 더 얇게 — 보드 가림 방지.
            var wantCompact = size.Width >= 16 || size.Height >= 8;
            if (wantCompact == _compact || stageLabel == null)
            {
                _compact = wantCompact;
                return;
            }

            _compact = wantCompact;
            var margin = _compact ? 20f : UiTheme.SafeMargin;
            var stageSize = _compact ? UiTheme.BodyFontMin + 6 : UiTheme.HudFont;
            var timerSize = _compact ? UiTheme.SubtitleFont + 4 : UiTheme.TimerFont;
            var goalSize = _compact ? UiTheme.BodyFontMin + 4 : UiTheme.SubtitleFont;

            stageLabel.fontSize = stageSize;
            timerLabel.fontSize = timerSize;
            goalLabel.fontSize = goalSize;
            progressLabel.fontSize = UiTheme.BodyFontMin + (_compact ? 0 : 2);

            SetAnchor(stageLabel.rectTransform, new Vector2(0f, 1f), new Vector2(margin, -margin));
            SetAnchor(timerLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -margin));
            SetAnchor(goalLabel.rectTransform, new Vector2(1f, 1f), new Vector2(-margin - 72f, -margin));
            if (pauseButton != null)
            {
                var prt = pauseButton.GetComponent<RectTransform>();
                prt.anchoredPosition = new Vector2(-margin, -margin);
            }
        }

        private static void SetAnchor(RectTransform rt, Vector2 anchor, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
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
            else
            {
                timerLabel.color = color;
            }
        }

        private string BuildProgress(StageDefinition stage)
        {
            if (!TryParse(stage.StageId, out var world, out var slot))
            {
                return string.Empty;
            }

            var parts = new string[4];
            for (var i = 1; i <= 4; i++)
            {
                if (i < slot)
                {
                    parts[i - 1] = "●";
                }
                else if (i == slot)
                {
                    parts[i - 1] = "◎";
                }
                else
                {
                    parts[i - 1] = "○";
                }
            }

            var best = ProgressTimeFormat.Incomplete;
            var progress = _main?.Save?.Progress;
            if (progress != null)
            {
                best = ProgressTimeFormat.FormatBestClear(progress, stage.StageId);
            }

            var bestPart = best == ProgressTimeFormat.Incomplete ? string.Empty : $"  BEST {best}";
            return $"W{world}  {string.Join(" ", parts)}{bestPart}";
        }

        private static bool TryParse(string stageId, out int world, out int slot)
        {
            world = 0;
            slot = 0;
            if (string.IsNullOrWhiteSpace(stageId))
            {
                return false;
            }

            var parts = stageId.Split('-');
            return parts.Length == 2 && int.TryParse(parts[0], out world) && int.TryParse(parts[1], out slot);
        }

        private void WirePause()
        {
            if (pauseButton == null)
            {
                return;
            }

            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(() => _main?.OpenPause());
        }

        private static string FormatTimer(long remainingMs)
        {
            var total = Mathf.Max(0, Mathf.CeilToInt(remainingMs / 1000f));
            var m = total / 60;
            var s = total % 60;
            return $"{m:0}:{s:00}";
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
            UiTypography.Apply(text, bold);
            return text;
        }
    }
}
