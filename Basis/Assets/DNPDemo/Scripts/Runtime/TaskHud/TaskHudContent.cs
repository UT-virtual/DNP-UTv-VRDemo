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
    /// does not match the current part. The running tool may add a status line (<see cref="InteractionRunner.Status"/>),
    /// such as "音声入力中…". When the step has gone on for a while, its hint (<see cref="InteractionHint"/>) is added
    /// for both players.
    /// </summary>
    public sealed class TaskHudContent
    {
        public const string ActorHeading = "TASK";

        /// <summary>Put in front of the hint. It must be in the UI font's character set.</summary>
        public const string HintPrefix = "ヒント：";

        public static readonly TaskHudContent Hidden = new(false, false, string.Empty, string.Empty, new TaskHudLine[0], string.Empty, string.Empty,
            string.Empty);

        TaskHudContent(bool isVisible, bool isLocalActor, string heading, string taskText, IReadOnlyList<TaskHudLine> lines, string status,
            string modeHint, string hint)
        {
            IsVisible = isVisible;
            IsLocalActor = isLocalActor;
            Heading = heading;
            TaskText = taskText;
            Lines = lines;
            Status = status;
            ModeHint = modeHint;
            Hint = hint;
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

        /// <summary>How the current operation is going on this device, from the running tool. Empty when there is none.</summary>
        public string Status { get; }

        /// <summary>Actor only: which mode to pick on the menu, while the pen is in another mode. Empty otherwise.</summary>
        public string ModeHint { get; }

        /// <summary>Both players: the running part's hint with <see cref="HintPrefix"/>, once it is out. Empty otherwise.</summary>
        public string Hint { get; }

        /// <summary>
        /// Builds the content for <paramref name="step"/> with the part at <paramref name="currentPartIndex"/> running.
        /// <paramref name="status"/> is the running tool's status line and <paramref name="hint"/> the text of the hint
        /// that is out (<see cref="Interaction.Hint.InteractionHintPresenter.Text"/>); either may be null or empty.
        /// Returns <see cref="Hidden"/> when there is no step or the index is outside its parts.
        /// </summary>
        public static TaskHudContent Build(ScenarioStep step, int currentPartIndex, bool isLocalActor, PenMode penMode, string status = null,
            string hint = null)
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
            string hintLine = string.IsNullOrWhiteSpace(hint) ? string.Empty : HintPrefix + hint.Trim();
            return new TaskHudContent(true, isLocalActor, heading, step.TaskText ?? string.Empty, lines, status ?? string.Empty, modeHint,
                hintLine);
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
