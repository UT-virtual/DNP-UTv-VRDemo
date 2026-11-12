
using UnityEngine;

namespace DNPDemo.Interaction
{
    public enum ZoneShapeType
    {
        Sphere,
        Capsule,
        Box
    }

    /// <summary>
    /// Sphere / Capsule / Box の形状を持つ検出ゾーン。
    /// 点との包含判定、および球との重なり判定を行う。
    /// </summary>
    public class DetectionZone : MonoBehaviour
    {
        [Header("ゾーン形状設定")]
        [SerializeField]
        private ZoneShapeType _shapeType = ZoneShapeType.Sphere;

        public ZoneShapeType ShapeType
        {
            get => _shapeType;
            set
            {
                _shapeType = value;

                if (_shapeType == ZoneShapeType.Capsule)
                {
                    _height = Mathf.Max(_height, _radius * 2f);
                }
            }
        }

        [Header("サイズパラメータ")]
        [SerializeField, Min(0f)]
        private float _radius = 0.15f;

        public float Radius
        {
            get => _radius;
            set
            {
                _radius = Mathf.Max(0f, value);

                if (_shapeType == ZoneShapeType.Capsule)
                {
                    _height = Mathf.Max(_height, _radius * 2f);
                }
            }
        }

        [SerializeField, Min(0f)]
        private float _height = 0.4f;

        public float Height
        {
            get => _height;
            set => _height = _shapeType == ZoneShapeType.Capsule
                ? Mathf.Max(_radius * 2f, value)
                : Mathf.Max(0f, value);
        }

        [SerializeField]
        private Vector3 _boxSize = new Vector3(0.2f, 0.2f, 0.2f);

        public Vector3 BoxSize
        {
            get => _boxSize;
            set
            {
                _boxSize = new Vector3(
                    Mathf.Max(0f, value.x),
                    Mathf.Max(0f, value.y),
                    Mathf.Max(0f, value.z)
                );
            }
        }

        [Header("ギズモ表示色")]
        [SerializeField]
        private Color _gizmoColor = new Color(0f, 1f, 0.2f, 0.5f);

        private const float Epsilon = 0.000001f;

        private void OnValidate()
        {
            _radius = Mathf.Max(0f, _radius);
            _height = Mathf.Max(0f, _height);

            _boxSize = new Vector3(
                Mathf.Max(0f, _boxSize.x),
                Mathf.Max(0f, _boxSize.y),
                Mathf.Max(0f, _boxSize.z)
            );

            if (_shapeType == ZoneShapeType.Capsule)
            {
                _height = Mathf.Max(_height, _radius * 2f);
            }
        }

        // --------------------------------------------------
        // Public API
        // --------------------------------------------------

        /// <summary>
        /// ワールド座標の点がゾーン内部にあるか判定する。
        /// </summary>
        public bool ContainsPoint(Vector3 worldPoint)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

            return CheckOverlap(
                localPoint,
                _shapeType,
                _radius,
                _height,
                _boxSize,
                0f
            );
        }

        /// <summary>
        /// ワールド座標の球とゾーンが重なっているか判定する。
        ///
        /// 注意：
        /// 均等スケールでは正確な判定になる。
        /// 不均等スケールでは最小スケールを使った
        /// 保守的な判定（偽陽性の可能性あり）となる。
        /// </summary>
        public bool OverlapsSphere(
            Vector3 sphereCenter,
            float sphereRadius)
        {
            sphereRadius = Mathf.Max(0f, sphereRadius);

            Vector3 scale = transform.lossyScale;

            float minScale = Mathf.Min(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.y),
                Mathf.Abs(scale.z)
            );

            // ゼロスケールの場合は判定できない
            if (minScale < Epsilon)
            {
                return false;
            }

            // ワールド単位の球の半径をローカル単位に変換
            //
            // 均等スケール：正確
            // 不均等スケール：最小スケールを使った保守的な近似
            float localMargin = sphereRadius / minScale;

            Vector3 localCenter =
                transform.InverseTransformPoint(sphereCenter);

            return CheckOverlap(
                localCenter,
                _shapeType,
                _radius,
                _height,
                _boxSize,
                localMargin
            );
        }

        // --------------------------------------------------
        // Geometry calculation
        // --------------------------------------------------

        /// <summary>
        /// ローカル座標系で形状との距離を計算する。
        ///
        /// Transformやフィールドを書き換えない静的判定関数。
        /// marginは形状の外側に追加する膨張量。
        /// </summary>
        private static bool CheckOverlap(
            Vector3 point,
            ZoneShapeType shape,
            float radius,
            float height,
            Vector3 boxSize,
            float margin)
        {
            radius = Mathf.Max(0f, radius);
            height = Mathf.Max(0f, height);
            margin = Mathf.Max(0f, margin);

            switch (shape)
            {
                // ------------------------------------------
                // Sphere
                // ------------------------------------------
                case ZoneShapeType.Sphere:
                {
                    float expandedRadius = radius + margin;

                    return point.sqrMagnitude
                        <= expandedRadius * expandedRadius;
                }

                // ------------------------------------------
                // Capsule
                // ------------------------------------------
                case ZoneShapeType.Capsule:
                {
                    // 元のカプセル形状から芯の線分を計算する。
                    // marginをradiusに加える前に計算することが重要。
                    float halfSegment = Mathf.Max(
                        0f,
                        height * 0.5f - radius
                    );

                    // 線分上の最近接点
                    float clampedY = Mathf.Clamp(
                        point.y,
                        -halfSegment,
                        halfSegment
                    );

                    Vector3 closestPoint =
                        new Vector3(0f, clampedY, 0f);

                    float expandedRadius = radius + margin;

                    return (point - closestPoint).sqrMagnitude
                        <= expandedRadius * expandedRadius;
                }

                // ------------------------------------------
                // Box
                // ------------------------------------------
                case ZoneShapeType.Box:
                {
                    Vector3 half = boxSize * 0.5f;

                    // ボックス上の最近接点を求める
                    Vector3 closestPoint = new Vector3(
                        Mathf.Clamp(point.x, -half.x, half.x),
                        Mathf.Clamp(point.y, -half.y, half.y),
                        Mathf.Clamp(point.z, -half.z, half.z)
                    );

                    // 最近接点との距離による正確な膨張判定
                    // 角部分は球状に膨張する
                    return (point - closestPoint).sqrMagnitude
                        <= margin * margin;
                }
            }

            return false;
        }

        // --------------------------------------------------
        // Gizmos
        // --------------------------------------------------

        private void OnDrawGizmos()
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;

            Gizmos.color = _gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            switch (_shapeType)
            {
                case ZoneShapeType.Sphere:
                {
                    Gizmos.DrawWireSphere(
                        Vector3.zero,
                        _radius
                    );
                    break;
                }

                case ZoneShapeType.Box:
                {
                    Gizmos.DrawWireCube(
                        Vector3.zero,
                        _boxSize
                    );
                    break;
                }

                case ZoneShapeType.Capsule:
                {
                    float halfSegment = Mathf.Max(
                        0f,
                        _height * 0.5f - _radius
                    );

                    // 上下の半球
                    Gizmos.DrawWireSphere(
                        Vector3.up * halfSegment,
                        _radius
                    );

                    Gizmos.DrawWireSphere(
                        Vector3.down * halfSegment,
                        _radius
                    );

                    // 側面の接続線
                    Gizmos.DrawLine(
                        new Vector3(-_radius, -halfSegment, 0f),
                        new Vector3(-_radius, halfSegment, 0f)
                    );

                    Gizmos.DrawLine(
                        new Vector3(_radius, -halfSegment, 0f),
                        new Vector3(_radius, halfSegment, 0f)
                    );

                    Gizmos.DrawLine(
                        new Vector3(0f, -halfSegment, -_radius),
                        new Vector3(0f, halfSegment, -_radius)
                    );

                    Gizmos.DrawLine(
                        new Vector3(0f, -halfSegment, _radius),
                        new Vector3(0f, halfSegment, _radius)
                    );

                    break;
                }
            }

            // Gizmosの状態を元に戻す
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
