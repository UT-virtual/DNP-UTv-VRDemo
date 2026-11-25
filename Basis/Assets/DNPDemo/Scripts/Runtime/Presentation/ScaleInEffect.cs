using DNPDemo.Scenario;
using UnityEngine;

namespace DNPDemo.Presentation
{
    /// <summary>
    /// Grows <see cref="_visual"/> from nothing to its own size, for an object that appears as the result of an operation
    /// (the hand mirror generated from the sketch). Only the visual scales, so detection zones on the object keep their
    /// size. <see cref="IStageResettable"/>: when the step changes mid-way the stage puts the full size back.
    /// </summary>
    public sealed class ScaleInEffect : MonoBehaviour, IStageResettable
    {
        [Tooltip("The child that grows. The object's zones and markers stay as they are.")]
        [SerializeField] Transform _visual;

        Vector3 _fullScale;
        bool _hasFullScale;
        float _duration;
        float _elapsed;
        bool _playing;

        public bool IsPlaying => _playing;

        /// <summary>Starts growing from zero; the visual reaches its full size after <paramref name="seconds"/>.</summary>
        public void Play(float seconds)
        {
            if (_visual == null)
            {
                return;
            }
            RememberFullScale();
            if (!(seconds > 0f))
            {
                Stop();
                return;
            }
            _duration = seconds;
            _elapsed = 0f;
            _playing = true;
            _visual.localScale = Vector3.zero;
        }

        public void ResetToBaseline() => Stop();

        void Update()
        {
            if (!_playing)
            {
                return;
            }
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            if (t >= 1f)
            {
                Stop();
                return;
            }
            // Ease out: quick at first, settling into place.
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);
            _visual.localScale = _fullScale * eased;
        }

        void Stop()
        {
            _playing = false;
            if (_visual != null && _hasFullScale)
            {
                _visual.localScale = _fullScale;
            }
        }

        // Taken before the first change, when the visual still has the size it was authored with.
        void RememberFullScale()
        {
            if (!_hasFullScale)
            {
                _fullScale = _visual.localScale;
                _hasFullScale = true;
            }
        }
    }
}
