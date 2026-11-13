using UnityEngine;

namespace DNPDemo.Interaction
{
    /// <summary>
    /// A region the tools test against: a single <see cref="DetectionZone"/> or a <see cref="DetectionZoneGroup"/>.
    /// Interaction parts name their zones by key, and the key may point at either.
    /// </summary>
    public interface IDetectionVolume
    {
        /// <summary>The world-space point lies inside the region.</summary>
        bool ContainsPoint(Vector3 worldPoint);

        /// <summary>The world-space sphere touches the region.</summary>
        bool OverlapsSphere(Vector3 sphereCenter, float sphereRadius);

        /// <summary>
        /// A world-space axis-aligned box that encloses the region. It may be larger than the region (a rotated shape);
        /// points that matter are tested with <see cref="ContainsPoint"/>.
        /// </summary>
        Bounds WorldBounds { get; }
    }

    public static class DetectionVolumes
    {
        /// <summary>
        /// The region on <paramref name="target"/>: its <see cref="DetectionZoneGroup"/> if it has one (the group also
        /// covers a zone on the same object), otherwise its <see cref="DetectionZone"/>. Null when it has neither.
        /// </summary>
        public static IDetectionVolume Find(GameObject target)
        {
            if (target == null)
            {
                return null;
            }
            if (target.TryGetComponent(out DetectionZoneGroup group))
            {
                return group;
            }
            return target.TryGetComponent(out DetectionZone zone) ? zone : null;
        }

        /// <summary>
        /// False for null and for a destroyed component. A plain <c>== null</c> on the interface does not see Unity's
        /// destroyed state.
        /// </summary>
        public static bool IsAlive(IDetectionVolume volume) =>
            volume is Object unityObject ? unityObject != null : volume != null;
    }
}
