using ShadowGarden.Core;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Architecture v1.1 Camera Fitter: fit the entire board into the gameplay viewport.
    /// </summary>
    public static class BoardCameraFitter
    {
        public const float DefaultPaddingCells = 0.5f;

        public readonly struct Framing
        {
            public Vector3 Center { get; }
            public float OrthographicSize { get; }

            public Framing(Vector3 center, float orthographicSize)
            {
                Center = center;
                OrthographicSize = orthographicSize;
            }
        }

        public static Framing Calculate(
            GridSize boardSize,
            float availableAspect,
            float cellSize = GridWorld.CellSize,
            float paddingCells = DefaultPaddingCells,
            float topVisualOverflowCells = 0f)
        {
            if (availableAspect <= 0.01f)
            {
                availableAspect = 16f / 9f;
            }

            topVisualOverflowCells = Mathf.Max(0f, topVisualOverflowCells);
            var halfVertical = boardSize.Height * cellSize * 0.5f
                + paddingCells * cellSize
                + topVisualOverflowCells * cellSize * 0.5f;
            var halfHorizontal = boardSize.Width * cellSize / (2f * availableAspect)
                + paddingCells * cellSize;
            var ortho = Mathf.Max(halfVertical, halfHorizontal);
            var center = GridWorld.BoardCenter(boardSize);
            center.y += topVisualOverflowCells * cellSize * 0.5f;
            return new Framing(new Vector3(center.x, center.y, -10f), ortho);
        }

        public static void Apply(
            Camera camera,
            GridSize boardSize,
            float paddingCells = DefaultPaddingCells,
            float topVisualOverflowCells = 0f)
        {
            if (camera == null)
            {
                return;
            }

            var aspect = camera.aspect > 0.01f ? camera.aspect : 16f / 9f;
            var framing = Calculate(boardSize, aspect, GridWorld.CellSize, paddingCells, topVisualOverflowCells);
            camera.orthographic = true;
            camera.orthographicSize = framing.OrthographicSize;
            camera.transform.position = framing.Center;
        }
    }
}
