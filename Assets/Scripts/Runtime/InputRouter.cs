using System;
using ShadowGarden.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShadowGarden.Runtime
{
    public sealed class InputRouter : IDisposable
    {
        private readonly InputActionAsset _actions;
        private readonly InputActionMap _gameplay;
        private readonly InputAction _move;
        private readonly InputAction _rotateLeft;
        private readonly InputAction _rotateRight;
        private readonly InputAction _reset;

        public event Action<CardinalDirection> MoveRequested;
        public event Action<int> RotateRequested;
        public event Action ResetRequested;

        private bool _gameplayEnabled = true;
        private float _lockRemaining;
        private CardinalDirection? _bufferedMove;
        private int? _bufferedRotate;

        public InputRouter(InputActionAsset actions)
        {
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            _gameplay = _actions.FindActionMap("Gameplay", true);
            _move = _gameplay.FindAction("Move", true);
            _rotateLeft = _gameplay.FindAction("RotateLeft", true);
            _rotateRight = _gameplay.FindAction("RotateRight", true);
            _reset = _gameplay.FindAction("Reset", true);

            _move.performed += OnMove;
            _rotateLeft.performed += OnRotateLeft;
            _rotateRight.performed += OnRotateRight;
            _reset.performed += OnReset;
        }

        public void EnableGameplay(bool enabled)
        {
            _gameplayEnabled = enabled;
            if (enabled)
            {
                _gameplay.Enable();
            }
            else
            {
                _gameplay.Disable();
                ClearBuffer();
            }
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
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            if (!_gameplayEnabled)
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

        private void OnRotateLeft(InputAction.CallbackContext context)
        {
            QueueRotate(-1);
        }

        private void OnRotateRight(InputAction.CallbackContext context)
        {
            QueueRotate(1);
        }

        private void QueueRotate(int turns)
        {
            if (!_gameplayEnabled)
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
            CancelLockAndBuffer();
            ResetRequested?.Invoke();
        }

        private void FlushBuffer()
        {
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
