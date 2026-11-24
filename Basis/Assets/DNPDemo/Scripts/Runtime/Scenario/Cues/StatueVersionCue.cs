using System;
using System.Collections;
using System.Collections.Generic;
using DNPDemo.Interaction;
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

        [Tooltip("Key of the object whose extent the transition centres on: the part that changed. A DetectionZone or DetectionZoneGroup gives its volume, anything else the bounds of its renderers. Empty: the whole statue.")]
        public string FocusKey;

        public StatueVersionCue()
        {
        }

        public StatueVersionCue(string statueKey, VenusVersion version, string focusKey = null)
        {
            StatueKey = statueKey;
            Version = version;
            FocusKey = focusKey;
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
            // Bounds set for a change that does not happen would carry over to the next one.
            if (animate && statue.Current != Version && TryGetFocusBounds(context, out Bounds focus)
                && statue.TryGetComponent(out ModelTransition transition))
            {
                transition.SetBounds(focus);
            }
            statue.Show(Version, animate);
        }

        bool TryGetFocusBounds(ScenarioCueContext context, out Bounds bounds)
        {
            bounds = default;
            if (string.IsNullOrWhiteSpace(FocusKey) || !context.Stage.TryGetObject(FocusKey, out GameObject focus))
            {
                return false;
            }
            IDetectionVolume volume = DetectionVolumes.Find(focus);
            if (DetectionVolumes.IsAlive(volume))
            {
                bounds = volume.WorldBounds;
                return true;
            }
            bool any = false;
            foreach (Renderer renderer in focus.GetComponentsInChildren<Renderer>())
            {
                if (any)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    any = true;
                }
            }
            return any;
        }

        public override void CollectRequirements(ScenarioRequirements requirements)
        {
            if (!string.IsNullOrEmpty(StatueKey))
            {
                requirements.ObjectKeys.Add(StatueKey);
            }
            if (!string.IsNullOrWhiteSpace(FocusKey))
            {
                requirements.ObjectKeys.Add(FocusKey);
            }
        }

        public override void Validate(List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(StatueKey))
            {
                problems.Add("像のキーが空です。");
            }
        }

        public override string ToString() =>
            $"{StatueKey} に {(Version != null ? Version.ToString() : "何も表示しない")}" +
            (string.IsNullOrWhiteSpace(FocusKey) ? "" : $"（{FocusKey} を中心に演出）");
    }
}
