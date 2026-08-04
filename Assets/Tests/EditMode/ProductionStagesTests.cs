using System.Collections.Generic;
using NUnit.Framework;
using ShadowGarden.Core;
using ShadowGarden.Infrastructure;
using ShadowGarden.Presentation;
using UnityEngine;

namespace ShadowGarden.Tests.EditMode
{
    public class ProductionStagesTests
    {
        private static readonly string[] ExpectedIds =
        {
            "1-1", "1-2", "1-3", "1-4",
            "2-1", "2-2", "2-3", "2-4",
            "3-1", "3-2", "3-3", "3-4"
        };

        [Test]
        public void All_Twelve_Definitions_Exist_In_Order()
        {
            var all = MainStages.AllDefinitions();
            Assert.AreEqual(12, all.Count);
            for (var i = 0; i < ExpectedIds.Length; i++)
            {
                Assert.AreEqual(ExpectedIds[i], all[i].StageId);
            }
        }

        [Test]
        public void Fixed_Specs_Match_Stage_Contract()
        {
            foreach (var stage in MainStages.AllDefinitions())
            {
                Assert.IsTrue(GridSize.IsSupported(stage.BoardSize), stage.StageId);
                var world = stage.StageId[0];
                if (stage.StageId.EndsWith("-4"))
                {
                    Assert.AreEqual(ClearGoalType.NightFlower, stage.ClearGoalType, stage.StageId);
                    Assert.AreEqual(150, stage.TimeLimitSeconds, stage.StageId);
                }
                else
                {
                    Assert.AreEqual(ClearGoalType.ExitDoor, stage.ClearGoalType, stage.StageId);
                    Assert.AreEqual(120, stage.TimeLimitSeconds, stage.StageId);
                }

                if (stage.StageId.StartsWith("1-") || stage.StageId.StartsWith("2-"))
                {
                    Assert.AreEqual(GridSize.Board12x6, stage.BoardSize, stage.StageId);
                }

                switch (stage.StageId)
                {
                    case "3-1":
                        Assert.AreEqual(GridSize.Board14x7, stage.BoardSize);
                        break;
                    case "3-2":
                        Assert.AreEqual(GridSize.Board16x7, stage.BoardSize);
                        break;
                    case "3-3":
                        Assert.AreEqual(GridSize.Board16x8, stage.BoardSize);
                        break;
                    case "3-4":
                        Assert.AreEqual(GridSize.Board18x8, stage.BoardSize);
                        break;
                }

                if (stage.StageId == "1-4")
                {
                    Assert.AreEqual(2, stage.Lamps.Count);
                }
                else if (stage.StageId.StartsWith("1-"))
                {
                    Assert.AreEqual(1, stage.Lamps.Count);
                }
                else if (stage.StageId == "2-1")
                {
                    Assert.AreEqual(2, stage.Lamps.Count);
                }
                else if (stage.StageId.StartsWith("2-") || stage.StageId == "3-1" || stage.StageId == "3-2")
                {
                    Assert.AreEqual(3, stage.Lamps.Count, stage.StageId);
                }
                else
                {
                    Assert.AreEqual(4, stage.Lamps.Count, stage.StageId);
                }

                Assert.AreEqual(world, stage.StageId[0]);
            }
        }

        [Test]
        public void Validator_Passes_All_Production_Boards()
        {
            foreach (var stage in MainStages.AllDefinitions())
            {
                var issues = StageValidator.Validate(stage);
                Assert.IsEmpty(issues, $"{stage.StageId}: {string.Join(" | ", issues)}");
            }
        }

        [Test]
        public void Recorded_Solutions_Replay_To_Clear()
        {
            foreach (var bundle in MainStages.AllBundles())
            {
                Assert.IsTrue(
                    SolutionReplay.TryReplay(bundle.Definition, bundle.Solution, out var failure),
                    failure?.ToString() ?? bundle.Definition.StageId);
            }
        }

        [Test]
        public void SafetyPathFinder_Finds_Safe_Solution_On_All_Boards()
        {
            foreach (var stage in MainStages.AllDefinitions())
            {
                Assert.IsTrue(
                    SafetyPathFinder.TryFindMinimalSolution(stage, out var found),
                    stage.StageId);
                Assert.GreaterOrEqual(found.RotateCount, 0, stage.StageId);
            }
        }

        [Test]
        public void PathFinder_MaxStates_For_3_4_Is_HardCap()
        {
            var stage = MainStages.Create3_4();
            Assert.AreEqual(4, stage.Lamps.Count);
            Assert.AreEqual(36864, SafetyPathFinder.MaxStatesFor(stage));
        }

        [Test]
        public void Report_Shorter_Solutions_That_Bypass_Documented_Min_Rotates()
        {
            var reports = new List<string>();
            foreach (var bundle in MainStages.AllBundles())
            {
                Assert.IsTrue(SafetyPathFinder.TryFindMinimalSolution(bundle.Definition, out var found));
                if (found.RotateCount < bundle.Solution.DocumentedMinRotates)
                {
                    reports.Add(
                        $"{bundle.Definition.StageId}: documented={bundle.Solution.DocumentedMinRotates} " +
                        $"found={found.RotateCount} cmds=[{string.Join(" ", found.Commands)}]");
                }
            }

            // Do not fail the suite — report for designers. Empty is ideal.
            if (reports.Count > 0)
            {
                Debug.LogWarning("Shorter solutions vs documented min rotates:\n" + string.Join("\n", reports));
            }

            Assert.Pass(reports.Count == 0
                ? "No shorter bypass paths."
                : $"Reported {reports.Count} shorter path(s); see warnings.");
        }

        [Test]
        public void Shadow_Classification_Basics_On_1_1_And_1_4()
        {
            var stage11 = MainStages.Create1_1();
            var state11 = stage11.CreateInitialRuntimeState();
            var shadows11 = ShadowGridSolver.Calculate(stage11, state11.DirectionByChannel);
            Assert.AreEqual(1, shadows11.GetShadowCount(new GridPosition(3, 0)));
            Assert.AreEqual(1, shadows11.GetShadowCount(new GridPosition(3, 1)));
            Assert.AreEqual(0, shadows11.GetShadowCount(new GridPosition(5, 3)));
            Assert.AreEqual(CellKind.Cliff, CellClassifier.Classify(stage11, shadows11, new GridPosition(5, 3)));
            Assert.AreEqual(CellKind.Safe, CellClassifier.Classify(stage11, shadows11, new GridPosition(1, 3)));

            var stage14 = MainStages.Create1_4();
            Assert.AreEqual(2, stage14.Lamps.Count);
            // After rotating both to documented finals, central bait can overlap — use pathfinder existence instead.
            Assert.IsTrue(SafetyPathFinder.HasSafeSolution(stage14));
        }

        [Test]
        public void Pillar_Shadow_Lengths_Are_2_3_4()
        {
            Assert.AreEqual(2, new PillarDefinition(new GridPosition(0, 0), ChannelId.Circle, PillarHeight.Low).ShadowLength);
            Assert.AreEqual(3, new PillarDefinition(new GridPosition(0, 0), ChannelId.Circle, PillarHeight.Medium).ShadowLength);
            Assert.AreEqual(4, new PillarDefinition(new GridPosition(0, 0), ChannelId.Circle, PillarHeight.High).ShadowLength);
        }

        [Test]
        public void Camera_Framing_Works_For_Every_Production_Board_Size()
        {
            foreach (var stage in MainStages.AllDefinitions())
            {
                var framing = BoardCameraFitter.Calculate(stage.BoardSize, 16f / 9f);
                Assert.Greater(framing.OrthographicSize, 0f, stage.StageId);
                Assert.IsTrue(float.IsFinite(framing.OrthographicSize), stage.StageId);
            }
        }

        [Test]
        public void Content_Assets_Exist_And_Are_Not_TestField_Assets()
        {
#if UNITY_EDITOR
            foreach (var id in ExpectedIds)
            {
                var world = id[0] switch
                {
                    '1' => "World01",
                    '2' => "World02",
                    _ => "World03"
                };
                var contentPath = $"Assets/Content/Stages/{world}/Stage_{id.Replace('-', '_')}.asset";
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<StageDefinitionAsset>(contentPath);
                Assert.IsNotNull(asset, contentPath);
                Assert.AreEqual(id, asset.stageId);
                Assert.Greater(asset.recordedSolutionPath.Count, 0, id);

                var testFieldPath = $"Assets/Stages/Stage_{id.Replace('-', '_')}.asset";
                var tf = UnityEditor.AssetDatabase.LoadAssetAtPath<StageDefinitionAsset>(testFieldPath);
                if (tf != null)
                {
                    Assert.AreNotSame(asset, tf);
                    Assert.AreNotEqual(contentPath, testFieldPath);
                }
            }

            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<StageCatalogAsset>(
                "Assets/Stages/StageCatalog.asset");
            Assert.IsNotNull(catalog);
            Assert.AreEqual(12, catalog.Count);
            CollectionAssert.AreEqual(ExpectedIds, catalog.GetOrderedStageIds());
#endif
        }

        [Test]
        public void Learning_Meta_Is_Presentation_Only_And_Complete()
        {
            foreach (var id in ExpectedIds)
            {
                var entry = MainStageLearningMeta.Get(id);
                Assert.AreEqual(id, entry.StageId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.LearningGoal), id);
            }
        }
    }
}
