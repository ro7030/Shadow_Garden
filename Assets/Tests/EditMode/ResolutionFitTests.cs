using NUnit.Framework;
using ShadowGarden.Core;
using ShadowGarden.Presentation;
using UnityEngine;

namespace ShadowGarden.Tests.EditMode
{
    public class ResolutionFitTests
    {
        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void BoardCameraFitter_Fits_12x6_At_Common_Resolutions(int width, int height)
        {
            var aspect = width / (float)height;
            var result = BoardCameraFitter.Calculate(GridSize.Board12x6, aspect);
            Assert.Greater(result.OrthographicSize, 0f);
            Assert.IsTrue(float.IsFinite(result.OrthographicSize));
            Assert.AreEqual(-10f, result.Center.z, 0.001f);
        }
    }
}
