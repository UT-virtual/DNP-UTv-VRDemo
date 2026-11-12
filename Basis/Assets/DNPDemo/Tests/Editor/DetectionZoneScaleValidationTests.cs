using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using DNPDemo.Interaction;

namespace DNPDemo.Tests
{
    /// <summary>
    /// 非均一スケール禁止の検証。
    /// 条件：ゾーン自身と、すべての祖先の localScale が均一であること。
    /// </summary>
    public class DetectionZoneScaleValidationTests
    {
        // プロジェクト内でのパス（Basis/Assets/DNPDemo は Assets/DNPDemo）
        private const string PrefabSearchRoot = "Assets/DNPDemo";

        private readonly List<GameObject> _created = new List<GameObject>();

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

        private Transform NewTransform(
            string name,
            Vector3 localScale,
            Transform parent = null)
        {
            var go = new GameObject(name);
            _created.Add(go);
            if (parent != null)
                go.transform.SetParent(parent, false);
            go.transform.localScale = localScale;
            return go.transform;
        }

        private DetectionZone NewZone(Vector3 localScale, Transform parent = null)
        {
            Transform t = NewTransform("Zone", localScale, parent);
            return t.gameObject.AddComponent<DetectionZone>();
        }

        private static string HierarchyPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        // --------------------------------------------------
        // IsUniformScale
        // --------------------------------------------------

        [TestCase(1f, 1f, 1f, true)]
        [TestCase(0.5f, 0.5f, 0.5f, true)]
        [TestCase(0f, 0f, 0f, true)]
        [TestCase(0.55f, 1f, 0.4f, false)]
        [TestCase(1f, 1f, 1.01f, false)]
        [TestCase(-1f, 1f, 1f, true)]   // 符号だけの反転（ミラー）は形状が歪まないので許容
        public void IsUniformScale_Cases(float x, float y, float z, bool expected)
        {
            Assert.AreEqual(
                expected,
                DetectionZone.IsUniformScale(new Vector3(x, y, z)));
        }

        // --------------------------------------------------
        // IsScaleHierarchyUniform
        // --------------------------------------------------

        [Test]
        public void Hierarchy_AllUniform_ReturnsTrue()
        {
            Transform grandParent = NewTransform("GrandParent", Vector3.one * 2f);
            Transform parent = NewTransform("Parent", Vector3.one * 0.5f, grandParent);
            DetectionZone zone = NewZone(Vector3.one, parent);

            Assert.IsTrue(zone.IsScaleHierarchyUniform());
        }

        [Test]
        public void Hierarchy_ZoneItselfNonUniform_ReturnsFalse()
        {
            DetectionZone zone = NewZone(new Vector3(1f, 2f, 1f));

            Assert.IsFalse(zone.IsScaleHierarchyUniform());
        }

        [Test]
        public void Hierarchy_ParentNonUniform_ReturnsFalse()
        {
            Transform parent = NewTransform("Body", new Vector3(0.55f, 1f, 0.4f));
            DetectionZone zone = NewZone(Vector3.one, parent);

            Assert.IsFalse(zone.IsScaleHierarchyUniform());
        }

        [Test]
        public void Hierarchy_GrandParentNonUniform_ReturnsFalse()
        {
            Transform grandParent = NewTransform("GrandParent", new Vector3(1f, 2f, 1f));
            Transform parent = NewTransform("Parent", Vector3.one, grandParent);
            DetectionZone zone = NewZone(Vector3.one, parent);

            Assert.IsFalse(zone.IsScaleHierarchyUniform());
        }

        [Test]
        public void Hierarchy_SiblingOfNonUniformBody_ReturnsTrue()
        {
            // 採用している配置：非均一な Body の「兄弟」に置く
            Transform parent = NewTransform("Venus", Vector3.one);
            NewTransform("Body", new Vector3(0.55f, 1f, 0.4f), parent);
            DetectionZone zone = NewZone(Vector3.one, parent);

            Assert.IsTrue(zone.IsScaleHierarchyUniform());
        }

        // --------------------------------------------------
        // プレハブ内の DetectionZone を走査して検証
        // --------------------------------------------------

        [Test]
        public void AllPrefabs_DetectionZones_HaveUniformScaleHierarchy()
        {
            var violations = new List<string>();

            string[] guids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { PrefabSearchRoot });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (root == null)
                    continue;

                foreach (DetectionZone zone in
                    root.GetComponentsInChildren<DetectionZone>(true))
                {
                    if (!zone.IsScaleHierarchyUniform())
                    {
                        violations.Add(
                            $"{path} : {HierarchyPath(zone.transform)}");
                    }
                }
            }

            Assert.IsEmpty(
                violations,
                "自身または祖先の localScale が非均一な DetectionZone があります:\n"
                + string.Join("\n", violations));
        }

        // --------------------------------------------------
        // カプセルの芯の線分は元の radius / height で決まる
        // --------------------------------------------------

        [Test]
        public void Capsule_OverlapsSphere_LargeProbe_SegmentNotShrunk()
        {
            // 半径0.5 / 高さ2 → 芯の線分は y=-0.5..0.5（先端 y=±1）
            var zone = NewZone(Vector3.one);
            zone.Radius = 0.5f;
            zone.Height = 2f;
            zone.ShapeType = ZoneShapeType.Capsule;

            // 球の半径 1.0 → 線分端 (0,0.5,0) から 1.5 以内なら重なる
            // 線分が縮む実装だと halfSegment が 0 になり、距離 1.9 > 1.5 で外れてしまう
            Assert.IsTrue(zone.OverlapsSphere(new Vector3(0f, 1.9f, 0f), 1f));
            Assert.IsFalse(zone.OverlapsSphere(new Vector3(0f, 2.1f, 0f), 1f));
        }
    }
}