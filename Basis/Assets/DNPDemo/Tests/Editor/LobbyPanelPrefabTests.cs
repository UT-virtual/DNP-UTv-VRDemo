using DNPDemo.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class LobbyPanelPrefabTests
    {
        [Test]
        public void EveryObjectIsOnUILayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPanelPrefabBuilder.PrefabPath);
            Assert.IsNotNull(prefab, LobbyPanelPrefabBuilder.PrefabPath);

            int uiLayer = LayerMask.NameToLayer(WorldUiBuilder.UILayerName);
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            {
                Assert.AreEqual(uiLayer, t.gameObject.layer,
                    $"'{t.name}' must be on the {WorldUiBuilder.UILayerName} layer so the Basis laser can hit it.");
            }
        }
    }
}
