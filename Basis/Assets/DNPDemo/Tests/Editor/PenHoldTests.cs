using DNPDemo.Pen;
using NUnit.Framework;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class PenHoldTests
    {
        static readonly Pose Hand = new(new Vector3(0f, 1f, 0f), Quaternion.Euler(0f, 90f, 0f));
        static readonly Pose Pen = new(new Vector3(0.05f, 1.02f, 0.1f), Quaternion.Euler(10f, 20f, 30f));

        [Test]
        public void GripPressInReachGrabs()
        {
            var hold = new PenHold();
            Assert.IsTrue(hold.Update(true, true, Hand, Pen, true));
            Assert.IsTrue(hold.IsHeld);
        }

        [Test]
        public void GripPressOutOfReachDoesNotGrab()
        {
            var hold = new PenHold();
            Assert.IsFalse(hold.Update(true, true, Hand, Pen, false));
            Assert.IsFalse(hold.IsHeld);
        }

        [Test]
        public void HoldingGripWhileMovingIntoReachDoesNotGrab()
        {
            var hold = new PenHold();
            hold.Update(true, true, Hand, Pen, false);
            hold.Update(true, true, Hand, Pen, true);
            Assert.IsFalse(hold.IsHeld);
        }

        [Test]
        public void ReleasingGripOrLosingTrackingLetsGo()
        {
            var hold = new PenHold();
            hold.Update(true, true, Hand, Pen, true);
            Assert.IsTrue(hold.Update(false, true, Hand, Pen, true));
            Assert.IsFalse(hold.IsHeld);

            hold.Update(true, true, Hand, Pen, true);
            Assert.IsTrue(hold.Update(true, false, Hand, Pen, true));
            Assert.IsFalse(hold.IsHeld);
        }

        [Test]
        public void HeldPenKeepsTheOffsetFromGrabTime()
        {
            var hold = new PenHold();
            hold.Update(true, true, Hand, Pen, true);

            Pose same = hold.Follow(Hand);
            Assert.That(Vector3.Distance(same.position, Pen.position), Is.LessThan(1e-5f));
            Assert.That(Quaternion.Angle(same.rotation, Pen.rotation), Is.LessThan(0.01f));

            var moved = new Pose(Hand.position + new Vector3(1f, 0f, 0f), Quaternion.Euler(0f, 180f, 0f) * Hand.rotation);
            Pose followed = hold.Follow(moved);
            Vector3 expectedOffset = Quaternion.Euler(0f, 180f, 0f) * (Pen.position - Hand.position);
            Assert.That(Vector3.Distance(followed.position, moved.position + expectedOffset), Is.LessThan(1e-5f));
        }
    }
}
