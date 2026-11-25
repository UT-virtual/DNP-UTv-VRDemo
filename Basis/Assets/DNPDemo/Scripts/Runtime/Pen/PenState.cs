using DNPDemo.Interaction;
using UnityEngine;

namespace DNPDemo.Pen
{
    /// <summary>
    /// What another device needs, besides the pose, to draw a player's pen: its mode, the selection radius, the
    /// length set in the other modes, and whether it is held with the trigger down (which lights the microphone
    /// indicator in voice mode). The pose travels separately through Basis's pickup sync.
    /// </summary>
    public struct PenState
    {
        // Below these the change is not worth a packet; the settle message sends the exact state afterwards.
        const float RadiusTolerance = 0.001f;
        const float ExtensionTolerance = 0.001f;

        public PenMode Mode;
        public float Radius;

        /// <summary>
        /// Extension (0..1) the player set with the stick for the modes other than select. Select mode derives the
        /// length from <see cref="Radius"/> instead; this value is kept meanwhile and comes back on leaving select mode.
        /// </summary>
        public float Extension;

        public bool IsHeld;
        public bool IsTriggerHeld;

        /// <summary>The extension the body shows in <see cref="Mode"/>.</summary>
        public readonly float BodyExtension =>
            Mode == PenMode.Select ? PenGeometry.ExtensionForRadius(Radius) : Mathf.Clamp01(Extension);

        /// <summary>The mode, the held flag or the trigger flag differ: changes that should reach the other side at once.</summary>
        public readonly bool DiffersDiscretely(in PenState other) =>
            Mode != other.Mode || IsHeld != other.IsHeld || IsTriggerHeld != other.IsTriggerHeld;

        /// <summary>The states differ by more than what the other side could notice.</summary>
        public readonly bool DiffersVisibly(in PenState other) =>
            DiffersDiscretely(other)
            || Mathf.Abs(Radius - other.Radius) > RadiusTolerance
            || Mathf.Abs(Extension - other.Extension) > ExtensionTolerance;
    }
}
