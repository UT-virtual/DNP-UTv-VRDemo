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

## シーンの編集

シーンファイル（`DNPDemo_Main.unity`）は複数人で同時に編集すると、マージで衝突しやすいファイルです。

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
| 3D モデル | `Art/Models/<名前>/`（例: `Art/Models/Venus/`） |
| マテリアル、テクスチャ | `Art/Materials/`、`Art/Textures/` |
| 音声 | `Audio/Voice/`（台詞）、`Audio/SE/`、`Audio/BGM/` |
| フォント、UI | `UI/Fonts/`、`UI/` |
| プレハブ | `Prefabs/` |
| シナリオ定義などのデータ | `Data/` |

- Blender のファイル（`.blend`）は、Unity に読み込ませないよう、名前が `~` で終わるフォルダ（例: `Art/Models/Venus/Source~/`）に置きます。Unity は `.blend` を Blender 経由で読み込むため、Blender が入っていない PC ではインポートエラーになります。Unity に読み込ませるモデルは FBX で書き出してください。
- Unity が自動で書き換えただけのファイルはコミットしません。例: Basis のフォントアセット（`Poppins-Regular SDF NamePlate.asset` など）、`ProjectSettings/QualitySettings.asset`、`UniversalRenderPipelineGlobalSettings.asset`、`AddressableAssetSettings.asset`。設定を意図して変えた場合だけコミットします。
- `.meta` ファイルは Unity に生成させます。手で作らないでください。アセットを移動するときは、Unity のプロジェクトウィンドウで移動するか、`.meta` ごと移動してください（GUID が保たれ、参照が切れません）。

## Git LFS

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
