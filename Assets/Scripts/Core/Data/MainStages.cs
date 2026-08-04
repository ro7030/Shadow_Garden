using System.Collections.Generic;

namespace ShadowGarden.Core
{
    /// <summary>
    /// Canonical main-story stage definitions from the level design document.
    /// Distinct from <see cref="GrayboxStages"/> (TestField technical validation subset).
    /// </summary>
    public static class MainStages
    {
        public static IReadOnlyList<StageDefinition> AllDefinitions()
        {
            return new[]
            {
                Create1_1(),
                Create1_2(),
                Create1_3(),
                Create1_4(),
                Create2_1(),
                Create2_2(),
                Create2_3(),
                Create2_4(),
                Create3_1(),
                Create3_2(),
                Create3_3(),
                Create3_4(),
            };
        }

        public static IReadOnlyList<StageBundle> AllBundles()
        {
            return new[]
            {
                Bundle(Create1_1(), Solution_1_1()),
                Bundle(Create1_2(), Solution_1_2()),
                Bundle(Create1_3(), Solution_1_3()),
                Bundle(Create1_4(), Solution_1_4()),
                Bundle(Create2_1(), Solution_2_1()),
                Bundle(Create2_2(), Solution_2_2()),
                Bundle(Create2_3(), Solution_2_3()),
                Bundle(Create2_4(), Solution_2_4()),
                Bundle(Create3_1(), Solution_3_1()),
                Bundle(Create3_2(), Solution_3_2()),
                Bundle(Create3_3(), Solution_3_3()),
                Bundle(Create3_4(), Solution_3_4()),
            };
        }

        private static StageBundle Bundle(StageDefinition definition, RecordedSolution solution) =>
            new StageBundle(definition, solution);

        public static StageDefinition Create1_1()
        {
            var safe = Cells(
                (0, 3),
                (1, 3),
                (2, 3),
                (3, 3),
                (4, 3),
                (6, 3),
                (7, 3),
                (8, 3),
                (9, 3),
                (10, 3),
                (11, 3)
            );
            return new StageDefinition(
                "1-1",
                GridSize.Board12x6,
                new GridPosition(1, 3),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(2, 3), ChannelId.Circle, CardinalDirection.North),
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(3, 2), ChannelId.Circle, PillarHeight.Medium),
                },
                ClearGoalType.ExitDoor,
                new GridPosition(11, 3),
                120);
        }

        public static StageDefinition Create1_2()
        {
            var safe = Cells(
                (1, 1),
                (6, 1),
                (1, 2),
                (4, 2),
                (6, 2),
                (0, 3),
                (1, 3),
                (2, 3),
                (3, 3),
                (4, 3),
                (1, 4),
                (8, 4),
                (9, 4),
                (10, 4),
                (11, 4)
            );
            return new StageDefinition(
                "1-2",
                GridSize.Board12x6,
                new GridPosition(1, 3),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(2, 3), ChannelId.Circle, CardinalDirection.North),
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(3, 1), ChannelId.Circle, PillarHeight.Low),
                    new PillarDefinition(new GridPosition(3, 4), ChannelId.Circle, PillarHeight.High),
                },
                ClearGoalType.ExitDoor,
                new GridPosition(11, 4),
                120);
        }

        public static StageDefinition Create1_3()
        {
            var safe = Cells(
                (6, 0),
                (7, 2),
                (7, 3),
                (0, 4),
                (1, 4),
                (2, 4),
                (3, 4),
                (4, 4),
                (8, 5),
                (9, 5),
                (10, 5),
                (11, 5)
            );
            return new StageDefinition(
                "1-3",
                GridSize.Board12x6,
                new GridPosition(1, 4),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(2, 4), ChannelId.Circle, CardinalDirection.West),
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(3, 0), ChannelId.Circle, PillarHeight.Low),
                    new PillarDefinition(new GridPosition(3, 2), ChannelId.Circle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(3, 5), ChannelId.Circle, PillarHeight.High),
                },
                ClearGoalType.ExitDoor,
                new GridPosition(11, 5),
                120);
        }

        public static StageDefinition Create1_4()
        {
            var safe = Cells(
                (0, 1),
                (1, 1),
                (2, 1),
                (3, 1),
                (4, 1),
                (7, 1),
                (8, 1),
                (9, 1),
                (10, 1),
                (0, 2),
                (1, 2),
                (2, 2),
                (3, 2),
                (4, 2),
                (8, 2),
                (10, 2),
                (8, 3),
                (10, 3)
            );
            return new StageDefinition(
                "1-4",
                GridSize.Board12x6,
                new GridPosition(1, 2),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(1, 2), ChannelId.Circle, CardinalDirection.West),
                    new LampDefinition(new GridPosition(9, 1), ChannelId.Triangle, CardinalDirection.East),
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(3, 0), ChannelId.Circle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(2, 3), ChannelId.Circle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(9, 2), ChannelId.Triangle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(6, 1), ChannelId.Triangle, PillarHeight.Medium),
                },
                ClearGoalType.NightFlower,
                new GridPosition(9, 5),
                150);
        }

        public static StageDefinition Create2_1()
        {
            var safe = Cells(
                (0, 1),
                (1, 1),
                (2, 1),
                (3, 1),
                (4, 1),
                (7, 1),
                (8, 1),
                (9, 1),
                (10, 1),
                (0, 2),
                (1, 2),
                (2, 2),
                (3, 2),
                (4, 2),
                (7, 2),
                (8, 2),
                (9, 2),
                (7, 3),
                (8, 3),
                (9, 3)
            );
            return new StageDefinition(
                "2-1",
                GridSize.Board12x6,
                new GridPosition(1, 2),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(1, 2), ChannelId.Circle, CardinalDirection.West),
                    new LampDefinition(new GridPosition(8, 2), ChannelId.Triangle, CardinalDirection.North),
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(3, 0), ChannelId.Circle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(10, 2), ChannelId.Triangle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(2, 4), ChannelId.Circle, PillarHeight.Low),
                    new PillarDefinition(new GridPosition(0, 5), ChannelId.Triangle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(4, 5), ChannelId.Circle, PillarHeight.Medium),
                },
                ClearGoalType.ExitDoor,
                new GridPosition(10, 5),
                120);
        }

        public static StageDefinition Create2_2()
        {
            var safe = Cells(
                (0, 2),
                (1, 2),
                (2, 2),
                (3, 2),
                (4, 2),
                (0, 3),
                (1, 3),
                (2, 3),
                (3, 3),
                (4, 3),
                (0, 4),
                (1, 4),
                (2, 4),
                (3, 4),
                (4, 4)
            );
            return new StageDefinition(
                "2-2",
                GridSize.Board12x6,
                new GridPosition(0, 3),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(1, 2), ChannelId.Circle, CardinalDirection.North),
                    new LampDefinition(new GridPosition(2, 3), ChannelId.Triangle, CardinalDirection.East),
                    new LampDefinition(new GridPosition(1, 4), ChannelId.Star, CardinalDirection.West),
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(3, 1), ChannelId.Circle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(7, 0), ChannelId.Triangle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(6, 4), ChannelId.Star, PillarHeight.High),
                    new PillarDefinition(new GridPosition(3, 5), ChannelId.Circle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(10, 0), ChannelId.Triangle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(8, 5), ChannelId.Star, PillarHeight.High),
                },
                ClearGoalType.ExitDoor,
                new GridPosition(11, 4),
                120);
        }

        public static StageDefinition Create2_3()
        {
            var safe = Cells(
                (7, 0),
                (8, 0),
                (0, 1),
                (1, 1),
                (2, 1),
                (3, 1),
                (4, 1),
                (7, 1),
                (8, 1),
                (0, 2),
                (1, 2),
                (2, 2),
                (3, 2),
                (4, 2),
                (10, 2),
                (11, 2),
                (0, 3),
                (1, 3),
                (2, 3),
                (3, 3),
                (4, 3),
                (0, 4),
                (1, 4),
                (2, 4),
                (3, 4),
                (4, 4)
            );
            return new StageDefinition(
                "2-3",
                GridSize.Board12x6,
                new GridPosition(0, 2),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(1, 2), ChannelId.Circle, CardinalDirection.North),
                    new LampDefinition(new GridPosition(2, 3), ChannelId.Triangle, CardinalDirection.West),
                    new LampDefinition(new GridPosition(8, 1), ChannelId.Star, CardinalDirection.West),
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(3, 1), ChannelId.Circle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(5, 0), ChannelId.Circle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(4, 4), ChannelId.Star, PillarHeight.High),
                    new PillarDefinition(new GridPosition(9, 5), ChannelId.Triangle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(2, 5), ChannelId.Circle, PillarHeight.Low),
                    new PillarDefinition(new GridPosition(11, 5), ChannelId.Triangle, PillarHeight.Low),
                    new PillarDefinition(new GridPosition(7, 5), ChannelId.Star, PillarHeight.Medium),
                },
                ClearGoalType.ExitDoor,
                new GridPosition(11, 2),
                120);
        }

        public static StageDefinition Create2_4()
        {
            var safe = Cells(
                (0, 2),
                (1, 2),
                (2, 2),
                (3, 2),
                (4, 2),
                (0, 3),
                (1, 3),
                (2, 3),
                (3, 3),
                (4, 3),
                (0, 4),
                (1, 4),
                (2, 4),
                (3, 4),
                (4, 4)
            );
            return new StageDefinition(
                "2-4",
                GridSize.Board12x6,
                new GridPosition(0, 3),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(1, 2), ChannelId.Circle, CardinalDirection.West),
                    new LampDefinition(new GridPosition(2, 3), ChannelId.Triangle, CardinalDirection.North),
                    new LampDefinition(new GridPosition(1, 4), ChannelId.Star, CardinalDirection.West),
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(3, 1), ChannelId.Circle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(7, 0), ChannelId.Triangle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(6, 4), ChannelId.Star, PillarHeight.High),
                    new PillarDefinition(new GridPosition(3, 5), ChannelId.Circle, PillarHeight.Low),
                    new PillarDefinition(new GridPosition(8, 2), ChannelId.Circle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(10, 0), ChannelId.Triangle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(5, 2), ChannelId.Triangle, PillarHeight.Low),
                    new PillarDefinition(new GridPosition(8, 5), ChannelId.Star, PillarHeight.Medium),
                },
                ClearGoalType.NightFlower,
                new GridPosition(11, 4),
                150);
        }

        public static StageDefinition Create3_1()
        {
            var safe = Cells(
                (0, 2),
                (1, 2),
                (2, 2),
                (3, 2),
                (4, 2),
                (5, 2),
                (0, 3),
                (1, 3),
                (2, 3),
                (3, 3),
                (4, 3),
                (5, 3),
                (0, 4),
                (1, 4),
                (2, 4),
                (3, 4),
                (4, 4),
                (5, 4)
            );
            return new StageDefinition(
                "3-1",
                GridSize.Board14x7,
                new GridPosition(0, 3),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(1, 2), ChannelId.Circle, CardinalDirection.North),
                    new LampDefinition(new GridPosition(2, 3), ChannelId.Triangle, CardinalDirection.North),
                    new LampDefinition(new GridPosition(1, 4), ChannelId.Star, CardinalDirection.West),
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(4, 1), ChannelId.Circle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(9, 0), ChannelId.Triangle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(8, 5), ChannelId.Star, PillarHeight.High),
                    new PillarDefinition(new GridPosition(4, 6), ChannelId.Circle, PillarHeight.Low),
                    new PillarDefinition(new GridPosition(12, 0), ChannelId.Triangle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(10, 6), ChannelId.Star, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(1, 6), ChannelId.Circle, PillarHeight.Medium),
                },
                ClearGoalType.ExitDoor,
                new GridPosition(13, 5),
                120);
        }

        public static StageDefinition Create3_2()
        {
            var safe = Cells(
                (0, 2),
                (1, 2),
                (2, 2),
                (3, 2),
                (4, 2),
                (5, 2),
                (0, 3),
                (1, 3),
                (2, 3),
                (3, 3),
                (4, 3),
                (5, 3),
                (0, 4),
                (1, 4),
                (2, 4),
                (3, 4),
                (4, 4),
                (5, 4),
                (13, 5),
                (14, 5),
                (15, 5)
            );
            return new StageDefinition(
                "3-2",
                GridSize.Board16x7,
                new GridPosition(0, 3),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(1, 2), ChannelId.Circle, CardinalDirection.West),
                    new LampDefinition(new GridPosition(2, 3), ChannelId.Triangle, CardinalDirection.North),
                    new LampDefinition(new GridPosition(1, 4), ChannelId.Star, CardinalDirection.West),
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(4, 1), ChannelId.Circle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(9, 0), ChannelId.Triangle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(8, 5), ChannelId.Star, PillarHeight.High),
                    new PillarDefinition(new GridPosition(4, 6), ChannelId.Circle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(10, 2), ChannelId.Circle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(13, 0), ChannelId.Triangle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(6, 2), ChannelId.Triangle, PillarHeight.Low),
                    new PillarDefinition(new GridPosition(11, 6), ChannelId.Star, PillarHeight.High),
                },
                ClearGoalType.ExitDoor,
                new GridPosition(15, 5),
                120);
        }

        public static StageDefinition Create3_3()
        {
            var safe = Cells(
                (0, 2),
                (1, 2),
                (2, 2),
                (3, 2),
                (4, 2),
                (5, 2),
                (14, 2),
                (15, 2),
                (0, 3),
                (1, 3),
                (2, 3),
                (3, 3),
                (4, 3),
                (5, 3),
                (0, 4),
                (1, 4),
                (2, 4),
                (3, 4),
                (4, 4),
                (5, 4),
                (0, 5),
                (1, 5),
                (2, 5),
                (3, 5),
                (4, 5),
                (5, 5)
            );
            return new StageDefinition(
                "3-3",
                GridSize.Board16x8,
                new GridPosition(0, 3),
                safe,
                new[]
                {
                    new LampDefinition(new GridPosition(1, 2), ChannelId.Circle, CardinalDirection.North),
                    new LampDefinition(new GridPosition(2, 3), ChannelId.Triangle, CardinalDirection.North),
                    new LampDefinition(new GridPosition(1, 4), ChannelId.Star, CardinalDirection.West),
                    new LampDefinition(new GridPosition(3, 4), ChannelId.Diamond, CardinalDirection.South),
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(4, 1), ChannelId.Circle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(9, 0), ChannelId.Triangle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(8, 5), ChannelId.Star, PillarHeight.High),
                    new PillarDefinition(new GridPosition(13, 6), ChannelId.Diamond, PillarHeight.High),
                    new PillarDefinition(new GridPosition(4, 7), ChannelId.Circle, PillarHeight.Low),
                    new PillarDefinition(new GridPosition(15, 0), ChannelId.Triangle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(10, 7), ChannelId.Star, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(6, 6), ChannelId.Diamond, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(1, 0), ChannelId.Circle, PillarHeight.Low),
                },
                ClearGoalType.ExitDoor,
                new GridPosition(15, 2),
                120);
        }

        public static StageDefinition Create3_4()
        {
            var safe = Cells(
                (0, 2),
                (1, 2),
                (2, 2),
                (3, 2),
                (4, 2),
                (5, 2),
                (14, 2),
                (15, 2),
                (16, 2),
                (17, 2),
                (0, 3),
                (1, 3),
                (2, 3),
                (3, 3),
                (4, 3),
                (5, 3),
                (0, 4),
                (1, 4),
                (2, 4),
                (3, 4),
                (4, 4),
                (5, 4),
                (0, 5),
                (1, 5),
                (2, 5),
                (3, 5),
                (4, 5),
                (5, 5)
            );
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
                    new LampDefinition(new GridPosition(3, 4), ChannelId.Diamond, CardinalDirection.South),
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
                    new PillarDefinition(new GridPosition(7, 6), ChannelId.Diamond, PillarHeight.Medium),
                },
                ClearGoalType.NightFlower,
                new GridPosition(17, 2),
                150);
        }

        public static RecordedSolution Solution_1_1()
        {
            var path = new[]
            {
                new GridPosition(1, 3), new GridPosition(2, 3), new GridPosition(2, 3), new GridPosition(3, 3),
                new GridPosition(4, 3), new GridPosition(4, 2), new GridPosition(5, 2), new GridPosition(6, 2),
                new GridPosition(6, 3), new GridPosition(7, 3), new GridPosition(8, 3), new GridPosition(9, 3),
                new GridPosition(10, 3), new GridPosition(11, 3)
            };
            var rotates = new[]
            {
                new RecordedRotate(ChannelId.Circle, 1),
            };
            return new RecordedSolution(path, rotates, 1, System.Array.Empty<RecordedSolution>());
        }

        public static RecordedSolution Solution_1_2()
        {
            var path = new[]
            {
                new GridPosition(1, 3), new GridPosition(2, 3), new GridPosition(2, 3), new GridPosition(3, 3),
                new GridPosition(4, 3), new GridPosition(4, 4), new GridPosition(5, 4), new GridPosition(6, 4),
                new GridPosition(7, 4), new GridPosition(8, 4), new GridPosition(9, 4), new GridPosition(10, 4),
                new GridPosition(11, 4)
            };
            var rotates = new[]
            {
                new RecordedRotate(ChannelId.Circle, 1),
            };
            return new RecordedSolution(path, rotates, 1, System.Array.Empty<RecordedSolution>());
        }

        public static RecordedSolution Solution_1_3()
        {
            var path = new[]
            {
                new GridPosition(1, 4), new GridPosition(2, 4), new GridPosition(2, 4), new GridPosition(2, 4),
                new GridPosition(3, 4), new GridPosition(4, 4), new GridPosition(4, 5), new GridPosition(5, 5),
                new GridPosition(6, 5), new GridPosition(7, 5), new GridPosition(8, 5), new GridPosition(9, 5),
                new GridPosition(10, 5), new GridPosition(11, 5)
            };
            var rotates = new[]
            {
                new RecordedRotate(ChannelId.Circle, -1),
                new RecordedRotate(ChannelId.Circle, -1),
            };
            return new RecordedSolution(path, rotates, 2, System.Array.Empty<RecordedSolution>());
        }

        public static RecordedSolution Solution_1_4()
        {
            var path = new[]
            {
                new GridPosition(1, 2), new GridPosition(1, 2), new GridPosition(1, 2), new GridPosition(1, 1),
                new GridPosition(2, 1), new GridPosition(3, 1), new GridPosition(4, 1), new GridPosition(4, 0),
                new GridPosition(5, 0), new GridPosition(6, 0), new GridPosition(7, 0), new GridPosition(7, 1),
                new GridPosition(8, 1), new GridPosition(9, 1), new GridPosition(9, 1), new GridPosition(10, 1),
                new GridPosition(10, 2), new GridPosition(10, 3), new GridPosition(9, 3), new GridPosition(9, 4),
                new GridPosition(9, 5)
            };
            var rotates = new[]
            {
                new RecordedRotate(ChannelId.Circle, -1),
                new RecordedRotate(ChannelId.Circle, -1),
                new RecordedRotate(ChannelId.Triangle, 1),
            };
            return new RecordedSolution(path, rotates, 3, System.Array.Empty<RecordedSolution>());
        }

        public static RecordedSolution Solution_2_1()
        {
            var path = new[]
            {
                new GridPosition(1, 2), new GridPosition(1, 2), new GridPosition(1, 2), new GridPosition(1, 1),
                new GridPosition(2, 1), new GridPosition(3, 1), new GridPosition(4, 1), new GridPosition(4, 0),
                new GridPosition(5, 0), new GridPosition(6, 0), new GridPosition(7, 0), new GridPosition(7, 1),
                new GridPosition(8, 1), new GridPosition(8, 2), new GridPosition(8, 2), new GridPosition(8, 2),
                new GridPosition(9, 2), new GridPosition(9, 3), new GridPosition(10, 3), new GridPosition(10, 4),
                new GridPosition(10, 5)
            };
            var rotates = new[]
            {
                new RecordedRotate(ChannelId.Circle, -1),
                new RecordedRotate(ChannelId.Circle, -1),
                new RecordedRotate(ChannelId.Triangle, -1),
                new RecordedRotate(ChannelId.Triangle, -1),
            };
            return new RecordedSolution(path, rotates, 4, System.Array.Empty<RecordedSolution>());
        }

        public static RecordedSolution Solution_2_2()
        {
            var path = new[]
            {
                new GridPosition(0, 3), new GridPosition(0, 2), new GridPosition(1, 2), new GridPosition(1, 2),
                new GridPosition(1, 3), new GridPosition(1, 4), new GridPosition(1, 4), new GridPosition(1, 4),
                new GridPosition(1, 3), new GridPosition(2, 3), new GridPosition(2, 3), new GridPosition(2, 2),
                new GridPosition(3, 2), new GridPosition(4, 2), new GridPosition(4, 1), new GridPosition(5, 1),
                new GridPosition(6, 1), new GridPosition(7, 1), new GridPosition(7, 2), new GridPosition(7, 3),
                new GridPosition(7, 4), new GridPosition(8, 4), new GridPosition(9, 4), new GridPosition(10, 4),
                new GridPosition(11, 4)
            };
            var rotates = new[]
            {
                new RecordedRotate(ChannelId.Circle, 1),
                new RecordedRotate(ChannelId.Star, -1),
                new RecordedRotate(ChannelId.Star, -1),
                new RecordedRotate(ChannelId.Triangle, 1),
            };
            return new RecordedSolution(path, rotates, 4, System.Array.Empty<RecordedSolution>());
        }

        public static RecordedSolution Solution_2_3()
        {
            var path = new[]
            {
                new GridPosition(0, 2), new GridPosition(1, 2), new GridPosition(1, 2), new GridPosition(2, 2),
                new GridPosition(3, 2), new GridPosition(4, 2), new GridPosition(4, 1), new GridPosition(5, 1),
                new GridPosition(6, 1), new GridPosition(7, 1), new GridPosition(8, 1), new GridPosition(8, 1),
                new GridPosition(8, 1), new GridPosition(7, 1), new GridPosition(6, 1), new GridPosition(5, 1),
                new GridPosition(4, 1), new GridPosition(4, 2), new GridPosition(3, 2), new GridPosition(2, 2),
                new GridPosition(1, 2), new GridPosition(1, 2), new GridPosition(2, 2), new GridPosition(2, 3),
                new GridPosition(2, 3), new GridPosition(3, 3), new GridPosition(4, 3), new GridPosition(5, 3),
                new GridPosition(5, 4), new GridPosition(6, 4), new GridPosition(7, 4), new GridPosition(8, 4),
                new GridPosition(9, 4), new GridPosition(9, 3), new GridPosition(9, 2), new GridPosition(10, 2),
                new GridPosition(11, 2)
            };
            var rotates = new[]
            {
                new RecordedRotate(ChannelId.Circle, 1),
                new RecordedRotate(ChannelId.Star, -1),
                new RecordedRotate(ChannelId.Star, -1),
                new RecordedRotate(ChannelId.Circle, 1),
                new RecordedRotate(ChannelId.Triangle, 1),
            };
            return new RecordedSolution(path, rotates, 5, System.Array.Empty<RecordedSolution>());
        }

        public static RecordedSolution Solution_2_4()
        {
            var path = new[]
            {
                new GridPosition(0, 3), new GridPosition(0, 2), new GridPosition(1, 2), new GridPosition(1, 2),
                new GridPosition(1, 2), new GridPosition(1, 3), new GridPosition(1, 4), new GridPosition(1, 4),
                new GridPosition(1, 4), new GridPosition(1, 3), new GridPosition(2, 3), new GridPosition(2, 3),
                new GridPosition(2, 3), new GridPosition(2, 2), new GridPosition(3, 2), new GridPosition(4, 2),
                new GridPosition(4, 1), new GridPosition(5, 1), new GridPosition(6, 1), new GridPosition(7, 1),
                new GridPosition(7, 2), new GridPosition(7, 3), new GridPosition(7, 4), new GridPosition(8, 4),
                new GridPosition(9, 4), new GridPosition(10, 4), new GridPosition(11, 4)
            };
            var rotates = new[]
            {
                new RecordedRotate(ChannelId.Circle, -1),
                new RecordedRotate(ChannelId.Circle, -1),
                new RecordedRotate(ChannelId.Star, -1),
                new RecordedRotate(ChannelId.Star, -1),
                new RecordedRotate(ChannelId.Triangle, -1),
                new RecordedRotate(ChannelId.Triangle, -1),
            };
            return new RecordedSolution(path, rotates, 6, System.Array.Empty<RecordedSolution>());
        }

        public static RecordedSolution Solution_3_1()
        {
            var path = new[]
            {
                new GridPosition(0, 3), new GridPosition(0, 2), new GridPosition(1, 2), new GridPosition(1, 2),
                new GridPosition(1, 3), new GridPosition(1, 4), new GridPosition(1, 4), new GridPosition(1, 4),
                new GridPosition(1, 3), new GridPosition(2, 3), new GridPosition(2, 3), new GridPosition(2, 3),
                new GridPosition(2, 2), new GridPosition(3, 2), new GridPosition(4, 2), new GridPosition(5, 2),
                new GridPosition(5, 1), new GridPosition(6, 1), new GridPosition(7, 1), new GridPosition(8, 1),
                new GridPosition(9, 1), new GridPosition(9, 2), new GridPosition(9, 3), new GridPosition(9, 4),
                new GridPosition(9, 5), new GridPosition(10, 5), new GridPosition(11, 5), new GridPosition(12, 5),
                new GridPosition(13, 5)
            };
            var rotates = new[]
            {
                new RecordedRotate(ChannelId.Circle, 1),
                new RecordedRotate(ChannelId.Star, -1),
                new RecordedRotate(ChannelId.Star, -1),
                new RecordedRotate(ChannelId.Triangle, -1),
                new RecordedRotate(ChannelId.Triangle, -1),
            };
            return new RecordedSolution(path, rotates, 5, System.Array.Empty<RecordedSolution>());
        }

        public static RecordedSolution Solution_3_2()
        {
            var path = new[]
            {
                new GridPosition(0, 3), new GridPosition(0, 2), new GridPosition(1, 2), new GridPosition(1, 2),
                new GridPosition(1, 2), new GridPosition(1, 3), new GridPosition(1, 4), new GridPosition(1, 4),
                new GridPosition(1, 4), new GridPosition(1, 3), new GridPosition(2, 3), new GridPosition(2, 3),
                new GridPosition(2, 3), new GridPosition(2, 2), new GridPosition(3, 2), new GridPosition(4, 2),
                new GridPosition(5, 2), new GridPosition(5, 1), new GridPosition(6, 1), new GridPosition(7, 1),
                new GridPosition(8, 1), new GridPosition(9, 1), new GridPosition(9, 2), new GridPosition(9, 3),
                new GridPosition(9, 4), new GridPosition(9, 5), new GridPosition(10, 5), new GridPosition(11, 5),
                new GridPosition(12, 5), new GridPosition(13, 5), new GridPosition(14, 5), new GridPosition(15, 5)
            };
            var rotates = new[]
            {
                new RecordedRotate(ChannelId.Circle, -1),
                new RecordedRotate(ChannelId.Circle, -1),
                new RecordedRotate(ChannelId.Star, -1),
                new RecordedRotate(ChannelId.Star, -1),
                new RecordedRotate(ChannelId.Triangle, -1),
                new RecordedRotate(ChannelId.Triangle, -1),
            };
            return new RecordedSolution(path, rotates, 6, System.Array.Empty<RecordedSolution>());
        }

        public static RecordedSolution Solution_3_3()
        {
            var path = new[]
            {
                new GridPosition(0, 3), new GridPosition(0, 2), new GridPosition(1, 2), new GridPosition(1, 2),
                new GridPosition(2, 2), new GridPosition(2, 3), new GridPosition(2, 3), new GridPosition(2, 3),
                new GridPosition(2, 4), new GridPosition(1, 4), new GridPosition(1, 4), new GridPosition(1, 4),
                new GridPosition(2, 4), new GridPosition(3, 4), new GridPosition(3, 4), new GridPosition(3, 4),
                new GridPosition(3, 3), new GridPosition(3, 2), new GridPosition(4, 2), new GridPosition(5, 2),
                new GridPosition(5, 1), new GridPosition(6, 1), new GridPosition(7, 1), new GridPosition(8, 1),
                new GridPosition(9, 1), new GridPosition(9, 2), new GridPosition(9, 3), new GridPosition(9, 4),
                new GridPosition(9, 5), new GridPosition(10, 5), new GridPosition(11, 5), new GridPosition(12, 5),
                new GridPosition(13, 5), new GridPosition(13, 4), new GridPosition(13, 3), new GridPosition(13, 2),
                new GridPosition(14, 2), new GridPosition(15, 2)
            };
            var rotates = new[]
            {
                new RecordedRotate(ChannelId.Circle, 1),
                new RecordedRotate(ChannelId.Triangle, -1),
                new RecordedRotate(ChannelId.Triangle, -1),
                new RecordedRotate(ChannelId.Star, -1),
                new RecordedRotate(ChannelId.Star, -1),
                new RecordedRotate(ChannelId.Diamond, -1),
                new RecordedRotate(ChannelId.Diamond, -1),
            };
            return new RecordedSolution(path, rotates, 7, System.Array.Empty<RecordedSolution>());
        }

        public static RecordedSolution Solution_3_4()
        {
            var path = new[]
            {
                new GridPosition(0, 3), new GridPosition(0, 2), new GridPosition(1, 2), new GridPosition(1, 2),
                new GridPosition(1, 2), new GridPosition(2, 2), new GridPosition(2, 3), new GridPosition(2, 3),
                new GridPosition(2, 3), new GridPosition(2, 4), new GridPosition(1, 4), new GridPosition(1, 4),
                new GridPosition(1, 4), new GridPosition(2, 4), new GridPosition(3, 4), new GridPosition(3, 4),
                new GridPosition(3, 4), new GridPosition(3, 3), new GridPosition(3, 2), new GridPosition(4, 2),
                new GridPosition(5, 2), new GridPosition(5, 1), new GridPosition(6, 1), new GridPosition(7, 1),
                new GridPosition(8, 1), new GridPosition(9, 1), new GridPosition(9, 2), new GridPosition(9, 3),
                new GridPosition(9, 4), new GridPosition(9, 5), new GridPosition(10, 5), new GridPosition(11, 5),
                new GridPosition(12, 5), new GridPosition(13, 5), new GridPosition(13, 4), new GridPosition(13, 3),
                new GridPosition(13, 2), new GridPosition(14, 2), new GridPosition(15, 2), new GridPosition(16, 2),
                new GridPosition(17, 2)
            };
            var rotates = new[]
            {
                new RecordedRotate(ChannelId.Circle, -1),
                new RecordedRotate(ChannelId.Circle, -1),
                new RecordedRotate(ChannelId.Triangle, -1),
                new RecordedRotate(ChannelId.Triangle, -1),
                new RecordedRotate(ChannelId.Star, -1),
                new RecordedRotate(ChannelId.Star, -1),
                new RecordedRotate(ChannelId.Diamond, -1),
                new RecordedRotate(ChannelId.Diamond, -1),
            };
            return new RecordedSolution(path, rotates, 8, System.Array.Empty<RecordedSolution>());
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
    }

    public readonly struct StageBundle
    {
        public StageDefinition Definition { get; }
        public RecordedSolution Solution { get; }

        public StageBundle(StageDefinition definition, RecordedSolution solution)
        {
            Definition = definition;
            Solution = solution;
        }
    }
}
