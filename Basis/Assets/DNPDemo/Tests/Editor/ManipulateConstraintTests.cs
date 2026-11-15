using DNPDemo.Interaction.Manipulate;
using NUnit.Framework;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class ManipulateConstraintTests
    {
        const float Tolerance = 1e-4f;

        static readonly Pose Start = new(new Vector3(0f, 1f, 0f), Quaternion.identity);

        static ManipulateConstraint Translate(float maxDistance = 0.5f, float snapDistance = 0.05f) =>
            ManipulateConstraint.Translate(Start, new Pose(new Vector3(0f, 0.8f, 0f), Quaternion.Euler(0f, 90f, 0f)), maxDistance, snapDistance);

        // The object starts 0.5m along +X from a pivot at the origin and turns around +Y.
        static ManipulateConstraint Rotate(RotateCompletion completion, float min = -90f, float max = 90f,
            float target = 45f, float snap = 10f, float threshold = 30f) =>
            ManipulateConstraint.Rotate(new Pose(new Vector3(0.5f, 0f, 0f), Quaternion.identity), Vector3.zero, Vector3.up,
                min, max, completion, target, snap, threshold);

        static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), Tolerance, $"expected {expected}, got {actual}");
        }

        [Test]
        public void TranslationMovesWithTheTipAndKeepsItsRotation()
        {
            ManipulateConstraint constraint = Translate();
            var drag = new ManipulateDrag(constraint, default, new Vector3(1f, 1f, 1f));
            ManipulateValue value = drag.Update(new Vector3(1.1f, 0.9f, 1f));
            Pose pose = constraint.PoseOf(value);
            AssertVector(new Vector3(0.1f, 0.9f, 0f), pose.position);
            Assert.AreEqual(Quaternion.identity, pose.rotation);
        }

        [Test]
        public void TranslationStaysWithinTheMaximumDistance()
        {
            ManipulateConstraint constraint = Translate(maxDistance: 0.3f);
            var drag = new ManipulateDrag(constraint, default, Vector3.zero);
            ManipulateValue value = drag.Update(new Vector3(0f, 0f, 2f));
            AssertVector(new Vector3(0f, 0f, 0.3f), value.Offset);
        }

        [Test]
        public void TranslationContinuesFromTheValueAtTheGrab()
        {
            ManipulateConstraint constraint = Translate();
            var drag = new ManipulateDrag(constraint, ManipulateValue.Translation(new Vector3(0f, -0.1f, 0f)), Vector3.zero);
            ManipulateValue value = drag.Update(new Vector3(0f, -0.05f, 0f));
            AssertVector(new Vector3(0f, -0.15f, 0f), value.Offset);
        }

        [Test]
        public void TranslationReleasedNearTheGoalSnapsToItsPose()
        {
            ManipulateConstraint constraint = Translate();
            Assert.IsTrue(constraint.TryComplete(ManipulateValue.Translation(new Vector3(0.03f, -0.19f, 0f)), out ManipulateValue result));
            Assert.IsTrue(result.Snapped);
            Pose pose = constraint.PoseOf(result);
            AssertVector(new Vector3(0f, 0.8f, 0f), pose.position);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 90f, 0f), pose.rotation), 0.01f);
        }

        [Test]
        public void TranslationReleasedFarFromTheGoalIsNotDone()
        {
            Assert.IsFalse(Translate().TryComplete(ManipulateValue.Translation(new Vector3(0f, -0.1f, 0f)), out _));
        }

        [Test]
        public void GoalBeyondTheMaximumDistanceIsReported()
        {
            Assert.IsTrue(Translate(maxDistance: 0.2f).IsGoalReachable);
            Assert.IsFalse(Translate(maxDistance: 0.1f, snapDistance: 0.05f).IsGoalReachable);
        }

        [Test]
        public void RotationFollowsTheTipAroundTheAxis()
        {
            ManipulateConstraint constraint = Rotate(RotateCompletion.Snap);
            var drag = new ManipulateDrag(constraint, default, new Vector3(1f, 0f, 0f));
            // The measured turn uses the same sign as Quaternion.AngleAxis, so the object follows the tip exactly.
            Vector3 tip = Quaternion.AngleAxis(30f, Vector3.up) * new Vector3(1f, 0f, 0f);
            ManipulateValue value = drag.Update(tip);
            Assert.AreEqual(30f, value.Angle, 0.01f);
            AssertVector(Quaternion.AngleAxis(30f, Vector3.up) * new Vector3(0.5f, 0f, 0f), constraint.PoseOf(value).position);
        }

        [Test]
        public void RotationAddsUpPastHalfATurn()
        {
            ManipulateConstraint constraint = Rotate(RotateCompletion.Snap, min: -360f, max: 360f, target: 270f);
            var drag = new ManipulateDrag(constraint, default, new Vector3(1f, 0f, 0f));
            ManipulateValue value = default;
            for (int degrees = 10; degrees <= 270; degrees += 10)
            {
                value = drag.Update(Quaternion.AngleAxis(degrees, Vector3.up) * new Vector3(1f, 0f, 0f));
            }
            Assert.AreEqual(270f, value.Angle, 0.01f);
        }

        [Test]
        public void RotationStopsAtTheLimitAndTurnsBackAtOnce()
        {
            ManipulateConstraint constraint = Rotate(RotateCompletion.Snap, min: -20f, max: 40f);
            var drag = new ManipulateDrag(constraint, default, new Vector3(1f, 0f, 0f));
            drag.Update(Quaternion.AngleAxis(30f, Vector3.up) * Vector3.right);
            ManipulateValue atLimit = drag.Update(Quaternion.AngleAxis(60f, Vector3.up) * Vector3.right);
            Assert.AreEqual(40f, atLimit.Angle, 0.01f);
            ManipulateValue back = drag.Update(Quaternion.AngleAxis(50f, Vector3.up) * Vector3.right);
            Assert.AreEqual(30f, back.Angle, 0.01f);
        }

        [Test]
        public void TipNearTheAxisDoesNotTurnTheObject()
        {
            ManipulateConstraint constraint = Rotate(RotateCompletion.Snap);
            var drag = new ManipulateDrag(constraint, default, new Vector3(1f, 0f, 0f));
            ManipulateValue value = drag.Update(new Vector3(0f, 0.3f, 0.005f));
            Assert.AreEqual(0f, value.Angle, 0.01f);
            Assert.IsFalse(constraint.TryMeasureTurn(Vector3.right, new Vector3(0.01f, 0.5f, 0f), out _));
        }

        [Test]
        public void RotationReleasedNearTheTargetSnapsToIt()
        {
            ManipulateConstraint constraint = Rotate(RotateCompletion.Snap, target: 45f, snap: 10f);
            Assert.IsTrue(constraint.TryComplete(ManipulateValue.Rotation(38f), out ManipulateValue result));
            Assert.IsTrue(result.Snapped);
            Assert.AreEqual(45f, result.Angle);
            Assert.IsFalse(constraint.TryComplete(ManipulateValue.Rotation(30f), out _));
        }

        [Test]
        public void RotationPastTheThresholdIsDoneWhereItWasReleased()
        {
            ManipulateConstraint positive = Rotate(RotateCompletion.Threshold, threshold: 30f);
            Assert.IsTrue(positive.TryComplete(ManipulateValue.Rotation(35f), out ManipulateValue result));
            Assert.IsFalse(result.Snapped);
            Assert.AreEqual(35f, result.Angle);
            Assert.IsFalse(positive.TryComplete(ManipulateValue.Rotation(25f), out _));

            ManipulateConstraint negative = Rotate(RotateCompletion.Threshold, threshold: -30f);
            Assert.IsTrue(negative.TryComplete(ManipulateValue.Rotation(-31f), out _));
            Assert.IsFalse(negative.TryComplete(ManipulateValue.Rotation(31f), out _));
        }

        [Test]
        public void GuidePoseShowsWhereTheObjectShouldGo()
        {
            AssertVector(new Vector3(0f, 0.8f, 0f), Translate().GuidePose.position);
            AssertVector(Quaternion.AngleAxis(45f, Vector3.up) * new Vector3(0.5f, 0f, 0f), Rotate(RotateCompletion.Snap).GuidePose.position);
            AssertVector(Quaternion.AngleAxis(30f, Vector3.up) * new Vector3(0.5f, 0f, 0f), Rotate(RotateCompletion.Threshold).GuidePose.position);
        }

        [Test]
        public void ClampKeepsReceivedValuesInRange()
        {
            Assert.AreEqual(90f, Rotate(RotateCompletion.Snap).Clamp(ManipulateValue.Rotation(500f)).Angle);
            AssertVector(new Vector3(0.5f, 0f, 0f), Translate().Clamp(ManipulateValue.Translation(new Vector3(3f, 0f, 0f))).Offset);
            // A snapped translation always lands on the goal, whatever offset came with it.
            AssertVector(new Vector3(0f, -0.2f, 0f), Translate().Clamp(ManipulateValue.Translation(Vector3.zero, snapped: true)).Offset);
        }

        [Test]
        public void RangeAlwaysIncludesTheStartingPose()
        {
            ManipulateConstraint constraint = Rotate(RotateCompletion.Snap, min: 10f, max: 40f);
            Assert.AreEqual(0f, constraint.ClampAngle(0f));
        }
    }
}
