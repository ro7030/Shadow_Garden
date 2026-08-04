using ShadowGarden.Infrastructure;
using ShadowGarden.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Main scene composition root: AppState flow, screens, input maps, save, and 1-1 gameplay host.
    /// TestField remains a separate composition and is not referenced here.
    /// </summary>
    public sealed class MainCompositionRoot : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private StageCatalogAsset stageCatalog;
        [SerializeField] private AppScreenRouter screenRouter;
        [SerializeField] private MainGameplayHost gameplayHost;
        [SerializeField] private MainFlowScreens flowScreens;
        [SerializeField] private bool usePlayerPrefs = true;

        private GameFlowController _flow;
        private InputRouter _input;
        private SaveService _save;
        private string _pendingStageId = "1-1";

        public GameFlowController Flow => _flow;
        public SaveService Save => _save;
        public StageCatalogAsset Catalog => stageCatalog;
        public InputRouter Input => _input;
        public MainGameplayHost Gameplay => gameplayHost;
        public AppState CurrentState => _flow?.Current ?? AppState.Title;
        public string PendingStageId => _pendingStageId;

        private void Awake()
        {
            EnsureScreenRouter();
            EnsureGameplayHost();
            EnsureFlowScreens();

            _flow = new GameFlowController(AppState.Title);
            _flow.StateChanged += OnStateChanged;

            if (inputActions != null)
            {
                _input = new InputRouter(inputActions);
            }

            _save = usePlayerPrefs
                ? new SaveService(new PlayerPrefsSaveRepository())
                : new SaveService(new MemoryProgressSaveRepository(), new MemoryUiPreferencesRepository());
            _save.LoadAll();

            gameplayHost?.Bind(this);
            flowScreens?.Bind(this, screenRouter);
        }

        private void Start()
        {
            BootInitialState();
            flowScreens?.RefreshWorldMapUnlock();
        }

        private void OnDestroy()
        {
            if (_flow != null)
            {
                _flow.StateChanged -= OnStateChanged;
            }

            _input?.Dispose();
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

            gameplayHost?.Bind(this);
            flowScreens?.Bind(this, screenRouter);
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
            if (_save?.Preferences != null && _save.Preferences.openingSeen)
            {
                RequestState(AppState.WorldMap);
            }
            else
            {
                RequestState(AppState.Opening);
            }
        }

        public void CompleteOpening()
        {
            _save?.MarkOpeningSeen();
            RequestState(AppState.WorldMap);
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

        public void NotifyGameOver() => RequestState(AppState.GameOver);

        public void NotifyCleared(string stageId, long elapsedMilliseconds)
        {
            _save?.RecordStageCleared(stageId, elapsedMilliseconds);
            RequestState(AppState.Cleared);
        }

        public void RetryFromGameOver()
        {
            RequestState(AppState.Playing);
        }

        public void ReturnToWorldMap() => RequestState(AppState.WorldMap);

        public void EnterEndingFromCleared() => RequestState(AppState.Ending);

        public void FinishEnding() => RequestState(AppState.Title);

        private void BootInitialState()
        {
            var initial = AppState.Title;
            _flow.Boot(initial);
            screenRouter?.Show(initial);
            _input?.SetTransitionInputLock(false);
            _input?.ApplyForAppState(initial);
            gameplayHost?.StopPlay();
        }

        private void OnStateChanged(AppState from, AppState to)
        {
            screenRouter?.Show(to);
            _input?.SetTransitionInputLock(false);
            _input?.ApplyForAppState(to);

            if (to == AppState.Playing)
            {
                gameplayHost?.Bind(this);
                if (from == AppState.GameOver || from == AppState.Cleared)
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
                if (to == AppState.WorldMap || to == AppState.Title)
                {
                    gameplayHost?.StopPlay();
                }
            }

            if (to == AppState.WorldMap)
            {
                flowScreens?.RefreshWorldMapUnlock();
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
