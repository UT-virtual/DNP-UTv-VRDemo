using System.Collections.Generic;
using DNPDemo.Interaction;
using DNPDemo.Interaction.Stroke;
using NUnit.Framework;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class StrokeJudgeTests
    {
        const float LineRadius = 0.003f;

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

        static List<Vector3> Line(Vector3 from, Vector3 to, int count)
        {
            var points = new List<Vector3>();
            for (int i = 0; i < count; i++)
            {
                points.Add(Vector3.Lerp(from, to, count == 1 ? 0f : (float)i / (count - 1)));
            }
            return points;
        }

        [Test]
        public void GuideSucceedsWhenTheLineReachesBothZones()
        {
            DetectionZone start = NewSphereZone(new Vector3(-0.3f, 1.6f, 0f), 0.1f);
            DetectionZone end = NewSphereZone(new Vector3(0f, 1.4f, 0.35f), 0.1f);
            List<Vector3> line = Line(new Vector3(-0.3f, 1.6f, 0f), new Vector3(0f, 1.4f, 0.35f), 50);

            Assert.IsTrue(StrokeJudge.Guide(line, start, end, LineRadius).Succeeded);
        }

        [Test]
        public void GuideAcceptsEitherDirection()
        {
            DetectionZone start = NewSphereZone(new Vector3(-0.3f, 1.6f, 0f), 0.1f);
            DetectionZone end = NewSphereZone(new Vector3(0f, 1.4f, 0.35f), 0.1f);
            List<Vector3> line = Line(new Vector3(0f, 1.4f, 0.35f), new Vector3(-0.3f, 1.6f, 0f), 50);

            Assert.IsTrue(StrokeJudge.Guide(line, start, end, LineRadius).Succeeded);
        }

        [Test]
        public void GuideFailsWhenTheLineStopsShort()
        {
            DetectionZone start = NewSphereZone(new Vector3(-0.3f, 1.6f, 0f), 0.1f);
            DetectionZone end = NewSphereZone(new Vector3(0f, 1.4f, 0.35f), 0.1f);
            // Ends halfway, well outside the end zone.
            List<Vector3> line = Line(new Vector3(-0.3f, 1.6f, 0f), new Vector3(-0.15f, 1.5f, 0.17f), 30);

            Assert.IsFalse(StrokeJudge.Guide(line, start, end, LineRadius).Succeeded);
        }

        [Test]
        public void GuideCountsTheLineThicknessAtTheZoneEdge()
        {
            DetectionZone start = NewSphereZone(Vector3.zero, 0.1f);
            DetectionZone end = NewSphereZone(new Vector3(1f, 0f, 0f), 0.1f);
            // Stops 2 mm short of the end zone; a line 6 mm wide still touches it.
            List<Vector3> line = Line(Vector3.zero, new Vector3(0.898f, 0f, 0f), 100);

            Assert.IsTrue(StrokeJudge.Guide(line, start, end, LineRadius).Succeeded);
            Assert.IsFalse(StrokeJudge.Guide(line, start, end, 0f).Succeeded);
        }

        [Test]
        public void GuideFailsWithoutBothZones()
        {
            DetectionZone start = NewSphereZone(Vector3.zero, 0.1f);
            List<Vector3> line = Line(Vector3.zero, Vector3.right, 10);

            Assert.IsFalse(StrokeJudge.Guide(line, start, null, LineRadius).Succeeded);
            Assert.IsFalse(StrokeJudge.Guide(line, null, start, LineRadius).Succeeded);
        }

        [Test]
        public void SketchUsesTheBoxAroundAllLines()
        {
            var set = new StrokeSet();
            // Two short lines, 0.1 m each, placed 0.2 m apart: together they span 0.3 m.
            StrokeLine a = set.Begin(new Vector3(0f, 1f, 0f));
            set.Extend(a, new Vector3(0.1f, 1f, 0f));
            set.Finish(a);
            Assert.IsFalse(StrokeJudge.Sketch(set, 0.15f).Succeeded);

            StrokeLine b = set.Begin(new Vector3(0.2f, 1f, 0f));
            set.Extend(b, new Vector3(0.3f, 1f, 0f));
            set.Finish(b);
            Assert.IsTrue(StrokeJudge.Sketch(set, 0.15f).Succeeded);
        }

        [Test]
        public void SketchFailsWhenEmpty()
        {
            Assert.IsFalse(StrokeJudge.Sketch(new StrokeSet(), 0.15f).Succeeded);
        }
    }
}
