using System;
using DNPDemo.Interaction.Stroke;
using NUnit.Framework;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class StrokeMessageTests
    {
        static StrokeLine Draw(StrokeSet set, Vector3 from, Vector3 to, int steps, bool finish = true)
        {
            StrokeLine line = set.Begin(from);
            for (int i = 1; i <= steps; i++)
            {
                set.Extend(line, Vector3.Lerp(from, to, (float)i / steps));
            }
            if (finish)
            {
                set.Finish(line);
            }
            return line;
        }

        static void AssertSameLine(StrokeLine expected, StrokeLine actual)
        {
            Assert.IsNotNull(actual, $"line {expected.Id}");
            Assert.AreEqual(expected.Count, actual.Count, $"points of line {expected.Id}");
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.LessOrEqual(Vector3.Distance(expected.Points[i], actual.Points[i]), StrokeMessage.PositionStep, $"point {i}");
            }
        }

        static bool Apply(byte[] payload, StrokeSet set, out StrokeUpdate update) =>
            StrokeMessage.TryApply(new ArraySegment<byte>(payload), set, out update);

        [Test]
        public void AppendsInPiecesRebuildTheLine()
        {
            var actor = new StrokeSet();
            var observer = new StrokeSet();
            StrokeLine line = actor.Begin(new Vector3(-0.4f, 1.5f, 2f));
            int sent = 0;
            for (int i = 1; i <= 60; i++)
            {
                actor.Extend(line, new Vector3(-0.4f + i * 0.01f, 1.5f + Mathf.Sin(i * 0.2f) * 0.1f, 2f));
                if (i % 7 == 0)
                {
                    Assert.IsTrue(Apply(StrokeMessage.Append(line, sent), observer, out _));
                    sent = line.Count;
                }
            }
            Assert.IsTrue(Apply(StrokeMessage.Append(line, sent), observer, out _));
            actor.Finish(line);
            Assert.IsTrue(Apply(StrokeMessage.End(line.Id), observer, out StrokeUpdate update));

            Assert.AreEqual(StrokeMessageKind.End, update.Kind);
            AssertSameLine(line, observer.Find(line.Id));
            Assert.IsTrue(observer.Find(line.Id).IsFinished);
        }

        [Test]
        public void RepeatedPointsAreSkipped()
        {
            var actor = new StrokeSet();
            var observer = new StrokeSet();
            StrokeLine line = Draw(actor, Vector3.zero, Vector3.right, 20, finish: false);

            Assert.IsTrue(Apply(StrokeMessage.Append(line, 0), observer, out _));
            Assert.IsTrue(Apply(StrokeMessage.Append(line, 5), observer, out _));
            AssertSameLine(line, observer.Find(line.Id));
        }

        [Test]
        public void EraseRemovesTheLineAndCarriesTheFadeFlag()
        {
            var actor = new StrokeSet();
            var observer = new StrokeSet();
            StrokeLine line = Draw(actor, Vector3.zero, Vector3.right, 10);
            Apply(StrokeMessage.Append(line, 0), observer, out _);

            Assert.IsTrue(Apply(StrokeMessage.Erase(line.Id, fade: true), observer, out StrokeUpdate update));
            Assert.AreEqual(StrokeMessageKind.Erase, update.Kind);
            Assert.AreEqual(line.Id, update.LineId);
            Assert.IsTrue(update.Fade);
            Assert.IsNull(observer.Find(line.Id));
        }

        [Test]
        public void SnapshotReplacesEverything()
        {
            var actor = new StrokeSet();
            StrokeLine a = Draw(actor, new Vector3(0f, 1f, 0f), new Vector3(0.3f, 1.2f, 0f), 40);
            StrokeLine b = Draw(actor, new Vector3(0.1f, 0.9f, 0.1f), new Vector3(0.1f, 1.3f, 0.1f), 30, finish: false);

            var observer = new StrokeSet();
            Draw(observer, new Vector3(5f, 5f, 5f), new Vector3(6f, 5f, 5f), 5);

            Assert.IsTrue(Apply(StrokeMessage.Snapshot(actor), observer, out StrokeUpdate update));
            Assert.AreEqual(StrokeMessageKind.Snapshot, update.Kind);
            Assert.AreEqual(2, observer.Lines.Count);
            AssertSameLine(a, observer.Find(a.Id));
            AssertSameLine(b, observer.Find(b.Id));
            Assert.IsTrue(observer.Find(a.Id).IsFinished);
            Assert.IsFalse(observer.Find(b.Id).IsFinished, "The line being drawn keeps growing after the snapshot.");
        }

        [Test]
        public void EmptySnapshotClears()
        {
            var observer = new StrokeSet();
            Draw(observer, Vector3.zero, Vector3.right, 5);

            Assert.IsTrue(Apply(StrokeMessage.Snapshot(new StrokeSet()), observer, out _));
            Assert.AreEqual(0, observer.Lines.Count);
        }

        [Test]
        public void ParsesFromAnOffsetSegment()
        {
            var actor = new StrokeSet();
            StrokeLine line = Draw(actor, Vector3.zero, Vector3.one, 10);
            byte[] payload = StrokeMessage.Append(line, 0);
            var wrapped = new byte[payload.Length + 6];
            Buffer.BlockCopy(payload, 0, wrapped, 6, payload.Length);

            var observer = new StrokeSet();
            Assert.IsTrue(StrokeMessage.TryApply(new ArraySegment<byte>(wrapped, 6, payload.Length), observer, out _));
            AssertSameLine(line, observer.Find(line.Id));

            byte[] snapshot = StrokeMessage.Snapshot(actor);
            wrapped = new byte[snapshot.Length + 3];
            Buffer.BlockCopy(snapshot, 0, wrapped, 3, snapshot.Length);
            Assert.IsTrue(StrokeMessage.TryApply(new ArraySegment<byte>(wrapped, 3, snapshot.Length), new StrokeSet(), out _));
        }

        [Test]
        public void RejectsMalformedPayloadsWithoutChangingTheSet()
        {
            var actor = new StrokeSet();
            StrokeLine line = Draw(actor, Vector3.zero, Vector3.right, 10);
            var observer = new StrokeSet();
            Draw(observer, Vector3.up, Vector3.up * 2f, 5);

            byte[] append = StrokeMessage.Append(line, 0);
            Assert.IsFalse(Apply(append[..^1], observer, out _), "truncated append");

            byte[] snapshot = StrokeMessage.Snapshot(actor);
            Assert.IsFalse(Apply(snapshot[..^1], observer, out _), "truncated snapshot");
            Assert.AreEqual(1, observer.Lines.Count, "A rejected snapshot leaves the set as it was.");

            byte[] wrongVersion = StrokeMessage.End(line.Id);
            wrongVersion[0] = StrokeMessage.FormatVersion + 1;
            Assert.IsFalse(Apply(wrongVersion, observer, out _), "unknown version");

            byte[] wrongKind = StrokeMessage.End(line.Id);
            wrongKind[1] = 99;
            Assert.IsFalse(Apply(wrongKind, observer, out _), "unknown kind");

            Assert.IsFalse(Apply(Array.Empty<byte>(), observer, out _), "empty");
        }
    }
}
