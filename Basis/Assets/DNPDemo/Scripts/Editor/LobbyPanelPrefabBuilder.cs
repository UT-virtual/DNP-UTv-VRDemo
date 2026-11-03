using System.IO;
using Basis.Scripts.UI;
using DNPDemo.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Creates the initial LobbyPanel prefab with every reference wired. It never overwrites an existing
    /// prefab: after creation the prefab is edited by hand like any other asset.
    /// </summary>
    public static class LobbyPanelPrefabBuilder
    {
        public const string PrefabPath = DNPDemoProjectSetup.Root + "/Prefabs/LobbyPanel.prefab";
        const float PixelsPerMeter = 1000f;
        static readonly Vector2 PanelSize = new(900, 820);
        static readonly Color BackgroundColor = new(0.08f, 0.08f, 0.1f, 0.92f);
        static readonly Color ErrorColor = new(1f, 0.55f, 0.5f);

        [MenuItem("DNP Demo/Create/Lobby Panel Prefab")]
        public static void Create()
        {
            if (File.Exists(PrefabPath))
            {
                EditorUtility.DisplayDialog("DNP Demo", $"{PrefabPath} は既にあります。作り直す場合は先に削除してください。", "OK");
                return;
            }

            GameObject root = new GameObject("LobbyPanel", typeof(RectTransform));
            try
            {
                Build(root);
                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                EditorGUIUtility.PingObject(prefab);
                Debug.Log($"[DNPDemo] Created {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static void Build(GameObject root)
        {
            var rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = PanelSize;
            rootRect.localScale = Vector3.one / PixelsPerMeter;

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var raycaster = root.AddComponent<BasisGraphicUIRayCaster>();
            raycaster.Canvas = canvas;
            root.AddComponent<Image>().color = BackgroundColor;

            TMP_Text title = CreateText("Title", root.transform, "DNP×UTv 復元ワークショップ", 48, TextAlignmentOptions.Center);
            Anchor((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -110), new Vector2(-40, -30));

            TMP_Text error = CreateText("ErrorText", root.transform, string.Empty, 28, TextAlignmentOptions.Center);
            error.color = ErrorColor;
            Anchor((RectTransform)error.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 20), new Vector2(-40, 110));

            // --- Role page ---
            Transform rolePage = CreatePage("RolePage", root.transform);
            CreateText("Prompt", rolePage, "役割を選んでください", 36, TextAlignmentOptions.Center, 80);
            Button hostButton = CreateButton("HostButton", rolePage, "プレイヤーA（ホスト）", 120, 40);
            Button joinButton = CreateButton("JoinButton", rolePage, "プレイヤーB（参加）", 120, 40);

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

        /// <summary>Layer every world-space panel must live on to be laser-interactable in Basis.</summary>
        public const string UILayerName = "UI";

        static void SetLayerRecursively(Transform target, int layer)
        {
            target.gameObject.layer = layer;
            foreach (Transform child in target)
            {
                SetLayerRecursively(child, layer);
            }
        }

        static Transform CreatePage(string name, Transform parent)
        {
            GameObject page = CreateChild(name, parent);
            Anchor((RectTransform)page.transform, Vector2.zero, Vector2.one, new Vector2(60, 120), new Vector2(-60, -120));
            var layout = page.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return page.transform;
        }

        static GameObject CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return go;
        }

        static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, float preferredHeight = -1)
        {
            GameObject go = TMP_DefaultControls.CreateText(new TMP_DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            if (preferredHeight > 0)
            {
                go.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
            }
            return tmp;
        }

        static Button CreateButton(string name, Transform parent, string label, float preferredHeight, float fontSize)
        {
            GameObject go = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            var labelText = go.GetComponentInChildren<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = fontSize;
            labelText.color = new Color(0.1f, 0.1f, 0.12f);
            if (preferredHeight > 0)
            {
                go.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
            }
            return go.GetComponent<Button>();
        }

        static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        static void Assign(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
