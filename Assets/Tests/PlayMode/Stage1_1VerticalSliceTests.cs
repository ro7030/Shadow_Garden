using System.Collections;
using NUnit.Framework;
using ShadowGarden.Core;
using ShadowGarden.Presentation;
using ShadowGarden.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ShadowGarden.Tests.PlayMode
{
    public class Stage1_1VerticalSliceTests
    {
        [UnityTest]
        public IEnumerator Title_WorldMap_Playing_Flow()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            Assert.IsNotNull(root);
            Assert.AreEqual(AppState.Title, root.CurrentState);

            // Skip opening for deterministic flow
            root.Save.Preferences.openingSeen = true;
            root.ContinueFromTitle();
            yield return null;
            Assert.AreEqual(AppState.WorldMap, root.CurrentState);

            root.StartStage("1-1");
            yield return null;
            yield return null;
            Assert.AreEqual(AppState.Playing, root.CurrentState);
            Assert.IsNotNull(root.Gameplay);
            Assert.IsNotNull(root.Gameplay.Session);
            Assert.AreEqual("1-1", root.Gameplay.Definition.StageId);
        }

        [UnityTest]
        public IEnumerator Normal_Solution_Reaches_Cleared_Screen()
        {
            yield return LoadMainAndPlay();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            var host = root.Gameplay;
            var session = host.Session;

            session.Move(CardinalDirection.East);
            session.Rotate(1);
            yield return null;

            var path = MainStages.Solution_1_1().PathCells;
            for (var i = 2; i < path.Count; i++)
            {
                if (path[i] == path[i - 1])
                {
                    continue;
                }

                StepToward(session, path[i]);
                yield return null;
            }

            // Wait door 0.45 + pass 0.35 (+ buffer)
            var guard = 0f;
            while (root.CurrentState != AppState.Cleared && guard < 3f)
            {
                guard += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(AppState.Cleared, root.CurrentState);
            CollectionAssert.Contains(root.Save.Progress.completedStageIds, "1-1");
        }

        [UnityTest]
        public IEnumerator Cliff_Leads_To_GameOver_Then_Retry()
        {
            yield return LoadMainAndPlay();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            var session = root.Gameplay.Session;
            session.Move(CardinalDirection.North); // cliff from (1,3)
            var guard = 0f;
            while (root.CurrentState != AppState.GameOver && guard < 2f)
            {
                guard += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(AppState.GameOver, root.CurrentState);
            root.RetryFromGameOver();
            yield return null;
            yield return null;
            Assert.AreEqual(AppState.Playing, root.CurrentState);
            Assert.AreEqual(new GridPosition(1, 3), root.Gameplay.Session.State.PlayerPosition);
        }

        [UnityTest]
        public IEnumerator Reset_Ten_Times_Does_Not_Leak_Session()
        {
            yield return LoadMainAndPlay();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            var host = root.Gameplay;
            for (var i = 0; i < 10; i++)
            {
                host.Session.Move(CardinalDirection.East);
                host.RestartActiveStage();
                yield return null;
            }

            Assert.AreEqual(10, host.RestartCount);
            Assert.AreEqual(AppState.Playing, root.CurrentState);
            Assert.AreEqual(new GridPosition(1, 3), host.Session.State.PlayerPosition);
            Assert.AreEqual(1, Object.FindObjectsByType<BoardPresenter>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator Focus_Loss_And_Return_Keeps_Playable_Session()
        {
            yield return LoadMainAndPlay();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            var session = root.Gameplay.Session;
            var before = session.State.RemainingMilliseconds;
            for (var i = 0; i < 5; i++)
            {
                session.SetFocus(false);
                session.Tick(500);
                session.SetFocus(true);
                session.Tick(100);
            }

            Assert.Less(session.State.RemainingMilliseconds, before);
            Assert.AreEqual(StagePhase.Playing, session.State.Phase);
            Assert.AreEqual(AppState.Playing, root.CurrentState);
        }

        [UnityTest]
        public IEnumerator Onboarding_Wasd_Hides_After_First_Move()
        {
            yield return LoadMainAndPlay();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            var host = root.Gameplay;
            Assert.IsTrue(host.Onboarding.WasdVisible);
            host.Session.Move(CardinalDirection.East);
            host.Onboarding.NotifyMoved();
            Assert.IsFalse(host.Onboarding.WasdVisible);
        }

        private static IEnumerator LoadMain()
        {
            var load = SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }

        private static IEnumerator LoadMainAndPlay()
        {
            yield return LoadMain();
            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            root.Save.Preferences.openingSeen = true;
            root.ContinueFromTitle();
            yield return null;
            root.StartStage("1-1");
            yield return null;
            yield return null;
        }

        private static void StepToward(StageSession session, GridPosition to)
        {
            var from = session.State.PlayerPosition;
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var dir = dx == 1 ? CardinalDirection.East
                : dx == -1 ? CardinalDirection.West
                : dy == 1 ? CardinalDirection.South
                : CardinalDirection.North;
            session.Move(dir);
        }
    }
}
