# Basis 本体への改造記録

upstream（`BasisVR/basis` の `long-term-support-20260916`）からの差分のうち、`Basis/Assets/DNPDemo/` 以外の変更をここに記録する。
upstream をマージするときの衝突確認に使う。

| 日付 | ファイル | 変更内容 | 理由 |
|---|---|---|---|
| 2026-10-13 | `Basis/ProjectSettings/ProjectSettings.asset` | productName / companyName / Android applicationIdentifier（`com.dnputv.vrdemo`） | デモ用の APK として識別するため（`DNP Demo/Setup` メニューで適用） |
| 2026-10-13 | `Basis/ProjectSettings/EditorBuildSettings.asset` | `DNPDemo_Main` をビルドの先頭シーンにし、initialization を無効化 | デモシーンを直接ワールドとして起動するため |
