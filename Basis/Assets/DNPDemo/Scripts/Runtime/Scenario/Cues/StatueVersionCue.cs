using System;
using System.Collections;
using System.Collections.Generic;
using DNPDemo.Venus;
using UnityEngine;

namespace DNPDemo.Scenario.Cues
{
    /// <summary>
    /// Shows <see cref="Version"/> on the <see cref="VenusStatue"/> bound to <see cref="StatueKey"/> (the main statue or the
    /// one on the comparison table). An empty version shows nothing. Persistent: playing it is a scripted change that a
    /// transition may animate, applying it instantly rebuilds the state of a later step.
    /// </summary>
    [Serializable]
    public sealed class StatueVersionCue : ScenarioCue
    {
        public string StatueKey = VenusStatueKeys.Main;
        public VenusVersion Version;

        public StatueVersionCue()
        {
        }

        public StatueVersionCue(string statueKey, VenusVersion version)
        {
            StatueKey = statueKey;
            Version = version;
        }

        public override IEnumerator Play(ScenarioCueContext context)
        {
            Apply(context, animate: true);
            yield break;
        }

        public override void ApplyInstant(ScenarioCueContext context) => Apply(context, animate: false);

        void Apply(ScenarioCueContext context, bool animate)
        {
            if (!context.Stage.TryGetObject(StatueKey, out GameObject target))
            {
                return;
            }
            if (!target.TryGetComponent(out VenusStatue statue))
            {
                Debug.LogWarning($"[DNPDemo] キー「{StatueKey}」のオブジェクトに VenusStatue がありません。", target);
                return;
            }
            statue.Show(Version, animate);
        }

        public override void CollectRequirements(ScenarioRequirements requirements)
        {
            if (!string.IsNullOrEmpty(StatueKey))
            {
                requirements.ObjectKeys.Add(StatueKey);
            }
        }

        public override void Validate(List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(StatueKey))
            {
                problems.Add("像のキーが空です。");
            }
        }

        public override string ToString() => $"{StatueKey} に {(Version != null ? Version.ToString() : "何も表示しない")}";
    }
}
