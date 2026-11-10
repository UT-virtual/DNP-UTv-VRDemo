using System;
using Basis.Network.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DNPDemo.Interaction
{
    /// <summary>
    /// Runs <see cref="DummyInteractionPart"/>: a panel in front of the player. The actor presses its button
    /// (F8 on desktop and in the editor) the required number of times; the other device shows the same count,
    /// sent as progress.
    /// </summary>
    public sealed class DummyInteractionTool : InteractionTool
    {
        [SerializeField] GameObject _content;
        [SerializeField] TMP_Text _titleText;
        [SerializeField] TMP_Text _statusText;
        [SerializeField] Button _pressButton;

        readonly byte[] _payload = new byte[1];

        InteractionSession _session;
        DummyInteractionPart _part;
        int _partIndex;
        int _partCount;
        DemoRole _actor;
        bool _isLocalActor;
        int _count;

        void Awake()
        {
            if (_pressButton != null)
            {
                _pressButton.onClick.AddListener(Press);
            }
            SetVisible(false);
        }

        void OnDestroy()
        {
            if (_pressButton != null)
            {
                _pressButton.onClick.RemoveListener(Press);
            }
        }

        void Update()
        {
            if (_session != null && _isLocalActor)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.f8Key.wasPressedThisFrame)
                {
                    Press();
                }
            }
        }

        public override bool CanRun(InteractionPart part) => part is DummyInteractionPart;

        public override void Begin(InteractionSession session)
        {
            _session = session;
            _part = (DummyInteractionPart)session.Part;
            _partIndex = session.PartIndex;
            _partCount = session.PartCount;
            _actor = session.Actor;
            _isLocalActor = session.IsLocalActor;
            _count = 0;
            SetVisible(true);
            Refresh();
        }

        public override void End()
        {
            _session = null;
            Refresh();
        }

        public override void Discard()
        {
            _session = null;
            _part = null;
            _count = 0;
            SetVisible(false);
        }

        public override void OnRemoteProgress(InteractionSession session, ArraySegment<byte> payload)
        {
            if (payload.Count < 1)
            {
                return;
            }
            _count = payload.Array[payload.Offset];
            Refresh();
        }

        void Press()
        {
            if (_session == null || !_session.IsLocalActor || !_session.IsCurrent)
            {
                return;
            }
            _count = Mathf.Min(_count + 1, Required);
            _payload[0] = (byte)_count;
            _session.SendProgress(_payload, DeliveryMethod.ReliableOrdered);
            Refresh();
            if (_count >= Required)
            {
                _session.Complete();
            }
        }

        int Required => Mathf.Clamp(_part != null ? _part.RequiredPresses : 1, 1, byte.MaxValue);

        void Refresh()
        {
            if (_part == null)
            {
                return;
            }
            if (_titleText != null)
            {
                _titleText.text = $"手順 {_partIndex + 1}/{_partCount}（{_part.Mode.ToDisplayName()}・仮）\n{_part.Instruction}";
            }
            bool accepting = _session != null && _isLocalActor;
            if (_statusText != null)
            {
                string count = $"{_count}/{Required}";
                if (_session == null)
                {
                    _statusText.text = $"この手順は完了しました（{count}）";
                }
                else if (_isLocalActor)
                {
                    _statusText.text = $"「完了」を押してください（{count}）";
                }
                else
                {
                    _statusText.text = $"{_actor.ToDisplayName()} が操作中（{count}）";
                }
            }
            if (_pressButton != null)
            {
                _pressButton.gameObject.SetActive(_isLocalActor);
                _pressButton.interactable = accepting;
            }
        }

        void SetVisible(bool visible)
        {
            if (_content != null)
            {
                _content.SetActive(visible);
            }
        }
    }
}
