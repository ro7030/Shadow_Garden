namespace ShadowGarden.Core
{
    public readonly struct MoveResolution
    {
        public MoveOutcome Outcome { get; }
        public GridPosition TargetPosition { get; }
        public GameOverCause? DeathCause { get; }

        public MoveResolution(MoveOutcome outcome, GridPosition targetPosition, GameOverCause? deathCause = null)
        {
            Outcome = outcome;
            TargetPosition = targetPosition;
            DeathCause = deathCause;
        }

        public static MoveResolution Moved(GridPosition target) =>
            new MoveResolution(MoveOutcome.Moved, target);

        public static MoveResolution Blocked(GridPosition current) =>
            new MoveResolution(MoveOutcome.Blocked, current);

        public static MoveResolution OverlapDeath(GridPosition target) =>
            new MoveResolution(MoveOutcome.OverlapDeath, target, GameOverCause.OverlappingShadows);

        public static MoveResolution CliffDeath(GridPosition target) =>
            new MoveResolution(MoveOutcome.CliffDeath, target, GameOverCause.CliffFall);

        public static MoveResolution ExitReached(GridPosition target) =>
            new MoveResolution(MoveOutcome.ExitReached, target);

        public static MoveResolution NightFlowerReached(GridPosition target) =>
            new MoveResolution(MoveOutcome.NightFlowerReached, target);
    }

    public enum StageEventType
    {
        StageStarted,
        PlayerMoved,
        MoveBlocked,
        LampRotated,
        ShadowGridChanged,
        ActionReady,
        TimerChanged,
        TimerWarning30,
        TimerWarning10,
        TimeExpired,
        GameOverStarted,
        ClearStarted,
        Cleared,
        StageRestarted
    }

    public readonly struct StageEvent
    {
        public StageEventType Type { get; }
        public GameOverCause? GameOverCause { get; }
        public ChannelId? Channel { get; }
        public CardinalDirection? Direction { get; }
        public GridPosition? Position { get; }
        public long? RemainingMilliseconds { get; }
        public ClearGoalType? ClearGoalType { get; }

        public StageEvent(
            StageEventType type,
            GameOverCause? gameOverCause = null,
            ChannelId? channel = null,
            CardinalDirection? direction = null,
            GridPosition? position = null,
            long? remainingMilliseconds = null,
            ClearGoalType? clearGoalType = null)
        {
            Type = type;
            GameOverCause = gameOverCause;
            Channel = channel;
            Direction = direction;
            Position = position;
            RemainingMilliseconds = remainingMilliseconds;
            ClearGoalType = clearGoalType;
        }
    }

    public sealed class StageCommandResult
    {
        public StageRuntimeState NextState { get; }
        public ShadowGridResult Shadows { get; }
        public MoveResolution? Move { get; }
        public StageEvent[] Events { get; }

        public StageCommandResult(
            StageRuntimeState nextState,
            ShadowGridResult shadows,
            MoveResolution? move,
            StageEvent[] events)
        {
            NextState = nextState;
            Shadows = shadows;
            Move = move;
            Events = events ?? System.Array.Empty<StageEvent>();
        }
    }
}
