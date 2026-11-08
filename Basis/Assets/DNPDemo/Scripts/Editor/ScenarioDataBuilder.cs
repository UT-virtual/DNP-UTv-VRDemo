using System.Collections.Generic;
using System.IO;
using DNPDemo.Scenario;
using DNPDemo.Scenario.Cues;
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

        /// <summary>Key of the statue spotlight in <see cref="ScenarioStage"/>.</summary>
        public const string SpotlightKey = "Spotlight";

        public const string CompletionBannerText = "WORKSHOP COMPLETE";

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
            var data = ScriptableObject.CreateInstance<ScenarioData>();
            data.SetSteps(CreateDefaultSteps());
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
            AssetDatabase.CreateAsset(data, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DNPDemo] Created {AssetPath}");
            return data;
        }

        public static List<ScenarioStep> CreateDefaultSteps()
        {
            return new List<ScenarioStep>
            {
                new()
                {
                    Id = "lobby",
                    Title = "ロビー（接続待ち）",
                    Completion = StepCompletion.PlayersReady,
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
                    "音声生成を起動し、「現在のヴィーナスについて、手のひらに林檎がのった水平に伸びる左手を追加」と出力せよ。"),
                Auto("s1-concerns", "Scene 1 構造の懸念",
                    A("私が考える、ヴィーナスの左手のイメージはまさにこんな感じですね。"),
                    B("ありがとうございます。うーむ...私の専門の構造力学的な観点からすると、腕をここまで水平に伸ばすことは付け根への大きな負担に繋がりそうです。ヴィーナスのような大事な像を昔の職人が、不安定な構造で作るとはとても思えないんですよね..."),
                    A("なるほど。他に懸念点などはございませんか？"),
                    B("そうですね...私的にはヴィーナスの林檎の持ち方も気になっています。残存しているヴィーナスの脇の部分と、現在の林檎の持ち方があまり一致しているように思えないんですよね。Ａ先生のイメージですと林檎を手のひらの上にのせていますが、ボールを投げる時のように、上から林檎を包み込むような持ち方の方が脇のあたりの筋肉の動きと自然につながると考えやすいんですよ。")),
                Auto("s2-request-b", "Scene 2 学者Bへの依頼",
                    A("なるほど..構造の面から見ると様々な問題がありそうですね。よろしければ、B先生が考える力学的に安定的で、筋肉の動きにも矛盾がないアイデアを3Dモデルで出していただけないでしょうか？"),
                    B("そうですね、一度私の考えもモデル化してみましょう。")),
                Operation("s2-move-apple", "Scene 2 [B] 林檎を手の下へ", DemoRole.PlayerB,
                    "空間ペンで林檎の領域を選択し、林檎の位置を調整せよ。（手の下に持ってくる）"),
                Operation("s2-regrip-apple", "Scene 2 [B] 林檎の持ち方を修正", DemoRole.PlayerB,
                    "空間ペンで腕を選択し、「選択した範囲について、上からボールを包み込むように林檎を持ち直す形に修正」と入力せよ。"),
                Auto("s2-natural-curve", "Scene 2 自然なカーブ",
                    B("恐らくこんな感じになるかと思われます。"),
                    A("ありがとうございます。そうですね、、、私の最初の案と比べると、より手の形は自然になりましたね\nただ一点気になる点がありまして。。。腕はもっと身体の輪郭に沿って、肘を曲げて引き寄せるような自然なカーブになっていた方が美しく見えると思うんですよね。ほかの古典彫刻でも、腕の自然な曲線が美しさを生んでいます。"),
                    B("なるほど。自然なカーブですか...うーん少しイメージが難しいですね。モデルで表現するとこんな感じでしょうか。")),
                Operation("s2-slide-arm", "Scene 2 [B] 腕を内側へ", DemoRole.PlayerB,
                    "空間ペンで左肩から肘の領域を選択し、腕の角度を内側へスライド修正せよ。"),
                Auto("s2-mismatch", "Scene 2 イメージのズレ",
                    A("うーん私のイメージとズレていますね。私のイメージだとこんな感じなんです。")),
                Operation("s2-guide-curve", "Scene 2 [A] ガイドでカーブを描く", DemoRole.PlayerA,
                    "空間ペン（ガイドモード）を使用し、左肩から胸の前へ「緩やかなカーブを描く軌跡」をなぞるように描け。"),
                Auto("s3-hand-mirror", "Scene 3 右腕と手鏡",
                    B("あーなるほど。確かにこれは凄く綺麗ですね。力学的にも矛盾点がなさそうです。"),
                    A("左腕は良い感じですね。それでは次に右腕の話に移行しましょう。古典彫刻においては手鏡が林檎と並ぶ代表的なシンボルとされているので、右腕には手鏡があったと考えられます。一度3Dモデルにしてみますね。")),
                Operation("s3-draw-mirror", "Scene 3 [A] 手鏡の線画を変換", DemoRole.PlayerA,
                    "空間ペンで空中に手鏡の線画を描け。「線画を西洋のアンティークな手鏡に変換」と音声指示せよ。"),
                Operation("s3-place-mirror", "Scene 3 [A] 手鏡を移動", DemoRole.PlayerA,
                    "生成された手鏡を所定の位置に移動させよ。"),
                Operation("s3-grip-mirror", "Scene 3 [A] 鏡を握らせる", DemoRole.PlayerA,
                    "続けて右肩から手鏡まで領域選択し、「鏡を握らせる」と発話せよ。"),
                Auto("s3-mirror-angle", "Scene 3 鏡の角度の指摘",
                    B("なるほど...手鏡を持っているのは納得ですが、生成されたモデルを見ると、手鏡の角度がおかしいかもしれませんね。なんというか、もっとヴィーナスの目線の先に手鏡がくる感じの方が良さそうです。"),
                    A("えーと、どんな感じでしょう？ よければBさんに修正をお任せしたいのですが。"),
                    B("承知しました。一旦、手首のひねりを少し調整してみましょう。")),
                Operation("s3-adjust-wrist", "Scene 3 [B] 手首の角度を修正", DemoRole.PlayerB,
                    "空間ペンで右手首と手鏡を領域選択し、空間ペンを用いながらヴィーナスの目線に合わせて鏡の角度を修正せよ。"),
                Auto("p2-review", "Phase 2 振り返りの提案",
                    A("あー、いいですね！ 視線と所作がぴったり合いました。"),
                    B("では、これまでの過程を振り返ってみましょう。最初の案と、今の案を並べてみてください。")),
                Operation("p2-compare", "Phase 2 [B] Ver 1.0 と比較", DemoRole.PlayerB,
                    "履歴一覧からVer 1.0を呼び出し、完成案の隣に並べよ。"),
                Auto("p3-temple-proposal", "Phase 3 神殿での確認",
                    A("うーん、やはりこうしてみると現在の案が一番美しく見えますね。"),
                    B("ええ、とてもいいかんじです。"),
                    A("最後に復元案のヴィーナスが神殿を背景にしても自然かどうか確かめてみましょう")),
                Operation("p3-temple", "Phase 3 [A] 背景を神殿に", DemoRole.PlayerA,
                    "復元案のヴィーナス像を領域選択。「選択した像の背景を神殿に変更」と音声指示せよ。"),
                new()
                {
                    Id = "closing",
                    Title = "結び",
                    Completion = StepCompletion.Manual,
                    Cues = new List<ScenarioCue>
                    {
                        A("おー。現在の案は神殿ともかなりよく調和していますね。"),
                        B("ええ、見た目の美しさと構造の自然さが、うまく両立できましたね"),
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
        }

        static ScenarioStep Auto(string id, string title, params ScenarioCue[] cues) => new()
        {
            Id = id,
            Title = title,
            Completion = StepCompletion.CuesFinished,
            Cues = new List<ScenarioCue>(cues),
        };

        static ScenarioStep Operation(string id, string title, DemoRole actor, string taskText) => new()
        {
            Id = id,
            Title = title,
            Actor = actor,
            Completion = StepCompletion.Interaction,
            TaskText = taskText,
        };

        static LineCue N(string text) => new(ScenarioSpeaker.Narrator, text);
        static LineCue A(string text) => new(ScenarioSpeaker.ScholarA, text);
        static LineCue B(string text) => new(ScenarioSpeaker.ScholarB, text);
    }
}
