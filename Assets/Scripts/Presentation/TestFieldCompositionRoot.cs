using ShadowGarden.Core;
using ShadowGarden.Infrastructure;
using ShadowGarden.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Playable TestField composition root styled after the concept gameplay mockup video.
    /// </summary>
    public sealed class TestFieldCompositionRoot : MonoBehaviour
    {
        private const float MoveLockSeconds = 0.12f;
        private const float RotateLockSeconds = 0.18f;

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private StageDefinitionAsset[] stages;
        [SerializeField] private BoardPresenter boardPresenter;
        [SerializeField] private PlayerPresenter playerPresenter;
        [SerializeField] private PlayHudPresenter playHudPresenter;
        [SerializeField] private int initialStageIndex;

        private readonly UnityClock _clock = new UnityClock();
        private InputRouter _input;
        private ApplicationFocusBridge _focusBridge;
        private StageSession _session;
        private StageDefinition _definition;
        private int _stageIndex;
        private bool _started;
        private bool _modalOpen;
        private bool _showBoardDebug;

        private void Awake()
        {
            if (boardPresenter == null)
            {
                boardPresenter = gameObject.AddComponent<BoardPresenter>();
            }

            if (playerPresenter == null)
            {
                playerPresenter = gameObject.AddComponent<PlayerPresenter>();
            }

            if (playHudPresenter == null)
            {
                playHudPresenter = gameObject.AddComponent<PlayHudPresenter>();
            }

            _focusBridge = gameObject.AddComponent<ApplicationFocusBridge>();
            _focusBridge.FocusChanged += OnFocusChanged;

            ApplyCameraLook();
        }

        private void Start()
        {
            if (inputActions == null)
            {
                Debug.LogError("ShadowGardenActions 입력 에셋이 연결되어 있지 않습니다.");
                enabled = false;
                return;
            }

            _input = new InputRouter(inputActions);
            _input.MoveRequested += OnMove;
            _input.RotateRequested += OnRotate;
            _input.ResetRequested += OnReset;
            _input.EnableGameplay(true);

            if (stages == null || stages.Length == 0)
            {
                LoadGrayboxFallback();
            }
            else
            {
                _stageIndex = Mathf.Clamp(initialStageIndex, 0, stages.Length - 1);
                BeginStage(StageDefinitionFactory.CreateFromAsset(stages[_stageIndex]));
            }

            FrameCamera();
            _started = true;
        }

        private void Update()
        {
            if (!_started || _session == null)
            {
                return;
            }

            HandleMetaInput();

            if (_modalOpen)
            {
                HandleModalInput();
                RefreshView();
                return;
            }

            _input.Tick(Time.unscaledDeltaTime);
            _session.Tick(_clock.DeltaMilliseconds);
            RefreshView();
            UpdateLampHint();
        }

        private void OnDestroy()
        {
            if (_focusBridge != null)
            {
                _focusBridge.FocusChanged -= OnFocusChanged;
            }

            if (_input != null)
            {
                _input.MoveRequested -= OnMove;
                _input.RotateRequested -= OnRotate;
                _input.ResetRequested -= OnReset;
                _input.Dispose();
            }
        }

        private void BeginStage(StageDefinition definition)
        {
            if (_session != null)
            {
                _session.CommandApplied -= OnCommandApplied;
            }

            _definition = definition;
            _session = new StageSession(definition);
            _session.CommandApplied += OnCommandApplied;
            _modalOpen = false;
            playHudPresenter.HideModal();
            playHudPresenter.Bind(definition);
            boardPresenter.Build(definition);
            playerPresenter.EnsureVisual();
            _input?.EnableGameplay(true);
            _input?.CancelLockAndBuffer();
            RefreshView();
            playHudPresenter.SetHint(BuildStageHint(definition));
        }

        private void LoadGrayboxFallback()
        {
            var grayboxes = AllGrayboxes();
            _stageIndex = Mathf.Clamp(initialStageIndex, 0, grayboxes.Length - 1);
            BeginStage(grayboxes[_stageIndex]);
        }

        private void CycleStage(int delta)
        {
            if (stages != null && stages.Length > 0)
            {
                _stageIndex = (_stageIndex + delta + stages.Length) % stages.Length;
                BeginStage(StageDefinitionFactory.CreateFromAsset(stages[_stageIndex]));
                return;
            }

            var grayboxes = AllGrayboxes();
            _stageIndex = (_stageIndex + delta + grayboxes.Length) % grayboxes.Length;
            BeginStage(grayboxes[_stageIndex]);
        }

        private static StageDefinition[] AllGrayboxes() => new[]
        {
            GrayboxStages.CreateTF_1(),
            GrayboxStages.Create1_1(),
            GrayboxStages.Create1_2(),
            GrayboxStages.Create1_4(),
            GrayboxStages.Create3_4()
        };

        private void OnMove(CardinalDirection direction)
        {
            if (_modalOpen || _session == null)
            {
                return;
            }

            var result = _session.Move(direction);
            if (result.Move.HasValue && result.Move.Value.Outcome != MoveOutcome.Blocked)
            {
                _input.LockForSeconds(MoveLockSeconds);
            }
        }

        private void OnRotate(int turns)
        {
            if (_modalOpen || _session == null)
            {
                return;
            }

            var result = _session.Rotate(turns);
            if (result.Events.Length > 0)
            {
                _input.LockForSeconds(RotateLockSeconds);
                playHudPresenter.SetHint("그림자가 바뀌었습니다. 남색 길만 건너세요.");
            }
        }

        private void OnReset()
        {
            if (_session == null)
            {
                return;
            }

            if (_modalOpen && playHudPresenter.Modal == PlayModalKind.GameOver)
            {
                ConfirmModal();
                return;
            }

            RestartCurrent();
        }

        private void RestartCurrent()
        {
            _input.CancelLockAndBuffer();
            _modalOpen = false;
            playHudPresenter.HideModal();
            _session.Restart();
            _input.EnableGameplay(true);
            playHudPresenter.SetHint("보드를 초기화했습니다.");
            RefreshView();
        }

        private void OnFocusChanged(bool hasFocus)
        {
            if (_modalOpen)
            {
                return;
            }

            _session?.SetFocus(hasFocus);
        }

        private void OnCommandApplied(StageCommandResult result)
        {
            foreach (var stageEvent in result.Events)
            {
                switch (stageEvent.Type)
                {
                    case StageEventType.GameOverStarted:
                        OpenGameOver(stageEvent.GameOverCause ?? GameOverCause.CliffFall);
                        break;
                    case StageEventType.Cleared:
                        OpenCleared();
                        break;
                    case StageEventType.TimerWarning30:
                        playHudPresenter.SetHint("남은 시간 30초");
                        break;
                    case StageEventType.TimerWarning10:
                        playHudPresenter.SetHint("남은 시간 10초!");
                        break;
                    case StageEventType.MoveBlocked:
                        playHudPresenter.SetHint("막힌 칸입니다.");
                        break;
                }
            }
        }

        private void OpenGameOver(GameOverCause cause)
        {
            _modalOpen = true;
            _input.EnableGameplay(false);
            playHudPresenter.ShowGameOver(cause);
        }

        private void OpenCleared()
        {
            _modalOpen = true;
            _input.EnableGameplay(false);
            playHudPresenter.ShowCleared(_session.State.RemainingMilliseconds, _definition.TimeLimitSeconds);
        }

        private void HandleMetaInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (!_modalOpen)
            {
                if (keyboard.leftBracketKey.wasPressedThisFrame)
                {
                    CycleStage(-1);
                }
                else if (keyboard.rightBracketKey.wasPressedThisFrame)
                {
                    CycleStage(1);
                }
            }

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                _showBoardDebug = !_showBoardDebug;
                playHudPresenter.ToggleDebug();
                boardPresenter.SetShowDebugCounts(_showBoardDebug);
            }
        }

        private void HandleModalInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            {
                playHudPresenter.MoveSelection(-1);
            }
            else if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                playHudPresenter.MoveSelection(1);
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            {
                ConfirmModal();
            }

            if (keyboard.rKey.wasPressedThisFrame && playHudPresenter.Modal == PlayModalKind.GameOver)
            {
                RestartCurrent();
            }
        }

        private void ConfirmModal()
        {
            var selection = playHudPresenter.ModalSelection;
            var kind = playHudPresenter.Modal;

            if (kind == PlayModalKind.GameOver)
            {
                if (selection == 0)
                {
                    RestartCurrent();
                }
                else
                {
                    CycleStage(1);
                }

                return;
            }

            if (kind == PlayModalKind.Cleared)
            {
                if (selection == 0)
                {
                    CycleStage(1);
                }
                else
                {
                    RestartCurrent();
                }
            }
        }

        private void UpdateLampHint()
        {
            if (_definition.TryGetLampAt(_session.State.PlayerPosition, out var lamp))
            {
                var direction = _session.State.GetDirection(lamp.Channel);
                playHudPresenter.SetHint(
                    $"Q/E  {MockupPalette.ChannelLabel(lamp.Channel)} 채널 90° 회전  {MockupPalette.DirectionArrow(direction)}");
            }
        }

        private void RefreshView()
        {
            boardPresenter.Render(_definition, _session.Shadows, _session.State);
            playerPresenter.Render(_session.State.PlayerPosition);
            playHudPresenter.Render(_definition, _session.State);
        }

        private static string BuildStageHint(StageDefinition stage)
        {
            return stage.StageId switch
            {
                "TF-1" => "×2는 심연입니다. 삼각형 태양등으로 중첩을 푼 뒤, 남색 단일 길로 출구에 가세요.",
                "1-1" => "태양등 위에서 Q/E로 방향을 바꾼 뒤, 남색 그림자 길로 출구에 가세요.",
                "1-2" => "낮은 기둥은 2칸, 높은 기둥은 4칸 그림자를 만듭니다.",
                "1-4" => "×2 자주색 칸은 위험합니다. 안전 지형으로 우회해 밤꽃에 도달하세요.",
                "3-4" => "네 채널을 조합해 밤꽃까지 이어지는 길을 만드세요.",
                _ => "남색 길은 안전, ×2와 빈 절벽은 위험합니다."
            };
        }

        private void FrameCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = 4.6f;
            var center = GridWorld.BoardCenter(GridSize.Board12x6);
            camera.transform.position = new Vector3(center.x, center.y - 0.15f, -10f);
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
