using System;
using System.Collections.Generic;
using Basis.Scripts.BasisSdk.Highlight;
using Basis.Scripts.BasisSdk.Interactions;
using Basis.Scripts.Device_Management.Devices;
using DNPDemo.Interaction;
using UnityEngine;

namespace DNPDemo.Pen
{
    /// <summary>
    /// One pen in the world. Grabbing, holding and the pose across devices are Basis's pickup
    /// (<see cref="BasisPickupInteractable"/> with <c>BasisPickupSyncNetworking</c> on the same object); this
    /// component draws the pen from a <see cref="PenState"/> and decides who may grab it.
    /// <list type="bullet">
    /// <item>Select: the tip sphere (<c>PenSelectArea</c>) at the selection radius. Above
    /// <see cref="PenGeometry.ExtensionStartRadius"/> the body extends so the sphere stays clear of the hand.</item>
    /// <item>Voice: the microphone head (<c>PenMicrophone</c>); its indicator ring turns from idle to active colour
    /// while the pen is held with the trigger down.</item>
    /// <item>Draw: the eraser sphere (<c>PenEraser</c>, radius <see cref="PenGeometry.EraserRadius"/>) at the butt.</item>
    /// <item>Manipulate: the tip sphere at <see cref="PenGeometry.GrabRadius"/>, the point that grabs.</item>
    /// </list>
    /// The local player's pen is driven by <see cref="SpatialPenController"/>, the other player's by
    /// <see cref="SpatialPenSync"/>.
    /// </summary>
    public sealed class SpatialPenView : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] BasisPickupInteractable _pickup;
        [Tooltip("The collider the hand grabs. Off while the pen is hidden.")]
        [SerializeField] Collider _grabCollider;

        [SerializeField] SkinnedMeshRenderer _body;
        [SerializeField] Renderer _microphone;
        [SerializeField] Renderer _selectArea;
        [SerializeField] Renderer _eraser;

        [Tooltip("Material of the indicator ring on the microphone head. Its colour is overridden per renderer, the asset is left untouched.")]
        [SerializeField] Material _micIndicatorMaterial;
        [SerializeField] Color _micIdleColor = Color.red;
        [SerializeField] Color _micActiveColor = Color.green;

        [SerializeField] string _extensionBlendShape = "Extend";

        readonly HashSet<BasisInput> _hoveringInputs = new();
        MaterialPropertyBlock _micBlock;
        int _extensionIndex = -1;
        float _extensionFullWeight = 100f;
        int _micIndicatorIndex = -1;
        bool _initialized;

        bool _visible = true;
        bool _grabbableLocally;
        bool _highlighted;
        PenMode _mode = PenModeSelection.Default;
        float _extension;
        bool? _micActiveShown;

        /// <summary>A hand of this device grabbed the pen.</summary>
        public event Action<BasisInput> Grabbed;

        /// <summary>A hand of this device let go of the pen.</summary>
        public event Action<BasisInput> Released;

        /// <summary>World position of the tip, the centre of the selection sphere.</summary>
        public Vector3 TipPosition => transform.TransformPoint(PenGeometry.TipLocalPosition(_extension));

        /// <summary>World position of the eraser sphere's centre, at the butt.</summary>
        public Vector3 EraserPosition => _eraser != null ? _eraser.transform.position : transform.position;

        /// <summary>Current distance from the butt to the tip.</summary>
        public float Length => PenGeometry.Length(_extension);

        void Awake()
        {
            Initialize();
            // Until a state arrives the pen shows the default mode, not every part the model has.
            ApplyRendererVisibility();
            if (_pickup == null)
            {
                Debug.LogError("[DNPDemo] SpatialPenView: BasisPickupInteractable が設定されていません。", this);
                return;
            }
            _pickup.CanHoverInjected.Add(CanBeGrabbed);
            _pickup.CanInteractInjected.Add(CanBeGrabbed);
            _pickup.OnInteractStartEvent.AddListener(HandleInteractStart);
            _pickup.OnInteractEndEvent.AddListener(HandleInteractEnd);
            _pickup.OnHoverStartEvent += HandleHoverStart;
            _pickup.OnHoverEndEvent += HandleHoverEnd;
        }

        void OnDestroy()
        {
            if (_pickup == null)
            {
                return;
            }
            _pickup.CanHoverInjected.Remove(CanBeGrabbed);
            _pickup.CanInteractInjected.Remove(CanBeGrabbed);
            _pickup.OnInteractStartEvent.RemoveListener(HandleInteractStart);
            _pickup.OnInteractEndEvent.RemoveListener(HandleInteractEnd);
            _pickup.OnHoverStartEvent -= HandleHoverStart;
            _pickup.OnHoverEndEvent -= HandleHoverEnd;
            SetHighlighted(false);
        }

        void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            _micBlock = new MaterialPropertyBlock();

            if (_body != null && _body.sharedMesh != null)
            {
                Mesh mesh = _body.sharedMesh;
                _extensionIndex = mesh.GetBlendShapeIndex(_extensionBlendShape);
                if (_extensionIndex >= 0)
                {
                    int lastFrame = mesh.GetBlendShapeFrameCount(_extensionIndex) - 1;
                    _extensionFullWeight = mesh.GetBlendShapeFrameWeight(_extensionIndex, lastFrame);
                }
            }
            if (_extensionIndex < 0)
            {
                Debug.LogError($"[DNPDemo] SpatialPenView: PenBody にブレンドシェイプ {_extensionBlendShape} がありません。", this);
            }

            if (_microphone != null && _micIndicatorMaterial != null)
            {
                _micIndicatorIndex = Array.IndexOf(_microphone.sharedMaterials, _micIndicatorMaterial);
            }
            if (_micIndicatorIndex < 0)
            {
                Debug.LogError("[DNPDemo] SpatialPenView: PenMicrophone にインジケーターのマテリアルが見つかりません。", this);
            }

            if (_eraser == null)
            {
                Debug.LogError("[DNPDemo] SpatialPenView: PenEraser が設定されていません。", this);
            }
        }

        /// <summary>Shows the mode, radius and trigger state.</summary>
        public void ShowState(PenMode mode, float radius, bool isHeld, bool isTriggerHeld)
        {
            Initialize();
            _mode = mode;
            bool selecting = mode == PenMode.Select;
            bool voice = mode == PenMode.Voice;

            _extension = selecting ? PenGeometry.ExtensionForRadius(radius) : 0f;
            if (_body != null && _extensionIndex >= 0)
            {
                _body.SetBlendShapeWeight(_extensionIndex, _extension * _extensionFullWeight);
            }

            if (_selectArea != null)
            {
                Transform area = _selectArea.transform;
                area.localPosition = PenGeometry.TipLocalPosition(_extension);
                float shownRadius = mode == PenMode.Manipulate ? PenGeometry.GrabRadius : radius;
                area.localScale = Vector3.one * PenGeometry.SelectAreaScale(shownRadius);
            }

            if (_microphone != null)
            {
                SetMicActive(voice && isHeld && isTriggerHeld);
            }
            ApplyRendererVisibility();
        }

        public void ShowState(in PenState state) => ShowState(state.Mode, state.Radius, state.IsHeld, state.IsTriggerHeld);

        /// <summary>
        /// Shows or hides the pen. The object stays active so its pickup and pose sync keep running; only what can be
        /// seen and grabbed is switched.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_visible == visible)
            {
                return;
            }
            _visible = visible;
            if (!visible)
            {
                Drop();
            }
            if (_grabCollider != null)
            {
                _grabCollider.enabled = visible;
            }
            ApplyRendererVisibility();
        }

        /// <summary>Whether this device may grab the pen (only the device of the pen's role may).</summary>
        public void SetGrabbableLocally(bool grabbable)
        {
            _grabbableLocally = grabbable;
            if (!grabbable)
            {
                Drop();
            }
        }

        /// <summary>Lets go of the pen if a hand of this device holds it.</summary>
        public void Drop()
        {
            // Only when held: dropping touches state the pickup sets up in its Start, which may not have run yet.
            if (_pickup != null && _pickup.Inputs.AnyInteracting())
            {
                _pickup.Drop();
            }
        }

        bool CanBeGrabbed(BasisInput input) => _grabbableLocally && _visible;

        void ApplyRendererVisibility()
        {
            if (_body != null)
            {
                _body.enabled = _visible;
            }
            if (_selectArea != null)
            {
                _selectArea.enabled = _visible && (_mode == PenMode.Select || _mode == PenMode.Manipulate);
            }
            if (_microphone != null)
            {
                _microphone.enabled = _visible && _mode == PenMode.Voice;
            }
            if (_eraser != null)
            {
                _eraser.enabled = _visible && _mode == PenMode.Draw;
            }
            if (!_visible)
            {
                SetHighlighted(false);
            }
        }

        void HandleInteractStart(BasisInput input) => Grabbed?.Invoke(input);

        void HandleInteractEnd(BasisInput input) => Released?.Invoke(input);

        // Basis's own hover highlight only covers MeshRenderers, which would light the tip sphere and the microphone
        // but not the body (a SkinnedMeshRenderer), so the pickup's highlight is off and the body is lit here instead.
        void HandleHoverStart(BasisInput input)
        {
            _hoveringInputs.Add(input);
            SetHighlighted(_visible);
        }

        void HandleHoverEnd(BasisInput input, bool willInteract)
        {
            _hoveringInputs.Remove(input);
            if (_hoveringInputs.Count == 0)
            {
                SetHighlighted(false);
            }
        }

        void SetHighlighted(bool highlighted)
        {
            if (_body == null || _highlighted == highlighted)
            {
                return;
            }
            _highlighted = highlighted;
            if (highlighted)
            {
                BasisHighlightManager.Highlight(_body);
            }
            else
            {
                BasisHighlightManager.Unhighlight(_body);
            }
        }

        void SetMicActive(bool active)
        {
            if (_micIndicatorIndex < 0 || _micActiveShown == active)
            {
                return;
            }
            _micActiveShown = active;
            _micBlock.SetColor(BaseColorId, active ? _micActiveColor : _micIdleColor);
            _microphone.SetPropertyBlock(_micBlock, _micIndicatorIndex);
        }
    }
}
