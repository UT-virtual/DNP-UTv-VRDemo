using System.Collections.Generic;
using DNPDemo.Interaction;
using DNPDemo.Scenario;
using DNPDemo.TaskHud;
using NUnit.Framework;

namespace DNPDemo.Tests
{
    public class TaskHudContentTests
    {
        static ScenarioStep Step(params InteractionPart[] parts) => new()
        {
            Id = "test",
            Actor = DemoRole.PlayerA,
            Completion = StepCompletion.Interaction,
            TaskText = "林檎を選んで手の下へ動かせ。",
            Interaction = new List<InteractionPart>(parts),
        };

        static ScenarioStep SelectThenManipulate() => Step(
            new DummyInteractionPart(PenMode.Select, "林檎を範囲選択する"),
            new DummyInteractionPart(PenMode.Manipulate, "林檎を手の下へ移動する"));

        [Test]
        public void ActorSeesTaskHeadingTaskTextAndEveryPart()
        {
            TaskHudContent content = TaskHudContent.Build(SelectThenManipulate(), 0, isLocalActor: true, PenMode.Select);

            Assert.IsTrue(content.IsVisible);
            Assert.IsTrue(content.IsLocalActor);
            Assert.AreEqual(TaskHudContent.ActorHeading, content.Heading);
            Assert.AreEqual("林檎を選んで手の下へ動かせ。", content.TaskText);
            Assert.AreEqual(2, content.Lines.Count);
            Assert.AreEqual("林檎を範囲選択する（範囲選択モード）", content.Lines[0].Text);
            Assert.AreEqual("林檎を手の下へ移動する（操作モード）", content.Lines[1].Text);
        }

        [Test]
        public void ObserverSeesWhoIsOperatingWithTheSameTaskAndParts()
        {
            TaskHudContent content = TaskHudContent.Build(SelectThenManipulate(), 1, isLocalActor: false, PenMode.Select);

            Assert.IsTrue(content.IsVisible);
            Assert.IsFalse(content.IsLocalActor);
            Assert.AreEqual("プレイヤーA が操作中", content.Heading);
            Assert.AreEqual("林檎を選んで手の下へ動かせ。", content.TaskText);
            Assert.AreEqual(2, content.Lines.Count);
            Assert.AreEqual(TaskHudLineState.Done, content.Lines[0].State);
            Assert.AreEqual(TaskHudLineState.Current, content.Lines[1].State);
        }

        [Test]
        public void PartsBeforeTheCurrentOneAreDoneAndLaterOnesPending()
        {
            ScenarioStep step = Step(
                new DummyInteractionPart(PenMode.Select, "a"),
                new DummyInteractionPart(PenMode.Voice, "b"),
                new DummyInteractionPart(PenMode.Draw, "c"));

            TaskHudContent content = TaskHudContent.Build(step, 1, isLocalActor: true, PenMode.Voice);

            CollectionAssert.AreEqual(
                new[] { TaskHudLineState.Done, TaskHudLineState.Current, TaskHudLineState.Pending },
                new[] { content.Lines[0].State, content.Lines[1].State, content.Lines[2].State });
        }

        [Test]
        public void ActorIsToldToPickTheModeOnlyWhileThePenIsInAnother()
        {
            ScenarioStep step = SelectThenManipulate();

            TaskHudContent wrongMode = TaskHudContent.Build(step, 1, isLocalActor: true, PenMode.Select);
            TaskHudContent rightMode = TaskHudContent.Build(step, 1, isLocalActor: true, PenMode.Manipulate);

            Assert.AreEqual("メニューの「空間ペン」で「操作」を選んでください", wrongMode.ModeHint);
            Assert.AreEqual(string.Empty, rightMode.ModeHint);
        }

        [Test]
        public void ObserverIsNeverToldToPickAMode()
        {
            TaskHudContent content = TaskHudContent.Build(SelectThenManipulate(), 1, isLocalActor: false, PenMode.Select);

            Assert.AreEqual(string.Empty, content.ModeHint);
        }

        [Test]
        public void ModesNotOnThePenMenuAreNeitherNamedNorHinted()
        {
            ScenarioStep step = Step(new DummyInteractionPart(PenMode.Panel, "履歴一覧から Ver 1.0 を呼び出す"));

            TaskHudContent content = TaskHudContent.Build(step, 0, isLocalActor: true, PenMode.Select);

            Assert.AreEqual("履歴一覧から Ver 1.0 を呼び出す", content.Lines[0].Text);
            Assert.AreEqual(string.Empty, content.ModeHint);
        }

        [Test]
        public void StepWithoutTaskTextShowsAnEmptyTaskText()
        {
            ScenarioStep step = SelectThenManipulate();
            step.TaskText = null;

            TaskHudContent content = TaskHudContent.Build(step, 0, isLocalActor: true, PenMode.Select);

            Assert.AreEqual(string.Empty, content.TaskText);
        }

        [TestCase(-1)]
        [TestCase(2)]
        public void HiddenWhenThePartIndexIsOutsideTheStep(int partIndex)
        {
            Assert.IsFalse(TaskHudContent.Build(SelectThenManipulate(), partIndex, isLocalActor: true, PenMode.Select).IsVisible);
        }

        [Test]
        public void HiddenWithoutAStep()
        {
            Assert.IsFalse(TaskHudContent.Build(null, 0, isLocalActor: true, PenMode.Select).IsVisible);
        }
    }
}
