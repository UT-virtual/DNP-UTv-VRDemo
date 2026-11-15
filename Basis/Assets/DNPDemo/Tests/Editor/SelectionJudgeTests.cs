using System.Collections.Generic;
using DNPDemo.Interaction;
using DNPDemo.Interaction.Selection;
using NUnit.Framework;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class SelectionJudgeTests
    {
        /// <summary>Probe-grid estimates are compared with exact volumes within this margin.</summary>
        const float CoverageTolerance = 0.05f;

        readonly List<GameObject> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _created.Clear();
        }

        DetectionZone NewSphereZone(Vector3 position, float radius)
        {
            var go = new GameObject("Zone");
            _created.Add(go);
            go.transform.position = position;
            var zone = go.AddComponent<DetectionZone>();
            zone.ShapeType = ZoneShapeType.Sphere;
            zone.Radius = radius;
            return zone;
        }

        static List<SelectionSample> Line(Vector3 from, Vector3 to, int count, float radius)
        {
            var samples = new List<SelectionSample>();
            for (int i = 0; i < count; i++)
            {
                samples.Add(new SelectionSample(Vector3.Lerp(from, to, count == 1 ? 0f : (float)i / (count - 1)), radius));
            }
            return samples;
        }

        /// <summary>Criteria that only look at touching and overshoot, for tests about those rules.</summary>
        static SelectionCriteria InsideOnly(float requiredInsideRatio) => new(requiredInsideRatio, 0f);

        [Test]
        public void SucceedsWhenEveryZoneIsTouchedAndTheSelectionStaysOnThem()
        {
            DetectionZone a = NewSphereZone(new Vector3(0f, 0f, 0f), 0.2f);
            DetectionZone b = NewSphereZone(new Vector3(0.4f, 0f, 0f), 0.2f);
            List<SelectionSample> samples = Line(Vector3.zero, new Vector3(0.4f, 0f, 0f), 9, 0.05f);

            SelectionVerdict verdict = SelectionJudge.Evaluate(samples, new IDetectionVolume[] { a, b }, InsideOnly(1f));
            Assert.IsTrue(verdict.Succeeded, verdict.ToString());
            Assert.AreEqual(9, verdict.InsideCount);
            Assert.AreEqual(0, verdict.MissedTargetCount);
        }

        [Test]
        public void FailsWhenAZoneIsMissed()
        {
            DetectionZone a = NewSphereZone(new Vector3(0f, 0f, 0f), 0.2f);
            DetectionZone b = NewSphereZone(new Vector3(2f, 0f, 0f), 0.2f);
            List<SelectionSample> samples = Line(Vector3.zero, new Vector3(0.1f, 0f, 0f), 5, 0.05f);

            SelectionVerdict verdict = SelectionJudge.Evaluate(samples, new IDetectionVolume[] { a, b }, InsideOnly(0f));
            Assert.IsFalse(verdict.Succeeded);
            Assert.AreEqual(1, verdict.MissedTargetCount);
            Assert.AreEqual(0f, verdict.MinCoverage);
        }

        [Test]
        public void ToleratesOvershootUpToTheRequiredRatio()
        {
            DetectionZone zone = NewSphereZone(Vector3.zero, 0.2f);
            // 9 samples on the zone, 1 far outside: 90% inside.
            List<SelectionSample> samples = Line(Vector3.zero, new Vector3(0.1f, 0f, 0f), 9, 0.05f);
            samples.Add(new SelectionSample(new Vector3(1f, 0f, 0f), 0.05f));
            var targets = new IDetectionVolume[] { zone };

            Assert.IsTrue(SelectionJudge.Evaluate(samples, targets, InsideOnly(0.9f)).Succeeded);
            Assert.IsFalse(SelectionJudge.Evaluate(samples, targets, InsideOnly(0.95f)).Succeeded);
        }

        [Test]
        public void SphereReachingIntoTheZoneCounts()
        {
            DetectionZone zone = NewSphereZone(Vector3.zero, 0.2f);
            // The centre is 0.25 m away, but the 0.1 m sphere reaches the zone.
            var samples = new List<SelectionSample> { new(new Vector3(0.25f, 0f, 0f), 0.1f) };
            Assert.IsTrue(SelectionJudge.Evaluate(samples, new IDetectionVolume[] { zone }, InsideOnly(1f)).Succeeded);
        }

        [Test]
        public void SmallSelectionInsideTheZoneFails()
        {
            DetectionZone zone = NewSphereZone(Vector3.zero, 0.2f);
            // Wholly inside the zone, so every sample touches it, but it covers well under 1% of it.
            var samples = new List<SelectionSample> { new(Vector3.zero, 0.025f) };

            SelectionVerdict verdict = SelectionJudge.Evaluate(samples, new IDetectionVolume[] { zone }, new SelectionCriteria(0.9f, 0.5f));
            Assert.AreEqual(1f, verdict.InsideRatio);
            Assert.Less(verdict.MinCoverage, CoverageTolerance);
            Assert.IsFalse(verdict.Succeeded, verdict.ToString());
        }

        [Test]
        public void CoverageIsTheCoveredShareOfTheZone()
        {
            DetectionZone zone = NewSphereZone(new Vector3(1f, 2f, 3f), 0.2f);
            var targets = new IDetectionVolume[] { zone };

            // A concentric sphere of half the radius fills an eighth of the volume.
            var half = new List<SelectionSample> { new(new Vector3(1f, 2f, 3f), 0.1f) };
            Assert.AreEqual(0.125f, SelectionJudge.Evaluate(half, targets, InsideOnly(0f)).MinCoverage, CoverageTolerance);

            var whole = new List<SelectionSample> { new(new Vector3(1f, 2f, 3f), 0.25f) };
            Assert.AreEqual(1f, SelectionJudge.Evaluate(whole, targets, InsideOnly(0f)).MinCoverage, 1e-6f);
        }

        [Test]
        public void SweepingThroughTheZoneSucceeds()
        {
            DetectionZone zone = NewSphereZone(Vector3.zero, 0.2f);
            // A 0.15 m sphere swept along a diameter covers everything within 0.15 m of the axis: about 71% of the zone.
            List<SelectionSample> samples = Line(new Vector3(-0.2f, 0f, 0f), new Vector3(0.2f, 0f, 0f), 9, 0.15f);

            SelectionVerdict verdict = SelectionJudge.Evaluate(samples, new IDetectionVolume[] { zone }, new SelectionCriteria(0.9f, 0.5f));
            Assert.AreEqual(0.71f, verdict.MinCoverage, CoverageTolerance);
            Assert.IsTrue(verdict.Succeeded, verdict.ToString());
        }

        [Test]
        public void EveryZoneMustBeCovered()
        {
            DetectionZone a = NewSphereZone(new Vector3(0f, 0f, 0f), 0.1f);
            DetectionZone b = NewSphereZone(new Vector3(0.5f, 0f, 0f), 0.1f);
            // Fills a, and only grazes b at the end of the stroke.
            var samples = new List<SelectionSample>
            {
                new(Vector3.zero, 0.12f),
                new(new Vector3(0.35f, 0f, 0f), 0.06f),
            };

            SelectionVerdict verdict = SelectionJudge.Evaluate(samples, new IDetectionVolume[] { a, b }, new SelectionCriteria(0f, 0.5f));
            Assert.AreEqual(0, verdict.MissedTargetCount);
            Assert.Less(verdict.MinCoverage, 0.5f);
            Assert.IsFalse(verdict.Succeeded, verdict.ToString());
        }

        [Test]
        public void GroupIsOneTargetCoveredAsAWhole()
        {
            var root = new GameObject("Group");
            _created.Add(root);
            var group = root.AddComponent<DetectionZoneGroup>();
            DetectionZone shoulder = NewSphereZone(new Vector3(0f, 0f, 0f), 0.1f);
            DetectionZone elbow = NewSphereZone(new Vector3(1f, 0f, 0f), 0.1f);
            shoulder.transform.SetParent(root.transform, true);
            elbow.transform.SetParent(root.transform, true);
            group.RefreshZones();

            // Filling only the shoulder covers half of the group.
            var samples = new List<SelectionSample> { new(Vector3.zero, 0.15f) };
            var targets = new IDetectionVolume[] { group };
            SelectionVerdict verdict = SelectionJudge.Evaluate(samples, targets, new SelectionCriteria(1f, 0.4f));
            Assert.AreEqual(1, verdict.TargetCount);
            Assert.AreEqual(0.5f, verdict.MinCoverage, CoverageTolerance);
            Assert.IsTrue(verdict.Succeeded, verdict.ToString());
            Assert.IsFalse(SelectionJudge.Evaluate(samples, targets, new SelectionCriteria(1f, 0.6f)).Succeeded);
        }

        [Test]
        public void FailsWithoutSamplesOrTargets()
        {
            DetectionZone zone = NewSphereZone(Vector3.zero, 0.2f);
            var samples = new List<SelectionSample> { new(Vector3.zero, 0.05f) };
            Assert.IsFalse(SelectionJudge.Evaluate(new List<SelectionSample>(), new IDetectionVolume[] { zone }, InsideOnly(0f)).Succeeded);
            Assert.IsFalse(SelectionJudge.Evaluate(samples, new IDetectionVolume[0], InsideOnly(0f)).Succeeded);
        }

        [Test]
        public void DestroyedTargetsAreIgnored()
        {
            DetectionZone zone = NewSphereZone(Vector3.zero, 0.2f);
            DetectionZone destroyed = NewSphereZone(Vector3.zero, 0.2f);
            Object.DestroyImmediate(destroyed.gameObject);
            var samples = new List<SelectionSample> { new(Vector3.zero, 0.25f) };

            SelectionVerdict verdict = SelectionJudge.Evaluate(samples, new IDetectionVolume[] { zone, destroyed }, new SelectionCriteria(1f, 0.5f));
            Assert.AreEqual(1, verdict.TargetCount);
            Assert.IsTrue(verdict.Succeeded, verdict.ToString());
        }

        [Test]
        public void GridStaysNearTheBudgetWhateverTheShape()
        {
            var sizes = new[]
            {
                new Vector3(1f, 1f, 1f),
                new Vector3(0.1f, 0.1f, 2f),
                new Vector3(1f, 1f, 0.001f),
                new Vector3(1f, 0.001f, 0.001f),
            };
            foreach (Vector3 size in sizes)
            {
                Vector3Int counts = SelectionCoverage.GridCounts(size, SelectionCoverage.ProbeBudget);
                int total = counts.x * counts.y * counts.z;
                Assert.That(total, Is.InRange(SelectionCoverage.ProbeBudget / 2, SelectionCoverage.ProbeBudget * 2), $"{size}: {counts}");
            }
            Assert.AreEqual(Vector3Int.one, SelectionCoverage.GridCounts(Vector3.zero, SelectionCoverage.ProbeBudget));
        }
    }
}
