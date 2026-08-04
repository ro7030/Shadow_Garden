using NUnit.Framework;
using ShadowGarden.Core;

namespace ShadowGarden.Tests.EditMode
{
    public class DirectionUtilityTests
    {
        [Test]
        public void Four_Clockwise_Rotations_Return_To_Start()
        {
            var direction = CardinalDirection.North;
            for (var i = 0; i < 4; i++)
            {
                direction = DirectionUtility.RotateClockwise(direction);
            }

            Assert.AreEqual(CardinalDirection.North, direction);
        }
    }

    public class ShadowGridSolverTests
    {
        [Test]
        public void Pillar_Heights_Project_2_3_4_Cells()
        {
            var stage = new StageDefinition(
                "height",
                GridSize.Board12x6,
                new GridPosition(0, 0),
                new[] { new GridPosition(0, 0), new GridPosition(0, 2), new GridPosition(0, 4) },
                new[]
                {
                    new LampDefinition(new GridPosition(0, 0), ChannelId.Circle, CardinalDirection.East),
                    new LampDefinition(new GridPosition(0, 2), ChannelId.Triangle, CardinalDirection.East),
                    new LampDefinition(new GridPosition(0, 4), ChannelId.Star, CardinalDirection.East)
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(1, 0), ChannelId.Circle, PillarHeight.Low),
                    new PillarDefinition(new GridPosition(1, 2), ChannelId.Triangle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(1, 4), ChannelId.Star, PillarHeight.High)
                },
                ClearGoalType.ExitDoor,
                new GridPosition(11, 0),
                120);

            var result = ShadowGridSolver.Calculate(stage, stage.CreateInitialRuntimeState().DirectionByChannel);

            Assert.AreEqual(1, result.GetShadowCount(new GridPosition(2, 0)));
            Assert.AreEqual(1, result.GetShadowCount(new GridPosition(3, 0)));
            Assert.AreEqual(0, result.GetShadowCount(new GridPosition(4, 0)));

            Assert.AreEqual(1, result.GetShadowCount(new GridPosition(4, 2)));
            Assert.AreEqual(0, result.GetShadowCount(new GridPosition(5, 2)));

            Assert.AreEqual(1, result.GetShadowCount(new GridPosition(5, 4)));
            Assert.AreEqual(0, result.GetShadowCount(new GridPosition(6, 4)));
        }

        [Test]
        public void Projection_Stops_At_Board_Edge_And_Other_Pillar()
        {
            var stage = new StageDefinition(
                "stop",
                GridSize.Board12x6,
                new GridPosition(0, 1),
                new[] { new GridPosition(0, 1) },
                new[] { new LampDefinition(new GridPosition(0, 1), ChannelId.Circle, CardinalDirection.East) },
                new[]
                {
                    new PillarDefinition(new GridPosition(10, 1), ChannelId.Circle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(2, 3), ChannelId.Circle, PillarHeight.High),
                    new PillarDefinition(new GridPosition(5, 3), ChannelId.Circle, PillarHeight.Low)
                },
                ClearGoalType.ExitDoor,
                new GridPosition(0, 0),
                120);

            var result = ShadowGridSolver.Calculate(stage, stage.CreateInitialRuntimeState().DirectionByChannel);

            Assert.AreEqual(1, result.GetShadowCount(new GridPosition(11, 1)));
            Assert.AreEqual(1, result.GetShadowCount(new GridPosition(3, 3)));
            Assert.AreEqual(1, result.GetShadowCount(new GridPosition(4, 3)));
            Assert.AreEqual(0, result.GetShadowCount(new GridPosition(5, 3)));
        }
    }

    public class MoveResolverTests
    {
        [Test]
        public void Empty_Cells_Branch_By_ShadowCount()
        {
            var stage = BuildOverlapStage();
            var state = stage.CreateInitialRuntimeState().WithPhase(StagePhase.Playing);
            var shadows = ShadowGridSolver.Calculate(stage, state.DirectionByChannel);

            Assert.AreEqual(0, shadows.GetShadowCount(new GridPosition(1, 2)));
            Assert.AreEqual(1, shadows.GetShadowCount(new GridPosition(4, 2)));
            Assert.AreEqual(2, shadows.GetShadowCount(new GridPosition(5, 2)));

            var cliff = MoveResolver.ResolveMove(stage, state, shadows, CardinalDirection.East);
            Assert.AreEqual(MoveOutcome.CliffDeath, cliff.Outcome);

            state = state.WithPlayer(new GridPosition(3, 2), StagePhase.Playing);
            Assert.AreEqual(MoveOutcome.Moved, MoveResolver.ResolveMove(stage, state, shadows, CardinalDirection.East).Outcome);

            state = state.WithPlayer(new GridPosition(4, 2), StagePhase.Playing);
            Assert.AreEqual(MoveOutcome.OverlapDeath, MoveResolver.ResolveMove(stage, state, shadows, CardinalDirection.East).Outcome);
        }

        [Test]
        public void Safe_Cells_Remain_Safe_With_Overlap_Count()
        {
            var baseStage = BuildOverlapStage();
            var stage = new StageDefinition(
                baseStage.StageId,
                baseStage.BoardSize,
                baseStage.PlayerStart,
                new[] { new GridPosition(0, 2), new GridPosition(5, 2) },
                baseStage.Lamps,
                baseStage.Pillars,
                baseStage.ClearGoalType,
                baseStage.GoalPosition,
                baseStage.TimeLimitSeconds);

            var state = stage.CreateInitialRuntimeState().WithPlayer(new GridPosition(4, 2), StagePhase.Playing);
            var shadows = ShadowGridSolver.Calculate(stage, state.DirectionByChannel);
            Assert.GreaterOrEqual(shadows.GetShadowCount(new GridPosition(5, 2)), 2);
            Assert.AreEqual(MoveOutcome.Moved, MoveResolver.ResolveMove(stage, state, shadows, CardinalDirection.East).Outcome);
        }

        [Test]
        public void Pillar_And_OutOfBounds_Are_Blocked()
        {
            var stage = GrayboxStages.Create1_1();
            var state = stage.CreateInitialRuntimeState().WithPlayer(new GridPosition(2, 2), StagePhase.Playing);
            var shadows = ShadowGridSolver.Calculate(stage, state.DirectionByChannel);

            Assert.AreEqual(MoveOutcome.Blocked, MoveResolver.ResolveMove(stage, state, shadows, CardinalDirection.East).Outcome);

            state = state.WithPlayer(new GridPosition(0, 2), StagePhase.Playing);
            Assert.AreEqual(MoveOutcome.Blocked, MoveResolver.ResolveMove(stage, state, shadows, CardinalDirection.West).Outcome);
        }

        private static StageDefinition BuildOverlapStage()
        {
            // Circle (2,2) East Med -> 3,4,5 on y=2
            // Triangle (5,0) South Med -> 5,1 5,2 5,3 -> overlap at 5,2
            return new StageDefinition(
                "overlap",
                GridSize.Board12x6,
                new GridPosition(0, 2),
                new[] { new GridPosition(0, 2) },
                new[]
                {
                    new LampDefinition(new GridPosition(0, 0), ChannelId.Circle, CardinalDirection.East),
                    new LampDefinition(new GridPosition(0, 5), ChannelId.Triangle, CardinalDirection.South)
                },
                new[]
                {
                    new PillarDefinition(new GridPosition(2, 2), ChannelId.Circle, PillarHeight.Medium),
                    new PillarDefinition(new GridPosition(5, 0), ChannelId.Triangle, PillarHeight.Medium)
                },
                ClearGoalType.ExitDoor,
                new GridPosition(8, 5),
                120);
        }
    }

    public class StageTimerTests
    {
        [Test]
        public void Focus_Loss_Pauses_Timer()
        {
            var stage = GrayboxStages.Create1_1();
            var state = StageCommands.Start(stage).NextState;
            var paused = StageCommands.SetFocus(stage, state, false).NextState;
            Assert.AreEqual(TimerPauseReason.FocusLost, paused.PauseReason);

            var tick = StageCommands.TickTimer(stage, paused, 1000).NextState;
            Assert.AreEqual(paused.RemainingMilliseconds, tick.RemainingMilliseconds);
        }

        [Test]
        public void Cleared_State_Ignores_Timer_Expiry()
        {
            var stage = GrayboxStages.Create1_1();
            var cleared = StageTimer.Stop(
                stage.CreateInitialRuntimeState()
                    .WithPlayer(stage.GoalPosition, StagePhase.ResolvingClear)
                    .WithTimer(0, true, true, TimerPauseReason.None));

            var tick = StageTimer.Tick(cleared, 1000);
            Assert.IsFalse(tick.Expired);
            Assert.AreEqual(StagePhase.ResolvingClear, tick.NextState.Phase);
        }

        [Test]
        public void Warnings_Fire_Once_At_Thresholds()
        {
            var stage = GrayboxStages.Create1_1();
            var state = StageCommands.Start(stage).NextState;
            var to30 = StageCommands.TickTimer(stage, state, stage.TimeLimitMilliseconds - 30_000);
            Assert.IsTrue(HasEvent(to30, StageEventType.TimerWarning30));

            var again = StageCommands.TickTimer(stage, to30.NextState, 1000);
            Assert.IsFalse(HasEvent(again, StageEventType.TimerWarning30));

            var to10 = StageCommands.TickTimer(
                stage,
                again.NextState,
                again.NextState.RemainingMilliseconds - 10_000);
            Assert.IsTrue(HasEvent(to10, StageEventType.TimerWarning10));
        }

        private static bool HasEvent(StageCommandResult result, StageEventType type)
        {
            foreach (var e in result.Events)
            {
                if (e.Type == type)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class SupportedBoardSizeTests
    {
        [TestCase(12, 6)]
        [TestCase(14, 7)]
        [TestCase(16, 7)]
        [TestCase(16, 8)]
        [TestCase(18, 8)]
        public void Supported_Sizes_Are_Accepted(int width, int height)
        {
            Assert.IsTrue(GridSize.IsSupported(width, height));
        }

        [TestCase(13, 6)]
        [TestCase(18, 9)]
        [TestCase(0, 6)]
        public void Unsupported_Sizes_Are_Rejected(int width, int height)
        {
            Assert.IsFalse(GridSize.IsSupported(width, height));
            var stage = new StageDefinition(
                "bad",
                new GridSize(width, height),
                new GridPosition(0, 0),
                new[] { new GridPosition(0, 0) },
                new[] { new LampDefinition(new GridPosition(0, 0), ChannelId.Circle, CardinalDirection.East) },
                System.Array.Empty<PillarDefinition>(),
                ClearGoalType.ExitDoor,
                new GridPosition(1, 0),
                120);
            Assert.IsNotEmpty(StageValidator.Validate(stage));
        }

        [Test]
        public void PathFinder_MaxStates_For_3_4_Is_36864()
        {
            var stage = GrayboxStages.Create3_4();
            Assert.AreEqual(18, stage.BoardSize.Width);
            Assert.AreEqual(8, stage.BoardSize.Height);
            Assert.AreEqual(4, stage.Lamps.Count);
            Assert.AreEqual(36864, SafetyPathFinder.MaxStatesFor(stage));
        }
    }

    public class GrayboxStageTests
    {
        [TestCase("1-1")]
        [TestCase("1-4")]
        [TestCase("2-2")]
        [TestCase("3-4")]
        public void Canonical_Stages_Are_Valid(string id)
        {
            var issues = StageValidator.Validate(Get(id));
            Assert.IsEmpty(issues, string.Join(" | ", issues));
        }

        [TestCase("1-1")]
        [TestCase("1-4")]
        [TestCase("2-2")]
        [TestCase("3-4")]
        public void Canonical_Stages_Have_Safe_Solution(string id)
        {
            Assert.IsTrue(SafetyPathFinder.HasSafeSolution(Get(id)), id);
        }

        [Test]
        public void Stage_1_1_Initial_Shadows_Match_Design()
        {
            var stage = GrayboxStages.Create1_1();
            var shadows = StageCommands.CurrentShadows(stage, stage.CreateInitialRuntimeState());
            Assert.AreEqual(1, shadows.GetShadowCount(new GridPosition(3, 0)));
            Assert.AreEqual(1, shadows.GetShadowCount(new GridPosition(3, 1)));
            Assert.AreEqual(0, shadows.GetShadowCount(new GridPosition(5, 2)));
        }

        [Test]
        public void Stage_1_1_One_Rotation_Opens_East_Bridge()
        {
            var stage = GrayboxStages.Create1_1();
            var state = StageCommands.Start(stage).NextState
                .WithPlayer(new GridPosition(2, 3), StagePhase.Playing);
            state = StageCommands.TryRotate(stage, state, 1).NextState;
            var shadows = StageCommands.CurrentShadows(stage, state);

            Assert.AreEqual(CardinalDirection.East, state.GetDirection(ChannelId.Circle));
            Assert.AreEqual(1, shadows.GetShadowCount(new GridPosition(4, 2)));
            Assert.AreEqual(1, shadows.GetShadowCount(new GridPosition(5, 2)));
            Assert.AreEqual(1, shadows.GetShadowCount(new GridPosition(6, 2)));
            Assert.AreEqual(0, shadows.GetShadowCount(new GridPosition(7, 2)));
        }

        [Test]
        public void Stage_1_4_Initial_Single_Shadows_Match_Design()
        {
            var stage = GrayboxStages.Create1_4();
            var shadows = StageCommands.CurrentShadows(stage, stage.CreateInitialRuntimeState());
            Assert.AreEqual(1, shadows.GetShadowCount(new GridPosition(0, 0)));
            Assert.AreEqual(1, shadows.GetShadowCount(new GridPosition(2, 0)));
            Assert.AreEqual(1, shadows.GetShadowCount(new GridPosition(11, 2)));
            Assert.AreEqual(0, shadows.GetShadowCount(new GridPosition(5, 2)));
        }

        [Test]
        public void Stage_3_4_Initial_Overlap_At_7_7()
        {
            var stage = GrayboxStages.Create3_4();
            Assert.AreEqual(GridSize.Board18x8, stage.BoardSize);
            var shadows = StageCommands.CurrentShadows(stage, stage.CreateInitialRuntimeState());
            Assert.AreEqual(2, shadows.GetShadowCount(new GridPosition(7, 7)));
            Assert.AreEqual(CellKind.OverlapHazard, CellClassifier.Classify(stage, shadows, new GridPosition(7, 7)));
        }

        [Test]
        public void Restart_Restores_Initial_State()
        {
            var stage = GrayboxStages.Create1_1();
            var started = StageCommands.Start(stage);
            var onLamp = started.NextState.WithPlayer(new GridPosition(2, 3), StagePhase.Playing);
            var rotated = StageCommands.TryRotate(stage, onLamp, 1);
            var restarted = StageCommands.Restart(stage);

            Assert.AreEqual(started.NextState.PlayerPosition, restarted.NextState.PlayerPosition);
            Assert.AreEqual(
                started.NextState.GetDirection(ChannelId.Circle),
                restarted.NextState.GetDirection(ChannelId.Circle));
            Assert.AreEqual(stage.TimeLimitMilliseconds, restarted.NextState.RemainingMilliseconds);
            Assert.AreNotEqual(
                rotated.NextState.GetDirection(ChannelId.Circle),
                restarted.NextState.GetDirection(ChannelId.Circle));
        }

        private static StageDefinition Get(string id) => id switch
        {
            "1-1" => GrayboxStages.Create1_1(),
            "1-4" => GrayboxStages.Create1_4(),
            "2-2" => GrayboxStages.Create2_2(),
            "3-4" => GrayboxStages.Create3_4(),
            _ => throw new AssertionException("Unknown stage")
        };
    }
}
