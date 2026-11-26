# 学者アバターの進捗（2026年11月26日）

[Issue #78](https://github.com/UT-virtual/DNP-UTv-VRDemo/issues/78) は、ローカルのUnityプロジェクトで学者A・BのモデルPrefabを生成した段階です。モデルと素材は未コミットです。

| NPC | 元データ（Art/Scholars/ 配下） | 生成したPrefab（同左） |
|---|---|---|
| 学者A（考古学者） | `Source~/ScholarA.vrm` | `nameA/ScholarA.prefab` |
| 学者B（技術者） | `Source~/ScholarB.vrm` | `nameB/ScholarB.prefab` |

両モデルともVRM 0.0で、顔メッシュの5母音（A・I・U・E・O）と、再配布ライセンス欄の `Other` およびVRoid公式の利用条件URLを確認しました。

## 残作業

- 展示室の学者NPCに各モデルを割り当て、足元・立ち姿・音源・頭上字幕の位置を合わせる。
- 各NPCのuLipSyncを対応する顔メッシュの5母音に接続する。
- 展示室でA・Bそれぞれの台詞を再生し、話者の位置から音声が聞こえ、本人の字幕と口パクが連動することを確認する。
