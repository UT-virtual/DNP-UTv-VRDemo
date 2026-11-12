using DNPDemo.Scenario;
using NUnit.Framework;

namespace DNPDemo.Tests
{
    public class ScenarioProgressTests
    {
        [Test]
        public void StartsIdle()
        {
            var progress = new ScenarioProgress(3);
            Assert.IsFalse(progress.IsRunning);
            Assert.AreEqual(-1, progress.StepIndex);
            Assert.IsFalse(progress.Next());
            Assert.IsFalse(progress.Back());
            Assert.IsFalse(progress.Restart());
        }

        [Test]
        public void StartGoesToFirstStepOnce()
        {
            var progress = new ScenarioProgress(3);
            Assert.IsTrue(progress.Start());
            Assert.AreEqual(0, progress.StepIndex);
            Assert.IsFalse(progress.Start());
        }

        [Test]
        public void StartFailsWithoutSteps()
        {
            Assert.IsFalse(new ScenarioProgress(0).Start());
        }

        [Test]
        public void NextAndBackStayInRange()
        {
            var progress = new ScenarioProgress(2);
            progress.Start();
            Assert.IsFalse(progress.Back());
            Assert.IsTrue(progress.Next());
            Assert.AreEqual(1, progress.StepIndex);
            Assert.IsFalse(progress.Next());
            Assert.IsTrue(progress.Back());
            Assert.AreEqual(0, progress.StepIndex);
        }

        [Test]
        public void EveryTransitionChangesSequence()
        {
            var progress = new ScenarioProgress(3);
            progress.Start();
            ushort afterStart = progress.Sequence;
            progress.Restart();
            Assert.AreEqual(0, progress.StepIndex);
            Assert.AreNotEqual(afterStart, progress.Sequence, "A restart onto the same step must still be a new state.");
        }

        [Test]
        public void SequenceWraps()
        {
            var progress = new ScenarioProgress(2);
            progress.Apply(ushort.MaxValue, 0);
            progress.Next();
            Assert.AreEqual(0, progress.Sequence);
        }

        [Test]
        public void IsCurrentMatchesOnlyTheShownState()
        {
            var progress = new ScenarioProgress(3);
            progress.Start();
            ushort sequence = progress.Sequence;
            Assert.IsTrue(progress.IsCurrent(sequence, 0));
            Assert.IsFalse(progress.IsCurrent(sequence, 1));
            progress.Next();
            Assert.IsFalse(progress.IsCurrent(sequence, 0), "A report made against an earlier step is stale.");
        }

        [Test]
        public void ApplyRejectsDuplicatesAndOutOfRange()
        {
            var progress = new ScenarioProgress(3);
            Assert.IsTrue(progress.Apply(5, 2));
            Assert.AreEqual(2, progress.StepIndex);
            Assert.AreEqual(5, progress.Sequence);
            Assert.IsFalse(progress.Apply(5, 2));
            Assert.IsFalse(progress.Apply(6, 3));
            Assert.IsFalse(progress.Apply(6, -1));
            Assert.IsTrue(progress.Apply(6, 2), "Same step with a new sequence is a restart of that step.");
        }

        [Test]
        public void StopEndsTheRunButKeepsCounting()
        {
            var progress = new ScenarioProgress(3);
            progress.Start();
            ushort sequence = progress.Sequence;
            progress.Stop();
            Assert.IsFalse(progress.IsRunning);
            Assert.IsTrue(progress.Start());
            Assert.AreNotEqual(sequence, progress.Sequence);
        }
    }
}
