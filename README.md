# DNP×UTv VRデモ「ミロのヴィーナス・復元ワークショップ」

Meta Quest 2台を LAN でつなぎ、2人の体験者（プレイヤーA・B）が考古学者と構造力学者の NPC の議論を空間ペンで形にしながら、腕のないミロのヴィーナスを VR 空間で復元していくデモです。

Built with [Basis](https://github.com/BasisVR/basis)

## 動作環境

| 項目 | 内容 |
|---|---|
| Unity | 6000.5.10f1（URP / OpenXR） |
| ターゲット | Meta Quest 2 / 3 / 3S / Pro（Android, IL2CPP, ARM64, Vulkan） |
| 人数 | 2人（プレイヤーA がホスト、プレイヤーB が参加） |
| ネットワーク | Quest 2台が同じ LAN 内で端末同士通信できること（ルーターの AP アイソレーションは無効）。UDP 4296 |

## セットアップ

> 注意: 仮素材の期間は、バイナリを Git LFS ではなく通常の git で管理しています（詳細は #60）。

1. [Git LFS](https://git-lfs.com) をインストールしてから、リポジトリを clone する。
2. リポジトリのルートで `sh tools/setup-git.sh` を実行する（Git LFS と UnityYAMLMerge の設定。詳しくは [CONTRIBUTING.md](CONTRIBUTING.md)）。
3. Unity で開く前に、[効果音（SE）の配置](#効果音seの配置) の手順で効果音を置く。
4. Unity Hub で `Basis/` フォルダをプロジェクトとして開く（Unity 6000.5.10f1）。
5. メニュー `DNP Demo > Setup > Run All` を実行する。Player Settings（アプリ名、Android のパッケージ名 `com.dnputv.vrdemo`）とビルドシーンの順序が設定されます。
6. Build Profiles でプラットフォームを Android に切り替え、APK をビルドする。
7. 次の節の手順で Quest にインストールする。

ビルドの先頭シーンは `Assets/DNPDemo/Scenes/DNPDemo_Main.unity` です。

## 効果音（SE）の配置

効果音の素材は再配布が禁止されているため、mp3 はリポジトリに含めていません（`.gitignore` で除外し、`.meta` だけを管理しています）。各自が配布元のサイトから入手して、`Basis/Assets/DNPDemo/Audio/SE/` に次のファイル名で置いてください。出典と利用規約は [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) にあります。

| 配置するファイル名 | 用途 | 元ファイル名 | 配布元 |
|---|---|---|---|
| `se_generate.mp3` | 生成 | ゲージ回復2.mp3 | 効果音ラボ |
| `se_morph.mp3` | 変形 | データ表示4.mp3 | 効果音ラボ |
| `se_select_confirm.mp3` | 選択の確定 | 決定ボタンを押す35.mp3 | 効果音ラボ |
| `se_snap.mp3` | スナップ | ビデオディスクをケースから出す.mp3 | 効果音ラボ |
| `se_voice_start.mp3` | 音声入力の開始 | maou_se_system35.mp3 | 魔王魂 |
| `se_voice_end.mp3` | 音声入力の終了 | maou_se_system35.mp3 | 魔王魂 |
| `se_task_show.mp3` | タスク表示 | データ表示2.mp3 | 効果音ラボ |
| `se_complete.mp3` | 完了 | 成功音.mp3 | 効果音ラボ |
| `se_fail.mp3` | 失敗 | ビープ音4.mp3 | 効果音ラボ |

`se_voice_start.mp3` と `se_voice_end.mp3` は、同じ maou_se_system35.mp3 を2つの名前でコピーします。

- mp3 を置いてから Unity を開いてください。mp3 がないまま開くと、対応するファイルのない `.meta` が Unity に削除されることがあります（未確認）。削除された `.meta` をコミットすると GUID が変わり、シーンや Prefab からの参照が切れます。削除されてしまったときは、その削除をコミットせず、mp3 を置き直してください。`.meta` をコミット済みであれば、`git restore` で戻せます。
- 配置したあとに Unity を開き、9件の `.meta` が生成される（またはリポジトリの `.meta` が使われる）ことを確認してください。`.meta` は手で作りません。
- ビルドの前に9件そろっていることを確認してください。足りない SE は APK に入らず、その場面では無音になります。

## Quest へのインストール

アプリはマイクを使います（音声指示）。インストールしたあとにマイクの権限を許可しておかないと、初めて起動したときに、ロビーの上に権限のダイアログが出ます。体験者が操作しなくて済むよう、インストールのたびに次の手順で許可しておいてください。

PC と Quest を USB でつなぎ、PowerShell で実行します。

1. APK をインストールする。

   ```powershell
   adb install -r <APK のパス>
   ```

2. マイクの権限を許可する。

   ```powershell
   adb shell pm grant com.dnputv.vrdemo android.permission.RECORD_AUDIO
   ```

3. 許可されたことを確かめる。`granted=true` と表示されれば完了です。

   ```powershell
   adb shell dumpsys package com.dnputv.vrdemo | Select-String "RECORD_AUDIO"
   ```

アンインストールすると許可も消えます。入れ直したときは、もう一度 2 から実行してください。

## 体験の流れ

1. 2台の Quest でアプリを起動する。
2. プレイヤーA がホストを開始し、プレイヤーB がホストに接続する。
3. 2人がそろうとシナリオが始まります。HUD に表示される操作（空間ペンによる範囲選択、線描、部位の移動、音声指示、履歴の呼び出しなど）を終えるごとに、像が次の版へ変わり、次の場面へ進みます。

各ステップの内容と進行条件、開発の進み具合は [docs/PLAN.md](docs/PLAN.md) にまとめています。

## ディレクトリ構成

```
DNP_UTv_VRDemo/
├─ Basis/                  Unity プロジェクト（Unity Hub で開く）
│  └─ Assets/DNPDemo/      本デモのコードとアセット一式
├─ Basis Server/           Basis 標準の専用サーバー（リモート接続に対応するときに使う）
└─ docs/                   開発プランと改造記録
```

本デモ独自のコードとアセットは、すべて `Basis/Assets/DNPDemo/` 以下に置きます。

## ドキュメント

- [CONTRIBUTING.md](CONTRIBUTING.md): 開発ルール（セットアップ、ブランチと PR、コミットメッセージ、テスト、アセットの置き場所）
- [docs/PLAN.md](docs/PLAN.md): 開発プラン（技術方針、アーキテクチャ、ステップ定義、マイルストーン）
- [docs/BASIS_PATCHES.md](docs/BASIS_PATCHES.md): Basis 本体への改造記録
- [docs/SCENARIO.md](docs/SCENARIO.md): シナリオ

## Basis との関係

このリポジトリは [BasisVR/basis](https://github.com/BasisVR/basis) の `long-term-support-20260916` ブランチをベースにしています。本家の更新は git remote `upstream` から取り込みます。

`Basis/Assets/DNPDemo/` 以外のファイル（Basis 本体）を変更したときは、upstream を merge するときに競合を確認できるよう、[docs/BASIS_PATCHES.md](docs/BASIS_PATCHES.md) に記録してください。

本プロジェクトは Basis Project の公式プロジェクトではなく、Basis Project による承認も受けていません。

## ライセンス

- Basis 由来の部分: MIT License（[LICENSE](LICENSE)）
- 本デモ独自の部分（`Basis/Assets/DNPDemo/` 以下のコードとアセット）: © 2026 UT-Virtual. All rights reserved. 無断での複製・改変・再配布を禁じます。
- サードパーティのソフトウェア: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)

"Basis"、"BasisVR"、"Basis Framework" の名称と Basis のロゴは Basis Project の商標です。使用については [TRADEMARK.md](TRADEMARK.md) に従ってください。
