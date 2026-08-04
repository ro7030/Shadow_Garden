using ShadowGarden.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Main-scene play HUD built with uGUI. Never uses OnGUI.
    /// </summary>
    public sealed class MainPlayHud : MonoBehaviour
    {
        [SerializeField] private Text stageLabel;
        [SerializeField] private Text timerLabel;
        [SerializeField] private Text goalLabel;
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
                new Vector2(24f, -24f), new Vector2(420f, 48f), TextAnchor.UpperLeft, 28);
            timerLabel = CreateLabel(root.transform, "TimerLabel", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -24f), new Vector2(220f, 48f), TextAnchor.UpperRight, 32);
            goalLabel = CreateLabel(root.transform, "GoalLabel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -24f), new Vector2(360f, 40f), TextAnchor.UpperCenter, 22);
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

        private static Text CreateLabel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 size,
            TextAnchor align,
            int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.alignment = align;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.text = name;
            text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
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
