using Basis.Scripts.BasisSdk;
using DNPDemo.Editor;
using DNPDemo.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace DNPDemo.Tests
{
    /// <summary>
    /// The assistant avatars are loaded by address through Basis, which only installs a prefab that carries a
    /// configured <see cref="BasisAvatar"/> and strips every component its avatar content police does not approve.
    /// </summary>
    public class AssistantAvatarPrefabTests
    {
        const string AvatarContentPolicePath = "Packages/com.basis.sdk/Settings/AvatarContentPoliceSelector.asset";

        [TestCase("_playerAAddressableKey")]
        [TestCase("_playerBAddressableKey")]
        public void AssistantAvatarIsLoadableByBasis(string keyField)
        {
            GameObject prefab = LoadAssistantPrefab(keyField, out string address);

            Assert.IsTrue(prefab.TryGetComponent(out BasisAvatar avatar),
                $"'{address}' needs a BasisAvatar on its root, or Basis leaves it unrigged in T-pose.");

            Assert.IsNotNull(avatar.Animator, $"'{address}': BasisAvatar.Animator is not set.");
            Assert.IsNotNull(avatar.Animator.avatar, $"'{address}': the Animator has no Avatar.");
            Assert.IsTrue(avatar.Animator.avatar.isHuman, $"'{address}': the Animator's Avatar is not humanoid.");

            AssertMeshInPrefab(prefab, avatar.FaceVisemeMesh, address, nameof(BasisAvatar.FaceVisemeMesh));
            AssertMeshInPrefab(prefab, avatar.FaceBlinkMesh, address, nameof(BasisAvatar.FaceBlinkMesh));

            int blendShapeCount = avatar.FaceBlinkMesh.sharedMesh.blendShapeCount;
            Assert.IsNotEmpty(avatar.BlinkViseme, $"'{address}': BasisAvatar.BlinkViseme is empty.");
            foreach (int index in avatar.BlinkViseme)
            {
                Assert.That(index, Is.InRange(0, blendShapeCount - 1),
                    $"'{address}': BlinkViseme {index} is not a blend shape on {avatar.FaceBlinkMesh.name}.");
            }
        }

        [TestCase("_playerAAddressableKey")]
        [TestCase("_playerBAddressableKey")]
        public void AssistantAvatarHasOnlyApprovedComponents(string keyField)
        {
            GameObject prefab = LoadAssistantPrefab(keyField, out string address);
            var police = AssetDatabase.LoadAssetAtPath<ContentPoliceSelector>(AvatarContentPolicePath);
            Assert.IsNotNull(police, AvatarContentPolicePath);

            foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
            {
                Assert.IsNotNull(component, $"'{address}' has a missing script.");
                if (component is Transform)
                {
                    continue;
                }
                Assert.IsTrue(police.IsTypeApproved(component.GetType()),
                    $"'{address}': {component.GetType().FullName} on '{component.name}' is not approved for avatars, " +
                    "so Basis removes it with an error on every load.");
            }
        }

        static GameObject LoadAssistantPrefab(string keyField, out string address)
        {
            var systems = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioInstaller.DemoSystemsPrefabPath);
            Assert.IsNotNull(systems, ScenarioInstaller.DemoSystemsPrefabPath);
            var services = systems.GetComponentsInChildren<DemoAssistantAvatarService>(true);
            Assert.AreEqual(1, services.Length, "DemoSystems must have exactly one DemoAssistantAvatarService.");

            address = new SerializedObject(services[0]).FindProperty(keyField).stringValue;
            Assert.IsFalse(string.IsNullOrWhiteSpace(address), $"DemoAssistantAvatarService.{keyField} is empty.");

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(settings, "Addressables settings are missing.");
            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                {
                    continue;
                }
                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry.address == address)
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.AssetPath);
                        Assert.IsNotNull(prefab, $"Address '{address}' does not point to a prefab ({entry.AssetPath}).");
                        return prefab;
                    }
                }
            }
            Assert.Fail($"No Addressables entry has the address '{address}'.");
            return null;
        }

        static void AssertMeshInPrefab(GameObject prefab, SkinnedMeshRenderer renderer, string address, string field)
        {
            Assert.IsNotNull(renderer, $"'{address}': BasisAvatar.{field} is not set.");
            Assert.IsTrue(renderer.transform.IsChildOf(prefab.transform), $"'{address}': BasisAvatar.{field} is outside the prefab.");
            Assert.IsNotNull(renderer.sharedMesh, $"'{address}': BasisAvatar.{field} has no mesh.");
        }
    }
}
