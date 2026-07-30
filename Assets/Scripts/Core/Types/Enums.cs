namespace ShadowGarden.Core
{
    public enum CardinalDirection
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public enum PillarHeight
    {
        Low = 2,
        Medium = 3,
        High = 4
    }

    public enum ChannelId
    {
        Circle = 0,
        Triangle = 1,
        Star = 2,
        Diamond = 3
    }

    public enum ClearGoalType
    {
        ExitDoor = 0,
        NightFlower = 1
    }

    public enum GameOverCause
    {
        OverlappingShadows = 0,
        CliffFall = 1,
        TimeExpired = 2
    }

    public enum MoveOutcome
    {
        Moved = 0,
        Blocked = 1,
        OverlapDeath = 2,
        CliffDeath = 3,
        ExitReached = 4,
        NightFlowerReached = 5
    }

    public enum StagePhase
    {
        Ready = 0,
        Playing = 1,
        ResolvingAction = 2,
        ResolvingDeath = 3,
        ResolvingClear = 4,
        Stopped = 5
    }

    public enum CellKind
    {
        Safe = 0,
        Blocked = 1,
        Cliff = 2,
        SingleShadow = 3,
        OverlapHazard = 4
    }

    public enum TimerPauseReason
    {
        None = 0,
        FocusLost = 1,
        Locked = 2
    }
}
