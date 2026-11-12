using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Interaction
{
    /// <summary>
    /// 複数のDetectionZoneを1つの複合ゾーンとして扱う。
    /// いずれか1つのゾーンに入れば検出成功とする。
    ///
    /// 仕様：
    /// ・手動登録ゾーン（インスペクタ / AddZone）と、
    ///   子階層からの自動収集ゾーンの和集合を判定対象とする。
    /// ・Group自身に付いたDetectionZoneも対象に含む。
    /// ・Groupが入れ子の場合、親Groupは子Group配下のゾーンも収集する。
    /// ・ゾーンが0個の場合、判定は常にfalse。
    /// ・無効化されたゾーン（isActiveAndEnabled == false）は判定時に無視する。
    /// </summary>
    public class DetectionZoneGroup : MonoBehaviour
    {
        [Header("グループ設定")]
        [SerializeField]
        private bool _autoCollectChildren = true;

        [SerializeField]
        private bool _includeInactive = true;

        [Header("手動登録ゾーン（子階層外も可）")]
        [SerializeField]
        private List<DetectionZone> _manualZones =
            new List<DetectionZone>();

        // 実行時に組み立てる判定対象（手動 + 自動）
        private readonly List<DetectionZone> _zones =
            new List<DetectionZone>();

        private bool _initialized;

        public IReadOnlyList<DetectionZone> Zones
        {
            get
            {
                EnsureInitialized();
                return _zones;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// 判定対象リストを再構築する。
        /// 手動登録ゾーンは保持され、子階層の自動収集分のみ再取得される。
        /// </summary>
        public void RefreshZones()
        {
            _zones.Clear();

            foreach (DetectionZone zone in _manualZones)
            {
                AddUnique(zone);
            }

            if (_autoCollectChildren)
            {
                DetectionZone[] found =
                    GetComponentsInChildren<DetectionZone>(
                        _includeInactive
                    );

                foreach (DetectionZone zone in found)
                {
                    AddUnique(zone);
                }
            }

            _initialized = true;
        }

        /// <summary>
        /// グループ内のいずれかのゾーンに点が入っているか。
        /// </summary>
        public bool ContainsPoint(Vector3 worldPoint)
        {
            EnsureInitialized();

            foreach (DetectionZone zone in _zones)
            {
                if (zone == null || !zone.isActiveAndEnabled)
                    continue;

                if (zone.ContainsPoint(worldPoint))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// グループ内のいずれかのゾーンと球が重なるか。
        /// </summary>
        public bool OverlapsSphere(
            Vector3 sphereCenter,
            float sphereRadius)
        {
            EnsureInitialized();

            foreach (DetectionZone zone in _zones)
            {
                if (zone == null || !zone.isActiveAndEnabled)
                    continue;

                if (zone.OverlapsSphere(sphereCenter, sphereRadius))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 個別ゾーンを手動登録する（子階層外でも可）。
        /// </summary>
        public void AddZone(DetectionZone zone)
        {
            if (zone == null)
                return;

            EnsureInitialized();

            if (!_manualZones.Contains(zone))
            {
                _manualZones.Add(zone);
            }

            AddUnique(zone);
        }

        /// <summary>
        /// ゾーンを手動登録および判定対象から外す。
        /// 自動収集対象の場合、次回RefreshZonesで再び入る。
        /// </summary>
        public void RemoveZone(DetectionZone zone)
        {
            if (zone == null)
                return;

            _manualZones.Remove(zone);
            _zones.Remove(zone);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            RefreshZones();
        }

        private void AddUnique(DetectionZone zone)
        {
            if (zone != null && !_zones.Contains(zone))
            {
                _zones.Add(zone);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // インスペクタ変更後に再構築させる
            _initialized = false;
        }
#endif
    }
}