using DNPDemo.Interaction;
using UnityEngine;

namespace DNPDemo.Pen
{
    /// <summary>
    /// Draws one pen from a <see cref="PenState"/>. It only shows what it is told; the local player's pen is driven by
    /// <see cref="SpatialPenController"/>, the other player's by <see cref="SpatialPenSync"/>.
    /// <list type="bullet">
    /// <item>Select: the tip sphere (<c>PenSelectArea</c>) at the selection radius. Above
    /// <see cref="PenGeometry.ExtensionStartRadius"/> the body extends so the sphere stays clear of the hand.</item>
    /// <item>Voice: the microphone head (<c>PenMicrophone</c>); its indicator ring turns from idle to active colour
    /// while the pen is held with the trigger down.</item>
    /// <item>Draw: the body only.</item>
    /// </list>
    /// </summary>
    public sealed class SpatialPenView : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] SkinnedMeshRenderer _body;
        [SerializeField] Renderer _microphone;
        [SerializeField] Renderer _selectArea;

        [Tooltip("Material of the indicator ring on the microphone head. Its colour is overridden per renderer, the asset is left untouched.")]
        [SerializeField] Material _micIndicatorMaterial;
        [SerializeField] Color _micIdleColor = Color.red;
        [SerializeField] Color _micActiveColor = Color.green;

        [SerializeField] string _extensionBlendShape = "Extend";

        [Tooltip("How quickly the other player's pen catches up with received poses (higher is snappier).")]
        [SerializeField, Min(1f)] float _remoteSharpness = 20f;

        MaterialPropertyBlock _micBlock;
        int _extensionIndex = -1;
        float _extensionFullWeight = 100f;
        int _micIndicatorIndex = -1;
        bool _initialized;

        float _extension;
        bool? _micActiveShown;

        bool _followsRemoteTarget;
        Vector3 _targetPosition;
        Quaternion _targetRotation = Quaternion.identity;

        /// <summary>World position of the tip, the centre of the selection sphere.</summary>
        public Vector3 TipPosition => transform.TransformPoint(PenGeometry.TipLocalPosition(_extension));

        /// <summary>Current distance from the butt to the tip.</summary>
        public float Length => PenGeometry.Length(_extension);

        public Pose WorldPose => new(transform.position, transform.rotation);

        void Awake() => Initialize();

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
                _micIndicatorIndex = System.Array.IndexOf(_microphone.sharedMaterials, _micIndicatorMaterial);
            }
            if (_micIndicatorIndex < 0)
            {
                Debug.LogError("[DNPDemo] SpatialPenView: PenMicrophone にインジケーターのマテリアルが見つかりません。", this);
            }
        }

        /// <summary>Shows the mode, radius and trigger state. Leaves the pose alone.</summary>
        public void ShowState(PenMode mode, float radius, bool isHeld, bool isTriggerHeld)
        {
            Initialize();
            bool selecting = mode == PenMode.Select;
            bool voice = mode == PenMode.Voice;

            _extension = selecting ? PenGeometry.ExtensionForRadius(radius) : 0f;
            if (_body != null && _extensionIndex >= 0)
            {
                _body.SetBlendShapeWeight(_extensionIndex, _extension * _extensionFullWeight);
            }

            if (_selectArea != null)
            {
                _selectArea.enabled = selecting;
                Transform area = _selectArea.transform;
                area.localPosition = PenGeometry.TipLocalPosition(_extension);
                area.localScale = Vector3.one * PenGeometry.SelectAreaScale(radius);
            }

            if (_microphone != null)
            {
                _microphone.enabled = voice;
                SetMicActive(voice && isHeld && isTriggerHeld);
            }
        }

        /// <summary>Places the pen at once (the local player's pen).</summary>
        public void SetPose(Pose pose)
        {
            _followsRemoteTarget = false;
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        /// <summary>Moves the pen towards a received pose; <paramref name="snap"/> jumps there instead.</summary>
        public void SetRemoteTarget(Pose pose, bool snap)
        {
            _targetPosition = pose.position;
            _targetRotation = pose.rotation;
            _followsRemoteTarget = true;
            if (snap)
            {
                transform.SetPositionAndRotation(pose.position, pose.rotation);
            }
        }

        public void ShowRemoteState(in PenState state, bool snap)
        {
            ShowState(state.Mode, state.Radius, state.IsHeld, state.IsTriggerHeld);
            SetRemoteTarget(new Pose(state.Position, state.Rotation), snap);
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        void LateUpdate()
        {
            if (!_followsRemoteTarget)
            {
                return;
            }
            float t = 1f - Mathf.Exp(-_remoteSharpness * Time.deltaTime);
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, _targetPosition, t),
                Quaternion.Slerp(transform.rotation, _targetRotation, t));
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
