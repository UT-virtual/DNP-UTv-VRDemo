using DNPDemo.Operator;
using DNPDemo.Presentation;
using DNPDemo.TaskHud;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static DNPDemo.Editor.WorldUiBuilder;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Generates the initial prefabs the scenario presents through: subtitles, the closing banner, the operator panel and
    /// the task HUD.
    /// Existing prefabs are kept as they are; after creation they are edited by hand.
    /// </summary>
    public static class ScenarioPrefabBuilder
    {
        const string Folder = DNPDemoProjectSetup.Root + "/Prefabs/Scenario";
        public const string SpeakerSubtitlePath = Folder + "/SpeakerSubtitle.prefab";
        public const string NarratorSubtitlePath = Folder + "/NarratorSubtitle.prefab";
        public const string BannerPath = Folder + "/Banner.prefab";
        public const string OperatorPanelPath = Folder + "/OperatorPanel.prefab";
        public const string TaskHudPath = Folder + "/TaskHud.prefab";

        static readonly Color SubtitleBackground = new(0f, 0f, 0f, 0.6f);
        static readonly Color SpeakerLabelColor = new(1f, 0.85f, 0.55f);
        static readonly Color PanelBackground = new(0.12f, 0.06f, 0.06f, 0.95f);
        static readonly Color TaskHudBackground = new(0.05f, 0.07f, 0.1f, 0.85f);
        static readonly Color TaskHudHintColor = new(1f, 0.65f, 0.45f);

        [MenuItem("DNP Demo/Create/Scenario Prefabs")]
        public static void CreateAllIfMissing()
        {
            CreateSpeakerSubtitle();
            CreateNarratorSubtitle();
            CreateBanner();
            CreateOperatorPanel();
            CreateTaskHud();
        }

        /// <summary>Subtitle above a scholar's head. Grows upwards from its anchor and turns to face the viewer.</summary>
        public static GameObject CreateSpeakerSubtitle() =>
            CreatePrefabIfMissing(SpeakerSubtitlePath, "SpeakerSubtitle", root =>
            {
                BuildSubtitle(root, width: 900, speakerFontSize: 30, bodyFontSize: 40, pivotY: 0f);
                root.AddComponent<FaceCamera>();
            });

        /// <summary>Narrator subtitle that follows the view, below the line of sight.</summary>
        public static GameObject CreateNarratorSubtitle() =>
            CreatePrefabIfMissing(NarratorSubtitlePath, "NarratorSubtitle", root =>
            {
                BuildSubtitle(root, width: 700, speakerFontSize: 24, bodyFontSize: 30, pivotY: 0.5f);
                AddHeadFollower(root, distance: 1.2f, heightOffset: -0.35f, sharpness: 4f);
            });

        public static GameObject CreateBanner() =>
            CreatePrefabIfMissing(BannerPath, "Banner", root =>
            {
                SetupWorldCanvas(root, new Vector2(1800, 400), interactive: false);
                var group = root.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
                TMP_Text text = CreateText("Text", root.transform, ScenarioDataBuilder.CompletionBannerText, 140, TextAlignmentOptions.Center);
                text.fontStyle = FontStyles.Bold;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                Anchor((RectTransform)text.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var view = root.AddComponent<BannerView>();
                Assign(view, "_group", group);
                Assign(view, "_text", text);
                AddHeadFollower(root, distance: 2f, heightOffset: 0.05f, sharpness: 2f);
            });

        public static GameObject CreateOperatorPanel() =>
            CreatePrefabIfMissing(OperatorPanelPath, "OperatorPanel", root =>
            {
                SetupWorldCanvas(root, new Vector2(800, 640), interactive: true);
                root.AddComponent<Image>().color = PanelBackground;

                Transform content = CreateVerticalGroup("Content", root.transform, new Vector2(40, 40), new Vector2(-40, -40), 14);
                CreateText("Title", content, "運営メニュー", 40, TextAlignmentOptions.Center, 60);
                TMP_Text status = CreateText("Status", content, string.Empty, 26, TextAlignmentOptions.TopLeft);
                status.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

                GameObject row = CreateChild("StepButtons", content);
                var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 16;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = true;
                row.AddComponent<LayoutElement>().preferredHeight = 90;
                Button back = CreateButton("BackButton", row.transform, "◀ 戻る", 0, 34);
                Button next = CreateButton("NextButton", row.transform, "次へ ▶", 0, 34);

                Button restart = CreateButton("RestartButton", content, "最初からやり直す", 80, 32);
                Button close = CreateButton("CloseButton", content, "閉じる", 70, 30);

                var panel = root.AddComponent<OperatorPanel>();
                Assign(panel, "_statusText", status);
                Assign(panel, "_backButton", back);
                Assign(panel, "_nextButton", next);
                Assign(panel, "_restartButton", restart);
                Assign(panel, "_closeButton", close);

                SetLayerRecursively(root.transform, LayerMask.NameToLayer(UILayerName));
            });

        /// <summary>
        /// Task HUD. The view sits on the root; the panel is a child canvas that follows the free hand and is switched off
        /// while no part waits. The box grows upwards from the panel's pivot, so the hand never covers its text.
        /// Its runner and pen are scene objects, wired by <see cref="ScenarioInstaller"/>.
        /// </summary>
        public static GameObject CreateTaskHud() =>
            CreatePrefabIfMissing(TaskHudPath, "TaskHud", root =>
            {
                const float width = 340;
                GameObject panel = CreateChild("Panel", root.transform);
                SetupWorldCanvas(panel, new Vector2(width, 10), interactive: false);
                ((RectTransform)panel.transform).pivot = new Vector2(0.5f, 0f);

                GameObject box = CreateChild("Box", panel.transform);
                var boxRect = (RectTransform)box.transform;
                boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0f);
                boxRect.pivot = new Vector2(0.5f, 0f);
                boxRect.sizeDelta = new Vector2(width, 0);
                box.AddComponent<Image>().color = TaskHudBackground;
                var layout = box.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(18, 18, 14, 16);
                layout.spacing = 8;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                box.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                TMP_Text heading = CreateText("Heading", box.transform, TaskHudContent.ActorHeading, 26, TextAlignmentOptions.Left);
                heading.fontStyle = FontStyles.Bold;
                TMP_Text task = CreateText("Task", box.transform, string.Empty, 18, TextAlignmentOptions.Left);
                task.textWrappingMode = TextWrappingModes.Normal;
                TMP_Text lines = CreateText("Lines", box.transform, string.Empty, 18, TextAlignmentOptions.Left);
                lines.textWrappingMode = TextWrappingModes.Normal;
                TMP_Text modeHint = CreateText("ModeHint", box.transform, string.Empty, 16, TextAlignmentOptions.Left);
                modeHint.textWrappingMode = TextWrappingModes.Normal;
                modeHint.color = TaskHudHintColor;

                var follower = panel.AddComponent<HandFollower>();

                var view = root.AddComponent<TaskHudView>();
                Assign(view, "_content", panel);
                Assign(view, "_follower", follower);
                Assign(view, "_headingText", heading);
                Assign(view, "_taskText", task);
                Assign(view, "_linesText", lines);
                Assign(view, "_modeHintText", modeHint);
            });

        static void BuildSubtitle(GameObject root, float width, float speakerFontSize, float bodyFontSize, float pivotY)
        {
            SetupWorldCanvas(root, new Vector2(width, 10), interactive: false);
            var group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            GameObject box = CreateChild("Box", root.transform);
            var boxRect = (RectTransform)box.transform;
            boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, pivotY);
            boxRect.pivot = new Vector2(0.5f, pivotY);
            boxRect.sizeDelta = new Vector2(width, 0);
            box.AddComponent<Image>().color = SubtitleBackground;
            var layout = box.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 18, 18);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = box.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text speaker = CreateText("Speaker", box.transform, string.Empty, speakerFontSize, TextAlignmentOptions.Left);
            speaker.color = SpeakerLabelColor;
            TMP_Text body = CreateText("Body", box.transform, string.Empty, bodyFontSize, TextAlignmentOptions.Left);
            body.textWrappingMode = TextWrappingModes.Normal;

            var view = root.AddComponent<SubtitleView>();
            Assign(view, "_group", group);
            Assign(view, "_speakerText", speaker);
            Assign(view, "_bodyText", body);
        }
    }
}
