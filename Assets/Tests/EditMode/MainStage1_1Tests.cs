using System.Collections.Generic;
using NUnit.Framework;
using ShadowGarden.Core;
using ShadowGarden.Infrastructure;

namespace ShadowGarden.Tests.EditMode
{
    public class MainStage1_1Tests
    {
        [Test]
        public void Content_Stage_1_1_Matches_Level_Design()
        {
            var expected = MainStages.Create1_1();
            Assert.AreEqual("1-1", expected.StageId);
            Assert.AreEqual(GridSize.Board12x6, expected.BoardSize);
            Assert.AreEqual(new GridPosition(1, 3), expected.PlayerStart);
            Assert.AreEqual(new GridPosition(11, 3), expected.GoalPosition);
            Assert.AreEqual(ClearGoalType.ExitDoor, expected.ClearGoalType);
            Assert.AreEqual(120, expected.TimeLimitSeconds);
            Assert.AreEqual(1, expected.Lamps.Count);
            Assert.AreEqual(new GridPosition(2, 3), expected.Lamps[0].Position);
            Assert.AreEqual(CardinalDirection.North, expected.Lamps[0].InitialDirection);
            Assert.AreEqual(new GridPosition(3, 2), expected.Pillars[0].Position);
            Assert.AreEqual(PillarHeight.Medium, expected.Pillars[0].Height);
            Assert.IsFalse(expected.IsAlwaysSafe(new GridPosition(5, 3)));
        }

        [Test]
        public void Stage_1_1_Safe_Solution_Clears()
        {
            var stage = MainStages.Create1_1();
            var session = new ShadowGarden.Runtime.StageSession(stage);

            // Walk to lamp
            Assert.AreEqual(MoveOutcome.Moved, session.Move(CardinalDirection.East).Move.Value.Outcome);
            // Rotate North -> East
            var rotate = session.Rotate(1);
            Assert.IsTrue(System.Array.Exists(rotate.Events, e => e.Type == StageEventType.LampRotated));

            var path = MainStages.Stage1_1SolutionPathAfterEastRotate();
            // path[0]=start, path[1]=lamp already occupied after first move
            for (var i = 2; i < path.Count; i++)
            {
                var from = session.State.PlayerPosition;
                var to = path[i];
                var dx = to.X - from.X;
                var dy = to.Y - from.Y;
                CardinalDirection dir;
                if (dx == 1 && dy == 0) dir = CardinalDirection.East;
                else if (dx == -1 && dy == 0) dir = CardinalDirection.West;
                else if (dx == 0 && dy == 1) dir = CardinalDirection.South;
                else if (dx == 0 && dy == -1) dir = CardinalDirection.North;
                else
                {
                    Assert.Fail($"Non-adjacent step {from} -> {to}");
                    return;
                }

                var result = session.Move(dir);
                Assert.IsTrue(result.Move.HasValue, $"move {from}->{to}");
                if (i < path.Count - 1)
                {
                    Assert.AreEqual(MoveOutcome.Moved, result.Move.Value.Outcome, $"{from}->{to}");
                }
                else
                {
                    Assert.AreEqual(MoveOutcome.ExitReached, result.Move.Value.Outcome);
                    Assert.IsTrue(System.Array.Exists(result.Events, e => e.Type == StageEventType.ClearStarted));
                    Assert.AreEqual(StagePhase.ResolvingClear, session.State.Phase);
                    // Timer locked on clear
                    var before = session.State.RemainingMilliseconds;
                    session.Tick(1000);
                    Assert.AreEqual(before, session.State.RemainingMilliseconds);
                }
            }
        }

        [Test]
        public void Wrong_Rotation_Keeps_Gap_As_Cliff()
        {
            var stage = MainStages.Create1_1();
            var session = new ShadowGarden.Runtime.StageSession(stage);
            session.Move(CardinalDirection.East);
            session.Rotate(-1); // West — gap stays cliff
            session.Move(CardinalDirection.East); // 3,3
            session.Move(CardinalDirection.East); // 4,3
            var cliff = session.Move(CardinalDirection.East); // 5,3
            Assert.AreEqual(MoveOutcome.CliffDeath, cliff.Move.Value.Outcome);
            Assert.IsTrue(System.Array.Exists(cliff.Events, e => e.Type == StageEventType.GameOverStarted));
        }

        [Test]
        public void Goal_Near_Zero_Ms_Clears_And_Locks_Timer()
        {
            var stage = MainStages.Create1_1();
            var session = new ShadowGarden.Runtime.StageSession(stage);
            session.Move(CardinalDirection.East);
            session.Rotate(1);
            session.Tick(stage.TimeLimitSeconds * 1000L - 1);
            Assert.AreEqual(1, session.State.RemainingMilliseconds);
            Assert.AreEqual(StagePhase.Playing, session.State.Phase);

            var path = MainStages.Stage1_1SolutionPathAfterEastRotate();
            for (var i = 2; i < path.Count; i++)
            {
                var from = session.State.PlayerPosition;
                var to = path[i];
                var dx = to.X - from.X;
                var dy = to.Y - from.Y;
                var dir = dx == 1 ? CardinalDirection.East
                    : dx == -1 ? CardinalDirection.West
                    : dy == 1 ? CardinalDirection.South
                    : CardinalDirection.North;
                var move = session.Move(dir);
                if (i == path.Count - 1)
                {
                    Assert.AreEqual(MoveOutcome.ExitReached, move.Move.Value.Outcome);
                    Assert.AreEqual(StagePhase.ResolvingClear, session.State.Phase);
                    var locked = session.State.RemainingMilliseconds;
                    session.Tick(5000);
                    Assert.AreEqual(locked, session.State.RemainingMilliseconds);
                }
            }
        }

        [Test]
        public void Time_Expiry_Locks_Timer()
        {
            var stage = MainStages.Create1_1();
            var session = new ShadowGarden.Runtime.StageSession(stage);
            var result = session.Tick(stage.TimeLimitSeconds * 1000L + 50);
            Assert.IsTrue(System.Array.Exists(result.Events, e => e.Type == StageEventType.GameOverStarted));
            var remaining = session.State.RemainingMilliseconds;
            session.Tick(500);
            Assert.AreEqual(remaining, session.State.RemainingMilliseconds);
        }

        [Test]
        public void Focus_Loss_Pauses_Timer()
        {
            var stage = MainStages.Create1_1();
            var session = new ShadowGarden.Runtime.StageSession(stage);
            session.SetFocus(false);
            var before = session.State.RemainingMilliseconds;
            session.Tick(2000);
            Assert.AreEqual(before, session.State.RemainingMilliseconds);
            session.SetFocus(true);
            session.Tick(2000);
            Assert.Less(session.State.RemainingMilliseconds, before);
        }

        [Test]
        public void Presentation_Timing_Constants()
        {
            Assert.AreEqual(0.12f, ShadowGarden.Presentation.PresentationTiming.MoveSeconds, 0.0001f);
            Assert.AreEqual(0.18f, ShadowGarden.Presentation.PresentationTiming.RotateSeconds, 0.0001f);
            Assert.AreEqual(0.45f, ShadowGarden.Presentation.PresentationTiming.DoorOpenSeconds, 0.0001f);
            Assert.AreEqual(0.35f, ShadowGarden.Presentation.PresentationTiming.GoalPassSeconds, 0.0001f);
        }

        [Test]
        public void Content_Asset_Path_Is_Not_TestField_Stages()
        {
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<StageDefinitionAsset>(
                "Assets/Content/Stages/World01/Stage_1_1.asset");
            Assert.IsNotNull(asset);
            Assert.AreEqual("1-1", asset.stageId);
            var prototype = UnityEditor.AssetDatabase.LoadAssetAtPath<StageDefinitionAsset>(
                "Assets/Stages/Stage_1_1.asset");
            Assert.IsNotNull(prototype);
            Assert.AreNotSame(asset, prototype);

            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<StageCatalogAsset>(
                "Assets/Stages/StageCatalog.asset");
            Assert.IsNotNull(catalog);
            Assert.AreEqual(asset, catalog.GetAt(0));
#endif
        }
    }
}
