using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Builds / refreshes Title, Opening, WorldMap, GameOver, Cleared screen buttons (uGUI/TMP).
    /// </summary>
    public sealed class MainFlowScreens : MonoBehaviour
    {
        [SerializeField] private AppScreenRouter router;
        [SerializeField] private MainCompositionRoot main;

        private Button _titleStart;
        private Button _openingContinue;
        private Button _worldMapStage11;
        private Button _gameOverRetry;
        private Button _gameOverWorldMap;
        private Button _clearedRetry;
        private Button _clearedWorldMap;

        public void Bind(MainCompositionRoot compositionRoot, AppScreenRouter screenRouter)
        {
            main = compositionRoot;
            router = screenRouter;
            BuildIfNeeded();
            WireCallbacks();
        }

        public void RefreshWorldMapUnlock()
        {
            if (_worldMapStage11 == null || main?.Save == null)
            {
                return;
            }

            // 1-1 is always unlocked.
            _worldMapStage11.interactable = true;
            var label = _worldMapStage11.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = "1-1  첫 그림자";
            }
        }

        private void BuildIfNeeded()
        {
            if (router == null)
            {
                return;
            }

            EnsureTitle(router.TitleRoot);
            EnsureOpening(router.OpeningRoot);
            EnsureWorldMap(router.WorldMapRoot);
            EnsureGameOver(router.GameOverRoot);
            EnsureCleared(router.ClearedRoot);
        }

        private void WireCallbacks()
        {
            SetClick(_titleStart, () => main?.ContinueFromTitle());
            SetClick(_openingContinue, () => main?.CompleteOpening());
            SetClick(_worldMapStage11, () => main?.StartStage("1-1"));
            SetClick(_gameOverRetry, () => main?.RetryFromGameOver());
            SetClick(_gameOverWorldMap, () => main?.ReturnToWorldMap());
            SetClick(_clearedRetry, () => main?.RetryFromGameOver());
            SetClick(_clearedWorldMap, () => main?.ReturnToWorldMap());
        }

        private void EnsureTitle(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            SetRootLabel(root, "그림자 정원");
            _titleStart = EnsureButton(root.transform, "StartButton", "시작", new Vector2(0f, -80f));
        }

        private void EnsureOpening(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            SetRootLabel(root, "Opening");
            _openingContinue = EnsureButton(root.transform, "ContinueButton", "계속", new Vector2(0f, -80f));
        }

        private void EnsureWorldMap(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            SetRootLabel(root, "월드 맵");
            _worldMapStage11 = EnsureButton(root.transform, "Stage11Button", "1-1  첫 그림자", new Vector2(0f, -40f));
        }

        private void EnsureGameOver(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            SetRootLabel(root, "게임 오버");
            _gameOverRetry = EnsureButton(root.transform, "RetryButton", "다시 도전", new Vector2(0f, -40f));
            _gameOverWorldMap = EnsureButton(root.transform, "WorldMapButton", "레벨 선택", new Vector2(0f, -120f));
        }

        private void EnsureCleared(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            SetRootLabel(root, "완료");
            _clearedRetry = EnsureButton(root.transform, "RetryButton", "다시 도전", new Vector2(0f, -40f));
            _clearedWorldMap = EnsureButton(root.transform, "WorldMapButton", "레벨 선택", new Vector2(0f, -120f));
        }

        private static void SetRootLabel(GameObject root, string text)
        {
            var existing = root.GetComponentInChildren<Text>();
            if (existing != null)
            {
                existing.text = text;
                return;
            }

            var tmp = root.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = text;
            }
        }

        private static Button EnsureButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(280f, 56f);
            rt.anchoredPosition = anchoredPos;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.16f, 0.22f, 0.34f, 0.92f);

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 26;
            tmp.color = Color.white;

            return go.GetComponent<Button>();
        }

        private static void SetClick(Button button, UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }
}
