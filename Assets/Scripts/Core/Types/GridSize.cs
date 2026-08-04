using System;
using System.Collections.Generic;

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
        public static GridSize Board14x7 => new GridSize(14, 7);
        public static GridSize Board16x7 => new GridSize(16, 7);
        public static GridSize Board16x8 => new GridSize(16, 8);
        public static GridSize Board18x8 => new GridSize(18, 8);

        public static IReadOnlyList<GridSize> SupportedBoardSizes { get; } = new[]
        {
            Board12x6,
            Board14x7,
            Board16x7,
            Board16x8,
            Board18x8
        };

        public static bool IsSupported(GridSize size)
        {
            for (var i = 0; i < SupportedBoardSizes.Count; i++)
            {
                if (SupportedBoardSizes[i].Equals(size))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsSupported(int width, int height) => IsSupported(new GridSize(width, height));

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
