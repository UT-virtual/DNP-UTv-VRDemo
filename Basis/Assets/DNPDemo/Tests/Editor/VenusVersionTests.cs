using System.Collections.Generic;
using DNPDemo.Editor;
using DNPDemo.Scenario;
using DNPDemo.Scenario.Cues;
using DNPDemo.Venus;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Tests
{
    /// <summary>The statue versions: the catalog, switching a <see cref="VenusStatue"/>, and the history worked out from the steps.</summary>
    public class VenusVersionTests
    {
        // Unlikely to clash with a binding of whatever scene the test runner has open.
        const string StatueKey = "VenusVersionTests_Statue";

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

        VenusVersion CreateVersion(string id, bool withModel = true)
        {
            GameObject model = null;
            if (withModel)
            {
                model = Track(new GameObject(id + "_Model"));
            }
            var version = Track(ScriptableObject.CreateInstance<VenusVersion>());
            version.name = id;
            version.Initialize(id, "表示名 " + id, model);
            return version;
        }

        VenusStatue CreateStatue()
        {
            GameObject go = Track(new GameObject(StatueKey));
            TestBinding.Bind(go, StatueKey);
            return go.AddComponent<VenusStatue>();
        }

        [Test]
        public void CatalogAssetIsValidAndFollowsThePlan()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VenusVersionCatalog>(VenusAssetBuilder.CatalogPath);
            Assert.IsNotNull(catalog, $"{VenusAssetBuilder.CatalogPath} (create it from DNP Demo/Create/Venus Versions)");
            List<string> problems = catalog.Validate();
            Assert.IsEmpty(problems, string.Join("\n", problems));

            var ids = new List<string>();
            foreach (VenusVersion version in catalog.Versions)
            {
                ids.Add(version.Id);
            }
            CollectionAssert.AreEqual(new[]
            {
                VenusAssetBuilder.ArmlessId, VenusAssetBuilder.Ver10Id, VenusAssetBuilder.Ver11Id, VenusAssetBuilder.Ver12Id,
                VenusAssetBuilder.Ver20Id, VenusAssetBuilder.Ver30Id, VenusAssetBuilder.Ver31Id,
            }, ids, "docs/PLAN.md §2.5: armless → Ver 1.0 → 1.1 → 1.2 → 2.0 → 3.0 → 3.1.");

            // docs/PLAN.md §2.5: the branches of the history panel. Id → (parent id, starts a branch).
            var branches = new Dictionary<string, (string Parent, bool StartsBranch)>
            {
                [VenusAssetBuilder.ArmlessId] = (null, false),
                [VenusAssetBuilder.Ver10Id] = (VenusAssetBuilder.ArmlessId, false),
                [VenusAssetBuilder.Ver11Id] = (VenusAssetBuilder.Ver10Id, true),
                [VenusAssetBuilder.Ver12Id] = (VenusAssetBuilder.Ver11Id, false),
                [VenusAssetBuilder.Ver20Id] = (VenusAssetBuilder.Ver11Id, true),
                [VenusAssetBuilder.Ver30Id] = (VenusAssetBuilder.Ver20Id, false),
                [VenusAssetBuilder.Ver31Id] = (VenusAssetBuilder.Ver30Id, false),
            };
            foreach (VenusVersion version in catalog.Versions)
            {
                (string parent, bool startsBranch) = branches[version.Id];
                Assert.AreEqual(parent, version.Parent != null ? version.Parent.Id : null, $"Parent of {version}.");
                Assert.AreEqual(startsBranch, version.StartsBranch, $"Whether {version} starts a branch.");
            }
        }

        [Test]
        public void CatalogAssetVersionsHaveThumbnails()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VenusVersionCatalog>(VenusAssetBuilder.CatalogPath);
            Assert.IsNotNull(catalog);
            foreach (VenusVersion version in catalog.Versions)
            {
                Assert.IsNotNull(version.Thumbnail, $"{version} has no thumbnail (create it from DNP Demo/Create/Venus Thumbnails).");
            }
        }

        [Test]
        public void CatalogValidateReportsParentsThatDoNotComeFirst()
        {
            VenusVersion root = CreateVersion("root");
            VenusVersion later = CreateVersion("later");
            VenusVersion outside = CreateVersion("outside");
            var self = CreateVersion("self");
            self.Initialize("self", "表示名 self", self.Model, self);
            var catalog = Track(ScriptableObject.CreateInstance<VenusVersionCatalog>());
            catalog.SetVersions(new[]
            {
                root,
                CreateWithParent("child", root),
                CreateWithParent("before-later", later),
                later,
                CreateWithParent("from-outside", outside),
                self,
            });

            List<string> problems = catalog.Validate();
            // parent after the version, parent outside the catalog, version as its own parent
            Assert.AreEqual(3, problems.Count, string.Join("\n", problems));
        }

        VenusVersion CreateWithParent(string id, VenusVersion parent)
        {
            VenusVersion version = CreateVersion(id);
            version.Initialize(id, "表示名 " + id, version.Model, parent);
            return version;
        }

        [Test]
        public void CatalogValidateReportsBrokenVersions()
        {
            VenusVersion ok = CreateVersion("ok");
            VenusVersion duplicateId = CreateVersion("ok");
            VenusVersion noModel = CreateVersion("no-model", withModel: false);
            VenusVersion noId = CreateVersion("");
            var catalog = Track(ScriptableObject.CreateInstance<VenusVersionCatalog>());
            catalog.SetVersions(new[] { ok, duplicateId, noModel, noId, null, ok });

            List<string> problems = catalog.Validate();
            // duplicate id, missing model, empty id, unset entry, same asset twice
            Assert.AreEqual(5, problems.Count, string.Join("\n", problems));
            Assert.IsNotEmpty(Track(ScriptableObject.CreateInstance<VenusVersionCatalog>()).Validate(), "An empty catalog is a problem.");
        }

        [Test]
        public void ShowActivatesOnlyTheModelOfTheVersion()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var changes = new List<VenusVersionChange>();
            statue.VersionChanged += changes.Add;

            statue.Show(first, animate: true);
            Assert.AreSame(first, statue.Current);
            Assert.IsTrue(statue.GetModel(first).activeSelf);
            Assert.AreSame(statue.transform, statue.GetModel(first).transform.parent);

            statue.Show(second, animate: false);
            Assert.IsFalse(statue.GetModel(first).activeSelf);
            Assert.IsTrue(statue.GetModel(second).activeSelf);

            statue.Show(second, animate: true);
            Assert.AreEqual(2, changes.Count, "Showing the current version again is not a change.");
            Assert.AreSame(first, changes[1].Previous);
            Assert.AreSame(second, changes[1].Current);
            Assert.IsTrue(changes[0].Animate);
            Assert.IsFalse(changes[1].Animate);

            statue.Show(null, animate: false);
            Assert.IsNull(statue.Current);
            Assert.IsFalse(statue.GetModel(first).activeSelf);
            Assert.IsFalse(statue.GetModel(second).activeSelf);
        }

        [Test]
        public void StageModelIsSpecificToTheMainStatueAndFallsBackToTheCompleteModel()
        {
            VenusVersion withProp = CreateVersion("with-prop");
            VenusVersion regular = CreateVersion("regular");
            GameObject stagePrefab = Track(new GameObject("DetachedPropModel"));
            var versionSettings = new SerializedObject(withProp);
            versionSettings.FindProperty("_stageModel").objectReferenceValue = stagePrefab;
            versionSettings.ApplyModifiedPropertiesWithoutUndo();

            VenusStatue main = CreateStatue();
            var mainSettings = new SerializedObject(main);
            mainSettings.FindProperty("_useStageModels").boolValue = true;
            mainSettings.ApplyModifiedPropertiesWithoutUndo();
            VenusStatue comparison = Track(new GameObject("Comparison")).AddComponent<VenusStatue>();

            main.Show(withProp, animate: false);
            comparison.Show(withProp, animate: false);
            Assert.AreEqual(stagePrefab.name, main.GetModel(withProp).name);
            Assert.AreEqual(withProp.Model.name, comparison.GetModel(withProp).name);

            main.Show(regular, animate: false);
            Assert.AreEqual(regular.Model.name, main.GetModel(regular).name);
        }

        [Test]
        public void StageResetReturnsTheStatueToItsBaselineAndCuesShowVersions()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion baseline = CreateVersion("baseline");
            VenusVersion version = CreateVersion("version");
            var so = new SerializedObject(statue);
            so.FindProperty("_baseline").objectReferenceValue = baseline;
            so.ApplyModifiedPropertiesWithoutUndo();
            var stage = Track(new GameObject("Stage")).AddComponent<ScenarioStage>();
            Assume.That(stage.gameObject.scene.isLoaded, "The stage collects bindings only from a loaded scene.");
            var scenario = Track(ScriptableObject.CreateInstance<ScenarioData>());
            var context = new ScenarioCueContext(stage, scenario);

            new StatueVersionCue(StatueKey, version).ApplyInstant(context);
            Assert.AreSame(version, statue.Current);

            stage.ResetToBaseline();
            Assert.AreSame(baseline, statue.Current, "The stage returns the statue to its baseline version.");
            Assert.IsFalse(statue.GetModel(version).activeSelf);
        }

        [Test]
        public void HistoryListsVersionsOfEarlierStepsOnceInOrder()
        {
            VenusVersion a = CreateVersion("a");
            VenusVersion b = CreateVersion("b");
            VenusVersion c = CreateVersion("c");
            var steps = new List<ScenarioStep>
            {
                Step(new StatueVersionCue(StatueKey, a)),
                Step(new StatueVersionCue("Other", c), new StatueVersionCue(StatueKey, null)),
                Step(new StatueVersionCue(StatueKey, b)),
                Step(new StatueVersionCue(StatueKey, a)),
                Step(new StatueVersionCue(StatueKey, c)),
            };

            CollectionAssert.IsEmpty(VenusVersionHistory.Collect(steps, 0, StatueKey));
            CollectionAssert.AreEqual(new[] { a }, VenusVersionHistory.Collect(steps, 2, StatueKey), "Other statues and empty versions do not count.");
            CollectionAssert.AreEqual(new[] { a, b }, VenusVersionHistory.Collect(steps, 3, StatueKey), "The given step's own cues are not counted.");
            CollectionAssert.AreEqual(new[] { a, b }, VenusVersionHistory.Collect(steps, 4, StatueKey), "Each version is listed once.");
            CollectionAssert.AreEqual(new[] { a, b, c }, VenusVersionHistory.Collect(steps, 99, StatueKey));
        }

        [Test]
        public void DefaultScenarioReachesEveryVersionBeforeTheComparison()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VenusVersionCatalog>(VenusAssetBuilder.CatalogPath);
            Assert.IsNotNull(catalog);
            List<ScenarioStep> steps = ScenarioDataBuilder.CreateDefaultSteps();
            int compare = steps.FindIndex(step => step.Id == "p2-compare");
            Assert.GreaterOrEqual(compare, 0);

            CollectionAssert.AreEqual(catalog.Versions, VenusVersionHistory.Collect(steps, compare, VenusStatueKeys.Main),
                "By step 19 the main statue has shown every version, in the order of the catalog.");
            CollectionAssert.IsEmpty(VenusVersionHistory.Collect(steps, compare + 1, VenusStatueKeys.Comparison),
                "The comparison table stays empty until Ver 1.0 has been chosen.");
            CollectionAssert.AreEqual(new[] { catalog.Find(VenusAssetBuilder.Ver10Id) },
                VenusVersionHistory.Collect(steps, compare + 2, VenusStatueKeys.Comparison));
        }

        [Test]
        public void ValidateReportsVersionCueWithoutStatueKey()
        {
            var steps = new List<ScenarioStep> { Step(new StatueVersionCue(" ", null)) };
            List<string> problems = ScenarioData.Validate(steps);
            Assert.AreEqual(1, problems.Count, string.Join("\n", problems));
        }

        static int _stepCount;

        static ScenarioStep Step(params ScenarioCue[] cues) => new()
        {
            Id = "step" + _stepCount++,
            Completion = StepCompletion.CuesFinished,
            Cues = new List<ScenarioCue>(cues),
        };
    }
}
