# Basis 本体への改造記録

upstream（`BasisVR/basis` の `long-term-support-20260916`）からの差分のうち、`Basis/Assets/DNPDemo/` 以外の変更をここに記録する。
upstream をマージするときの衝突確認に使う。

| 日付 | ファイル | 変更内容 | 理由 |
|---|---|---|---|
| 2026-10-13 | `Basis/ProjectSettings/ProjectSettings.asset` | productName / companyName / Android applicationIdentifier（`com.dnputv.vrdemo`） | デモ用の APK として識別するため（`DNP Demo/Setup` メニューで適用） |
| 2026-10-13 | `Basis/ProjectSettings/EditorBuildSettings.asset` | `DNPDemo_Main` をビルドの先頭シーンにし、initialization を無効化 | デモシーンを直接ワールドとして起動するため |
| 2026-11-03 | `README.md` | 本デモ用の内容に全面的に書き換え。サードパーティのライセンス表記は `THIRD_PARTY_NOTICES.md`（新規）へ移動 | Basis 本家の案内（問い合わせ先、ロゴなど）を本デモの説明に置き換えるため。upstream で README が更新されたときは、`THIRD_PARTY_NOTICES.md` への反映だけを検討する |
| 2026-11-03 | `AI.md` / `CI.md` / `CONTRIBUTING.md` / `PHILOSOPHY.md` / `SECURITY.md` / `STYLE.md` | 削除 | Basis 本家のコントリビューター向け文書で、本デモには当てはまらないため。upstream の merge でこれらに modify/delete 競合が出たら、削除を維持する |
| 2026-11-03 | `Basis/Assets/Plugins/Android/AndroidManifest.xml` | `android.permission.CHANGE_WIFI_MULTICAST_STATE` を追加 | LAN でホストを探すとき、参加側の Quest が UDP ブロードキャストを受信できるよう `WifiManager.MulticastLock` を取得するため |
| 2026-11-03 | `Basis/Packages/com.basis.rtao/Tests/BasisRTAODepthContinuityTests.cs` / `BasisRTAOEndToEndTests.cs` | TearDown でのレンダーパイプライン復元を、SetUp で実際に差し替えたとき（`pipelineSwapped`）だけに限定 | レイトレーシング非対応環境（DX11 エディタなど）では SetUp が退避前に `Assert.Ignore` で抜けるが、NUnit は TearDown を実行するため、未退避の null が GraphicsSettings / QualitySettings に書き戻され、プロジェクトが Built-in RP に切り替わっていた |
