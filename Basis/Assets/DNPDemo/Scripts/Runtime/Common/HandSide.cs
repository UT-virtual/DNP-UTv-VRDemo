using Basis.Scripts.TransformBinders.BoneControl;

namespace DNPDemo
{
    /// <summary>Left or right hand of the local player.</summary>
    public enum HandSide : byte
    {
        Left = 0,
        Right = 1,
    }

    public static class HandSideExtensions
    {
        public static HandSide Opposite(this HandSide side) => side == HandSide.Left ? HandSide.Right : HandSide.Left;

        /// <summary>The hand a Basis tracked role stands for. False for roles that are not a hand (head, trackers, desktop eye).</summary>
        public static bool TryFromRole(BasisBoneTrackedRole role, out HandSide side)
        {
            switch (role)
            {
                case BasisBoneTrackedRole.LeftHand:
                    side = HandSide.Left;
                    return true;
                case BasisBoneTrackedRole.RightHand:
                    side = HandSide.Right;
                    return true;
                default:
                    side = default;
                    return false;
            }
        }
    }
}
