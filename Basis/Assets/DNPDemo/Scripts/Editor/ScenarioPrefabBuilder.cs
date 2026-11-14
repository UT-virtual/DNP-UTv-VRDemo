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
        static readonly Color TaskHudStatusColor = new(0.55f, 0.85f, 1f);
        static readonly Color MeterBackground = new(0.05f, 0.05f, 0.07f, 1f);
        static readonly Color MeterFillColor = new(0.55f, 0.6f, 0.7f);
        static readonly Color ThresholdMarkerColor = new(1f, 0.8f, 0.3f);
        const float MicrophoneSwitchButtonWidth = 260;

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
                SetupWorldCanvas(root, new Vector2(800, 940), interactive: true);
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
                AddMicrophoneThresholdSection(content);
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
                TMP_Text status = CreateTaskHudStatusText(box.transform);
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
                Assign(view, "_statusText", status);
                Assign(view, "_modeHintText", modeHint);
            });

        /// <summary>The task HUD's line for the running tool's status (<see cref="TaskHudView"/>), hidden while empty.</summary>
        public static TMP_Text CreateTaskHudStatusText(Transform box)
        {
            TMP_Text status = CreateText("Status", box, string.Empty, 18, TextAlignmentOptions.Left);
            status.textWrappingMode = TextWrappingModes.Normal;
            status.color = TaskHudStatusColor;
            status.gameObject.SetActive(false);
            return status;
        }

        /// <summary>
        /// Operator panel section for the voice command threshold (<see cref="MicrophoneThresholdControl"/>): a heading,
        /// the microphone in use with a button to switch it, the live level, a meter with the threshold marked on it, and
        /// buttons to lower, raise and reset the threshold.
        /// Added to the panel's vertical <paramref name="content"/>.
        /// </summary>
        public static MicrophoneThresholdControl AddMicrophoneThresholdSection(Transform content)
        {
            GameObject section = CreateChild("MicrophoneThreshold", content);
            var sectionLayout = section.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 8;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            CreateText("Heading", section.transform, "音声入力のしきい値", 28, TextAlignmentOptions.Left, 40);
            CreateMicrophoneDeviceRow(section.transform, out TMP_Text device, out Button switchDevice);
            TMP_Text level = CreateText("Level", section.transform, string.Empty, 24, TextAlignmentOptions.Left, 34);

            GameObject meter = CreateChild("Meter", section.transform);
            meter.AddComponent<Image>().color = MeterBackground;
            meter.AddComponent<LayoutElement>().preferredHeight = 28;
            GameObject fill = CreateChild("Fill", meter.transform);
            fill.AddComponent<Image>().color = MeterFillColor;
            var fillRect = (RectTransform)fill.transform;
            Anchor(fillRect, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            GameObject marker = CreateChild("ThresholdMarker", meter.transform);
            marker.AddComponent<Image>().color = ThresholdMarkerColor;
            var markerRect = (RectTransform)marker.transform;
            Anchor(markerRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-3f, -4f), new Vector2(3f, 4f));

            GameObject row = CreateChild("Buttons", section.transform);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 16;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            row.AddComponent<LayoutElement>().preferredHeight = 70;
            Button lower = CreateButton("LowerButton", row.transform, "下げる", 0, 28);
            Button raise = CreateButton("RaiseButton", row.transform, "上げる", 0, 28);
            Button reset = CreateButton("ResetButton", row.transform, "初期値", 0, 28);

            var control = section.AddComponent<MicrophoneThresholdControl>();
            Assign(control, "_deviceText", device);
            Assign(control, "_switchDeviceButton", switchDevice);
            Assign(control, "_levelText", level);
            Assign(control, "_meterFill", fillRect);
            Assign(control, "_thresholdMarker", markerRect);
            Assign(control, "_lowerButton", lower);
            Assign(control, "_raiseButton", raise);
            Assign(control, "_resetButton", reset);
            return control;
        }

        /// <summary>Row of the threshold section with the microphone in use and the button that switches to the next one.</summary>
        public static GameObject CreateMicrophoneDeviceRow(Transform section, out TMP_Text device, out Button switchDevice)
        {
            GameObject row = CreateChild("Device", section);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 16;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            row.AddComponent<LayoutElement>().preferredHeight = 60;
            device = CreateText("Name", row.transform, string.Empty, 22, TextAlignmentOptions.Left);
            device.textWrappingMode = TextWrappingModes.NoWrap;
            device.overflowMode = TextOverflowModes.Ellipsis;
            // The name takes whatever width the button leaves; a long name is cut with an ellipsis instead of squeezing
            // the button (the row lays children out between their minimum and preferred widths).
            var nameLayout = device.gameObject.AddComponent<LayoutElement>();
            nameLayout.minWidth = 0;
            nameLayout.preferredWidth = 0;
            nameLayout.flexibleWidth = 1;
            switchDevice = CreateButton("SwitchButton", row.transform, "マイクを切り替える", 0, 24);
            var buttonLayout = switchDevice.gameObject.AddComponent<LayoutElement>();
            buttonLayout.minWidth = MicrophoneSwitchButtonWidth;
            buttonLayout.preferredWidth = MicrophoneSwitchButtonWidth;
            buttonLayout.flexibleWidth = 0;
            return row;
        }

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
