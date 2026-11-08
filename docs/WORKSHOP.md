# Steam Workshop 頁面文案草稿（v0.1.0）

> 狀態：**草稿，尚未發佈**。張貼前必須完成：
> ① 人類依 `docs/TEST_PROTOCOL.md` 完成 17.5 十二步並簽核；② P9 自製模型／動畫／正式圖示替換佔位；
> ③ §19.2 Go/No-Go 全 PASS（目前為 **No-Go**）；④ 用語校對（「蒸汽引擎」用於本模組、「蒸汽機」指原版）。
> 本文件僅供審閱，**不代表已通過實機驗收或已上架**。
>
> **2026-11-08 更新（熱修-5）**：實際工作坊頁面文案以 `mod/workshop/DESCRIPTION.md`（簡中 BBCode）與
> `mod/workshop/DESCRIPTION.en.md`（English）為準；本文件為繁中草稿。熱修-5 已在正式文案加入「更新」段
> （完工引擎移除原版「需要搬運工才能運轉」狀態、全 12 檔耗水減半、水艙同步重算）與已知限制
> （擺放預覽仍顯示原版說明列）。Steam 工作坊更新只能由擁有者在遊戲內 Mods 管理器按 **Update**
> （Steam 無公開發佈 API），前置檔案與步驟見 `mod/workshop/README.md`。

---

## 1. 標題

**繁體中文（主標題）**

> 蒸汽引擎擴充（Steam Engine Expansion）— 12 檔免工人蒸汽引擎、兩陣營、含維護

**英文（副標題／工作坊 Title 欄）**

> Steam Engine Expansion — 12 worker-free steam engines for Folktails & Iron Teeth

> 備註：工作坊 Title 建議 ≤ 80 字元；上列英文 71 字元、中文 42 字，皆在常見上限內。

---

## 2. 簡短描述

### 繁體中文（≤ 200 字）

> 新增 12 種免工人蒸汽引擎（3 尺寸 × 4 等級），兩陣營皆可建。以原木／木板與水換取 130–1500 馬力，
> 燃料消耗隨負載比例調整；低負載省料，只要燃料與水充足，**夜間無電網需求時仍以 10% 待機運轉、不熄火**
> （可在 `SEE_Config.json` 把 `standbyLoadRatio` 設 0 回到完全不耗料），但每棟都需要定期維護（機況、材料、停機）。
> Timber Pipes／Power Lines／Conveyor Belts v2 皆為選用軟依賴，全部缺席時仍可完整遊玩。
> 不覆寫原版蒸汽機。

### English（short description）

> Twelve new worker-free steam engines (3 sizes × 4 tiers) for both factions, producing 130–1500 hp from Log/Plank fuel and Water. Fuel use scales with load; as long as fuel and water last the engine keeps running at a 10% standby load overnight instead of stopping (set `standbyLoadRatio` to 0 for the old idle-without-consumption behaviour), and every engine needs periodic maintenance. Optional soft support for Timber Pipes, Power Lines and Conveyor Belts v2 — fully playable without them. Does not overwrite the vanilla Steam Engine.

---

## 3. 長描述（繁體中文）

### 蒸汽引擎擴充（SEE）

Timberborn 的動力與飲水都綁在人力上：多蓋水泵、多蓋踩輪，其實在搶同一批海狸。本模組把動力端變成
**不需工人的基礎設施**：燃料＋水 → 動能，再接到你的機械水泵與工廠。代價不是人力，而是**燃料、水與維護**。

#### 12 檔 × 2 陣營

| 尺寸 | 佔地 | 等級 | 輸出（FT／IT） | 燃料 | 維護間隔 |
|---|---|---|---|---|---|
| 小型 | 2×2×3 | Mk.I–Mk.IV | 130–520／137–546 hp | Log（Mk.III／IV 可 Plank） | 6–12 日 |
| 中型 | 3×3×3 | Mk.I–Mk.IV | 230–920／242–966 hp | Log／Plank | 6–12 日 |
| 大型 | 4×4×4 | Mk.I–Mk.IV | 360–1500／378–1575 hp | Log／Plank | 6–12 日 |

- Folktails：輸出 ×1.00、燃料 ×0.90；Iron Teeth：輸出 ×1.05、燃料 ×1.00。
- 燃料庫存＝`ceil(燃料/h × 25)`；水艙以「200 小時消耗量」設計（獨立 `WaterTank` public input 庫存），
  燃料與水都由**普通搬運工**補給（2026-11-08 修根因；Timber Pipes 的閥門／管網暫不接管水艙）。
- T3／T4 可燒木板（1 木板＝2 原木能量），讓中後期的木板產線有去處。

#### 核心循環

1. **建造**（研究點 90–2250，依尺寸與等級）。
2. **接燃料與水**：燃料走原版 `GoodConsumingBuildingSpec` 庫存（搬運工；裝了 Conveyor Belts v2 可自動餵料）；
   水走獨立 `WaterTank` public input 庫存（搬運工）。兩者是分開的搬運目標，普通搬運工都能送。
3. **發電**：接到機械網路；低負載時燃料與水按比例下降；只要燃料與水充足，**入夜後電網需求 0 仍以 10% 待機運轉**
   （`balance.standbyLoadRatio`，設 0 可回到「需求 0 待機不耗料」的舊行為）。
4. **維護**：機況 100→0，只在帶載運轉時磨損（含夜間待機）；自動維護在 50 觸發，需材料與道路，停機 2 遊戲小時；
   沒有隨機故障，所有代價可預先計算。

#### 維護不是裝飾

| 等級 | 維護間隔 | 每次材料（不新增物品） |
|---|---|---|
| Mk.I | 6 日 | Log 8 |
| Mk.II | 8 日 | Plank 12＋Gear 2 |
| Mk.III | 10 日 | MetalBlock 10＋Plank 10 |
| Mk.IV | 12 日 | MetalBlock 12＋Gear 6 |

- 缺料 → **缺少維護材料**、無路 → **無道路可達**；兩者都**不扣材料、不崩潰**。
- 機況乘數：61–100 ×1.00、41–60 ×0.93、21–40 ×0.85、1–20 ×0.40、0 停機。
- 可切換自動／手動維護，也可暫停引擎（暫停期間不耗料、不磨損）。

#### 三模組軟依賴

- **Timber Pipes**：v1 暫不接管水艙（水艙不是液體 `StockpileSpec`，槽管只能接 Stockpile）；供水一律由搬運工負責。
- **Power Lines**：偵測到才注入連接點；注入失敗退回原版節點，不崩潰、只記一次日誌。
- **Conveyor Belts v2**：燃料列入輸入貨物，帶子可自動餵料。
- 三者**全部缺席**時仍完整可玩；不會把 SEE 變成硬依賴，也不 Harmony patch 機械圖。

#### 差異化

- **不覆寫原版蒸汽機**（Iron Teeth、400 hp）；只新增自己的模板與本地化鍵。
- 對照原版：中型 T2（460 hp）輸出 +15%、研究點同為 400，但多了維護與兩陣營選擇；
  大型 Mk.IV（1500 hp）以「1.00× 輸出、1.56× 每 hp 燃料效率、64 格 vs 約 101 格」定位，不靠單棟碾壓。
- 不新增燃料、不新增物品、不新增工種。

#### 目前版本與已知限制（請務必閱讀）

- **v0.1.0 尚未完成實機驗收**；本頁上架前會完成 17.5 驗收與美術替換。若你拿到的是測試版，
  請照壓縮檔內的 `docs/TEST_PROTOCOL.md` 回報。
- 模型與圖示為**原版佔位**（Folktails 用水車模型；Iron Teeth 用原版蒸汽機模型），動畫與 LOD 之後才更新。
- Timber Pipes 的 **`tank` 自動補水尚未預設啟用**；v1 請用 One-way Valve 補水，或改用搬運工。
- 存檔採混合形態（世界節點索引＋逐棟狀態）；移除模組後載入的行為仍在實機驗證。
- 平衡與效能數據來自離線模擬；1,000 棟實機壓測與 30 日實機結果會在驗收後補上。

---

## 4. 重點圖（Key Image）與截圖建議

### 4.1 Key Image（工作坊封面，建議 1280×720 或 16:9）

- **主題**：大型 Mk.IV 蒸汽引擎 3/4 視角，日間、煙囪冒煙、飛輪運轉；背景是一條接到機械工廠的傳動軸，
  遠景有 Timber Pipes 管線與輸送帶（示意「三個自動化鉤子」）。
- **構圖**：建築占畫面右 2/3，左側留 1/3 給標題文字（「Steam Engine Expansion」＋「12 engines · 2 factions」）。
- **色調**：暖色木頭＋鐵灰，避免與原版蒸汽機的紅色警示混淆；不要放 UI 面板（縮圖會看不清）。
- **狀態**：等 P9 自製模型到位再拍攝；目前佔位模型（WaterWheel／原版蒸汽機）**不適合**當封面。
- **必備元素**：至少 2 檔尺寸同框（例如大型 Mk.IV＋小型 Mk.II），讓玩家一眼看出有 12 檔。

### 4.2 頁面截圖（4–6 張，P9 後重拍）

1. **工具列**：Power 分類、12 檔排在原版蒸汽機之後（證明不覆寫、排序正確）。
2. **面板**：狀態列＋輸出／電網供需／燃料剩餘時數／水（含 m³ 與「≈N 隻海狸飲水」）／機況與維護材料。
3. **雙補給**：搬運工把 Log 與水分別送進燃料庫存與 `WaterTank`（或 Conveyor Belts 餵 Log）。
4. **維護中**：`Maintaining` 進度與 2 小時倒數；旁邊顯示材料扣除。
5. **缺料／無路**：`MissingMaintenanceMaterials`／`NoAccessForMaintenance` 的紅字與修復指引。
6. **兩陣營對照**：同一檔位 FT／IT 外觀與材質差異（P9 後才成立）。

### 4.3 短 GIF／影片（可選）

- 8–12 秒：引擎啟動（0→滿載）、負載下降時燃料列同步下降、暫停後停止耗料。
- 另有 P9 後的「飛輪＋活塞相位」與「LOD 15／30／45 m」短片，可作為第二支媒體。

---

## 5. 標籤與分類

`Timberborn` `Power` `Steam` `Automation` `Maintenance` `IronTeeth` `Folktails` `QoL` `Buildings`

---

## 6. 頁面用語檢查（上架前）

- [ ] 本模組一律稱「蒸汽引擎」；原版稱「蒸汽機」，不混用。
- [ ] 不寫「與原版蒸汽機相同」或「取代原版」；差異化說法照 §3。
- [ ] 不宣稱「已通過全部實機測試」；只寫「離線驗證完成、實機驗收進行中」直到簽核。
- [ ] 12 檔數值、維護材料、研究點與 `mod/data/engines.json` 一致（以生成器輸出為準，不手抄）。
- [ ] 外部模組名稱與工作坊連結正確：Timber Pipes `3801726769`、Power Lines `3766893705`、
      Conveyor Belts v2 `3815376360`。
- [ ] 已知限制（模型佔位、`tank` 未預設、實機未簽核）必須留在頁面上，不可為了美觀刪除。
- [ ] 圖片不得使用原版蒸汽機的官方宣傳圖；截圖必須是實機畫面。

---

## 7. 英文版短文案（給非中文玩家）

**Title:** Steam Engine Expansion — 12 worker-free steam engines (Folktails & Iron Teeth)

**Short description:**
Twelve new steam engines across three sizes and four tiers, for both factions. Burn Log/Plank and Water for
130–1500 hp of mechanical power. Fuel scales with load, idle engines cost nothing, and every engine needs periodic
maintenance (condition, materials, downtime). Optional soft support for Timber Pipes, Power Lines and Conveyor
Belts v2 — fully playable without them. Does not overwrite the vanilla Steam Engine.

**Notes for the page:**
- v0.1.0 is not yet through human in-game acceptance; this page will be published only after it is.
- Models/icons in the current build are vanilla placeholders; custom meshes, LODs and animation are planned.
- Timber Pipes tank/valve are not supported in v1 — haulers supply both fuel and water.
