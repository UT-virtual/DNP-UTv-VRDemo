using System.Collections.Generic;
using DNPDemo.Interaction;
using DNPDemo.Menu;
using DNPDemo.Pen;
using DNPDemo.Scenario;

namespace DNPDemo.TaskHud
{
    /// <summary>Where a part of the step stands, as the task HUD marks it.</summary>
    public enum TaskHudLineState : byte
    {
        Done = 0,
        Current = 1,
        Pending = 2,
    }

    /// <summary>One part of the step on the task HUD: its instruction and how far it is.</summary>
    public readonly struct TaskHudLine
    {
        public TaskHudLine(string text, TaskHudLineState state)
        {
            Text = text;
            State = state;
        }

        public string Text { get; }
        public TaskHudLineState State { get; }
    }

    /// <summary>
    /// What the task HUD shows, derived from the running step (<see cref="InteractionRunner"/>). Both players see the
    /// step's task text and its parts in order, marked done / current / pending. The heading tells them apart: the actor
    /// reads "TASK", the other player reads who is operating. Only the actor is told to switch the pen's mode when it
    /// does not match the current part.
    /// </summary>
    public sealed class TaskHudContent
    {
        public const string ActorHeading = "TASK";

        public static readonly TaskHudContent Hidden = new(false, false, string.Empty, string.Empty, new TaskHudLine[0], string.Empty);

        TaskHudContent(bool isVisible, bool isLocalActor, string heading, string taskText, IReadOnlyList<TaskHudLine> lines, string modeHint)
        {
            IsVisible = isVisible;
            IsLocalActor = isLocalActor;
            Heading = heading;
            TaskText = taskText;
            Lines = lines;
            ModeHint = modeHint;
        }

        /// <summary>False while no part is waiting: the HUD is hidden.</summary>
        public bool IsVisible { get; }

        /// <summary>This device performs the step.</summary>
        public bool IsLocalActor { get; }

        /// <summary>"TASK" for the actor, "&lt;role&gt; が操作中" for the other player.</summary>
        public string Heading { get; }

        /// <summary>The step's task text (<see cref="ScenarioStep.TaskText"/>). Empty when the step has none.</summary>
        public string TaskText { get; }

        /// <summary>One line per part of the step, in order.</summary>
        public IReadOnlyList<TaskHudLine> Lines { get; }

        /// <summary>Actor only: which mode to pick on the menu, while the pen is in another mode. Empty otherwise.</summary>
        public string ModeHint { get; }

        /// <summary>
        /// Builds the content for <paramref name="step"/> with the part at <paramref name="currentPartIndex"/> running.
        /// Returns <see cref="Hidden"/> when there is no step or the index is outside its parts.
        /// </summary>
        public static TaskHudContent Build(ScenarioStep step, int currentPartIndex, bool isLocalActor, PenMode penMode)
        {
            List<InteractionPart> parts = step?.Interaction;
            if (parts == null || currentPartIndex < 0 || currentPartIndex >= parts.Count)
            {
                return Hidden;
            }

            var lines = new TaskHudLine[parts.Count];
            for (int i = 0; i < parts.Count; i++)
            {
                TaskHudLineState state = i < currentPartIndex ? TaskHudLineState.Done
                    : i == currentPartIndex ? TaskHudLineState.Current
                    : TaskHudLineState.Pending;
                lines[i] = new TaskHudLine(LineText(parts[i]), state);
            }

            string heading = isLocalActor || step.Actor == DemoRole.None
                ? ActorHeading
                : $"{step.Actor.ToDisplayName()} が操作中";
            string modeHint = isLocalActor ? ModeHintFor(parts[currentPartIndex], penMode) : string.Empty;
            return new TaskHudContent(true, isLocalActor, heading, step.TaskText ?? string.Empty, lines, modeHint);
        }

        static string LineText(InteractionPart part)
        {
            if (part == null)
            {
                return string.Empty;
            }
            string instruction = part.Instruction ?? string.Empty;
            // Only the modes chosen on the pen menu are named; the others need no choice from the player.
            return PenModeSelection.IsSelectable(part.Mode)
                ? $"{instruction}（{part.Mode.ToDisplayName()}モード）"
                : instruction;
        }

        static string ModeHintFor(InteractionPart part, PenMode penMode)
        {
            if (part == null || !PenModeSelection.IsSelectable(part.Mode) || part.Mode == penMode)
            {
                return string.Empty;
            }
            return $"メニューの「{PenMenuProvider.MenuTitle}」で「{part.Mode.ToDisplayName()}」を選んでください";
        }
    }
}
