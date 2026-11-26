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
3. Unity Hub で `Basis/` フォルダをプロジェクトとして開く（Unity 6000.5.10f1）。
4. メニュー `DNP Demo > Setup > Run All` を実行する。Player Settings（アプリ名、Android のパッケージ名 `com.dnputv.vrdemo`）とビルドシーンの順序が設定されます。
5. Build Profiles でプラットフォームを Android に切り替える。
6. 次の節の手順で APK をビルドし、Quest にインストールする。

ビルドの先頭シーンは `Assets/DNPDemo/Scenes/DNPDemo_Main.unity` です。

## リリース APK のビルド

展示で使う APK は、メニュー `DNP Demo > Build > Release APK` でビルドします。セットアップ（`DNP Demo > Setup > Run All`）の適用、Development Build の解除、出力先の決定までを一度に行います。

- 出力先: `Builds/Release/DNPDemo_<日時>-<コミット>.apk`（例: `DNPDemo_20261125-1830-0861ec3.apk`）
- コミットしていない変更があると確認のダイアログが出ます。どのコードからビルドしたかを APK の名前で追えるよう、コミットしてからビルドしてください。
- ビルドが終わると、続けて USB でつないでいる Quest すべてにインストールします（次の節のインストールスクリプトを実行します）。進み具合は Console に出て、終わると結果がダイアログで表示されます。Quest をつないでいなければ、インストールは行いません。

Unity を開かずにバッチモードでもビルドできます（Unity を閉じてから実行します）。バッチモードではビルドだけ行います。

```powershell
& "<Unity.exe のパス>" -batchmode -quit -projectPath Basis -buildTarget Android -executeMethod DNPDemo.Editor.DemoReleaseBuild.BuildFromCommandLine -logFile Logs/release-build.log
```

### 署名

署名用のキーストアは作らず、ビルドした PC の debug 鍵（`%USERPROFILE%\.android\debug.keystore`）で署名します。そのため、リリース APK は決めた1台の PC（ビルド担当 PC）でビルドしてください。

Android は、入っているアプリと署名が同じ APK しか上書きインストールしません。別の PC でビルドした APK を入れるときは、一度アンインストールする必要があります。次の節のインストールスクリプトは、署名の違いを検出すると、自動でアンインストールしてから入れ直します。

### バージョン

- versionCode は 300 に固定しています（`DemoBuildVersionPolicy`）。どのビルドも、入っているアプリに上書きインストールできます。
- versionName は、リリース APK ではファイル名と同じ `<日時>-<コミット>` になります。Quest に入っているビルドは、次のコマンドで確かめられます。

  ```powershell
  adb shell dumpsys package com.dnputv.vrdemo | Select-String "versionName"
  ```

## Quest へのインストール

アプリはマイクを使います（音声指示）。インストールしたあとにマイクの権限を許可しておかないと、初めて起動したときに、ロビーの上に権限のダイアログが出ます。体験者が操作しなくて済むよう、インストールスクリプトでインストールと権限の許可をまとめて行います。メニューからビルドしたときはこのスクリプトが自動で実行されるので、次の手順は、ビルド済みの APK を入れるときに使います。

事前に、Quest で開発者モードを有効にしておいてください（Meta Horizon アプリから設定します）。

1. PC と Quest を USB でつなぐ。初めてつないだ PC では、ヘッドセットの中に USB デバッグを許可するダイアログが出るので、許可する。2台同時につないでもかまいません。
2. PowerShell で、リポジトリのルートから実行する。

   ```powershell
   powershell -ExecutionPolicy Bypass -File tools\install-quest.ps1 <APK のパス>
   ```

3. Quest ごとに `完了しました（versionName …、マイクの権限を許可済み）` と表示されれば完了です。

スクリプトは、つないだすべての Quest に対して、インストール（署名が違えばアンインストールしてから入れ直す）、マイクの権限の許可、許可されたことの確認を行います。

アンインストールすると権限の許可も消えます。手でアンインストールしたときも、このスクリプトで入れ直してください。

## 体験の流れ

1. 2台の Quest でアプリを起動する。
2. プレイヤーA がホストを開始し、プレイヤーB がホストに接続する。
3. 2人がそろうとシナリオが始まります。HUD に表示される操作（空間ペンによる範囲選択、線描、部位の移動、音声指示、履歴の呼び出しなど）を終えるごとに、像が次の版へ変わり、次の場面へ進みます。

各ステップの内容と進行条件、開発の進み具合は [docs/PLAN.md](docs/PLAN.md) にまとめています。

## 運営の操作

運営パネルは、左右のスティックを同時に2秒押し込むと目の前に開きます（エディタとデスクトップでは F9）。どちらの端末からでも操作できます。

次の体験者に交代するとき:

1. 「WORKSHOP COMPLETE」が消えたら、運営パネルの「ロビーへ戻す（次の体験者へ）」を押す。像や背景が初期状態に戻り、2人ともそれぞれの開始位置へ移動し、ペンは手から離れて元の位置に戻ります（モードも範囲選択に戻る）。
2. 次の体験者がヘッドセットを着けたら、「次へ ▶」で開始する。

接続が切れたとき:

- プレイヤーB の接続が切れると、Basis が自動で再接続を試みます。プレイヤーA 側では、台詞が終わっても次へ進まずに待ち、B が戻ると現在のステップを2人とも頭から再生します。再接続できなかったときや、B のアプリを再起動したときは、ロビーで「プレイヤーB（参加）」を選び直すと、進行中のステップに合流します。
- プレイヤーA のアプリを再起動したときは、もう一度ホストを開始します。進行はロビーから始まり、運営パネルに「中断したステップ N から再開」が出ます。B が接続したら、このボタンで中断したステップへ戻ります（2人がそろっても自動では始まりません。最初からやり直すときは「次へ ▶」）。
- どのステップでも、運営パネルの「◀ ▶」でステップを選んで「移動」を押すと、そのステップへ移ります。

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
