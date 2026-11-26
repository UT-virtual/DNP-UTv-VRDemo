# 開発ルール

このリポジトリで作業するときの手順と決まりごとです。プロジェクトの概要は [README.md](README.md)、設計は [docs/PLAN.md](docs/PLAN.md) を参照してください。

## セットアップ

1. [Git LFS](https://git-lfs.com) をインストールする。
2. リポジトリを clone する。
3. Git Bash（Windows）またはターミナル（macOS）で、リポジトリのルートから次を実行する。

   ```sh
   sh tools/setup-git.sh
   ```

   このスクリプトは次の2つを行います。
   - Git LFS のフックをこの clone に入れ、LFS のファイルをダウンロードする。
   - Unity 付属の UnityYAMLMerge を、git の mergetool `unityyamlmerge` として登録する（このリポジトリだけの設定）。

   UnityYAMLMerge は、`Basis/ProjectSettings/ProjectVersion.txt` に書かれたバージョンの Unity を、Unity Hub のインストール先から探します。見つからないときは、パスを指定して実行してください。

   ```sh
   sh tools/setup-git.sh --yaml-merge "F:/Unity/6000.5.10f1/Editor/Data/Tools/UnityYAMLMerge.exe"
   ```

4. Unity Hub で `Basis/` フォルダを開き、[README.md](README.md) のセットアップを続ける。

## ブランチと PR

- main には直接 push せず、ブランチを切って PR を作ります。レビューは任意です。見てほしい点があればレビューを依頼してください。
- ブランチ名は `<種類>/issue-<番号>-<概要>` にします。種類はコミットメッセージと同じものを使います。
  - 例: `feat/issue-9-detection-zone`、`chore/issue-1-repository-ops`
  - Issue がない作業は `<種類>/<概要>` にします。
- PR の本文はテンプレートに沿って書き、必須チェックをすべて埋めてください。埋まっていないと PR Checklist のチェックが失敗します。
- Issue を閉じる PR には、本文に `Closes #番号` を書きます。

## コミットメッセージ

[Conventional Commits](https://www.conventionalcommits.org/ja/v1.0.0/) に従います。

```
<種類>(<スコープ>): <説明>

<本文（任意）: 何を、なぜ変えたか>
```

| 種類 | 使う場面 |
|---|---|
| `feat` | 機能の追加 |
| `fix` | 不具合の修正 |
| `refactor` | 動作を変えないコードの整理 |
| `test` | テストの追加・修正 |
| `docs` | ドキュメントだけの変更 |
| `chore` | 設定、依存関係、アセットの整理など |

- スコープは任意です。`scenario`、`lobby`、`font`、`deps` のように、変更した領域を書きます。
- 説明と本文は日本語で書きます。
- 例: `feat(scenario): シナリオエンジンの基盤を実装`

## Editor テスト

`Basis/Assets/DNPDemo/Tests/Editor/`（アセンブリ `DNPDemo.Tests.Editor`）に Editor テストがあります。PR を出す前に、すべて通ることを確認してください。

エディタで実行する場合:

1. メニュー `Window > General > Test Runner` を開く。
2. `EditMode` タブで `DNPDemo.Tests.Editor` を選び、`Run Selected` を押す。

コマンドラインで実行する場合（PowerShell、リポジトリのルートから。プロジェクトをエディタで開いている間は実行できません）:

```powershell
& "<Unity のインストール先>\6000.5.10f1\Editor\Unity.exe" -batchmode -projectPath Basis `
  -runTests -testPlatform EditMode -assemblyNames DNPDemo.Tests.Editor `
  -testResults "$PWD\Logs\editmode-results.xml" -logFile "$PWD\Logs\editmode.log"
```

結果は `Logs/editmode-results.xml` に出力されます（`Logs/` は git の追跡対象外です）。

## 起動時のダイアログの確認

Basis を更新したときや、権限を使う機能を足したときは、新規インストールした Quest で、起動からロビーまでに体験者が操作するダイアログが出ないことを確かめてください。

Development Build の APK では、`DialogProbe`（`Basis/Assets/DNPDemo/Scripts/Runtime/Diagnostics/`）が、Basis のパネルの開閉、通知一覧に回されたポップアップ、フォーカスの出入り、権限の状態をログに出します。Android の権限ダイアログは Unity からは見えないので、フォーカスを失った時刻と、権限の状態の変化から読み取ります。

PowerShell で、リポジトリのルートから実行します。

1. Build Profiles で Development Build にチェックを入れて、APK をビルドする。
2. アプリをアンインストールしてから（権限の許可も消えます）、`-g` を付けずにインストールする。

   ```powershell
   adb uninstall com.dnputv.vrdemo
   adb install <APK のパス>
   ```

3. ログを消してから記録を始める。1 行目は、日本語の文字化けを防ぐ設定です。

   ```powershell
   [Console]::OutputEncoding = [Text.Encoding]::UTF8
   adb logcat -c
   adb logcat -s Unity | Select-String DialogProbe | Tee-Object -FilePath Logs\dialogprobe.txt
   ```

4. Quest でアプリを起動し、ロビーが出るまで何も操作しない。そのあと、出たダイアログに一つずつ応え、ロビーが出てから 30 秒ほど記録してから Ctrl+C で止める。

ログの読み方:

- `panel opened`：Basis のパネル（ダイアログを含む）が開いた。
- `notification`：ポップアップが画面に出ず、通知一覧に回された。
- `focus lost` から `focus regained` まで：システムのダイアログや画面が前に出ていた。その間に `permission granted` が出ていれば、権限ダイアログで許可された。
- `lobby visible`：ロビーが表示された。

今わかっているダイアログは、マイクの権限ダイアログだけです（[docs/PLAN.md](docs/PLAN.md) §2.1）。ほかのダイアログが見つかったら、Issue を作って対処してください。

## シーンとプレハブの構成

`DNPDemo_Main.unity` は、機能ごとのプレハブを並べただけの構成です。普段の作業では、シーンではなく該当するプレハブを編集してください（プレハブをダブルクリックして開くか、シーン上のインスタンスで編集して Overrides から Apply する）。

| シーンのルート | プレハブ（`Basis/Assets/DNPDemo/Prefabs/`） | 内容 |
|---|---|---|
| BasisScene | （シーンに直接置く） | Basis のシーン定義、初期スポーン位置、カメラ |
| Env_ExhibitionRoom | `Env_ExhibitionRoom.prefab` | 展示室。見た目は `Model`、床と壁の当たり判定は `Colliders` に分けてある。モデルを差し替えたら `Colliders` を見た目に合わせる。キー `ExhibitionRoom` を持ち、環境が展示室を隠すときは `EnvironmentController` が配下の Renderer をフェードする（当たり判定は残る） |
| Env_Temple | `Env_Temple.prefab` | 神殿（最初は非表示） |
| Lighting | `Lighting.prefab` | ライト。像のスポットライトはロビー中は消えている。ルートにキー `Environment` と `EnvironmentController` を持ち、環境プリセットに合わせてライト、Skybox、Ambient、Reflection を切り替える |
| Statue_Original | `Statue_Original.prefab` | 台座と像 |
| ComparisonTable | `ComparisonTable.prefab` | 比較台（最初は非表示） |
| Scholars | `Scholars.prefab` | 学者 A・B。どちらも `Scholars/Scholar.prefab` のバリアント |
| DemoSystems | `DemoSystems.prefab` | 接続、ロール、スポーン、ロビーパネル、シナリオ（`Scenario`） |

神殿の背景は、環境プリセット（`Data/Environment/EnvironmentPreset_Temple.asset`）のパノラマとライトで表示していて、`Env_Temple` は使っていません。`Env_Temple.prefab` は今は中身のない置き場で、シーンでは非表示のままです。神殿の柱などの立体物を足すときはこのプレハブに置き、キー（`ScenarioObjectBinding`）を付けて、結びのステップ（`closing`）の `SetObjectActiveCue` で表示してください。`EnvironmentController` は `Env_Temple` を操作しないので、表示の切り替えはシナリオのデータで行います。

- シナリオのデータ（`Data/ScenarioData.asset`）はシーンのオブジェクトを直接参照できません。キューから操作したいオブジェクトには `ScenarioObjectBinding`（キー）を、台詞を話すオブジェクトには `ScenarioSpeakerBinding`（話者、音声、字幕）を、そのオブジェクトのプレハブに付けてください。ScenarioStage が起動時にシーンから集めます。
- データが使う話者とキーがシーンにそろっているか、重複がないかは、Editor テスト `ScenarioSceneBindingTests` で確認できます。
- プレハブを新しく足すときだけ、シーンの編集が必要です。

## シーンの編集

シーンファイル（`DNPDemo_Main.unity`）は複数人で同時に編集すると、マージで衝突しやすいファイルです。上の表のとおり、ほとんどの作業はプレハブの編集で済みます。

- 編集を始める前に、チームのチャットで「DNPDemo_Main を編集します」と伝えてください。同時に編集するのは1人だけにします。
- 編集が終わったら、push してチャットで伝えてください。
- 衝突したときは、UnityYAMLMerge で解決します（次の節）。

## マージの衝突

シーン、プレハブ、マテリアルなど Unity の YAML ファイルが衝突したときは、UnityYAMLMerge で解決します。UnityYAMLMerge は Unity のオブジェクト構造を理解して、同じオブジェクトの別のプロパティへの変更などを自動でまとめます。

```sh
git mergetool --tool=unityyamlmerge -- <衝突したファイル>
```

自動で解決できなかった箇所が残った場合は、そのファイルを編集した人と相談して解決してください。

## アセットの置き場所

本デモのアセットは、すべて `Basis/Assets/DNPDemo/` 以下に置きます。

| 種類 | 置き場所 |
|---|---|
| 3D モデル、マテリアル、テクスチャ | `Art/<機能>/Models/`、`Art/<機能>/Materials/`、`Art/<機能>/Textures/` |
| 音声 | `Audio/Voice/`（台詞）、`Audio/SE/`、`Audio/BGM/` |
| フォント、UI | `UI/Fonts/`、`UI/` |
| プレハブ | `Prefabs/` |
| シーン | `Scenes/` |
| シナリオ定義などのデータ | `Data/` |

- `Art/` は、機能ごとのフォルダに分けてから、種類（Models、Materials、Textures）で分けます。機能のフォルダには `ExhibitionRoom`（展示室）、`Venus`（像と台座）、`Scholars`（学者）などがあります。新しい機能の素材は、同じ形でフォルダを作って置いてください。
- 複数の機能で使う素材は `Art/Common/` に置きます。
- Blender のファイル（`.blend`）は、Unity に読み込ませないよう、名前が `~` で終わるフォルダ（例: `Art/Venus/Models/Source~/`）に置きます。Unity は `.blend` を Blender 経由で読み込むため、Blender が入っていない PC ではインポートエラーになります。Unity に読み込ませるモデルは FBX で書き出してください。
- VRM のファイル（`.vrm`）も、`~` で終わるフォルダ（例: `Art/Assistants/Source~/`）に置きます。UniVRM は `.vrm` がインポートされるたびに同じフォルダの `<名前>.prefab` を作り直すので、`Assets` 以下に置いたままだと、clone した PC で開いたときにプレハブへ加えた Basis の設定が消えます。コミットするのは生成されたプレハブとアセットです。VRM を差し替えるときは、`.vrm` を一時的にプレハブと同じフォルダへコピーしてプレハブを作り直し、ルートに `BasisAvatar` を付けて設定し、VRM のコンポーネント（`VRMSpringBone`、`VRMBlendShapeProxy` など）を外してから、`.vrm` を `Source~/` に戻します。VRM のコンポーネントは Basis が読み込むときに消してしまうためです。設定漏れは Editor テスト `AssistantAvatarPrefabTests` が検出します。
- UI の日本語フォント（`UI/Fonts/NotoSansJP-Regular SDF.asset`）は、文字セット `UI/Fonts/JapaneseCharacterSet-3353.txt` から作った静的な SDF です。セットにない文字は □ で表示されます。UI や台詞に新しい文字や記号を使うときは、文字セットに加えてから SDF を作り直してください（フォントアセットを選び、インスペクターの Update Atlas Texture → Generate Font Atlas → Save）。作り直し忘れは Editor テスト `UiFontTests` が検出します。作成設定の Get Font Features（`includeFontFeatures`）はオフにしています。この SDF ではカーニングが元のフォントの約 12 倍の値で取り込まれ、「Ver」の V と e が重なるためです。
- Unity が自動で書き換えただけのファイルはコミットしません。例: Basis のフォントアセット（`Poppins-Regular SDF NamePlate.asset` など）、`ProjectSettings/QualitySettings.asset`、`UniversalRenderPipelineGlobalSettings.asset`、`AddressableAssetSettings.asset`。設定を意図して変えた場合だけコミットします。
- `.meta` ファイルは Unity に生成させます。手で作らないでください。アセットを移動するときは、Unity のプロジェクトウィンドウで移動するか、`.meta` ごと移動してください（GUID が保たれ、参照が切れません）。
- SE の mp3（`Audio/SE/`）はリポジトリに含めず、`.meta` だけを管理します。素材の利用規約で再配布が禁止されているためです。配置手順は [README.md](README.md#効果音seの配置) にあります。

## Git LFS

> 注意: 仮素材の期間は、`Basis/Assets/DNPDemo/` のバイナリも通常の git で管理しています。以下の LFS の手順は現在使っていません（詳細は #60）。

`Basis/Assets/DNPDemo/` 以下のバイナリ（3D モデル、画像、音声、動画、フォント、ライトマップなど）は Git LFS で管理します。対象の拡張子は [.gitattributes](.gitattributes) にあります。

- セットアップを済ませていれば、普段どおり `git add` するだけで LFS に入ります。`git lfs ls-files` で、LFS に入ったファイルを確認できます。
- LFS を通さずにコミットされたバイナリがあると、PR の LFS Check が失敗します。その場合は、次のコマンドで LFS に入れ直してコミットしてください。

  ```sh
  git add --renormalize Basis/Assets/DNPDemo
  ```

- Basis 本体（`Basis/Packages/` など）は LFS の対象外です。upstream をマージしたときに、LFS を通していないバイナリが混ざらないようにするためです。

### LFS 導入前に作ったアセットのブランチを取り込む

LFS 導入前のブランチをそのままマージすると、LFS を通っていないバイナリが main の履歴に入ります。次の手順で、最新の main から新しいブランチを作り、アセットだけを移してください。

```sh
git fetch origin
git switch -c feat/issue-<番号>-<概要> origin/main

# 古いブランチから、アセットのフォルダだけを取り出す（.meta も一緒に取り出される）
git restore --source=origin/<古いブランチ> -- <アセットのフォルダ> <アセットのフォルダ>.meta
```

アセットが `Basis/Assets/DNPDemo/` の外にある場合は、Unity を開く前に、フォルダを `.meta` ごと `Basis/Assets/DNPDemo/` 以下の置き場所へ移します。そのうえで、次のように追加します。

```sh
git add Basis/Assets/DNPDemo
git lfs status    # バイナリが「LFS」として追加されていることを確認する
git commit
git push -u origin HEAD
```

PR がマージされたら、古いブランチは削除します。
