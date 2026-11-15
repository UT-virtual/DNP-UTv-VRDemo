using DNPDemo.Interaction.Stroke;
using NUnit.Framework;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class StrokeSetTests
    {
        static StrokeLine DrawLine(StrokeSet set, Vector3 from, Vector3 to, int steps)
        {
            StrokeLine line = set.Begin(from);
            for (int i = 1; i <= steps; i++)
            {
                set.Extend(line, Vector3.Lerp(from, to, (float)i / steps));
            }
            set.Finish(line);
            return line;
        }

        [Test]
        public void ExtendSkipsPointsCloserThanTheSpacing()
        {
            var set = new StrokeSet();
            StrokeLine line = set.Begin(Vector3.zero);
            Assert.IsFalse(set.Extend(line, new Vector3(StrokeSet.MinPointSpacing * 0.5f, 0f, 0f)));
            Assert.IsTrue(set.Extend(line, new Vector3(StrokeSet.MinPointSpacing * 1.5f, 0f, 0f)));
            Assert.AreEqual(2, line.Count);
            Assert.AreEqual(2, set.TotalPoints);
        }

        [Test]
        public void LineStopsGrowingAtItsLimit()
        {
            var set = new StrokeSet();
            StrokeLine line = set.Begin(Vector3.zero);
            for (int i = 1; i < StrokeSet.MaxPointsPerLine + 10; i++)
            {
                set.Extend(line, new Vector3(i * 0.01f, 0f, 0f));
            }
            Assert.AreEqual(StrokeSet.MaxPointsPerLine, line.Count);
        }

        [Test]
        public void SetStopsGrowingAtItsTotalLimit()
        {
            var set = new StrokeSet();
            int lines = 0;
            while (set.TotalPoints < StrokeSet.MaxTotalPoints && lines < 10)
            {
                DrawLine(set, new Vector3(0f, lines, 0f), new Vector3(20f, lines, 0f), StrokeSet.MaxPointsPerLine);
                lines++;
            }
            Assert.AreEqual(StrokeSet.MaxTotalPoints, set.TotalPoints);
            Assert.IsNull(set.Begin(Vector3.zero), "A full set cannot start a new line.");
        }

        [Test]
        public void RemovingALineFreesItsPoints()
        {
            var set = new StrokeSet();
            StrokeLine a = DrawLine(set, Vector3.zero, Vector3.right, 10);
            DrawLine(set, Vector3.up, Vector3.up + Vector3.right, 10);
            int before = set.TotalPoints;

            Assert.IsTrue(set.Remove(a.Id));
            Assert.AreEqual(before - a.Count, set.TotalPoints);
            Assert.IsNull(set.Find(a.Id));
            Assert.AreEqual(1, set.Lines.Count);
        }

        [Test]
        public void NewLinesGetIdsThatAreNotInUse()
        {
            var set = new StrokeSet();
            StrokeLine a = DrawLine(set, Vector3.zero, Vector3.right, 2);
            StrokeLine b = DrawLine(set, Vector3.up, Vector3.up + Vector3.right, 2);
            Assert.AreNotEqual(a.Id, b.Id);
        }

        [Test]
        public void EraserTouchesTheMiddleOfASegment()
        {
            var set = new StrokeSet();
            // Two points 1 m apart: the eraser near the middle is far from both points but close to the segment.
            StrokeLine line = set.Begin(Vector3.zero);
            set.Extend(line, new Vector3(1f, 0f, 0f));
            set.Finish(line);

            Assert.IsTrue(set.TryFindTouching(new Vector3(0.5f, 0.04f, 0f), 0.05f, out StrokeLine touched));
            Assert.AreSame(line, touched);
            Assert.IsFalse(set.TryFindTouching(new Vector3(0.5f, 0.06f, 0f), 0.05f, out _));
            Assert.IsFalse(set.TryFindTouching(new Vector3(1.06f, 0f, 0f), 0.05f, out _));
        }

        [Test]
        public void EraserTouchesASinglePointLine()
        {
            var set = new StrokeSet();
            StrokeLine dot = set.Begin(new Vector3(0f, 1f, 0f));
            set.Finish(dot);

            Assert.IsTrue(set.TryFindTouching(new Vector3(0.03f, 1f, 0f), 0.05f, out _));
            Assert.IsFalse(set.TryFindTouching(new Vector3(0.07f, 1f, 0f), 0.05f, out _));
        }

        [Test]
        public void EraserIgnoresTheLineBeingDrawn()
        {
            var set = new StrokeSet();
            StrokeLine line = set.Begin(Vector3.zero);
            set.Extend(line, new Vector3(0.1f, 0f, 0f));

            Assert.IsFalse(set.TryFindTouching(Vector3.zero, 0.05f, out _));
            set.Finish(line);
            Assert.IsTrue(set.TryFindTouching(Vector3.zero, 0.05f, out _));
        }

        [Test]
        public void BoundsCoverEveryLine()
        {
            var set = new StrokeSet();
            Assert.IsFalse(set.TryGetBounds(out _));

            DrawLine(set, new Vector3(-0.1f, 1f, 0f), new Vector3(0.1f, 1f, 0f), 10);
            DrawLine(set, new Vector3(0f, 0.8f, 0.2f), new Vector3(0f, 1.3f, 0.2f), 10);

            Assert.IsTrue(set.TryGetBounds(out Bounds bounds));
            Assert.AreEqual(-0.1f, bounds.min.x, 1e-5f);
            Assert.AreEqual(0.1f, bounds.max.x, 1e-5f);
            Assert.AreEqual(0.8f, bounds.min.y, 1e-5f);
            Assert.AreEqual(1.3f, bounds.max.y, 1e-5f);
            Assert.AreEqual(0f, bounds.min.z, 1e-5f);
            Assert.AreEqual(0.2f, bounds.max.z, 1e-5f);
        }
    }
}
