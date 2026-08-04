using System.Collections;
using NUnit.Framework;
using ShadowGarden.Presentation;
using ShadowGarden.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ShadowGarden.Tests.PlayMode
{
    public class WorldProgressFlowTests
    {
        [UnityTest]
        public IEnumerator Fail_Then_WorldMap_Focuses_Failed_Stage()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            Assert.IsNotNull(root);
            root.Save.Preferences.openingSeen = true;
            root.ContinueFromTitle();
            yield return null;
            Assert.AreEqual(AppState.WorldMap, root.CurrentState);

            root.StartStage("1-1");
            yield return null;
            yield return null;
            Assert.AreEqual(AppState.Playing, root.CurrentState);

            root.Gameplay.Session.Move(ShadowGarden.Core.CardinalDirection.North);
            var guard = 0f;
            while (root.CurrentState != AppState.GameOver && guard < 2f)
            {
                guard += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(AppState.GameOver, root.CurrentState);
            Assert.AreEqual("1-1", root.Save.Progress.lastStageId);

            root.ReturnToWorldMap();
            yield return null;
            Assert.AreEqual(AppState.WorldMap, root.CurrentState);

            var screens = Object.FindFirstObjectByType<MainFlowScreens>();
            Assert.IsNotNull(screens);
            Assert.IsNotNull(screens.CurrentWorldMap);
            Assert.AreEqual("1-1", screens.CurrentWorldMap.FocusedStageId);
        }

        [UnityTest]
        public IEnumerator Clear_Then_Next_Stage_Advances()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.Preferences.openingSeen = true;
            root.Save.RecordStageCleared("1-1", 10_000);
            root.ContinueFromTitle();
            yield return null;

            root.StartStage("1-1");
            yield return null;
            yield return null;
            Assert.AreEqual(AppState.Playing, root.CurrentState);

            root.NotifyCleared("1-1", 9_000);
            yield return null;
            Assert.AreEqual(AppState.Cleared, root.CurrentState);

            var screens = Object.FindFirstObjectByType<MainFlowScreens>();
            Assert.IsNotNull(screens.CurrentModal);
            Assert.AreEqual("next", screens.CurrentModal.Selected.Id);

            root.ContinueAfterClear();
            yield return null;
            yield return null;
            Assert.AreEqual(AppState.Playing, root.CurrentState);
            Assert.AreEqual("1-2", root.PendingStageId);
            Assert.IsNotNull(root.Gameplay);
            Assert.IsNotNull(root.Gameplay.Definition);
            Assert.AreEqual("1-2", root.Gameplay.Definition.StageId);
        }

        [UnityTest]
        public IEnumerator Final_Clear_Can_Enter_Ending()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.Preferences.openingSeen = true;
            foreach (var id in new[]
                     {
                         "1-1", "1-2", "1-3", "1-4",
                         "2-1", "2-2", "2-3", "2-4",
                         "3-1", "3-2", "3-3"
                     })
            {
                root.Save.RecordStageCleared(id, 12_000);
            }

            root.ContinueFromTitle();
            yield return null;
            root.StartStage("3-4");
            yield return null;
            yield return null;
            Assert.AreEqual(AppState.Playing, root.CurrentState);

            root.NotifyCleared("3-4", 40_000);
            yield return null;
            Assert.AreEqual(AppState.Cleared, root.CurrentState);
            Assert.IsTrue(root.IsFinalStageClear());

            var screens = Object.FindFirstObjectByType<MainFlowScreens>();
            Assert.AreEqual("ending", screens.CurrentModal.Selected.Id);

            root.EnterEndingFromCleared();
            yield return null;
            Assert.AreEqual(AppState.Ending, root.CurrentState);
        }

        private static IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }
    }
}
