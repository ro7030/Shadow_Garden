using System.Collections.Generic;

namespace ShadowGarden.Core
{
    /// <summary>
    /// Canonical main-story stage definitions from the level design document.
    /// Distinct from <see cref="GrayboxStages"/> which backs TestField prototype assets.
    /// </summary>
    public static class MainStages
    {
        public static StageDefinition Create1_1()
        {
            // 1-1 · 첫 그림자 · 노을 과수원 · 12×6 · ExitDoor 120s
            // 원 태양등 (2,3) 북→동 1회전, 중간 기둥 (3,2) 3칸이 끊긴 지형을 잇는다.
            var safe = Cells(
                (0, 3), (1, 3), (2, 3), (3, 3), (4, 3),
                (6, 3), (7, 3), (8, 3), (9, 3), (10, 3), (11, 3));
            return new StageDefinition(
                "1-1",
                GridSize.Board12x6,
                new GridPosition(1, 3),
                safe,
                new[] { new LampDefinition(new GridPosition(2, 3), ChannelId.Circle, CardinalDirection.North) },
                new[] { new PillarDefinition(new GridPosition(3, 2), ChannelId.Circle, PillarHeight.Medium) },
                ClearGoalType.ExitDoor,
                new GridPosition(11, 3),
                120);
        }

        private static GridPosition[] Cells(params (int x, int y)[] cells)
        {
            var result = new GridPosition[cells.Length];
            for (var i = 0; i < cells.Length; i++)
            {
                result[i] = new GridPosition(cells[i].x, cells[i].y);
            }

            return result;
        }

        /// <summary>
        /// Minimal safe path after rotating Circle to East (level design §14).
        /// </summary>
        public static IReadOnlyList<GridPosition> Stage1_1SolutionPathAfterEastRotate() =>
            new[]
            {
                new GridPosition(1, 3),
                new GridPosition(2, 3),
                new GridPosition(3, 3),
                new GridPosition(4, 3),
                new GridPosition(4, 2),
                new GridPosition(5, 2),
                new GridPosition(6, 2),
                new GridPosition(6, 3),
                new GridPosition(7, 3),
                new GridPosition(8, 3),
                new GridPosition(9, 3),
                new GridPosition(10, 3),
                new GridPosition(11, 3)
            };
    }
}
