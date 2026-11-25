using System.Collections.Generic;
using DNPDemo.Interaction.Manipulate;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Tests
{
    /// <summary>
    /// <see cref="ManipulateGhostView"/> draws the object as it looks at each moment, at the pose it should reach.
    /// </summary>
    public class ManipulateGhostViewTests
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

        ManipulateGhostView CreateView()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _created.Add(material);
            var go = new GameObject("Ghost");
            _created.Add(go);
            var view = go.AddComponent<ManipulateGhostView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("_material").objectReferenceValue = material;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>An object with one cube under a child "Visual", as the hand mirror has.</summary>
        Transform CreateTarget(out Transform visual)
        {
            var target = new GameObject("Target");
            _created.Add(target);
            target.transform.SetPositionAndRotation(new Vector3(0.3f, 1.2f, -1.4f), Quaternion.Euler(0f, 30f, 0f));
            visual = new GameObject("Visual").transform;
            visual.SetParent(target.transform, false);
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(visual, false);
            cube.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            return target.transform;
        }

        [Test]
        public void GhostShowsTheObjectAtTheGoal()
        {
            ManipulateGhostView view = CreateView();
            Transform target = CreateTarget(out _);
            var goal = new Pose(new Vector3(-0.3f, 1.6f, -0.5f), Quaternion.Euler(10f, -20f, 0f));

            view.Show(target, goal);
            var matrices = new List<Matrix4x4>();
            view.CollectMatrices(matrices);

            Assert.IsTrue(view.IsVisible);
            Assert.AreEqual(1, matrices.Count);
            Vector3 expected = goal.position + goal.rotation * new Vector3(0f, 0.1f, 0f);
            Assert.Less(Vector3.Distance(expected, matrices[0].GetPosition()), 1e-5f);
        }

        [Test]
        public void GhostFollowsAVisualThatGrowsInAfterItWasShown()
        {
            ManipulateGhostView view = CreateView();
            Transform target = CreateTarget(out Transform visual);
            var goal = new Pose(new Vector3(-0.3f, 1.6f, -0.5f), Quaternion.identity);

            // Step 14: the mirror's visual starts from nothing as the part (and its ghost) begins.
            visual.localScale = Vector3.zero;
            view.Show(target, goal);
            visual.localScale = Vector3.one;
            var matrices = new List<Matrix4x4>();
            view.CollectMatrices(matrices);

            Assert.AreEqual(1, matrices.Count);
            Assert.Less(Vector3.Distance(Vector3.one, matrices[0].lossyScale), 1e-5f, "The ghost grows with the object instead of keeping the size it had when shown.");
        }

        [Test]
        public void GhostStaysAtTheGoalWhileTheObjectMoves()
        {
            ManipulateGhostView view = CreateView();
            Transform target = CreateTarget(out _);
            var goal = new Pose(new Vector3(-0.3f, 1.6f, -0.5f), Quaternion.identity);

            view.Show(target, goal);
            target.SetPositionAndRotation(new Vector3(0f, 1.4f, -1f), Quaternion.Euler(0f, 90f, 0f));
            var matrices = new List<Matrix4x4>();
            view.CollectMatrices(matrices);

            Assert.Less(Vector3.Distance(goal.position + new Vector3(0f, 0.1f, 0f), matrices[0].GetPosition()), 1e-5f);
        }
    }
}
