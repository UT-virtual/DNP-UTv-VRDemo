using System.Text;
using DNPDemo.Interaction;
using DNPDemo.Pen;
using DNPDemo.Presentation;
using TMPro;
using UnityEngine;

namespace DNPDemo.TaskHud
{
    /// <summary>
    /// The task HUD: a small panel above the player's free hand that shows the running step (<see cref="TaskHudContent"/>)
    /// while it waits for an operation, on both devices. It redraws when the runner's state, the pen's mode or the hand
    /// that last grabbed the pen changes, and follows the other hand (<see cref="HandFollower"/>).
    /// </summary>
    public sealed class TaskHudView : MonoBehaviour
    {
        /// <summary>Marks in front of each part. They must be in the UI font's character set.</summary>
        public const string DoneMark = "✓";
        public const string CurrentMark = "▶";
        public const string PendingMark = "・";

        [SerializeField] InteractionRunner _runner;

        [Tooltip("Local pen. The HUD follows the hand opposite to the one that last grabbed it. Without it the HUD stays on the follower's initial hand.")]
        [SerializeField] SpatialPenController _pen;

        [Tooltip("Shown while a part is waiting, hidden otherwise. Holds the follower so it starts afresh each time.")]
        [SerializeField] GameObject _content;
        [SerializeField] HandFollower _follower;
        [SerializeField] TMP_Text _headingText;
        [SerializeField] TMP_Text _taskText;
        [SerializeField] TMP_Text _linesText;
        [SerializeField] TMP_Text _modeHintText;

        [SerializeField] Color _actorHeadingColor = new(1f, 0.8f, 0.3f);
        [SerializeField] Color _observerHeadingColor = new(0.6f, 0.8f, 1f);
        [SerializeField] Color _doneColor = new(0.45f, 0.9f, 0.55f);
        [SerializeField] Color _currentColor = Color.white;
        [SerializeField] Color _pendingColor = new(0.6f, 0.6f, 0.6f);

        readonly StringBuilder _builder = new();

        void Awake()
        {
            SetVisible(false);
        }

        void OnEnable()
        {
            if (_runner != null)
            {
                _runner.StateChanged += Refresh;
            }
            if (_pen != null)
            {
                _pen.LastGrabHandChanged += HandleLastGrabHandChanged;
                HandleLastGrabHandChanged(_pen.LastGrabHand);
            }
            PenModeSelection.Changed += HandleModeChanged;
            Refresh();
        }

        void OnDisable()
        {
            if (_runner != null)
            {
                _runner.StateChanged -= Refresh;
            }
            if (_pen != null)
            {
                _pen.LastGrabHandChanged -= HandleLastGrabHandChanged;
            }
            PenModeSelection.Changed -= HandleModeChanged;
        }

        void HandleModeChanged(PenMode mode) => Refresh();

        void HandleLastGrabHandChanged(HandSide penHand)
        {
            if (_follower != null)
            {
                _follower.Side = penHand.Opposite();
            }
        }

        void Refresh()
        {
            TaskHudContent content = _runner != null
                ? TaskHudContent.Build(_runner.CurrentStep, _runner.PartIndex, _runner.IsLocalActor, PenModeSelection.Current)
                : TaskHudContent.Hidden;
            SetVisible(content.IsVisible);
            if (!content.IsVisible)
            {
                return;
            }

            if (_headingText != null)
            {
                _headingText.text = content.Heading;
                _headingText.color = content.IsLocalActor ? _actorHeadingColor : _observerHeadingColor;
            }
            if (_taskText != null)
            {
                _taskText.text = content.TaskText;
                _taskText.gameObject.SetActive(content.TaskText.Length > 0);
            }
            if (_linesText != null)
            {
                _linesText.text = FormatLines(content);
            }
            if (_modeHintText != null)
            {
                _modeHintText.text = content.ModeHint;
                _modeHintText.gameObject.SetActive(content.ModeHint.Length > 0);
            }
        }

        string FormatLines(TaskHudContent content)
        {
            _builder.Clear();
            for (int i = 0; i < content.Lines.Count; i++)
            {
                TaskHudLine line = content.Lines[i];
                if (i > 0)
                {
                    _builder.Append('\n');
                }
                (string mark, Color color) = line.State switch
                {
                    TaskHudLineState.Done => (DoneMark, _doneColor),
                    TaskHudLineState.Current => (CurrentMark, _currentColor),
                    _ => (PendingMark, _pendingColor),
                };
                // The indent makes wrapped lines start under the text, not under the mark.
                _builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGBA(color)).Append('>')
                    .Append(mark).Append("<indent=1.2em>").Append(line.Text).Append("</indent></color>");
            }
            return _builder.ToString();
        }

        void SetVisible(bool visible)
        {
            if (_content != null && _content.activeSelf != visible)
            {
                _content.SetActive(visible);
            }
        }
    }
}
