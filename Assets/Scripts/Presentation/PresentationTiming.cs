namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Presentation-only timing from UI/UX + in-game asset contracts.
    /// Core rule results resolve immediately; these values drive visuals and input locks.
    /// </summary>
    public static class PresentationTiming
    {
        public const float MoveSeconds = 0.12f;
        public const float RotateSeconds = 0.18f;
        public const float DoorOpenSeconds = 0.45f;
        public const float GoalPassSeconds = 0.35f;
        public const float CliffApproachCells = 0.35f;
        public const float CliffFallSeconds = 0.5f;
        public const float OverlapSinkSeconds = 0.55f;
        public const float TimeVacuumSeconds = 0.65f;
        public const float NightFlowerBloomSeconds = 1.5f;
        public const float OpeningSkipHoldSeconds = 1f;
        public const float WorldUnlockBeatSeconds = 1.2f;
        public const float EndingBeatSeconds = 10f;
        public const float EndingWorldRecoverSeconds = 6f;
        public const float EndingCelebrateSeconds = 4f;
    }
}
