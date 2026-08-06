using NUnit.Framework;
using ShadowGarden.Core;
using ShadowGarden.Presentation;
using UnityEngine;

namespace ShadowGarden.Tests.EditMode
{
    public class BoardCameraFitterTests
    {
        [Test]
        public void Framing_12x6_At_16x9_Matches_Architecture()
        {
            var framing = BoardCameraFitter.Calculate(GridSize.Board12x6, 16f / 9f);
            Assert.AreEqual(3.875f, framing.OrthographicSize, 0.02f);
        }

        [Test]
        public void Framing_18x8_At_16x9_Matches_Architecture()
        {
            var framing = BoardCameraFitter.Calculate(GridSize.Board18x8, 16f / 9f);
            Assert.AreEqual(5.5625f, framing.OrthographicSize, 0.02f);
        }

        [Test]
        public void Framing_With_Top_Visual_Overflow_Leaves_Room_For_High_Pillars()
        {
            var framing = BoardCameraFitter.Calculate(
                GridSize.Board12x6,
                16f / 9f,
                GridWorld.CellSize,
                BoardCameraFitter.DefaultPaddingCells,
                1.5f);

            Assert.AreEqual(4.25f, framing.OrthographicSize, 0.02f);
            Assert.AreEqual(-1.75f, framing.Center.y, 0.02f);
        }
    }
}
