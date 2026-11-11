using NUnit.Framework;
using UnityEngine;

public class DetectionZoneTests
{
    private GameObject testGo;
    private DetectionZone zone;

    [SetUp]
    public void SetUp()
    {
        testGo = new GameObject("TestZone");
        zone = testGo.AddComponent<DetectionZone>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testGo);
    }

    [Test]
    public void Sphere_ContainsPoint_InsideAndOutside()
    {
        zone.shapeType = ZoneShapeType.Sphere;
        zone.radius = 1.0f;
        zone.transform.position = Vector3.zero;

        // 原点は球の内側
        Assert.IsTrue(zone.ContainsPoint(Vector3.zero));
        // 半径以内の点は内側
        Assert.IsTrue(zone.ContainsPoint(new Vector3(0.5f, 0f, 0f)));
        // 半径を超える点は外側
        Assert.IsFalse(zone.ContainsPoint(new Vector3(1.5f, 0f, 0f)));
    }

    [Test]
    public void Box_ContainsPoint_InsideAndOutside()
    {
        zone.shapeType = ZoneShapeType.Box;
        zone.boxSize = new Vector3(2f, 2f, 2f); // 各軸 -1〜1 の範囲
        zone.transform.position = Vector3.zero;

        // 範囲内の点
        Assert.IsTrue(zone.ContainsPoint(new Vector3(0.5f, -0.5f, 0f)));
        // 範囲外の点
        Assert.IsFalse(zone.ContainsPoint(new Vector3(1.2f, 0f, 0f)));
    }

    [Test]
    public void Capsule_ContainsPoint_InsideAndOutside()
    {
        zone.shapeType = ZoneShapeType.Capsule;
        zone.radius = 0.5f;
        zone.height = 2.0f;
        zone.transform.position = Vector3.zero;

        // 中心線分付近（内側）
        Assert.IsTrue(zone.ContainsPoint(new Vector3(0f, 0.8f, 0f)));
        // 外側の点
        Assert.IsFalse(zone.ContainsPoint(new Vector3(1.0f, 0f, 0f)));
    }

    [Test]
    public void OverlapsSphere_DetectsOverlap()
    {
        zone.shapeType = ZoneShapeType.Sphere;
        zone.radius = 1.0f;
        zone.transform.position = Vector3.zero;

        // 中心は外側だが、スフィア自体の半径によってゾーンと接触するケース
        Vector3 targetCenter = new Vector3(1.3f, 0f, 0f);
        float targetRadius = 0.5f;

        Assert.IsTrue(zone.OverlapsSphere(targetCenter, targetRadius));
    }
}