using ShadowGarden.Core;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    public static class GridWorld
    {
        public const float CellSize = 1f;

        public static Vector3 ToWorld(GridPosition position, float z = 0f)
        {
            return new Vector3(position.X * CellSize, -position.Y * CellSize, z);
        }

        public static Vector3 BoardCenter(GridSize size)
        {
            return new Vector3((size.Width - 1) * CellSize * 0.5f, -(size.Height - 1) * CellSize * 0.5f, 0f);
        }
    }
}
