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
    ///
    /// スケールの制約：
    /// ゾーン自身と、すべての祖先の localScale が均一であること。
    /// 非均一スケールは扱いが難しくメリットが小さいため禁止する。
    /// （IsScaleHierarchyUniform で検査し、OnValidate と Editor テストで検出する）
    /// </summary>
    public class DetectionZone : MonoBehaviour, IDetectionVolume
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

        // 各軸スケールの差が最大値に対してこの比率以内なら均一とみなす
        private const float UniformTolerance = 0.001f;

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

            if (!IsScaleHierarchyUniform())
            {
                Debug.LogWarning(
                    $"[DetectionZone] '{name}' 自身または祖先の localScale が非均一です。" +
                    "非均一スケールは禁止です。OverlapsSphere は最小スケールによる近似になります。",
                    this
                );
            }
        }

        // --------------------------------------------------
        // Scale validation
        // --------------------------------------------------

        /// <summary>
        /// ゾーン自身と、すべての祖先の localScale が均一かを返す。
        /// （符号だけ反転したミラーは形状が歪まないため、絶対値で比較する）
        /// </summary>
        public bool IsScaleHierarchyUniform()
        {
            for (Transform t = transform; t != null; t = t.parent)
            {
                if (!IsUniformScale(t.localScale))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 3軸のスケールが均一かを返す（絶対値で比較）。
        /// </summary>
        public static bool IsUniformScale(Vector3 scale)
        {
            float x = Mathf.Abs(scale.x);
            float y = Mathf.Abs(scale.y);
            float z = Mathf.Abs(scale.z);

            float max = Mathf.Max(x, y, z);
            float min = Mathf.Min(x, y, z);

            return max - min <= UniformTolerance * max;
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
        /// 前提：ゾーン自身と祖先の localScale が均一であること
        /// （IsScaleHierarchyUniform）。この前提では厳密な判定になる。
        /// 前提に反する場合は、最小スケールを使った近似となり、
        /// 偽陽性（実際には重ならないのに重なると判定）が出る。
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

        /// <summary>
        /// ゾーン形状を囲むワールド座標の軸平行ボックス。
        /// 回転している場合は形状より大きくなる。
        /// </summary>
        public Bounds WorldBounds
        {
            get
            {
                Vector3 localExtents = GetLocalExtents(
                    _shapeType,
                    _radius,
                    _height,
                    _boxSize
                );

                // ローカルの箱を変換したときの各ワールド軸の半幅は、
                // 変換行列の各行の絶対値とローカル半幅の内積になる
                Matrix4x4 m = transform.localToWorldMatrix;

                Vector3 worldExtents = new Vector3(
                    Mathf.Abs(m.m00) * localExtents.x
                        + Mathf.Abs(m.m01) * localExtents.y
                        + Mathf.Abs(m.m02) * localExtents.z,
                    Mathf.Abs(m.m10) * localExtents.x
                        + Mathf.Abs(m.m11) * localExtents.y
                        + Mathf.Abs(m.m12) * localExtents.z,
                    Mathf.Abs(m.m20) * localExtents.x
                        + Mathf.Abs(m.m21) * localExtents.y
                        + Mathf.Abs(m.m22) * localExtents.z
                );

                return new Bounds(transform.position, worldExtents * 2f);
            }
        }

        // --------------------------------------------------
        // Geometry calculation
        // --------------------------------------------------

        /// <summary>
        /// ローカル座標系での形状の半幅（軸平行ボックス）。
        /// </summary>
        private static Vector3 GetLocalExtents(
            ZoneShapeType shape,
            float radius,
            float height,
            Vector3 boxSize)
        {
            radius = Mathf.Max(0f, radius);

            switch (shape)
            {
                case ZoneShapeType.Sphere:
                    return new Vector3(radius, radius, radius);

                case ZoneShapeType.Capsule:
                    return new Vector3(
                        radius,
                        Mathf.Max(height * 0.5f, radius),
                        radius
                    );

                case ZoneShapeType.Box:
                    return new Vector3(
                        Mathf.Max(0f, boxSize.x),
                        Mathf.Max(0f, boxSize.y),
                        Mathf.Max(0f, boxSize.z)
                    ) * 0.5f;
            }

            return Vector3.zero;
        }

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
                    // 芯の線分は、元の radius / height から計算する。
                    // marginを加えた半径では計算しない
                    // （球の半径を足しても線分の長さは変わらない）。
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
                    Gizmos.DrawWireSphere(Vector3.zero, _radius);
                    break;
                }

                case ZoneShapeType.Box:
                {
                    Gizmos.DrawWireCube(Vector3.zero, _boxSize);
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
                        Vector3.up * halfSegment, _radius);
                    Gizmos.DrawWireSphere(
                        Vector3.down * halfSegment, _radius);

                    // 側面の接続線
                    Gizmos.DrawLine(
                        new Vector3(-_radius, -halfSegment, 0f),
                        new Vector3(-_radius, halfSegment, 0f));
                    Gizmos.DrawLine(
                        new Vector3(_radius, -halfSegment, 0f),
                        new Vector3(_radius, halfSegment, 0f));
                    Gizmos.DrawLine(
                        new Vector3(0f, -halfSegment, -_radius),
                        new Vector3(0f, halfSegment, -_radius));
                    Gizmos.DrawLine(
                        new Vector3(0f, -halfSegment, _radius),
                        new Vector3(0f, halfSegment, _radius));

                    break;
                }
            }

            // Gizmosの状態を元に戻す
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
