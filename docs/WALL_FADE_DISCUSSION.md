# 壁フェード(b)：段階的な表示制御と部分修正の実装

## 現状の問題

### 問題 1：壁 alpha は環境切り替え時にのみ変更
- Exhibition（表示）→ Temple（非表示）時に、wall alpha が 1.0 → 0.0 にフェード。
- **それ以外のプリセット間の遷移では alpha が変わらない。**
- 結果：展示室の壁が Temple で見えてしまう（視覚的に違和感）。
- 参照：`EnvironmentPreset.ExhibitionRoomVisible` フラグはプリセットごとの静的属性。

### 問題 2：一部の壁のみを修正したい場合の手段がない
- Exhibition 内部でも、エリアごとに視認性を変えたい場合がある（ライティング調整時など）。
- 現在は global alpha のみ対応で、部分修正が困難。

### 問題 3：Fading in/out の中間状態が不安定
- alpha ～ 0.5 付近で、半透明壁の奥に見える要素がチラつく可能性がある（深度ソート順序によっては）。

---

## 解決案

### 案(i)：ExhibitionRoomVisible を 3 値以上に拡張

**状態定義：**
```csharp
public enum ExhibitionRoomVisibility
{
    Visible,      // alpha = 1.0 (通常)
    SemiTransparent, // alpha = 0.5 (ライティング調査時など)
    Hidden,       // alpha = 0.0 (Temple など)
}
```

**実装内容：**
- `EnvironmentPreset.ExhibitionRoomVisible` を `ExhibitionRoomVisibility` enum に変更。
- `EnvironmentController.ApplyState()` で target alpha を enum から決定。
- 各プリセットで以下のように設定：
  - Exhibition = Visible
  - Temple = Hidden
  - （将来拡張用：ライティング調査プリセット = SemiTransparent）

**利点：** シンプル、既存ロジックへの影響小。  
**欠点：** 複雑な部分フェード（例：壁の左半分だけ）には非対応。

---

### 案(ii)：Renderer ごとの Wall Alpha 制御

**実装内容：**
- Env_ExhibitionRoom.prefab 内の壁 Renderer を複数の子オブジェクトに分割（例：WallFront, WallSide, WallBack）。
- 各壁に `ScenarioObjectBinding` キーを付与（例："WallFront"）。
- `EnvironmentController` で binding キーごとに alpha 制御マップを保持。
- プリセットで "WallFront" → 1.0、"WallSide" → 0.3 のような設定が可能。

**利点：** 柔軟（エリアごとの部分修正が可能）。  
**欠点：** 
- Prefab 大幅編集が必要（Renderer 分割）。
- 設定複雑性が大きく増す。
- テストケースが多数必要。

---

### 案(iii)：Material Override による単プリセット特例

**実装内容：**
- Env_ExhibitionRoom の Material に複数の透明度バリアント を URP Material Variant で用意。
  - Material_AlphaFull（alpha = 1.0）
  - Material_AlphaSemi（alpha = 0.5）
  - Material_AlphaHidden（alpha = 0.0）
- プリセットで Material Override フィールドを追加。
- `EnvironmentController.ApplyState()` で Material を切り替え。

**利点：** 
- Prefab 編集不要。
- パフォーマンス良好（Material Slot 切り替えのみ）。
- 局所的な調整に強い。

**欠点：**
- Material 管理が増える。
- 中間状態（Fade during transition）では補間不可（段階的に切り替わる）。

---

## 推奨実装順序

1. **Phase 1（現在の PR #61 後）：案(i) を実装**
   - `ExhibitionRoomVisibility` enum を導入。
   - 既存 `bool` ロジックを enum 判定に変更（1-2h）。
   - テスト修正（0.5h）。
   - **優先度：High**（現状の問題を即座に解決）。

2. **Phase 2（機能フリーズ前）：案(ii) の検討**
   - 壁の部分フェード要件が確定した場合のみ実装。
   - Prefab 再分割 → Binding キー追加 → Controller ロジック拡張（4-5h）。
   - **優先度：Medium**（要件次第）。

3. **Phase 3（リリース後アップデート）：案(iii) の補足対応**
   - 特定のライティング調査シーン用 Material バリアント 追加。
   - **優先度：Low**（ニッチな要件向け）。

---

## 必要な Prefab 編集

### 案(i) の場合
- **編集対象：** EnvironmentPreset_Exhibition.asset / EnvironmentPreset_Temple.asset
- **変更内容：** ExhibitionRoomVisible フィールドを bool → enum に型変更のみ。
- **Prefab 編集不要。**

### 案(ii) の場合
- **編集対象：** Env_ExhibitionRoom.prefab
- **変更内容：**
  1. 既存の "ExhibitionRoom" オブジェクトの WallDisplay（単一 Renderer）を、複数の子オブジェクト（WallFront, WallSide, WallBack など）に分割。
  2. 各壁に `ScenarioObjectBinding` を追加し、キーを設定。
  3. 既存の wall collider が全体を覆うよう再配置。

### 案(iii) の場合
- **編集対象：** WallDisplay Material（URP Material）
- **変更内容：**
  1. Base Material と 2 つ以上の Variant を作成。
  2. Alpha 値をそれぞれ設定（1.0, 0.5, 0.0）。
  3. EnvironmentPreset に Material フィールドを追加。
  4. プリセット内で material をアサイン。

---

## テスト項目（案(i) 実装時）

```csharp
[Test]
public void ExhibitionRoomVisibilityControlsWallAlpha()
{
    // Exhibition = Visible → alpha = 1.0
    fixture.Controller.ApplyInstant(_exhibition);
    Assert.AreEqual(1.0f, fixture.WallMaterial().GetColor("_BaseColor").a, 0.001f);
    
    // Temple = Hidden → alpha = 0.0
    fixture.Controller.ApplyInstant(_temple);
    Assert.AreEqual(0.0f, fixture.WallMaterial().GetColor("_BaseColor").a, 0.001f);
    
    // （将来拡張：SemiTransparent = 0.5）
}

[Test]
public void WallFadesGraduallyDuringTransition()
{
    fixture.Controller.ApplyInstant(_exhibition);
    fixture.Controller.TransitionTo(_temple, 2f);
    fixture.Controller.AdvanceTransition(1f);
    
    float midAlpha = fixture.WallMaterial().GetColor("_BaseColor").a;
    Assert.Greater(midAlpha, 0.0f);
    Assert.Less(midAlpha, 1.0f);
    Assert.AreEqual(0.5f, midAlpha, 0.05f); // Lerp(1.0, 0.0, 0.5) = 0.5
}
```

---

## 履歴・関連情報

- **関連 Issue：** #19（環境切り替え実装）
- **関連 PR：** #61（EnvironmentPreset 実装）
- **発見時期：** PR #61 レビュー時
- **優先度：** 低（ビジュアル改善・ユーザビリティ向上の範囲）
