using System;
using System.Collections.Generic;
using DNPDemo.Editor;
using DNPDemo.Interaction;
using DNPDemo.Interaction.History;
using DNPDemo.Scenario;
using DNPDemo.Venus;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNPDemo.Tests
{
    /// <summary>The history panel's parts that run without a scene: stepping through versions, the part, the payload and the preview fit.</summary>
    public class HistoryTests
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

        VenusVersion CreateVersion(string id, VenusVersion parent = null, bool startsBranch = false)
        {
            var version = ScriptableObject.CreateInstance<VenusVersion>();
            _created.Add(version);
            version.name = id;
            version.Initialize(id, "表示名 " + id, null, parent, startsBranch);
            return version;
        }

        static void AssertCell(HistoryTree tree, VenusVersion version, int column, int row)
        {
            Assert.IsTrue(tree.TryGetNode(version, out HistoryTree.Node node), $"{version.name} is in the tree.");
            Assert.AreEqual(column, node.Column, $"Column of {version.name}.");
            Assert.AreEqual(row, node.Row, $"Row of {version.name}.");
        }

        [Test]
        public void TreeStartsARowForEachBranch()
        {
            // The scenario's history: armless → 1.0, a branch 1.1 → 1.2 off 1.0, and a branch 2.0 → 3.0 → 3.1 off 1.1.
            VenusVersion armless = CreateVersion("armless");
            VenusVersion v10 = CreateVersion("v1.0", armless);
            VenusVersion v11 = CreateVersion("v1.1", v10, startsBranch: true);
            VenusVersion v12 = CreateVersion("v1.2", v11);
            VenusVersion v20 = CreateVersion("v2.0", v11, startsBranch: true);
            VenusVersion v30 = CreateVersion("v3.0", v20);
            VenusVersion v31 = CreateVersion("v3.1", v30);
            var tree = new HistoryTree();
            tree.Build(new[] { armless, v10, v11, v12, v20, v30, v31 });

            AssertCell(tree, armless, 0, 0);
            AssertCell(tree, v10, 1, 0);
            AssertCell(tree, v11, 2, 1);
            AssertCell(tree, v12, 3, 1);
            AssertCell(tree, v20, 3, 2);
            AssertCell(tree, v30, 4, 2);
            AssertCell(tree, v31, 5, 2);
            Assert.AreEqual(6, tree.ColumnCount);
            Assert.AreEqual(3, tree.RowCount);
            Assert.IsTrue(tree.TryGetNode(v20, out HistoryTree.Node node));
            Assert.AreSame(v11, node.Parent);

            tree.Build(new[] { armless, v10, v11 });
            Assert.AreEqual(3, tree.Nodes.Count, "The tree grows with the versions reached; rebuilding drops the rest.");
            Assert.IsFalse(tree.TryGetNode(v12, out _));
            Assert.AreEqual(2, tree.RowCount);
        }

        [Test]
        public void TreeContinuesARowOnceAndStartsRowsForTheOtherChildrenAndTheOrphans()
        {
            VenusVersion root = CreateVersion("root");
            VenusVersion child = CreateVersion("child", root);
            VenusVersion second = CreateVersion("second", root);
            VenusVersion third = CreateVersion("third", root);
            VenusVersion unreachedParent = CreateVersion("unreached");
            VenusVersion orphan = CreateVersion("orphan", unreachedParent);
            var tree = new HistoryTree();
            tree.Build(new[] { root, null, child, second, root, orphan, third });

            Assert.AreEqual(5, tree.Nodes.Count, "Empty entries and repeats are dropped.");
            AssertCell(tree, root, 0, 0);
            AssertCell(tree, child, 1, 0);
            AssertCell(tree, second, 1, 1);
            AssertCell(tree, orphan, 0, 2);
            AssertCell(tree, third, 1, 3);
            Assert.IsTrue(tree.TryGetNode(orphan, out HistoryTree.Node node));
            Assert.IsNull(node.Parent, "A parent that is not reached is not drawn.");

            tree.Build(null);
            Assert.AreEqual(0, tree.Nodes.Count);
            Assert.AreEqual(0, tree.ColumnCount);
            Assert.AreEqual(0, tree.RowCount);
        }

        [Test]
        public void BrowserSelectsOnlyVersionsInTheList()
        {
            VenusVersion a = CreateVersion("a");
            VenusVersion b = CreateVersion("b");
            VenusVersion c = CreateVersion("c");
            var browser = new HistoryBrowser();
            browser.SetVersions(new[] { a, b });

            Assert.IsTrue(browser.Select(a));
            Assert.AreSame(a, browser.Selected);
            Assert.IsFalse(browser.Select(c));
            Assert.IsFalse(browser.Select(null));
            Assert.AreSame(a, browser.Selected, "A version outside the list leaves the selection as it was.");
        }

        [Test]
        public void TreeViewPlacesACardPerVersionWithinItsAreaAndFramesTheNewest()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(InteractionPrefabBuilder.HistoryToolPath);
            Assert.IsNotNull(prefab);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            _created.Add(instance);
            HistoryTreeView view = instance.GetComponentInChildren<HistoryTreeView>(true);
            Assert.IsNotNull(view);

            var catalog = AssetDatabase.LoadAssetAtPath<VenusVersionCatalog>(VenusAssetBuilder.CatalogPath);
            var all = new List<VenusVersion>(catalog.Versions);
            view.Show(all);

            var area = (RectTransform)view.transform;
            foreach (VenusVersion version in all)
            {
                HistoryTreeCard card = view.FindCard(version);
                Assert.IsNotNull(card, $"{version} has a card.");
                Assert.AreEqual(version == all[all.Count - 1], card.transform.Find("CurrentFrame").gameObject.activeSelf,
                    "Only the newest version, the one on the statue, is framed.");
                AssertInside(area, card.Rect, version.ToString());
            }

            view.Show(all.GetRange(0, 2));
            Assert.IsNotNull(view.FindCard(all[1]));
            Assert.IsNull(view.FindCard(all[2]), "Cards of versions not reached yet are hidden.");
            Assert.IsTrue(view.FindCard(all[1]).transform.Find("CurrentFrame").gameObject.activeSelf);
        }

        /// <summary>Asserts that every corner of <paramref name="inner"/> lies within <paramref name="outer"/>.</summary>
        static void AssertInside(RectTransform outer, RectTransform inner, string label)
        {
            var corners = new Vector3[4];
            inner.GetWorldCorners(corners);
            Rect bounds = outer.rect;
            foreach (Vector3 corner in corners)
            {
                Vector3 local = outer.InverseTransformPoint(corner);
                Assert.That(local.x, Is.InRange(bounds.xMin - 0.01f, bounds.xMax + 0.01f), $"The card of {label} fits the tree area horizontally.");
                Assert.That(local.y, Is.InRange(bounds.yMin - 0.01f, bounds.yMax + 0.01f), $"The card of {label} fits the tree area vertically.");
            }
        }

        [Test]
        public void BrowserIsEmptyWithoutVersions()
        {
            var browser = new HistoryBrowser();
            browser.SetVersions(null);

            Assert.AreEqual(-1, browser.Index);
            Assert.IsNull(browser.Selected);
            Assert.IsFalse(browser.CanGoBack);
            Assert.IsFalse(browser.CanGoForward);
            Assert.IsFalse(browser.GoBack());
            Assert.IsFalse(browser.GoForward());
        }

        [Test]
        public void BrowserStartsAtTheNewestAndStepsWithinTheList()
        {
            VenusVersion a = CreateVersion("a");
            VenusVersion b = CreateVersion("b");
            VenusVersion c = CreateVersion("c");
            var browser = new HistoryBrowser();
            browser.SetVersions(new[] { a, b, null, c, a });

            CollectionAssert.AreEqual(new[] { a, b, c }, browser.Versions, "Empty entries and repeats are dropped.");
            Assert.AreSame(c, browser.Selected);
            Assert.IsFalse(browser.GoForward());

            Assert.IsTrue(browser.GoBack());
            Assert.IsTrue(browser.GoBack());
            Assert.AreSame(a, browser.Selected);
            Assert.IsFalse(browser.CanGoBack);
            Assert.IsFalse(browser.GoBack());

            Assert.IsTrue(browser.GoForward());
            Assert.AreSame(b, browser.Selected);
        }

        [Test]
        public void BrowserKeepsTheSelectionWhileTheListChanges()
        {
            VenusVersion a = CreateVersion("a");
            VenusVersion b = CreateVersion("b");
            VenusVersion c = CreateVersion("c");
            var browser = new HistoryBrowser();
            browser.SetVersions(new[] { a, b });
            browser.GoBack();

            browser.SetVersions(new[] { a, b, c });
            Assert.AreSame(a, browser.Selected, "A new version reached while the panel is open does not move the selection.");

            browser.SetVersions(new[] { b, c });
            Assert.AreSame(c, browser.Selected, "When the selected version leaves the list, the newest is selected.");

            browser.GoBack();
            browser.SelectLatest();
            Assert.AreSame(c, browser.Selected);
        }

        [Test]
        public void PartNeedsATargetAndAStatueKey()
        {
            var problems = new List<string>();
            new HistoryPart("x", null, " ").Validate(problems);
            Assert.AreEqual(2, problems.Count, string.Join("\n", problems));

            problems.Clear();
            var part = new HistoryPart("x", CreateVersion("v"));
            part.Validate(problems);
            CollectionAssert.IsEmpty(problems);
            Assert.AreEqual(PenMode.Panel, part.Mode);

            var requirements = new ScenarioRequirements();
            part.CollectRequirements(requirements);
            CollectionAssert.Contains(requirements.ObjectKeys, VenusStatueKeys.Comparison);
        }

        [Test]
        public void MessageCarriesTheVersionId()
        {
            byte[] payload = HistoryMessage.Serialize("v1.0");
            Assert.IsTrue(HistoryMessage.TryParse(new ArraySegment<byte>(payload), out string id));
            Assert.AreEqual("v1.0", id);

            var padded = new byte[payload.Length + 3];
            Buffer.BlockCopy(payload, 0, padded, 2, payload.Length);
            Assert.IsTrue(HistoryMessage.TryParse(new ArraySegment<byte>(padded, 2, payload.Length), out id), "Reads from the segment's offset.");
            Assert.AreEqual("v1.0", id);
        }

        [Test]
        public void MessageRejectsWhatCannotBeAVersionId()
        {
            Assert.IsNull(HistoryMessage.Serialize(null));
            Assert.IsNull(HistoryMessage.Serialize(string.Empty));
            Assert.IsNull(HistoryMessage.Serialize(new string('a', HistoryMessage.MaxIdBytes + 1)));
            Assert.IsNotNull(HistoryMessage.Serialize(new string('a', HistoryMessage.MaxIdBytes)));

            Assert.IsFalse(HistoryMessage.TryParse(default, out _));
            Assert.IsFalse(HistoryMessage.TryParse(new ArraySegment<byte>(new byte[] { 0 }), out _), "Empty id.");
            Assert.IsFalse(HistoryMessage.TryParse(new ArraySegment<byte>(new byte[] { 3, (byte)'a' }), out _), "Shorter than its length.");
            Assert.IsFalse(HistoryMessage.TryParse(new ArraySegment<byte>(new byte[] { 1, (byte)'a', (byte)'b' }), out _), "Longer than its length.");
            Assert.IsFalse(HistoryMessage.TryParse(new ArraySegment<byte>(new byte[] { 1, 0xFF }), out _), "Not UTF-8.");
        }

        [Test]
        public void PreviewMeasuresMeshesInTheGivenSpaceIncludingHiddenOnes()
        {
            var space = new GameObject("Space");
            _created.Add(space);
            space.transform.localScale = Vector3.one * 0.5f;
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(space.transform, false);
            cube.transform.localPosition = new Vector3(0f, 1f, 0f);
            cube.transform.localScale = new Vector3(1f, 2f, 1f);
            cube.SetActive(false);

            Assert.IsTrue(HistoryPreview.TryMeasure(cube, space.transform, out Bounds bounds));
            Assert.That(Vector3.Distance(new Vector3(0f, 1f, 0f), bounds.center), Is.LessThan(1e-4f));
            Assert.That(Vector3.Distance(new Vector3(1f, 2f, 1f), bounds.size), Is.LessThan(1e-4f), "Measured in the space's units, not in world units.");

            Assert.IsFalse(HistoryPreview.TryMeasure(new GameObject("Empty"), space.transform, out _));
        }

        [Test]
        public void DefaultScenarioRestoresAVersionThePanelLists()
        {
            List<ScenarioStep> steps = ScenarioDataBuilder.CreateDefaultSteps();
            int compare = steps.FindIndex(step => step.Id == "p2-compare");
            Assert.GreaterOrEqual(compare, 0);
            var part = steps[compare].Interaction[0] as HistoryPart;
            Assert.IsNotNull(part, "Step 19 calls a version back from the history panel.");
            Assert.AreSame(VenusAssetBuilder.LoadVersion(VenusAssetBuilder.Ver10Id), part.TargetVersion);
            CollectionAssert.Contains(VenusVersionHistory.Collect(steps, compare + 1, VenusStatueKeys.Main), part.TargetVersion,
                "The panel lists the versions shown up to the current step, so the target has to be among them.");
        }

        [Test]
        public void ScenarioAssetRestoresVer10AtTheComparison()
        {
            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioDataBuilder.AssetPath);
            Assert.IsNotNull(scenario);
            ScenarioStep step = null;
            foreach (ScenarioStep candidate in scenario.Steps)
            {
                if (candidate.Id == "p2-compare")
                {
                    step = candidate;
                }
            }
            Assert.IsNotNull(step);
            var part = step.Interaction.Count == 1 ? step.Interaction[0] as HistoryPart : null;
            Assert.IsNotNull(part, "The scenario asset's step 19 runs a HistoryPart.");
            Assert.AreSame(VenusAssetBuilder.LoadVersion(VenusAssetBuilder.Ver10Id), part.TargetVersion);
            Assert.AreEqual(VenusStatueKeys.Comparison, part.ComparisonStatueKey);
        }
    }
}
