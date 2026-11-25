using System;
using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Interaction
{
    /// <summary>
    /// The hint of an interaction part (<see cref="InteractionPart.Hint"/>): when the part is still not done
    /// <see cref="DelaySeconds"/> after it began, both players see <see cref="Text"/> on the task HUD and the part's zones
    /// light up in the scene (see <see cref="Hint.InteractionHintPresenter"/>).
    /// </summary>
    [Serializable]
    public sealed class InteractionHint
    {
        public const float DefaultDelaySeconds = 20f;

        [Tooltip("Seconds after the part begins before the hint appears. 0: no hint for this part.")]
        [Min(0f)] public float DelaySeconds = DefaultDelaySeconds;

        [Tooltip("Hint shown on the task HUD. Empty: only the zones light up.")]
        [TextArea(1, 3)]
        public string Text;

        [Tooltip("Keys (ScenarioObjectBinding) of the zones that light up. Each key's object carries a DetectionZoneGroup or a DetectionZone. Empty: the part's own zones (the selection's targets, the guide's start and end, the grab zone).")]
        public List<string> ZoneKeys = new();

        public InteractionHint()
        {
        }

        public InteractionHint(string text, float delaySeconds = DefaultDelaySeconds)
        {
            Text = text;
            DelaySeconds = delaySeconds;
        }

        /// <summary>False when the part shows no hint (<see cref="DelaySeconds"/> is 0).</summary>
        public bool IsEnabled => DelaySeconds > 0f;

        /// <summary>True when <see cref="ZoneKeys"/> names at least one zone, replacing the part's own zones.</summary>
        public bool HasCustomZones
        {
            get
            {
                if (ZoneKeys == null)
                {
                    return false;
                }
                foreach (string key in ZoneKeys)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>Adds the zone keys set on the hint. The part's own zones are already required by the part.</summary>
        public void CollectRequirements(Scenario.ScenarioRequirements requirements)
        {
            if (ZoneKeys == null)
            {
                return;
            }
            foreach (string key in ZoneKeys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    requirements.ObjectKeys.Add(key);
                }
            }
        }

        public void Validate(List<string> problems)
        {
            if (!(DelaySeconds >= 0f))
            {
                problems.Add("ヒントの待ち時間は 0 以上にしてください。");
            }
            if (ZoneKeys == null)
            {
                return;
            }
            var seen = new HashSet<string>();
            for (int i = 0; i < ZoneKeys.Count; i++)
            {
                string key = ZoneKeys[i];
                if (string.IsNullOrWhiteSpace(key))
                {
                    problems.Add($"ヒントのゾーン {i} のキーが空です。");
                }
                else if (!seen.Add(key))
                {
                    problems.Add($"ヒントのゾーン「{key}」が重複しています。");
                }
            }
        }
    }
}
