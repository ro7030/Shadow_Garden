using System.Collections;
using NUnit.Framework;
using ShadowGarden.Presentation;
using ShadowGarden.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShadowGarden.Tests.PlayMode
{
    public class MainSceneFlowTests
    {
        [UnityTest]
        public IEnumerator Main_Scene_Boots_Title_Without_Missing_Scripts()
        {
#if UNITY_EDITOR
            Assert.IsTrue(System.IO.File.Exists("Assets/Scenes/Main.unity"));
#endif
            var load = SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            Assert.IsNotNull(load);
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;

            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            Assert.IsNotNull(root, "MainCompositionRoot missing");
            Assert.AreEqual(AppState.Title, root.CurrentState);

            var router = Object.FindFirstObjectByType<AppScreenRouter>();
            Assert.IsNotNull(router);
            Assert.AreEqual(1, router.CountActiveRoots());
            Assert.IsTrue(router.TitleRoot.activeSelf);

            Assert.IsNull(FindMissingScriptPath(), "Missing Script or broken reference in Main");
        }

        [UnityTest]
        public IEnumerator Main_Reentry_Keeps_Flow_References()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            var first = Object.FindFirstObjectByType<MainCompositionRoot>();
            Assert.IsNotNull(first);
            // Force Opening regardless of persisted openingSeen.
            if (first.CurrentState != AppState.Opening)
            {
                if (first.CurrentState == AppState.WorldMap)
                {
                    Assert.IsTrue(first.RequestState(AppState.Opening).Accepted);
                }
                else if (first.CurrentState == AppState.Title)
                {
                    Assert.IsTrue(first.RequestState(AppState.Opening).Accepted);
                }
                else
                {
                    Assert.IsTrue(first.RequestState(AppState.WorldMap).Accepted);
                    Assert.IsTrue(first.RequestState(AppState.Opening).Accepted);
                }
            }

            yield return null;
            Assert.AreEqual(AppState.Opening, first.CurrentState);

            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var second = Object.FindFirstObjectByType<MainCompositionRoot>();
            Assert.IsNotNull(second);
            Assert.AreEqual(AppState.Title, second.CurrentState);
            Assert.IsNotNull(second.Save);
            Assert.IsNotNull(second.Flow);
        }

        [UnityTest]
        public IEnumerator Save_Failure_Still_Allows_Main_Boot()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            var root = Object.FindFirstObjectByType<MainCompositionRoot>();
            Assert.IsNotNull(root);
            Assert.IsNotNull(root.Save);
            Assert.IsNotNull(root.Save.Progress);
            Assert.IsNotNull(root.Save.Preferences);
            Assert.AreEqual(AppState.Title, root.CurrentState);
        }

        private static string FindMissingScriptPath()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                var components = go.GetComponents<Component>();
                for (var i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        return go.name;
                    }
                }
            }

            return null;
        }
    }
}
