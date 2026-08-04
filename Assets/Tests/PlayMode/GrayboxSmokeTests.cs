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
            Assert.AreEqual(GridSize.Board12x6, stage.BoardSize);
            Assert.AreEqual(new GridPosition(1, 3), stage.PlayerStart);
        }

        [Test]
        public void Canonical_3_4_Is_18x8()
        {
            var stage = GrayboxStages.Create3_4();
            Assert.AreEqual("3-4", stage.StageId);
            Assert.AreEqual(GridSize.Board18x8, stage.BoardSize);
            Assert.AreEqual(150, stage.TimeLimitSeconds);
            Assert.AreEqual(4, stage.Lamps.Count);
        }
    }
}
