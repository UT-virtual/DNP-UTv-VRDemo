using System.IO;
using DNPDemo.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static DNPDemo.Editor.WorldUiBuilder;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Creates the initial LobbyPanel prefab with every reference wired. It never overwrites an existing
    /// prefab: after creation the prefab is edited by hand like any other asset. The credits on the role page are the
    /// exception: their text comes from <see cref="DemoCredits"/>, and <see cref="InstallCredits"/> adds them to the
    /// existing prefab or brings their text up to date.
    /// </summary>
    public static class LobbyPanelPrefabBuilder
    {
        public const string PrefabPath = DNPDemoProjectSetup.Root + "/Prefabs/LobbyPanel.prefab";
        static readonly Vector2 PanelSize = new(900, 820);
        static readonly Color BackgroundColor = new(0.08f, 0.08f, 0.1f, 0.92f);
        static readonly Color ErrorColor = new(1f, 0.55f, 0.5f);

        public const string RolePageName = "RolePage";
        public const string CreditsName = "Credits";
        /// <summary>Font size of the credits, well below the buttons (40) so they stay in the background.</summary>
        public const float CreditsFontSize = 20;
        /// <summary>Height of the credits at the bottom of the role page: two lines of <see cref="CreditsFontSize"/>.</summary>
        const float CreditsHeight = 64;
        static readonly Color CreditsColor = new(0.62f, 0.62f, 0.66f);

        [MenuItem("DNP Demo/Create/Lobby Panel Prefab")]
        public static void Create()
        {
            if (File.Exists(PrefabPath))
            {
                EditorUtility.DisplayDialog("DNP Demo", $"{PrefabPath} は既にあります。作り直す場合は先に削除してください。", "OK");
                return;
            }
            GameObject prefab = CreatePrefabIfMissing(PrefabPath, "LobbyPanel", Build);
            EditorGUIUtility.PingObject(prefab);
        }

        /// <summary>
        /// Adds the credits to the role page of the existing prefab, or updates their text to
        /// <see cref="DemoCredits"/>, and saves the prefab when anything changed.
        /// </summary>
        [MenuItem("DNP Demo/Setup/Install Lobby Credits")]
        public static void InstallCredits()
        {
            if (!File.Exists(PrefabPath))
            {
                Debug.LogWarning($"[DNPDemo] {PrefabPath} がないため、クレジットを追加できません。");
                return;
            }
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform rolePage = root.transform.Find(RolePageName);
                if (rolePage == null)
                {
                    Debug.LogWarning($"[DNPDemo] {PrefabPath} に {RolePageName} がないため、クレジットを追加できません。");
                    return;
                }
                if (EnsureCredits(rolePage))
                {
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                    Debug.Log($"[DNPDemo] {PrefabPath} のクレジットを更新しました。");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Places the credits at the bottom of <paramref name="rolePage"/>, outside its layout so the buttons keep their
        /// place, unless they are there, and sets their text. Returns true when anything changed.
        /// </summary>
        static bool EnsureCredits(Transform rolePage)
        {
            bool changed = false;
            Transform existing = rolePage.Find(CreditsName);
            TMP_Text credits = existing != null ? existing.GetComponent<TMP_Text>() : null;
            if (credits == null)
            {
                credits = CreateText(CreditsName, rolePage, string.Empty, CreditsFontSize, TextAlignmentOptions.Bottom);
                credits.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Anchor((RectTransform)credits.transform, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, CreditsHeight));
                credits.color = CreditsColor;
                credits.raycastTarget = false;
                changed = true;
            }
            if (credits.text != DemoCredits.Text)
            {
                credits.text = DemoCredits.Text;
                changed = true;
            }
            return changed;
        }

        static void Build(GameObject root)
        {
            SetupWorldCanvas(root, PanelSize, interactive: true);
            root.AddComponent<Image>().color = BackgroundColor;

            TMP_Text title = CreateText("Title", root.transform, "DNP×UTv 復元ワークショップ", 48, TextAlignmentOptions.Center);
            Anchor((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -110), new Vector2(-40, -30));

            TMP_Text error = CreateText("ErrorText", root.transform, string.Empty, 28, TextAlignmentOptions.Center);
            error.color = ErrorColor;
            Anchor((RectTransform)error.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 20), new Vector2(-40, 110));

            // --- Role page ---
            Transform rolePage = CreatePage(RolePageName, root.transform);
            CreateText("Prompt", rolePage, "役割を選んでください", 36, TextAlignmentOptions.Center, 80);
            Button hostButton = CreateButton("HostButton", rolePage, "プレイヤーA（ホスト）", 120, 40);
            Button joinButton = CreateButton("JoinButton", rolePage, "プレイヤーB（参加）", 120, 40);
            EnsureCredits(rolePage);

            // --- Search page ---
            Transform searchPage = CreatePage("SearchPage", root.transform);
            TMP_Text searchStatus = CreateText("SearchStatus", searchPage, "プレイヤーA の端末を探しています…", 32, TextAlignmentOptions.Center, 70);
            GameObject hostList = CreateChild("HostList", searchPage);
            var hostListLayout = hostList.AddComponent<VerticalLayoutGroup>();
            hostListLayout.spacing = 12;
            hostListLayout.childControlWidth = true;
            hostListLayout.childControlHeight = true;
            hostListLayout.childForceExpandHeight = false;
            hostList.AddComponent<LayoutElement>().flexibleHeight = 1;
            Button hostItemTemplate = CreateButton("HostItemTemplate", hostList.transform, "ホスト", 90, 30);
            Button manualEntryButton = CreateButton("ManualEntryButton", searchPage, "IP アドレスを入力", 90, 32);
            Button searchBackButton = CreateButton("BackButton", searchPage, "戻る", 90, 32);

            // --- Manual entry page ---
            Transform manualPage = CreatePage("ManualPage", root.transform);
            TMP_Text display = CreateText("AddressDisplay", manualPage, string.Empty, 44, TextAlignmentOptions.Center, 80);
            GameObject keypadObject = CreateChild("Keypad", manualPage);
            var grid = keypadObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(240, 72);
            grid.spacing = new Vector2(16, 12);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            keypadObject.AddComponent<LayoutElement>().preferredHeight = 4 * 72 + 3 * 12;
            var keypad = keypadObject.AddComponent<IpKeypad>();
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "0", "←" };
            foreach (string key in keys)
            {
                Button keyButton = CreateButton("Key_" + key, keypadObject.transform, key, 0, 36);
                var keypadKey = keyButton.gameObject.AddComponent<IpKeypadKey>();
                var keySo = new SerializedObject(keypadKey);
                keySo.FindProperty("_kind").enumValueIndex = key == "←" ? (int)IpKeypadKey.KeyKind.Backspace : (int)IpKeypadKey.KeyKind.Character;
                keySo.FindProperty("_character").stringValue = key == "←" ? string.Empty : key;
                keySo.ApplyModifiedPropertiesWithoutUndo();
            }
            Assign(keypad, "_display", display);
            Button connectButton = CreateButton("ConnectButton", manualPage, "接続", 90, 34);
            Button manualBackButton = CreateButton("BackButton", manualPage, "戻る", 90, 32);

            // --- Status page ---
            Transform statusPage = CreatePage("StatusPage", root.transform);
            TMP_Text statusText = CreateText("StatusText", statusPage, "接続しています…", 36, TextAlignmentOptions.Center);
            statusText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            var lobby = root.AddComponent<LobbyPanel>();
            var so = new SerializedObject(lobby);
            so.FindProperty("_rolePage").objectReferenceValue = rolePage.gameObject;
            so.FindProperty("_searchPage").objectReferenceValue = searchPage.gameObject;
            so.FindProperty("_manualPage").objectReferenceValue = manualPage.gameObject;
            so.FindProperty("_statusPage").objectReferenceValue = statusPage.gameObject;
            so.FindProperty("_errorText").objectReferenceValue = error;
            so.FindProperty("_hostButton").objectReferenceValue = hostButton;
            so.FindProperty("_joinButton").objectReferenceValue = joinButton;
            so.FindProperty("_searchStatusText").objectReferenceValue = searchStatus;
            so.FindProperty("_hostListRoot").objectReferenceValue = hostList.transform;
            so.FindProperty("_hostItemTemplate").objectReferenceValue = hostItemTemplate;
            so.FindProperty("_manualEntryButton").objectReferenceValue = manualEntryButton;
            so.FindProperty("_searchBackButton").objectReferenceValue = searchBackButton;
            so.FindProperty("_keypad").objectReferenceValue = keypad;
            so.FindProperty("_connectButton").objectReferenceValue = connectButton;
            so.FindProperty("_manualBackButton").objectReferenceValue = manualBackButton;
            so.FindProperty("_statusText").objectReferenceValue = statusText;
            so.ApplyModifiedPropertiesWithoutUndo();

            searchPage.gameObject.SetActive(false);
            manualPage.gameObject.SetActive(false);
            statusPage.gameObject.SetActive(false);

            // Basis only draws the laser and dispatches pointer events when the hit collider is on a UI layer.
            SetLayerRecursively(root.transform, LayerMask.NameToLayer(UILayerName));
        }

        static Transform CreatePage(string name, Transform parent) =>
            CreateVerticalGroup(name, parent, new Vector2(60, 120), new Vector2(-60, -120), 16);
    }
}
