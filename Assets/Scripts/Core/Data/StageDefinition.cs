using System;
using System.Collections.Generic;

namespace ShadowGarden.Core
{
    public sealed class LampDefinition
    {
        public GridPosition Position { get; }
        public ChannelId Channel { get; }
        public CardinalDirection InitialDirection { get; }

        public LampDefinition(GridPosition position, ChannelId channel, CardinalDirection initialDirection)
        {
            Position = position;
            Channel = channel;
            InitialDirection = initialDirection;
        }
    }

    public sealed class PillarDefinition
    {
        public GridPosition Position { get; }
        public ChannelId Channel { get; }
        public PillarHeight Height { get; }

        public PillarDefinition(GridPosition position, ChannelId channel, PillarHeight height)
        {
            Position = position;
            Channel = channel;
            Height = height;
        }

        public int ShadowLength => (int)Height;
    }

    public sealed class StageDefinition
    {
        public string StageId { get; }
        public GridSize BoardSize { get; }
        public GridPosition PlayerStart { get; }
        public IReadOnlyList<GridPosition> SafeCells { get; }
        public IReadOnlyList<LampDefinition> Lamps { get; }
        public IReadOnlyList<PillarDefinition> Pillars { get; }
        public ClearGoalType ClearGoalType { get; }
        public GridPosition GoalPosition { get; }
        public int TimeLimitSeconds { get; }

        private readonly HashSet<GridPosition> _safeCellSet;
        private readonly HashSet<GridPosition> _pillarSet;
        private readonly Dictionary<GridPosition, LampDefinition> _lampByPosition;
        private readonly Dictionary<ChannelId, LampDefinition> _lampByChannel;

        public StageDefinition(
            string stageId,
            GridSize boardSize,
            GridPosition playerStart,
            IReadOnlyList<GridPosition> safeCells,
            IReadOnlyList<LampDefinition> lamps,
            IReadOnlyList<PillarDefinition> pillars,
            ClearGoalType clearGoalType,
            GridPosition goalPosition,
            int timeLimitSeconds)
        {
            StageId = stageId ?? throw new ArgumentNullException(nameof(stageId));
            BoardSize = boardSize;
            PlayerStart = playerStart;
            SafeCells = CopyPositions(safeCells);
            Lamps = CopyLamps(lamps);
            Pillars = CopyPillars(pillars);
            ClearGoalType = clearGoalType;
            GoalPosition = goalPosition;
            TimeLimitSeconds = timeLimitSeconds;

            _safeCellSet = new HashSet<GridPosition>(SafeCells);
            _pillarSet = new HashSet<GridPosition>();
            foreach (var pillar in Pillars)
            {
                _pillarSet.Add(pillar.Position);
            }

            _lampByPosition = new Dictionary<GridPosition, LampDefinition>();
            _lampByChannel = new Dictionary<ChannelId, LampDefinition>();
            foreach (var lamp in Lamps)
            {
                _lampByPosition[lamp.Position] = lamp;
                _lampByChannel[lamp.Channel] = lamp;
            }
        }

        public long TimeLimitMilliseconds => TimeLimitSeconds * 1000L;

        public bool IsInBounds(GridPosition position) => BoardSize.Contains(position);
        public bool IsPillar(GridPosition position) => _pillarSet.Contains(position);
        public bool IsSafeTerrain(GridPosition position) => _safeCellSet.Contains(position);
        public bool IsLamp(GridPosition position) => _lampByPosition.ContainsKey(position);
        public bool IsGoal(GridPosition position) => position == GoalPosition;
        public bool TryGetLampAt(GridPosition position, out LampDefinition lamp) =>
            _lampByPosition.TryGetValue(position, out lamp);
        public bool TryGetLamp(ChannelId channel, out LampDefinition lamp) =>
            _lampByChannel.TryGetValue(channel, out lamp);

        public bool IsAlwaysSafe(GridPosition position) =>
            IsSafeTerrain(position) || IsLamp(position) || IsGoal(position);

        public StageRuntimeState CreateInitialRuntimeState()
        {
            var directions = new Dictionary<ChannelId, CardinalDirection>();
            foreach (var lamp in Lamps)
            {
                directions[lamp.Channel] = lamp.InitialDirection;
            }

            return new StageRuntimeState(
                PlayerStart,
                directions,
                StagePhase.Ready,
                TimeLimitMilliseconds,
                warning30Fired: false,
                warning10Fired: false,
                TimerPauseReason.None);
        }

        private static IReadOnlyList<GridPosition> CopyPositions(IReadOnlyList<GridPosition> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<GridPosition>();
            }

            var copy = new GridPosition[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }

        private static IReadOnlyList<LampDefinition> CopyLamps(IReadOnlyList<LampDefinition> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<LampDefinition>();
            }

            var copy = new LampDefinition[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                var lamp = source[i] ?? throw new ArgumentException("Lamp entry cannot be null.", nameof(source));
                copy[i] = new LampDefinition(lamp.Position, lamp.Channel, lamp.InitialDirection);
            }

            return copy;
        }

        private static IReadOnlyList<PillarDefinition> CopyPillars(IReadOnlyList<PillarDefinition> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<PillarDefinition>();
            }

            var copy = new PillarDefinition[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                var pillar = source[i] ?? throw new ArgumentException("Pillar entry cannot be null.", nameof(source));
                copy[i] = new PillarDefinition(pillar.Position, pillar.Channel, pillar.Height);
            }

            return copy;
        }
    }
}
