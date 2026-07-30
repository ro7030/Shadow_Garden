using System;

namespace ShadowGarden.Core
{
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public int X { get; }
        public int Y { get; }

        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public GridPosition Offset(int dx, int dy) => new GridPosition(X + dx, Y + dy);

        public GridPosition Step(CardinalDirection direction)
        {
            return direction switch
            {
                CardinalDirection.North => Offset(0, -1),
                CardinalDirection.East => Offset(1, 0),
                CardinalDirection.South => Offset(0, 1),
                CardinalDirection.West => Offset(-1, 0),
                _ => this
            };
        }

        public bool Equals(GridPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X},{Y})";

        public static bool operator ==(GridPosition left, GridPosition right) => left.Equals(right);
        public static bool operator !=(GridPosition left, GridPosition right) => !left.Equals(right);
    }
}
