using UnityEngine;

namespace DNPDemo.Pen
{
    /// <summary>
    /// Shape of the pen model (<c>Art/Pen/FBXs/Pen.fbx</c>) and the rules that derive its pose-independent geometry
    /// from the selection radius. Lengths are in the pen's local space, where the butt sits at the origin and the tip
    /// points along +X.
    /// </summary>
    public static class PenGeometry
    {
        /// <summary>Length of <c>PenBody</c> with the <c>Extension</c> blend shape at 0.</summary>
        public const float BodyLength = 0.30f;

        /// <summary>Extra length <c>PenBody</c> gains with the <c>Extension</c> blend shape at 1.</summary>
        public const float ExtensionLength = 1.00f;

        /// <summary>Radius of the <c>PenSelectArea</c> mesh at scale 1.</summary>
        public const float ModelSelectRadius = 0.05f;

        public const float MinSelectRadius = 0.025f;
        public const float MaxSelectRadius = 0.5f;
        public const float DefaultSelectRadius = ModelSelectRadius;

        /// <summary>
        /// The body starts to extend above this radius and reaches full extension at <see cref="MaxSelectRadius"/>,
        /// so a large sphere stays clear of the hand holding the pen.
        /// </summary>
        public const float ExtensionStartRadius = 0.05f;

        /// <summary>Radius of the eraser sphere (<c>PenEraser</c>) at the butt. A stroke it touches is erased.</summary>
        public const float EraserRadius = 0.025f;

        /// <summary>
        /// Radius of the tip sphere in manipulate mode. An object is grabbed when this sphere touches its grab zone; the
        /// sphere is drawn at this size so the player sees the point that grabs.
        /// </summary>
        public const float GrabRadius = 0.025f;

        public static float ClampRadius(float radius) => Mathf.Clamp(radius, MinSelectRadius, MaxSelectRadius);

        /// <summary><c>Extension</c> blend shape amount (0..1) for a selection radius.</summary>
        public static float ExtensionForRadius(float radius) =>
            Mathf.Clamp01((radius - ExtensionStartRadius) / (MaxSelectRadius - ExtensionStartRadius));

        /// <summary>Distance from the butt to the tip for an extension amount.</summary>
        public static float Length(float extension) => BodyLength + Mathf.Clamp01(extension) * ExtensionLength;

        public static Vector3 TipLocalPosition(float extension) => new(Length(extension), 0f, 0f);

        /// <summary>Uniform scale of <c>PenSelectArea</c> that shows a sphere of <paramref name="radius"/>.</summary>
        public static float SelectAreaScale(float radius) => radius / ModelSelectRadius;

        /// <summary>
        /// Radius after holding the stick at <paramref name="stick"/> (-1..1) for <paramref name="deltaTime"/>.
        /// The change is multiplicative, so small and large spheres resize at the same perceived speed; a full push
        /// covers the whole range in <paramref name="fullRangeSeconds"/>.
        /// </summary>
        public static float AdjustRadius(float radius, float stick, float deltaTime, float fullRangeSeconds)
        {
            if (stick == 0f || deltaTime <= 0f || fullRangeSeconds <= 0f)
            {
                return ClampRadius(radius);
            }
            float rate = Mathf.Log(MaxSelectRadius / MinSelectRadius) / fullRangeSeconds;
            return ClampRadius(radius * Mathf.Exp(Mathf.Clamp(stick, -1f, 1f) * rate * deltaTime));
        }
    }
}
