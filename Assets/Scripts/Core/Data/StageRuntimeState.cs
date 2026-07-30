using System;
using System.Collections.Generic;

namespace ShadowGarden.Core
{
    public sealed class StageRuntimeState
    {
        private readonly Dictionary<ChannelId, CardinalDirection> _directionByChannel;

        public GridPosition PlayerPosition { get; }
        public StagePhase Phase { get; }
        public long RemainingMilliseconds { get; }
        public bool Warning30Fired { get; }
        public bool Warning10Fired { get; }
        public TimerPauseReason PauseReason { get; }
        public IReadOnlyDictionary<ChannelId, CardinalDirection> DirectionByChannel => _directionByChannel;

        public StageRuntimeState(
            GridPosition playerPosition,
            IReadOnlyDictionary<ChannelId, CardinalDirection> directionByChannel,
            StagePhase phase,
            long remainingMilliseconds,
            bool warning30Fired,
            bool warning10Fired,
            TimerPauseReason pauseReason)
        {
            PlayerPosition = playerPosition;
            _directionByChannel = CopyDirections(directionByChannel);
            Phase = phase;
            RemainingMilliseconds = remainingMilliseconds;
            Warning30Fired = warning30Fired;
            Warning10Fired = warning10Fired;
            PauseReason = pauseReason;
        }

        public CardinalDirection GetDirection(ChannelId channel) => _directionByChannel[channel];

        public StageRuntimeState WithPlayer(GridPosition position, StagePhase phase) =>
            new StageRuntimeState(
                position,
                _directionByChannel,
                phase,
                RemainingMilliseconds,
                Warning30Fired,
                Warning10Fired,
                PauseReason);

        public StageRuntimeState WithPhase(StagePhase phase) =>
            new StageRuntimeState(
                PlayerPosition,
                _directionByChannel,
                phase,
                RemainingMilliseconds,
                Warning30Fired,
                Warning10Fired,
                PauseReason);

        public StageRuntimeState WithDirection(ChannelId channel, CardinalDirection direction, StagePhase phase)
        {
            var next = new Dictionary<ChannelId, CardinalDirection>(_directionByChannel)
            {
                [channel] = direction
            };

            return new StageRuntimeState(
                PlayerPosition,
                next,
                phase,
                RemainingMilliseconds,
                Warning30Fired,
                Warning10Fired,
                PauseReason);
        }

        public StageRuntimeState WithTimer(
            long remainingMilliseconds,
            bool warning30Fired,
            bool warning10Fired,
            TimerPauseReason pauseReason) =>
            new StageRuntimeState(
                PlayerPosition,
                _directionByChannel,
                Phase,
                remainingMilliseconds,
                warning30Fired,
                warning10Fired,
                pauseReason);

        private static Dictionary<ChannelId, CardinalDirection> CopyDirections(
            IReadOnlyDictionary<ChannelId, CardinalDirection> source)
        {
            var copy = new Dictionary<ChannelId, CardinalDirection>();
            if (source == null)
            {
                return copy;
            }

            foreach (var pair in source)
            {
                copy[pair.Key] = pair.Value;
            }

            return copy;
        }
    }
}
