using System;
using System.Collections.Generic;
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

        [Tooltip("Hint shown to both players when this operation is not done after a while.")]
        public InteractionHint Hint = new();

        /// <summary>Pen mode the actor needs for this part. The tool takes pen input only while the pen is in this mode.</summary>
        public abstract PenMode Mode { get; }

        /// <summary>
        /// Adds the keys of the zones that light up with the hint: <see cref="InteractionHint.ZoneKeys"/> when set,
        /// otherwise the part's own zones (<see cref="CollectDefaultHintZoneKeys"/>).
        /// </summary>
        public void CollectHintZoneKeys(List<string> keys)
        {
            if (Hint != null && Hint.HasCustomZones)
            {
                foreach (string key in Hint.ZoneKeys)
                {
                    AddHintZoneKey(keys, key);
                }
                return;
            }
            CollectDefaultHintZoneKeys(keys);
        }

        /// <summary>Adds the keys of the zones the operation is about, which light up with the hint unless it names its own.</summary>
        protected virtual void CollectDefaultHintZoneKeys(List<string> keys)
        {
        }

        /// <summary>Adds <paramref name="key"/> to <paramref name="keys"/> unless it is blank or already there.</summary>
        protected static void AddHintZoneKey(List<string> keys, string key)
        {
            if (!string.IsNullOrWhiteSpace(key) && !keys.Contains(key))
            {
                keys.Add(key);
            }
        }

        /// <summary>Adds the scene bindings (object keys) this part addresses, so the scene can be checked for them.</summary>
        public virtual void CollectRequirements(Scenario.ScenarioRequirements requirements)
        {
        }

        /// <summary>Adds problems in the part's parameters that would break a run (for example a missing zone key).</summary>
        public virtual void Validate(List<string> problems)
        {
        }

        /// <summary>One-line summary used as the inspector label.</summary>
        public abstract override string ToString();
    }
}
