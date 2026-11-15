using DNPDemo.Interaction;
using UnityEngine;

namespace DNPDemo.Pen
{
    /// <summary>
    /// What another device needs, besides the pose, to draw a player's pen: its mode, the selection radius, and
    /// whether it is held with the trigger down (which lights the microphone indicator in voice mode).
    /// The pose travels separately through Basis's pickup sync.
    /// </summary>
    public struct PenState
    {
        // Below this the change is not worth a packet; the settle message sends the exact state afterwards.
        const float RadiusTolerance = 0.001f;

        public PenMode Mode;
        public float Radius;
        public bool IsHeld;
        public bool IsTriggerHeld;

        /// <summary>The mode, the held flag or the trigger flag differ: changes that should reach the other side at once.</summary>
        public readonly bool DiffersDiscretely(in PenState other) =>
            Mode != other.Mode || IsHeld != other.IsHeld || IsTriggerHeld != other.IsTriggerHeld;

        /// <summary>The states differ by more than what the other side could notice.</summary>
        public readonly bool DiffersVisibly(in PenState other) =>
            DiffersDiscretely(other) || Mathf.Abs(Radius - other.Radius) > RadiusTolerance;
    }
}
