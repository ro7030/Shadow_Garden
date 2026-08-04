using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ShadowGarden.Core;
using ShadowGarden.Infrastructure;
using ShadowGarden.Presentation;
using ShadowGarden.Runtime;
using UnityEngine;

namespace ShadowGarden.Tests.EditMode
{
    public class WorldProgressStabilityTests
    {
        private static readonly string[] AllStageIds =
        {
            "1-1", "1-2", "1-3", "1-4",
            "2-1", "2-2", "2-3", "2-4",
            "3-1", "3-2", "3-3", "3-4"
        };

        [Test]
        public void Force_Clear_1_1_Through_3_4_Unlocks_Linearly_And_Ends()
        {
            var catalog = CreateTwelveStageCatalog();
            var repo = new MemoryProgressSaveRepository();
            var save = new SaveService(repo, new MemoryUiPreferencesRepository());
            save.LoadAll();

            try
            {
                long elapsed = 60_000L;
                for (var i = 0; i < AllStageIds.Length; i++)
                {
                    var id = AllStageIds[i];
                    Assert.IsTrue(catalog.IsUnlocked(id, save.Progress), "should unlock " + id);
                    save.RecordStageCleared(id, elapsed - i * 1000L);
                    Assert.Contains(id, save.Progress.completedStageIds);
                    Assert.AreEqual(id, save.Progress.lastStageId);
                }

                Assert.AreEqual(12, save.Progress.completedStageIds.Count);
                Assert.IsTrue(catalog.IsUnlocked("3-4", save.Progress));

                var vm = WorldMapViewModel.Build(catalog, save.Progress, "3-4");
                Assert.AreEqual(3, vm.Worlds.Count);
                Assert.AreEqual(12, vm.FlatNodes.Count);
                Assert.IsTrue(vm.Worlds[2].Unlocked);
                Assert.AreEqual("꽃", vm.FlatNodes[11].CompletionIcon);
                Assert.AreEqual("문", vm.FlatNodes[0].CompletionIcon);

                var modal = ModalViewModel.CreateCleared(hasNextStage: true, isFinalStage: true);
                Assert.AreEqual("ending", modal.Selected.Id);
                Assert.AreEqual(0, modal.SelectedIndex);
            }
            finally
            {
                DestroyCatalog(catalog);
            }
        }

        [Test]
        public void App_Restart_Restores_Progress_Without_Reset()
        {
            var repo = new MemoryProgressSaveRepository();
            var prefs = new MemoryUiPreferencesRepository();
            var session1 = new SaveService(repo, prefs);
            session1.LoadAll();
            session1.MarkOpeningSeen();
            session1.RecordStageCleared("1-1", 12_500);
            session1.RecordStageCleared("1-2", 20_000);

            var session2 = new SaveService(repo, prefs);
            session2.LoadAll();
            Assert.IsTrue(session2.CanContinue());
            CollectionAssert.AreEquivalent(new[] { "1-1", "1-2" }, session2.Progress.completedStageIds);
            Assert.AreEqual("1-2", session2.Progress.lastStageId);
            Assert.AreEqual(12_500, session2.TryGetBestClearMilliseconds("1-1"));
        }

        [Test]
        public void Repeated_Sessions_Accumulate_Without_Wipe()
        {
            var repo = new MemoryProgressSaveRepository();
            var prefs = new MemoryUiPreferencesRepository();
            for (var session = 0; session < 5; session++)
            {
                var save = new SaveService(repo, prefs);
                save.LoadAll();
                save.RecordStageCleared(AllStageIds[session], 30_000 - session * 100);
            }

            var final = new SaveService(repo, prefs);
            final.LoadAll();
            Assert.AreEqual(5, final.Progress.completedStageIds.Count);
            Assert.AreEqual(AllStageIds[4], final.Progress.lastStageId);
        }

        [Test]
        public void Missing_Best_Time_Shows_Incomplete_Placeholder()
        {
            var catalog = CreateTwelveStageCatalog();
            try
            {
                var progress = SaveData.CreateDefault();
                progress.completedStageIds.Add("1-1");
                progress.lastStageId = "1-1";
                var vm = WorldMapViewModel.Build(catalog, progress, "1-1");
                Assert.AreEqual(ProgressTimeFormat.Incomplete, vm.FlatNodes[0].TimeLabel);
                Assert.AreEqual("문", vm.FlatNodes[0].CompletionIcon);
            }
            finally
            {
                DestroyCatalog(catalog);
            }
        }

        [Test]
        public void Unknown_StageId_Does_Not_Crash_Or_Unlock_Ghost_Nodes()
        {
            var catalog = CreateTwelveStageCatalog();
            var save = new SaveService(new MemoryProgressSaveRepository(), new MemoryUiPreferencesRepository());
            save.LoadAll();
            try
            {
                save.RecordStageCleared("ghost-99", 1000);
                Assert.IsFalse(catalog.IsUnlocked("ghost-99", save.Progress));
                Assert.IsFalse(catalog.IsUnlocked("9-9", save.Progress));
                var vm = WorldMapViewModel.Build(catalog, save.Progress, "ghost-99");
                Assert.AreEqual("1-1", vm.FocusedStageId);
                Assert.DoesNotThrow(() => save.RecordStageFailed("nope"));
            }
            finally
            {
                DestroyCatalog(catalog);
            }
        }

        [Test]
        public void Slower_Clear_Does_Not_Overwrite_Best_Time()
        {
            var save = new SaveService(new MemoryProgressSaveRepository(), new MemoryUiPreferencesRepository());
            save.LoadAll();
            save.RecordStageCleared("1-1", 8_000);
            save.RecordStageCleared("1-1", 15_000);
            Assert.AreEqual(8_000, save.TryGetBestClearMilliseconds("1-1"));
            Assert.AreEqual(1, save.Progress.completedStageIds.Count);
        }

        [Test]
        public void Faster_Clear_Updates_Best_Time()
        {
            var save = new SaveService(new MemoryProgressSaveRepository(), new MemoryUiPreferencesRepository());
            save.LoadAll();
            save.RecordStageCleared("1-1", 15_000);
            save.RecordStageCleared("1-1", 7_250);
            Assert.AreEqual(7_250, save.TryGetBestClearMilliseconds("1-1"));
            Assert.AreEqual("0:07.2", ProgressTimeFormat.FormatBestClear(save.Progress, "1-1"));
        }

        [Test]
        public void Fail_Then_Level_Select_Restores_Failed_Node_Focus()
        {
            var catalog = CreateTwelveStageCatalog();
            var save = new SaveService(new MemoryProgressSaveRepository(), new MemoryUiPreferencesRepository());
            save.LoadAll();
            save.RecordStageCleared("1-1", 10_000);
            save.RecordStageSelected("1-2");
            save.RecordStageFailed("1-2");
            try
            {
                Assert.AreEqual("1-2", save.Progress.lastStageId);
                var vm = WorldMapViewModel.Build(catalog, save.Progress);
                Assert.AreEqual("1-2", vm.FocusedStageId);
                Assert.IsTrue(vm.FindFocused().IsFocused);
            }
            finally
            {
                DestroyCatalog(catalog);
            }
        }

        [Test]
        public void Clear_Resolves_Next_Stage_And_World_Unlock()
        {
            var catalog = CreateTwelveStageCatalog();
            var save = new SaveService(new MemoryProgressSaveRepository(), new MemoryUiPreferencesRepository());
            save.LoadAll();
            try
            {
                foreach (var id in new[] { "1-1", "1-2", "1-3" })
                {
                    save.RecordStageCleared(id, 11_000);
                }

                Assert.IsFalse(catalog.IsUnlocked("2-1", save.Progress));
                save.RecordStageCleared("1-4", 22_000);
                Assert.IsTrue(catalog.IsUnlocked("2-1", save.Progress));
                Assert.AreEqual("2-1", save.ResolveNextStageId(catalog, "1-4"));

                var modal = ModalViewModel.CreateCleared(true, false, nextIsNewWorld: true);
                Assert.AreEqual("다음 월드", modal.Selected.Label);
                Assert.AreEqual(0, modal.SelectedIndex);
            }
            finally
            {
                DestroyCatalog(catalog);
            }
        }

        [Test]
        public void Final_World_Clear_Uses_Ending_Default_Focus()
        {
            var gameOver = ModalViewModel.CreateGameOver();
            Assert.AreEqual("retry", gameOver.Selected.Id);
            Assert.AreEqual(0, gameOver.SelectedIndex);

            var final = ModalViewModel.CreateCleared(true, true);
            Assert.AreEqual("ending", final.Selected.Id);

            final.MoveSelection(1);
            Assert.AreEqual("worldmap", final.Selected.Id);
            final.MoveSelection(1);
            Assert.AreEqual("ending", final.Selected.Id);
        }

        [Test]
        public void New_Game_Resets_Progress_While_Continue_Keeps_It()
        {
            var repo = new MemoryProgressSaveRepository();
            var prefs = new MemoryUiPreferencesRepository();
            var save = new SaveService(repo, prefs);
            save.LoadAll();
            save.MarkOpeningSeen();
            save.RecordStageCleared("1-1", 9_000);
            Assert.IsTrue(save.CanContinue());

            save.ResetProgressForNewGame();
            Assert.IsFalse(save.CanContinue());
            Assert.IsEmpty(save.Progress.completedStageIds);
            Assert.AreEqual("1-1", save.Progress.lastStageId);
            Assert.IsFalse(save.Preferences.openingSeen);
        }

        [Test]
        public void Incomplete_Stages_Show_Placeholder_Time()
        {
            Assert.AreEqual("--:--.-", ProgressTimeFormat.Incomplete);
            Assert.AreEqual("--:--.-", ProgressTimeFormat.FormatBestClear(null));
            Assert.AreEqual("1:02.3", ProgressTimeFormat.FormatBestClear(62_300));
        }

        [Test]
        public void WorldMap_Grid_Navigation_Stays_On_Unlocked_Nodes()
        {
            var catalog = CreateTwelveStageCatalog();
            var progress = SaveData.CreateDefault();
            progress.completedStageIds.AddRange(new[] { "1-1", "1-2" });
            try
            {
                var vm = WorldMapViewModel.Build(catalog, progress, "1-2");
                vm.MoveFocusGrid(0, 1);
                Assert.AreEqual("1-3", vm.FocusedStageId);
                vm.MoveFocusGrid(1, 0);
                // 2-x still locked — wraps among unlocked
                Assert.IsTrue(vm.FindFocused().Unlocked);
            }
            finally
            {
                DestroyCatalog(catalog);
            }
        }

        private static StageCatalogAsset CreateTwelveStageCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<StageCatalogAsset>();
            var stages = new List<StageDefinitionAsset>();
            foreach (var id in AllStageIds)
            {
                var asset = ScriptableObject.CreateInstance<StageDefinitionAsset>();
                asset.stageId = id;
                asset.clearGoalType = id.EndsWith("-4") ? ClearGoalType.NightFlower : ClearGoalType.ExitDoor;
                stages.Add(asset);
            }

            var field = typeof(StageCatalogAsset).GetField(
                "stages",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(catalog, stages);
            return catalog;
        }

        private static void DestroyCatalog(StageCatalogAsset catalog)
        {
            if (catalog == null)
            {
                return;
            }

            foreach (var stage in catalog.Stages)
            {
                if (stage != null)
                {
                    Object.DestroyImmediate(stage);
                }
            }

            Object.DestroyImmediate(catalog);
        }
    }
}
