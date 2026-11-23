using System;
using Basis.Network.Core;
using DNPDemo.Scenario;
using DNPDemo.Venus;
using UnityEngine;

namespace DNPDemo.Interaction.History
{
    /// <summary>
    /// Runs <see cref="HistoryPart"/> through the <see cref="HistoryPanel"/>. On the actor's device it enables
    /// 「モデルを復元」for as long as the part is current; each restore shows the version on the comparison statue and sends
    /// its id as progress, so the other device shows it too. Restoring <see cref="HistoryPart.TargetVersion"/> closes the
    /// panel and completes the part.
    /// <para>
    /// What the comparison statue shows afterwards follows from the step index: the stage resets it when the step changes and
    /// the next step's <see cref="Scenario.Cues.StatueVersionCue"/> shows the target version, so this tool never puts it back.
    /// </para>
    /// </summary>
    public sealed class HistoryTool : InteractionTool
    {
        [SerializeField] HistoryPanel _panel;
        [SerializeField] ScenarioStage _stage;

        InteractionSession _session;
        HistoryPart _part;
        VenusVersion _restored;
        bool _accepting;

        /// <summary>Status line after restoring a version other than the target.</summary>
        public static string FormatRestored(VenusVersion version) => $"比較台に {version} を表示しました";

        void OnDestroy() => StopAccepting();

        public override bool CanRun(InteractionPart part) => part is HistoryPart;

        public override void Begin(InteractionSession session)
        {
            StopAccepting();
            _session = session;
            _part = (HistoryPart)session.Part;
            _restored = null;
            if (session.IsLocalActor && _panel != null)
            {
                _panel.RestoreRequested += HandleRestoreRequested;
                _accepting = true;
                _panel.SetRestoreAllowed(true);
            }
        }

        public override void End()
        {
            StopAccepting();
            _session = null;
        }

        public override void Discard()
        {
            StopAccepting();
            _session = null;
            _part = null;
            _restored = null;
        }

        public override void OnRemoteProgress(InteractionSession session, ArraySegment<byte> payload)
        {
            if (!HistoryMessage.TryParse(payload, out string id) || !TryGetComparisonStatue(out VenusStatue statue))
            {
                return;
            }
            VenusVersion version = statue.Catalog != null ? statue.Catalog.Find(id) : null;
            if (version == null)
            {
                Debug.LogWarning($"[DNPDemo] 履歴から復元された版「{id}」が比較台の像のカタログにありません。", this);
                return;
            }
            _restored = version;
            statue.Show(version, true);
        }

        public override byte[] CreateSnapshot(InteractionSession session) =>
            _restored != null ? HistoryMessage.Serialize(_restored.Id) : null;

        void HandleRestoreRequested(VenusVersion version)
        {
            InteractionSession session = _session;
            if (session == null || !session.IsLocalActor || !session.IsCurrent || version == null)
            {
                return;
            }
            if (TryGetComparisonStatue(out VenusStatue statue))
            {
                statue.Show(version, true);
            }
            _restored = version;
            byte[] payload = HistoryMessage.Serialize(version.Id);
            if (payload != null)
            {
                session.SendProgress(payload, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                Debug.LogWarning($"[DNPDemo] {version.name} の Id「{version.Id}」は送信できません。", version);
            }

            if (version == _part.TargetVersion)
            {
                _panel.Close();
                session.Complete();
            }
            else
            {
                _panel.SetStatus(FormatRestored(version));
            }
        }

        bool TryGetComparisonStatue(out VenusStatue statue)
        {
            statue = null;
            if (_part == null || _stage == null || !_stage.TryGetObject(_part.ComparisonStatueKey, out GameObject target))
            {
                return false;
            }
            if (!target.TryGetComponent(out statue))
            {
                Debug.LogWarning($"[DNPDemo] キー「{_part.ComparisonStatueKey}」のオブジェクトに VenusStatue がありません。", target);
                return false;
            }
            return true;
        }

        void StopAccepting()
        {
            if (!_accepting)
            {
                return;
            }
            _accepting = false;
            if (_panel != null)
            {
                _panel.RestoreRequested -= HandleRestoreRequested;
                _panel.SetRestoreAllowed(false);
            }
        }
    }
}
