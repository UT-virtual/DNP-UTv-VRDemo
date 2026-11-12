using System.Text;
using DNPDemo.Presentation;
using DNPDemo.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DNPDemo.Operator
{
    /// <summary>
    /// Hidden panel for exhibition staff: shows the current step and moves the scenario next / back / to the start.
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
        [SerializeField] Button _restartButton;
        [SerializeField] Button _closeButton;

        readonly StringBuilder _builder = new();

        public bool IsOpen => gameObject.activeSelf;

        void Awake()
        {
            _backButton.onClick.AddListener(() => _director.RequestCommand(OperatorCommand.Back));
            _nextButton.onClick.AddListener(() => _director.RequestCommand(OperatorCommand.Next));
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
            Refresh();
        }

        void OnDisable()
        {
            if (_director != null)
            {
                _director.StepChanged -= Refresh;
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

        void Refresh()
        {
            bool running = _director.IsRunning;
            _builder.Clear();
            if (!running)
            {
                _builder.Append("セッションに接続していません。");
            }
            else
            {
                ScenarioStep step = _director.CurrentStep;
                _builder.Append("ステップ ").Append(_director.StepIndex).Append(" / ").Append(_director.StepCount - 1)
                    .Append("　").Append(step.Title).Append('\n');
                _builder.Append(step.Completion.ToDisplayName(step.Actor)).Append('\n');
                if (!string.IsNullOrEmpty(step.TaskText))
                {
                    _builder.Append("タスク：").Append(step.TaskText).Append('\n');
                }
                _builder.Append(_director.HasAuthority ? "この端末が進行を管理しています。" : "操作は進行を管理する端末に送られます。");
            }
            _statusText.text = _builder.ToString();
            _backButton.interactable = running && _director.StepIndex > 0;
            _nextButton.interactable = running && _director.StepIndex < _director.StepCount - 1;
            _restartButton.interactable = running;
        }
    }
}
