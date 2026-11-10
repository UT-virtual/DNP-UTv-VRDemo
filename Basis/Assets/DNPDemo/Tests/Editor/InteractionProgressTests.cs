using DNPDemo.Interaction;
using NUnit.Framework;

namespace DNPDemo.Tests
{
    public class InteractionProgressTests
    {
        [Test]
        public void StartsInactive()
        {
            var progress = new InteractionProgress();
            Assert.IsFalse(progress.IsActive);
            Assert.IsFalse(progress.IsFinished);
            Assert.IsFalse(progress.IsCurrentStep(0, 0));
        }

        [Test]
        public void PartsCompleteInOrderThenFinish()
        {
            var progress = new InteractionProgress();
            progress.Begin(5, 6, 2);
            Assert.IsTrue(progress.IsActive);
            Assert.AreEqual(0, progress.PartIndex);

            Assert.IsFalse(progress.CompletePart(1), "Only the current part can complete.");
            Assert.IsTrue(progress.CompletePart(0));
            Assert.AreEqual(1, progress.PartIndex);
            Assert.IsFalse(progress.CompletePart(0), "A part completes once.");

            Assert.IsTrue(progress.CompletePart(1));
            Assert.IsFalse(progress.IsActive);
            Assert.IsTrue(progress.IsFinished);
            Assert.IsFalse(progress.CompletePart(2));
        }

        [Test]
        public void RemoteProgressOnlyMovesForwardWithinTheSameState()
        {
            var progress = new InteractionProgress();
            progress.Begin(5, 6, 3);

            Assert.IsFalse(progress.ApplyRemote(4, 6, 1), "Another sequence is another visit of the step.");
            Assert.IsFalse(progress.ApplyRemote(5, 7, 1), "Another step.");
            Assert.IsFalse(progress.ApplyRemote(5, 6, 0), "Not forward.");
            Assert.IsFalse(progress.ApplyRemote(5, 6, 4), "Past the end.");

            Assert.IsTrue(progress.ApplyRemote(5, 6, 2), "Parts can be skipped when messages were missed.");
            Assert.AreEqual(2, progress.PartIndex);
            Assert.IsFalse(progress.ApplyRemote(5, 6, 1), "Late message.");

            Assert.IsTrue(progress.ApplyRemote(5, 6, 3));
            Assert.IsTrue(progress.IsFinished);
        }

        [Test]
        public void BeginRestartsAndClearEnds()
        {
            var progress = new InteractionProgress();
            progress.Begin(1, 3, 2);
            progress.CompletePart(0);

            progress.Begin(2, 3, 2);
            Assert.AreEqual(0, progress.PartIndex, "A new scenario state starts from the first part.");
            Assert.IsTrue(progress.IsCurrentStep(2, 3));
            Assert.IsFalse(progress.IsCurrentStep(1, 3));

            progress.Clear();
            Assert.IsFalse(progress.IsActive);
            Assert.IsFalse(progress.IsCurrentStep(2, 3));
        }
    }
}
