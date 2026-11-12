using DNPDemo.Interaction;
using UnityEngine;

namespace DNPDemo.Pen
{
    /// <summary>
    /// Everything another device needs to draw a player's pen: its pose, its mode, the selection radius, and whether it
    /// is held with the trigger down (which lights the microphone indicator in voice mode).
    /// </summary>
    public struct PenState
    {
        // Below these the change is not worth a packet; the settle message sends the exact state afterwards.
        const float PositionTolerance = 0.002f;
        const float AngleTolerance = 0.5f;
        const float RadiusTolerance = 0.001f;

        public PenMode Mode;
        public float Radius;
        public bool IsHeld;
        public bool IsTriggerHeld;
        public Vector3 Position;
        public Quaternion Rotation;

        /// <summary>The mode, the held flag or the trigger flag differ: changes that should reach the other side at once.</summary>
        public readonly bool DiffersDiscretely(in PenState other) =>
            Mode != other.Mode || IsHeld != other.IsHeld || IsTriggerHeld != other.IsTriggerHeld;

        /// <summary>The states differ by more than what the other side could notice.</summary>
        public readonly bool DiffersVisibly(in PenState other) =>
            DiffersDiscretely(other)
            || Mathf.Abs(Radius - other.Radius) > RadiusTolerance
            || (Position - other.Position).sqrMagnitude > PositionTolerance * PositionTolerance
            || Quaternion.Angle(Rotation, other.Rotation) > AngleTolerance;
    }
}
