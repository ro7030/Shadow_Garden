using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ShadowGarden.Infrastructure;
using ShadowGarden.Presentation;
using ShadowGarden.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShadowGarden.Tests.EditMode
{
    public class AppStateMachineTests
    {
        [Test]
        public void Allowed_Transitions_Are_Accepted()
        {
            var flow = new GameFlowController(AppState.Title);
            Assert.IsTrue(flow.TryTransition(AppState.Opening).Accepted);
            Assert.IsTrue(flow.TryTransition(AppState.WorldMap).Accepted);
            Assert.IsTrue(flow.TryTransition(AppState.Playing).Accepted);
            Assert.IsTrue(flow.TryTransition(AppState.GameOver).Accepted);
            Assert.IsTrue(flow.TryTransition(AppState.Playing).Accepted);
            Assert.IsTrue(flow.TryTransition(AppState.Cleared).Accepted);
            Assert.IsTrue(flow.TryTransition(AppState.Ending).Accepted);
            Assert.IsTrue(flow.TryTransition(AppState.Title).Accepted);
        }

        [Test]
        public void Disallowed_Transitions_Are_Rejected_Without_Side_Effects()
        {
            var flow = new GameFlowController(AppState.Title);
            var result = flow.TryTransition(AppState.Playing);
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual("transition_not_allowed", result.RejectionReason);
            Assert.AreEqual(AppState.Title, flow.Current);
        }

        [Test]
        public void Transition_Lock_Blocks_Nested_Changes()
        {
            var flow = new GameFlowController(AppState.Title);
            flow.SetTransitionLock(true);
            var result = flow.TryTransition(AppState.Opening);
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual("transition_locked", result.RejectionReason);
            Assert.AreEqual(AppState.Title, flow.Current);
        }
    }

    public class AppScreenRouterTests
    {
        [Test]
        public void Show_Activates_Exactly_One_Root()
        {
            var host = new GameObject("ScreenHost");
            try
            {
                var roots = new GameObject[7];
                for (var i = 0; i < roots.Length; i++)
                {
                    roots[i] = new GameObject("Root" + i);
                    roots[i].SetActive(true);
                }

                var router = host.AddComponent<AppScreenRouter>();
                router.Bind(roots[0], roots[1], roots[2], roots[3], roots[4], roots[5], roots[6]);

                foreach (AppState state in System.Enum.GetValues(typeof(AppState)))
                {
                    router.Show(state);
                    Assert.AreEqual(1, router.CountActiveRoots(), "state=" + state);
                }
            }
            finally
            {
                Object.DestroyImmediate(host);
                for (var i = 0; i < 7; i++)
                {
                    var leftover = GameObject.Find("Root" + i);
                    if (leftover != null)
                    {
                        Object.DestroyImmediate(leftover);
                    }
                }
            }
        }
    }

    public class SaveRecoveryTests
    {
        [Test]
        public void Missing_Progress_Recovers_Default()
        {
            var result = SaveDataNormalizer.Parse(null);
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.UsedFallback);
            Assert.AreEqual(SaveData.CurrentVersion, result.Data.version);
            Assert.AreEqual("1-1", result.Data.lastStageId);
            Assert.IsEmpty(result.Data.completedStageIds);
        }

        [Test]
        public void Empty_Json_Recovers_Default()
        {
            var result = SaveDataNormalizer.Parse("   ");
            Assert.IsTrue(result.UsedFallback);
            Assert.AreEqual("empty", result.Error);
        }

        [Test]
        public void Corrupt_Json_Recovers_Default()
        {
            var result = SaveDataNormalizer.Parse("{not-json");
            Assert.IsTrue(result.UsedFallback);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(SaveData.CurrentVersion, result.Data.version);
        }

        [Test]
        public void Previous_Version_Is_Normalized()
        {
            var json = "{\"version\":0,\"completedStageIds\":[\"1-1\"],\"lastStageId\":\"1-4\",\"bestClearMillisecondsByStage\":[]}";
            var result = SaveDataNormalizer.Parse(json);
            Assert.IsTrue(result.UsedFallback);
            Assert.AreEqual(SaveData.CurrentVersion, result.Data.version);
            CollectionAssert.AreEqual(new[] { "1-1" }, result.Data.completedStageIds);
            Assert.AreEqual("1-4", result.Data.lastStageId);
        }

        [Test]
        public void Legacy_ClearedStageIds_Key_Is_Rewritten()
        {
            var json = "{\"version\":1,\"clearedStageIds\":[\"1-1\"],\"lastStageId\":\"1-1\",\"bestClearMillisecondsByStage\":[]}";
            var result = SaveDataNormalizer.Parse(json);
            Assert.IsTrue(result.Success);
            CollectionAssert.Contains(result.Data.completedStageIds, "1-1");
        }

        [Test]
        public void Missing_Fields_Are_Filled()
        {
            var json = "{\"version\":1,\"lastStageId\":\"\"}";
            var result = SaveDataNormalizer.Parse(json);
            Assert.IsTrue(result.UsedFallback);
            Assert.IsNotNull(result.Data.completedStageIds);
            Assert.AreEqual("1-1", result.Data.lastStageId);
            Assert.AreEqual(SaveData.CurrentVersion, result.Data.version);
        }

        [Test]
        public void UiPreferences_Empty_And_Corrupt_Recover()
        {
            Assert.IsTrue(UiPreferencesNormalizer.Parse("").UsedFallback);
            Assert.IsTrue(UiPreferencesNormalizer.Parse("{bad").UsedFallback);
            var ok = UiPreferencesNormalizer.Parse(
                "{\"version\":1,\"bgmVolume\":0.5,\"sfxVolume\":0.25,\"reduceMotion\":true,\"openingSeen\":true}");
            Assert.IsTrue(ok.Success);
            Assert.AreEqual(0.5f, ok.Data.bgmVolume, 0.001f);
            Assert.IsTrue(ok.Data.openingSeen);
        }

        [Test]
        public void Save_Failure_Does_Not_Block_Entry()
        {
            var save = new SaveService(new FailingProgressRepository(), new MemoryUiPreferencesRepository());
            save.LoadAll();
            Assert.IsNotNull(save.Progress);
            Assert.IsFalse(save.TrySaveProgress());
            Assert.AreEqual(SaveData.CurrentVersion, save.Progress.version);
        }

        [Test]
        public void Progress_And_Prefs_Use_Distinct_Keys()
        {
            Assert.AreNotEqual(PlayerPrefsSaveRepository.ProgressKey, PlayerPrefsSaveRepository.UiPrefsKey);
        }

        private sealed class FailingProgressRepository : IProgressSaveRepository
        {
            public SaveLoadResult<SaveData> Load() =>
                SaveLoadResult<SaveData>.Recovered(SaveData.CreateDefault(), "fail_load");

            public bool Save(SaveData data) => false;
        }
    }

    public class CoreAssemblyIsolationTests
    {
        [Test]
        public void Core_Assembly_Does_Not_Reference_UnityEngine()
        {
            var core = typeof(ShadowGarden.Core.GridSize).Assembly;
            var forbidden = core.GetReferencedAssemblies()
                .Any(a => a.Name == "UnityEngine" || a.Name.StartsWith("UnityEngine."));
            Assert.IsFalse(forbidden, "Core must stay Unity-free");
        }
    }

    public class InputRouterMapTests
    {
        [Test]
        public void Transition_Input_Lock_Disables_Maps()
        {
            var asset = AssetDatabaseLoadActions();
            Assert.IsNotNull(asset, "ShadowGardenActions.inputactions required");

            var router = new InputRouter(asset);
            try
            {
                router.ApplyForAppState(AppState.Title);
                Assert.AreEqual(InputMapMode.Ui, router.ActiveMode);
                Assert.IsTrue(asset.FindActionMap("UI").enabled);

                router.SetTransitionInputLock(true);
                Assert.IsTrue(router.IsInputLocked);
                Assert.IsFalse(asset.FindActionMap("UI").enabled);
                Assert.IsFalse(asset.FindActionMap("Gameplay").enabled);

                router.SetTransitionInputLock(false);
                Assert.IsTrue(asset.FindActionMap("UI").enabled);

                router.ApplyForAppState(AppState.Playing);
                Assert.AreEqual(InputMapMode.Gameplay, router.ActiveMode);
                Assert.IsTrue(asset.FindActionMap("Gameplay").enabled);
                Assert.IsFalse(asset.FindActionMap("UI").enabled);
            }
            finally
            {
                router.Dispose();
            }
        }

        [Test]
        public void Ui_Map_Contains_Point_And_Click()
        {
            var asset = AssetDatabaseLoadActions();
            Assert.IsNotNull(asset);
            var ui = asset.FindActionMap("UI", true);
            Assert.IsNotNull(ui.FindAction("Point"));
            Assert.IsNotNull(ui.FindAction("Click"));
        }

        private static InputActionAsset AssetDatabaseLoadActions()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/Input/ShadowGardenActions.inputactions");
#else
            return null;
#endif
        }
    }

    public class StageCatalogTests
    {
        [Test]
        public void First_Stage_Is_Unlocked_By_Default()
        {
            var catalog = ScriptableObject.CreateInstance<StageCatalogAsset>();
            var stage = ScriptableObject.CreateInstance<StageDefinitionAsset>();
            try
            {
                stage.stageId = "1-1";
                var field = typeof(StageCatalogAsset).GetField(
                    "stages",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                field.SetValue(catalog, new List<StageDefinitionAsset> { stage });

                Assert.IsTrue(catalog.IsUnlocked("1-1", SaveData.CreateDefault()));
                Assert.IsFalse(catalog.IsUnlocked("1-4", SaveData.CreateDefault()));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(stage);
            }
        }
    }
}
