using System.Collections.Generic;
using DNPDemo.Scenario;
using DNPDemo.Venus;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Tests
{
    /// <summary>The set guide curve of step 11 and the effect that runs along it as the step completes.</summary>
    public class GuideSnapTests
    {
        readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }
            _created.Clear();
        }

        T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }

        GuideSnapEffect CreateEffect(out Transform start, out Transform end, out LineRenderer glow, out Transform head)
        {
            start = Track(new GameObject("Start")).transform;
            start.position = new Vector3(-0.3f, 1.6f, 0f);
            end = Track(new GameObject("End")).transform;
            end.position = new Vector3(-0.1f, 1.35f, -0.35f);
            var root = Track(new GameObject("Guide"));
            glow = new GameObject("Glow").AddComponent<LineRenderer>();
            glow.transform.SetParent(root.transform, false);
            glow.gameObject.SetActive(false);
            head = new GameObject("Head").transform;
            head.SetParent(root.transform, false);
            head.gameObject.SetActive(false);

            var curve = root.AddComponent<GuideCurve>();
            var curveSo = new SerializedObject(curve);
            curveSo.FindProperty("_start").objectReferenceValue = start;
            curveSo.FindProperty("_end").objectReferenceValue = end;
            curveSo.FindProperty("_startHandle").vector3Value = new Vector3(-0.4f, 1.3f, 0f);
            curveSo.FindProperty("_endHandle").vector3Value = new Vector3(-0.3f, 1.2f, -0.3f);
            curveSo.ApplyModifiedPropertiesWithoutUndo();

            var effect = root.AddComponent<GuideSnapEffect>();
            var effectSo = new SerializedObject(effect);
            effectSo.FindProperty("_glow").objectReferenceValue = glow;
            effectSo.FindProperty("_head").objectReferenceValue = head;
            effectSo.FindProperty("_fadeSeconds").floatValue = 0.5f;
            effectSo.ApplyModifiedPropertiesWithoutUndo();
            return effect;
        }

        [Test]
        public void CurveRunsFromTheStartZoneToTheEndZone()
        {
            GuideSnapEffect effect = CreateEffect(out Transform start, out Transform end, out _, out _);
            var curve = effect.GetComponent<GuideCurve>();
            Assert.Less(Vector3.Distance(start.position, curve.Evaluate(0f)), 1e-5f);
            Assert.Less(Vector3.Distance(end.position, curve.Evaluate(1f)), 1e-5f);

            // Moving a zone moves the end of the curve with it.
            end.position += new Vector3(0.05f, 0f, 0f);
            Assert.Less(Vector3.Distance(end.position, curve.Evaluate(1f)), 1e-5f);
        }

        [Test]
        public void EffectGrowsAlongTheCurveThenFadesOut()
        {
            GuideSnapEffect effect = CreateEffect(out Transform start, out Transform end, out LineRenderer glow, out Transform head);

            effect.Play(0.8f);
            Assert.IsTrue(effect.IsPlaying);
            Assert.IsTrue(glow.gameObject.activeSelf);
            Assert.Less(Vector3.Distance(start.position, head.position), 1e-4f);

            effect.Tick(0.8f);
            Assert.Less(Vector3.Distance(end.position, glow.GetPosition(glow.positionCount - 1)), 1e-4f);
            Assert.Less(Vector3.Distance(start.position, glow.GetPosition(0)), 1e-4f);

            effect.Tick(0.6f);
            Assert.IsFalse(effect.IsPlaying);
            Assert.IsFalse(glow.gameObject.activeSelf);
            Assert.IsFalse(head.gameObject.activeSelf);
        }

        [Test]
        public void StageResetStopsTheEffect()
        {
            GuideSnapEffect effect = CreateEffect(out _, out _, out LineRenderer glow, out Transform head);
            effect.Play(0.8f);
            effect.Tick(0.3f);

            ((IStageResettable)effect).ResetToBaseline();

            Assert.IsFalse(effect.IsPlaying);
            Assert.IsFalse(glow.gameObject.activeSelf);
            Assert.IsFalse(head.gameObject.activeSelf);
        }
    }
}
