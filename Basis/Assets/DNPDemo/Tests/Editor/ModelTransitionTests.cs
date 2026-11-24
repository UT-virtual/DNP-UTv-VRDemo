using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using DNPDemo.Venus;
using DNPDemo.Scenario;
using DNPDemo.Scenario.Cues;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class ModelTransitionTests
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

        VenusVersion CreateVersion(string id)
        {
            GameObject modelPrefab = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            var renderer = modelPrefab.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Track(new Material(FindTestShader()));
            modelPrefab.name = id + "_Model";

            var version = Track(ScriptableObject.CreateInstance<VenusVersion>());
            version.name = id;
            version.Initialize(id, id, modelPrefab);
            return version;
        }

        VenusStatue CreateStatue()
        {
            var go = Track(new GameObject("ModelTransitionTests_Statue"));
            return go.AddComponent<VenusStatue>();
        }

        ModelTransitionProfile CreateProfile(float duration)
        {
            var profile = Track(ScriptableObject.CreateInstance<ModelTransitionProfile>());
            Shader shader = FindTestShader();
            var material = Track(new Material(shader));
            profile.Initialize(material, null, duration);
            return profile;
        }

        static Shader FindTestShader()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/DNPDemo/Art/Venus/Materials/ModelTransition.shader");
            if (shader != null)
            {
                return shader;
            }
            Debug.LogError("ModelTransitionTest: Assets/DNPDemo/Art/Venus/Materials/ModelTransition.shader was not found.");
            throw new System.InvalidOperationException("ModelTransitionTest: DNPDemo/ModelTransition shader is missing.");
        }

        void SetProfile(ModelTransition transition, ModelTransitionProfile profile)
        {
            VenusStatue statue = transition.GetComponent<VenusStatue>();
            transition.Initialize(statue, profile);
            var serialized = new SerializedObject(transition);
            serialized.FindProperty("_profile").objectReferenceValue = profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void ApplyInstantRestoresCurrentAndDeactivatesPrevious()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));

            statue.Show(first, false);
            statue.Show(second, false);

            Assert.AreSame(second, statue.Current);
            Assert.IsTrue(statue.GetModel(second).activeSelf);
            Assert.IsFalse(statue.GetModel(first).activeSelf);
            Assert.IsFalse(transition.IsAnimating);
        }

        [Test]
        public void TickCompletesAndRestoresSharedMaterials()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));

            statue.Show(first, false);
            statue.Show(second, false);
            statue.Show(first, false);
            Renderer previousRenderer = statue.GetModel(first).GetComponentInChildren<Renderer>(true);
            Material[] previousOriginal = previousRenderer.sharedMaterials;
            Renderer currentRenderer = statue.GetModel(second).GetComponentInChildren<Renderer>(true);
            Material[] currentOriginal = currentRenderer.sharedMaterials;

            statue.Show(second, true);

            Assert.IsTrue(transition.IsAnimating);
            Assert.IsTrue(statue.GetModel(first).activeSelf);

            transition.Tick(0.5f);
            Assert.IsTrue(transition.IsAnimating);
            transition.Tick(0.5f);

            Assert.IsFalse(transition.IsAnimating);
            Assert.IsFalse(statue.GetModel(first).activeSelf);
            Assert.IsTrue(statue.GetModel(second).activeSelf);
            CollectionAssert.AreEqual(previousOriginal, previousRenderer.sharedMaterials);
            CollectionAssert.AreEqual(currentOriginal, currentRenderer.sharedMaterials);
        }

        [Test]
        public void ZeroOrMissingProfileCompletesImmediately()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var transition = statue.gameObject.AddComponent<ModelTransition>();

            statue.Show(first, false);
            statue.Show(second, true);
            Assert.IsFalse(transition.IsAnimating);

            SetProfile(transition, CreateProfile(0f));
            statue.Show(first, true);
            Assert.IsFalse(transition.IsAnimating);
            Assert.IsTrue(statue.GetModel(first).activeSelf);
            Assert.IsFalse(statue.GetModel(second).activeSelf);
        }

        [Test]
        public void SetBoundsIsConsumedByTheNextTransition()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));

            statue.Show(first, false);
            transition.SetBounds(new Bounds(Vector3.zero, Vector3.one));
            statue.Show(second, true);
            Assert.IsTrue(transition.IsAnimating);

            transition.Tick(1f);
            Assert.IsFalse(transition.IsAnimating);

            statue.Show(first, true);
            Assert.IsTrue(transition.IsAnimating);
            transition.CompleteImmediately();
        }

        [Test]
        public void BoundsSetDuringAnimationIsUsedByTheInterruptedTransition()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            VenusVersion third = CreateVersion("third");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));

            statue.Show(first, false);
            statue.Show(second, true);
            Assert.IsTrue(transition.IsAnimating);

            var firstBounds = new Bounds(new Vector3(1f, 2f, 3f), Vector3.one);
            var latestBounds = new Bounds(new Vector3(4f, 5f, 6f), Vector3.one * 2f);
            transition.SetBounds(firstBounds);
            transition.SetBounds(latestBounds);
            statue.Show(third, true);

            Assert.IsTrue(transition.IsAnimating);
            Assert.AreEqual(latestBounds, transition.ActiveBounds);
            Assert.IsTrue(statue.GetModel(third).activeSelf);
            transition.CompleteImmediately();
        }

        [Test]
        public void NewVersionWithoutPreviousIsDissolveInOnly()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));

            statue.Show(first, true);

            Assert.IsTrue(transition.IsAnimating);
            Assert.IsTrue(statue.GetModel(first).activeSelf);
            transition.Tick(1f);
            Assert.IsFalse(transition.IsAnimating);
            Assert.IsTrue(statue.GetModel(first).activeSelf);
        }

        [Test]
        public void NewVersionChangedInterruptsAnimationAndStartsNext()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            VenusVersion third = CreateVersion("third");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));

            statue.Show(first, false);
            statue.Show(second, true);
            Assert.IsTrue(transition.IsAnimating);

            statue.Show(third, true);

            Assert.IsTrue(transition.IsAnimating);
            Assert.IsTrue(statue.GetModel(third).activeSelf);
            Assert.IsFalse(statue.GetModel(first).activeSelf);
            Assert.IsTrue(statue.GetModel(second).activeSelf);
            transition.CompleteImmediately();
        }

        [Test]
        public void NullBoundsUsesFullModelBounds()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));

            statue.Show(first, false);
            statue.Show(second, true);

            Assert.IsTrue(transition.IsAnimating);
            Assert.Greater(transition.ActiveBounds.size.x, 0f);
            Assert.Greater(transition.ActiveBounds.size.y, 0f);
            Assert.Greater(transition.ActiveBounds.size.z, 0f);
            transition.CompleteImmediately();
        }

        [Test]
        public void AnimateFalseDuringAnimationRestoresAndKeepsOnlyCurrent()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));

            statue.Show(first, false);
            statue.Show(second, false);
            statue.Show(first, false);
            Renderer firstRenderer = statue.GetModel(first).GetComponentInChildren<Renderer>(true);
            Renderer secondRenderer = statue.GetModel(second).GetComponentInChildren<Renderer>(true);
            Material[] firstOriginal = (Material[])firstRenderer.sharedMaterials.Clone();
            Material[] secondOriginal = (Material[])secondRenderer.sharedMaterials.Clone();

            statue.Show(second, true);
            Assert.IsTrue(transition.IsAnimating);

            statue.Show(first, false);

            Assert.IsFalse(transition.IsAnimating);
            Assert.IsTrue(statue.GetModel(first).activeSelf);
            Assert.IsFalse(statue.GetModel(second).activeSelf);
            CollectionAssert.AreEqual(firstOriginal, firstRenderer.sharedMaterials);
            CollectionAssert.AreEqual(secondOriginal, secondRenderer.sharedMaterials);

            statue.Show(statue.Baseline, false);
        }

        [Test]
        public void OnDisableCompletesBeforeUnsubscribeAndLaterChangeDoesNotThrow()
        {
            // EditMode does not invoke Unity lifecycle callbacks, so call OnDisable explicitly.
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));

            statue.Show(first, false);
            statue.Show(second, true);
            Assert.IsTrue(transition.IsAnimating);

            statue.gameObject.SetActive(false);
            typeof(ModelTransition).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(transition, null);
            Assert.IsFalse(transition.IsAnimating);
            Assert.IsFalse(statue.GetModel(first).activeSelf);
            Assert.IsTrue(statue.GetModel(second).activeSelf);

            Assert.DoesNotThrow(() => statue.Show(first, false));
            statue.gameObject.SetActive(true);
            typeof(ModelTransition).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(transition, null);
            Assert.DoesNotThrow(() => statue.Show(second, true));
            Assert.IsTrue(transition.IsAnimating);
        }

        public static IEnumerable<TestCaseData> CatalogPairs
        {
            get
            {
                var catalog = AssetDatabase.LoadAssetAtPath<VenusVersionCatalog>(
                    "Assets/DNPDemo/Data/Venus/VenusVersions.asset");
                if (catalog == null)
                {
                    yield break;
                }
                foreach (VenusVersion previous in catalog.Versions)
                {
                    foreach (VenusVersion current in catalog.Versions)
                    {
                        yield return new TestCaseData(previous.Id, current.Id);
                    }
                }
            }
        }

        [TestCaseSource(nameof(CatalogPairs))]
        public void CatalogVersionPairStartsPreviousAndFinishesWithCurrent(string previousId, string currentId)
        {
            VenusStatue statue = CreateStatue();
            VenusVersion previous = FindCatalogVersion(previousId);
            VenusVersion current = FindCatalogVersion(currentId);
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));

            statue.Show(previous, false);
            statue.Show(current, true);

            if (previous == current)
            {
                Assert.IsFalse(transition.IsAnimating);
                Assert.IsTrue(statue.GetModel(current).activeSelf);
                return;
            }

            Assert.IsTrue(statue.GetModel(previous).activeSelf);
            Assert.IsTrue(statue.GetModel(current).activeSelf);

            transition.Tick(1f);

            Assert.IsFalse(transition.IsAnimating);
            Assert.IsFalse(statue.GetModel(previous).activeSelf);
            Assert.IsTrue(statue.GetModel(current).activeSelf);
        }

        VenusVersion FindCatalogVersion(string id)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VenusVersionCatalog>(
                "Assets/DNPDemo/Data/Venus/VenusVersions.asset");
            return catalog != null ? catalog.Find(id) : null;
        }

        [Test]
        public void DissolveValuesMoveMonotonicallyFromStartToEnd()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));

            statue.Show(first, false);
            statue.Show(second, true);

            Assert.AreEqual(1f, transition.PreviousDissolveValue, 0.001f);
            Assert.AreEqual(0f, transition.CurrentDissolveValue, 0.001f);

            transition.Tick(0.25f);
            Assert.AreEqual(0.75f, transition.PreviousDissolveValue, 0.001f);
            Assert.AreEqual(0.25f, transition.CurrentDissolveValue, 0.001f);

            transition.Tick(0.75f);
            Assert.AreEqual(0f, transition.PreviousDissolveValue, 0.001f);
            Assert.AreEqual(1f, transition.CurrentDissolveValue, 0.001f);
        }

        [Test]
        public void ParticleSpawnUsesBoundsCenterAndIsDestroyed()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            var profile = CreateProfile(1f);
            GameObject particlePrefab = Track(new GameObject("TransitionParticle"));
            particlePrefab.AddComponent<ParticleSystem>();
            profile.Initialize(profile.TransitionMaterial, particlePrefab, 1f);
            SetProfile(transition, profile);

            statue.Show(first, false);
            transition.SetBounds(new Bounds(new Vector3(2f, 3f, 4f), Vector3.one));
            statue.Show(second, true);

            Assert.IsNotNull(transition.ParticleInstance);
            Assert.AreEqual(new Vector3(2f, 3f, 4f), transition.ParticleInstance.transform.position);

            transition.CompleteImmediately();
            Assert.IsNull(transition.ParticleInstance);
        }

        [Test]
        public void IndependentStatuesAnimateSeparately()
        {
            VenusStatue firstStatue = CreateStatue();
            firstStatue.name = "IndependentA";
            VenusStatue secondStatue = CreateStatue();
            secondStatue.name = "IndependentB";
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var firstTransition = firstStatue.gameObject.AddComponent<ModelTransition>();
            var secondTransition = secondStatue.gameObject.AddComponent<ModelTransition>();
            SetProfile(firstTransition, CreateProfile(1f));
            SetProfile(secondTransition, CreateProfile(1f));

            firstStatue.Show(first, false);
            secondStatue.Show(first, false);
            firstStatue.Show(second, true);

            Assert.IsTrue(firstTransition.IsAnimating);
            Assert.IsFalse(secondTransition.IsAnimating);
            Assert.IsTrue(firstStatue.GetModel(second).activeSelf);
            Assert.IsTrue(secondStatue.GetModel(first).activeSelf);
        }

        [Test]
        public void ResetToBaselineDuringAnimationCompletesAndDoesNotReactivatePrevious()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion baseline = CreateVersion("baseline");
            VenusVersion next = CreateVersion("next");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));

            var statueSo = new SerializedObject(statue);
            statueSo.FindProperty("_baseline").objectReferenceValue = baseline;
            statueSo.ApplyModifiedPropertiesWithoutUndo();

            statue.Show(baseline, false);
            statue.Show(next, true);
            Assert.IsTrue(transition.IsAnimating);

            ((IStageResettable)statue).ResetToBaseline();

            Assert.IsFalse(transition.IsAnimating);
            Assert.IsTrue(statue.GetModel(baseline).activeSelf);
            Assert.IsFalse(statue.GetModel(next).activeSelf);
        }

        [Test]
        public void ResetToSameBaselineDuringAnimationStillSendsAnimateFalse()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion baseline = CreateVersion("baseline");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));

            var statueSo = new SerializedObject(statue);
            statueSo.FindProperty("_baseline").objectReferenceValue = baseline;
            statueSo.ApplyModifiedPropertiesWithoutUndo();

            statue.Show(baseline, true);
            Assert.IsTrue(transition.IsAnimating);

            bool receivedAnimateFalse = false;
            statue.VersionChanged += change =>
            {
                if (!change.Animate)
                {
                    receivedAnimateFalse = true;
                }
            };

            ((IStageResettable)statue).ResetToBaseline();

            Assert.IsTrue(receivedAnimateFalse);
            Assert.IsFalse(transition.IsAnimating);
            Assert.IsTrue(statue.GetModel(baseline).activeSelf);
        }

        [Test]
        public void ScenarioJumpAndLateJoinUseInstantStateWithoutAnimation()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion baseline = CreateVersion("baseline");
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));

            var statueSo = new SerializedObject(statue);
            statueSo.FindProperty("_baseline").objectReferenceValue = baseline;
            statueSo.ApplyModifiedPropertiesWithoutUndo();

            var binding = statue.gameObject.AddComponent<ScenarioObjectBinding>();
            var bindingSo = new SerializedObject(binding);
            bindingSo.FindProperty("_key").stringValue = "RouteStatue";
            bindingSo.ApplyModifiedPropertiesWithoutUndo();

            var stage = Track(new GameObject("RouteStage")).AddComponent<ScenarioStage>();
            Assume.That(stage.gameObject.scene.isLoaded);
            var scenario = Track(ScriptableObject.CreateInstance<ScenarioData>());
            var context = new ScenarioCueContext(stage, scenario);

            statue.Show(baseline, false);
            statue.Show(first, true);
            Assert.IsTrue(transition.IsAnimating);

            new StatueVersionCue("RouteStatue", second).ApplyInstant(context);

            Assert.IsFalse(transition.IsAnimating);
            Assert.IsFalse(statue.GetModel(baseline).activeSelf);
            Assert.IsFalse(statue.GetModel(first).activeSelf);
            Assert.IsTrue(statue.GetModel(second).activeSelf);
        }

        [Test]
        public void StatueVersionCueCentresTheTransitionOnItsFocus()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));
            Bind(statue.gameObject, "FocusStatue");

            GameObject focus = Track(new GameObject("FocusZone"));
            focus.transform.position = new Vector3(4f, 1f, -2f);
            var zone = focus.AddComponent<DNPDemo.Interaction.DetectionZone>();
            zone.ShapeType = DNPDemo.Interaction.ZoneShapeType.Box;
            zone.BoxSize = new Vector3(0.5f, 0.6f, 0.7f);
            Bind(focus, "FocusZone");

            var stage = Track(new GameObject("FocusStage")).AddComponent<ScenarioStage>();
            Assume.That(stage.gameObject.scene.isLoaded);
            var context = new ScenarioCueContext(stage, Track(ScriptableObject.CreateInstance<ScenarioData>()));
            statue.Show(first, false);

            // Showing the version already shown does not leave the focus pending for a later change.
            new StatueVersionCue("FocusStatue", first, "FocusZone").Play(context).MoveNext();
            new StatueVersionCue("FocusStatue", second).Play(context).MoveNext();
            Assert.IsTrue(transition.IsAnimating);
            Assert.AreNotEqual(zone.WorldBounds, transition.ActiveBounds);

            transition.CompleteImmediately();
            new StatueVersionCue("FocusStatue", first, "FocusZone").Play(context).MoveNext();
            Assert.IsTrue(transition.IsAnimating);
            Assert.AreEqual(zone.WorldBounds, transition.ActiveBounds);
        }

        static void Bind(GameObject target, string key)
        {
            var binding = target.AddComponent<ScenarioObjectBinding>();
            var so = new SerializedObject(binding);
            so.FindProperty("_key").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void ParticleIsPlayingAndDestroyedOnComplete()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            var profile = CreateProfile(1f);
            GameObject particlePrefab = Track(new GameObject("TransitionParticle"));
            particlePrefab.AddComponent<ParticleSystem>();
            profile.Initialize(profile.TransitionMaterial, particlePrefab, 1f);
            SetProfile(transition, profile);

            statue.Show(first, false);
            statue.Show(second, true);

            Assert.IsNotNull(transition.ParticleInstance);
            ParticleSystem particleSystem = transition.ParticleInstance.GetComponentInChildren<ParticleSystem>(true);
            Assert.IsNotNull(particleSystem);
            Assert.IsTrue(particleSystem.isPlaying);

            transition.CompleteImmediately();
            Assert.IsNull(transition.ParticleInstance);
            Assert.IsFalse(statue.GetModel(first).activeSelf);
            Assert.IsTrue(statue.GetModel(second).activeSelf);
        }

        [Test]
        public void MissingShaderPropertiesDoNotThrow()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            var profile = CreateProfile(1f);
            var profileSo = new SerializedObject(profile);
            profileSo.FindProperty("_dissolvePropertyName").stringValue = "_PropertyThatDoesNotExist";
            profileSo.FindProperty("_scanLinePropertyName").stringValue = "_PropertyThatDoesNotExist";
            profileSo.FindProperty("_boundsMinPropertyName").stringValue = "_PropertyThatDoesNotExist";
            profileSo.FindProperty("_boundsMaxPropertyName").stringValue = "_PropertyThatDoesNotExist";
            profileSo.ApplyModifiedPropertiesWithoutUndo();
            SetProfile(transition, profile);

            statue.Show(first, false);
            Assert.DoesNotThrow(() => statue.Show(second, true));
            Assert.DoesNotThrow(() => transition.Tick(1f));
            Assert.IsFalse(transition.IsAnimating);
        }

        [Test]
        public void MultipleRenderersAndSharedMaterialsAreRestored()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");

            GameObject firstModel = statue.GetModel(first);
            if (firstModel == null)
            {
                statue.Show(first, false);
                firstModel = statue.GetModel(first);
            }
            Renderer[] renderers = firstModel.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.sharedMaterials = new[]
                {
                    Track(new Material(FindTestShader())),
                    Track(new Material(FindTestShader()))
                };
            }
            Material[] originals = (Material[])renderers[0].sharedMaterials.Clone();

            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));
            statue.Show(second, true);
            transition.Tick(1f);

            CollectionAssert.AreEqual(originals, renderers[0].sharedMaterials);
        }

        [Test]
        public void ScenarioPlayerRebuildUsesInstantForJumpAndLateJoin()
        {
            VenusStatue statue = CreateStatue();
            VenusVersion baseline = CreateVersion("baseline");
            VenusVersion first = CreateVersion("first");
            VenusVersion second = CreateVersion("second");
            var transition = statue.gameObject.AddComponent<ModelTransition>();
            SetProfile(transition, CreateProfile(1f));

            var statueSo = new SerializedObject(statue);
            statueSo.FindProperty("_baseline").objectReferenceValue = baseline;
            statueSo.ApplyModifiedPropertiesWithoutUndo();

            var binding = statue.gameObject.AddComponent<ScenarioObjectBinding>();
            var bindingSo = new SerializedObject(binding);
            bindingSo.FindProperty("_key").stringValue = "RebuildStatue";
            bindingSo.ApplyModifiedPropertiesWithoutUndo();

            var stageGo = Track(new GameObject("RebuildStage"));
            var stage = stageGo.AddComponent<ScenarioStage>();
            Assume.That(stage.gameObject.scene.isLoaded);
            Assert.IsTrue(stage.TryGetObject("RebuildStatue", out GameObject bound));
            Assert.AreSame(statue.gameObject, bound);

            var scenario = Track(ScriptableObject.CreateInstance<ScenarioData>());
            var steps = new List<ScenarioStep>
            {
                Step(new StatueVersionCue("RebuildStatue", baseline)),
                Step(new StatueVersionCue("RebuildStatue", first)),
                new ScenarioStep
                {
                    Id = "late-join-step",
                    Completion = StepCompletion.CuesFinished,
                    Cues = new List<ScenarioCue>(),
                },
            };
            scenario.SetSteps(steps);

            var directorGo = Track(new GameObject("RebuildDirector"));
            directorGo.SetActive(false);
            var director = directorGo.AddComponent<ScenarioDirector>();
            var directorSo = new SerializedObject(director);
            directorSo.FindProperty("_scenario").objectReferenceValue = scenario;
            directorSo.ApplyModifiedPropertiesWithoutUndo();

            var roles = directorGo.AddComponent<DNPDemo.Roles.DemoRoleService>();
            directorSo = new SerializedObject(director);
            directorSo.FindProperty("_roles").objectReferenceValue = roles;
            directorSo.ApplyModifiedPropertiesWithoutUndo();
            directorGo.SetActive(true);

            Assert.IsTrue(director.Scenario == scenario);
            var progressField = typeof(ScenarioDirector).GetField("_progress", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(progressField);
            var progress = new ScenarioProgress(steps.Count);
            Assert.IsTrue(progress.Start());
            Assert.IsTrue(progress.Next());
            Assert.IsTrue(progress.Next());
            progressField.SetValue(director, progress);

            var playerGo = Track(new GameObject("RebuildPlayer"));
            playerGo.SetActive(false);
            var player = playerGo.AddComponent<ScenarioPlayer>();
            var playerSo = new SerializedObject(player);
            playerSo.FindProperty("_director").objectReferenceValue = director;
            playerSo.FindProperty("_stage").objectReferenceValue = stage;
            playerSo.ApplyModifiedPropertiesWithoutUndo();

            statue.Show(baseline, false);
            statue.Show(first, false);
            statue.Show(second, false);

            playerGo.SetActive(true);
            typeof(ScenarioPlayer).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(player, null);

            Assert.AreSame(first, statue.Current);
            Assert.IsFalse(transition.IsAnimating);
            Assert.IsFalse(statue.GetModel(baseline).activeSelf);
            Assert.IsTrue(statue.GetModel(first).activeSelf);
            Assert.IsFalse(statue.GetModel(second).activeSelf);

            statue.Show(second, true);
            Assert.IsTrue(transition.IsAnimating);

            typeof(ScenarioPlayer).GetMethod("Rebuild", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(player, null);

            Assert.AreSame(first, statue.Current);
            Assert.IsFalse(transition.IsAnimating);
            Assert.IsTrue(statue.GetModel(first).activeSelf);
            Assert.IsFalse(statue.GetModel(second).activeSelf);
        }

        static ScenarioStep Step(params ScenarioCue[] cues)
        {
            return new ScenarioStep
            {
                Completion = StepCompletion.CuesFinished,
                Cues = new List<ScenarioCue>(cues),
            };
        }
    }
}
