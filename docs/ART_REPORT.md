# SEE P9 美術報告

> 產生器：`mod/art/tools/write_art_report.py`（資料源：`mod/art/timbermesh/export-manifest.json`、24 份藍圖、`material_patches.json`、`mod/art/lod/`、`mod/art/icons/`、`mod/art/preview/`）。
> 範圍：把原版佔位模型換成自製 `.timbermesh`、補 LOD／圖示／掛點／動畫綁定；**未改**任何玩法數值、藍圖數值、程式邏輯、存檔或 loc。
> 實機外觀**未**由 AI 宣告通過：仍需 P8 人類實機 gate 或後續人類確認。

## 1. 交付摘要

| 項目 | 數量 | 內容 |
|---|---:|---|
| LOD0 `.timbermesh` | 24 | 24 棟（3 尺寸 x 4 等級 x 2 陣營）成品模型 |
| ConstructionStage0 `.timbermesh` | 24 | 24 棟施工階段灰盒（每棟 144 tris） |
| LOD1 / LOD2 `.timbermesh` | 48 | `mod/art/lod/` 的 LOD 來源檔（各 24） |
| 圖示 | 24 PNG + 24 meta | 128x128 RGBA、透明背景、無文字 |
| 抽樣預覽圖 | 6 | `mod/art/preview/*_iso.png`（Blender Cycles 640x480） |
| LOD0 面數合計 | 87768 tris | 24 棟成品模型總和 |
| LOD0 檔案合計 | 1.15 MB | 24 個 `.Model.timbermesh` |
| ConstructionStage0 合計 | 3456 tris / 53.3 KB | 24 個灰盒 |
| LOD1+LOD2 檔案合計 | 586.4 KB | 48 個來源檔 |

- 模型輸出位置：`mod/art/timbermesh/`（來源）與 `mod/data/Buildings/Power/SteamBoiler/`（遊戲載入副本，SHA-256 逐檔相同）。
- 24 份藍圖的 `Children.#Finished.TimbermeshSpec.Model` 與 `Children.#Unfinished.Children.ConstructionStage0.TimbermeshSpec.Model` 已指向自製模型；**只改模型路徑與掛點，未動任何玩法數值、藍圖數值、程式邏輯、存檔或 loc。**
- 材質只使用原版 `MaterialCollection` 名稱（逐檔靜態核對存在），未新增貼圖格式、未覆寫原版資產。
- 實機外觀**尚未**由 AI 宣告通過：仍需 P8 人類實機 gate 或後續人類確認。

## 2. 39 mesh 對映

需求 §16.4 的 39 個 mesh 是**邏輯資產槽**：3 個 Body（各 3 個 LOD）、9 個 TierKit（各 LOD0/LOD1）、6 個 FactionKit（各 LOD0/LOD1）。遊戲的 `TimbermeshSpec` 每棟只載入一個模型路徑，因此本實作把 Body + TierKit + FactionKit **合併成該棟唯一的 `.Model.timbermesh`**（本體為主 mesh，飛輪／曲軸／活塞／連桿為具名子節點），24 份藍圖 = 12 種幾何 x 2 陣營材質。

| 邏輯資產 | 數量 | LOD 級數 | mesh 槽數 | 落地方式 |
|---|---:|---:|---:|---|
| Body(Size) | 3 | LOD0/LOD1/LOD2 | 9 | 每檔的 root mesh（合併本體與平台） |
| TierKit(Size,Tier) | 9 | LOD0/LOD1（LOD2 併入本體） | 18 | 同一 root mesh 的等級裝飾（壓力錶／齒輪箱／凝結器／玻璃水位計） |
| FactionKit(Size,Faction) | 6 | LOD0/LOD1 | 12 | 同一 root mesh 的陣營小件（FT 木支架／IT 鑄鐵斜撐＋鉚釘銘牌） |
| **合計** | **39** | — | **39** | **24 個 `.Model.timbermesh`（12 幾何 x 2 陣營）** |

| 藍圖（12 種幾何） | Body | TierKit | FactionKit | 合併輸出檔 |
|---|---|---|---|---|
| `SteamBoiler.Small.T1` | Body(Small) | TierKit(Small,T1) | FactionKit(Small,FT/IT) | `SteamBoiler.Small.T1.<Faction>.Model.timbermesh` |
| `SteamBoiler.Small.T2` | Body(Small) | TierKit(Small,T2) | FactionKit(Small,FT/IT) | `SteamBoiler.Small.T2.<Faction>.Model.timbermesh` |
| `SteamBoiler.Small.T3` | Body(Small) | TierKit(Small,T3) | FactionKit(Small,FT/IT) | `SteamBoiler.Small.T3.<Faction>.Model.timbermesh` |
| `SteamBoiler.Small.T4` | Body(Small) | TierKit(Small,T4) | FactionKit(Small,FT/IT) | `SteamBoiler.Small.T4.<Faction>.Model.timbermesh` |
| `SteamBoiler.Medium.T1` | Body(Medium) | TierKit(Medium,T1) | FactionKit(Medium,FT/IT) | `SteamBoiler.Medium.T1.<Faction>.Model.timbermesh` |
| `SteamBoiler.Medium.T2` | Body(Medium) | TierKit(Medium,T2) | FactionKit(Medium,FT/IT) | `SteamBoiler.Medium.T2.<Faction>.Model.timbermesh` |
| `SteamBoiler.Medium.T3` | Body(Medium) | TierKit(Medium,T3) | FactionKit(Medium,FT/IT) | `SteamBoiler.Medium.T3.<Faction>.Model.timbermesh` |
| `SteamBoiler.Medium.T4` | Body(Medium) | TierKit(Medium,T4) | FactionKit(Medium,FT/IT) | `SteamBoiler.Medium.T4.<Faction>.Model.timbermesh` |
| `SteamBoiler.Large.T1` | Body(Large) | TierKit(Large,T1) | FactionKit(Large,FT/IT) | `SteamBoiler.Large.T1.<Faction>.Model.timbermesh` |
| `SteamBoiler.Large.T2` | Body(Large) | TierKit(Large,T2) | FactionKit(Large,FT/IT) | `SteamBoiler.Large.T2.<Faction>.Model.timbermesh` |
| `SteamBoiler.Large.T3` | Body(Large) | TierKit(Large,T3) | FactionKit(Large,FT/IT) | `SteamBoiler.Large.T3.<Faction>.Model.timbermesh` |
| `SteamBoiler.Large.T4` | Body(Large) | TierKit(Large,T4) | FactionKit(Large,FT/IT) | `SteamBoiler.Large.T4.<Faction>.Model.timbermesh` |

等級外觀差異（需求 §16.2；2026-11-08 更新）：**T1/Mk.I 與 T2/Mk.II 改為全木構造**——殼體、箍帶、鉚釘／細節、煙囪、飛輪、曲軸、活塞全部使用木質材質（Mk.I 深木色主構＋淺木色飾件；Mk.II 淺木色主構＋深木色飾件；Folktails／IronTeeth 各自木色），不再有任何金屬或黃銅件；T3 鑄鐵／鋼殼、鉚釘列、齒輪箱、凝結器、排氣閥；T4 精煉金屬、玻璃水位計、拋光飾條。尺寸差異：煙囪數 Small 1 / Medium 2 / Large 2-3；飛輪 T1 x1、T2 x2、T3 x2、T4 x1（大型 T4 為巨型飛輪）；汽缸數 T1 1 / T2 2 / T3 4 / T4 4。2026-11-08 追加修正：T2/T3 的雙飛輪原本安裝在機殼內部（x=0.28W/0.72W）而被殼體完全遮住，現已改裝到機身 +X 外側（與 T1/T4 單飛輪同側、同軸），曲軸同步延長；T2 維持全木、T3 維持金屬，triangles/vertices/節點/動畫幀數不變，飛輪仍完整落在占地內（不外伸）。

LOD 槽數換算：Body 3x3=9、TierKit 9x2=18、FactionKit 6x2=12，合計 **39**；另有 24 個 ConstructionStage0 灰盒（`<stem>.ConstructionStage0.Model.timbermesh`）。

| 藍圖 | 成品模型 | 施工灰盒 |
|---|---|---|
| `SteamBoiler.Small.T1.Folktails` | `SteamBoiler.Small.T1.Folktails.Model` | `SteamBoiler.Small.T1.Folktails.ConstructionStage0.Model` |
| `SteamBoiler.Small.T1.IronTeeth` | `SteamBoiler.Small.T1.IronTeeth.Model` | `SteamBoiler.Small.T1.IronTeeth.ConstructionStage0.Model` |
| `SteamBoiler.Small.T2.Folktails` | `SteamBoiler.Small.T2.Folktails.Model` | `SteamBoiler.Small.T2.Folktails.ConstructionStage0.Model` |
| `SteamBoiler.Small.T2.IronTeeth` | `SteamBoiler.Small.T2.IronTeeth.Model` | `SteamBoiler.Small.T2.IronTeeth.ConstructionStage0.Model` |
| `SteamBoiler.Small.T3.Folktails` | `SteamBoiler.Small.T3.Folktails.Model` | `SteamBoiler.Small.T3.Folktails.ConstructionStage0.Model` |
| `SteamBoiler.Small.T3.IronTeeth` | `SteamBoiler.Small.T3.IronTeeth.Model` | `SteamBoiler.Small.T3.IronTeeth.ConstructionStage0.Model` |
| `SteamBoiler.Small.T4.Folktails` | `SteamBoiler.Small.T4.Folktails.Model` | `SteamBoiler.Small.T4.Folktails.ConstructionStage0.Model` |
| `SteamBoiler.Small.T4.IronTeeth` | `SteamBoiler.Small.T4.IronTeeth.Model` | `SteamBoiler.Small.T4.IronTeeth.ConstructionStage0.Model` |
| `SteamBoiler.Medium.T1.Folktails` | `SteamBoiler.Medium.T1.Folktails.Model` | `SteamBoiler.Medium.T1.Folktails.ConstructionStage0.Model` |
| `SteamBoiler.Medium.T1.IronTeeth` | `SteamBoiler.Medium.T1.IronTeeth.Model` | `SteamBoiler.Medium.T1.IronTeeth.ConstructionStage0.Model` |
| `SteamBoiler.Medium.T2.Folktails` | `SteamBoiler.Medium.T2.Folktails.Model` | `SteamBoiler.Medium.T2.Folktails.ConstructionStage0.Model` |
| `SteamBoiler.Medium.T2.IronTeeth` | `SteamBoiler.Medium.T2.IronTeeth.Model` | `SteamBoiler.Medium.T2.IronTeeth.ConstructionStage0.Model` |
| `SteamBoiler.Medium.T3.Folktails` | `SteamBoiler.Medium.T3.Folktails.Model` | `SteamBoiler.Medium.T3.Folktails.ConstructionStage0.Model` |
| `SteamBoiler.Medium.T3.IronTeeth` | `SteamBoiler.Medium.T3.IronTeeth.Model` | `SteamBoiler.Medium.T3.IronTeeth.ConstructionStage0.Model` |
| `SteamBoiler.Medium.T4.Folktails` | `SteamBoiler.Medium.T4.Folktails.Model` | `SteamBoiler.Medium.T4.Folktails.ConstructionStage0.Model` |
| `SteamBoiler.Medium.T4.IronTeeth` | `SteamBoiler.Medium.T4.IronTeeth.Model` | `SteamBoiler.Medium.T4.IronTeeth.ConstructionStage0.Model` |
| `SteamBoiler.Large.T1.Folktails` | `SteamBoiler.Large.T1.Folktails.Model` | `SteamBoiler.Large.T1.Folktails.ConstructionStage0.Model` |
| `SteamBoiler.Large.T1.IronTeeth` | `SteamBoiler.Large.T1.IronTeeth.Model` | `SteamBoiler.Large.T1.IronTeeth.ConstructionStage0.Model` |
| `SteamBoiler.Large.T2.Folktails` | `SteamBoiler.Large.T2.Folktails.Model` | `SteamBoiler.Large.T2.Folktails.ConstructionStage0.Model` |
| `SteamBoiler.Large.T2.IronTeeth` | `SteamBoiler.Large.T2.IronTeeth.Model` | `SteamBoiler.Large.T2.IronTeeth.ConstructionStage0.Model` |
| `SteamBoiler.Large.T3.Folktails` | `SteamBoiler.Large.T3.Folktails.Model` | `SteamBoiler.Large.T3.Folktails.ConstructionStage0.Model` |
| `SteamBoiler.Large.T3.IronTeeth` | `SteamBoiler.Large.T3.IronTeeth.Model` | `SteamBoiler.Large.T3.IronTeeth.ConstructionStage0.Model` |
| `SteamBoiler.Large.T4.Folktails` | `SteamBoiler.Large.T4.Folktails.Model` | `SteamBoiler.Large.T4.Folktails.ConstructionStage0.Model` |
| `SteamBoiler.Large.T4.IronTeeth` | `SteamBoiler.Large.T4.IronTeeth.Model` | `SteamBoiler.Large.T4.IronTeeth.ConstructionStage0.Model` |

## 3. 逐檔面數與 LOD

### 3.1 成品模型（LOD0）與 LOD1/LOD2

| 檔案 stem | LOD0 tris | LOD1 tris（比例） | LOD2 tris（比例） | LOD0 bytes | LOD1 bytes | LOD2 bytes | 材質 |
|---|---:|---:|---:|---:|---:|---:|---|
| `SteamBoiler.Small.T1.Folktails` | 2332 | 1008（43.2%） | 240（10.3%） | 33.3 KB | 13.7 KB | 3.6 KB | BaseWood_Brown.Folktails, BaseWood_LightBrown.Folktails |
| `SteamBoiler.Small.T1.IronTeeth` | 2344 | 1008（43.0%） | 240（10.2%） | 33.4 KB | 13.7 KB | 3.6 KB | BaseWood_DarkBrown.IronTeeth, BaseWood_Grey.IronTeeth |
| `SteamBoiler.Small.T2.Folktails` | 3008 | 1348（44.8%） | 288（9.6%） | 42.4 KB | 18.4 KB | 3.9 KB | BaseWood_LightBrown.Folktails, BaseWood_Brown.Folktails |
| `SteamBoiler.Small.T2.IronTeeth` | 3020 | 1348（44.6%） | 288（9.5%） | 42.5 KB | 18.4 KB | 3.9 KB | BaseWood_Grey.IronTeeth, BaseWood_DarkBrown.IronTeeth |
| `SteamBoiler.Small.T3.Folktails` | 3484 | 1576（45.2%） | 324（9.3%） | 47.2 KB | 20.6 KB | 4.6 KB | BaseMetal.Folktails, Details.Folktails |
| `SteamBoiler.Small.T3.IronTeeth` | 3496 | 1576（45.1%） | 324（9.3%） | 47.2 KB | 20.6 KB | 4.6 KB | BaseMetal.IronTeeth, Details.IronTeeth |
| `SteamBoiler.Small.T4.Folktails` | 3260 | 1488（45.6%） | 288（8.8%） | 44.0 KB | 18.7 KB | 4.5 KB | BaseMetal.Folktails, Details.Folktails, WindowsAtlas.Folktails |
| `SteamBoiler.Small.T4.IronTeeth` | 3272 | 1488（45.5%） | 288（8.8%） | 44.1 KB | 18.7 KB | 4.5 KB | BaseMetal.IronTeeth, Details.IronTeeth, WindowsAtlas.IronTeeth |
| `SteamBoiler.Medium.T1.Folktails` | 2844 | 1160（40.8%） | 264（9.3%） | 39.7 KB | 15.8 KB | 3.9 KB | BaseWood_Brown.Folktails, BaseWood_LightBrown.Folktails |
| `SteamBoiler.Medium.T1.IronTeeth` | 2856 | 1160（40.6%） | 264（9.2%） | 39.9 KB | 15.7 KB | 3.9 KB | BaseWood_DarkBrown.IronTeeth, BaseWood_Grey.IronTeeth |
| `SteamBoiler.Medium.T2.Folktails` | 3480 | 1500（43.1%） | 312（9.0%） | 48.0 KB | 20.5 KB | 4.2 KB | BaseWood_LightBrown.Folktails, BaseWood_Brown.Folktails |
| `SteamBoiler.Medium.T2.IronTeeth` | 3492 | 1500（43.0%） | 312（8.9%） | 48.2 KB | 20.5 KB | 4.2 KB | BaseWood_Grey.IronTeeth, BaseWood_DarkBrown.IronTeeth |
| `SteamBoiler.Medium.T3.Folktails` | 4364 | 1728（39.6%） | 348（8.0%） | 56.5 KB | 22.5 KB | 4.9 KB | BaseMetal.Folktails, Details.Folktails |
| `SteamBoiler.Medium.T3.IronTeeth` | 4376 | 1728（39.5%） | 348（8.0%） | 56.7 KB | 22.6 KB | 4.9 KB | BaseMetal.IronTeeth, Details.IronTeeth |
| `SteamBoiler.Medium.T4.Folktails` | 4140 | 1640（39.6%） | 312（7.5%） | 53.3 KB | 21.5 KB | 4.8 KB | BaseMetal.Folktails, Details.Folktails, WindowsAtlas.Folktails |
| `SteamBoiler.Medium.T4.IronTeeth` | 4152 | 1640（39.5%） | 312（7.5%） | 53.5 KB | 21.5 KB | 4.8 KB | BaseMetal.IronTeeth, Details.IronTeeth, WindowsAtlas.IronTeeth |
| `SteamBoiler.Large.T1.Folktails` | 3280 | 1312（40.0%） | 288（8.8%） | 44.8 KB | 17.8 KB | 4.3 KB | BaseWood_Brown.Folktails, BaseWood_LightBrown.Folktails |
| `SteamBoiler.Large.T1.IronTeeth` | 3292 | 1312（39.9%） | 288（8.8%） | 44.8 KB | 17.7 KB | 4.3 KB | BaseWood_DarkBrown.IronTeeth, BaseWood_Grey.IronTeeth |
| `SteamBoiler.Large.T2.Folktails` | 3956 | 1652（41.8%） | 336（8.5%） | 53.2 KB | 22.4 KB | 4.6 KB | BaseWood_LightBrown.Folktails, BaseWood_Brown.Folktails |
| `SteamBoiler.Large.T2.IronTeeth` | 3968 | 1652（41.6%） | 336（8.5%） | 53.3 KB | 22.4 KB | 4.6 KB | BaseWood_Grey.IronTeeth, BaseWood_DarkBrown.IronTeeth |
| `SteamBoiler.Large.T3.Folktails` | 4944 | 1880（38.0%） | 372（7.5%） | 63.2 KB | 24.6 KB | 5.1 KB | BaseMetal.Folktails, Details.Folktails |
| `SteamBoiler.Large.T3.IronTeeth` | 4956 | 1880（37.9%） | 372（7.5%） | 63.4 KB | 24.6 KB | 5.1 KB | BaseMetal.IronTeeth, Details.IronTeeth |
| `SteamBoiler.Large.T4.Folktails` | 4720 | 1792（38.0%） | 336（7.1%） | 60.5 KB | 23.4 KB | 5.1 KB | BaseMetal.Folktails, Details.Folktails, WindowsAtlas.Folktails |
| `SteamBoiler.Large.T4.IronTeeth` | 4732 | 1792（37.9%） | 336（7.1%） | 60.6 KB | 23.4 KB | 5.1 KB | BaseMetal.IronTeeth, Details.IronTeeth, WindowsAtlas.IronTeeth |

### 3.2 尺寸面數預算（需求 §16.5）

| 尺寸 | 占地面積 | 高度 | LOD0 上限 | 本卡 LOD0 最大 | 餘裕 | LOD1 上限（<=50%） | LOD1 最大比例 | LOD2 上限（<=15%） | LOD2 最大比例 |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Small | 2x2 | 3 | 3500 | 3496 | +4 | 1750 | 45.6% | 525 | 10.3% |
| Medium | 3x3 | 3 | 6000 | 4376 | +1624 | 3000 | 43.1% | 900 | 9.3% |
| Large | 4x4 | 4 | 9000 | 4956 | +4044 | 4500 | 41.8% | 1350 | 8.8% |

所有 24 檔 LOD0 均在預算內；LOD1 為 LOD0 的 37.9%-45.6%（<=50%）；LOD2 為 7.1%-10.3%（<=15%）。LOD 距離照 `SEE_Config.json` 的 `anim.lod0MaxDistance=18`、`anim.lod1MaxDistance=40`（需求 §16.5 的 0-18 / 18-40 / >40 m）。

### 3.3 ConstructionStage0 灰盒（24 檔）

| 檔案 stem | tris | bytes | 材質 |
|---|---:|---:|---|
| `SteamBoiler.Small.T1.Folktails.ConstructionStage0.Model` | 144 | 2.2 KB | BaseWood_Brown.Folktails, BaseWood_LightBrown.Folktails |
| `SteamBoiler.Small.T1.IronTeeth.ConstructionStage0.Model` | 144 | 2.2 KB | BaseWood_DarkBrown.IronTeeth, BaseWood_Grey.IronTeeth |
| `SteamBoiler.Small.T2.Folktails.ConstructionStage0.Model` | 144 | 2.2 KB | BaseWood_LightBrown.Folktails, BaseWood_Brown.Folktails |
| `SteamBoiler.Small.T2.IronTeeth.ConstructionStage0.Model` | 144 | 2.2 KB | BaseWood_Grey.IronTeeth, BaseWood_DarkBrown.IronTeeth |
| `SteamBoiler.Small.T3.Folktails.ConstructionStage0.Model` | 144 | 2.3 KB | BaseWood_Brown.Folktails, BaseMetal.Folktails |
| `SteamBoiler.Small.T3.IronTeeth.ConstructionStage0.Model` | 144 | 2.3 KB | BaseWood_DarkBrown.IronTeeth, BaseMetal.IronTeeth |
| `SteamBoiler.Small.T4.Folktails.ConstructionStage0.Model` | 144 | 2.3 KB | BaseWood_Brown.Folktails, BaseMetal.Folktails |
| `SteamBoiler.Small.T4.IronTeeth.ConstructionStage0.Model` | 144 | 2.3 KB | BaseWood_DarkBrown.IronTeeth, BaseMetal.IronTeeth |
| `SteamBoiler.Medium.T1.Folktails.ConstructionStage0.Model` | 144 | 2.2 KB | BaseWood_Brown.Folktails, BaseWood_LightBrown.Folktails |
| `SteamBoiler.Medium.T1.IronTeeth.ConstructionStage0.Model` | 144 | 2.2 KB | BaseWood_DarkBrown.IronTeeth, BaseWood_Grey.IronTeeth |
| `SteamBoiler.Medium.T2.Folktails.ConstructionStage0.Model` | 144 | 2.2 KB | BaseWood_LightBrown.Folktails, BaseWood_Brown.Folktails |
| `SteamBoiler.Medium.T2.IronTeeth.ConstructionStage0.Model` | 144 | 2.2 KB | BaseWood_Grey.IronTeeth, BaseWood_DarkBrown.IronTeeth |
| `SteamBoiler.Medium.T3.Folktails.ConstructionStage0.Model` | 144 | 2.3 KB | BaseWood_Brown.Folktails, BaseMetal.Folktails |
| `SteamBoiler.Medium.T3.IronTeeth.ConstructionStage0.Model` | 144 | 2.3 KB | BaseWood_DarkBrown.IronTeeth, BaseMetal.IronTeeth |
| `SteamBoiler.Medium.T4.Folktails.ConstructionStage0.Model` | 144 | 2.3 KB | BaseWood_Brown.Folktails, BaseMetal.Folktails |
| `SteamBoiler.Medium.T4.IronTeeth.ConstructionStage0.Model` | 144 | 2.3 KB | BaseWood_DarkBrown.IronTeeth, BaseMetal.IronTeeth |
| `SteamBoiler.Large.T1.Folktails.ConstructionStage0.Model` | 144 | 2.2 KB | BaseWood_Brown.Folktails, BaseWood_LightBrown.Folktails |
| `SteamBoiler.Large.T1.IronTeeth.ConstructionStage0.Model` | 144 | 2.2 KB | BaseWood_DarkBrown.IronTeeth, BaseWood_Grey.IronTeeth |
| `SteamBoiler.Large.T2.Folktails.ConstructionStage0.Model` | 144 | 2.2 KB | BaseWood_LightBrown.Folktails, BaseWood_Brown.Folktails |
| `SteamBoiler.Large.T2.IronTeeth.ConstructionStage0.Model` | 144 | 2.2 KB | BaseWood_Grey.IronTeeth, BaseWood_DarkBrown.IronTeeth |
| `SteamBoiler.Large.T3.Folktails.ConstructionStage0.Model` | 144 | 2.3 KB | BaseWood_Brown.Folktails, BaseMetal.Folktails |
| `SteamBoiler.Large.T3.IronTeeth.ConstructionStage0.Model` | 144 | 2.3 KB | BaseWood_DarkBrown.IronTeeth, BaseMetal.IronTeeth |
| `SteamBoiler.Large.T4.Folktails.ConstructionStage0.Model` | 144 | 2.3 KB | BaseWood_Brown.Folktails, BaseMetal.Folktails |
| `SteamBoiler.Large.T4.IronTeeth.ConstructionStage0.Model` | 144 | 2.3 KB | BaseWood_DarkBrown.IronTeeth, BaseMetal.IronTeeth |

## 4. 材質

規則：只使用原版 `MaterialCollection` 名稱（分陣營白名單靜態核對），每棟 <=2 個不透明材質 + <=1 個玻璃材質（需求 §16.5）；未新增貼圖格式、未覆寫原版資產。UV 為各原版 `_D` 貼圖的固定 patch（由 `material_patches.json` 記錄，沿用 HDBF 已驗證流程）。

| 等級 | 陣營 | 結構材質 | 細節／次要材質 | 玻璃 | 不透明數 | 來源貼圖（patch） |
|---|---|---|---|---|---:|---|
| T1 | Folktails | `BaseWood_Brown.Folktails` | `BaseWood_LightBrown.Folktails` | — | 2 | `BaseWood_1K_D_Brown.png`, `BaseWood_1K_D_White.png` |
| T1 | IronTeeth | `BaseWood_DarkBrown.IronTeeth` | `BaseWood_Grey.IronTeeth` | — | 2 | `BaseWood_1K_D_Grey.png` |
| T2 | Folktails | `BaseWood_LightBrown.Folktails` | `BaseWood_Brown.Folktails` | — | 2 | `BaseWood_1K_D_Brown.png`, `BaseWood_1K_D_White.png` |
| T2 | IronTeeth | `BaseWood_Grey.IronTeeth` | `BaseWood_DarkBrown.IronTeeth` | — | 2 | `BaseWood_1K_D_Grey.png` |
| T3 | Folktails | `BaseMetal.Folktails` | `BaseMetal.Folktails` | — | 2 | `BaseMetal_D1.png`, `Details_D.Folktails.png` |
| T3 | IronTeeth | `BaseMetal.IronTeeth` | `BaseMetal.IronTeeth` | — | 2 | `BaseMetal_D2.png`, `Details_D.IronTeeth.png` |
| T4 | Folktails | `BaseMetal.Folktails` | `BaseMetal.Folktails` | `WindowsAtlas.Folktails` | 2 | `BaseMetal_D1.png`, `Details_D.Folktails.png`, `WindowsAtlas_D.Folktails.png` |
| T4 | IronTeeth | `BaseMetal.IronTeeth` | `BaseMetal.IronTeeth` | `WindowsAtlas.IronTeeth` | 2 | `BaseMetal_D2.png`, `Details_D.IronTeeth.png`, `WindowsAtlas_D.IronTeeth.png` |

| 材質名稱 | 角色 | patch class | 原版貼圖 | UV patch（u0,v0,u1,v1） |
|---|---|---|---|---|
| `BaseMetal.Folktails` | metal | metal | BaseMetal_D1.png | `[0.0078125, 0.5078125, 0.4921875, 0.9921875]` |
| `BaseMetal.IronTeeth` | metal | metal | BaseMetal_D2.png | `[0.0078125, 0.5078125, 0.4921875, 0.9921875]` |
| `BaseWood_Brown.Folktails` | wood | wood | BaseWood_1K_D_Brown.png | `[0.00390625, 0.56640625, 0.5, 0.99609375]` |
| `BaseWood_DarkBrown.IronTeeth` | wood | wood | BaseWood_1K_D_Grey.png | `[0.00390625, 0.56640625, 0.5, 0.99609375]` |
| `BaseWood_Grey.IronTeeth` | wood | wood | BaseWood_1K_D_Grey.png | `[0.00390625, 0.56640625, 0.5, 0.99609375]` |
| `BaseWood_LightBrown.Folktails` | wood | wood | BaseWood_1K_D_White.png | `[0.00390625, 0.56640625, 0.5, 0.99609375]` |
| `Details.Folktails` | detail | details | Details_D.Folktails.png | `[0.00390625, 0.01171875, 0.49609375, 0.49609375]` |
| `Details.IronTeeth` | detail | details | Details_D.IronTeeth.png | `[0.00390625, 0.01171875, 0.49609375, 0.49609375]` |
| `WindowsAtlas.Folktails` | glass | glass | WindowsAtlas_D.Folktails.png | `[0.75390625, 0.00390625, 0.99609375, 0.24609375]` |
| `WindowsAtlas.IronTeeth` | glass | glass | WindowsAtlas_D.IronTeeth.png | `[0.75390625, 0.00390625, 0.99609375, 0.24609375]` |

註：T1/T2 為全木構造，`wood`／`metal`／`detail` 三個 slot 全部映射到該陣營的兩種木質材質（`metal` 與 `detail` 共用木色飾材），仍維持「2 不透明」上限；T4 玻璃使用 `WindowsAtlas`（原版材質，非新增貼圖）。色調受原版貼圖與材質 tint 限制，與 §16.3 的目標 hex 可能有差異（風險見 §9）。

## 5. 端口與掛點

### 5.1 `TransputProviderSpec`（每棟 5 個：左／右／後三側面中央 + 底面 2 點；正面不設）

| 尺寸 | Directions | Coordinates（格，Z=高度層） | 對應面 |
|---|---|---|---|
| Small | Left | (0, 1, 0) | 左 -X 面 |
| Small | Right | (1, 1, 0) | 右 +X 面 |
| Small | Up | (1, 1, 0) | 後 +Y 面（尺寸換算 X=floor(X/2), Y=Y-1） |
| Small | Bottom | (0, 0, 0) | 底面 -Z |
| Small | Bottom | (1, 0, 0) | 底面 -Z |
| Medium | Left | (0, 1, 0) | 左 -X 面 |
| Medium | Right | (2, 1, 0) | 右 +X 面 |
| Medium | Up | (1, 2, 0) | 後 +Y 面（尺寸換算 X=floor(X/2), Y=Y-1） |
| Medium | Bottom | (1, 1, 0) | 底面 -Z |
| Medium | Bottom | (1, 0, 0) | 底面 -Z |
| Large | Left | (0, 2, 0) | 左 -X 面 |
| Large | Right | (3, 2, 0) | 右 +X 面 |
| Large | Up | (2, 3, 0) | 後 +Y 面（尺寸換算 X=floor(X/2), Y=Y-1） |
| Large | Bottom | (2, 2, 0) | 底面 -Z |
| Large | Bottom | (2, 0, 0) | 底面 -Z |

驗證：`validate_art.py` 逐檔核對 5 個 transput、恰好 1 個 `Up` 後側點、無 `Top` 正面點；`tools/validate` 的 `BoilerBlueprintChecks` 另以 `BlockObjectSpec.Size` 反算後側槽位。尺寸換算沿用原版 `SteamEngine.IronTeeth` 的母版比例（左 (0,1,0)、右 (X-1,1,0)、後 (floor(X/2),Y-1,0)、底面 (1,1,0)/(1,0,0)）。

### 5.2 模型掛點（empty nodes）

| 掛點 | 用途 | 對應藍圖欄位 |
|---|---|---|
| `#EmptySmoke1..3` | 煙霧掛點（對應 3 根煙囪／通風管） | `TemplateAttachmentsSpec.Attachments[].Parent` + `PowerGeneratorParticleControllerSpec.AttachmentIds` |
| `ATT_WaterIn` | 後側水艙接管面參考 | 視覺標記 |
| `ATT_FuelIn` | 左前燃料帶端面參考 | 視覺標記 |
| `ATT_Entrance` | 正面入口參考 | 視覺標記 |
| `ATT_FlywheelPivot` | 飛輪樞軸參考 | 動畫接口 |

24 檔全部具備上述 7 個 empty；煙霧掛點座標取自實際煙囪／通風管頂端（`#EmptySmoke1..3`，模型空間 X/Y/Z）。煙霧 prefab 沿用原版 `Buildings/Power/SteamEngine/SteamEngineSmoke`；入口前（正面 -Y）不設幾何，模型邊界盒 min >= 0 且不超出占地。

## 6. 動畫（C-9）

### 6.1 已交付（模型／藍圖層）

| 項目 | 內容 | 依據 |
|---|---|---|
| 零骨架 | `.timbermesh` 無 skin／bone 資料；可動件為獨立具名子節點 | C-9 ① / REQ-ART-11 |
| 飛輪 | `ANIM_SEE_Flywheel` 節點，樞軸 `(W/2, fly_y, D/2)`，繞 X 軸；T1/T4 單碟、T2/T3 雙碟皆安裝在機身 +X 外側（不遮擋） | §16.9.1 |
| 曲軸 | `ANIM_SEE_Crankshaft` 節點，與飛輪同軸同步 | §16.9.1 |
| 主旋轉綁定 | 24 份藍圖均含原版 `GoodPoweredGeneratorAnimatorSpec`（與原版蒸汽機同構），藍圖不寫死轉速 | C-9 ② / REQ-ART-12 |
| 內建 node animation | `Default` clip，30 fps、31 幀、0-360 度循環（LOD0/LOD1 有，LOD2 無） | C-9 ② |
| 活塞／連桿 | `ANIM_SEE_Piston1..4` / `ANIM_SEE_ConnectingRod1..4` 獨立 Transform（缸數 T1 1、T2 2、T3/T4 4） | C-9 ③ / REQ-ART-14 |
| 煙霧 | 3 個掛點 + 原版 `SteamEngineSmoke` prefab + `PowerGeneratorParticleControllerSpec` | C-9 ⑥ / REQ-ART-17 |
| 動畫 LOD 距離 | 18 m / 40 m（`SEE_Config.json` anim.lod0MaxDistance / lod1MaxDistance） | C-9 ⑦ / REQ-ART-18 |

### 6.2 程式層接口（本卡只提供 Transform 名稱；控制器改動由 Lead 另開卡）

| 接口 | 用途 | 狀態 |
|---|---|---|
| `ANIM_SEE_Flywheel` / `ANIM_SEE_Crankshaft` | 由 `GoodPoweredGeneratorAnimatorSpec` 或控制器驅動轉速 | 模型已提供 |
| `ANIM_SEE_Piston1..4` | 程序化 sin 往復（沿模型 Y 軸，LOD0 30 Hz；T2 90°、T3/T4 四缸 0/90/180/270°） | 模型已提供；sin 由程式層實作 |
| `ANIM_SEE_ConnectingRod1..4` | 由活塞位置反解擺角（XY 平面） | 模型已提供 |
| `ATT_FlywheelPivot` | 飛輪樞軸參考 | 模型已提供 |

> 啟動／停機／暫停／維護的緩動、機況四階抖動（±0.5/1.5/3/5 度）、壓力錶指針（5 Hz）、調速器（2x 曲軸）、排氣脈衝與 VFX emission 皆屬**程式層**（P3 控制器／UI），本卡只交付可綁定的 Transform 與 spec 宣告，未改任何程式邏輯。

### 6.3 藍圖動畫／VFX spec（24 檔全數具備）

| spec | 內容 |
|---|---|
| `GoodPoweredGeneratorAnimatorSpec` | `{}`（原版同構；綁定飛輪／曲軸主旋轉） |
| `PowerGeneratorParticleControllerSpec` | `AttachmentIds = [Smoke1, Smoke2, Smoke3]` |
| `TemplateAttachmentsSpec` | 3 筆，Parent=`#EmptySmoke1..3`，Prefab=`Buildings/Power/SteamEngine/SteamEngineSmoke`，`CreateInstantly=true` |

## 7. 圖示與預覽

### 7.1 圖示（24 張，替換 `data/Icons/` 佔位）

| 圖示 | bytes | 尺寸 | meta |
|---|---:|---|---|
| `SteamBoilerSmallT1Folktails.png` | 20.9 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerSmallT1IronTeeth.png` | 20.5 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerSmallT2Folktails.png` | 20.5 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerSmallT2IronTeeth.png` | 20.2 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerSmallT3Folktails.png` | 20.6 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerSmallT3IronTeeth.png` | 20.0 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerSmallT4Folktails.png` | 21.0 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerSmallT4IronTeeth.png` | 20.4 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerMediumT1Folktails.png` | 22.1 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerMediumT1IronTeeth.png` | 21.8 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerMediumT2Folktails.png` | 22.6 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerMediumT2IronTeeth.png` | 22.2 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerMediumT3Folktails.png` | 22.8 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerMediumT3IronTeeth.png` | 22.1 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerMediumT4Folktails.png` | 23.0 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerMediumT4IronTeeth.png` | 22.5 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerLargeT1Folktails.png` | 23.0 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerLargeT1IronTeeth.png` | 22.6 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerLargeT2Folktails.png` | 22.5 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerLargeT2IronTeeth.png` | 22.1 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerLargeT3Folktails.png` | 23.0 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerLargeT3IronTeeth.png` | 22.4 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerLargeT4Folktails.png` | 23.7 KB | 128x128 RGBA | `{"isSprite": true}` |
| `SteamBoilerLargeT4IronTeeth.png` | 23.2 KB | 128x128 RGBA | `{"isSprite": true}` |

圖示由 Blender 4.2.1 headless（Cycles CPU）依 LOD0 幾何 + 原版材質 patch 離線渲染，透明背景、無文字；`tools/generate-blueprints` 的 P9 ingest hook 會把 `mod/art/icons/<IconName>.png` 原樣複製到 `data/Icons/`，因此產生器 `--check` 零 diff。

### 7.2 抽樣預覽圖（6 張）

| 預覽 | 檔案 | bytes |
|---|---|---:|
| 1 | `mod/art/preview/SteamBoiler.Small.T1.Folktails_iso.png` | 366.0 KB |
| 2 | `mod/art/preview/SteamBoiler.Small.T3.IronTeeth_iso.png` | 357.7 KB |
| 3 | `mod/art/preview/SteamBoiler.Medium.T2.Folktails_iso.png` | 370.1 KB |
| 4 | `mod/art/preview/SteamBoiler.Medium.T4.IronTeeth_iso.png` | 371.4 KB |
| 5 | `mod/art/preview/SteamBoiler.Large.T1.Folktails_iso.png` | 372.5 KB |
| 6 | `mod/art/preview/SteamBoiler.Large.T4.IronTeeth_iso.png` | 373.4 KB |

> 預覽圖為 Blender headless 離線渲染（直接讀取已匯出的 `.Model.timbermesh`，含節點世界變換；靜止姿勢不套用 `Default` 動畫），**不是**遊戲內截圖；實機外觀仍需 P8 人類 gate。

另附**全變體 gallery**（非 §7.2 的 6 張交付，供人工比對 12 造型 × 2 陣營）：
`mod/art/preview/gallery/` 共 24 張 640x480 模型圖＋2 張 4x3 contact sheet
（`Folktails_grid.png`／`IronTeeth_grid.png`，含 T1-T4 欄與 Small/Medium/Large 列）。

## 8. 驗證證據

### 8.1 美術驗證器（`python mod/art/tools/validate_art.py`）

```text
SEE P9 art validation - art
==============================================================================

==============================================================================
finished LOD0 tris: total=87768  min=2332  max=4956
  Small  LOD0 max=3496  budget=3500  headroom=   4
  Medium LOD0 max=4376  budget=6000  headroom=1624
  Large  LOD0 max=4956  budget=9000  headroom=4044

Checks: 6545/6545 passed, 0 failed
RESULT: PASS
```

### 8.2 專案門禁（`build.ps1 -Clean`）

> 本機沒有 PowerShell 7（`pwsh`），改用 Windows PowerShell 5.1：`powershell -NoProfile -ExecutionPolicy Bypass -File mod/build.ps1 -Clean`。

```text
  gate G01 PASS  0 warnings x 4 shipping projects
CHECK PASS (0 differences, 80 files, 24 blueprints)
  gate G03 PASS  CHECK PASS (0 differences)
VALIDATION PASSED
  gate G05 PASS  VALIDATION PASSED
  gate G21 PASS  24 png icons + 24 isSprite metas
  gate G22 PASS  version tracks 0.1.0/1/1, package hash 6d6ee3858cb9...
  G01  PASS  0 warnings in all four shipping assemblies
  G05  PASS  data validator prints VALIDATION PASSED for every gate group
  G21  PASS  staged icons: 24 PNG sprites plus 24 .png.meta.json with isSprite=true
BUILD PASSED
  files       : 132
  size        : 2,322 KB
  timbermesh  : 48 (P9 art deliverable; not required for v1)
  package sha : 6d6ee3858cb93987a3355fdcf449d65951828cc3dc95c4db293e926e0b3ed57c
```

### 8.3 檢查項對照

| 檢查 | 方式 | 結果 |
|---|---|---|
| 檔案存在／數量 | `validate_art.py` A 節：24 成品 + 24 灰盒 + 48 LOD + 圖示 | PASS |
| `.timbermesh` 結構 | zlib+protobuf 解析、單根、父節點順序、屬性維度、winding 與法線一致 | PASS |
| 面數預算 | LOD0 <= 3,500/6,000/9,000；LOD1 <= 50%；LOD2 <= 15% | PASS |
| 材質白名單 | 由原版 `MaterialCollection` 藍圖解析白名單，逐材質比對 | PASS |
| 端口／掛點 | 5 transput + 7 empty + 煙霧 attachments + 動畫 spec | PASS |
| 藍圖路徑 | 24 檔成品／灰盒 `TimbermeshSpec.Model` 指向自製模型 | PASS |
| 圖示 | 24 x 128x128、`isSprite`、與 Blender 渲染逐位元相同 | PASS |
| 藍圖 diff | 與 P8 基線逐葉比對，只允許模型路徑／端口／掛點／動畫 spec 變更 | PASS（見 8.4） |

### 8.4 藍圖 diff（P8 基線 → P9）

基線：`mod/build/_install-backup-20261106/SteamEngineExpansion.bak-20261106-214744/`（P8 安裝前的 shipped 藍圖快照）。逐檔逐葉比對，允許變更只限：模型路徑、`TransputProviderSpec`、`TemplateAttachmentsSpec`、`PowerGeneratorParticleControllerSpec`、`GoodPoweredGeneratorAnimatorSpec`、`LabeledEntitySpec.Icon`、`CollidersSpec`。

```text
SUMMARY
  files compared          : 24
  changed leaf values     : 200
  art-only allow-list     : Children.#Finished.TimbermeshSpec.Model, Children.#Unfinished.Children.ConstructionStage0.TimbermeshSpec.Model, TransputProviderSpec, TemplateAttachmentsSpec, PowerGeneratorParticleControllerSpec, GoodPoweredGeneratorAnimatorSpec, LabeledEntitySpec.Icon, Children.#Finished.CollidersSpec, Children.#Unfinished.Children.ConstructionStage0.CollidersSpec
  disallowed changes      : 0

RESULT: PASS
```

> 24 份藍圖的玩法數值（PowerOutput、燃料／水率、成本、尺寸、loc key…）**零變更**；P9 只動模型路徑與掛點／端口宣告。

### 8.5 指令

```powershell
python mod/art/gen_models.py                 # 產生 48 LOD0 + 48 LOD1/2 + manifest
python mod/art/tools/validate_art.py         # 美術驗證（本節 8.1）
python mod/art/tools/diff_blueprints_vs_p8.py  # 藍圖 diff（本節 8.4）
python mod/art/tools/write_art_report.py     # 產生本報告
blender.exe -b --factory-startup -noaudio --python mod/art/render_previews.py  # 24 圖示 + 6 預覽
powershell -NoProfile -File mod/build.ps1 -Clean  # 專案門禁（本節 8.2）
```

## 9. 風險與待驗

| # | 風險／待驗 | 影響 | 緩解／下一步 |
|---:|---|---|---|
| 1 | **實機外觀未驗**：材質 UV patch、winding、LOD 由 Unity prefab optimizer 產生，離線無法證實遊戲內觀感 | 外觀可能與離線預覽有差異 | P8 人類實機 gate 或後續人類確認；本卡不宣告外觀通過 |
| 2 | `GoodPoweredGeneratorAnimatorSpec` 的 runtime 行為（帶動哪個 Transform／clip、轉速來源）未能在離線 IL 完全證實；模型端已內建同名同構的 `Default` node animation | 實機可能不動或轉速不如預期 | 實機確認；若 spec 不吃 node animation，改用 `WindRotationAnimatorSpec{TransformName,RotationAxis,RotationSpeed}` 或由控制器驅動（屬程式改動，Lead 開卡） |
| 3 | 活塞 sin、相位、起停緩動、機況四階回饋、指針、調速器、VFX emission 皆為程式層 | 本卡只交付 Transform／spec，不動畫 | 由 P3/P4 控制器與 VFX 實作；本卡提供 `ANIM_SEE_*` 名稱與 `ATT_FlywheelPivot` |
| 4 | LOD1/LOD2 以獨立 `.timbermesh` 交付於 `mod/art/lod/`；遊戲執行時只載入 `TimbermeshSpec.Model` 單一檔案 | 若不經 Unity prefab optimizer／AssetBundle，遊戲內只會看到 LOD0；LOD2「不投影」也需 prefab 設定 | 本卡照需求交付 LOD 來源與命名（`.Model.LOD1/.LOD2`）；整合方式待 P8/後續卡 |
| 5 | 材質只能用原版 `MaterialCollection` 與原版貼圖 patch，色調受原版資產限制 | 與 §16.3 的目標 hex 可能有色差 | 以原版可用材質為準；若需精準色票，須走 HDBF 已驗證的自製材質流程（本卡未做，避免破壞原版材質引用） |
| 6 | UV 為各原版材質貼圖的固定 patch（同一材質的面共用 patch），非 §16.5 的「UV 不重疊專屬圖集」 | 同一材質的面會取樣相同貼圖區域；原版材質多為可平鋪紋理，遠看可接受 | 若需完全符合「UV 不重疊」，要新增 4 張手繪圖集（§16.4），屬自製材質流程，另開卡 |
| 7 | Small T1 LOD0 = 3,496 / 3,500 tris（餘裕僅 4） | 後續加裝飾即破預算 | 任何加件先跑 `validate_art.py`；必要時降 Small 裝飾 |
| 8 | 平面著色（flat）與需求 §16.5 的「法線平滑角 60 度」不同 | 硬邊較多、觀感較稜角 | 低多邊形手繪風格可接受；若需平滑，於 LOD1/2 或後續流程調整（不影響面數） |
| 9 | 圖示為離線渲染，非遊戲內截圖 | 與遊戲內實際材質光照可能不同 | 實機確認後可重新渲染替換（ingest hook 會自動同步 `data/Icons/`） |
| 10 | `build.ps1` G11 的**標籤文字**仍寫「no Up/Top transput」，但實作檢查（`BoilerBlueprintChecks`）依原版證據只禁 `Top`、允許後側 `Up` | 門禁文字與實作不一致，可能誤導閱讀者；實際 gate 為 PASS | 建議 Lead 更新 G11 標籤文字（屬 build.ps1，非本卡寫入範圍） |

> 本卡結論：**離線可驗證項全部 PASS；實機外觀與動畫 runtime 行為未宣告通過**，需 P8 人類 gate 或後續人類確認。
