using ShadowGarden.Core;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    public enum PlayModalKind
    {
        None = 0,
        GameOver = 1,
        Cleared = 2
    }

    /// <summary>
    /// Video-mockup HUD: stage badge, timer, goal, bottom controls, game-over/clear modals.
    /// </summary>
    public sealed class PlayHudPresenter : MonoBehaviour
    {
        private StageDefinition _stage;
        private StageRuntimeState _state;
        private string _hint = string.Empty;
        private PlayModalKind _modal = PlayModalKind.None;
        private GameOverCause _gameOverCause;
        private int _modalSelection;
        private long _clearElapsedMs;
        private bool _showDebug;

        public PlayModalKind Modal => _modal;
        public int ModalSelection => _modalSelection;

        public void Bind(StageDefinition stage)
        {
            _stage = stage;
            _modal = PlayModalKind.None;
            _hint = string.Empty;
            _modalSelection = 0;
        }

        public void Render(StageDefinition stage, StageRuntimeState state)
        {
            _stage = stage;
            _state = state;
        }

        public void SetHint(string hint) => _hint = hint ?? string.Empty;

        public void ShowGameOver(GameOverCause cause)
        {
            _modal = PlayModalKind.GameOver;
            _gameOverCause = cause;
            _modalSelection = 0;
        }

        public void ShowCleared(long remainingMilliseconds, int timeLimitSeconds)
        {
            _modal = PlayModalKind.Cleared;
            _clearElapsedMs = timeLimitSeconds * 1000L - remainingMilliseconds;
            if (_clearElapsedMs < 0)
            {
                _clearElapsedMs = 0;
            }

            _modalSelection = 0;
        }

        public void HideModal() => _modal = PlayModalKind.None;

        public void MoveSelection(int delta)
        {
            if (_modal == PlayModalKind.None)
            {
                return;
            }

            _modalSelection = (_modalSelection + delta + 2) % 2;
        }

        public void ToggleDebug() => _showDebug = !_showDebug;

        private void OnGUI()
        {
            if (_stage == null || _state == null)
            {
                return;
            }

            DrawTopBar();
            DrawBottomBar();
            if (!string.IsNullOrEmpty(_hint) && _modal == PlayModalKind.None)
            {
                DrawHintChip();
            }

            if (_modal == PlayModalKind.GameOver)
            {
                DrawGameOverModal();
            }
            else if (_modal == PlayModalKind.Cleared)
            {
                DrawClearModal();
            }

            if (_showDebug)
            {
                DrawDebugStrip();
            }
        }

        private void DrawTopBar()
        {
            var stageLabel = $"{_stage.StageId}  |  {MockupPalette.WorldName(_stage.StageId)}";
            var goalLabel = $"목표  ·  {MockupPalette.GoalLabel(_stage.ClearGoalType)}";
            var timer = FormatTimer(_state.RemainingMilliseconds);
            var timerColor = Color.white;
            if (_state.RemainingMilliseconds <= 10_000)
            {
                timerColor = MockupPalette.WarningCoral;
            }
            else if (_state.RemainingMilliseconds <= 30_000)
            {
                timerColor = MockupPalette.WarningAmber;
            }

            DrawPill(new Rect(24, 18, 260, 44), stageLabel);
            DrawPill(new Rect(Screen.width * 0.5f - 70, 18, 140, 44), timer, timerColor, 28);
            DrawPill(new Rect(Screen.width - 284, 18, 260, 44), goalLabel);
        }

        private void DrawBottomBar()
        {
            var text = "WASD 이동   ·   Q/E 90° 회전   ·   R 다시 도전   ·   [ ] 스테이지";
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                normal = { textColor = new Color(1f, 1f, 1f, 0.9f) }
            };
            UiTypography.ApplyToGuiStyle(style, bold: false);
            GUI.Label(new Rect(0, Screen.height - 48, Screen.width, 36), text, style);
        }

        private void DrawHintChip()
        {
            DrawPill(new Rect(24, Screen.height - 96, Mathf.Min(420, _hint.Length * 14 + 40), 36), _hint, Color.white, 15);
        }

        private void DrawGameOverModal()
        {
            DrawDim();
            var box = CenterBox(420, 280);
            GUI.Box(box, GUIContent.none);
            DrawPanel(box);

            var title = TitleStyle(28);
            GUI.Label(new Rect(box.x, box.y + 28, box.width, 40), "게임 오버", title);

            var body = BodyStyle(16);
            GUI.Label(
                new Rect(box.x + 28, box.y + 80, box.width - 56, 60),
                MockupPalette.GameOverReason(_gameOverCause, _stage.ClearGoalType),
                body);

            DrawModalButton(box, 0, "다시 도전", _modalSelection == 0);
            DrawModalButton(box, 1, "다음 스테이지", _modalSelection == 1);

            var footer = BodyStyle(13);
            footer.alignment = TextAnchor.MiddleCenter;
            footer.normal.textColor = new Color(0.25f, 0.28f, 0.35f, 0.9f);
            GUI.Label(
                new Rect(box.x, box.y + box.height - 36, box.width, 24),
                "방향키/WASD 선택  ·  Enter/Space 확인  ·  R 다시 도전",
                footer);
        }

        private void DrawClearModal()
        {
            DrawDim();
            var box = CenterBox(420, 260);
            DrawPanel(box);

            var title = TitleStyle(28);
            GUI.Label(new Rect(box.x, box.y + 28, box.width, 40), "스테이지 완료", title);

            var body = BodyStyle(18);
            body.alignment = TextAnchor.MiddleCenter;
            GUI.Label(
                new Rect(box.x, box.y + 90, box.width, 30),
                $"클리어 시간  {FormatClear(_clearElapsedMs)}",
                body);

            DrawModalButton(box, 0, "다음 스테이지", _modalSelection == 0);
            DrawModalButton(box, 1, "다시 도전", _modalSelection == 1);
        }

        private void DrawModalButton(Rect box, int index, string label, bool selected)
        {
            var y = box.y + 150 + index * 48;
            var rect = new Rect(box.x + 70, y, box.width - 140, 40);
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = selected ? MockupPalette.HudNavy : new Color(0.85f, 0.86f, 0.90f, 1f);
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = selected ? Color.white : new Color(0.2f, 0.22f, 0.28f) }
            };
            UiTypography.ApplyToGuiStyle(style, bold: true);
            GUI.Button(rect, label, style);
            GUI.backgroundColor = prev;
        }

        private void DrawDebugStrip()
        {
            var text =
                $"DEBUG  phase={_state.Phase}  pos={_state.PlayerPosition}  pause={_state.PauseReason}  F1 토글";
            GUI.Label(new Rect(24, 70, Screen.width - 48, 20), text, BodyStyle(12));
        }

        private static void DrawDim()
        {
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private static void DrawPanel(Rect rect)
        {
            var prev = GUI.color;
            GUI.color = new Color(0.98f, 0.98f, 0.99f, 0.96f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private static void DrawPill(Rect rect, string text, Color? textColor = null, int fontSize = 16)
        {
            var prev = GUI.color;
            GUI.color = MockupPalette.HudNavy;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prev;
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = textColor ?? Color.white }
            };
            UiTypography.ApplyToGuiStyle(style, bold: true);
            GUI.Label(rect, text, style);
        }

        private static Rect CenterBox(float width, float height) =>
            new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

        private static GUIStyle TitleStyle(int size)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = size,
                fontStyle = FontStyle.Bold,
                normal = { textColor = MockupPalette.HudNavy }
            };
            UiTypography.ApplyToGuiStyle(style, bold: true);
            return style;
        }

        private static GUIStyle BodyStyle(int size)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = size,
                wordWrap = true,
                normal = { textColor = new Color(0.18f, 0.20f, 0.28f, 1f) }
            };
            UiTypography.ApplyToGuiStyle(style, bold: false);
            return style;
        }

        private static string FormatTimer(long ms)
        {
            var totalSeconds = Mathf.Max(0, Mathf.CeilToInt(ms / 1000f));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private static string FormatClear(long ms)
        {
            var total = ms / 1000f;
            var minutes = Mathf.FloorToInt(total / 60f);
            var seconds = total - minutes * 60f;
            return $"{minutes:00}:{seconds:00.0}";
        }
    }
}
