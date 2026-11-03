# DNP×UTv VRデモ「ミロのヴィーナス・復元ワークショップ」

Meta Quest 2台を LAN でつなぎ、2人の体験者（助手A・助手B）が学者 NPC の指示を受けながら、腕のないミロのヴィーナスを VR 空間で復元していくデモです。

Built with [Basis](https://github.com/BasisVR/basis)

## 動作環境

| 項目 | 内容 |
|---|---|
| Unity | 6000.5.10f1（URP / OpenXR） |
| ターゲット | Meta Quest 2 / 3 / 3S / Pro（Android, IL2CPP, ARM64, Vulkan） |
| 人数 | 2人（助手A がホスト、助手B が参加） |
| ネットワーク | Quest 2台が同じ LAN 内で端末同士通信できること（ルーターの AP アイソレーションは無効）。UDP 4296 |

## セットアップ

1. リポジトリを clone する。
2. Unity Hub で `Basis/` フォルダをプロジェクトとして開く（Unity 6000.5.10f1）。
3. メニュー `DNP Demo > Setup > Run All` を実行する。Player Settings（アプリ名、Android のパッケージ名 `com.dnputv.vrdemo`）とビルドシーンの順序が設定されます。
4. Build Profiles でプラットフォームを Android に切り替え、APK をビルドして Quest にインストールする。

ビルドの先頭シーンは `Assets/DNPDemo/Scenes/DNPDemo_Main.unity` です。

## 体験の流れ

1. 2台の Quest でアプリを起動する。
2. 助手A がホストを開始し、助手B がホストに接続する。
3. 2人がそろうとシナリオが始まり、各ステップの操作（像の複製、空間選択ペンによる範囲選択など）を終えるごとに次の場面へ進みます。

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
