using System;
using UnityEngine;

namespace DNPDemo.Interaction
{
    /// <summary>
    /// One operation the actor performs in an interaction step: the kind of tool plus its parameters
    /// (zones, targets, constraints, the scripted sentence). A step lists its parts in order in
    /// <see cref="Scenario.ScenarioStep.Interaction"/>, for example range selection followed by a voice command.
    /// <para>
    /// Each tool defines its own subclass and an <see cref="InteractionTool"/> that runs it. Parts live in the scenario
    /// asset, so scene objects are referred to by key, not by reference.
    /// </para>
    /// </summary>
    [Serializable]
    public abstract class InteractionPart
    {
        [Tooltip("Instruction for this operation, shown on the task HUD.")]
        [TextArea(1, 3)]
        public string Instruction;

        /// <summary>Pen mode the actor needs for this part. The tool takes pen input only while the pen is in this mode.</summary>
        public abstract PenMode Mode { get; }

        /// <summary>One-line summary used as the inspector label.</summary>
        public abstract override string ToString();
    }
}
