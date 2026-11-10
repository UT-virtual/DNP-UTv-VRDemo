using System;
using UnityEngine;

namespace DNPDemo.Interaction
{
    /// <summary>
    /// Stand-in for a part whose tool does not exist yet: the actor presses a button <see cref="RequiredPresses"/> times.
    /// It exercises the whole path (pen mode, progress sync, part order, step completion) until the real tool replaces it.
    /// </summary>
    [Serializable]
    public sealed class DummyInteractionPart : InteractionPart
    {
        [Tooltip("Pen mode of the real part this one stands in for.")]
        public PenMode SimulatedMode;

        [Min(1)] public int RequiredPresses = 3;

        public DummyInteractionPart()
        {
        }

        public DummyInteractionPart(PenMode simulatedMode, string instruction, int requiredPresses = 3)
        {
            SimulatedMode = simulatedMode;
            Instruction = instruction;
            RequiredPresses = requiredPresses;
        }

        public override PenMode Mode => SimulatedMode;

        public override string ToString() => $"仮（{SimulatedMode.ToDisplayName()}）{Instruction}";
    }
}
