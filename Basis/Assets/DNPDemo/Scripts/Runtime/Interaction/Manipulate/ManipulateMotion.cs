using System;
using UnityEngine;

namespace DNPDemo.Interaction.Manipulate
{
    /// <summary>
    /// Moves a manipulated object's transform in world space in one of three ways: placed at once (the actor's own drag,
    /// a snapshot), followed smoothly (drags received at a limited rate), or animated over a fixed time (snapping to the
    /// goal). The object is only written to while a motion runs, so nothing here fights the stage when it resets the
    /// object.
    /// </summary>
    public sealed class ManipulateMotion
    {
        enum State
        {
            Idle,
            Follow,
            Tween,
        }

        // Closer than this the smooth follow stops and places the object exactly.
        const float SettleDistance = 0.0005f;
        const float SettleAngle = 0.05f;

        readonly Transform _target;
        State _state;
        Pose _from;
        Pose _to;
        float _elapsed;
        float _duration;

        public ManipulateMotion(Transform target)
        {
            _target = target != null ? target : throw new ArgumentNullException(nameof(target));
        }

        public Transform Target => _target;

        /// <summary>True while the object is still moving toward its last given pose.</summary>
        public bool IsMoving => _state != State.Idle;

        /// <summary>The object's current world pose.</summary>
        public Pose Current => new(_target.position, _target.rotation);

        /// <summary>Places the object at <paramref name="pose"/> now.</summary>
        public void Place(Pose pose)
        {
            _state = State.Idle;
            Apply(pose);
        }

        /// <summary>Moves the object toward <paramref name="pose"/> a little every <see cref="Tick"/>.</summary>
        public void Follow(Pose pose)
        {
            _to = pose;
            _state = State.Follow;
        }

        /// <summary>Animates the object from where it is to <paramref name="pose"/> in <paramref name="seconds"/>.</summary>
        public void Animate(Pose pose, float seconds)
        {
            if (seconds <= 0f)
            {
                Place(pose);
                return;
            }
            _from = Current;
            _to = pose;
            _elapsed = 0f;
            _duration = seconds;
            _state = State.Tween;
        }

        /// <summary>Stops any motion and leaves the object where it is.</summary>
        public void Stop() => _state = State.Idle;

        /// <summary>
        /// Advances the motion. <paramref name="followSharpness"/> is how quickly a followed object catches up (per second;
        /// higher is tighter).
        /// </summary>
        public void Tick(float deltaTime, float followSharpness)
        {
            if (_target == null)
            {
                _state = State.Idle;
                return;
            }
            switch (_state)
            {
                case State.Follow:
                {
                    float t = 1f - Mathf.Exp(-Mathf.Max(0f, followSharpness) * deltaTime);
                    Pose current = Current;
                    var next = new Pose(Vector3.Lerp(current.position, _to.position, t), Quaternion.Slerp(current.rotation, _to.rotation, t));
                    if (Vector3.Distance(next.position, _to.position) <= SettleDistance && Quaternion.Angle(next.rotation, _to.rotation) <= SettleAngle)
                    {
                        Place(_to);
                        return;
                    }
                    Apply(next);
                    break;
                }
                case State.Tween:
                {
                    _elapsed += deltaTime;
                    float t = Mathf.Clamp01(_elapsed / _duration);
                    float eased = t * t * (3f - 2f * t);
                    Apply(new Pose(Vector3.Lerp(_from.position, _to.position, eased), Quaternion.Slerp(_from.rotation, _to.rotation, eased)));
                    if (t >= 1f)
                    {
                        _state = State.Idle;
                    }
                    break;
                }
            }
        }

        void Apply(Pose pose)
        {
            if (_target != null)
            {
                _target.SetPositionAndRotation(pose.position, pose.rotation);
            }
        }
    }
}
