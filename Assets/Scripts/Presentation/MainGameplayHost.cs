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
        [SerializeField] private PresentationAudioController presentationAudio;
        [SerializeField] private GameplayFxPresenter gameplayFx;
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
        public void PlayUiMove() => presentationAudio?.PlayUiMove();
        public void PlayUiSubmit() => presentationAudio?.PlayUiSubmit();

        private bool _externalPause;
        private bool _reduceMotion;

        public void ApplyReduceMotion(bool enabled)
        {
            _reduceMotion = enabled;
            boardPresenter?.SetReduceMotion(enabled);
            gameplayFx?.Bind(enabled);
            playHud?.ApplyPreferences();
        }

        public void ApplyAudioPreferences() => presentationAudio?.ApplyPreferences();

        public void UnlockAudioFromUserGesture() => presentationAudio?.UnlockFromUserInteraction();

        public void PlayMenuMusic() => presentationAudio?.PlayMenuMusic();

        public void Bind(MainCompositionRoot main)
        {
            UnsubscribeInput();
            _main = main;
            EnsureComponents();
            playHud?.Bind(main);
            presentationAudio?.Bind(main);
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
            presentationAudio.BeginStage(definition.StageId);
            gameplayFx.Bind(_reduceMotion);
            onboarding.ResetProgress();
            playHud.EnsureBuilt(hudParent != null ? hudParent : transform);
            playHud.Bind(_main);
            playHud.SetVisible(true);
            playHud.Render(definition, _session.State);
            ApplyReduceMotion(_main?.Save?.Preferences != null && _main.Save.Preferences.reduceMotion);
            var boardPadding = definition.BoardSize.Width >= 16 || definition.BoardSize.Height >= 8
                ? 2f
                : definition.BoardSize.Width > 12 || definition.BoardSize.Height > 6
                    ? 1.55f
                    : BoardCameraFitter.DefaultPaddingCells;
            var topVisualOverflow = definition.BoardSize.Width == 12 && definition.BoardSize.Height == 6
                ? 1.5f
                : 0f;
            BoardCameraFitter.Apply(Camera.main, definition.BoardSize, boardPadding, topVisualOverflow);
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
            presentationAudio?.PlayMenuMusic();
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
                presentationAudio?.Play(presentationAudio.Clips?.blocked, 0.7f);
                return;
            }

            onboarding.NotifyMoved();
            presentationAudio?.Play(presentationAudio.Clips?.move, 0.72f);
            _main.Input?.LockForSeconds(PresentationTiming.MoveSeconds);
            var to = result.Move.Value.TargetPosition;
            if (result.Move.Value.Outcome == MoveOutcome.Moved ||
                result.Move.Value.Outcome == MoveOutcome.ExitReached ||
                result.Move.Value.Outcome == MoveOutcome.NightFlowerReached)
            {
                playerPresenter.AnimateMove(from, to, PresentationTiming.MoveSeconds);
            }
            else if (result.Move.Value.Outcome == MoveOutcome.CliffDeath ||
                     result.Move.Value.Outcome == MoveOutcome.OverlapDeath)
            {
                var cause = result.Move.Value.DeathCause ??
                            (result.Move.Value.Outcome == MoveOutcome.OverlapDeath
                                ? GameOverCause.OverlappingShadows
                                : GameOverCause.CliffFall);
                StartCoroutine(GameOverSequence(cause, from, to, direction, movementDeath: true));
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

            var previousCounts = _session.Shadows?.ShadowCountByCell;
            var result = _session.Rotate(turns);
            if (result.Events.Length == 0)
            {
                return;
            }

            onboarding.NotifyRotated();
            presentationAudio?.Play(presentationAudio.Clips?.rotate, 0.82f);
            gameplayFx?.PlayRotate(_session.State.PlayerPosition, MockupPalette.ChannelColor(lamp.Channel),
                PresentationTiming.RotateSeconds);
            _main.Input?.LockForSeconds(PresentationTiming.RotateSeconds);
            boardPresenter.PulseChannelPillars(_definition, lamp.Channel, PresentationTiming.RotateSeconds);
            boardPresenter.PlayEnvironmentReaction(PresentationTiming.RotateSeconds + 0.30f);
            presentationAudio?.PlayShadowCellChimes(CountChangedShadowCells(previousCounts));
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
                        // Movement deaths are dispatched by OnMove after StageSession.Move returns.
                        // Starting them from this synchronous callback races with the normal move
                        // presentation and used to leave the host permanently sequencing.
                        if (result.Move.HasValue)
                        {
                            StartCoroutine(DeferredMovementGameOver(
                                stageEvent.GameOverCause ?? GameOverCause.CliffFall,
                                result.Move.Value.TargetPosition));
                        }
                        else
                        {
                            var position = result.NextState.PlayerPosition;
                            StartCoroutine(GameOverSequence(
                                stageEvent.GameOverCause ?? GameOverCause.TimeExpired,
                                position,
                                position,
                                CardinalDirection.South,
                                movementDeath: false));
                        }
                        break;
                    case StageEventType.TimerWarning30:
                        playHud?.ShowTransientWarning("남은 시간 30초", UiTheme.Brass);
                        presentationAudio?.Play(presentationAudio.Clips?.warning30);
                        break;
                    case StageEventType.TimerWarning10:
                        playHud?.ShowTransientWarning("남은 시간 10초!", UiTheme.Coral);
                        presentationAudio?.Play(presentationAudio.Clips?.warning10);
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

        private IEnumerator DeferredMovementGameOver(GameOverCause cause, GridPosition target)
        {
            // Let the real Gameplay input path resume first. OnMove starts the authoritative
            // sequence in the same frame with its exact origin and direction. Direct session
            // calls used by tests/tools have no OnMove continuation, so they take this fallback.
            yield return null;
            if (_sequencing || _main == null || _main.CurrentState != AppState.Playing) yield break;

            var from = _lastPlayerPos;
            var dx = target.X - from.X;
            var dy = target.Y - from.Y;
            var direction = Mathf.Abs(dx) >= Mathf.Abs(dy)
                ? (dx >= 0 ? CardinalDirection.East : CardinalDirection.West)
                : (dy >= 0 ? CardinalDirection.South : CardinalDirection.North);
            StartCoroutine(GameOverSequence(cause, from, target, direction, movementDeath: true));
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
                presentationAudio?.Play(presentationAudio.Clips?.flowerBloom);
                gameplayFx?.PlayFlowerBloom(_definition.GoalPosition, bloom);
                var flower = boardPresenter.PlayFlowerBloom(_definition, bloom);
                if (flower != null) yield return flower;
                else yield return new WaitForSecondsRealtime(bloom);
            }
            else
            {
                presentationAudio?.Play(presentationAudio.Clips?.doorOpen);
                gameplayFx?.PlayDoorGlow(_definition.GoalPosition, PresentationTiming.DoorOpenSeconds);
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
                presentationAudio?.Play(presentationAudio.Clips?.doorPass);
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

            gameplayFx?.PlayComplete(_definition.GoalPosition, 0.7f);
            _main?.NotifyCleared(_stageId, _clearElapsedMs);
            presentationAudio?.Play(presentationAudio.Clips?.complete);
            _sequencing = false;
        }

        private int CountChangedShadowCells(System.Collections.Generic.IReadOnlyList<int> previousCounts)
        {
            var current = _session?.Shadows?.ShadowCountByCell;
            if (previousCounts == null || current == null) return 1;
            var changed = 0;
            var count = Mathf.Min(previousCounts.Count, current.Count);
            for (var index = 0; index < count; index++)
            {
                if (previousCounts[index] != current[index]) changed++;
            }
            return Mathf.Clamp(changed, 1, 4);
        }

        private IEnumerator GameOverSequence(
            GameOverCause cause,
            GridPosition from,
            GridPosition target,
            CardinalDirection direction,
            bool movementDeath)
        {
            if (_sequencing)
            {
                yield break;
            }

            _sequencing = true;
            _main?.Input?.EnableGameplay(false);
            var effectPosition = movementDeath ? target : _session.State.PlayerPosition;
            var sequenceSeconds = cause switch
            {
                GameOverCause.OverlappingShadows => PresentationTiming.MoveSeconds + PresentationTiming.OverlapSinkSeconds,
                GameOverCause.CliffFall => PresentationTiming.MoveSeconds + PresentationTiming.CliffFallSeconds,
                GameOverCause.TimeExpired => PresentationTiming.TimeVacuumSeconds,
                _ => PresentationTiming.CliffFallSeconds
            };
            gameplayFx?.PlayDeath(cause, effectPosition, sequenceSeconds);
            switch (cause)
            {
                case GameOverCause.OverlappingShadows:
                    presentationAudio?.Play(presentationAudio.Clips?.overlapDeath);
                    playerPresenter.AnimateOverlapSink(
                        from,
                        target,
                        direction,
                        PresentationTiming.MoveSeconds,
                        PresentationTiming.OverlapSinkSeconds);
                    break;
                case GameOverCause.CliffFall:
                    presentationAudio?.Play(presentationAudio.Clips?.cliffDeath);
                    playerPresenter.AnimateCliffFall(from, direction, PresentationTiming.CliffFallSeconds);
                    break;
                case GameOverCause.TimeExpired:
                    presentationAudio?.Play(presentationAudio.Clips?.timeDeath);
                    playerPresenter.AnimateTimeVacuum(effectPosition, PresentationTiming.TimeVacuumSeconds);
                    break;
            }

            // The host owns completion. A missing asset or a presentation coroutine being
            // interrupted must never prevent the state machine from reaching GameOver.
            var elapsed = 0f;
            while (elapsed < sequenceSeconds && _main != null && _main.CurrentState == AppState.Playing)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_main != null && _main.CurrentState == AppState.Playing)
            {
                _main.NotifyGameOver(cause);
            }
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

            if (presentationAudio == null)
            {
                presentationAudio = GetComponent<PresentationAudioController>() ??
                                    gameObject.AddComponent<PresentationAudioController>();
            }

            if (gameplayFx == null)
            {
                gameplayFx = GetComponent<GameplayFxPresenter>() ?? gameObject.AddComponent<GameplayFxPresenter>();
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
