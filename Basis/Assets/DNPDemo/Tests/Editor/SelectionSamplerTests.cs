using DNPDemo.Interaction.Selection;
using NUnit.Framework;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class SelectionSamplerTests
    {
        const float Radius = 0.1f;

        [Test]
        public void FirstPointIsAlwaysSampled()
        {
            var sampler = new SelectionSampler(0.5f, 16);
            Assert.IsTrue(sampler.Add(Vector3.one, Radius));
            Assert.AreEqual(1, sampler.Count);
            Assert.AreEqual(Vector3.one, sampler.Samples[0].Position);
            Assert.AreEqual(Radius, sampler.Samples[0].Radius);
        }

        [Test]
        public void SmallMovesAreSkipped()
        {
            var sampler = new SelectionSampler(0.5f, 16);
            sampler.Add(Vector3.zero, Radius);
            Assert.IsFalse(sampler.Add(new Vector3(0.049f, 0f, 0f), Radius), "Below half the radius.");
            Assert.AreEqual(1, sampler.Count);
            Assert.IsTrue(sampler.Add(new Vector3(0.051f, 0f, 0f), Radius), "Just over half the radius.");
            Assert.AreEqual(2, sampler.Count);
        }

        [Test]
        public void SpacingScalesWithRadius()
        {
            var small = new SelectionSampler(0.5f, 1000);
            var large = new SelectionSampler(0.5f, 1000);
            var end = new Vector3(1.005f, 0f, 0f);
            small.Add(Vector3.zero, 0.02f);
            small.Add(end, 0.02f);
            large.Add(Vector3.zero, 0.2f);
            large.Add(end, 0.2f);
            // About 1 m at 0.01 m spacing vs 0.1 m spacing.
            Assert.AreEqual(101, small.Count);
            Assert.AreEqual(11, large.Count);
        }

        [Test]
        public void LongJumpsAreFilledEvenly()
        {
            var sampler = new SelectionSampler(0.5f, 100);
            sampler.Add(Vector3.zero, Radius);
            sampler.Add(new Vector3(0.525f, 0f, 0f), Radius);
            Assert.AreEqual(11, sampler.Count, "Ten spacings fit; the remainder is shorter than one.");
            for (int i = 1; i < sampler.Count; i++)
            {
                float gap = Vector3.Distance(sampler.Samples[i - 1].Position, sampler.Samples[i].Position);
                Assert.AreEqual(0.05f, gap, 1e-5f, $"gap before sample {i}");
            }
        }

        [Test]
        public void GrowingInPlaceIsRecorded()
        {
            var sampler = new SelectionSampler(0.5f, 100);
            sampler.Add(Vector3.zero, 0.1f);
            Assert.IsTrue(sampler.Add(Vector3.zero, 0.2f));
            Assert.AreEqual(0.2f, sampler.Samples[sampler.Count - 1].Radius, 1e-5f);
        }

        [Test]
        public void StopsAtMaxSamples()
        {
            var sampler = new SelectionSampler(0.5f, 5);
            sampler.Add(Vector3.zero, Radius);
            sampler.Add(new Vector3(10f, 0f, 0f), Radius);
            Assert.AreEqual(5, sampler.Count);
            Assert.IsTrue(sampler.IsFull);
            Assert.IsFalse(sampler.Add(new Vector3(20f, 0f, 0f), Radius));
        }

        [Test]
        public void ClearStartsOver()
        {
            var sampler = new SelectionSampler(0.5f, 5);
            sampler.Add(Vector3.zero, Radius);
            sampler.Add(new Vector3(10f, 0f, 0f), Radius);
            sampler.Clear();
            Assert.AreEqual(0, sampler.Count);
            Assert.IsTrue(sampler.Add(Vector3.one, Radius));
            Assert.AreEqual(Vector3.one, sampler.Samples[0].Position);
        }
    }
}
