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
        public void MenuOffersSelectVoiceAndDraw()
        {
            CollectionAssert.AreEqual(new[] { PenMode.Select, PenMode.Voice, PenMode.Draw }, PenModeSelection.Selectable);
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
            PenModeSelection.Select(PenMode.Manipulate);
            PenModeSelection.Select(PenMode.None);
            Assert.AreEqual(PenModeSelection.Default, PenModeSelection.Current);
        }
    }
}
