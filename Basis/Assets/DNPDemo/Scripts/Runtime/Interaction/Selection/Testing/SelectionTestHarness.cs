using System.Collections.Generic;
using DNPDemo.Pen;
using UnityEngine;

namespace DNPDemo.Interaction.Selection.Testing
{
    /// <summary>
    /// Lets range selection be tried in a test scene without a session, roles or scenario. Every press of the pen in
    /// select mode records a selection, judges it against <see cref="_targets"/> with the same rule as
    /// <see cref="SelectionTool"/>, and logs the verdict: a success stays until the next press, a failure fades out.
    /// Network sync is not covered.
    /// Not for the demo scenes, where <see cref="SelectionTool"/> runs the selection parts.
    /// </summary>
    public sealed class SelectionTestHarness : MonoBehaviour
    {
        [SerializeField] SpatialPenController _pen;
        [SerializeField] SelectionVolumeView _view;

        [Tooltip("Objects with a DetectionZoneGroup or DetectionZone. Every one must be touched.")]
        [SerializeField] List<GameObject> _targets = new();

        [SerializeField, Range(0f, 1f)] float _requiredInsideRatio = 0.9f;
        [SerializeField, Range(0f, 1f)] float _requiredCoverage = 0.5f;
        [SerializeField, Min(0f)] float _rejectFadeSeconds = 0.4f;

        readonly List<IDetectionVolume> _volumes = new();
        SelectionRecorder _recorder;

        void Start()
        {
            if (_pen == null || _view == null)
            {
                Debug.LogError("[DNPDemo] SelectionTestHarness: SpatialPenController と SelectionVolumeView を設定してください。", this);
                return;
            }
            foreach (GameObject target in _targets)
            {
                IDetectionVolume volume = DetectionVolumes.Find(target);
                if (volume == null)
                {
                    Debug.LogError($"[DNPDemo] SelectionTestHarness: {(target != null ? target.name : "（空）")} に判定ゾーンがありません。", this);
                    continue;
                }
                _volumes.Add(volume);
            }
            _recorder = new SelectionRecorder(_pen, _view, new SelectionSampler());
            _recorder.Finished += HandleFinished;
            _recorder.Listen(PenMode.Select);
        }

        void OnDestroy()
        {
            if (_recorder != null)
            {
                _recorder.Finished -= HandleFinished;
                _recorder.Stop();
            }
        }

        void LateUpdate()
        {
            _recorder?.Tick();
        }

        void HandleFinished(IReadOnlyList<SelectionSample> samples)
        {
            SelectionVerdict verdict = SelectionJudge.Evaluate(samples, _volumes, new SelectionCriteria(_requiredInsideRatio, _requiredCoverage));
            Debug.Log($"[DNPDemo] 範囲選択のテスト：{verdict}（メッセージ {SelectionMessage.Serialize(samples).Length} バイト）", this);
            if (!verdict.Succeeded)
            {
                _view.Clear(_rejectFadeSeconds);
            }
        }
    }
}
