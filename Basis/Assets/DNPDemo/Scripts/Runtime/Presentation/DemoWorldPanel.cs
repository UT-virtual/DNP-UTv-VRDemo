using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DNPDemo.Presentation
{
    /// <summary>
    /// Base of the demo's world-space panels that a main menu entry opens (the history panel, the background panel).
    /// Opening puts the panel in front of the view, where it stays to be pointed at with the laser; × closes it. While
    /// closed, the canvas (<see cref="_content"/>) is switched off, so its laser target disappears with it.
    /// <para>
    /// The panel is a plain transform: the canvas and anything drawn beside it are its children. Subclasses fill the
    /// canvas and call <see cref="ShowInFrontOfView"/> from their own Open.
    /// </para>
    /// </summary>
    public abstract class DemoWorldPanel : MonoBehaviour
    {
        /// <summary>Opacity of the label or icon on a disabled button.</summary>
        const float DisabledContentAlpha = 0.35f;

        [SerializeField, Min(0.2f)] float _openDistance = 0.8f;
        [SerializeField] float _openHeightOffset = -0.15f;

        [Tooltip("The canvas, shown while the panel is open.")]
        [SerializeField] GameObject _content;

        [SerializeField] Button _closeButton;

        public bool IsOpen => _content != null && _content.activeSelf;

        protected virtual void Awake()
        {
            AddListener(_closeButton, Close);
            SetContentVisible(false);
        }

        protected virtual void OnDestroy() => RemoveListener(_closeButton, Close);

        public void Close() => SetContentVisible(false);

        /// <summary>Puts the panel in front of the view and shows it. Opening it again brings it back in front.</summary>
        protected void ShowInFrontOfView()
        {
            if (HeadPose.IsAvailable)
            {
                HeadPose.Place(transform, HeadPose.GetYaw(transform.eulerAngles.y), _openDistance, _openHeightOffset);
            }
            SetContentVisible(true);
        }

        /// <summary>The canvas was shown or hidden. Hide what is drawn outside it here.</summary>
        protected virtual void OnVisibilityChanged(bool visible)
        {
        }

        void SetContentVisible(bool visible)
        {
            if (_content != null)
            {
                _content.SetActive(visible);
            }
            OnVisibilityChanged(visible);
        }

        protected static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        /// <summary>
        /// Sets <paramref name="button"/>'s state. Its colour tint greys out only the target graphic, so the label or icon
        /// on it is faded here as well.
        /// </summary>
        protected static void SetInteractable(Button button, bool interactable)
        {
            if (button == null)
            {
                return;
            }
            button.interactable = interactable;
            foreach (Graphic graphic in button.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic != button.targetGraphic)
                {
                    graphic.canvasRenderer.SetAlpha(interactable ? 1f : DisabledContentAlpha);
                }
            }
        }

        protected static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        protected static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }
    }
}
