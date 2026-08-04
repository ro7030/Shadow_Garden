namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Presentation-only timing. Core rule results resolve immediately; these values drive visuals and input locks.
    /// </summary>
    public static class PresentationTiming
    {
        public const float MoveSeconds = 0.12f;
        public const float RotateSeconds = 0.18f;
        public const float DoorOpenSeconds = 0.45f;
        public const float GoalPassSeconds = 0.35f;
        public const float CliffFallSeconds = 0.5f;
    }
}
