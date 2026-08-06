using System;
using ShadowGarden.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShadowGarden.Runtime
{
    public enum InputMapMode
    {
        None = 0,
        Gameplay = 1,
        Ui = 2
    }

    public sealed class InputRouter : IDisposable
    {
        private readonly InputActionAsset _actions;
        private readonly InputActionMap _gameplay;
        private readonly InputActionMap _ui;
        private readonly InputAction _move;
        private readonly InputAction _rotateLeft;
        private readonly InputAction _rotateRight;
        private readonly InputAction _reset;
        private readonly InputAction _navigate;
        private readonly InputAction _submit;
        private readonly InputAction _point;
        private readonly InputAction _click;
        private readonly InputAction _pause;

        public event Action<CardinalDirection> MoveRequested;
        public event Action<int> RotateRequested;
        public event Action ResetRequested;
        public event Action<Vector2> NavigateRequested;
        public event Action SubmitRequested;
        public event Action ClickRequested;
        public event Action PauseRequested;

        private bool _gameplayEnabled = true;
        private bool _inputLocked;
        private bool _pauseAvailableInUi;
        private float _lockRemaining;
        private CardinalDirection? _bufferedMove;
        private int? _bufferedRotate;
        private InputMapMode _mode = InputMapMode.None;

        public InputMapMode ActiveMode => _mode;
        public bool IsInputLocked => _inputLocked || _lockRemaining > 0f;

        public InputRouter(InputActionAsset actions)
        {
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            _gameplay = _actions.FindActionMap("Gameplay", true);
            _ui = _actions.FindActionMap("UI", true);
            _move = _gameplay.FindAction("Move", true);
            _rotateLeft = _gameplay.FindAction("RotateLeft", true);
            _rotateRight = _gameplay.FindAction("RotateRight", true);
            _reset = _gameplay.FindAction("Reset", true);
            _navigate = _ui.FindAction("Navigate", true);
            _submit = _ui.FindAction("Submit", true);
            _point = _ui.FindAction("Point", true);
            _click = _ui.FindAction("Click", true);
            // Standalone so Esc still works while gameplay map is soft-disabled during pause.
            _pause = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");

            _move.performed += OnMove;
            _rotateLeft.performed += OnRotateLeft;
            _rotateRight.performed += OnRotateRight;
            _reset.performed += OnReset;
            _navigate.performed += OnNavigate;
            _submit.performed += OnSubmit;
            _click.performed += OnClick;
            _pause.performed += OnPause;
        }

        /// <summary>
        /// Exactly one map may be active. Gameplay and UI never enable together.
        /// Pause (Esc) stays available whenever gameplay mode is active, even if move input is soft-disabled.
        /// </summary>
        public void SetMapMode(InputMapMode mode)
        {
            _mode = mode;
            switch (mode)
            {
                case InputMapMode.Gameplay:
                    _ui.Disable();
                    if (_gameplayEnabled && !_inputLocked)
                    {
                        _gameplay.Enable();
                    }
                    else
                    {
                        _gameplay.Disable();
                    }

                    break;
                case InputMapMode.Ui:
                    _gameplay.Disable();
                    ClearBuffer();
                    if (!_inputLocked)
                    {
                        _ui.Enable();
                    }
                    else
                    {
                        _ui.Disable();
                    }

                    break;
                default:
                    _gameplay.Disable();
                    _ui.Disable();
                    ClearBuffer();
                    break;
            }

            RefreshPauseAction();
        }

        public void ApplyForAppState(AppState state)
        {
            if (AppStateMachine.IsGameplayMapState(state))
            {
                SetMapMode(InputMapMode.Gameplay);
            }
            else if (AppStateMachine.IsUiMapState(state))
            {
                SetMapMode(InputMapMode.Ui);
            }
            else
            {
                SetMapMode(InputMapMode.None);
            }
        }

        public void SetTransitionInputLock(bool locked)
        {
            _inputLocked = locked;
            if (locked)
            {
                _gameplay.Disable();
                _ui.Disable();
                _pause.Disable();
                ClearBuffer();
            }
            else
            {
                SetMapMode(_mode);
            }
        }

        public void EnableGameplay(bool enabled)
        {
            _gameplayEnabled = enabled;
            if (_mode == InputMapMode.Gameplay)
            {
                SetMapMode(InputMapMode.Gameplay);
            }

            if (!enabled)
            {
                ClearBuffer();
            }
        }

        /// <summary>
        /// When true, Esc pause remains available while the UI map is active (pause/settings overlays).
        /// </summary>
        public void SetPauseAvailableInUi(bool available)
        {
            _pauseAvailableInUi = available;
            RefreshPauseAction();
        }

        public void LockForSeconds(float seconds)
        {
            _lockRemaining = Mathf.Max(_lockRemaining, seconds);
        }

        public void CancelLockAndBuffer()
        {
            _lockRemaining = 0f;
            ClearBuffer();
        }

        public void Tick(float deltaTime)
        {
            if (_lockRemaining <= 0f)
            {
                FlushBuffer();
                return;
            }

            _lockRemaining -= deltaTime;
            if (_lockRemaining <= 0f)
            {
                _lockRemaining = 0f;
                FlushBuffer();
            }
        }

        public void Dispose()
        {
            _move.performed -= OnMove;
            _rotateLeft.performed -= OnRotateLeft;
            _rotateRight.performed -= OnRotateRight;
            _reset.performed -= OnReset;
            _navigate.performed -= OnNavigate;
            _submit.performed -= OnSubmit;
            _click.performed -= OnClick;
            _pause.performed -= OnPause;
            _pause.Disable();
            _pause.Dispose();
        }

        private void RefreshPauseAction()
        {
            var allow =
                !_inputLocked &&
                (_mode == InputMapMode.Gameplay ||
                 (_mode == InputMapMode.Ui && _pauseAvailableInUi));
            if (allow)
            {
                _pause.Enable();
            }
            else
            {
                _pause.Disable();
            }
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            if (!_gameplayEnabled || _inputLocked || _mode != InputMapMode.Gameplay)
            {
                return;
            }

            var value = context.ReadValue<Vector2>();
            CardinalDirection? direction = null;
            if (value.y > 0.5f)
            {
                direction = CardinalDirection.North;
            }
            else if (value.y < -0.5f)
            {
                direction = CardinalDirection.South;
            }
            else if (value.x > 0.5f)
            {
                direction = CardinalDirection.East;
            }
            else if (value.x < -0.5f)
            {
                direction = CardinalDirection.West;
            }

            if (!direction.HasValue)
            {
                return;
            }

            if (_lockRemaining > 0f)
            {
                _bufferedMove = direction;
                _bufferedRotate = null;
                return;
            }

            MoveRequested?.Invoke(direction.Value);
        }

        private void OnRotateLeft(InputAction.CallbackContext context) => QueueRotate(-1);

        private void OnRotateRight(InputAction.CallbackContext context) => QueueRotate(1);

        private void QueueRotate(int turns)
        {
            if (!_gameplayEnabled || _inputLocked || _mode != InputMapMode.Gameplay)
            {
                return;
            }

            if (_lockRemaining > 0f)
            {
                _bufferedRotate = turns;
                _bufferedMove = null;
                return;
            }

            RotateRequested?.Invoke(turns);
        }

        private void OnReset(InputAction.CallbackContext context)
        {
            if (_inputLocked || _mode != InputMapMode.Gameplay)
            {
                return;
            }

            CancelLockAndBuffer();
            ResetRequested?.Invoke();
        }

        private void OnNavigate(InputAction.CallbackContext context)
        {
            if (_inputLocked || _mode != InputMapMode.Ui)
            {
                return;
            }

            NavigateRequested?.Invoke(context.ReadValue<Vector2>());
        }

        private void OnSubmit(InputAction.CallbackContext context)
        {
            if (_inputLocked || _mode != InputMapMode.Ui)
            {
                return;
            }

            SubmitRequested?.Invoke();
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            if (_inputLocked || _mode != InputMapMode.Ui)
            {
                return;
            }

            ClickRequested?.Invoke();
        }

        private void OnPause(InputAction.CallbackContext context)
        {
            if (_inputLocked)
            {
                return;
            }

            if (_mode != InputMapMode.Gameplay &&
                !(_mode == InputMapMode.Ui && _pauseAvailableInUi))
            {
                return;
            }

            PauseRequested?.Invoke();
        }

        private void FlushBuffer()
        {
            if (_inputLocked || _mode != InputMapMode.Gameplay)
            {
                ClearBuffer();
                return;
            }

            if (_bufferedRotate.HasValue)
            {
                var turns = _bufferedRotate.Value;
                ClearBuffer();
                RotateRequested?.Invoke(turns);
                return;
            }

            if (_bufferedMove.HasValue)
            {
                var direction = _bufferedMove.Value;
                ClearBuffer();
                MoveRequested?.Invoke(direction);
            }
        }

        private void ClearBuffer()
        {
            _bufferedMove = null;
            _bufferedRotate = null;
        }
    }
}
