namespace ShadowGarden.Runtime
{
    /// <summary>
    /// Application-level screen flow states for Main (architecture v1.1 + Opening).
    /// Distinct from Core StagePhase which covers in-stage puzzle timing only.
    /// </summary>
    public enum AppState
    {
        Title = 0,
        Opening = 1,
        WorldMap = 2,
        Playing = 3,
        GameOver = 4,
        Cleared = 5,
        Ending = 6
    }
}
