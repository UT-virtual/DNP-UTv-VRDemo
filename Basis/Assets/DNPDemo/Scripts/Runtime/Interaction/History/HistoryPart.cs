using System;
using System.Collections.Generic;
using DNPDemo.Venus;
using UnityEngine;

namespace DNPDemo.Interaction.History
{
    /// <summary>
    /// Calling a past version back from the history panel: the actor opens「モデルの履歴」from the main menu, steps to a
    /// version and presses「モデルを復元」. Any version restored is shown on the comparison table on both devices; the
    /// part completes when it is <see cref="TargetVersion"/>. See <see cref="HistoryTool"/>.
    /// </summary>
    [Serializable]
    public sealed class HistoryPart : InteractionPart
    {
        [Tooltip("Version the actor has to restore. Restoring it completes the part.")]
        public VenusVersion TargetVersion;

        [Tooltip("Key of the VenusStatue that shows the restored version (the statue on the comparison table).")]
        public string ComparisonStatueKey = VenusStatueKeys.Comparison;

        public HistoryPart()
        {
        }

        public HistoryPart(string instruction, VenusVersion targetVersion, string comparisonStatueKey = VenusStatueKeys.Comparison)
        {
            Instruction = instruction;
            TargetVersion = targetVersion;
            ComparisonStatueKey = comparisonStatueKey;
        }

        public override PenMode Mode => PenMode.Panel;

        public override void CollectRequirements(Scenario.ScenarioRequirements requirements)
        {
            if (!string.IsNullOrEmpty(ComparisonStatueKey))
            {
                requirements.ObjectKeys.Add(ComparisonStatueKey);
            }
        }

        public override void Validate(List<string> problems)
        {
            if (TargetVersion == null)
            {
                problems.Add("履歴から復元する版が未設定です。");
            }
            if (string.IsNullOrWhiteSpace(ComparisonStatueKey))
            {
                problems.Add("復元した版を表示する像のキーが空です。");
            }
        }

        public override string ToString() =>
            $"履歴（{(TargetVersion != null ? TargetVersion.ToString() : "版が未設定")} を復元）{Instruction}";
    }
}
