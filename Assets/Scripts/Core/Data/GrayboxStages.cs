using System.Collections.Generic;

namespace ShadowGarden.Core
{
    /// <summary>
    /// Canonical level-design boards for TestField technical validation (architecture v1.1).
    /// Coordinates come from the level design document — not legacy graybox layouts.
    /// </summary>
    public static class GrayboxStages
    {
        public static StageDefinition Create1_1()
        {
            // 첫 그림자 · 12×6 · ExitDoor 120s
            // Lamp North initially; one clockwise rotate opens Medium bridge across the gap at (5,3)/(5,2)/(6,2).
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

        public static StageDefinition Create1_4()
        {
            // 두 빛의 정원 · 12×6 · NightFlower 150s
            var safe = Cells(
                (0, 1), (1, 1), (2, 1), (3, 1), (4, 1), (7, 1), (8, 1), (9, 1), (10, 1),
                (0, 2), (1, 2), (2, 2), (3, 2), (4, 2), (8, 2), (10, 2),
                (8, 3), (10, 3));
            return new StageDefinition(
                "1-4",
                GridSize.Board12x6,
                new GridPosition(1, 2),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(1, 2), ChannelId.Circle, CardinalDirection.West),
                    new LampDefinition(new GridPosition(9, 1), ChannelId.Triangle, CardinalDirection.East)
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(3, 0), ChannelId.Circle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(2, 3), ChannelId.Circle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(9, 2), ChannelId.Triangle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(6, 1), ChannelId.Triangle, PillarHeight.Medium)
                },
                ClearGoalType.NightFlower,
                new GridPosition(9, 5),
                150);
        }

        public static StageDefinition Create2_2()
        {
            // 세 갈래의 바람 · 12×6 · ExitDoor 120s · 3 channels
            var safe = Cells(
                (0, 2), (1, 2), (2, 2), (3, 2), (4, 2),
                (0, 3), (1, 3), (2, 3), (3, 3), (4, 3),
                (0, 4), (1, 4), (2, 4), (3, 4), (4, 4));
            return new StageDefinition(
                "2-2",
                GridSize.Board12x6,
                new GridPosition(0, 3),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(1, 2), ChannelId.Circle, CardinalDirection.North),
                    new LampDefinition(new GridPosition(2, 3), ChannelId.Triangle, CardinalDirection.East),
                    new LampDefinition(new GridPosition(1, 4), ChannelId.Star, CardinalDirection.West)
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(3, 1), ChannelId.Circle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(7, 0), ChannelId.Triangle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(6, 4), ChannelId.Star, PillarHeight.High),
                    new PillarDefinition(new GridPosition(3, 5), ChannelId.Circle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(10, 0), ChannelId.Triangle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(8, 5), ChannelId.Star, PillarHeight.High)
                },
                ClearGoalType.ExitDoor,
                new GridPosition(11, 4),
                120);
        }

        public static StageDefinition Create3_4()
        {
            // 정원의 심장 · 18×8 · NightFlower 150s · 4 channels
            var safe = Cells(
                (0, 2), (1, 2), (2, 2), (3, 2), (4, 2), (5, 2),
                (14, 2), (15, 2), (16, 2), (17, 2),
                (0, 3), (1, 3), (2, 3), (3, 3), (4, 3), (5, 3),
                (0, 4), (1, 4), (2, 4), (3, 4), (4, 4), (5, 4),
                (0, 5), (1, 5), (2, 5), (3, 5), (4, 5), (5, 5));
            return new StageDefinition(
                "3-4",
                GridSize.Board18x8,
                new GridPosition(0, 3),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(1, 2), ChannelId.Circle, CardinalDirection.West),
                    new LampDefinition(new GridPosition(2, 3), ChannelId.Triangle, CardinalDirection.North),
                    new LampDefinition(new GridPosition(1, 4), ChannelId.Star, CardinalDirection.West),
                    new LampDefinition(new GridPosition(3, 4), ChannelId.Diamond, CardinalDirection.South)
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(4, 1), ChannelId.Circle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(9, 0), ChannelId.Triangle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(8, 5), ChannelId.Star, PillarHeight.High),
                    new PillarDefinition(new GridPosition(13, 6), ChannelId.Diamond, PillarHeight.High),
                    new PillarDefinition(new GridPosition(4, 7), ChannelId.Circle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(14, 3), ChannelId.Circle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(16, 0), ChannelId.Triangle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(6, 2), ChannelId.Triangle, PillarHeight.Low),
                    new PillarDefinition(new GridPosition(11, 7), ChannelId.Star, PillarHeight.High),
                    new PillarDefinition(new GridPosition(7, 6), ChannelId.Diamond, PillarHeight.Medium)
                },
                ClearGoalType.NightFlower,
                new GridPosition(17, 2),
                150);
        }

        public static IEnumerable<StageDefinition> All()
        {
            yield return Create1_1();
            yield return Create1_4();
            yield return Create2_2();
            yield return Create3_4();
        }

        private static GridPosition[] Cells(params (int x, int y)[] values)
        {
            var cells = new GridPosition[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                cells[i] = new GridPosition(values[i].x, values[i].y);
            }

            return cells;
        }
    }
}
