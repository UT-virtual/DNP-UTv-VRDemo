using System.IO;
using DNPDemo.Editor;
using DNPDemo.Lobby;
using NUnit.Framework;
using TMPro;
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

        [Test]
        public void RolePageShowsTheCredits()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPanelPrefabBuilder.PrefabPath);
            Assert.IsNotNull(prefab, LobbyPanelPrefabBuilder.PrefabPath);

            Transform credits = prefab.transform.Find($"{LobbyPanelPrefabBuilder.RolePageName}/{LobbyPanelPrefabBuilder.CreditsName}");
            Assert.IsNotNull(credits, "DNP Demo > Setup > Install Lobby Credits を実行してください。");
            var text = credits.GetComponent<TMP_Text>();
            Assert.AreEqual(DemoCredits.Text, text.text, "DNP Demo > Setup > Install Lobby Credits を実行してください。");
            Assert.AreEqual(LobbyPanelPrefabBuilder.CreditsFontSize, text.fontSize, "ボタンより小さな文字にすること。");
            Assert.IsFalse(text.raycastTarget, "クレジットがボタンへのレーザーを遮らないこと。");
        }

        [Test]
        public void CreditsAreListedInThirdPartyNotices()
        {
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            string notices = File.ReadAllText(Path.Combine(projectPath, "..", "THIRD_PARTY_NOTICES.md"));
            foreach (string name in DemoCredits.Voices)
            {
                StringAssert.Contains(name, notices);
            }
            foreach (string name in DemoCredits.SoundEffects)
            {
                StringAssert.Contains(name, notices);
            }
        }
    }
}
