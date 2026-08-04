using System.Collections;
using ShadowGarden.Core;
using ShadowGarden.Infrastructure;
using ShadowGarden.Runtime;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Main-scene StageSession host for the 1-1 vertical slice.
    /// Presenters never recompute Core rules — they only display StageSession results.
    /// </summary>
    public sealed class MainGameplayHost : MonoBehaviour
    {
        [SerializeField] private BoardPresenter boardPresenter;
        [SerializeField] private PlayerPresenter playerPresenter;
        [SerializeField] private MainPlayHud playHud;
        [SerializeField] private OnboardingHintsPresenter onboarding;
        [SerializeField] private Transform hudParent;

        private MainCompositionRoot _main;
        private readonly UnityClock _clock = new UnityClock();
        private ApplicationFocusBridge _focusBridge;
        private StageSession _session;
        private StageDefinition _definition;
        private string _stageId;
        private bool _active;
        private bool _sequencing;
        private bool _nearLampPulseArmed = true;
        private GridPosition _lastPlayerPos;
        private long _clearElapsedMs;
        private int _restartCount;

        public StageSession Session => _session;
        public StageDefinition Definition => _definition;
        public bool IsSequencing => _sequencing;
        public int RestartCount => _restartCount;
        public OnboardingHintsPresenter Onboarding => onboarding;
        public MainPlayHud PlayHud => playHud;

        private bool _externalPause;
        private bool _reduceMotion;

        public void ApplyReduceMotion(bool enabled)
        {
            _reduceMotion = enabled;
            boardPresenter?.SetReduceMotion(enabled);
            playHud?.ApplyPreferences();
        }

        public void Bind(MainCompositionRoot main)
        {
            UnsubscribeInput();
            _main = main;
            EnsureComponents();
            playHud?.Bind(main);
            SubscribeInput();
        }

        public void SetExternalPause(bool paused)
        {
            _externalPause = paused;
            if (_session == null || !_active)
            {
                return;
            }

            // Reuse focus pause contract so timer freezes without failing the stage.
            _session.SetFocus(!paused);
            if (paused)
            {
                _main?.Input?.EnableGameplay(false);
            }
        }

        public void BeginStage(string stageId)
        {
            EnsureComponents();
            _stageId = stageId;
            if (_main?.Catalog == null || !_main.Catalog.TryGetById(stageId, out var asset) || asset == null)
            {
                Debug.LogError($"Main catalog missing stage '{stageId}'.");
                return;
            }

            var definition = StageDefinitionFactory.CreateFromAsset(asset);
            BeginDefinition(definition);
        }

        public void BeginDefinition(StageDefinition definition)
        {
            EnsureComponents();
            TeardownSession();
            _definition = definition;
            _stageId = definition.StageId;
            _session = new StageSession(definition);
            _session.CommandApplied += OnCommandApplied;
            _active = true;
            _sequencing = false;
            _nearLampPulseArmed = true;
            _lastPlayerPos = definition.PlayerStart;
            _clearElapsedMs = 0;

            boardPresenter.Build(definition);
            playerPresenter.Snap(definition.PlayerStart);
            onboarding.ResetProgress();
            playHud.EnsureBuilt(hudParent != null ? hudParent : transform);
            playHud.Bind(_main);
            playHud.SetVisible(true);
            playHud.Render(definition, _session.State);
            ApplyReduceMotion(_main?.Save?.Preferences != null && _main.Save.Preferences.reduceMotion);
            BoardCameraFitter.Apply(Camera.main, definition.BoardSize);
            ApplyCameraLook();

            _main?.Input?.EnableGameplay(true);
            _main?.Input?.CancelLockAndBuffer();
            _main?.Input?.ApplyForAppState(AppState.Playing);
        }

        public void RestartActiveStage()
        {
            if (_definition == null)
            {
                return;
            }

            _restartCount++;
            _main?.Input?.CancelLockAndBuffer();
            BeginDefinition(_definition);
        }

        public void StopPlay()
        {
            TeardownSession();
            _active = false;
            playHud?.SetVisible(false);
            _main?.Input?.EnableGameplay(false);
        }

        private void Awake()
        {
            EnsureComponents();
            _focusBridge = gameObject.GetComponent<ApplicationFocusBridge>();
            if (_focusBridge == null)
            {
                _focusBridge = gameObject.AddComponent<ApplicationFocusBridge>();
            }

            _focusBridge.FocusChanged += OnFocusChanged;
        }

        private void SubscribeInput()
        {
            if (_main?.Input == null)
            {
                return;
            }

            _main.Input.MoveRequested -= OnMove;
            _main.Input.RotateRequested -= OnRotate;
            _main.Input.ResetRequested -= OnReset;
            _main.Input.MoveRequested += OnMove;
            _main.Input.RotateRequested += OnRotate;
            _main.Input.ResetRequested += OnReset;
        }

        private void UnsubscribeInput()
        {
            if (_main?.Input == null)
            {
                return;
            }

            _main.Input.MoveRequested -= OnMove;
            _main.Input.RotateRequested -= OnRotate;
            _main.Input.ResetRequested -= OnReset;
        }

        private void OnDestroy()
        {
            if (_focusBridge != null)
            {
                _focusBridge.FocusChanged -= OnFocusChanged;
            }

            UnsubscribeInput();
            TeardownSession();
        }

        private void OnEnable()
        {
            SubscribeInput();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
        }

        private void Update()
        {
            if (!_active || _session == null || _sequencing)
            {
                return;
            }

            if (_main?.CurrentState != AppState.Playing)
            {
                return;
            }

            _main.Input?.Tick(Time.unscaledDeltaTime);
            _session.Tick(_clock.DeltaMilliseconds);
            RefreshView();
            UpdateLampApproachPulse();
        }

        private void OnMove(CardinalDirection direction)
        {
            if (!_active || _sequencing || _session == null)
            {
                return;
            }

            var from = _session.State.PlayerPosition;
            var result = _session.Move(direction);
            if (!result.Move.HasValue || result.Move.Value.Outcome == MoveOutcome.Blocked)
            {
                return;
            }

            onboarding.NotifyMoved();
            _main.Input?.LockForSeconds(PresentationTiming.MoveSeconds);
            var to = result.Move.Value.TargetPosition;
            if (result.Move.Value.Outcome == MoveOutcome.Moved ||
                result.Move.Value.Outcome == MoveOutcome.ExitReached ||
                result.Move.Value.Outcome == MoveOutcome.NightFlowerReached ||
                result.Move.Value.Outcome == MoveOutcome.CliffDeath ||
                result.Move.Value.Outcome == MoveOutcome.OverlapDeath)
            {
                playerPresenter.AnimateMove(from, to, PresentationTiming.MoveSeconds);
            }
        }

        private void OnRotate(int turns)
        {
            if (!_active || _sequencing || _session == null)
            {
                return;
            }

            if (!_definition.TryGetLampAt(_session.State.PlayerPosition, out var lamp))
            {
                return;
            }

            var result = _session.Rotate(turns);
            if (result.Events.Length == 0)
            {
                return;
            }

            onboarding.NotifyRotated();
            _main.Input?.LockForSeconds(PresentationTiming.RotateSeconds);
            boardPresenter.PulseChannelPillars(_definition, lamp.Channel, PresentationTiming.RotateSeconds);
        }

        private void OnReset()
        {
            if (!_active || _session == null)
            {
                return;
            }

            if (_sequencing)
            {
                return;
            }

            // R always restarts during Playing (immediate reset).
            if (_main.CurrentState == AppState.Playing)
            {
                onboarding?.NotifyResetUsed();
                RestartActiveStage();
            }
        }

        private void OnFocusChanged(bool hasFocus)
        {
            if (!_active || _sequencing || _session == null)
            {
                return;
            }

            if (_main?.CurrentState != AppState.Playing)
            {
                return;
            }

            if (_externalPause)
            {
                return;
            }

            if (!hasFocus)
            {
                _session.SetFocus(false);
                _main?.NotifyFocusLost();
                return;
            }

            _main?.NotifyFocusGained();
            // Timer resumes only after the focus-return overlay is dismissed.
        }

        private void OnCommandApplied(StageCommandResult result)
        {
            foreach (var stageEvent in result.Events)
            {
                switch (stageEvent.Type)
                {
                    case StageEventType.GameOverStarted:
                        StartCoroutine(GameOverSequence(stageEvent.GameOverCause ?? GameOverCause.CliffFall));
                        break;
                    case StageEventType.TimerWarning30:
                        playHud?.ShowTransientWarning("남은 시간 30초", UiTheme.Brass);
                        break;
                    case StageEventType.TimerWarning10:
                        playHud?.ShowTransientWarning("남은 시간 10초!", UiTheme.Coral);
                        break;
                    case StageEventType.ClearStarted:
                        _clearElapsedMs = _definition.TimeLimitSeconds * 1000L -
                                          result.NextState.RemainingMilliseconds;
                        if (_clearElapsedMs < 0)
                        {
                            _clearElapsedMs = 0;
                        }

                        StartCoroutine(ClearSequence());
                        break;
                }
            }
        }

        private IEnumerator ClearSequence()
        {
            if (_sequencing)
            {
                yield break;
            }

            _sequencing = true;
            _main?.Input?.EnableGameplay(false);

            if (_definition.ClearGoalType == ClearGoalType.NightFlower)
            {
                var bloom = _reduceMotion
                    ? PresentationTiming.NightFlowerBloomSeconds * 0.35f
                    : PresentationTiming.NightFlowerBloomSeconds;
                yield return new WaitForSecondsRealtime(bloom);
            }
            else
            {
                var door = boardPresenter.PlayDoorOpen(_definition, PresentationTiming.DoorOpenSeconds);
                if (door != null)
                {
                    yield return door;
                }
                else
                {
                    yield return new WaitForSecondsRealtime(PresentationTiming.DoorOpenSeconds);
                }

                var pass = playerPresenter.AnimatePassThroughDoor(
                    _definition.GoalPosition,
                    PresentationTiming.GoalPassSeconds);
                if (pass != null)
                {
                    yield return pass;
                }
                else
                {
                    yield return new WaitForSecondsRealtime(PresentationTiming.GoalPassSeconds);
                }
            }

            if (_main != null && _main.IsWorldFinaleClear())
            {
                yield return new WaitForSecondsRealtime(
                    _reduceMotion ? 0.35f : PresentationTiming.WorldUnlockBeatSeconds);
            }

            _main?.NotifyCleared(_stageId, _clearElapsedMs);
            _sequencing = false;
        }

        private IEnumerator GameOverSequence(GameOverCause cause)
        {
            if (_sequencing)
            {
                yield break;
            }

            _sequencing = true;
            _main?.Input?.EnableGameplay(false);
            var pos = _session.State.PlayerPosition;
            Coroutine motion = null;
            switch (cause)
            {
                case GameOverCause.OverlappingShadows:
                    motion = playerPresenter.AnimateOverlapSink(pos, PresentationTiming.OverlapSinkSeconds);
                    break;
                case GameOverCause.CliffFall:
                    motion = playerPresenter.AnimateCliffFall(pos, PresentationTiming.CliffFallSeconds);
                    break;
                case GameOverCause.TimeExpired:
                    motion = playerPresenter.AnimateTimeVacuum(pos, PresentationTiming.TimeVacuumSeconds);
                    break;
            }

            if (motion != null)
            {
                yield return motion;
            }
            else
            {
                yield return new WaitForSecondsRealtime(0.35f);
            }

            _main?.NotifyGameOver(cause);
            _sequencing = false;
        }

        private void RefreshView()
        {
            if (_definition == null || _session == null)
            {
                return;
            }

            boardPresenter.Render(_definition, _session.Shadows, _session.State);
            playHud.Render(_definition, _session.State);
            onboarding.Tick(_definition, _session.State, playerPresenter.Visual);
        }

        private void UpdateLampApproachPulse()
        {
            if (_definition == null || _session == null)
            {
                return;
            }

            var pos = _session.State.PlayerPosition;
            if (pos == _lastPlayerPos)
            {
                return;
            }

            _lastPlayerPos = pos;
            if (_definition.TryGetLampAt(pos, out var lamp))
            {
                if (_nearLampPulseArmed)
                {
                    boardPresenter.PulseChannelPillars(_definition, lamp.Channel, 0.28f);
                    _nearLampPulseArmed = false;
                }
            }
            else
            {
                _nearLampPulseArmed = true;
            }
        }

        private void TeardownSession()
        {
            if (_session != null)
            {
                _session.CommandApplied -= OnCommandApplied;
                _session = null;
            }
        }

        private void EnsureComponents()
        {
            if (boardPresenter == null)
            {
                boardPresenter = GetComponent<BoardPresenter>() ?? gameObject.AddComponent<BoardPresenter>();
            }

            if (playerPresenter == null)
            {
                playerPresenter = GetComponent<PlayerPresenter>() ?? gameObject.AddComponent<PlayerPresenter>();
            }

            if (playHud == null)
            {
                playHud = GetComponent<MainPlayHud>() ?? gameObject.AddComponent<MainPlayHud>();
            }

            if (onboarding == null)
            {
                onboarding = GetComponent<OnboardingHintsPresenter>() ??
                             gameObject.AddComponent<OnboardingHintsPresenter>();
            }
        }

        private static void ApplyCameraLook()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.backgroundColor = MockupPalette.SoftSky;
            camera.clearFlags = CameraClearFlags.SolidColor;
        }
    }
}
