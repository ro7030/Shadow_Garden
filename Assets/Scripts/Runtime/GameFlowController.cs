using System;

namespace ShadowGarden.Runtime
{
    public readonly struct AppStateChangeResult
    {
        public bool Accepted { get; }
        public AppState From { get; }
        public AppState To { get; }
        public string RejectionReason { get; }

        public AppStateChangeResult(bool accepted, AppState from, AppState to, string rejectionReason = null)
        {
            Accepted = accepted;
            From = from;
            To = to;
            RejectionReason = rejectionReason;
        }

        public static AppStateChangeResult Ok(AppState from, AppState to) =>
            new AppStateChangeResult(true, from, to);

        public static AppStateChangeResult Reject(AppState from, AppState to, string reason) =>
            new AppStateChangeResult(false, from, to, reason);
    }

    /// <summary>
    /// Single-responsibility owner of AppState transitions, input lock, and map mode.
    /// Presentation binds screens; this class does not touch UnityEngine.
    /// </summary>
    public sealed class GameFlowController
    {
        public AppState Current { get; private set; }
        public bool IsTransitionLocked { get; private set; }
        public event Action<AppState, AppState> StateChanged;

        public GameFlowController(AppState initial = AppState.Title)
        {
            Current = initial;
        }

        public AppStateChangeResult TryTransition(AppState to)
        {
            if (IsTransitionLocked)
            {
                return AppStateChangeResult.Reject(Current, to, "transition_locked");
            }

            if (!AppStateMachine.CanTransition(Current, to))
            {
                return AppStateChangeResult.Reject(Current, to, "transition_not_allowed");
            }

            var from = Current;
            IsTransitionLocked = true;
            Current = to;
            StateChanged?.Invoke(from, to);
            IsTransitionLocked = false;
            return AppStateChangeResult.Ok(from, to);
        }

        /// <summary>
        /// Force initial state without transition rules (boot / domain reload restore).
        /// </summary>
        public void Boot(AppState state)
        {
            var from = Current;
            Current = state;
            if (from != state)
            {
                StateChanged?.Invoke(from, state);
            }
        }

        public void SetTransitionLock(bool locked) => IsTransitionLocked = locked;
    }
}
