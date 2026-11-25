using System;
using System.Collections.Generic;
using DNPDemo.Environment;
using UnityEngine;

namespace DNPDemo.Interaction.Background
{
    /// <summary>
    /// Applying a generated background from the background panel. While the part is current, <see cref="TargetPreset"/>
    /// is listed on the panel on both devices; the actor selects it and presses「背景を適用」, which completes the part.
    /// The background itself changes at the head of the next step (<see cref="Scenario.Cues.EnvironmentCue"/>), so it
    /// follows from the step index like every other lasting state. See <see cref="BackgroundTool"/>.
    /// </summary>
    [Serializable]
    public sealed class BackgroundPart : InteractionPart
    {
        [Tooltip("Background generated for this part, which the actor applies. The next step's EnvironmentCue should switch to it.")]
        public EnvironmentPreset TargetPreset;

        [Tooltip("Open the background panel in front of the actor when the part begins.")]
        public bool OpensPanel = true;

        public BackgroundPart()
        {
        }

        public BackgroundPart(string instruction, EnvironmentPreset targetPreset, bool opensPanel = true)
        {
            Instruction = instruction;
            TargetPreset = targetPreset;
            OpensPanel = opensPanel;
        }

        public override PenMode Mode => PenMode.Panel;

        public override void Validate(List<string> problems)
        {
            if (TargetPreset == null)
            {
                problems.Add("適用する背景が未設定です。");
            }
        }

        public override string ToString() =>
            $"背景（{(TargetPreset != null ? TargetPreset.DisplayName : "背景が未設定")} を適用）{Instruction}";
    }
}
