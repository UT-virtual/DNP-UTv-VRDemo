using System.Collections.Generic;
using System.IO;
using DNPDemo.Audio;
using DNPDemo.Interaction;
using DNPDemo.Interaction.Background;
using DNPDemo.Interaction.History;
using DNPDemo.Interaction.Manipulate;
using DNPDemo.Interaction.Selection;
using DNPDemo.Interaction.Stroke;
using DNPDemo.Interaction.Voice;
using DNPDemo.Environment;
using DNPDemo.Menu;
using DNPDemo.Scenario;
using DNPDemo.Scenario.Cues;
using DNPDemo.Venus;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Generates the initial scenario asset from the script (docs/SCENARIO.md ver.4.0, steps of docs/PLAN.md §3).
    /// It never overwrites an existing asset: after creation the asset is the source of truth and is edited in the inspector.
    /// </summary>
    public static class ScenarioDataBuilder
    {
        public const string AssetPath = DNPDemoProjectSetup.Root + "/Data/ScenarioData.asset";

        /// <summary>Key of the statue spotlight (<see cref="ScenarioObjectBinding"/> on Lighting/Spot_Statue).</summary>
        public const string SpotlightKey = "Spotlight";

        /// <summary>Key of the left shoulder zone on the placeholder statue (<see cref="ScenarioObjectBinding"/> on Statue_Original/Zone_LeftShoulder).</summary>
        public const string LeftShoulderZoneKey = "Zone_LeftShoulder";

        /// <summary>Key of the zone in front of the chest on the placeholder statue (<see cref="ScenarioObjectBinding"/> on Statue_Original/Zone_ChestFront).</summary>
        public const string ChestFrontZoneKey = "Zone_ChestFront";

        /// <summary>
        /// Key of the placeholder apple of Ver 1.0 (<see cref="ScenarioObjectBinding"/> on Statue_Original/Apple). Hidden at
        /// scene load and shown once the left arm has been generated. It carries the zone that is selected and grabbed.
        /// </summary>
        public const string AppleKey = "Apple";

        /// <summary>Key of the marker the apple is moved to, under the hand (<see cref="ScenarioObjectBinding"/> on Statue_Original/Goal_AppleUnderHand).</summary>
        public const string AppleGoalKey = "Goal_AppleUnderHand";

        /// <summary>Key of the zone around the left hand and forearm of Ver 1.0 and 1.1, selected in step 7.</summary>
        public const string LeftForearmZoneKey = "Zone_LeftForearm";

        /// <summary>Key of the zone from the left shoulder to the elbow of Ver 1.1, selected in step 9.</summary>
        public const string LeftUpperArmZoneKey = "Zone_LeftUpperArm";

        /// <summary>
        /// Key of the translucent stand-in for Ver 1.1's left arm that step 9 turns about the shoulder: the arm is part of
        /// the whole-body model, so the pen moves this proxy and the statue itself stays as it is (docs/PLAN.md §2.5).
        /// Hidden at scene load and shown only during step 9.
        /// </summary>
        public const string LeftArmProxyKey = "LeftArmProxy";

        /// <summary>Key of the zone on the arm proxy that the pen's tip grabs; it turns with the proxy.</summary>
        public const string LeftArmProxyGrabKey = "LeftArmProxy_Grab";

        /// <summary>Key of the left shoulder joint the arm proxy turns about; the axis is its local up.</summary>
        public const string LeftShoulderPivotKey = "Pivot_LeftShoulder";

        /// <summary>Key of the region of the left arm in every version, where the version transitions of steps 4 to 12 centre.</summary>
        public const string LeftArmFocusKey = "Focus_LeftArm";

        /// <summary>
        /// Key of the set curve from the left shoulder to the front of the chest (<see cref="GuideCurve"/>), whose ends
        /// are the centres of the guide's zones. It carries the <see cref="GuideSnapEffect"/> played as step 11 completes.
        /// </summary>
        public const string LeftArmGuideKey = "Guide_LeftArmCurve";

        /// <summary>Key of the dashed line that shows <see cref="LeftArmGuideKey"/> while step 11 waits for the stroke.</summary>
        public const string LeftArmGuideLineKey = "GuideLine_LeftArm";

        /// <summary>
        /// Key of the hand mirror on its own (<see cref="ScenarioObjectBinding"/> on Statue_Original/Targets/HandMirror),
        /// generated from the sketch of step 13 and moved to the right hand in step 14. Hidden at scene load, shown from
        /// step 14 and hidden again once Ver 3.0 holds the mirror. It carries the zone that is grabbed.
        /// </summary>
        public const string HandMirrorKey = "HandMirror";

        /// <summary>
        /// Key of the marker the hand mirror appears at when there is no sketch to place it by, and around which a
        /// sketched mirror is kept (<see cref="MirrorSpawnMaxOffset"/>). Turned like the goal.
        /// </summary>
        public const string MirrorSpawnKey = "Spawn_HandMirror";

        /// <summary>Key of the marker the hand mirror is moved to: the pose of the mirror Ver 3.0 holds in its right hand.</summary>
        public const string MirrorGoalKey = "Goal_MirrorRightHand";

        /// <summary>Key of the zone from the right shoulder to the mirror at the right hand, selected in step 15.</summary>
        public const string RightArmToMirrorZoneKey = "Zone_RightArmToMirror";

        /// <summary>Key of the zone around the right wrist and the mirror of Ver 3.0, selected in step 17.</summary>
        public const string RightHandMirrorZoneKey = "Zone_RightHandMirror";

        /// <summary>
        /// Key of the translucent stand-in for Ver 3.0's right hand and mirror that step 17 twists about the wrist, like
        /// <see cref="LeftArmProxyKey"/> in step 9. Hidden at scene load and shown only during step 17.
        /// </summary>
        public const string RightHandProxyKey = "RightHandProxy";

        /// <summary>Key of the zone on the hand proxy that the pen's tip grabs, around the mirror.</summary>
        public const string RightHandProxyGrabKey = "RightHandProxy_Grab";

        /// <summary>Key of the right wrist joint the hand proxy twists about; the axis is its local up, along the forearm.</summary>
        public const string RightWristPivotKey = "Pivot_RightWrist";

        /// <summary>Key of the region of the right arm and the mirror, where the version transitions of steps 16 and 18 centre.</summary>
        public const string RightArmFocusKey = "Focus_RightArm";

        /// <summary>Id of step 13, whose sketch the hand mirror is generated from.</summary>
        public const string DrawMirrorStepId = "s3-draw-mirror";

        /// <summary>Step 14: the mirror appears at most this far from <see cref="MirrorSpawnKey"/>, in metres.</summary>
        public const float MirrorSpawnMaxOffset = 0.8f;

        /// <summary>
        /// Step 14: the mirror may move this far from where it appeared, in metres. Enough to reach the goal from anywhere
        /// it can appear (ScenarioSceneBindingTests checks it against the markers).
        /// </summary>
        public const float MirrorMaxDistance = 2.5f;

        /// <summary>Step 17: the hand twists to this angle about the forearm, where Ver 3.1's mirror faces the head.</summary>
        public const float WristTwistTargetAngle = VenusAssetBuilder.MirrorTwistDegrees;

        /// <summary>Step 17: the hand may twist this much past 0° and past the target, so the target is easy to pass and come back to.</summary>
        public const float WristTwistMargin = 30f;

        /// <summary>Step 17: released within this many degrees of the target, the hand snaps to it.</summary>
        public const float WristTwistSnapAngle = 12f;

        /// <summary>Step 9: the arm may turn outwards (a negative angle) this far, so a small slip the wrong way does not stop it.</summary>
        public const float ArmSlideMinAngle = -10f;

        /// <summary>
        /// Step 9: the arm turns inwards (towards the body: the left arm is on the +X side, so a positive angle about the
        /// shoulder's up) at most this far.
        /// </summary>
        public const float ArmSlideMaxAngle = 60f;

        /// <summary>Step 9: released past this angle inwards, the slide is done.</summary>
        public const float ArmSlideThresholdAngle = 25f;

        /// <summary>
        /// Key of the comparison table (<see cref="ScenarioObjectBinding"/> on the ComparisonTable root). Hidden at scene load
        /// and shown when step 19 asks for the comparison; the statue on it is <see cref="VenusStatueKeys.Comparison"/>.
        /// </summary>
        public const string ComparisonTableKey = "ComparisonTable";

        /// <summary>
        /// Key of the temple's floor (<see cref="ScenarioObjectBinding"/> on the Env_Temple root, which holds the floor). Hidden
        /// at scene load and shown right after the closing step switches to the temple, so the statue stands on something
        /// that receives its shadow.
        /// </summary>
        public const string TempleFloorKey = "TempleFloor";

        /// <summary>The apple may move this far from where it starts, in metres.</summary>
        public const float AppleMaxDistance = 0.4f;

        public const string CompletionBannerText = "WORKSHOP COMPLETE";

        /// <summary>Step 21: the voice command that generates the temple background.</summary>
        public const string TempleTranscript = "ヴィーナス像に似合う神殿の背景を生成";

        /// <summary>Hint of every voice command (<see cref="InteractionPart.Hint"/>).</summary>
        public const string VoiceHint = "トリガーを押している間に、指示の文をはっきり読み上げてください";

        [MenuItem("DNP Demo/Create/Scenario Data")]
        public static void CreateFromMenu()
        {
            if (File.Exists(AssetPath))
            {
                EditorUtility.DisplayDialog("DNP Demo", $"{AssetPath} は既にあります。作り直す場合は先に削除してください。", "OK");
                return;
            }
            EditorGUIUtility.PingObject(CreateIfMissing());
        }

        public static ScenarioData CreateIfMissing()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ScenarioData>(AssetPath);
            if (existing != null)
            {
                return existing;
            }
            // The version cues and the environment cue refer to these assets.
            VenusAssetBuilder.CreateIfMissing();
            EnvironmentPresetBuilder.CreatePresetsIfMissing();
            var data = ScriptableObject.CreateInstance<ScenarioData>();
            List<ScenarioStep> steps = CreateDefaultSteps();
            EnsureVersionSounds(steps);
            data.SetSteps(steps);
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
            AssetDatabase.CreateAsset(data, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DNPDemo] Created {AssetPath}");
            return data;
        }

        /// <summary>
        /// Brings an existing asset's closing step up to <see cref="CreateDefaultSteps"/>: it opens with the temple's
        /// <see cref="EnvironmentCue"/>, followed by the cue that shows the temple floor.
        /// </summary>
        public static void EnsureEnvironmentCueInExistingAsset()
        {
            ScenarioData data = AssetDatabase.LoadAssetAtPath<ScenarioData>(AssetPath);
            if (data != null && EnsureClosingEnvironmentCues(data))
            {
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
            }
        }

        static bool EnsureClosingEnvironmentCues(ScenarioData data)
        {
            EnvironmentPreset templePreset = AssetDatabase.LoadAssetAtPath<EnvironmentPreset>(EnvironmentPresetBuilder.TemplePresetPath);
            foreach (ScenarioStep step in data.Steps)
            {
                if (step == null || step.Id != "closing")
                {
                    continue;
                }
                step.Cues ??= new List<ScenarioCue>();
                bool changed = false;
                int environmentIndex = step.Cues.FindIndex(cue => cue is EnvironmentCue);
                if (environmentIndex < 0)
                {
                    step.Cues.Insert(0, new EnvironmentCue(templePreset));
                    environmentIndex = 0;
                    changed = true;
                }
                else if (step.Cues[environmentIndex] is EnvironmentCue { Preset: null } environmentCue && templePreset != null)
                {
                    environmentCue.Preset = templePreset;
                    changed = true;
                }
                if (!step.Cues.Exists(cue => cue is SetObjectActiveCue { Key: TempleFloorKey }))
                {
                    step.Cues.Insert(environmentIndex + 1, new SetObjectActiveCue(TempleFloorKey, true));
                    changed = true;
                }
                return changed;
            }
            return false;
        }

        /// <summary>
        /// Steps whose statue change has a sound, played by a <see cref="SoundCue"/> right after the step's first line
        /// (<see cref="EnsureVersionSounds"/>). A part added to
        /// the statue is <see cref="SeEvent.Generate"/>, a reshape is <see cref="SeEvent.Morph"/>. The comparison table
        /// (p3-temple-proposal) and the temple (closing) have none.
        /// </summary>
        public static readonly IReadOnlyList<(string StepId, SeEvent Sound)> VersionSounds = new[]
        {
            ("s1-concerns", SeEvent.Generate),
            ("s2-natural-curve", SeEvent.Morph),
            ("s2-mismatch", SeEvent.Morph),
            ("s3-hand-mirror", SeEvent.Morph),
            ("s3-mirror-angle", SeEvent.Generate),
            ("p2-review", SeEvent.Morph),
        };

        /// <summary>
        /// Adds the sound cues of <see cref="VersionSounds"/> to the existing asset, or moves them or fills their empty
        /// clips, and saves it. Run from "DNP Demo/Setup/Install Sound Effects".
        /// </summary>
        public static void EnsureVersionSoundsInExistingAsset()
        {
            ScenarioData data = AssetDatabase.LoadAssetAtPath<ScenarioData>(AssetPath);
            if (data == null)
            {
                Debug.LogWarning($"[DNPDemo] {AssetPath} がないため、効果音のキューを追加できません。");
                return;
            }
            Undo.RecordObject(data, "Add SE Cues");
            if (EnsureVersionSounds(data.Steps))
            {
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);
                Debug.Log($"[DNPDemo] {AssetPath} に効果音のキューを追加しました。", data);
            }
        }

        /// <summary>
        /// Puts the sound cue of <see cref="VersionSounds"/> right after the first line of each listed step, or right before
        /// it when the step has no other line: after its only line the step would end at once, and changing the step stops
        /// the effects. Overlapping a line is fine, the effects are lowered while lines play (<see cref="SeDucking"/>). A
        /// sound cue already in the step is moved there and keeps its clip and volume unless the clip is empty. Returns true
        /// when anything changed.
        /// </summary>
        public static bool EnsureVersionSounds(IReadOnlyList<ScenarioStep> steps)
        {
            bool changed = false;
            foreach ((string stepId, SeEvent sound) in VersionSounds)
            {
                ScenarioStep step = FindStep(steps, stepId);
                if (step?.Cues == null || step.Cues.FindIndex(cue => cue is LineCue) < 0)
                {
                    Debug.LogWarning($"[DNPDemo] ステップ {stepId} に台詞がないため、効果音のキューを置けません。");
                    continue;
                }
                AudioClip clip = SoundEffectsSetup.LoadClip(sound);
                int previousIndex = step.Cues.FindIndex(c => c is SoundCue);
                SoundCue cue;
                if (previousIndex < 0)
                {
                    cue = new SoundCue { Clip = clip };
                }
                else
                {
                    cue = (SoundCue)step.Cues[previousIndex];
                    step.Cues.RemoveAt(previousIndex);
                    if (cue.Clip == null && clip != null)
                    {
                        cue.Clip = clip;
                        changed = true;
                    }
                }
                int index = VersionSoundIndex(step.Cues);
                step.Cues.Insert(index, cue);
                changed |= index != previousIndex;
            }
            return changed;
        }

        /// <summary>Where the sound cue goes in <paramref name="cues"/> (without it): see <see cref="EnsureVersionSounds"/>.</summary>
        public static int VersionSoundIndex(List<ScenarioCue> cues)
        {
            int firstLine = cues.FindIndex(c => c is LineCue);
            bool onlyLine = cues.FindLastIndex(c => c is LineCue) == firstLine;
            return onlyLine ? firstLine : firstLine + 1;
        }

        static ScenarioStep FindStep(IReadOnlyList<ScenarioStep> steps, string id)
        {
            foreach (ScenarioStep step in steps)
            {
                if (step != null && step.Id == id)
                {
                    return step;
                }
            }
            return null;
        }

        /// <summary>
        /// The steps of docs/PLAN.md §3. A version reached by an operation is shown by a cue at the head of the next step,
        /// so it appears as the step completes and every later step applies it instantly. The version assets are loaded
        /// from <see cref="VenusAssetBuilder.CatalogPath"/> and the temple preset from
        /// <see cref="EnvironmentPresetBuilder.TemplePresetPath"/>; this method only loads them and never creates assets.
        /// The sound cues are not included: <see cref="EnsureVersionSounds"/> adds them. It loads their clips from the asset
        /// database, which most callers do not need.
        /// </summary>
        public static List<ScenarioStep> CreateDefaultSteps()
        {
            EnvironmentPreset templePreset = AssetDatabase.LoadAssetAtPath<EnvironmentPreset>(EnvironmentPresetBuilder.TemplePresetPath);
            var steps = new List<ScenarioStep>
            {
                new()
                {
                    Id = "lobby",
                    Title = "ロビー（接続待ち）",
                    Completion = StepCompletion.PlayersReady,
                    Cues = new List<ScenarioCue> { Version(VenusAssetBuilder.ArmlessId) },
                },
                Auto("opening", "Opening ナレーター導入",
                    new SetObjectActiveCue(SpotlightKey, true),
                    new WaitCue(1f),
                    N("ようこそ、文化財の復元ワークショップへ。\n今回のテーマは、言葉だけではすれ違ってしまう「認識の差」を、3D空間の操作によって埋めることです。"),
                    N("舞台は、失われたミロのヴィーナスの腕をめぐる復元会議。神話や発掘記録から姿を探る考古学者と、石の形状や接合部にかかる負荷を検討する構造力学の専門家が議論しています。"),
                    N("体験者のお二人は、それぞれの専門家の助手です。お手元には、3Dモデルの生成や修正が簡単にできる空間ペンがございます。画面の案内に従って、二人のイメージを空間ペンを使って具体的な形にしていきましょう。")),
                Auto("s1-apple-theory", "Scene 1 林檎説",
                    A("ミロのヴィーナスの近くでは、林檎を持った手の断片が見つかっています。このことから腕が欠損する前は、林檎を持っていたという説が有力だとされています。私はもしこの説が本当であれば、ヴィーナスは前方に腕を伸ばして林檎を掲げていたと考えています。"),
                    B("なるほど...『前方に腕を伸ばして林檎を掲げる』姿ですね。まずは先生のイメージを3Dモデルでお見せしていただいたけないでしょうか？"),
                    A("分かりました。一度、3Dモデルにおこしてみましょう！")),
                Operation("s1-voice-left-arm", "Scene 1 [A] 左腕を音声生成", DemoRole.PlayerA,
                    "音声生成を起動し、「現在のヴィーナスについて、手のひらに林檎がのった水平に伸びる左手を追加」と出力せよ。",
                    Voice("「手のひらに林檎がのった水平に伸びる左手を追加」と音声指示する", "現在のヴィーナスについて、手のひらに林檎がのった水平に伸びる左手を追加")),
                Auto("s1-concerns", "Scene 1 構造の懸念",
                    Version(VenusAssetBuilder.Ver10Id, LeftArmFocusKey),
                    new SetObjectActiveCue(AppleKey, true),
                    A("私が考える、ヴィーナスの左手のイメージはまさにこんな感じですね。"),
                    B("ありがとうございます。うーむ...私の専門の構造力学的な観点からすると、腕をここまで水平に伸ばすことは付け根への大きな負担に繋がりそうです。ヴィーナスのような大事な像を昔の職人が、不安定な構造で作るとはとても思えないんですよね..."),
                    A("なるほど。他に懸念点などはございませんか？"),
                    B("そうですね...私的にはヴィーナスの林檎の持ち方も気になっています。残存しているヴィーナスの脇の部分と、現在の林檎の持ち方があまり一致しているように思えないんですよね。Ａ先生のイメージですと林檎を手のひらの上にのせていますが、ボールを投げる時のように、上から林檎を包み込むような持ち方の方が脇のあたりの筋肉の動きと自然につながると考えやすいんですよ。")),
                Auto("s2-request-b", "Scene 2 学者Bへの依頼",
                    A("あー...構造の面から見ると様々な問題がありそうですね。よろしければ、B先生が考える力学的に安定的で、筋肉の動きにも矛盾がないアイデアを3Dモデルで出していただけないでしょうか？"),
                    B("そうですね、一度私の考えもモデル化してみましょう。")),
                Operation("s2-move-apple", "Scene 2 [B] 林檎を手の下へ", DemoRole.PlayerB,
                    "空間ペンで林檎の領域を選択し、林檎の位置を調整せよ。（手の下に持ってくる）",
                    WithHint(new SelectionPart("林檎を範囲選択する", AppleKey),
                        "光っている林檎のまわりを、トリガーを押したままペン先でなぞってください"),
                    WithHint(ManipulatePart.Translate("林檎を手の下へ移動する", AppleKey, AppleGoalKey, AppleMaxDistance),
                        "光っている林檎にペン先を当て、トリガーを押したまま水色の林檎の位置まで動かしてください")),
                WithCues(Operation("s2-regrip-apple", "Scene 2 [B] 林檎の持ち方を修正", DemoRole.PlayerB,
                        "空間ペンで腕を選択し、「選択した範囲について、上からボールを包み込むように林檎を持ち直す形に修正」と入力せよ。",
                        WithHint(new SelectionPart("左手から前腕を範囲選択する", LeftForearmZoneKey),
                            "左手から前腕までを、トリガーを押したままペン先でなぞってください"),
                        Voice("「上からボールを包み込むように林檎を持ち直す形に修正」と音声指示する", "選択した範囲について、上からボールを包み込むように林檎を持ち直す形に修正")),
                    // The stage puts the apple back on the palm when the step changes; this keeps the result of step 6.
                    new MatchPoseCue(AppleKey, AppleGoalKey)),
                Auto("s2-natural-curve", "Scene 2 自然なカーブ",
                    // From Ver 1.1 on the apple is part of the model.
                    new SetObjectActiveCue(AppleKey, false),
                    Version(VenusAssetBuilder.Ver11Id, LeftArmFocusKey),
                    B("恐らくこんな感じになるかと思われます。"),
                    A("ありがとうございます。そうですね、、、私の最初の案と比べると、より手の形は自然になりましたね\nただ一点気になる点がありまして。。。腕はもっと身体の輪郭に沿って、肘を曲げて引き寄せるような自然なカーブになっていた方が美しく見えると思うんですよね。ほかの古典彫刻でも、腕の自然な曲線が美しさを生んでいます。"),
                    B("なるほど。自然なカーブですか...うーん少しイメージが難しいですね。モデルで表現するとこんな感じでしょうか。")),
                WithCues(Operation("s2-slide-arm", "Scene 2 [B] 腕を内側へ", DemoRole.PlayerB,
                        "空間ペンで左肩から肘の領域を選択し、腕の角度を内側へスライド修正せよ。",
                        WithHint(new SelectionPart("左肩から肘を範囲選択する", LeftUpperArmZoneKey),
                            "光っている左肩から肘までを、トリガーを押したままペン先でなぞってください"),
                        WithHint(SlideArmInwards(),
                            "腕にペン先を当て、トリガーを押したまま体の内側へ回してください")),
                    new SetObjectActiveCue(LeftArmProxyKey, true)),
                Auto("s2-mismatch", "Scene 2 イメージのズレ",
                    new SetObjectActiveCue(LeftArmProxyKey, false),
                    Version(VenusAssetBuilder.Ver12Id, LeftArmFocusKey),
                    A("うーん私のイメージとズレていますね。私のイメージだとこんな感じなんです。")),
                WithCues(Operation("s2-guide-curve", "Scene 2 [A] ガイドでカーブを描く", DemoRole.PlayerA,
                        "空間ペン（ガイドモード）を使用し、左肩から胸の前へ「緩やかなカーブを描く軌跡」をなぞるように描け。",
                        WithHint(StrokePart.Guide("左肩から胸の前へ緩やかなカーブを描く", LeftShoulderZoneKey, ChestFrontZoneKey),
                            "光っている左肩と胸の前を、トリガーを押したまま一筆の線でつないでください")),
                    new SetObjectActiveCue(LeftArmGuideLineKey, true)),
                Auto("s3-hand-mirror", "Scene 3 右腕と手鏡",
                    new SetObjectActiveCue(LeftArmGuideLineKey, false),
                    new GuideSnapCue(LeftArmGuideKey),
                    Version(VenusAssetBuilder.Ver20Id, LeftArmFocusKey),
                    B("あーなるほど。確かにこれは凄く綺麗ですね。力学的にも矛盾点がなさそうです。"),
                    A("左腕は良い感じですね。それでは次に右腕の話に移行しましょう。古典彫刻においては手鏡が林檎と並ぶ代表的なシンボルとされているので、右腕には手鏡があったと考えられます。一度3Dモデルにしてみますね。")),
                Operation(DrawMirrorStepId, "Scene 3 [A] 手鏡の線画を変換", DemoRole.PlayerA,
                    "空間ペンで空中に手鏡の線画を描け。「線画を西洋のアンティークな手鏡に変換」と音声指示せよ。",
                    WithHint(StrokePart.Sketch("空中に手鏡の線画を描く"), "トリガーを押したまま、手のひらほどの大きさで手鏡の形を描いてください"),
                    Voice("「線画を西洋のアンティークな手鏡に変換」と音声指示する", "線画を西洋のアンティークな手鏡に変換")),
                WithCues(Operation("s3-place-mirror", "Scene 3 [A] 手鏡を移動", DemoRole.PlayerA,
                        "生成された手鏡を所定の位置に移動させよ。",
                        WithHint(ManipulatePart.Translate("手鏡を右手の位置へ移動する", HandMirrorKey, MirrorGoalKey, MirrorMaxDistance),
                            "光っている手鏡にペン先を当て、トリガーを押したまま水色の手鏡の位置まで動かしてください")),
                    // The mirror generated from the sketch appears where the sketch was drawn.
                    new PlaceAtSketchCue(HandMirrorKey, MirrorSpawnKey, DrawMirrorStepId, MirrorSpawnMaxOffset)),
                WithCues(Operation("s3-grip-mirror", "Scene 3 [A] 鏡を握らせる", DemoRole.PlayerA,
                        "続けて右肩から手鏡まで領域選択し、「鏡を握らせる」と発話せよ。",
                        WithHint(new SelectionPart("右肩から手鏡までを範囲選択する", RightArmToMirrorZoneKey),
                            "光っている右肩から手鏡までを、トリガーを押したままペン先でなぞってください"),
                        Voice("「鏡を握らせる」と音声指示する", "鏡を握らせる")),
                    // The stage puts the mirror back where it appeared when the step changes; this keeps the result of step 14.
                    new MatchPoseCue(HandMirrorKey, MirrorGoalKey)),
                Auto("s3-mirror-angle", "Scene 3 鏡の角度の指摘",
                    // From Ver 3.0 on the mirror is part of the model.
                    new SetObjectActiveCue(HandMirrorKey, false),
                    Version(VenusAssetBuilder.Ver30Id, RightArmFocusKey),
                    B("なるほど...手鏡を持っているのは納得ですが、生成されたモデルを見ると、手鏡の角度がおかしいかもしれませんね。なんというか、もっとヴィーナスの目線の先に手鏡がくる感じの方が良さそうです。"),
                    A("それはどんな感じでしょうか？ よければBさんに修正をお任せしたいのですが。"),
                    B("承知しました。一旦、手首のひねりを少し調整してみましょう。")),
                WithCues(Operation("s3-adjust-wrist", "Scene 3 [B] 手首の角度を修正", DemoRole.PlayerB,
                        "空間ペンで右手首と手鏡を領域選択し、空間ペンを用いながらヴィーナスの目線に合わせて鏡の角度を修正せよ。",
                        WithHint(new SelectionPart("右手首と手鏡を範囲選択する", RightHandMirrorZoneKey),
                            "光っている右手首と手鏡を、トリガーを押したままペン先でなぞってください"),
                        WithHint(TwistWristToFace(),
                            "手鏡にペン先を当て、トリガーを押したまま水色の手鏡の向きまで回してください")),
                    new SetObjectActiveCue(RightHandProxyKey, true)),
                Auto("p2-review", "Phase 2 振り返りの提案",
                    new SetObjectActiveCue(RightHandProxyKey, false),
                    Version(VenusAssetBuilder.Ver31Id, RightArmFocusKey),
                    A("あー、いいですね！ 視線と所作がぴったり合いました。"),
                    B("では、これまでの過程を振り返ってみましょう。最初の案と、今の案を並べてみてください。")),
                WithCues(Operation("p2-compare", "Phase 2 [B] Ver 1.0 と比較", DemoRole.PlayerB,
                        "履歴一覧からVer 1.0を呼び出し、完成案の隣に並べよ。",
                        History(VenusAssetBuilder.Ver10Id)),
                    new SetObjectActiveCue(ComparisonTableKey, true)),
                Auto("p3-temple-proposal", "Phase 3 神殿での確認",
                    new StatueVersionCue(VenusStatueKeys.Comparison, VenusAssetBuilder.LoadVersion(VenusAssetBuilder.Ver10Id)),
                    A("うーん、やはりこうしてみると現在の案が一番美しく見えますね。"),
                    B("はい、とてもいいかんじです。"),
                    A("最後に復元案のヴィーナスが神殿を背景にしても自然かどうか確かめてみましょう")),
                // The background (the skybox and the lighting) applies to the whole space, so no statue is selected: the
                // voice command generates it, and the background panel, opened on recognition, applies it.
                Operation("p3-temple", "Phase 3 [A] 背景を神殿に", DemoRole.PlayerA,
                    $"「{TempleTranscript}」と音声指示し、背景パネルで神殿を選んで「{BackgroundPanel.ApplyLabel}」を押せ。",
                    Voice($"「{TempleTranscript}」と音声指示する", TempleTranscript),
                    Background(templePreset)),
                new()
                {
                    Id = "closing",
                    Title = "結び",
                    Completion = StepCompletion.Manual,
                    Cues = new List<ScenarioCue>
                    {
                        new EnvironmentCue(templePreset),
                        new SetObjectActiveCue(TempleFloorKey, true),
                        A("おー。現在の案は神殿ともかなりよく調和していますね。"),
                        B("はい、見た目の美しさと構造の自然さが、うまく両立できましたね"),
                        A("Bさんの鋭いご指摘や空間ペンによる修正がヴィーナス像の復元案の構築に有効的でした。ありがとうございます。"),
                        B("こちらこそ本日はどうもありがとうございました。"),
                        N("言葉から生まれた『認識のズレ』について、空間ツールを使って互いのイメージを『すり合わせる』。\nこれが、XRとAIによる新しい共創の形です。文化財復元ワークショップ、これにて終了です"),
                        new WaitCue(1f),
                        new BannerCue(CompletionBannerText, true, 1.5f),
                        new WaitCue(5f),
                        new BannerCue(CompletionBannerText, false, 2f),
                    },
                },
            };
            return steps;
        }

        static ScenarioStep Auto(string id, string title, params ScenarioCue[] cues) => new()
        {
            Id = id,
            Title = title,
            Completion = StepCompletion.CuesFinished,
            Cues = new List<ScenarioCue>(cues),
        };

        static ScenarioStep Operation(string id, string title, DemoRole actor, string taskText, params InteractionPart[] parts) => new()
        {
            Id = id,
            Title = title,
            Actor = actor,
            Completion = StepCompletion.Interaction,
            TaskText = taskText,
            Interaction = new List<InteractionPart>(parts),
        };

        static ScenarioStep WithCues(ScenarioStep step, params ScenarioCue[] cues)
        {
            step.Cues = new List<ScenarioCue>(cues);
            return step;
        }

        static StatueVersionCue Version(string id, string focusKey = null) =>
            new(VenusStatueKeys.Main, VenusAssetBuilder.LoadVersion(id), focusKey);

        /// <summary>Step 9: the arm proxy turns inwards about the shoulder until it passes the threshold.</summary>
        static ManipulatePart SlideArmInwards()
        {
            ManipulatePart part = ManipulatePart.RotatePastThreshold("腕を内側へスライドする", LeftArmProxyKey, LeftShoulderPivotKey,
                Vector3.up, ArmSlideMinAngle, ArmSlideMaxAngle, ArmSlideThresholdAngle);
            part.GrabZoneKey = LeftArmProxyGrabKey;
            return part;
        }

        /// <summary>
        /// Step 17: the hand proxy twists about the forearm (the wrist pivot's up) until the mirror faces the head, where
        /// it snaps; that pose is Ver 3.1's.
        /// </summary>
        static ManipulatePart TwistWristToFace()
        {
            ManipulatePart part = ManipulatePart.RotateToTarget("目線に合わせて鏡の角度を修正する", RightHandProxyKey, RightWristPivotKey,
                Vector3.up,
                Mathf.Min(0f, WristTwistTargetAngle) - WristTwistMargin,
                Mathf.Max(0f, WristTwistTargetAngle) + WristTwistMargin,
                WristTwistTargetAngle, WristTwistSnapAngle);
            part.GrabZoneKey = RightHandProxyGrabKey;
            return part;
        }

        static VoicePart Voice(string instruction, string transcript) => WithHint(new VoicePart(instruction, transcript), VoiceHint);

        /// <summary>Sets the hint text of <paramref name="part"/>, shown after the default delay.</summary>
        static T WithHint<T>(T part, string hint) where T : InteractionPart
        {
            part.Hint = new InteractionHint(hint);
            return part;
        }

        static InteractionPart History(string versionId)
        {
            VenusVersion version = VenusAssetBuilder.LoadVersion(versionId);
            string name = version != null ? version.DisplayName : versionId;
            return WithHint(new HistoryPart(HistoryInstruction(name), version), HistoryHint(name));
        }

        static InteractionPart Background(EnvironmentPreset preset)
        {
            string name = preset != null ? preset.DisplayName : EnvironmentPresetBuilder.TempleDisplayName;
            return WithHint(new BackgroundPart(BackgroundInstruction(name), preset), BackgroundHint(name));
        }

        /// <summary>Task HUD line of the part that applies the generated background <paramref name="presetName"/>.</summary>
        public static string BackgroundInstruction(string presetName) =>
            $"背景パネルで{presetName}を選び、「{BackgroundPanel.ApplyLabel}」を押す";

        /// <summary>Hint of the part that applies the generated background <paramref name="presetName"/>.</summary>
        public static string BackgroundHint(string presetName) =>
            $"背景パネルを閉じたときは、メニューの「{BackgroundMenuProvider.MenuTitle}」で開けます。{presetName}のカードを押し、「{BackgroundPanel.ApplyLabel}」を押してください";

        /// <summary>Task HUD line of the step that calls <paramref name="versionName"/> back from the history.</summary>
        public static string HistoryInstruction(string versionName) =>
            $"メニューの「{HistoryMenuProvider.MenuTitle}」で {versionName} を表示し、「{HistoryPanel.RestoreLabel}」を押す";

        /// <summary>Hint of the step that calls <paramref name="versionName"/> back from the history.</summary>
        public static string HistoryHint(string versionName) =>
            $"メニューの「{HistoryMenuProvider.MenuTitle}」で {versionName} のカードを押し、「{HistoryPanel.RestoreLabel}」を押してください";

        static LineCue N(string text) => new(ScenarioSpeaker.Narrator, text);
        static LineCue A(string text) => new(ScenarioSpeaker.ScholarA, text);
        static LineCue B(string text) => new(ScenarioSpeaker.ScholarB, text);
    }
}
