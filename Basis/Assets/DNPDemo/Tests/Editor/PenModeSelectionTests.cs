using DNPDemo.Interaction;
using DNPDemo.Pen;
using NUnit.Framework;

namespace DNPDemo.Tests
{
    public class PenModeSelectionTests
    {
        [SetUp]
        public void SetUp() => PenModeSelection.ResetToDefault();

        [TearDown]
        public void TearDown() => PenModeSelection.ResetToDefault();

        [Test]
        public void MenuOffersSelectManipulateVoiceAndDraw()
        {
            CollectionAssert.AreEqual(new[] { PenMode.Select, PenMode.Manipulate, PenMode.Voice, PenMode.Draw }, PenModeSelection.Selectable);
            Assert.IsTrue(PenModeSelection.IsSelectable(PenModeSelection.Default));
        }

        [Test]
        public void SelectChangesModeAndNotifiesOnce()
        {
            int notified = 0;
            PenModeSelection.Changed += _ => notified++;
            PenModeSelection.Select(PenMode.Voice);
            PenModeSelection.Select(PenMode.Voice);
            Assert.AreEqual(PenMode.Voice, PenModeSelection.Current);
            Assert.AreEqual(1, notified);
        }

        [Test]
        public void ModesNotOnTheMenuAreIgnored()
        {
            PenModeSelection.Select(PenMode.Panel);
            PenModeSelection.Select(PenMode.None);
            Assert.AreEqual(PenModeSelection.Default, PenModeSelection.Current);
        }

        [Test]
        public void SelectNextStepsThroughTheMenuOrderAndWraps()
        {
            var visited = new System.Collections.Generic.List<PenMode>();
            PenModeSelection.Changed += visited.Add;
            for (int i = 0; i < PenModeSelection.Selectable.Length; i++)
            {
                PenModeSelection.SelectNext();
            }
            CollectionAssert.AreEqual(new[] { PenMode.Manipulate, PenMode.Voice, PenMode.Draw, PenMode.Select }, visited);
            Assert.AreEqual(PenMode.Select, PenModeSelection.Current);
        }

        [Test]
        public void ModesNotOnTheMenuAreFollowedByTheFirst()
        {
            Assert.AreEqual(PenModeSelection.Selectable[0], PenModeSelection.Next(PenMode.Panel));
            Assert.AreEqual(PenModeSelection.Selectable[0], PenModeSelection.Next(PenMode.None));
        }
    }
}
