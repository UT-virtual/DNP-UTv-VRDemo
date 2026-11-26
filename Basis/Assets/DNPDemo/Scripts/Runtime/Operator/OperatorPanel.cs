using System.Text;
using DNPDemo.Presentation;
using DNPDemo.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DNPDemo.Operator
{
    /// <summary>
    /// Hidden panel for exhibition staff: shows the current step and moves the scenario next / back, to any chosen step,
    /// back to the lobby for the next visitors, or to the step where an interrupted run stopped.
    /// Opened in front of the view by <see cref="OperatorInput"/> and left in place so it can be pointed at with the laser.
    /// </summary>
    public sealed class OperatorPanel : MonoBehaviour
    {
        [SerializeField] ScenarioDirector _director;

        [SerializeField, Min(0.2f)] float _openDistance = 0.7f;
        [SerializeField] float _openHeightOffset = -0.15f;

        [SerializeField] TMP_Text _statusText;
        [SerializeField] Button _backButton;
        [SerializeField] Button _nextButton;

        [Tooltip("Shown while the previous run was interrupted; moves to the step where it stopped.")]
        [SerializeField] Button _resumeButton;
        [SerializeField] TMP_Text _resumeText;

        [Tooltip("Step picker: the chosen step, the buttons that move the choice, and the button that jumps there.")]
        [SerializeField] TMP_Text _jumpTargetText;
        [SerializeField] Button _jumpPreviousButton;
        [SerializeField] Button _jumpNextButton;
        [SerializeField] Button _jumpButton;

        [Tooltip("Back to the lobby for the next visitors.")]
        [SerializeField] Button _restartButton;
        [SerializeField] Button _closeButton;

        readonly StringBuilder _builder = new();
        int _jumpTarget;

        public bool IsOpen => gameObject.activeSelf;

        void Awake()
        {
            _backButton.onClick.AddListener(() => _director.RequestCommand(OperatorCommand.Back));
            _nextButton.onClick.AddListener(() => _director.RequestCommand(OperatorCommand.Next));
            _resumeButton.onClick.AddListener(() => _director.RequestJumpTo(_director.ResumeStepIndex));
            _jumpPreviousButton.onClick.AddListener(() => MoveJumpTarget(-1));
            _jumpNextButton.onClick.AddListener(() => MoveJumpTarget(1));
            _jumpButton.onClick.AddListener(() => _director.RequestJumpTo(_jumpTarget));
            _restartButton.onClick.AddListener(() => _director.RequestCommand(OperatorCommand.Restart));
            _closeButton.onClick.AddListener(Close);
        }

        void OnEnable()
        {
            if (_director == null)
            {
                Debug.LogError("[DNPDemo] OperatorPanel needs a ScenarioDirector reference.", this);
                return;
            }
            _director.StepChanged += Refresh;
            _director.StatusChanged += Refresh;
            // The picker starts at the step on show and then stays where the operator puts it, even as the run moves on.
            _jumpTarget = Mathf.Max(0, _director.StepIndex);
            Refresh();
        }

        void OnDisable()
        {
            if (_director != null)
            {
                _director.StepChanged -= Refresh;
                _director.StatusChanged -= Refresh;
            }
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            if (HeadPose.IsAvailable)
            {
                HeadPose.Place(transform, HeadPose.GetYaw(transform.eulerAngles.y), _openDistance, _openHeightOffset);
            }
            gameObject.SetActive(true);
        }

        public void Close() => gameObject.SetActive(false);

        void MoveJumpTarget(int delta)
        {
            _jumpTarget = Mathf.Clamp(_jumpTarget + delta, 0, Mathf.Max(0, _director.StepCount - 1));
            Refresh();
        }

        void Refresh()
        {
            bool running = _director.IsRunning;
            int stepCount = _director.StepCount;
            _jumpTarget = Mathf.Clamp(_jumpTarget, 0, Mathf.Max(0, stepCount - 1));

            _statusText.text = BuildStatusText(running);
            _backButton.interactable = running && _director.StepIndex > 0;
            _nextButton.interactable = running && _director.StepIndex < stepCount - 1;
            _restartButton.interactable = running;

            int resumeStep = _director.ResumeStepIndex;
            bool canResume = running && resumeStep >= 0 && resumeStep < stepCount;
            _resumeButton.gameObject.SetActive(canResume);
            if (canResume)
            {
                _resumeText.text = $"中断したステップ {resumeStep}（{StepTitle(resumeStep)}）から再開";
            }

            _jumpTargetText.text = stepCount > 0 ? $"{_jumpTarget}　{StepTitle(_jumpTarget)}" : string.Empty;
            _jumpPreviousButton.interactable = running && _jumpTarget > 0;
            _jumpNextButton.interactable = running && _jumpTarget < stepCount - 1;
            _jumpButton.interactable = running;
        }

        string BuildStatusText(bool running) =>
            running
                ? FormatStatus(_builder, _director.CurrentStep, _director.StepIndex, _director.StepCount,
                    _director.IsWaitingForPartner, _director.HasAuthority)
                : "セッションに接続していません。";

        /// <summary>
        /// Status of a running scenario. Takes the state as arguments, so tests can lay the panel out with the text of
        /// every step.
        /// </summary>
        internal static string FormatStatus(StringBuilder builder, ScenarioStep step, int stepIndex, int stepCount,
            bool waitingForPartner, bool hasAuthority)
        {
            builder.Clear();
            builder.Append("ステップ ").Append(stepIndex).Append(" / ").Append(stepCount - 1)
                .Append("　").Append(step.Title).Append('\n');
            builder.Append(step.Completion.ToDisplayName(step.Actor)).Append('\n');
            if (!string.IsNullOrEmpty(step.TaskText))
            {
                builder.Append("タスク：").Append(step.TaskText).Append('\n');
            }
            if (waitingForPartner)
            {
                builder.Append("相手の端末が接続していません。台詞が終わっても次へ進まず、相手が戻るとこのステップを頭から再生します。\n");
            }
            builder.Append(hasAuthority ? "この端末が進行を管理しています。" : "操作は進行を管理する端末に送られます。");
            return builder.ToString();
        }

        string StepTitle(int stepIndex)
        {
            ScenarioData scenario = _director.Scenario;
            return scenario != null && stepIndex >= 0 && stepIndex < scenario.Steps.Count
                ? scenario.Steps[stepIndex].Title
                : string.Empty;
        }
    }
}
