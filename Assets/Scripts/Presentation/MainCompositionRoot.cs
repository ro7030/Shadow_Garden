using ShadowGarden.Infrastructure;
using ShadowGarden.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Main scene composition root: AppState flow, screen roots, input maps, save boot.
    /// Does not host TestField puzzle play — StageSession stays on TestField / later stages.
    /// </summary>
    public sealed class MainCompositionRoot : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private StageCatalogAsset stageCatalog;
        [SerializeField] private AppScreenRouter screenRouter;
        [SerializeField] private bool usePlayerPrefs = true;

        private GameFlowController _flow;
        private InputRouter _input;
        private SaveService _save;

        public GameFlowController Flow => _flow;
        public SaveService Save => _save;
        public StageCatalogAsset Catalog => stageCatalog;
        public InputRouter Input => _input;
        public AppState CurrentState => _flow?.Current ?? AppState.Title;

        private void Awake()
        {
            EnsureScreenRouter();
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
        }

        private void Start()
        {
            BootInitialState();
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
            StageCatalogAsset catalog)
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
            if (stageCatalog != null && !string.IsNullOrWhiteSpace(stageId))
            {
                _save?.RecordStageSelected(stageId);
            }

            RequestState(AppState.Playing);
        }

        public void NotifyGameOver() => RequestState(AppState.GameOver);

        public void NotifyCleared(string stageId, long elapsedMilliseconds)
        {
            _save?.RecordStageCleared(stageId, elapsedMilliseconds);
            if (stageId == "3-4")
            {
                RequestState(AppState.Cleared);
                return;
            }

            RequestState(AppState.Cleared);
        }

        public void RetryFromGameOver() => RequestState(AppState.Playing);

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
        }

        private void OnStateChanged(AppState from, AppState to)
        {
            screenRouter?.Show(to);
            _input?.SetTransitionInputLock(false);
            _input?.ApplyForAppState(to);
        }

        private void EnsureScreenRouter()
        {
            if (screenRouter == null)
            {
                screenRouter = GetComponent<AppScreenRouter>();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor helper used when constructing Main scene roots programmatically.
        /// </summary>
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
