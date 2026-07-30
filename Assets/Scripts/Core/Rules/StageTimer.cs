using System.Collections.Generic;

namespace ShadowGarden.Core
{
    public readonly struct TimerTickResult
    {
        public StageRuntimeState NextState { get; }
        public bool FiredWarning30 { get; }
        public bool FiredWarning10 { get; }
        public bool Expired { get; }

        public TimerTickResult(
            StageRuntimeState nextState,
            bool firedWarning30,
            bool firedWarning10,
            bool expired)
        {
            NextState = nextState;
            FiredWarning30 = firedWarning30;
            FiredWarning10 = firedWarning10;
            Expired = expired;
        }
    }

    public static class StageTimer
    {
        public const long Warning30Milliseconds = 30_000L;
        public const long Warning10Milliseconds = 10_000L;

        public static bool CanTick(StageRuntimeState state)
        {
            if (state.PauseReason != TimerPauseReason.None)
            {
                return false;
            }

            return state.Phase == StagePhase.Playing || state.Phase == StagePhase.ResolvingAction;
        }

        public static StageRuntimeState Pause(StageRuntimeState state, TimerPauseReason reason) =>
            state.WithTimer(state.RemainingMilliseconds, state.Warning30Fired, state.Warning10Fired, reason);

        public static StageRuntimeState Resume(StageRuntimeState state) =>
            state.WithTimer(
                state.RemainingMilliseconds,
                state.Warning30Fired,
                state.Warning10Fired,
                TimerPauseReason.None);

        public static StageRuntimeState Stop(StageRuntimeState state) =>
            state.WithTimer(
                state.RemainingMilliseconds,
                state.Warning30Fired,
                state.Warning10Fired,
                TimerPauseReason.Locked);

        public static TimerTickResult Tick(StageRuntimeState state, long deltaMilliseconds)
        {
            if (!CanTick(state) || deltaMilliseconds <= 0)
            {
                return new TimerTickResult(state, false, false, false);
            }

            var remaining = state.RemainingMilliseconds - deltaMilliseconds;
            if (remaining < 0)
            {
                remaining = 0;
            }

            var warning30 = state.Warning30Fired;
            var warning10 = state.Warning10Fired;
            var fired30 = false;
            var fired10 = false;

            if (!warning30 && remaining <= Warning30Milliseconds)
            {
                warning30 = true;
                fired30 = true;
            }

            if (!warning10 && remaining <= Warning10Milliseconds)
            {
                warning10 = true;
                fired10 = true;
            }

            var next = state.WithTimer(remaining, warning30, warning10, state.PauseReason);
            var expired = remaining <= 0;
            if (expired)
            {
                next = next.WithPhase(StagePhase.ResolvingDeath);
            }

            return new TimerTickResult(next, fired30, fired10, expired);
        }
    }
}
