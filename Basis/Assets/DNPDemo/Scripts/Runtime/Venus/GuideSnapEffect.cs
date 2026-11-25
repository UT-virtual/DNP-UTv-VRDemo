using System.Collections.Generic;
using DNPDemo.Scenario;
using UnityEngine;

namespace DNPDemo.Venus
{
    /// <summary>
    /// The arm snapping along the guide (docs/PLAN.md §3, step 11): a glowing line grows along <see cref="GuideCurve"/>
    /// from the start to the end with a bright head at its tip, then fades while the statue changes version. Played by
    /// <see cref="Scenario.Cues.GuideSnapCue"/>; transient, so a device that jumps or joins later does not see it.
    /// <para>
    /// The effect runs along the set curve, not along the line the player drew: the guide judges that the line reached
    /// both zones, and the curve joins the same zones.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(GuideCurve))]
    public sealed class GuideSnapEffect : MonoBehaviour, IStageResettable
    {
        [Tooltip("Line that grows along the curve. Kept inactive while the effect is not playing.")]
        [SerializeField] LineRenderer _glow;

        [Tooltip("Object that travels at the tip of the line. Kept inactive while the effect is not playing. Optional.")]
        [SerializeField] Transform _head;

        [Tooltip("Points the line is drawn with when it has reached the end.")]
        [SerializeField, Range(4, 128)] int _resolution = 48;

        [Tooltip("Seconds the line takes to fade after reaching the end.")]
        [SerializeField, Min(0f)] float _fadeSeconds = 0.6f;

        readonly List<Vector3> _points = new();
        GuideCurve _curve;
        float _travelSeconds;
        float _elapsed;
        float _baseWidth = -1f;

        public bool IsPlaying { get; private set; }

        /// <summary>True when the line has reached the end (progress = 1), before fading.</summary>
        public bool HasArrived { get; private set; }

        GuideCurve Curve => _curve != null ? _curve : _curve = GetComponent<GuideCurve>();

        /// <summary>Starts the effect: the line reaches the end after <paramref name="travelSeconds"/>, then fades.</summary>
        public void Play(float travelSeconds)
        {
            Stop();
            if (!Curve.IsValid || _glow == null)
            {
                Debug.LogWarning("[DNPDemo] GuideSnapEffect: カーブの端点か線が設定されていないため、演出を省きます。", this);
                return;
            }
            if (_baseWidth < 0f)
            {
                _baseWidth = _glow.widthMultiplier;
            }
            _travelSeconds = Mathf.Max(0.01f, travelSeconds);
            _elapsed = 0f;
            IsPlaying = true;
            _glow.useWorldSpace = true;
            _glow.widthMultiplier = _baseWidth;
            _glow.gameObject.SetActive(true);
            if (_head != null)
            {
                _head.gameObject.SetActive(true);
            }
            Apply(0f);
        }

        /// <summary>Ends the effect at once and hides it.</summary>
        public void Stop()
        {
            IsPlaying = false;
            HasArrived = false;
            if (_glow != null)
            {
                if (_baseWidth >= 0f)
                {
                    _glow.widthMultiplier = _baseWidth;
                }
                _glow.positionCount = 0;
                _glow.gameObject.SetActive(false);
            }
            if (_head != null)
            {
                _head.gameObject.SetActive(false);
            }
        }

        void IStageResettable.ResetToBaseline() => Stop();

        void Update() => Tick(Time.deltaTime);

        void OnDisable() => Stop();

        /// <summary>Advances the effect by <paramref name="deltaTime"/> seconds.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }
            _elapsed += Mathf.Max(0f, deltaTime);
            if (_elapsed < _travelSeconds)
            {
                Apply(Mathf.SmoothStep(0f, 1f, _elapsed / _travelSeconds));
                return;
            }
            if (!HasArrived)
            {
                HasArrived = true;
                Apply(1f);
            }
            float fade = _fadeSeconds > 0f ? (_elapsed - _travelSeconds) / _fadeSeconds : 1f;
            if (fade >= 1f)
            {
                Stop();
                return;
            }
            _glow.widthMultiplier = _baseWidth * (1f - fade);
            if (_head != null)
            {
                _head.gameObject.SetActive(false);
            }
        }

        /// <summary>Draws the line from the start of the curve to <paramref name="progress"/> (0 to 1).</summary>
        void Apply(float progress)
        {
            int count = Mathf.Max(2, Mathf.CeilToInt(_resolution * progress) + 1);
            _points.Clear();
            for (int i = 0; i < count; i++)
            {
                _points.Add(Curve.Evaluate(progress * i / (count - 1)));
            }
            _glow.positionCount = _points.Count;
            for (int i = 0; i < _points.Count; i++)
            {
                _glow.SetPosition(i, _points[i]);
            }
            if (_head != null)
            {
                _head.position = _points[_points.Count - 1];
            }
        }
    }
}
