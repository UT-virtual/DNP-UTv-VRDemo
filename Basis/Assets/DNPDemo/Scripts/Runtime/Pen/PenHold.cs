using UnityEngine;

namespace DNPDemo.Pen
{
    /// <summary>
    /// Whether the hand holds the pen, and where the pen sits in the hand. Pressing the grip while the hand is within
    /// reach grabs the pen and keeps the hand-to-pen offset of that moment; releasing the grip (or losing hand
    /// tracking) lets go, and the pen stays where it is. Holding the grip and then moving into reach does not grab:
    /// only the press itself does.
    /// </summary>
    public sealed class PenHold
    {
        Vector3 _offsetPosition;
        Quaternion _offsetRotation = Quaternion.identity;
        bool _gripWasHeld;

        public bool IsHeld { get; private set; }

        /// <summary>Advances one frame. Returns true when <see cref="IsHeld"/> changed.</summary>
        /// <param name="gripHeld">The grip button is down this frame.</param>
        /// <param name="handTracked">The hand pose is valid this frame.</param>
        /// <param name="hand">World pose of the hand.</param>
        /// <param name="pen">World pose of the pen.</param>
        /// <param name="handInReach">The hand is close enough to grab the pen.</param>
        public bool Update(bool gripHeld, bool handTracked, Pose hand, Pose pen, bool handInReach)
        {
            bool pressed = gripHeld && !_gripWasHeld;
            _gripWasHeld = gripHeld;

            if (IsHeld)
            {
                if (gripHeld && handTracked)
                {
                    return false;
                }
                IsHeld = false;
                return true;
            }

            if (!pressed || !handTracked || !handInReach)
            {
                return false;
            }
            Quaternion inverseHand = Quaternion.Inverse(hand.rotation);
            _offsetPosition = inverseHand * (pen.position - hand.position);
            _offsetRotation = inverseHand * pen.rotation;
            IsHeld = true;
            return true;
        }

        /// <summary>World pose of the held pen for a hand pose.</summary>
        public Pose Follow(Pose hand) =>
            new(hand.position + hand.rotation * _offsetPosition, hand.rotation * _offsetRotation);

        /// <summary>Lets go immediately, for example when the local player switches to another pen.</summary>
        public void Reset()
        {
            IsHeld = false;
            _gripWasHeld = false;
        }
    }
}
