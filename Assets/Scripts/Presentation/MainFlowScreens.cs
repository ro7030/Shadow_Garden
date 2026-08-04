using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using ShadowGarden.Runtime;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Builds / refreshes Title, Opening, WorldMap, GameOver, Cleared, Ending screens (uGUI/TMP).
    /// Keyboard Navigate/Submit and mouse clicks share the same ViewModel selection state.
    /// </summary>
    public sealed class MainFlowScreens : MonoBehaviour
    {
        [SerializeField] private AppScreenRouter router;
        [SerializeField] private MainCompositionRoot main;

        private Button _titleContinue;
        private Button _titleNewGame;
        private TextMeshProUGUI _titleProgressLabel;
        private Button _openingContinue;
        private Button _endingWorldMap;
        private Button _endingTitle;

        private readonly Dictionary<string, Button> _stageButtons = new Dictionary<string, Button>();
        private readonly List<Button> _modalButtons = new List<Button>();
        private Transform _worldCardsRoot;
        private TextMeshProUGUI _worldMapHint;

        private WorldMapViewModel _worldMapVm;
        private ModalViewModel _modalVm;
        private bool _inputHooked;
        private float _navigateCooldown;

        public WorldMapViewModel CurrentWorldMap => _worldMapVm;
        public ModalViewModel CurrentModal => _modalVm;

        public void Bind(MainCompositionRoot compositionRoot, AppScreenRouter screenRouter)
        {
            UnbindInput();
            main = compositionRoot;
            router = screenRouter;
            BuildIfNeeded();
            WireCallbacks();
            HookInput();
            RefreshTitleBranch();
            RefreshWorldMapUnlock();
        }

        public void UnbindInput()
        {
            if (!_inputHooked || main?.Input == null)
            {
                _inputHooked = false;
                return;
            }

            main.Input.NavigateRequested -= OnNavigate;
            main.Input.SubmitRequested -= OnSubmit;
            _inputHooked = false;
        }

        public void RefreshTitleBranch()
        {
            if (router?.TitleRoot == null || main?.Save == null)
            {
                return;
            }

            EnsureTitle(router.TitleRoot);
            var canContinue = main.Save.CanContinue();
            if (_titleContinue != null)
            {
                _titleContinue.gameObject.SetActive(true);
                var label = _titleContinue.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = canContinue ? "정원으로 돌아가기" : "시작";
                    UiTypography.Apply(label, bold: true);
                }
            }

            if (_titleNewGame != null)
            {
                _titleNewGame.gameObject.SetActive(canContinue);
            }

            if (_titleProgressLabel != null)
            {
                if (canContinue)
                {
                    var progress = main.Save.Progress;
                    var cleared = progress?.completedStageIds?.Count ?? 0;
                    var last = string.IsNullOrWhiteSpace(progress?.lastStageId) ? "1-1" : progress.lastStageId;
                    _titleProgressLabel.gameObject.SetActive(true);
                    _titleProgressLabel.text = $"마지막 {last} · 완료 {cleared}/12";
                    UiTypography.Apply(_titleProgressLabel, bold: false);
                }
                else
                {
                    _titleProgressLabel.gameObject.SetActive(false);
                }
            }

            if (main.CurrentState == AppState.Title)
            {
                SelectButton(_titleContinue);
            }
        }

        public void RefreshWorldMapUnlock()
        {
            if (router?.WorldMapRoot == null || main == null)
            {
                return;
            }

            EnsureWorldMap(router.WorldMapRoot);
            _worldMapVm = WorldMapViewModel.Build(
                main.Catalog,
                main.Save?.Progress,
                main.Save?.Progress?.lastStageId);

            RebuildWorldCards();
            SyncWorldMapFocusVisual();
            if (main.CurrentState == AppState.WorldMap)
            {
                FocusWorldMapButton(_worldMapVm?.FocusedStageId);
            }
        }

        public void RefreshModalForState(AppState state)
        {
            if (state == AppState.GameOver)
            {
                EnsureGameOver(router?.GameOverRoot);
                _modalVm = ModalViewModel.CreateGameOver();
                RebuildModalButtons(router.GameOverRoot, _modalVm);
                SelectModalIndex(0);
            }
            else if (state == AppState.Cleared)
            {
                EnsureCleared(router?.ClearedRoot);
                var next = main?.ResolveNextStageAfterClear();
                var isFinal = main != null && main.IsFinalStageClear();
                var nextWorld = main != null && main.IsWorldFinaleClear();
                _modalVm = ModalViewModel.CreateCleared(
                    hasNextStage: !string.IsNullOrWhiteSpace(next) || isFinal,
                    isFinalStage: isFinal,
                    nextIsNewWorld: nextWorld);
                RebuildModalButtons(router.ClearedRoot, _modalVm);
                SelectModalIndex(0);
            }
            else if (state == AppState.Ending)
            {
                EnsureEnding(router?.EndingRoot);
                SelectButton(_endingWorldMap);
            }
        }

        private void HookInput()
        {
            if (_inputHooked || main?.Input == null)
            {
                return;
            }

            main.Input.NavigateRequested += OnNavigate;
            main.Input.SubmitRequested += OnSubmit;
            _inputHooked = true;
        }

        private void Update()
        {
            if (_navigateCooldown > 0f)
            {
                _navigateCooldown -= Time.unscaledDeltaTime;
            }
        }

        private void OnNavigate(Vector2 value)
        {
            if (_navigateCooldown > 0f || main == null)
            {
                return;
            }

            var dx = Mathf.Abs(value.x) >= 0.5f ? (int)Mathf.Sign(value.x) : 0;
            var dy = Mathf.Abs(value.y) >= 0.5f ? (int)Mathf.Sign(value.y) : 0;
            if (dx == 0 && dy == 0)
            {
                return;
            }

            _navigateCooldown = 0.18f;
            var state = main.CurrentState;
            if (state == AppState.WorldMap && _worldMapVm != null)
            {
                _worldMapVm.MoveFocusGrid(dx, -dy);
                SyncWorldMapFocusVisual();
                FocusWorldMapButton(_worldMapVm.FocusedStageId);
            }
            else if (state == AppState.GameOver || state == AppState.Cleared)
            {
                if (_modalVm != null && dy != 0)
                {
                    _modalVm.MoveSelection(-dy);
                    SelectModalIndex(_modalVm.SelectedIndex);
                }
            }
            else if (state == AppState.Title)
            {
                CycleTitleFocus(dy != 0 ? -dy : dx);
            }
            else if (state == AppState.Ending)
            {
                if (dy != 0 || dx != 0)
                {
                    ToggleEndingFocus();
                }
            }
        }

        private void OnSubmit()
        {
            if (main == null)
            {
                return;
            }

            switch (main.CurrentState)
            {
                case AppState.Title:
                    ActivateSelectedOrDefault(_titleContinue);
                    break;
                case AppState.Opening:
                    main.CompleteOpening();
                    break;
                case AppState.WorldMap:
                    if (_worldMapVm != null &&
                        !string.IsNullOrWhiteSpace(_worldMapVm.FocusedStageId))
                    {
                        var node = _worldMapVm.FindFocused();
                        if (node != null && node.Unlocked)
                        {
                            main.StartStage(node.StageId);
                        }
                    }

                    break;
                case AppState.GameOver:
                case AppState.Cleared:
                    if (_modalVm?.Selected != null)
                    {
                        main.ApplyModalSelection(_modalVm.Selected.Id);
                    }

                    break;
                case AppState.Ending:
                    ActivateSelectedOrDefault(_endingWorldMap);
                    break;
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
            EnsureEnding(router.EndingRoot);
        }

        private void WireCallbacks()
        {
            SetClick(_titleContinue, () => main?.ContinueFromTitle());
            SetClick(_titleNewGame, () => main?.StartNewGameFromTitle());
            SetClick(_openingContinue, () => main?.CompleteOpening());
            SetClick(_endingWorldMap, () => main?.FinishEndingToWorldMap());
            SetClick(_endingTitle, () => main?.FinishEnding());
        }

        private void EnsureTitle(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            SetRootLabel(root, "그림자 정원");
            _titleContinue = EnsureButton(root.transform, "ContinueButton", "시작", new Vector2(0f, -40f));
            _titleNewGame = EnsureButton(root.transform, "NewGameButton", "새로 시작", new Vector2(0f, -120f));
            _titleProgressLabel = EnsureLabel(root.transform, "ProgressLabel", string.Empty, new Vector2(0f, -190f), 22f);
            // Remove legacy StartButton if present.
            var legacy = root.transform.Find("StartButton");
            if (legacy != null)
            {
                legacy.gameObject.SetActive(false);
            }
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
            var legacy = root.transform.Find("Stage11Button");
            if (legacy != null)
            {
                legacy.gameObject.SetActive(false);
            }

            if (_worldCardsRoot == null)
            {
                var existing = root.transform.Find("WorldCards");
                if (existing != null)
                {
                    _worldCardsRoot = existing;
                }
                else
                {
                    var go = new GameObject("WorldCards", typeof(RectTransform));
                    go.transform.SetParent(root.transform, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(980f, 420f);
                    rt.anchoredPosition = new Vector2(0f, -20f);
                    _worldCardsRoot = go.transform;
                }
            }

            _worldMapHint = EnsureLabel(
                root.transform,
                "WorldMapHint",
                "←→↑↓ 이동 · Enter 선택",
                new Vector2(0f, -250f),
                18f);
        }

        private void EnsureGameOver(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            SetRootLabel(root, "게임 오버");
            // Buttons rebuilt from ModalViewModel on show.
            EnsureButton(root.transform, "RetryButton", "다시 도전", new Vector2(0f, -40f));
            EnsureButton(root.transform, "WorldMapButton", "레벨 선택", new Vector2(0f, -120f));
        }

        private void EnsureCleared(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            SetRootLabel(root, "완료");
            EnsureButton(root.transform, "RetryButton", "다시 도전", new Vector2(0f, -40f));
            EnsureButton(root.transform, "WorldMapButton", "레벨 선택", new Vector2(0f, -120f));
            EnsureButton(root.transform, "NextButton", "다음 스테이지", new Vector2(0f, -40f));
            EnsureButton(root.transform, "EndingButton", "엔딩 보기", new Vector2(0f, -40f));
        }

        private void EnsureEnding(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            SetRootLabel(root, "엔딩");
            var body = EnsureLabel(
                root.transform,
                "EndingBody",
                "세 정원이 다시 숨을 쉬기 시작했습니다.",
                new Vector2(0f, -20f),
                24f);
            body.gameObject.SetActive(true);
            _endingWorldMap = EnsureButton(root.transform, "WorldMapButton", "레벨 선택", new Vector2(0f, -100f));
            _endingTitle = EnsureButton(root.transform, "TitleButton", "타이틀", new Vector2(0f, -180f));
        }

        private void RebuildWorldCards()
        {
            if (_worldCardsRoot == null || _worldMapVm?.Worlds == null)
            {
                return;
            }

            for (var i = _worldCardsRoot.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_worldCardsRoot.GetChild(i).gameObject);
            }

            _stageButtons.Clear();
            const float cardWidth = 300f;
            const float gap = 30f;
            var startX = -((3 - 1) * (cardWidth + gap)) * 0.5f;

            for (var w = 0; w < _worldMapVm.Worlds.Count; w++)
            {
                var world = _worldMapVm.Worlds[w];
                var card = CreateWorldCard(_worldCardsRoot, world, new Vector2(startX + w * (cardWidth + gap), 0f));
                for (var n = 0; n < world.Nodes.Count; n++)
                {
                    var node = world.Nodes[n];
                    var button = CreateStageNodeButton(card, node, new Vector2(0f, 70f - n * 70f));
                    _stageButtons[node.StageId] = button;
                    var captured = node.StageId;
                    SetClick(button, () =>
                    {
                        if (_worldMapVm != null)
                        {
                            _worldMapVm.SetFocus(captured);
                            SyncWorldMapFocusVisual();
                        }

                        if (node.Unlocked)
                        {
                            main?.StartStage(captured);
                        }
                    });
                }
            }
        }

        private static Transform CreateWorldCard(Transform parent, WorldCardViewModel world, Vector2 pos)
        {
            var go = new GameObject($"WorldCard_{world.WorldNumber}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300f, 360f);
            rt.anchoredPosition = pos;
            var image = go.GetComponent<Image>();
            image.color = world.Unlocked
                ? new Color(0.14f, 0.2f, 0.3f, 0.95f)
                : new Color(0.08f, 0.09f, 0.12f, 0.9f);

            var title = EnsureLabel(
                go.transform,
                "WorldTitle",
                world.Unlocked ? world.WorldTitle : $"{world.WorldTitle} (잠김)",
                new Vector2(0f, 150f),
                24f);
            title.fontStyle = FontStyles.Bold;

            if (!world.Unlocked)
            {
                var lockHint = EnsureLabel(
                    go.transform,
                    "LockHint",
                    "이전 월드의 밤꽃을 피워 주세요",
                    new Vector2(0f, 110f),
                    16f);
                lockHint.color = new Color(0.75f, 0.75f, 0.8f, 1f);
            }

            return go.transform;
        }

        private static Button CreateStageNodeButton(Transform parent, StageNodeViewModel node, Vector2 pos)
        {
            var icon = string.IsNullOrEmpty(node.CompletionIcon) ? string.Empty : node.CompletionIcon + " ";
            var label = $"{icon}{node.StageId}  {node.TimeLabel}";
            var button = EnsureButton(parent, $"Stage_{node.StageId}", label, pos);
            button.interactable = node.Unlocked;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = node.Unlocked
                    ? new Color(0.18f, 0.28f, 0.4f, 0.95f)
                    : new Color(0.1f, 0.1f, 0.12f, 0.7f);
            }

            var tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.fontSize = 20;
                tmp.color = node.Unlocked ? Color.white : new Color(0.55f, 0.55f, 0.58f, 1f);
            }

            return button;
        }

        private void SyncWorldMapFocusVisual()
        {
            if (_worldMapVm?.FlatNodes == null)
            {
                return;
            }

            foreach (var node in _worldMapVm.FlatNodes)
            {
                if (!_stageButtons.TryGetValue(node.StageId, out var button) || button == null)
                {
                    continue;
                }

                var image = button.GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                if (!node.Unlocked)
                {
                    image.color = new Color(0.1f, 0.1f, 0.12f, 0.7f);
                }
                else if (node.IsFocused)
                {
                    image.color = new Color(0.32f, 0.48f, 0.72f, 1f);
                }
                else
                {
                    image.color = new Color(0.18f, 0.28f, 0.4f, 0.95f);
                }
            }
        }

        private void FocusWorldMapButton(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId) ||
                !_stageButtons.TryGetValue(stageId, out var button))
            {
                return;
            }

            SelectButton(button);
        }

        private void RebuildModalButtons(GameObject root, ModalViewModel modal)
        {
            if (root == null || modal?.Options == null)
            {
                return;
            }

            foreach (Transform child in root.transform)
            {
                if (child.name.EndsWith("Button"))
                {
                    child.gameObject.SetActive(false);
                }
            }

            _modalButtons.Clear();
            for (var i = 0; i < modal.Options.Count; i++)
            {
                var option = modal.Options[i];
                var name = option.Id switch
                {
                    "retry" => "RetryButton",
                    "worldmap" => "WorldMapButton",
                    "next" => "NextButton",
                    "ending" => "EndingButton",
                    _ => option.Id + "Button"
                };
                var button = EnsureButton(root.transform, name, option.Label, new Vector2(0f, -40f - i * 80f));
                button.gameObject.SetActive(true);
                var captured = option.Id;
                SetClick(button, () => main?.ApplyModalSelection(captured));
                _modalButtons.Add(button);
            }
        }

        private void SelectModalIndex(int index)
        {
            if (_modalVm == null)
            {
                return;
            }

            _modalVm.SetSelectedIndex(index);
            if (_modalButtons.Count == 0)
            {
                return;
            }

            var clamped = Mathf.Clamp(_modalVm.SelectedIndex, 0, _modalButtons.Count - 1);
            SelectButton(_modalButtons[clamped]);
            for (var i = 0; i < _modalButtons.Count; i++)
            {
                var image = _modalButtons[i].GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                image.color = i == clamped
                    ? new Color(0.32f, 0.48f, 0.72f, 1f)
                    : new Color(0.16f, 0.22f, 0.34f, 0.92f);
            }
        }

        private void CycleTitleFocus(int delta)
        {
            if (_titleNewGame != null && _titleNewGame.gameObject.activeInHierarchy)
            {
                var currentIsNew = EventSystem.current != null &&
                                   EventSystem.current.currentSelectedGameObject == _titleNewGame.gameObject;
                if (delta == 0)
                {
                    return;
                }

                SelectButton(currentIsNew ? _titleContinue : _titleNewGame);
            }
            else
            {
                SelectButton(_titleContinue);
            }
        }

        private void ToggleEndingFocus()
        {
            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == _endingTitle?.gameObject)
            {
                SelectButton(_endingWorldMap);
            }
            else
            {
                SelectButton(_endingTitle);
            }
        }

        private static void ActivateSelectedOrDefault(Button fallback)
        {
            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject != null)
            {
                var button = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
                if (button != null && button.interactable && button.gameObject.activeInHierarchy)
                {
                    button.onClick.Invoke();
                    return;
                }
            }

            fallback?.onClick.Invoke();
        }

        private static void SelectButton(Button button)
        {
            if (button == null || EventSystem.current == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        private static void SetRootLabel(GameObject root, string text)
        {
            foreach (var legacy in root.GetComponentsInChildren<Text>(true))
            {
                Object.Destroy(legacy);
            }

            var tmp = root.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp == null)
            {
                tmp = CreateTitleLabel(root.transform, text);
            }

            // Prefer the root title label, not nested button labels.
            var title = root.transform.Find("TitleLabel");
            if (title != null)
            {
                tmp = title.GetComponent<TextMeshProUGUI>();
            }

            if (tmp == null)
            {
                return;
            }

            tmp.gameObject.SetActive(true);
            tmp.text = text;
            tmp.fontSize = Mathf.Max(tmp.fontSize, 40);
            tmp.alignment = TextAlignmentOptions.Center;
            UiTypography.Apply(tmp, bold: true);
        }

        private static TextMeshProUGUI CreateTitleLabel(Transform parent, string text)
        {
            var go = new GameObject("TitleLabel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(640f, 80f);
            rt.anchoredPosition = new Vector2(0f, 180f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 48;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            UiTypography.Apply(tmp, bold: true);
            return tmp;
        }

        private static TextMeshProUGUI EnsureLabel(
            Transform parent,
            string name,
            string text,
            Vector2 anchoredPos,
            float fontSize)
        {
            var existing = parent.Find(name);
            TextMeshProUGUI tmp;
            if (existing != null)
            {
                tmp = existing.GetComponent<TextMeshProUGUI>();
                if (tmp == null)
                {
                    tmp = existing.gameObject.AddComponent<TextMeshProUGUI>();
                }
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                tmp = go.AddComponent<TextMeshProUGUI>();
            }

            var rt = tmp.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(640f, 40f);
            rt.anchoredPosition = anchoredPos;
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            UiTypography.Apply(tmp, bold: false);
            return tmp;
        }

        private static Button EnsureButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                var existingButton = existing.GetComponent<Button>();
                var existingTmp = existing.GetComponentInChildren<TextMeshProUGUI>(true);
                if (existingTmp != null)
                {
                    existingTmp.text = label;
                    UiTypography.Apply(existingTmp, bold: true);
                }

                var existingRt = existing.GetComponent<RectTransform>();
                if (existingRt != null)
                {
                    existingRt.anchoredPosition = anchoredPos;
                }

                existing.gameObject.SetActive(true);
                return existingButton;
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
            UiTypography.Apply(tmp, bold: true);

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
