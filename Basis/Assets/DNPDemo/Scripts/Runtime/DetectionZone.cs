using UnityEngine;

public enum ZoneShapeType
{
    Sphere,
    Capsule,
    Box
}

/// <summary>
/// 部位ゾーン・目標ゾーンを表し、点やスフィアとの重なり判定およびギズモ表示を行うコンポーネント (#9)
/// </summary>
public class DetectionZone : MonoBehaviour
{
    [Header("ゾーン形状設定")]
    public ZoneShapeType shapeType = ZoneShapeType.Sphere;

    [Header("サイズパラメータ")]
    public float radius = 0.15f;           // 球・カプセルの半径
    public float height = 0.4f;            // カプセルの全高（Y軸方向）
    public Vector3 boxSize = new Vector3(0.2f, 0.2f, 0.2f); // ボックスの寸法

    [Header("ギズモ表示色")]
    public Color gizmoColor = new Color(0f, 1f, 0.2f, 0.5f);

    /// <summary>
    /// 指定されたワールド座標（点）がゾーン内にあるか判定
    /// </summary>
    public bool ContainsPoint(Vector3 worldPoint)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint);

        switch (shapeType)
        {
            case ZoneShapeType.Sphere:
                return local.magnitude <= radius;

            case ZoneShapeType.Box:
                Vector3 half = boxSize * 0.5f;
                return Mathf.Abs(local.x) <= half.x &&
                       Mathf.Abs(local.y) <= half.y &&
                       Mathf.Abs(local.z) <= half.z;

            case ZoneShapeType.Capsule:
                float halfSegment = Mathf.Max(0f, (height * 0.5f) - radius);
                float clampedY = Mathf.Clamp(local.y, -halfSegment, halfSegment);
                Vector3 pointOnSegment = new Vector3(0f, clampedY, 0f);
                return Vector3.Distance(local, pointOnSegment) <= radius;
        }

        return false;
    }

    /// <summary>
    /// 指定されたスフィア（中心と半径）がゾーンと重なっているか判定
    /// </summary>
    public bool OverlapsSphere(Vector3 sphereCenter, float sphereRadius)
    {
        float origRadius = radius;
        Vector3 origBoxSize = boxSize;

        radius += sphereRadius;
        boxSize += Vector3.one * (sphereRadius * 2f);

        bool isOverlapping = ContainsPoint(sphereCenter);

        radius = origRadius;
        boxSize = origBoxSize;

        return isOverlapping;
    }

    // シーンビューでゾーン枠を表示する（ギズモ）
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;

        switch (shapeType)
        {
            case ZoneShapeType.Sphere:
                Gizmos.DrawWireSphere(Vector3.zero, radius);
                break;

            case ZoneShapeType.Box:
                Gizmos.DrawWireCube(Vector3.zero, boxSize);
                break;

            case ZoneShapeType.Capsule:
                float halfSegment = Mathf.Max(0f, (height * 0.5f) - radius);
                Gizmos.DrawWireSphere(Vector3.up * halfSegment, radius);
                Gizmos.DrawWireSphere(Vector3.down * halfSegment, radius);
                Gizmos.DrawLine(new Vector3(-radius, -halfSegment, 0f), new Vector3(-radius, halfSegment, 0f));
                Gizmos.DrawLine(new Vector3(radius, -halfSegment, 0f), new Vector3(radius, halfSegment, 0f));
                Gizmos.DrawLine(new Vector3(0f, -halfSegment, -radius), new Vector3(0f, halfSegment, -radius));
                Gizmos.DrawLine(new Vector3(0f, -halfSegment, radius), new Vector3(0f, halfSegment, radius));
                break;
        }
    }
}
