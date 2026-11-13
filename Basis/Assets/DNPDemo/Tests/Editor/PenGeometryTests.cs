using DNPDemo.Pen;
using NUnit.Framework;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class PenGeometryTests
    {
        const float Tolerance = 1e-5f;

        [Test]
        public void ExtensionStaysZeroUpToStartRadius()
        {
            Assert.AreEqual(0f, PenGeometry.ExtensionForRadius(PenGeometry.MinSelectRadius), Tolerance);
            Assert.AreEqual(0f, PenGeometry.ExtensionForRadius(0.05f), Tolerance);
        }

        [Test]
        public void ExtensionGrowsLinearlyToFullAtMaxRadius()
        {
            Assert.AreEqual(0.5f, PenGeometry.ExtensionForRadius(0.275f), Tolerance);
            Assert.AreEqual(1f, PenGeometry.ExtensionForRadius(PenGeometry.MaxSelectRadius), Tolerance);
            Assert.AreEqual(1.3f, PenGeometry.Length(PenGeometry.ExtensionForRadius(PenGeometry.MaxSelectRadius)), Tolerance);
        }

        [Test]
        public void TipAndSphereScaleFollowTheModel()
        {
            Assert.AreEqual(new Vector3(0.3f, 0f, 0f), PenGeometry.TipLocalPosition(0f));
            Assert.AreEqual(1f, PenGeometry.SelectAreaScale(PenGeometry.ModelSelectRadius), Tolerance);
            Assert.AreEqual(10f, PenGeometry.SelectAreaScale(0.5f), Tolerance);
        }

        [Test]
        public void AdjustRadiusStaysWithinLimits()
        {
            Assert.AreEqual(PenGeometry.MaxSelectRadius, PenGeometry.AdjustRadius(0.4f, 1f, 10f, 1.5f), Tolerance);
            Assert.AreEqual(PenGeometry.MinSelectRadius, PenGeometry.AdjustRadius(0.03f, -1f, 10f, 1.5f), Tolerance);
            Assert.AreEqual(0.1f, PenGeometry.AdjustRadius(0.1f, 0f, 1f, 1.5f), Tolerance);
        }

        [Test]
        public void FullPushCoversTheWholeRangeInTheGivenTime()
        {
            float radius = PenGeometry.MinSelectRadius;
            for (int i = 0; i < 150; i++)
            {
                radius = PenGeometry.AdjustRadius(radius, 1f, 0.01f, 1.5f);
            }
            Assert.AreEqual(PenGeometry.MaxSelectRadius, radius, 1e-4f);
        }
    }
}
