using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DNPDemo.Interaction.Stroke.Testing
{
    /// <summary>
    /// World-space panel for the pen test scene: shows what <see cref="StrokeTestHarness"/> judges released lines
    /// against and the last verdict, and switches between the guide and the sketch with its button (pressed with the
    /// laser in VR, or the mouse on desktop). Not for the demo scenes.
    /// </summary>
    public sealed class StrokeTestPanel : MonoBehaviour
    {
        [SerializeField] StrokeTestHarness _harness;
        [SerializeField] TMP_Text _goalText;
        [SerializeField] TMP_Text _verdictText;
        [SerializeField] Button _switchButton;
        [SerializeField] TMP_Text _switchLabel;

        void Start()
        {
            if (_harness == null)
            {
                Debug.LogError("[DNPDemo] StrokeTestPanel: StrokeTestHarness を設定してください。", this);
                return;
            }
            _harness.GoalChanged += HandleGoalChanged;
            _harness.Judged += HandleJudged;
            if (_switchButton != null)
            {
                _switchButton.onClick.AddListener(Switch);
            }
            ShowGoal(_harness.Goal);
        }

        void OnDestroy()
        {
            if (_harness != null)
            {
                _harness.GoalChanged -= HandleGoalChanged;
                _harness.Judged -= HandleJudged;
            }
            if (_switchButton != null)
            {
                _switchButton.onClick.RemoveListener(Switch);
            }
        }

        void Switch() => _harness.SetGoal(_harness.Goal == StrokeGoal.Guide ? StrokeGoal.Sketch : StrokeGoal.Guide);

        void HandleGoalChanged(StrokeGoal goal) => ShowGoal(goal);

        void HandleJudged(StrokeVerdict verdict) => SetText(_verdictText, verdict.ToString());

        void ShowGoal(StrokeGoal goal)
        {
            bool guide = goal == StrokeGoal.Guide;
            SetText(_goalText, guide
                ? "ガイド：始点の球から終点の球まで届く線を描く"
                : $"線画：線全体の大きさが {_harness.MinExtent:0.##}m 以上になるまで描く");
            SetText(_verdictText, "線を描いて離すと、判定の結果をここに表示します。");
            SetText(_switchLabel, guide ? "線画に切り替える" : "ガイドに切り替える");
        }

        static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
