using System;
using System.Collections.Generic;
using DNPDemo.Scenario;
using UnityEngine;

namespace DNPDemo.Interaction.Selection
{
    /// <summary>
    /// Range selection: while the trigger is held, the pen's tip sphere marks the path it sweeps; releasing it confirms
    /// one selection, which <see cref="SelectionTool"/> judges against <see cref="TargetZoneKeys"/>
    /// (see <see cref="SelectionJudge"/> for the rule). A failed selection is cleared and the actor tries again.
    /// </summary>
    [Serializable]
    public sealed class SelectionPart : InteractionPart
    {
        [Tooltip("Keys (ScenarioObjectBinding) of the zones to select. Each key's object carries a DetectionZoneGroup or a DetectionZone. Every zone must be touched.")]
        public List<string> TargetZoneKeys = new();

        [Tooltip("Share of the selection's spheres that must touch a target zone. Below 1, a little overshoot is tolerated.")]
        [Range(0f, 1f)] public float RequiredInsideRatio = 0.9f;

        [Tooltip("Share of each target zone's volume the selection must cover, so a small selection inside the zone is not enough.")]
        [Range(0f, 1f)] public float RequiredCoverage = 0.5f;

        public SelectionCriteria Criteria => new(RequiredInsideRatio, RequiredCoverage);

        public SelectionPart()
        {
        }

        public SelectionPart(string instruction, params string[] targetZoneKeys)
        {
            Instruction = instruction;
            TargetZoneKeys = new List<string>(targetZoneKeys);
        }

        public override PenMode Mode => PenMode.Select;

        public override void CollectRequirements(ScenarioRequirements requirements)
        {
            if (TargetZoneKeys == null)
            {
                return;
            }
            foreach (string key in TargetZoneKeys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    requirements.ObjectKeys.Add(key);
                }
            }
        }

        public override void Validate(List<string> problems)
        {
            if (TargetZoneKeys == null || TargetZoneKeys.Count == 0)
            {
                problems.Add("範囲選択の対象ゾーンがありません。");
                return;
            }
            var seen = new HashSet<string>();
            for (int i = 0; i < TargetZoneKeys.Count; i++)
            {
                string key = TargetZoneKeys[i];
                if (string.IsNullOrWhiteSpace(key))
                {
                    problems.Add($"範囲選択の対象ゾーン {i} のキーが空です。");
                }
                else if (!seen.Add(key))
                {
                    problems.Add($"範囲選択の対象ゾーン「{key}」が重複しています。");
                }
            }
        }

        public override string ToString()
        {
            string targets = TargetZoneKeys != null && TargetZoneKeys.Count > 0 ? string.Join("・", TargetZoneKeys) : "対象なし";
            return $"範囲選択（{targets}）{Instruction}";
        }
    }
}
