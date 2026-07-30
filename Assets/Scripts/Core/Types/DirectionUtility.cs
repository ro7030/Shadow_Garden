namespace ShadowGarden.Core
{
    public static class DirectionUtility
    {
        public static CardinalDirection Rotate(CardinalDirection direction, int quarterTurnsClockwise)
        {
            var steps = ((int)direction + quarterTurnsClockwise) % 4;
            if (steps < 0)
            {
                steps += 4;
            }

            return (CardinalDirection)steps;
        }

        public static CardinalDirection RotateClockwise(CardinalDirection direction) =>
            Rotate(direction, 1);

        public static CardinalDirection RotateCounterClockwise(CardinalDirection direction) =>
            Rotate(direction, -1);
    }
}
