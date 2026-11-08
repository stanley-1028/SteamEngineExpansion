# SEE 美術佔位清單（v1 → P9 已替換）

> **P9（2026-11-07）已把本檔原列的「原版佔位模型」與「程式繪製圖示」全部替換為自製資產。**
> 正式交付報告見 `docs/ART_REPORT.md`；本檔保留為「佔位 → 自製」對照與 P9 待驗紀錄。
> 藍圖數值不在此調整；唯一數值源仍是 `mod/data/engines.json`，模型/圖示由
> `mod/tools/generate-blueprints` 產生（P9 ingest hook：`mod/art/icons/*.png` 原樣進 `data/Icons/`）。

## 1. 模型（已替換，24 成品 + 24 施工灰盒）

| 陣營 | 用途 | v1 原版佔位 | P9 自製模型 |
|---|---|---|---|
| IronTeeth | `#Finished` | `Buildings/Power/SteamEngine/SteamEngine.IronTeeth.Model` | `Buildings/Power/SteamBoiler/SteamBoiler.<Size>.<Tier>.IronTeeth.Model` |
| IronTeeth | `ConstructionStage0` | `Buildings/Power/SteamEngine/SteamEngine.IronTeeth.ConstructionStage0.Model` | `.../SteamBoiler.<Size>.<Tier>.IronTeeth.ConstructionStage0.Model` |
| Folktails | `#Finished` | `Buildings/Power/WaterWheel/WaterWheel.Folktails.Model` | `.../SteamBoiler.<Size>.<Tier>.Folktails.Model` |
| Folktails | `ConstructionStage0` | `Buildings/Power/WaterWheel/WaterWheel.Folktails.ConstructionStage0.Model` | `.../SteamBoiler.<Size>.<Tier>.Folktails.ConstructionStage0.Model` |

- 24 檔（3 尺寸 × 4 等級 × 2 陣營）成品 + 24 檔施工灰盒；來源 `mod/art/timbermesh/`，
  逐位元複製到 `mod/data/Buildings/Power/SteamBoiler/`（SHA-256 相同）。
- **一檔一陣營維持不變**（材質陣營綁定，PLAN §1.6 R-A）：FT 只引用 Folktails 材質、IT 只引用 IronTeeth。
- 模型邊界盒 min >= 0、不超出占地（煙囪／飛輪允許外伸 ≤1 格）、視覺高度 <= 尺寸高度+1；
  逐檔由 `mod/art/tools/validate_art.py` 檢查。
- 面數：LOD0 <= 3,500／6,000／9,000（S／M／L）；LOD1 <= 50%；LOD2 <= 15%；
  LOD1/LOD2 來源檔在 `mod/art/lod/`（`.Model.LOD1.timbermesh`／`.Model.LOD2.timbermesh`）。
- 碰撞盒仍由生成器依占地／高度換算（沿用 v1 口徑，P9 未改數值）。

## 2. 圖示（已替換）

- 24 張 `mod/data/Icons/SteamBoiler<Size><Tier><Faction>.png` 已改為 Blender 4.2.1 headless
  離線渲染（Cycles CPU，128×128 RGBA、透明背景、無文字），與 `mod/art/icons/` 逐位元相同。
- `.png.meta.json` 內容維持 `{"isSprite": true}`；`LabeledEntitySpec.Icon` 路徑不變。

## 3. 煙霧掛點與動畫（已補齊）

- 24 檔自製模型均含 `#EmptySmoke1–3`，且 24 份藍圖均輸出
  `TemplateAttachmentsSpec`（Parent=`#EmptySmoke1..3`，Prefab 原版
  `Buildings/Power/SteamEngine/SteamEngineSmoke`）＋`PowerGeneratorParticleControllerSpec`
  （Smoke1–3）＋`GoodPoweredGeneratorAnimatorSpec`。
- 飛輪／曲軸為 `ANIM_SEE_Flywheel`／`ANIM_SEE_Crankshaft` 具名節點（內建 `Default`
  node animation，LOD2 無動畫）；活塞／連桿為 `ANIM_SEE_Piston1..4`／`ANIM_SEE_ConnectingRod1..4`，
  供程式層 sin 驅動（C-9 ③）；本卡不改程式邏輯。
- 動畫 LOD 距離照 `SEE_Config.json`：18 m／40 m。

## 4. 仍待驗（不由 AI 宣告通過）

1. **實機外觀**（材質 UV patch、光照、實機 LOD／prefab 行為）：需 P8 人類 gate 或後續人類確認。
2. **動畫 runtime 行為**：`GoodPoweredGeneratorAnimatorSpec` 的實際帶動方式、活塞 sin／相位／
   起停緩動／機況四階抖動／VFX emission 屬程式層，需由 Lead 開卡實作並實機確認。
3. **LOD 整合**：`mod/art/lod/` 為 LOD 來源檔；遊戲執行只載入 `TimbermeshSpec.Model` 單一檔，
   LODGroup／prefab optimizer 整合不在本卡範圍。
4. **Large 4×4 施工底座**：沿用 `ConstructionBase4x4WithoutCorners`（原版無 `ConstructionBase4x4`）。

## 5. ConstructionBase 對應（沿用 v1，未變）

| 尺寸 | 佔地 | 採用的 ConstructionBase | 說明 |
|---|---|---|---|
| Small | 2×2 | `ConstructionBase2x2` | 完全對應 |
| Medium | 3×3 | `ConstructionBase3x3` | 完全對應 |
| Large | 4×4 | `ConstructionBase4x4WithoutCorners` | 原版沒有 `ConstructionBase4x4`，改用最接近且可包覆者 |

## 6. 歷史紀錄：v1 原版佔位（P9 前，已不再使用）

- v1 可玩版用原版蒸汽機（IT）與水車（FT）模型佔位；FT 的水車模型尺寸 3×2×3 與 2×2／4×4 佔地
  不完全吻合，視覺溢出為已知偏差，已在 P9 由自製模型消除。
- v1 的 24 張圖示由生成器程式繪製（陣營底色＋尺寸色塊＋等級 pips），P9 已替換為正式渲染。
