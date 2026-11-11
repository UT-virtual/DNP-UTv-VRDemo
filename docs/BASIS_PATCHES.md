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
| 2026-11-03 | `Basis/Packages/com.basis.textmeshpro/Resources/TMP Settings.asset` | 既定のフォントアセットを NotoSansJP-Regular SDF（`Basis/Assets/DNPDemo/UI/Fonts/`）に変更。同時に、エディタが `m_ActiveFontFeatures`（kern）、空の `m_fallbackFontAssets`、`m_EmojiFallbackTextAssets` を書き換えている | 日本語の字幕と UI を表示するため |
| 2026-11-10 | `.gitattributes` / `.gitignore` | `Basis/Assets/DNPDemo/` 以下のバイナリを Git LFS の対象に追加。`*.sh` の改行を LF に固定。`*.blend1` を追跡対象外に追加 | 本デモのアセットを LFS で管理するため。Basis 本体のパスは LFS の対象にしない |
| 2026-11-10 | `.github/` | `workflows/` の `vpm-publish.yml` / `build-docker-server.yml` / `compilation.yml` / `media-native.yml` と、それらが使う `docker/` / `scripts/` / `vpm-packages.txt` を削除。`pr-checklist.yml`、Issue テンプレート、PR テンプレートを本デモ用に書き換え。`workflows/lfs-check.yml` を追加 | Basis 本家の配布・ビルド用の CI は本デモでは使わないため。upstream の merge でこれらに modify/delete 競合が出たら、削除を維持する。テンプレートと `pr-checklist.yml` は本デモの内容を維持する |
| 2026-11-11 | `Basis/Packages/com.basis.framework/Device Management/Devices/Base/BasisActionDriver.cs` | `LoadBindings()` の最後で発火するイベント `BindingsLoaded` を追加 | 起動時と VR／デスクトップの切り替えのたびにボタン割り当てが作り直されるため、デモで使わない割り当て（ミュート、ジャンプ、下降）をそのたびに外す（`DemoInputPolicy`） |
| 2026-11-11 | `Basis/Packages/com.basis.framework/Drivers/Local/BasisLocalMicrophoneDriver.cs` | ミュート解除を禁止する `SetUnmuteLocked` / `IsUnmuteLocked` を追加。禁止中は `IsPaused` の setter がミュート解除を状態を変える前に無視し、`Initialize` はミュートで始める | マイクを常にミュートに固定するため（`MicrophoneMuteLock`）。`OnPausedAction` の中でミュートし直す方法では、後に登録された購読者（マイクアイコンなど）に古い「非ミュート」が後から届き、表示と状態がずれる |
