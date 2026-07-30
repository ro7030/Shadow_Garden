using System;

namespace ShadowGarden.Core
{
    public readonly struct GridSize : IEquatable<GridSize>
    {
        public int Width { get; }
        public int Height { get; }
        public int CellCount => Width * Height;

        public GridSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public static GridSize Board12x6 => new GridSize(12, 6);

        public bool Contains(GridPosition position) =>
            position.X >= 0 && position.Y >= 0 && position.X < Width && position.Y < Height;

        public int ToIndex(GridPosition position) => position.Y * Width + position.X;

        public GridPosition FromIndex(int index) => new GridPosition(index % Width, index / Width);

        public bool Equals(GridSize other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is GridSize other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Width, Height);
        public override string ToString() => $"{Width}x{Height}";
    }
}
