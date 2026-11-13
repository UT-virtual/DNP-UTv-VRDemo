using System.Collections.Generic;
using DNPDemo.Interaction;
using DNPDemo.Interaction.Manipulate;
using DNPDemo.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class ManipulatePartTests
    {
        static List<string> Problems(ManipulatePart part)
        {
            var problems = new List<string>();
            part.Validate(problems);
            return problems;
        }

        [Test]
        public void WellFormedPartsHaveNoProblems()
        {
            Assert.IsEmpty(Problems(ManipulatePart.Translate("move", "Apple", "Goal")));
            Assert.IsEmpty(Problems(ManipulatePart.RotateToTarget("turn", "Arm", "Shoulder", Vector3.up, -60f, 60f, 30f)));
            Assert.IsEmpty(Problems(ManipulatePart.RotatePastThreshold("slide", "Arm", "Shoulder", Vector3.forward, -90f, 0f, -30f)));
        }

        [Test]
        public void UsesManipulateMode()
        {
            Assert.AreEqual(PenMode.Manipulate, new ManipulatePart().Mode);
        }

        [Test]
        public void MissingKeysAreReported()
        {
            Assert.IsNotEmpty(Problems(ManipulatePart.Translate("move", "", "Goal")));
            Assert.IsNotEmpty(Problems(ManipulatePart.Translate("move", "Apple", " ")));
            Assert.IsNotEmpty(Problems(ManipulatePart.RotateToTarget("turn", "Arm", null, Vector3.up, -60f, 60f, 30f)));
        }

        [Test]
        public void BrokenRotationParametersAreReported()
        {
            Assert.IsNotEmpty(Problems(ManipulatePart.RotateToTarget("zero axis", "Arm", "Shoulder", Vector3.zero, -60f, 60f, 30f)));
            Assert.IsNotEmpty(Problems(ManipulatePart.RotateToTarget("range misses 0", "Arm", "Shoulder", Vector3.up, 10f, 60f, 30f)));
            Assert.IsNotEmpty(Problems(ManipulatePart.RotateToTarget("target outside", "Arm", "Shoulder", Vector3.up, -60f, 60f, 90f)));
            Assert.IsNotEmpty(Problems(ManipulatePart.RotatePastThreshold("zero threshold", "Arm", "Shoulder", Vector3.up, -60f, 60f, 0f)));
            Assert.IsNotEmpty(Problems(ManipulatePart.RotatePastThreshold("threshold outside", "Arm", "Shoulder", Vector3.up, -60f, 60f, 70f)));
        }

        [Test]
        public void RequirementsNameTheKeysTheConstraintUses()
        {
            var translate = new ScenarioRequirements();
            ManipulatePart.Translate("move", "Apple", "Goal").CollectRequirements(translate);
            CollectionAssert.AreEquivalent(new[] { "Apple", "Goal" }, translate.ObjectKeys);

            var rotate = new ScenarioRequirements();
            ManipulatePart part = ManipulatePart.RotateToTarget("turn", "Arm", "Shoulder", Vector3.up, -60f, 60f, 30f);
            part.GrabZoneKey = "Zone_Arm";
            part.CollectRequirements(rotate);
            CollectionAssert.AreEquivalent(new[] { "Arm", "Shoulder", "Zone_Arm" }, rotate.ObjectKeys);
        }

        [Test]
        public void GrabZoneDefaultsToTheTarget()
        {
            ManipulatePart part = ManipulatePart.Translate("move", "Apple", "Goal");
            Assert.AreEqual("Apple", part.EffectiveGrabZoneKey);
            part.GrabZoneKey = "Zone_Apple";
            Assert.AreEqual("Zone_Apple", part.EffectiveGrabZoneKey);
        }
    }
}
