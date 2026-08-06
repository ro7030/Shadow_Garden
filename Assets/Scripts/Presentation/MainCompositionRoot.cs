using ShadowGarden.Core;
using ShadowGarden.Infrastructure;
using ShadowGarden.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Main scene composition root: AppState flow, production uGUI, save, gameplay.
    /// TestField remains a separate composition and is not referenced here.
    /// </summary>
    public sealed class MainCompositionRoot : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private StageCatalogAsset stageCatalog;
        [SerializeField] private AppScreenRouter screenRouter;
        [SerializeField] private MainGameplayHost gameplayHost;
        [SerializeField] private MainFlowScreens flowScreens;
        [SerializeField] private MainOverlayController overlay;
        [SerializeField] private MainScreenArtDecorator screenArt;
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private bool usePlayerPrefs = true;

        private GameFlowController _flow;
        private InputRouter _input;
        private SaveService _save;
        private string _pendingStageId = "1-1";
        private string _lastClearedStageId;
        private long _lastClearElapsedMs;
        private GameOverCause _lastGameOverCause = GameOverCause.CliffFall;
        private bool _playPaused;
        private bool _openingReturnToTitle;

        public GameFlowController Flow => _flow;
        public SaveService Save => _save;
        public StageCatalogAsset Catalog => stageCatalog;
        public InputRouter Input => _input;
        public MainGameplayHost Gameplay => gameplayHost;
        public MainOverlayController Overlay => overlay;
        public AppState CurrentState => _flow?.Current ?? AppState.Title;
        public string PendingStageId => _pendingStageId;
        public string LastClearedStageId => _lastClearedStageId;
        public long LastClearElapsedMilliseconds => _lastClearElapsedMs;
        public GameOverCause LastGameOverCause => _lastGameOverCause;
        public bool IsPlayPaused => _playPaused;

        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = false;
#endif
            UiTypography.ApplyDefaultSettings();
            EnsureCanvas();
            EnsureScreenRouter();
            EnsureGameplayHost();
            EnsureFlowScreens();
            EnsureOverlay();
            EnsureScreenArt();
            ConfigureUiNavigationOwner(AppState.Title);

            _flow = new GameFlowController(AppState.Title);
            _flow.StateChanged += OnStateChanged;

            if (inputActions != null)
            {
                _input = new InputRouter(inputActions);
                _input.PauseRequested += TogglePauseFromInput;
            }

            _save = usePlayerPrefs
                ? new SaveService(new PlayerPrefsSaveRepository())
                : new SaveService(new MemoryProgressSaveRepository(), new MemoryUiPreferencesRepository());
            _save.LoadAll();
            ApplyUiPreferences();

            if (!string.IsNullOrWhiteSpace(_save.Progress?.lastStageId))
            {
                _pendingStageId = _save.Progress.lastStageId;
            }

            gameplayHost?.Bind(this);
            flowScreens?.Bind(this, screenRouter);
            overlay?.Bind(this, mainCanvas != null ? mainCanvas.transform : null);
            screenArt?.Bind(this, screenRouter);
        }

        private void Start()
        {
            BootInitialState();
            flowScreens?.RefreshTitleBranch();
            flowScreens?.RefreshWorldMapUnlock();
            // Flow refreshes may rebuild or reparent controls; layout is always the last step.
            screenArt?.ApplyLayout(CurrentState);
            if (CurrentState != AppState.Playing)
            {
                gameplayHost?.PlayMenuMusic();
            }
        }

        /// <summary>WebGL shell / first-gesture bridge — unlocks audio after a user click.</summary>
        public void UnlockAudioFromWebShell()
        {
            gameplayHost?.UnlockAudioFromUserGesture();
        }

        private void OnDestroy()
        {
            if (_flow != null)
            {
                _flow.StateChanged -= OnStateChanged;
            }

            flowScreens?.UnbindInput();
            if (_input != null)
            {
                _input.PauseRequested -= TogglePauseFromInput;
                _input.Dispose();
            }
        }

        public void BindForTests(
            GameFlowController flow,
            SaveService save,
            InputRouter input,
            AppScreenRouter router,
            StageCatalogAsset catalog,
            MainGameplayHost host = null)
        {
            if (_flow != null)
            {
                _flow.StateChanged -= OnStateChanged;
            }

            _flow = flow ?? new GameFlowController(AppState.Title);
            _flow.StateChanged += OnStateChanged;
            _save = save ?? new SaveService(new MemoryProgressSaveRepository(), new MemoryUiPreferencesRepository());
            _input = input;
            screenRouter = router;
            stageCatalog = catalog;
            if (host != null)
            {
                gameplayHost = host;
            }

            if (!string.IsNullOrWhiteSpace(_save.Progress?.lastStageId))
            {
                _pendingStageId = _save.Progress.lastStageId;
            }

            ApplyUiPreferences();
            gameplayHost?.Bind(this);
            flowScreens?.Bind(this, screenRouter);
            EnsureOverlay();
            if (mainCanvas != null)
            {
                overlay?.Bind(this, mainCanvas.transform);
            }
            EnsureScreenArt();
            screenArt?.Bind(this, screenRouter);
        }

        public AppStateChangeResult RequestState(AppState to)
        {
            if (_flow == null)
            {
                return AppStateChangeResult.Reject(AppState.Title, to, "not_ready");
            }

            _input?.SetTransitionInputLock(true);
            var result = _flow.TryTransition(to);
            if (!result.Accepted)
            {
                _input?.SetTransitionInputLock(false);
                _input?.ApplyForAppState(_flow.Current);
            }

            return result;
        }

        public void ContinueFromTitle()
        {
            if (_save == null || !_save.CanContinue())
            {
                return;
            }

            RequestState(AppState.WorldMap);
        }

        public void StartNewGameFromTitle()
        {
            // 「처음 시작하기」 → 오프닝 재생 후 레벨 선택.
            _save?.ResetProgressForNewGame();
            _pendingStageId = "1-1";
            _openingReturnToTitle = false;
            RequestState(AppState.Opening);
        }

        public void CompleteOpening()
        {
            _save?.MarkOpeningSeen();
            if (_openingReturnToTitle)
            {
                _openingReturnToTitle = false;
                RequestState(AppState.Title);
                return;
            }

            RequestState(AppState.WorldMap);
        }

        public void ReplayOpening()
        {
            overlay?.HideAllForStateChange();
            _playPaused = false;
            _openingReturnToTitle = true;
            if (CurrentState == AppState.Playing)
            {
                RequestState(AppState.Title);
            }

            if (CurrentState != AppState.Opening)
            {
                RequestState(AppState.Opening);
            }
        }

        public void ReturnToTitle()
        {
            overlay?.HideAllForStateChange();
            _playPaused = false;
            RequestState(AppState.Title);
        }

        public void OpenSettingsFromTitle()
        {
            EnsureOverlay();
            overlay?.OpenSettings(AppState.Title);
        }

        public void OpenPause()
        {
            EnsureOverlay();
            overlay?.OpenPause();
        }

        /// <summary>Esc and the HUD pause button share this toggle path.</summary>
        public void TogglePauseFromInput()
        {
            EnsureOverlay();
            if (overlay == null || CurrentState != AppState.Playing)
            {
                return;
            }

            if (overlay.IsFocusOverlayVisible)
            {
                return;
            }

            if (overlay.IsSettingsOpen)
            {
                overlay.CloseSettings();
                return;
            }

            if (overlay.IsPauseOpen)
            {
                overlay.ClosePause(true);
                return;
            }

            overlay.OpenPause();
        }

        public void SetPlayPaused(bool paused)
        {
            _playPaused = paused;
            gameplayHost?.SetExternalPause(paused);
            if (CurrentState != AppState.Playing || _input == null)
            {
                return;
            }

            if (paused)
            {
                // Pause/settings overlays need UI Navigate/Submit; Esc pause toggle stays armed.
                _input.SetPauseAvailableInUi(true);
                _input.SetMapMode(InputMapMode.Ui);
            }
            else
            {
                _input.SetPauseAvailableInUi(false);
                _input.EnableGameplay(true);
                _input.SetMapMode(InputMapMode.Gameplay);
            }
        }

        public void ApplyUiPreferences()
        {
            gameplayHost?.PlayHud?.ApplyPreferences();
            gameplayHost?.ApplyAudioPreferences();
            gameplayHost?.ApplyReduceMotion(
                _save?.Preferences != null && _save.Preferences.reduceMotion);
        }

        public void NotifyFocusLost()
        {
            EnsureOverlay();
            overlay?.ShowFocusLost();
        }

        public void NotifyFocusGained()
        {
        }

        public void StartStage(string stageId)
        {
            if (!string.IsNullOrWhiteSpace(stageId))
            {
                _pendingStageId = stageId.Trim();
                _save?.RecordStageSelected(_pendingStageId);
            }

            RequestState(AppState.Playing);
        }

        public void NotifyGameOver(GameOverCause cause = GameOverCause.CliffFall)
        {
            _lastGameOverCause = cause;
            _save?.RecordStageFailed(_pendingStageId);
            RequestState(AppState.GameOver);
        }

        public void NotifyCleared(string stageId, long elapsedMilliseconds)
        {
            _lastClearedStageId = string.IsNullOrWhiteSpace(stageId) ? _pendingStageId : stageId.Trim();
            _lastClearElapsedMs = elapsedMilliseconds;
            _save?.RecordStageCleared(_lastClearedStageId, elapsedMilliseconds);
            RequestState(AppState.Cleared);
        }

        public void RetryFromGameOver() => RequestState(AppState.Playing);

        /// <summary>
        /// Pause menu retry: stay in Playing and rebuild the active stage.
        /// Must not call RequestState(Playing) — Playing→Playing is rejected.
        /// </summary>
        public void RetryFromPause()
        {
            if (CurrentState != AppState.Playing)
            {
                RetryFromGameOver();
                return;
            }

            overlay?.ClosePause(false);
            _playPaused = false;
            gameplayHost?.SetExternalPause(false);
            gameplayHost?.RestartActiveStage();
            _input?.EnableGameplay(true);
            _input?.ApplyForAppState(AppState.Playing);
        }

        public void ReturnToWorldMap() => RequestState(AppState.WorldMap);

        public void EnterEndingFromCleared() => RequestState(AppState.Ending);

        public void FinishEnding() => RequestState(AppState.Title);

        public void FinishEndingToWorldMap() => RequestState(AppState.WorldMap);

        public string ResolveNextStageAfterClear() =>
            _save?.ResolveNextStageId(stageCatalog, _lastClearedStageId ?? _pendingStageId);

        public bool IsFinalStageClear() =>
            string.Equals(_lastClearedStageId ?? _pendingStageId, "3-4", System.StringComparison.Ordinal);

        public bool IsWorldFinaleClear()
        {
            var id = _lastClearedStageId ?? _pendingStageId;
            return id != null && id.EndsWith("-4") && !IsFinalStageClear();
        }

        public void ContinueAfterClear()
        {
            if (IsFinalStageClear())
            {
                EnterEndingFromCleared();
                return;
            }

            var next = ResolveNextStageAfterClear();
            if (!string.IsNullOrWhiteSpace(next))
            {
                StartStage(next);
                return;
            }

            ReturnToWorldMap();
        }

        public void ApplyModalSelection(string optionId)
        {
            if (string.IsNullOrWhiteSpace(optionId))
            {
                return;
            }

            switch (optionId)
            {
                case "retry":
                    RetryFromGameOver();
                    break;
                case "worldmap":
                    ReturnToWorldMap();
                    break;
                case "next":
                    ContinueAfterClear();
                    break;
                case "ending":
                    EnterEndingFromCleared();
                    break;
            }
        }

        private void BootInitialState()
        {
            var initial = AppState.Title;
            _flow.Boot(initial);
            screenRouter?.Show(initial);
            _input?.SetTransitionInputLock(false);
            _input?.ApplyForAppState(initial);
            gameplayHost?.StopPlay();
            overlay?.HideAllForStateChange();
        }

        private void OnStateChanged(AppState from, AppState to)
        {
            ConfigureUiNavigationOwner(to);
            overlay?.HideAllForStateChange();
            _playPaused = false;
            _input?.SetPauseAvailableInUi(false);
            screenRouter?.Show(to);
            _input?.SetTransitionInputLock(false);
            _input?.ApplyForAppState(to);

            if (to == AppState.Playing)
            {
                gameplayHost?.Bind(this);
                if (from == AppState.GameOver)
                {
                    gameplayHost?.RestartActiveStage();
                }
                else
                {
                    gameplayHost?.BeginStage(_pendingStageId);
                }
            }
            else if (from == AppState.Playing)
            {
                if (to == AppState.WorldMap || to == AppState.Title || to == AppState.Ending)
                {
                    gameplayHost?.StopPlay();
                }
            }

            if (to == AppState.Title)
            {
                flowScreens?.RefreshTitleBranch();
                gameplayHost?.PlayMenuMusic();
            }

            if (to == AppState.WorldMap)
            {
                flowScreens?.RefreshWorldMapUnlock();
                gameplayHost?.PlayMenuMusic();
            }

            if (to == AppState.Opening)
            {
                flowScreens?.RefreshOpening();
                gameplayHost?.PlayMenuMusic();
            }

            if (to == AppState.Ending)
            {
                gameplayHost?.PlayMenuMusic();
            }

            if (to == AppState.GameOver || to == AppState.Cleared || to == AppState.Ending)
            {
                gameplayHost?.PlayHud?.SetVisible(false);
                flowScreens?.RefreshModalForState(to);
            }

            // One explicit owner guarantees deterministic layout after every refresh,
            // including Ending -> Title and repeated Opening playback.
            screenArt?.ApplyLayout(to);
        }

        private static void ConfigureUiNavigationOwner(AppState state)
        {
            if (EventSystem.current == null)
            {
                return;
            }

            // Flow screens use MainFlowScreens as the sole keyboard owner. Playing overlays
            // use the standard EventSystem navigation while gameplay input remains paused.
            EventSystem.current.sendNavigationEvents = state == AppState.Playing;
        }

        private void EnsureCanvas()
        {
            if (mainCanvas == null)
            {
                mainCanvas = FindFirstObjectByType<Canvas>();
            }

            if (mainCanvas != null)
            {
                UiFactory.ConfigureCanvas(mainCanvas);
            }
        }

        private void EnsureScreenRouter()
        {
            if (screenRouter == null)
            {
                screenRouter = GetComponent<AppScreenRouter>();
            }
        }

        private void EnsureGameplayHost()
        {
            if (gameplayHost == null)
            {
                gameplayHost = GetComponent<MainGameplayHost>();
            }

            if (gameplayHost == null)
            {
                gameplayHost = gameObject.AddComponent<MainGameplayHost>();
            }
        }

        private void EnsureFlowScreens()
        {
            if (flowScreens == null)
            {
                flowScreens = GetComponent<MainFlowScreens>();
            }

            if (flowScreens == null)
            {
                flowScreens = gameObject.AddComponent<MainFlowScreens>();
            }
        }

        private void EnsureOverlay()
        {
            if (overlay == null)
            {
                overlay = GetComponent<MainOverlayController>();
            }

            if (overlay == null)
            {
                overlay = gameObject.AddComponent<MainOverlayController>();
            }

            if (mainCanvas == null)
            {
                EnsureCanvas();
            }
        }

        private void EnsureScreenArt()
        {
            if (screenArt == null)
            {
                screenArt = GetComponent<MainScreenArtDecorator>() ??
                            gameObject.AddComponent<MainScreenArtDecorator>();
            }
        }

#if UNITY_EDITOR
        public static void WireSerialized(
            MainCompositionRoot root,
            InputActionAsset actions,
            StageCatalogAsset catalog,
            AppScreenRouter router)
        {
            root.inputActions = actions;
            root.stageCatalog = catalog;
            root.screenRouter = router;
        }
#endif
    }
}
