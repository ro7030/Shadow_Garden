using NUnit.Framework;
using ShadowGarden.Core;
using UnityEngine;

namespace ShadowGarden.Tests.PlayMode
{
    public class GrayboxSmokeTests
    {
        [Test]
        public void Graybox_1_1_Exists()
        {
            var stage = GrayboxStages.Create1_1();
            Assert.AreEqual("1-1", stage.StageId);
            Assert.AreEqual(120, stage.TimeLimitSeconds);
        }
    }
}
