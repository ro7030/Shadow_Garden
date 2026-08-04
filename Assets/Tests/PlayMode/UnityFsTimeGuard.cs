#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ShadowGarden.Tests.PlayMode
{
    /// <summary>
    /// Ensures project Temp exists around PlayMode test builds so Unity's FSTimeGet
    /// probe is less likely to assert "(folder exists no)".
    /// </summary>
    public sealed class UnityFsTimeGuard : IPrebuildSetup, IPostBuildCleanup
    {
        public void Setup() => EnsureTemp();

        public void Cleanup() => EnsureTemp();

        private static void EnsureTemp()
        {
            var temp = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp"));
            Directory.CreateDirectory(temp);
            var probe = Path.Combine(temp, ".shadowgarden_temp_ok");
            if (!File.Exists(probe))
            {
                File.WriteAllText(probe, "ok");
            }
        }
    }

    [SetUpFixture]
    public sealed class UnityFsTimeSetUpFixture
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var temp = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp"));
            Directory.CreateDirectory(temp);
        }
    }
}
#endif
