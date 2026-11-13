using System;
using DNPDemo.Pen;
using UnityEngine;

namespace DNPDemo.Interaction.Stroke.Testing
{
    /// <summary>
    /// Lets drawing be tried in a test scene without a session, roles or scenario. In draw mode the pen draws lines and
    /// the eraser at its butt removes them. Each released line is judged with the same rule as <see cref="StrokeTool"/>
    /// for the current <see cref="Goal"/>, and the verdict is logged and raised as <see cref="Judged"/>: a guide line that
    /// misses fades out; everything else stays until erased.
    /// <para>
    /// The goal can be switched while playing (<see cref="SetGoal"/>, from <see cref="StrokeTestPanel"/>). Switching
    /// clears every line, and the guide's start and end zones are shown only while the goal is the guide.
    /// </para>
    /// Network sync is not covered. Not for the demo scenes, where <see cref="StrokeTool"/> runs the stroke parts.
    /// </summary>
    public sealed class StrokeTestHarness : MonoBehaviour
    {
        [SerializeField] SpatialPenController _pen;
        [SerializeField] StrokeView _view;

        [Tooltip("Goal at start. It can be switched while playing.")]
        [SerializeField] StrokeGoal _goal = StrokeGoal.Guide;

        [Tooltip("Guide: object with a DetectionZoneGroup or DetectionZone where the line starts. Shown only for the guide.")]
        [SerializeField] GameObject _startZone;

        [Tooltip("Guide: object with a DetectionZoneGroup or DetectionZone the line must reach. Shown only for the guide.")]
        [SerializeField] GameObject _endZone;

        [Tooltip("Sketch: smallest longest side of the box around all lines, in metres.")]
        [SerializeField, Min(0.01f)] float _minExtent = StrokePart.DefaultMinExtent;

        [SerializeField, Min(0f)] float _rejectFadeSeconds = 0.4f;

        readonly StrokeSet _set = new();
        StrokeRecorder _recorder;
        IDetectionVolume _start;
        IDetectionVolume _end;

        public StrokeGoal Goal => _goal;

        public float MinExtent => _minExtent;

        /// <summary>The goal was switched; every line has been cleared.</summary>
        public event Action<StrokeGoal> GoalChanged;

        /// <summary>A released line was judged.</summary>
        public event Action<StrokeVerdict> Judged;

        void Start()
        {
            ApplyZoneVisibility();
            if (_pen == null || _view == null)
            {
                Debug.LogError("[DNPDemo] StrokeTestHarness: SpatialPenController と StrokeView を設定してください。", this);
                return;
            }
            _start = DetectionVolumes.Find(_startZone);
            _end = DetectionVolumes.Find(_endZone);
            if (_start == null || _end == null)
            {
                Debug.LogError("[DNPDemo] StrokeTestHarness: ガイドの始点・終点に判定ゾーンがありません。", this);
            }
            _recorder = new StrokeRecorder(_pen, _view, _set);
            _recorder.Finished += HandleFinished;
            _recorder.Erased += HandleErased;
            _recorder.Listen(PenMode.Draw);
        }

        void OnDestroy()
        {
            if (_recorder != null)
            {
                _recorder.Finished -= HandleFinished;
                _recorder.Erased -= HandleErased;
                _recorder.Stop();
            }
        }

        void LateUpdate()
        {
            _recorder?.Tick();
        }

        /// <summary>Switches what released lines are judged against, and clears every line (including one being drawn).</summary>
        public void SetGoal(StrokeGoal goal)
        {
            if (_goal == goal)
            {
                return;
            }
            _goal = goal;
            if (_recorder != null)
            {
                // Stop drops a line being drawn; listening again picks up the next press.
                _recorder.Stop();
                _set.Clear();
                _view.Clear();
                _recorder.Listen(PenMode.Draw);
            }
            ApplyZoneVisibility();
            Debug.Log($"[DNPDemo] 線画のテスト：判定を {goal} に切り替えました。", this);
            GoalChanged?.Invoke(goal);
        }

        void ApplyZoneVisibility()
        {
            bool guide = _goal == StrokeGoal.Guide;
            if (_startZone != null)
            {
                _startZone.SetActive(guide);
            }
            if (_endZone != null)
            {
                _endZone.SetActive(guide);
            }
        }

        void HandleFinished(StrokeLine line)
        {
            StrokeVerdict verdict = _goal == StrokeGoal.Guide
                ? StrokeJudge.Guide(line.Points, _start, _end, _view.Width * 0.5f)
                : StrokeJudge.Sketch(_set, _minExtent);
            Debug.Log($"[DNPDemo] 線画のテスト（{_goal}）：{verdict}（この線 {StrokeMessage.Append(line, 0).Length} バイト、全体 {StrokeMessage.Snapshot(_set).Length} バイト）", this);
            if (_goal == StrokeGoal.Guide && !verdict.Succeeded)
            {
                _set.Remove(line.Id);
                _view.Hide(line.Id, _rejectFadeSeconds);
            }
            Judged?.Invoke(verdict);
        }

        void HandleErased(StrokeLine line)
        {
            Debug.Log($"[DNPDemo] 線画のテスト：線 {line.Id} を消しました（残り {_set.Lines.Count} 本）。", this);
        }
    }
}
