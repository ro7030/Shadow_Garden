using ShadowGarden.Core;
using TMPro;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Main-scene play HUD built with uGUI + TextMeshPro. Never uses OnGUI.
    /// </summary>
    public sealed class MainPlayHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI stageLabel;
        [SerializeField] private TextMeshProUGUI timerLabel;
        [SerializeField] private TextMeshProUGUI goalLabel;
        [SerializeField] private GameObject root;

        public void EnsureBuilt(Transform parent)
        {
            if (root != null)
            {
                return;
            }

            root = new GameObject("PlayHud", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            StretchFull(root.GetComponent<RectTransform>());

            stageLabel = CreateLabel(root.transform, "StageLabel", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -24f), new Vector2(420f, 48f), TextAlignmentOptions.TopLeft, 28, bold: true);
            timerLabel = CreateLabel(root.transform, "TimerLabel", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -24f), new Vector2(220f, 48f), TextAlignmentOptions.TopRight, 32, bold: true);
            goalLabel = CreateLabel(root.transform, "GoalLabel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -24f), new Vector2(360f, 40f), TextAlignmentOptions.Top, 22, bold: false);
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

            stageLabel.text = $"{stage.StageId}  ·  {MockupPalette.WorldName(stage.StageId)}";
            goalLabel.text = $"목표  ·  {MockupPalette.GoalLabel(stage.ClearGoalType)}";
            timerLabel.text = FormatTimer(state.RemainingMilliseconds);
            if (state.RemainingMilliseconds <= 10_000)
            {
                timerLabel.color = MockupPalette.WarningCoral;
            }
            else if (state.RemainingMilliseconds <= 30_000)
            {
                timerLabel.color = MockupPalette.WarningAmber;
            }
            else
            {
                timerLabel.color = Color.white;
            }
        }

        private static string FormatTimer(long remainingMs)
        {
            var total = Mathf.Max(0, Mathf.CeilToInt(remainingMs / 1000f));
            var m = total / 60;
            var s = total % 60;
            return $"{m:0}:{s:00}";
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 size,
            TextAlignmentOptions align,
            int fontSize,
            bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.alignment = align;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.text = name;
            text.raycastTarget = false;
            UiTypography.Apply(text, bold);
            return text;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
