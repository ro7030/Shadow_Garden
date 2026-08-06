using System.Collections;
using System.Collections.Generic;
using ShadowGarden.Core;
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
        private Button _titleReplayOpening;
        private Button _titleSettings;
        private TextMeshProUGUI _titleProgressLabel;
        private Button _openingContinue;
        private Button _openingSkip;
        private TextMeshProUGUI _openingBody;
        private TextMeshProUGUI _openingPageLabel;
        private TextMeshProUGUI _openingHoldHint;
        private Image _openingHoldTrack;
        private Image _openingHoldFill;
        private int _openingPage;
        private float _openingHoldSeconds;
        private bool _openingHoldActive;
        private Button _endingWorldMap;
        private Button _endingTitle;
        private TextMeshProUGUI _gameOverReason;
        private TextMeshProUGUI _clearedDetail;
        private TextMeshProUGUI _endingBody;
        private Coroutine _endingBeatRoutine;
        private bool _endingBeatComplete;

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
        public bool IsEndingBeatComplete => _endingBeatComplete;

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
                _titleContinue.interactable = canContinue;
                var label = _titleContinue.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = "정원으로 돌아가기";
                    UiTypography.Apply(label, bold: true);
                }
            }

            if (_titleNewGame != null)
            {
                _titleNewGame.gameObject.SetActive(true);
                var label = _titleNewGame.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = "처음 시작하기";
                    UiTypography.Apply(label, bold: true);
                }
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
                SelectButton(canContinue ? _titleContinue : _titleNewGame);
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
                if (_gameOverReason != null && main != null)
                {
                    var goal = ClearGoalType.ExitDoor;
                    if (main.Catalog != null &&
                        main.Catalog.TryGetById(main.PendingStageId, out var asset) &&
                        asset != null)
                    {
                        goal = asset.clearGoalType;
                    }

                    _gameOverReason.text = MockupPalette.GameOverReason(main.LastGameOverCause, goal);
                    UiTypography.Apply(_gameOverReason, bold: false);
                }

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
                if (_clearedDetail != null && main != null)
                {
                    var id = main.LastClearedStageId ?? main.PendingStageId;
                    var time = ProgressTimeFormat.FormatBestClear((long?)main.LastClearElapsedMilliseconds);
                    var best = ProgressTimeFormat.FormatBestClear(main.Save?.Progress, id);
                    var nightFlower = false;
                    if (main.Catalog != null &&
                        main.Catalog.TryGetById(id, out var clearedAsset) &&
                        clearedAsset != null)
                    {
                        nightFlower = clearedAsset.clearGoalType == ClearGoalType.NightFlower;
                    }

                    var bestLine = best == ProgressTimeFormat.Incomplete
                        ? string.Empty
                        : $"\nBEST {best}";
                    var isNewBest = best != ProgressTimeFormat.Incomplete &&
                                    best == time;
                    var newBadge = isNewBest ? "  NEW" : string.Empty;

                    if (isFinal)
                    {
                        _clearedDetail.text =
                            $"3-4 밤꽃 완료  ·  {time}{newBadge}{bestLine}\n세 정원이 모두 되살아났습니다.";
                    }
                    else if (nextWorld)
                    {
                        _clearedDetail.text =
                            $"{id} 밤꽃 완료  ·  {time}{newBadge}{bestLine}\n다음 월드가 해금되었습니다.";
                    }
                    else if (nightFlower)
                    {
                        _clearedDetail.text = $"{id} 밤꽃 완료  ·  {time}{newBadge}{bestLine}";
                    }
                    else
                    {
                        _clearedDetail.text = $"{id} 출구 완료  ·  {time}{newBadge}{bestLine}";
                    }

                    UiTypography.Apply(_clearedDetail, bold: false);
                }

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
                BeginEndingBeat();
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

            TickOpeningHold();
            TickGameOverResetShortcut();
            if (main != null && main.CurrentState != AppState.Ending && _endingBeatRoutine != null)
            {
                StopCoroutine(_endingBeatRoutine);
                _endingBeatRoutine = null;
            }
        }

        private void TickGameOverResetShortcut()
        {
            if (main == null || main.CurrentState != AppState.GameOver)
            {
                return;
            }

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                main.RetryFromGameOver();
            }
        }

        private void TickOpeningHold()
        {
            if (main == null || main.CurrentState != AppState.Opening)
            {
                _openingHoldSeconds = 0f;
                _openingHoldActive = false;
                if (_openingHoldFill != null)
                {
                    UpdateOpeningHoldGauge(0f);
                }

                return;
            }

            // Skip is hold-on-Skip-button only. Enter/Space are reserved for 「다음」 via Submit.
            var holding = _openingHoldActive;

            if (!holding)
            {
                _openingHoldSeconds = Mathf.MoveTowards(_openingHoldSeconds, 0f, Time.unscaledDeltaTime * 2f);
            }
            else
            {
                _openingHoldSeconds += Time.unscaledDeltaTime;
            }

            UpdateOpeningHoldGauge(Mathf.Clamp01(
                _openingHoldSeconds / PresentationTiming.OpeningSkipHoldSeconds));

            if (_openingHoldSeconds >= PresentationTiming.OpeningSkipHoldSeconds)
            {
                _openingHoldSeconds = 0f;
                _openingHoldActive = false;
                main.CompleteOpening();
            }
        }

        private void UpdateOpeningHoldGauge(float progress)
        {
            var normalized = Mathf.Clamp01(progress);
            if (_openingHoldFill != null)
            {
                _openingHoldFill.gameObject.SetActive(true);
                var rect = _openingHoldFill.rectTransform;
                var trackWidth = _openingHoldTrack != null
                    ? Mathf.Max(1f, _openingHoldTrack.rectTransform.sizeDelta.x)
                    : 320f;
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 0f);
                rect.sizeDelta = new Vector2(Mathf.Max(2f, trackWidth * Mathf.Max(0.02f, normalized)), 10f);
                _openingHoldFill.color = Color.Lerp(
                    new Color(UiTheme.Mint.r, UiTheme.Mint.g, UiTheme.Mint.b, 0.35f),
                    UiTheme.Mint,
                    normalized);
            }

            if (_openingHoldHint != null)
            {
                _openingHoldHint.text = normalized > 0.02f
                    ? $"홀드하여 건너뛰기  {Mathf.RoundToInt(normalized * 100f)}%"
                    : "홀드하여 건너뛰기";
                UiTypography.Apply(_openingHoldHint, bold: false);
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
            var movedFocus = false;
            var state = main.CurrentState;
            if (state == AppState.WorldMap && _worldMapVm != null)
            {
                _worldMapVm.MoveFocusGrid(dx, -dy);
                SyncWorldMapFocusVisual();
                FocusWorldMapButton(_worldMapVm.FocusedStageId);
                movedFocus = true;
            }
            else if (state == AppState.GameOver || state == AppState.Cleared)
            {
                if (_modalVm != null && dy != 0)
                {
                    _modalVm.MoveSelection(-dy);
                    SelectModalIndex(_modalVm.SelectedIndex);
                    movedFocus = true;
                }
            }
            else if (state == AppState.Title)
            {
                CycleTitleFocus(dy != 0 ? -dy : dx);
                movedFocus = true;
            }
            else if (state == AppState.Opening)
            {
                if (dy != 0 || dx != 0)
                {
                    ToggleOpeningFocus();
                    movedFocus = true;
                }
            }
            else if (state == AppState.Ending)
            {
                if (dy != 0 || dx != 0)
                {
                    ToggleEndingFocus();
                    movedFocus = true;
                }
            }
            if (movedFocus) main.Gameplay?.PlayUiMove();
        }

        private void OnSubmit()
        {
            if (main == null)
            {
                return;
            }

            main.Gameplay?.PlayUiSubmit();

            switch (main.CurrentState)
            {
                case AppState.Title:
                    ActivateSelectedOrDefault(_titleContinue);
                    break;
                case AppState.Opening:
                    // Always advance/complete — do not activate Skip (empty click / hold-only).
                    AdvanceOpeningOrComplete();
                    break;
                case AppState.WorldMap:
                    ActivateSelectedOrDefault(FocusedWorldMapButton());
                    break;
                case AppState.GameOver:
                case AppState.Cleared:
                    ActivateSelectedOrDefault(SelectedModalButton());
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
            HideGameplayPlaceholderLabel(router.GameplayRoot);
        }

        private static void HideGameplayPlaceholderLabel(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp == null)
                {
                    continue;
                }

                // Scene placeholder "Gameplay" must not cover the board.
                if (tmp.transform.parent == root.transform &&
                    (tmp.name.Contains("Label") || tmp.text == "Gameplay" || tmp.fontSize >= 32f))
                {
                    tmp.gameObject.SetActive(false);
                }
            }
        }

        private void WireCallbacks()
        {
            SetClick(_titleContinue, () => main?.ContinueFromTitle());
            SetClick(_titleNewGame, () => main?.StartNewGameFromTitle());
            SetClick(_titleReplayOpening, () => main?.ReplayOpening());
            SetClick(_titleSettings, () => main?.OpenSettingsFromTitle());
            SetClick(_openingContinue, AdvanceOpeningOrComplete);
            SetClick(_openingSkip, () => { });
            ConfigureOpeningHoldEvents(_openingSkip);
            SetClick(_endingWorldMap, () => main?.FinishEndingToWorldMap());
            SetClick(_endingTitle, () => main?.FinishEnding());
        }

        public void RefreshOpening()
        {
            if (router?.OpeningRoot == null)
            {
                return;
            }

            EnsureOpening(router.OpeningRoot);
            // Re-bind after layout/ensure so 「다음」 never loses its click handler.
            SetClick(_openingContinue, AdvanceOpeningOrComplete);
            SetClick(_openingSkip, () => { });
            ConfigureOpeningHoldEvents(_openingSkip);
            _openingPage = 0;
            _openingHoldSeconds = 0f;
            _openingHoldActive = false;
            UpdateOpeningHoldGauge(0f);
            ApplyOpeningPage();
            SelectButton(_openingContinue);
        }

        private void AdvanceOpeningOrComplete()
        {
            if (main == null || main.CurrentState != AppState.Opening)
            {
                return;
            }

            if (_openingPage < OpeningPages.Length - 1)
            {
                _openingPage++;
                ApplyOpeningPage();
                SelectButton(_openingContinue);
                return;
            }

            main.CompleteOpening();
        }

        private void ApplyOpeningPage()
        {
            if (_openingBody == null)
            {
                return;
            }

            var index = Mathf.Clamp(_openingPage, 0, OpeningPages.Length - 1);
            _openingBody.text = OpeningPages[index];
            UiTypography.Apply(_openingBody, bold: false);
            if (_openingPageLabel != null)
            {
                _openingPageLabel.text = $"{index + 1} / {OpeningPages.Length}";
                UiTypography.Apply(_openingPageLabel, bold: false);
            }

            if (_openingContinue != null)
            {
                var label = _openingContinue.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = index >= OpeningPages.Length - 1 ? "정원으로" : "다음";
                    UiTypography.Apply(label, bold: true);
                }
            }
        }

        private static readonly string[] OpeningPages =
        {
            "정오의 정원이 멈췄다.\n해가 제자리에 굳어 그림자도 숨이 멎었다.",
            "모아가 밤씨앗을 품고 도착한다.\n잠든 길을 다시 열어야 한다.",
            "태양등을 돌리면 그림자가 길을 연다.\n남색 길만이 발이 닿는 땅이다.",
            "겹친 그림자와 빈 절벽은 위험하다.\n한 칸씩, 천천히 읽으며 걷자.",
            "출구 문으로 방을 지나고,\n밤꽃을 피워 월드를 깨운다.",
            "첫 그림자를 따라 걸어 보자.\n정원이 다시 숨을 쉴 때까지."
        };

        private void EnsureTitle(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            SetRootLabel(root, "그림자 정원");
            _titleContinue = EnsureButton(root.transform, "ContinueButton", "정원으로 돌아가기", new Vector2(0f, 20f));
            _titleNewGame = EnsureButton(root.transform, "NewGameButton", "처음 시작하기", new Vector2(0f, -50f));
            _titleReplayOpening = EnsureButton(root.transform, "ReplayOpeningButton", "오프닝 다시 보기",
                new Vector2(0f, -120f));
            _titleSettings = EnsureButton(root.transform, "SettingsButton", "설정", new Vector2(0f, -190f));
            _titleProgressLabel = EnsureLabel(root.transform, "ProgressLabel", string.Empty, new Vector2(0f, -260f), 20f);
            // Remove legacy StartButton / concept line if present.
            var legacyStart = FindDescendant(root.transform, "StartButton");
            if (legacyStart != null)
            {
                legacyStart.gameObject.SetActive(false);
            }

            var legacyConcept = FindDescendant(root.transform, "ConceptLabel");
            if (legacyConcept != null)
            {
                legacyConcept.gameObject.SetActive(false);
            }
        }

        private void EnsureOpening(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            SetRootLabel(root, "오프닝");
            _openingPageLabel = EnsureLabel(root.transform, "OpeningPageLabel", "1 / 6",
                new Vector2(0f, 200f), 18f);
            _openingBody = EnsureLabel(root.transform, "OpeningBody",
                "정원이 숨을 고른다.", new Vector2(0f, 40f), 24f);
            _openingBody.rectTransform.sizeDelta = new Vector2(720f, 220f);
            _openingContinue = EnsureButton(root.transform, "ContinueButton", "다음", new Vector2(0f, -140f));
            _openingSkip = EnsureButton(root.transform, "SkipButton", "홀드하여 건너뛰기", new Vector2(0f, -210f));
            var skipRt = _openingSkip != null ? _openingSkip.GetComponent<RectTransform>() : null;
            if (skipRt != null)
            {
                skipRt.sizeDelta = new Vector2(360f, Mathf.Max(56f, UiTheme.ButtonMinHeight));
            }

            EnsureOpeningHoldGauge(root.transform);
        }

        private void EnsureOpeningHoldGauge(Transform root)
        {
            var gaugeTransform = FindDescendant(root, "SkipHoldGauge");
            RectTransform gaugeRt;
            if (gaugeTransform == null)
            {
                var gauge = new GameObject("SkipHoldGauge", typeof(RectTransform));
                gauge.transform.SetParent(root, false);
                gaugeRt = gauge.GetComponent<RectTransform>();
            }
            else
            {
                gaugeRt = gaugeTransform as RectTransform ?? gaugeTransform.gameObject.AddComponent<RectTransform>();
                // Legacy single-image gauge: convert to track/fill/hint hierarchy.
                var legacyImage = gaugeRt.GetComponent<Image>();
                if (legacyImage != null && gaugeRt.Find("Track") == null)
                {
                    Object.Destroy(legacyImage);
                }
            }

            gaugeRt.anchorMin = gaugeRt.anchorMax = new Vector2(0.5f, 0.5f);
            gaugeRt.pivot = new Vector2(0.5f, 0.5f);
            gaugeRt.sizeDelta = new Vector2(360f, 48f);
            gaugeRt.anchoredPosition = new Vector2(0f, -270f);

            var trackTransform = gaugeRt.Find("Track");
            if (trackTransform == null)
            {
                var trackGo = new GameObject("Track", typeof(RectTransform), typeof(Image));
                trackGo.transform.SetParent(gaugeRt, false);
                trackTransform = trackGo.transform;
            }

            _openingHoldTrack = trackTransform.GetComponent<Image>() ?? trackTransform.gameObject.AddComponent<Image>();
            var trackRt = _openingHoldTrack.rectTransform;
            trackRt.anchorMin = new Vector2(0.5f, 0.5f);
            trackRt.anchorMax = new Vector2(0.5f, 0.5f);
            trackRt.pivot = new Vector2(0.5f, 0.5f);
            trackRt.sizeDelta = new Vector2(320f, 12f);
            trackRt.anchoredPosition = new Vector2(0f, 8f);
            _openingHoldTrack.color = new Color(UiTheme.NavyDeep.r, UiTheme.NavyDeep.g, UiTheme.NavyDeep.b, 0.22f);
            _openingHoldTrack.raycastTarget = false;

            var fillTransform = gaugeRt.Find("Fill");
            if (fillTransform == null)
            {
                var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillGo.transform.SetParent(trackTransform, false);
                fillTransform = fillGo.transform;
            }

            _openingHoldFill = fillTransform.GetComponent<Image>() ?? fillTransform.gameObject.AddComponent<Image>();
            var fillRt = _openingHoldFill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0.5f);
            fillRt.anchorMax = new Vector2(0f, 0.5f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.sizeDelta = new Vector2(2f, 10f);
            fillRt.anchoredPosition = Vector2.zero;
            _openingHoldFill.color = UiTheme.Mint;
            _openingHoldFill.raycastTarget = false;

            _openingHoldHint = EnsureLabel(gaugeRt, "HoldHintLabel", "홀드하여 건너뛰기",
                new Vector2(0f, -14f), 16f);
            _openingHoldHint.rectTransform.sizeDelta = new Vector2(360f, 28f);
            _openingHoldHint.alignment = TextAlignmentOptions.Center;
            UiTypography.Apply(_openingHoldHint, bold: false);
        }

        private void ConfigureOpeningHoldEvents(Button button)
        {
            if (button == null)
            {
                return;
            }

            var trigger = button.GetComponent<EventTrigger>() ?? button.gameObject.AddComponent<EventTrigger>();
            trigger.triggers ??= new List<EventTrigger.Entry>();
            trigger.triggers.Clear();
            AddTrigger(trigger, EventTriggerType.PointerDown, () => _openingHoldActive = true);
            AddTrigger(trigger, EventTriggerType.PointerUp, () => _openingHoldActive = false);
            AddTrigger(trigger, EventTriggerType.PointerExit, () => _openingHoldActive = false);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action?.Invoke());
            trigger.triggers.Add(entry);
        }

        private void EnsureWorldMap(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            SetRootLabel(root, "월드 맵");
            var titleTransform = FindDescendant(root.transform, "TitleLabel") ??
                                 FindDescendant(root.transform, "Label");
            var title = titleTransform != null ? titleTransform.GetComponent<RectTransform>() : null;
            if (title != null)
            {
                title.anchorMin = title.anchorMax = new Vector2(0.5f, 0.5f);
                title.pivot = new Vector2(0.5f, 0.5f);
                title.anchoredPosition = new Vector2(0f, 430f);
                title.sizeDelta = new Vector2(460f, 64f);
            }
            var legacy = FindDescendant(root.transform, "Stage11Button");
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
            _gameOverReason = EnsureLabel(root.transform, "ReasonLabel", string.Empty,
                new Vector2(0f, 40f), 22f);
            _gameOverReason.rectTransform.sizeDelta = new Vector2(720f, 100f);
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
            _clearedDetail = EnsureLabel(root.transform, "DetailLabel", string.Empty,
                new Vector2(0f, 50f), 22f);
            _clearedDetail.rectTransform.sizeDelta = new Vector2(720f, 80f);
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
            _endingBody = EnsureLabel(
                root.transform,
                "EndingBody",
                "세 정원이 다시 숨을 쉬기 시작했습니다.",
                new Vector2(0f, 40f),
                24f);
            _endingBody.gameObject.SetActive(true);
            _endingBody.rectTransform.sizeDelta = new Vector2(720f, 80f);
            var legacyCredits = FindDescendant(root.transform, "CreditsLabel");
            if (legacyCredits != null)
            {
                legacyCredits.gameObject.SetActive(false);
            }

            _endingWorldMap = EnsureButton(root.transform, "WorldMapButton", "레벨 선택", new Vector2(0f, -140f));
            _endingTitle = EnsureButton(root.transform, "TitleButton", "타이틀", new Vector2(0f, -210f));
        }

        private void BeginEndingBeat()
        {
            if (_endingBeatRoutine != null)
            {
                StopCoroutine(_endingBeatRoutine);
                _endingBeatRoutine = null;
            }

            _endingBeatComplete = false;
            SetEndingCtaInteractable(false);
            _endingBeatRoutine = StartCoroutine(EndingBeatRoutine());
        }

        private IEnumerator EndingBeatRoutine()
        {
            // OnStateChanged applies screen art after RefreshModalForState; wait one frame.
            yield return null;
            var root = router?.EndingRoot;
            if (root == null || main == null || main.CurrentState != AppState.Ending)
            {
                FinishEndingBeatUi();
                yield break;
            }

            var worldA = FindDescendant(root.transform, "EndingWorldA")?.GetComponent<Image>();
            var worldB = FindDescendant(root.transform, "EndingWorldB")?.GetComponent<Image>();
            var flower = FindDescendant(root.transform, "EndingFlower")?.GetComponent<Image>();
            var moa = FindDescendant(root.transform, "EndingMoa")?.GetComponent<Image>();
            var vfx = FindDescendant(root.transform, "EndingVfx")?.GetComponent<Image>();

            var worlds = new[]
            {
                PresentationAssetLibrary.ForStage("1-1"),
                PresentationAssetLibrary.ForStage("2-1"),
                PresentationAssetLibrary.ForStage("3-1")
            };

            if (_endingBody != null)
            {
                _endingBody.text = "멈췄던 정원이 하나둘 숨을 고릅니다.";
                UiTypography.Apply(_endingBody, bold: false);
            }

            var recoverSeconds = PresentationTiming.EndingWorldRecoverSeconds;
            var celebrateSeconds = PresentationTiming.EndingCelebrateSeconds;
            var reduceMotion = main?.Save?.Preferences != null && main.Save.Preferences.reduceMotion;
            if (reduceMotion)
            {
                recoverSeconds = 2.4f;
                celebrateSeconds = 1.6f;
            }

            var perWorld = recoverSeconds / 3f;
            for (var i = 0; i < 3; i++)
            {
                var art = worlds[i];
                if (worldA != null && art?.background != null)
                {
                    if (i == 0)
                    {
                        worldA.sprite = art.background;
                        worldA.color = Color.white;
                        if (worldB != null) worldB.color = new Color(1f, 1f, 1f, 0f);
                    }
                    else if (worldB != null)
                    {
                        worldB.sprite = art.background;
                        yield return CrossfadeImages(worldA, worldB, Mathf.Max(0.35f, perWorld * 0.45f));
                        worldA.sprite = art.background;
                        worldA.color = Color.white;
                        worldB.color = new Color(1f, 1f, 1f, 0f);
                    }
                }

                if (flower != null)
                {
                    flower.sprite = art?.flowerBloom ?? flower.sprite;
                    yield return FadeImage(flower, 0f, 1f, Mathf.Max(0.25f, perWorld * 0.35f));
                    if (i < 2)
                    {
                        yield return FadeImage(flower, 1f, 0.25f, Mathf.Max(0.15f, perWorld * 0.2f));
                    }
                }
                else
                {
                    yield return new WaitForSecondsRealtime(perWorld);
                }
            }

            if (moa != null)
            {
                moa.sprite = PresentationAssetLibrary.Catalog?.moa?.celebrateQuietly ??
                             PresentationAssetLibrary.Catalog?.moa?.relieved ??
                             moa.sprite;
            }

            if (_endingBody != null)
            {
                _endingBody.text = "세 정원이 다시 숨을 쉬기 시작했습니다.";
                UiTypography.Apply(_endingBody, bold: false);
            }

            var celebrateElapsed = 0f;
            while (celebrateElapsed < celebrateSeconds)
            {
                celebrateElapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(celebrateElapsed / celebrateSeconds);
                if (vfx != null)
                {
                    var pulse = 0.35f + 0.45f * Mathf.Sin(t * Mathf.PI);
                    vfx.sprite = PresentationAssetLibrary.Catalog?.gameplayFx?.completionGlow ??
                                 PresentationAssetLibrary.Catalog?.gameplayFx?.flowerPetal ??
                                 vfx.sprite;
                    vfx.color = new Color(1f, 1f, 1f, pulse);
                }

                if (flower != null)
                {
                    flower.color = Color.Lerp(flower.color, Color.white, Time.unscaledDeltaTime * 2f);
                }

                yield return null;
            }

            if (vfx != null)
            {
                yield return FadeImage(vfx, vfx.color.a, 0.55f, 0.35f);
            }

            FinishEndingBeatUi();
            _endingBeatRoutine = null;
        }

        private void FinishEndingBeatUi()
        {
            _endingBeatComplete = true;
            SetEndingCtaInteractable(true);
            SelectButton(_endingWorldMap);
        }

        private void SetEndingCtaInteractable(bool enabled)
        {
            if (_endingWorldMap != null)
            {
                _endingWorldMap.interactable = enabled;
                _endingWorldMap.gameObject.SetActive(true);
            }

            if (_endingTitle != null)
            {
                _endingTitle.interactable = enabled;
                _endingTitle.gameObject.SetActive(true);
            }
        }

        private static IEnumerator CrossfadeImages(Image from, Image to, float duration)
        {
            if (from == null || to == null)
            {
                yield break;
            }

            var elapsed = 0f;
            to.color = new Color(1f, 1f, 1f, 0f);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                from.color = new Color(1f, 1f, 1f, 1f - t);
                to.color = new Color(1f, 1f, 1f, t);
                yield return null;
            }

            from.color = new Color(1f, 1f, 1f, 0f);
            to.color = Color.white;
        }

        private static IEnumerator FadeImage(Image image, float fromAlpha, float toAlpha, float duration)
        {
            if (image == null)
            {
                yield break;
            }

            var elapsed = 0f;
            var color = image.color;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
                image.color = color;
                yield return null;
            }

            color.a = toAlpha;
            image.color = color;
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
            var accent = WorldAccent(world.WorldNumber);
            image.sprite = PresentationAssetLibrary.Catalog?.worldCardFrame;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = world.Unlocked ? Color.white : new Color(0.35f, 0.37f, 0.42f, 0.92f);

            var art = PresentationAssetLibrary.ForStage($"{world.WorldNumber}-1");
            var preview = new GameObject("WorldPreview", typeof(RectTransform), typeof(Image));
            preview.transform.SetParent(go.transform, false);
            var previewRt = preview.GetComponent<RectTransform>();
            previewRt.anchorMin = new Vector2(0f, 1f);
            previewRt.anchorMax = new Vector2(1f, 1f);
            previewRt.pivot = new Vector2(0.5f, 1f);
            previewRt.sizeDelta = new Vector2(-24f, 112f);
            previewRt.anchoredPosition = new Vector2(0f, -20f);
            var previewImage = preview.GetComponent<Image>();
            previewImage.sprite = art?.background;
            previewImage.color = world.Unlocked ? Color.white : new Color(0.35f, 0.35f, 0.38f, 0.85f);
            previewImage.preserveAspect = false;
            previewImage.raycastTarget = false;

            var band = new GameObject("AccentBand", typeof(RectTransform), typeof(Image));
            band.transform.SetParent(go.transform, false);
            var brt = band.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(0f, 10f);
            brt.anchoredPosition = Vector2.zero;
            band.GetComponent<Image>().color = world.Unlocked ? accent : UiTheme.Disabled;

            var title = EnsureLabel(
                go.transform,
                "WorldTitle",
                world.Unlocked ? world.WorldTitle : $"{world.WorldTitle} (잠김)",
                new Vector2(0f, 145f),
                24f);
            title.fontStyle = FontStyles.Bold;

            if (!world.Unlocked)
            {
                var lockHint = EnsureLabel(
                    go.transform,
                    "LockHint",
                    "이전 월드의 밤꽃을 피워 주세요",
                    new Vector2(0f, 102f),
                    16f);
                lockHint.color = new Color(0.75f, 0.75f, 0.8f, 1f);
            }

            return go.transform;
        }

        private static Color WorldAccent(int worldNumber) => worldNumber switch
        {
            2 => new Color(0.35f, 0.72f, 0.68f, 1f), // 바람종 협곡 — 청록
            3 => new Color(0.62f, 0.48f, 0.88f, 1f), // 별뿌리 온실 — 보라
            _ => new Color(0.83f, 0.55f, 0.28f, 1f)  // 노을 과수원 — 황토
        };

        private static Button CreateStageNodeButton(Transform parent, StageNodeViewModel node, Vector2 pos)
        {
            var label = $"{node.StageId}   {node.TimeLabel}";
            var button = EnsureButton(parent, $"Stage_{node.StageId}", label, pos);
            button.interactable = node.Unlocked;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = node.Unlocked ? Color.white : new Color(0.38f, 0.4f, 0.44f, 0.82f);
            }

            var catalog = PresentationAssetLibrary.Catalog;
            var completed = !string.IsNullOrEmpty(node.CompletionIcon);
            var sprite = !node.Unlocked ? catalog?.iconLock : completed
                ? (node.StageId.EndsWith("-4") ? catalog?.iconFlower : catalog?.iconDoor)
                : catalog?.iconDoor;
            var status = UiFactory.EnsureIcon(button.transform, "StatusIcon", sprite,
                new Vector2(24f, 24f), new Vector2(-128f, 0f));
            status.color = !node.Unlocked ? new Color(1f, 1f, 1f, 0.45f) : completed
                ? UiTheme.Mint : new Color(1f, 1f, 1f, 0.3f);

            var tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.fontSize = 20;
                tmp.color = node.Unlocked ? UiTheme.Ivory : new Color(0.55f, 0.55f, 0.58f, 1f);
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
                    image.color = new Color(0.38f, 0.4f, 0.44f, 0.82f);
                }
                else
                {
                    image.color = Color.white;
                }

                var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.color = node.Unlocked
                        ? UiTheme.Ivory
                        : new Color(0.55f, 0.55f, 0.58f, 1f);
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

        private Button FocusedWorldMapButton()
        {
            if (_worldMapVm == null || string.IsNullOrWhiteSpace(_worldMapVm.FocusedStageId))
            {
                return null;
            }

            return _stageButtons.TryGetValue(_worldMapVm.FocusedStageId, out var button)
                ? button
                : null;
        }

        private Button SelectedModalButton()
        {
            if (_modalButtons.Count == 0)
            {
                return null;
            }

            var index = _modalVm != null ? _modalVm.SelectedIndex : 0;
            return _modalButtons[Mathf.Clamp(index, 0, _modalButtons.Count - 1)];
        }

        private void RebuildModalButtons(GameObject root, ModalViewModel modal)
        {
            if (root == null || modal?.Options == null)
            {
                return;
            }

            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button != null && button.name.EndsWith("Button"))
                {
                    button.gameObject.SetActive(false);
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
                if (image != null) image.color = Color.white;
            }
        }

        private void CycleTitleFocus(int delta)
        {
            var buttons = new List<Button>();
            if (_titleContinue != null &&
                _titleContinue.gameObject.activeInHierarchy &&
                _titleContinue.interactable)
            {
                buttons.Add(_titleContinue);
            }

            if (_titleNewGame != null && _titleNewGame.gameObject.activeInHierarchy)
            {
                buttons.Add(_titleNewGame);
            }

            if (_titleReplayOpening != null && _titleReplayOpening.gameObject.activeInHierarchy)
            {
                buttons.Add(_titleReplayOpening);
            }

            if (_titleSettings != null && _titleSettings.gameObject.activeInHierarchy)
            {
                buttons.Add(_titleSettings);
            }

            if (buttons.Count == 0 || delta == 0)
            {
                return;
            }

            var current = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            var index = 0;
            for (var i = 0; i < buttons.Count; i++)
            {
                if (buttons[i].gameObject == current)
                {
                    index = i;
                    break;
                }
            }

            index = (index + delta + buttons.Count * 8) % buttons.Count;
            SelectButton(buttons[index]);
        }

        private void ToggleOpeningFocus()
        {
            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == _openingSkip?.gameObject)
            {
                SelectButton(_openingContinue);
            }
            else
            {
                SelectButton(_openingSkip);
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

            var titleTransform = FindDescendant(root.transform, "TitleLabel") ??
                                 FindDescendant(root.transform, "Label");
            var tmp = titleTransform != null
                ? titleTransform.GetComponent<TextMeshProUGUI>()
                : null;
            if (tmp == null)
            {
                tmp = CreateTitleLabel(root.transform, text);
            }

            if (tmp == null)
            {
                return;
            }

            tmp.gameObject.SetActive(true);
            tmp.text = text;
            tmp.fontSize = Mathf.Max(tmp.fontSize, 40);
            tmp.alignment = TextAlignmentOptions.Center;
            var rt = tmp.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -UiTheme.SafeMargin);
            rt.sizeDelta = new Vector2(720f, 64f);
            UiTypography.Apply(tmp, bold: true);
        }

        private static TextMeshProUGUI CreateTitleLabel(Transform parent, string text)
        {
            var go = new GameObject("TitleLabel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(720f, 64f);
            rt.anchoredPosition = new Vector2(0f, -UiTheme.SafeMargin);
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
            var existing = FindDescendant(parent, name);
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
            var existing = FindDescendant(parent, name);
            return UiFactory.CreateButton(
                existing != null ? existing.parent : parent,
                name,
                label,
                anchoredPos,
                null,
                width: Mathf.Max(280f, UiTheme.ButtonWidth * 0.9f),
                height: Mathf.Max(56f, UiTheme.ButtonMinHeight));
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDescendant(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void SetClick(Button button, UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                main?.Gameplay?.PlayUiSubmit();
                action?.Invoke();
            });
        }
    }
}
