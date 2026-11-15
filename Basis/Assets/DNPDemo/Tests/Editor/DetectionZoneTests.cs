using NUnit.Framework;
using UnityEngine;
using DNPDemo.Interaction;

namespace DNPDemo.Tests
{
    public class DetectionZoneTests
    {
        private readonly System.Collections.Generic.List<GameObject> _created =
            new System.Collections.Generic.List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
            _created.Clear();
        }

        // --------------------------------------------------
        // Helpers
        // --------------------------------------------------

        private GameObject NewObject(
            string name,
            Transform parent = null)
        {
            var go = new GameObject(name);
            _created.Add(go);
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go;
        }

        private DetectionZone NewSphere(
            float radius,
            Transform parent = null)
        {
            var zone = NewObject("Sphere", parent)
                .AddComponent<DetectionZone>();
            zone.ShapeType = ZoneShapeType.Sphere;
            zone.Radius = radius;
            return zone;
        }

        private DetectionZone NewCapsule(
            float radius,
            float height,
            Transform parent = null)
        {
            var zone = NewObject("Capsule", parent)
                .AddComponent<DetectionZone>();
            zone.Radius = radius;
            zone.Height = height;
            zone.ShapeType = ZoneShapeType.Capsule;
            return zone;
        }

        private DetectionZone NewBox(
            Vector3 size,
            Transform parent = null)
        {
            var zone = NewObject("Box", parent)
                .AddComponent<DetectionZone>();
            zone.ShapeType = ZoneShapeType.Box;
            zone.BoxSize = size;
            return zone;
        }

        // --------------------------------------------------
        // ContainsPoint
        // --------------------------------------------------

        [Test]
        public void Sphere_ContainsPoint_InsideAndOutside()
        {
            var zone = NewSphere(1f);

            Assert.IsTrue(zone.ContainsPoint(new Vector3(0.9f, 0f, 0f)));
            Assert.IsFalse(zone.ContainsPoint(new Vector3(1.1f, 0f, 0f)));
        }

        [Test]
        public void Sphere_ContainsPoint_ExactBoundary_IsInside()
        {
            var zone = NewSphere(0.5f);

            Assert.IsTrue(zone.ContainsPoint(new Vector3(0.5f, 0f, 0f)));
        }

        [Test]
        public void Capsule_ContainsPoint_HemisphereEnd()
        {
            // 半径0.5 / 高さ2 → 芯は y=-0.5..0.5、端の球中心は (0,±0.5,0)
            var zone = NewCapsule(0.5f, 2f);

            Assert.IsTrue(zone.ContainsPoint(new Vector3(0f, 0.9f, 0f)));
            Assert.IsTrue(zone.ContainsPoint(new Vector3(0f, 1.0f, 0f)));  // 先端ちょうど
            Assert.IsFalse(zone.ContainsPoint(new Vector3(0f, 1.1f, 0f)));

            // 半球の角：円筒としては入るが半球としては外
            Assert.IsFalse(zone.ContainsPoint(new Vector3(0.4f, 0.9f, 0f)));
        }

        [Test]
        public void Capsule_ContainsPoint_Cylinder()
        {
            var zone = NewCapsule(0.5f, 2f);

            Assert.IsTrue(zone.ContainsPoint(new Vector3(0.4f, 0.3f, 0f)));
            Assert.IsFalse(zone.ContainsPoint(new Vector3(0.6f, 0.3f, 0f)));
        }

        [Test]
        public void Box_ContainsPoint()
        {
            var zone = NewBox(new Vector3(2f, 2f, 2f));

            Assert.IsTrue(zone.ContainsPoint(new Vector3(0.9f, 0.9f, 0.9f)));
            Assert.IsTrue(zone.ContainsPoint(new Vector3(1f, 1f, 1f)));    // 角ちょうど
            Assert.IsFalse(zone.ContainsPoint(new Vector3(1.1f, 0f, 0f)));
        }

        // --------------------------------------------------
        // OverlapsSphere
        // --------------------------------------------------

        [Test]
        public void Sphere_OverlapsSphere_BoundaryTouching()
        {
            var zone = NewSphere(0.5f);

            // 中心距離 1.0 == 0.5 + 0.5
            Assert.IsTrue(zone.OverlapsSphere(new Vector3(1f, 0f, 0f), 0.5f));
            Assert.IsFalse(zone.OverlapsSphere(new Vector3(1.01f, 0f, 0f), 0.5f));
        }

        [Test]
        public void Capsule_OverlapsSphere_EndCap_ReviewCase()
        {
            // レビュー指摘#1：距離0.9 <= 1.0 で重なるべき
            var zone = NewCapsule(0.5f, 2f);

            Assert.IsTrue(zone.OverlapsSphere(new Vector3(0f, 1.4f, 0f), 0.5f));
            Assert.IsFalse(zone.OverlapsSphere(new Vector3(0f, 1.6f, 0f), 0.5f));
        }

        [Test]
        public void Capsule_OverlapsSphere_Side()
        {
            var zone = NewCapsule(0.5f, 2f);

            Assert.IsTrue(zone.OverlapsSphere(new Vector3(0.9f, 0f, 0f), 0.5f));
            Assert.IsFalse(zone.OverlapsSphere(new Vector3(1.1f, 0f, 0f), 0.5f));
        }

        [Test]
        public void Box_OverlapsSphere_Face()
        {
            var zone = NewBox(new Vector3(2f, 2f, 2f));

            Assert.IsTrue(zone.OverlapsSphere(new Vector3(1.25f, 0f, 0f), 0.3f));
            Assert.IsFalse(zone.OverlapsSphere(new Vector3(1.35f, 0f, 0f), 0.3f));
        }

        [Test]
        public void Box_OverlapsSphere_Corner_IsRounded_ReviewCase()
        {
            // レビュー指摘#7：角までの実距離は約0.354
            var zone = NewBox(new Vector3(2f, 2f, 2f));
            var center = new Vector3(1.25f, 1.25f, 0f);

            Assert.IsFalse(zone.OverlapsSphere(center, 0.3f));
            Assert.IsTrue(zone.OverlapsSphere(center, 0.36f));
        }

        [Test]
        public void OverlapsSphere_NegativeRadius_TreatedAsZero()
        {
            var zone = NewSphere(0.5f);

            Assert.IsTrue(zone.OverlapsSphere(Vector3.zero, -1f));
            Assert.IsFalse(zone.OverlapsSphere(new Vector3(0.6f, 0f, 0f), -1f));
        }

        // --------------------------------------------------
        // 親のスケール・回転
        // --------------------------------------------------

        [Test]
        public void Sphere_OverlapsSphere_ParentScale_ReviewCase()
        {
            // レビュー指摘#2：親スケール0.5 / radius 1 → ワールド半径0.5
            var parent = NewObject("Parent");
            parent.transform.localScale = Vector3.one * 0.5f;
            var zone = NewSphere(1f, parent.transform);

            Assert.IsTrue(zone.OverlapsSphere(new Vector3(0.75f, 0f, 0f), 0.3f));
            Assert.IsFalse(zone.OverlapsSphere(new Vector3(0.85f, 0f, 0f), 0.3f));
        }

        [Test]
        public void Capsule_ParentRotation_AxisFollowsParent()
        {
            // Z軸90°回転 → ローカルY軸がワールドX軸になる
            var parent = NewObject("Parent");
            parent.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            var zone = NewCapsule(0.5f, 2f, parent.transform);

            Assert.IsTrue(zone.OverlapsSphere(new Vector3(1.4f, 0f, 0f), 0.5f));
            Assert.IsFalse(zone.OverlapsSphere(new Vector3(0f, 1.4f, 0f), 0.5f));
        }

        [Test]
        public void Box_ParentRotationAndScale()
        {
            var parent = NewObject("Parent");
            parent.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            parent.transform.localScale = Vector3.one * 2f;
            var zone = NewBox(new Vector3(1f, 2f, 1f), parent.transform);

            // ワールドでは X方向の半長 = 2、Y方向の半幅 = 1
            Assert.IsTrue(zone.ContainsPoint(new Vector3(1.9f, 0f, 0f)));
            Assert.IsFalse(zone.ContainsPoint(new Vector3(0f, 1.1f, 0f)));
        }

        [Test]
        public void OverlapsSphere_ZeroScale_ReturnsFalse()
        {
            var parent = NewObject("Parent");
            parent.transform.localScale = Vector3.zero;
            var zone = NewSphere(1f, parent.transform);

            Assert.IsFalse(zone.OverlapsSphere(Vector3.zero, 0.5f));
        }

        // --------------------------------------------------
        // プロパティの制約
        // --------------------------------------------------

        [Test]
        public void Properties_ClampNegativeValues()
        {
            var zone = NewSphere(1f);

            zone.Radius = -1f;
            Assert.AreEqual(0f, zone.Radius);

            zone.BoxSize = new Vector3(-1f, 2f, -3f);
            Assert.AreEqual(new Vector3(0f, 2f, 0f), zone.BoxSize);
        }

        [Test]
        public void Capsule_Height_NeverBelowDiameter()
        {
            var zone = NewCapsule(0.5f, 2f);

            zone.Height = 0.2f;
            Assert.AreEqual(1f, zone.Height, 1e-6f);
        }

        // --------------------------------------------------
        // 境界値：OverlapsSphere で「ちょうど接する」
        //
        // 浮動小数点の誤差で結果が揺れないよう、
        // ・値は2進で正確に表せる数（0.25 / 0.5 / 0.75 / 1.25 など）
        // ・ゾーンは回転・スケールなしのルート
        // ・斜めは 3-4-5 の三角形（0.75, 1.0, 1.25 / 0.25, 0.5, 0.5, 0.75）
        // にしている。接するケースは true、少し外側は false を確認する。
        // --------------------------------------------------

        [Test]
        public void Capsule_OverlapsSphere_EndPoint_ExactTouch()
        {
            // 半径0.5 / 高さ2 → 芯は y=-0.5..0.5、先端は y=±1
            var zone = NewCapsule(0.5f, 2f);

            Assert.IsTrue(zone.OverlapsSphere(new Vector3(0f, 1.5f, 0f), 0.5f));
            Assert.IsFalse(zone.OverlapsSphere(new Vector3(0f, 1.51f, 0f), 0.5f));

            // 下側も対称
            Assert.IsTrue(zone.OverlapsSphere(new Vector3(0f, -1.5f, 0f), 0.5f));
            Assert.IsFalse(zone.OverlapsSphere(new Vector3(0f, -1.51f, 0f), 0.5f));
        }

        [Test]
        public void Capsule_OverlapsSphere_Side_ExactTouch()
        {
            var zone = NewCapsule(0.5f, 2f);

            Assert.IsTrue(zone.OverlapsSphere(new Vector3(1f, 0f, 0f), 0.5f));
            Assert.IsFalse(zone.OverlapsSphere(new Vector3(1.01f, 0f, 0f), 0.5f));
        }

        [Test]
        public void Capsule_OverlapsSphere_HemisphereDiagonal_ExactTouch()
        {
            var zone = NewCapsule(0.5f, 2f);

            // 半球の中心 (0, 0.5, 0) から (0.75, 1.0, 0) 離れた点 → 距離 1.25
            // 0.5（カプセル半径）+ 0.75（球半径）= 1.25 でちょうど接する
            var center = new Vector3(0.75f, 1.5f, 0f);

            Assert.IsTrue(zone.OverlapsSphere(center, 0.75f));
            Assert.IsFalse(zone.OverlapsSphere(center, 0.74f));
        }

        [Test]
        public void Capsule_OverlapsSphere_ParentUniformScale()
        {
            // 親スケール2 → ワールド半径1、先端は y=2
            var parent = NewObject("Parent");
            parent.transform.localScale = Vector3.one * 2f;
            var zone = NewCapsule(0.5f, 2f, parent.transform);

            Assert.IsTrue(zone.OverlapsSphere(new Vector3(0f, 2.4f, 0f), 0.5f));
            Assert.IsFalse(zone.OverlapsSphere(new Vector3(0f, 2.6f, 0f), 0.5f));

            Assert.IsTrue(zone.OverlapsSphere(new Vector3(1.4f, 0f, 0f), 0.5f));
            Assert.IsFalse(zone.OverlapsSphere(new Vector3(1.6f, 0f, 0f), 0.5f));
        }

        [Test]
        public void Box_OverlapsSphere_Face_ExactTouch()
        {
            var zone = NewBox(new Vector3(2f, 2f, 2f));

            Assert.IsTrue(zone.OverlapsSphere(new Vector3(1.5f, 0f, 0f), 0.5f));
            Assert.IsFalse(zone.OverlapsSphere(new Vector3(1.51f, 0f, 0f), 0.5f));
        }

        [Test]
        public void Box_OverlapsSphere_Edge_ExactTouch()
        {
            var zone = NewBox(new Vector3(2f, 2f, 2f));

            // 辺（x=1, y=1, z は範囲内）から (0.75, 1.0) 離れた点 → 距離 1.25
            var center = new Vector3(1.75f, 2.0f, 0f);

            Assert.IsTrue(zone.OverlapsSphere(center, 1.25f));
            Assert.IsFalse(zone.OverlapsSphere(center, 1.24f));
        }

        [Test]
        public void Box_OverlapsSphere_Corner_ExactTouch()
        {
            var zone = NewBox(new Vector3(2f, 2f, 2f));

            // 角 (1,1,1) から (0.25, 0.5, 0.5) 離れた点 → 距離 0.75
            var center = new Vector3(1.25f, 1.5f, 1.5f);

            Assert.IsTrue(zone.OverlapsSphere(center, 0.75f));
            Assert.IsFalse(zone.OverlapsSphere(center, 0.74f));
        }

        // --------------------------------------------------
        // DetectionZoneGroup
        // --------------------------------------------------

        private DetectionZoneGroup NewGroup()
        {
            return NewObject("Group").AddComponent<DetectionZoneGroup>();
        }

        [Test]
        public void Group_Empty_ReturnsFalse()
        {
            var group = NewGroup();

            Assert.IsFalse(group.ContainsPoint(Vector3.zero));
            Assert.IsFalse(group.OverlapsSphere(Vector3.zero, 1f));
        }

        [Test]
        public void Group_AnyChildZone_Matches()
        {
            var group = NewGroup();
            var a = NewSphere(0.5f, group.transform);
            var b = NewSphere(0.5f, group.transform);
            a.transform.localPosition = new Vector3(-5f, 0f, 0f);
            b.transform.localPosition = new Vector3(5f, 0f, 0f);

            Assert.IsTrue(group.ContainsPoint(new Vector3(-5f, 0f, 0f)));
            Assert.IsTrue(group.ContainsPoint(new Vector3(5f, 0f, 0f)));
            Assert.IsFalse(group.ContainsPoint(Vector3.zero));
        }

        [Test]
        public void Group_OverlapsSphere_AnyZone()
        {
            var group = NewGroup();
            var capsule = NewCapsule(0.5f, 2f, group.transform);
            NewBox(new Vector3(1f, 1f, 1f), group.transform)
                .transform.localPosition = new Vector3(10f, 0f, 0f);

            Assert.IsTrue(group.OverlapsSphere(new Vector3(0f, 1.4f, 0f), 0.5f));
            Assert.IsTrue(group.OverlapsSphere(new Vector3(10.7f, 0f, 0f), 0.3f));
            Assert.IsFalse(group.OverlapsSphere(new Vector3(0f, 5f, 0f), 0.5f));
        }

        [Test]
        public void Group_InactiveZone_Ignored()
        {
            var group = NewGroup();
            var zone = NewSphere(1f, group.transform);

            Assert.IsTrue(group.ContainsPoint(Vector3.zero));

            zone.gameObject.SetActive(false);
            Assert.IsFalse(group.ContainsPoint(Vector3.zero));
        }

        [Test]
        public void Group_ManualZone_OutsideHierarchy_IsKept()
        {
            var group = NewGroup();
            var outside = NewSphere(1f);
            outside.transform.position = new Vector3(20f, 0f, 0f);

            group.AddZone(outside);
            group.RefreshZones(); // 再収集しても手動分は消えない

            Assert.IsTrue(group.ContainsPoint(new Vector3(20f, 0f, 0f)));
        }

        [Test]
        public void Group_RemoveZone_ManualZone()
        {
            var group = NewGroup();
            var outside = NewSphere(1f);
            outside.transform.position = new Vector3(20f, 0f, 0f);

            group.AddZone(outside);
            group.RemoveZone(outside);
            group.RefreshZones();

            Assert.IsFalse(group.ContainsPoint(new Vector3(20f, 0f, 0f)));
        }

        [Test]
        public void Group_ZoneOnSelf_IsIncluded()
        {
            var group = NewGroup();
            var self = group.gameObject.AddComponent<DetectionZone>();
            self.ShapeType = ZoneShapeType.Sphere;
            self.Radius = 1f;
            group.RefreshZones();

            Assert.IsTrue(group.ContainsPoint(Vector3.zero));
        }

        [Test]
        public void Group_DuplicateAdd_DoesNotDuplicate()
        {
            var group = NewGroup();
            var zone = NewSphere(1f, group.transform);

            group.AddZone(zone);
            group.AddZone(zone);

            Assert.AreEqual(1, group.Zones.Count);
        }

        // --------------------------------------------------
        // WorldBounds
        // --------------------------------------------------

        private static void AssertVector(Vector3 expected, Vector3 actual, string label)
        {
            Assert.AreEqual(expected.x, actual.x, 1e-4f, $"{label}.x");
            Assert.AreEqual(expected.y, actual.y, 1e-4f, $"{label}.y");
            Assert.AreEqual(expected.z, actual.z, 1e-4f, $"{label}.z");
        }

        [Test]
        public void Sphere_WorldBounds_FollowsPositionAndScale()
        {
            var zone = NewSphere(0.5f);
            zone.transform.position = new Vector3(1f, 2f, 3f);
            zone.transform.localScale = Vector3.one * 2f;

            Bounds bounds = zone.WorldBounds;
            AssertVector(new Vector3(1f, 2f, 3f), bounds.center, "center");
            AssertVector(Vector3.one * 2f, bounds.size, "size");
        }

        [Test]
        public void Capsule_WorldBounds_FollowsRotation()
        {
            var zone = NewCapsule(0.1f, 0.6f);
            zone.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

            AssertVector(new Vector3(0.6f, 0.2f, 0.2f), zone.WorldBounds.size, "size");
        }

        [Test]
        public void Box_WorldBounds_EnclosesTheRotatedBox()
        {
            var zone = NewBox(new Vector3(1f, 2f, 1f));
            zone.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

            // The 1 x 1 footprint turned by 45 degrees spans its diagonal.
            float diagonal = Mathf.Sqrt(2f);
            AssertVector(new Vector3(diagonal, 2f, diagonal), zone.WorldBounds.size, "size");
        }

        [Test]
        public void Group_WorldBounds_EnclosesActiveZones()
        {
            var group = NewGroup();
            var a = NewSphere(0.5f, group.transform);
            var b = NewSphere(0.5f, group.transform);
            var inactive = NewSphere(0.5f, group.transform);
            b.transform.localPosition = new Vector3(3f, 0f, 0f);
            inactive.transform.localPosition = new Vector3(-10f, 0f, 0f);
            inactive.gameObject.SetActive(false);
            group.RefreshZones();

            Bounds bounds = group.WorldBounds;
            AssertVector(new Vector3(-0.5f, -0.5f, -0.5f), bounds.min, "min");
            AssertVector(new Vector3(3.5f, 0.5f, 0.5f), bounds.max, "max");
        }

        [Test]
        public void Group_WorldBounds_Empty_IsAPointAtTheGroup()
        {
            var group = NewGroup();
            group.transform.position = new Vector3(1f, 2f, 3f);

            Bounds bounds = group.WorldBounds;
            AssertVector(new Vector3(1f, 2f, 3f), bounds.center, "center");
            AssertVector(Vector3.zero, bounds.size, "size");
        }
    }
}