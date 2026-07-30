using System.Collections.Generic;

namespace ShadowGarden.Core
{
    /// <summary>
    /// Minimal graybox layouts matching documented learning goals for TestField boards.
    /// Coordinates are not fixed in design docs; these satisfy placement constraints only.
    /// </summary>
    public static class GrayboxStages
    {
        /// <summary>
        /// TestField abyss puzzle: fake east shortcut is ×2; cliffs after short Low shadows;
        /// clear by rotating Triangle away, then crossing Circle High's single shadow bridge.
        /// Grid: North decreases Y (screen-up). Triangle at (1,4) is south of the Circle start.
        /// </summary>
        public static StageDefinition CreateTF_1()
        {
            // Circle High East (3,2): (4,2)(5,2)(6,2)(7,2) → exit (8,2)
            // Triangle Med North (5,4): (5,3)(5,2)(5,1) → intentional ×2 at (5,2)
            // Circle Low East (3,0): (4,0)(5,0) then cliff (6,0)
            var safe = Cells(
                (0, 2), (1, 2), (2, 2),
                (2, 1), (3, 1), (4, 1),
                (1, 1), (1, 0), (0, 0), (2, 0),
                (1, 3), (1, 4), (0, 4), (2, 4));
            return new StageDefinition(
                "TF-1",
                GridSize.Board12x6,
                new GridPosition(1, 2),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(1, 2), ChannelId.Circle, CardinalDirection.East),
                    new LampDefinition(new GridPosition(1, 4), ChannelId.Triangle, CardinalDirection.North)
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(3, 2), ChannelId.Circle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(3, 0), ChannelId.Circle, PillarHeight.Low),
                    new PillarDefinition(new GridPosition(5, 4), ChannelId.Triangle, PillarHeight.Medium)
                },
                ClearGoalType.ExitDoor,
                new GridPosition(8, 2),
                120);
        }

        public static StageDefinition Create1_1()
        {
            // Lamp starts facing North (no east path). One clockwise rotate -> East.
            // Medium pillar casts 3 shadows; safe detour around pillar reaches the path.
            // Layout y=2: S L S P # # # E
            var safe = Cells(
                (0, 2), (1, 2), (2, 2),
                (2, 1), (3, 1), (4, 1));
            return new StageDefinition(
                "1-1",
                GridSize.Board12x6,
                new GridPosition(1, 2),
                safe,
                new[] { new LampDefinition(new GridPosition(1, 2), ChannelId.Circle, CardinalDirection.North) },
                new[] { new PillarDefinition(new GridPosition(3, 2), ChannelId.Circle, PillarHeight.Medium) },
                ClearGoalType.ExitDoor,
                new GridPosition(7, 2),
                120);
        }

        public static StageDefinition Create1_2()
        {
            // Same channel: Low projects 2 cells on row 1, High projects 4 cells on row 4.
            var safe = Cells(
                (0, 1), (1, 1), (2, 1),
                (1, 2), (1, 3), (1, 4), (2, 4),
                (2, 5), (3, 5), (4, 5));
            return new StageDefinition(
                "1-2",
                GridSize.Board12x6,
                new GridPosition(1, 1),
                safe,
                new[] { new LampDefinition(new GridPosition(1, 1), ChannelId.Circle, CardinalDirection.East) },
                new[]
                {
                    new PillarDefinition(new GridPosition(3, 1), ChannelId.Circle, PillarHeight.Low),
                    new PillarDefinition(new GridPosition(3, 4), ChannelId.Circle, PillarHeight.High)
                },
                ClearGoalType.ExitDoor,
                new GridPosition(8, 4),
                120);
        }

        public static StageDefinition Create1_4()
        {
            // Circle East + Triangle North overlap at (5,2). Safe southern bypass to night flower.
            var safe = Cells(
                (0, 2), (1, 2), (2, 2),
                (1, 3), (1, 4),
                (0, 4), (2, 4), (3, 4), (4, 4), (4, 5), (5, 5), (6, 5), (6, 4), (7, 4),
                (7, 3), (7, 2));
            return new StageDefinition(
                "1-4",
                GridSize.Board12x6,
                new GridPosition(1, 2),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(1, 2), ChannelId.Circle, CardinalDirection.East),
                    new LampDefinition(new GridPosition(1, 4), ChannelId.Triangle, CardinalDirection.North)
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(3, 2), ChannelId.Circle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(5, 4), ChannelId.Triangle, PillarHeight.Medium)
                },
                ClearGoalType.NightFlower,
                new GridPosition(7, 2),
                150);
        }

        public static StageDefinition Create3_4()
        {
            // Four channels and three heights on display rows; safe spine to night flower.
            var safe = Cells(
                (0, 0), (1, 0), (2, 0),
                (1, 1), (1, 2), (1, 3), (1, 4), (1, 5),
                (0, 2), (2, 2),
                (0, 4), (2, 4),
                (0, 5), (2, 5), (3, 5), (4, 5), (5, 5), (6, 5), (7, 5), (8, 5), (9, 5), (10, 5), (11, 5));
            return new StageDefinition(
                "3-4",
                GridSize.Board12x6,
                new GridPosition(1, 0),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(1, 0), ChannelId.Circle, CardinalDirection.East),
                    new LampDefinition(new GridPosition(1, 2), ChannelId.Triangle, CardinalDirection.East),
                    new LampDefinition(new GridPosition(1, 4), ChannelId.Star, CardinalDirection.East),
                    new LampDefinition(new GridPosition(1, 5), ChannelId.Diamond, CardinalDirection.East)
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(3, 0), ChannelId.Circle, PillarHeight.Low),
                    new PillarDefinition(new GridPosition(3, 2), ChannelId.Triangle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(3, 4), ChannelId.Star, PillarHeight.High),
                    new PillarDefinition(new GridPosition(8, 2), ChannelId.Diamond, PillarHeight.Medium)
                },
                ClearGoalType.NightFlower,
                new GridPosition(11, 5),
                150);
        }

        public static IEnumerable<StageDefinition> All()
        {
            yield return CreateTF_1();
            yield return Create1_1();
            yield return Create1_2();
            yield return Create1_4();
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
