# SEE 需求追溯表（REQ Traceability，274 條）

> **用途**：把《蒸汽引擎模組需求文檔》§21.2 的 **274 條 REQ** 逐條對映到負責卡（SEE-P0～SEE-P9）與 P0 覆蓋狀態，
> 作為 P0 契約凍結的驗收證據，以及 P1–P9 卡片的「需求 → 實作位置 → 驗證方式 → 結果」起點
> （PLAN §7.7、TASKS.md）。
>
> **權威來源**：`_investigate/doc-extract.md` §11（行 1874–2236，274 條逐字清單與摘要）；數量與 §0.2／§21.2 相符。
> **搭配契約**：`mod/docs/CONTRACT_SEE.md`（本表「P0 狀態」欄引用的 §章節皆指該檔）。
> **注意**：本表摘要為**可實作要點**，不是需求全文；驗收細節以需求文檔各章為準，數值一律以需求／簡報／PLAN 為準。

---

## 0. 欄位定義與圖例

| 欄位 | 定義 |
|---|---|
| **REQ ID** | `REQ-<域>-<兩位序號>`；域共 18 個（CORE／BLD／BAL／TECH／FUEL／POWER／MAINT／COMPAT／ARCH／NFR／SAFE／IMPL／SAVE／UI／OPS／ART／TEST／RISK） |
| **一句摘要** | 該 REQ 的可實作要點（來源：doc-extract §11；不新增需求） |
| **負責卡** | 完成該 REQ 驗收的**主要卡**（P0–P9）；跨卡者以「主卡＋協作卡」表示（如 `P1＋P3`）。**§1 統計以主卡計。** |
| **P0 是否已覆蓋／凍結** | 見下表圖例；**✅ 只代表 P0 已在 CONTRACT_SEE 凍結介面／規則／數值，不代表已實作**。 |

**P0 狀態圖例**

| 標記 | 意義 |
|---|---|
| ✅ | **契約凍結**：介面／schema／數值／規則已寫入 `CONTRACT_SEE.md` 對應章節（✅ 後標章節） |
| 🔬 | **已證實**：P0 以 IL 或取證確認（`API_FACTS_SEE.md`／`_investigate`），並已在契約登錄 |
| ⏳ | **待取證／待裁決**：D-1～D-6 或 AD 系列開放項（`CONTRACT_SEE.md` §11） |
| 🔧 | **待實機**：只能由 P8 人類實機 gate 驗證（AI 不得宣告通過） |
| — | **P0 不涵蓋**：由後續卡實作／驗收，P0 不凍結該項細節 |

**統計規則**
1. 每條 REQ **恰好一個主要卡**；`P1＋P3` 等跨卡者以斜線前的主卡（P1）計入 §1 統計，協作職責另註。
2. 域清點以需求 §0.2／§21.2 為準：**18 個域、274 條**（任務描述曾記「17 域」，依清點為 18；合計不變）。
3. 任何新增／修改 REQ 時，本表、核對行與 `CONTRACT_SEE.md` 必須同步更新；由 Lead 核准。

---

## 1. 數量核對（自算驗證）

### 1.1 域 × 主要卡 分布

| 域 | 條數 | P0 | P1 | P2 | P3 | P4 | P5 | P6 | P7 | P8 | P9 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| REQ-CORE | 12 | 0 | 3 | 4 | 3 | 1 | 0 | 1 | 0 | 0 | 0 |
| REQ-BLD | 14 | 0 | 14 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| REQ-BAL | 13 | 0 | 0 | 9 | 1 | 0 | 0 | 0 | 3 | 0 | 0 |
| REQ-TECH | 10 | 0 | 8 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 0 |
| REQ-FUEL | 20 | 0 | 9 | 0 | 7 | 0 | 4 | 0 | 0 | 0 | 0 |
| REQ-POWER | 18 | 0 | 4 | 2 | 8 | 1 | 3 | 0 | 0 | 0 | 0 |
| REQ-MAINT | 20 | 0 | 1 | 9 | 5 | 3 | 1 | 0 | 1 | 0 | 0 |
| REQ-COMPAT | 18 | 0 | 5 | 0 | 0 | 0 | 12 | 1 | 0 | 0 | 0 |
| REQ-ARCH | 14 | 4 | 6 | 1 | 0 | 0 | 0 | 3 | 0 | 0 | 0 |
| REQ-NFR | 18 | 4 | 1 | 2 | 1 | 1 | 1 | 8 | 0 | 0 | 0 |
| REQ-SAFE | 15 | 0 | 0 | 4 | 5 | 0 | 3 | 3 | 0 | 0 | 0 |
| REQ-IMPL | 18 | 3 | 2 | 8 | 5 | 0 | 0 | 0 | 0 | 0 | 0 |
| REQ-SAVE | 14 | 0 | 0 | 0 | 1 | 1 | 12 | 0 | 0 | 0 | 0 |
| REQ-UI | 12 | 0 | 0 | 0 | 0 | 12 | 0 | 0 | 0 | 0 | 0 |
| REQ-OPS | 8 | 0 | 0 | 0 | 0 | 4 | 1 | 3 | 0 | 0 | 0 |
| REQ-ART | 22 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 22 |
| REQ-TEST | 18 | 0 | 1 | 3 | 3 | 0 | 6 | 1 | 2 | 2 | 0 |
| REQ-RISK | 10 | 0 | 3 | 0 | 1 | 2 | 1 | 0 | 2 | 1 | 0 |
| **合計** | **274** | **11** | **57** | **42** | **40** | **25** | **44** | **20** | **9** | **4** | **22** |

### 1.2 卡片合計與核對行（逐項算術）

| 卡 | 條數 | 占比 |
|---|---:|---:|
| SEE-P0 | 11 | 4.0% |
| SEE-P1 | 57 | 20.8% |
| SEE-P2 | 42 | 15.3% |
| SEE-P3 | 40 | 14.6% |
| SEE-P4 | 25 | 9.1% |
| SEE-P5 | 44 | 16.1% |
| SEE-P6 | 20 | 7.3% |
| SEE-P7 | 9 | 3.3% |
| SEE-P8 | 4 | 1.5% |
| SEE-P9 | 22 | 8.0% |
| **合計** | **274** | **100%** |

**核對行（自行驗算）**

- 域合計：`12+14+13+10+20+18+20+18+14+18+15+18+14+12+8+22+18+10 = 274` ✓
- 卡合計：`11+57+42+40+25+44+20+9+4+22 = 274` ✓
- 交叉驗算：域分布表逐列加總＝1.2 卡片合計＝**274**；**無空白卡**（P0–P9 皆 >0）；**無未對映 REQ**（§2 共 274 列）。
- 跨卡註記：`REQ-BLD-09`、`REQ-FUEL-01/02/03` 為 `P1＋P3`（生成器＋消耗對帳，Lead 2026-11-06 指示），統計歸 P1。

---

## 2. 完整對映表（274 條，依域分組）

### 2.1 REQ-CORE（12 條；需求 §3.7）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-CORE-01 | 雙輸入條件：燃料庫存>0 且水艙>0 才輸出>0；清空其一 1 遊戲分鐘內輸出 0 | P2 | ✅ §8.3 |
| REQ-CORE-02 | 免工人：12 檔工人數皆 0（WorkplaceSpec 工人欄位 0 或不存在） | P1 | ✅ §4.3 |
| REQ-CORE-03 | 不產水：無產水配方／WaterMoverSpec；運轉 10 日 Water 只減不增 | P3 | ✅ §4.3 |
| REQ-CORE-04 | 檔位覆蓋：12 檔 130–1500 hp、每級三尺寸齊備；24 藍圖與 §4.1 一致 | P1 | ✅ §4.2 |
| REQ-CORE-05 | 不得開局可建：最低 ScienceCost = 90｜科學點 0 時工具列不出現 | P1 | ✅ §4.2 |
| REQ-CORE-06 | 狀態機優先序：九主狀態；Maintaining 第 0 級覆蓋態、不列入九主狀態；至少 8 組組合驗收 | P2 | ✅ §8.2 |
| REQ-CORE-07 | 比例耗燃料（預設）：負載 50% → 燃料與水皆 50%（±2%） | P2 | ✅ §8.3（λ 待 D-1 取證） |
| REQ-CORE-08 | 零需求停機：Idle，燃料／水／condition 24 遊戲小時不變 | P2 | ⚠ **2026-11-08 Lead 裁決 AD-16 改預設行為**：需求 0 仍以 `balance.standbyLoadRatio`（預設 0.10）待機運轉；`Idle` 只在未接電網或 `standbyLoadRatio=0` 時；見 `CONTRACT_SEE.md` §8.4／`LEAD_RULINGS_ADDENDUM.md` §七 |
| REQ-CORE-09 | 無水停機與恢復：輸出 0、Status.SEE.NoWater、不磨損；補水 1 分鐘內回復 | P3 | ✅ §8.3 |
| REQ-CORE-10 | 驅動原版機械水泵：≥500 hp 檔位可單獨驅動 MechanicalPump；可用集合限 6 檔 | P3 | 🔧 P8 |
| REQ-CORE-11 | 30 秒可判讀：單面板顯示燃料／水剩餘時數、機況%、下次維護、電網供需；輸出 0 可歸因四類 | P4 | — |
| REQ-CORE-12 | 不覆寫原版：SteamEngine.IronTeeth 藍圖與數值（400 hp／Log 0.1／Water 0.3／研究 400）不變 | P6 | ✅ §10 |

### 2.2 REQ-BLD（14 條；需求 §4.6）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-BLD-01 | 藍圖齊備與命名：24 份、TemplateName＝SteamBoiler.<Size>.<Tier>.<Faction>、路徑 Buildings/Power/SteamBoiler/ | P1 | ✅ §4.1／§2.7 |
| REQ-BLD-02 | 權威值零偏差：12 檔馬力／研究點／建造費／燃料／水／庫存／水艙／佔地／高度與 §4.1 逐值相同（零容差） | P1 | ✅ §4.2 |
| REQ-BLD-03 | 佔地與高度：2×2×3／3×3×3／4×4×4 | P1 | ✅ §4.4 |
| REQ-BLD-04 | 免工人：WorkplaceSpec 不存在或工人 0 | P1 | ✅ §4.3／§4.4 |
| REQ-BLD-05 | 動力節點：PowerOutput＝表定馬力、PowerInput=0、IsShaft=false | P1 | ✅ §4.3 |
| REQ-BLD-06 | 入口與可達：Entrance.HasEntrance=true 且正面臨路才可建；搬運工可達入口 | P1 | ✅ §4.3（實機 P8） |
| REQ-BLD-07 | 燃料接口：接受清單含 Log；Mk.III／IV 另含 Plank；搬運工與帶子皆可送入 | P1 | ✅ §4.7 |
| REQ-BLD-08 | 水接口分離：水艙為程式建立的 `WaterTank` public input Inventory（2026-11-08 修根因；不用 StockpileSpec），與燃料庫存為不同庫存 | P1＋P3 | ✅ §4.7（AD-14） |
| REQ-BLD-09 | 庫存容量：燃料＝ceil(燃料率×25)、水艙＝round(耗水率×200)；大型 Mk.I 水艙=58 | P1＋P3 | ✅ §3.5.2（AD-3 容量口徑）；P3 消耗對帳 |
| REQ-BLD-10 | 陣營輸出係數：FT ×1.00／IT ×1.05，馬力無條件進位（中型 Mk.I IT ceil(241.5)=242） | P1 | ✅ §4.2 |
| REQ-BLD-11 | 陣營燃料係數：FT ×0.90／IT ×1.00，四捨五入 2 位；量化偏差上限 4.8% 須寫入測試報告 | P1 | ✅ §4.2 |
| REQ-BLD-12 | 陣營其餘不變：研究點／建造費／耗水／維護間隔／維護材料兩陣營逐值相同 | P1 | ✅ §4.2 |
| REQ-BLD-13 | 顯示名與工具列：zhTW「小型蒸汽引擎 Mk.I」…「大型蒸汽引擎 Mk.IV」；沿用 Power 群組、ToolOrder 排原版之後 | P1 | ✅ §4.9／§5.2 |
| REQ-BLD-14 | 朝向與連接面：四向旋轉皆可放置；3 側面＋底面連接點旋轉後仍可接軸；水／燃料面不與動力面衝突 | P1 | ✅ §4.3（實機 P8） |

### 2.3 REQ-BAL（13 條；需求 §5.7）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-BAL-01 | 滿載輸出：12 檔滿載馬力 ≥ 對照馬力，誤差 −5% 內視為達標 | P2 | — |
| REQ-BAL-02 | 燃料效率：Mk.II 以上每 100 hp ≤0.025/h（派生速率量測；逐列回算 ±8% 捨入） | P2 | — |
| REQ-BAL-03 | 水效率：Mk.II 以上每 100 hp 耗水 ≤0.075/h | P2 | — |
| REQ-BAL-04 | Mk.I 燃料上限：0.032 ≤ 1.3×0.025 = 0.0325（餘裕 1.6%，敏感項） | P2 | — |
| REQ-BAL-05 | 30 日平均：12 檔 ≥ 對照 ×0.95（含維護停機與磨損）；大型 Mk.IV 記 +2.9% 餘裕 | P7 | 🔧 P8（30 日實機量測） |
| REQ-BAL-06 | 規模效應：同級內尺寸越大，研究點/hp 與 BCI/hp 單調下降；下限 ≥ 小型 Mk.I 的 40% | P2 | — |
| REQ-BAL-07 | 密度梯度：同尺寸內每格馬力嚴格遞增（小型 32.5<65<95<130 等） | P2 | — |
| REQ-BAL-08 | 燃料曲線：0.032>0.025>0.021>0.016，Mk.IV 相對 Mk.I 改善 ≥2.0× | P2 | — |
| REQ-BAL-09 | 水曲線：0.080>0.070>0.040>0.018，Mk.IV 相對 Mk.I 改善 ≥4.0× | P2 | — |
| REQ-BAL-10 | 維護負擔：滿載＋自動維護下 30 日停機占比 ≤3%（實測 0.69%–1.37%） | P7 | — |
| REQ-BAL-11 | 比例消耗一致：同一負載下燃料／水實際消耗比＝表定比（±2%）；100/75/50/25% 四點量測 | P3 | ✅ §8.3 |
| REQ-BAL-12 | 陣營平衡：以未取整乘數計，每 100 hp 燃料效率差 ≤6%、水效率差 ≤5% | P2 | — |
| REQ-BAL-13 | 頂級檔論證：大型 Mk.IV 每 hp 燃料優於原版 1.56×，且立體佔位 64 格 ≤ 3.75×27 = 101 格 | P7 | — |

### 2.4 REQ-TECH（10 條；需求 §6.5）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-TECH-01 | 成本逐值一致：90/150/225/240/400/420/600/700/900/1050/1500/2250；合計 8,525 | P1 | ✅ §4.2 |
| REQ-TECH-02 | 無免費檔：任一 ScienceCost > 0，最低 90 | P1 | ✅ §4.2 |
| REQ-TECH-03 | 沿用原版機制：只用 BuildingSpec.ScienceCost 逐棟解鎖，不新增時代／科技樹／科研建築 | P1 | ✅ §4.3 |
| REQ-TECH-04 | 科技帶重疊：Mk.I 90–225／Mk.II 240–600／Mk.III 420–1050／Mk.IV 900–2250，須與 150/400/700/1500 帶重疊 | P1 | ✅ §4.2 |
| REQ-TECH-05 | 單檔可玩：任一單檔解鎖後可獨立運作 10 遊戲日；不依賴同級其他檔或前置科技 | P1 | — |
| REQ-TECH-06 | 兩陣營同價：同檔位兩陣營 ScienceCost 完全相同 | P1 | ✅ §4.2 |
| REQ-TECH-07 | 原版共存：安裝後原版 SteamEngine 仍可建、仍 400 hp、工具列仍可見 | P8 | 🔧 P8 |
| REQ-TECH-08 | 不複製原版：不得同時滿足「3×3×3＋400 hp＋每 100hp 燃料 0.025＋研究點 400」四項 | P1 | ✅ §4.2 |
| REQ-TECH-09 | 投資回報：以 1 台 Observatory（192 點/日）計，中型 Mk.II（400）≤3 日、大型 Mk.IV（2250）≤12 日 | P7 | — |
| REQ-TECH-10 | 外部模組區隔：不得要求外部物品作燃料；Mk.III/IV 水效率須優於 TWSteamEngine 至少 2×（實測 2.68×） | P1 | — |

### 2.5 REQ-FUEL（20 條；需求 §7.10）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-FUEL-01 | 燃料白名單：T1/T2 只收 Log；T3/T4 收 Log 與 Plank；其他貨物一律不接受 | P1＋P3 | ✅ §3.2／§4.7（生成器＋對帳） |
| REQ-FUEL-02 | 燒 Plank 消耗率 = Log ×0.5（1 木板 = 2 原木能量） | P1＋P3 | ✅ §8.3（能量換算） |
| REQ-FUEL-03 | 燃料庫存 = ceil(基礎燃料率×25)，12 檔誤差 0 | P1＋P3 | 🔬 §3.5.2（D-3 已證實；AD-3 定容量口徑） |
| REQ-FUEL-04 | FullInventoryWorkHours = 25；滿載可支撐 25–29 運轉小時（誤差 ≤5%） | P1 | ✅ §4.3 |
| REQ-FUEL-05 | 只在帶載運轉扣除；Idle/Paused/NoFuel/NoWater/NeedsMaintenance/Maintaining 皆不扣 | P3 | ✅ §8.3 |
| REQ-FUEL-06 | 燃料扣除 = 表定率 × min(1, 實際輸出/額定)；負載 50% → 一半（±2%） | P3 | ✅ §8.3 |
| REQ-FUEL-07 | 水耗率符合 §3；無水時輸出 0、狀態 NoWater、燃料停止扣除 | P3 | ✅ §8.3 |
| REQ-FUEL-08 | 水艙 = round(耗水率×200)；12 檔誤差 0 | P1 | ✅ §3.5.1 |
| REQ-FUEL-09 | 每檔水艙 ≥ 燃料庫存 ×8（最小比值 ≥8） | P1 | ✅ §4.7 |
| REQ-FUEL-10 | 燃料庫存與水艙為兩個獨立庫存，不得共用宣告；水艙由 `SEEWaterInventoryInitializer` 建立、燃料仍為 GCB | P3 | ✅ §4.7（AD-14） |
| REQ-FUEL-11 | 水艙容量＝`SEE.SEEBoilerSpec.WaterCapacityGoods`；庫存由 `SEEWaterInventoryInitializer` 建立、`Water` 為 giveable-and-takeable public input；不放 `StockpileSpec`／`BuildingPipeSpec` | P1＋P3 | ✅ §4.3／§4.7（AD-14） |
| REQ-FUEL-12 | tank 模式：V-01/V-02 通過後，管道接上即自動補水與排水，不需閥門 | P5 | — |
| REQ-FUEL-13 | valve 模式：One-way Valve 指向引擎並勾選 Fill 時可補水；未勾選不動作 | P5 | — |
| REQ-FUEL-14 | hauler 模式：搬運工由入口補水（`WaterTank`）與補燃料（GCB），連續 3 日成功；離線 wiring 由 `WaterTankSupplyTests` 鎖定，實機待簽核 | P5 | 離線 ✅／實機待驗（AD-15） |
| REQ-FUEL-15 | 供水模式可即時切換、不清空水艙、可存檔；預設 auto 且依 §7.7 降級 | P5 | — |
| REQ-FUEL-16 | 燃料列進 InputGoods（Log/Plank），裝 CB 時可被帶端自動餵料，不需程式配接 | P1 | ✅ §4.3 |
| REQ-FUEL-17 | NoFuel／NoWater 互不扣另一項資源、不磨損、可自動恢復 | P3 | ✅ §8.3 |
| REQ-FUEL-18 | 庫存滿停止收料不浪費；剩餘 <8 遊戲小時顯示 Status.SEE.LowFuel | P3 | ✅ §5.2 |
| REQ-FUEL-19 | 放置需平坦地面、正面入口連通同 district 道路；無路時不可補料 | P3 | ✅ §4.3 |
| REQ-FUEL-20 | 動力面、水接口面、燃料帶端面可同時存在且互不阻擋；UI 教學標示各面用途 | P1 | ✅ §4.3 |

### 2.6 REQ-POWER（18 條；需求 §8.9）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-POWER-01 | 每檔輸出＝額定×陣營係數（FT 1.00／IT 1.05），無條件進位後寫入 PowerOutput（24 份比對） | P1 | ✅ §4.3 |
| REQ-POWER-02 | PowerInput = 0、IsShaft = false | P1 | ✅ §4.3 |
| REQ-POWER-03 | 不宣告 WorkplaceSpec／WorkerSpec；免工人 | P1 | ✅ §4.3 |
| REQ-POWER-04 | 左/右/後三側面與底面設傳輸點、正面不設；IgnoreRotation = true | P1 | ✅ §4.3 |
| REQ-POWER-05 | 與原版傳動軸、相鄰耗電建築自動導通，不需額外設定 | P3 | ✅ §8.4 |
| REQ-POWER-06 | Clutch 切斷後視為無負載：Idle、輸出 0、不耗燃料與水、不磨損 | P3 | ✅ §8.4 |
| REQ-POWER-07 | 供不應求時沿用原版「全網一起降效率」，不新增優先序或配額 | P3 | ✅ §8.4 |
| REQ-POWER-08 | 效率與供需百分比讀自原版機械圖結果，不自行重算、不自建網路快取 | P3 | ✅ §8.4（λ 防回授公式） |
| REQ-POWER-09 | 需求 0 → Idle（輸出 0、不耗燃料/水、不磨損）；需求恢復後同 tick 回到 Running | P3 | ⚠ **2026-11-08 AD-16 改預設行為**：需求 0 且有燃料／水時以 `standbyLoadRatio`（0.10）待機運轉（`λ_run=max(λ,standby)`）；`standbyLoadRatio=0` 或未接電網才 Idle |
| REQ-POWER-10 | 預設比例耗燃料：燃料 = 表定 × min(1, 實際輸出/額定)；50% 負載 → 50%（±2%） | P3 | ✅ §8.3 |
| REQ-POWER-11 | 可切換 fuelScaling = vanilla：需求 >0 即全速耗；需求 0 仍停機 | P3 | ✅ §8.3（AD-3：controller λ=1）；⚠ 2026-11-08 AD-16：`standbyLoadRatio>0` 時待機運轉仍算 λ>0，vanilla 模式會全速耗燃料 |
| REQ-POWER-12 | 耗水同樣按負載比例縮放（50% 負載 → 50%，±2%） | P3 | ✅ §8.3 |
| REQ-POWER-13 | 實際輸出不得超過 PowerOutput；機況乘數與陣營係數只在最後一次無條件進位 | P2 | ✅ §8.4 |
| REQ-POWER-14 | Paused 優先於所有輸出狀態：停止輸出與消耗、不磨損 | P2 | ✅ §8.2 |
| REQ-POWER-15 | 偵測到 PL 且注入成功可拉電纜；未安裝或失敗時完全以原版節點運作 | P5 | ✅ §8.6 |
| REQ-POWER-16 | 主藍圖不含 PowerLineSpec；注入僅允許執行期條件式進行 | P5 | ✅ §4.4／§8.6 |
| REQ-POWER-17 | 不 Harmony patch 機械圖；與 PL 及同類動力 mod 並存不衝突（10 分鐘無例外日誌） | P5 | ✅ §8.6／§10 |
| REQ-POWER-18 | 狀態→視覺對應符合 §8.8；缺燃料／缺水／維護三種停機視覺可區分 | P4 | — |

### 2.7 REQ-MAINT（20 條；需求 §9.12）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-MAINT-01 | 每棟有 condition（0–100），新建完成 = 100 | P2 | ✅ §8.5 |
| REQ-MAINT-02 | 只在帶載運轉累積磨損；六種非帶載狀態靜置 1 遊戲日機況不變 | P2 | ✅ §8.5 |
| REQ-MAINT-03 | 磨損速率 = 50/(間隔日×24) × (0.4+0.6×負載比)，每運轉小時結算；滿載 24h T1 掉 8.33 點（±0.1） | P2 | ✅ §8.5 |
| REQ-MAINT-04 | 間隔 T1 6／T2 8／T3 10／T4 12 運轉日；滿載 100→50 恰為一間隔、100→0 為 2 間隔 | P2 | ✅ §8.5 |
| REQ-MAINT-05 | 機況乘數與狀態依 §9.3（1.00／0.93／0.85／0.40／0），狀態與乘數同段、無重疊；邊界逐一斷言 | P2 | ✅ §8.2 |
| REQ-MAINT-06 | condition = 0 → 輸出 0、狀態 NeedsMaintenance，等待材料與道路 | P2 | ✅ §8.2 |
| REQ-MAINT-07 | 自動維護於 condition ≤ 50 且 autoMaintenance = true 時觸發 | P2 | ✅ §8.5 |
| REQ-MAINT-08 | 材料依 §9.4 表；只用既有貨物，不新增材料 | P1 | ✅ §8.5 |
| REQ-MAINT-09 | 維護需 2 遊戲小時；期間輸出 0、不耗燃料與水、不磨損（38.4 秒 ±1 秒） | P2 | ✅ §8.5 |
| REQ-MAINT-10 | 開始時一次扣除材料；完成後 condition = 100、maintenanceState = None | P3 | ✅ §8.5 |
| REQ-MAINT-11 | 材料不足 → MissingMaintenanceMaterials：不扣料、不崩潰、補料後自動恢復 | P3 | ✅ §8.5 |
| REQ-MAINT-12 | 無道路 → NoAccessForMaintenance：不扣料、不崩潰；接路後自動恢復 | P3 | ✅ §8.5（D-5 待實機） |
| REQ-MAINT-13 | 手動維護：condition<100 且有料有路即可觸發；>90 仍可按但提示；僅 100 時禁用 | P4 | ✅ §8.5（C-5） |
| REQ-MAINT-14 | 自動維護逐棟開關、預設開；關閉後不自動觸發、手動仍可用 | P4 | ✅ §8.5 |
| REQ-MAINT-15 | 無隨機故障：相同運轉輸入重複兩次得相同機況（決定性） | P2 | ✅ §8.5 |
| REQ-MAINT-16 | 存檔 5 欄位齊備；載入後狀態一致；單棟損壞重置 100 並關閉自動維護，不影響其他棟 | P5 | ✅ §6.2／§6.6 |
| REQ-MAINT-17 | Config 可整體關閉維護：機況固定 100、無狀態、無材料需求、不影響其他系統 | P3 | ✅ §8.5 |
| REQ-MAINT-18 | 維護材料由既有庫存／搬運／輸送帶供應；不新增工種、不新增工程 | P3 | ✅ §8.5 |
| REQ-MAINT-19 | 面板顯示機況、狀態、缺料清單、維護進度、下次門檻與開關；狀態顏色依 §9.7 | P4 | — |
| REQ-MAINT-20 | 維護停機佔比符合 §9.4（T1 1.37%、T4 0.69%）；30 日平均有效輸出 ≥ 額定 ×0.95 | P7 | 🔧 P8 |

### 2.8 REQ-COMPAT（18 條；需求 §10.11）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-COMPAT-01 | 無任何外部模組時 12 檔全部可建/可運轉/可存讀檔；核心玩法不依賴外部模組（乾淨環境 30 遊戲日） | P5 | 🔧 P8 |
| REQ-COMPAT-02 | 水艙以原版 StockpileSpec{WhitelistedGoodType:"Liquid"} 宣告、容量＝表定值；不用自訂庫存型別 | P1 | ✅ §4.3／§4.7（AD-10） |
| REQ-COMPAT-03 | 燃料庫存獨立於水艙，且接受 Log（T1/T2）與 Log/Plank（T3/T4） | P1 | ✅ §4.7 |
| REQ-COMPAT-04 | 全 12 檔水艙/燃料庫存比 ≥8×；超出時建置期即失敗（生成器閘門） | P1 | ✅ §4.10 |
| REQ-COMPAT-05 | 混裝體積誤差上界 ≤12%；V-02 實測超出時 auto 須解析為 valve | P5 | ⏳ V-02 |
| REQ-COMPAT-06 | 三種供水模式皆可玩/可存檔/可執行期切換；切換不清空水艙、不改變 condition | P5 | ✅ §8.6 |
| REQ-COMPAT-07 | 藍圖不得出現 BuildingPipeSpec／BuildingPipeTargetPortsSpec／PowerLineSpec（CI 靜態掃描） | P1 | ✅ §4.4 |
| REQ-COMPAT-08 | 編譯期不得引用 TimberPipes／PowerLines／ConveyorBelt*；manifest.json 不得列為必要依賴 | P5 | ✅ §2.6／§10 |
| REQ-COMPAT-09 | 偵測必須三段式、全程 try/catch、總預算 ≤2 秒、單次快取、每模組只記一次日誌 | P5 | ✅ §8.6 |
| REQ-COMPAT-10 | Degraded 時視同 Absent 並顯示「整合不可用（版本不符）」；不嘗試半套整合 | P5 | — |
| REQ-COMPAT-11 | PL 注入必須執行期可選、可整段關閉；失敗時完全退回 MUST 且不影響存檔 | P5 | ✅ §8.6 |
| REQ-COMPAT-12 | 禁止對原版機械圖/庫存搬運/建造流程/全域存檔序列化做 Harmony patch（程式碼掃描 0 命中） | P6 | ✅ §10 |
| REQ-COMPAT-13 | 存檔中不得出現任何外部模組型別或識別碼；載入時所有外部接點重算 | P5 | ✅ §6.3 |
| REQ-COMPAT-14 | 存檔後移除任一/全部外部模組，載入必須成功不壞檔；模式旗標自動校正、貨物保留 | P5 | — |
| REQ-COMPAT-15 | 使用原版 Power 工具群組且不新增群組；ToolOrder 不與原版蒸汽機衝突、跨陣營一致 | P1 | ✅ §4.9 |
| REQ-COMPAT-16 | 偵測到舊版 Opolame.PowerLines 時只提示、不整合、不 patch；不得因此停用本模組 | P5 | — |
| REQ-COMPAT-17 | 任一適配器關閉後不得殘留事件訂閱或寫入 UI/存檔狀態（啟動與關閉對稱） | P5 | — |
| REQ-COMPAT-18 | §10.8 的 12 個相容性情境各須一條測試案例且全部通過 | P5 | — |

### 2.9 REQ-ARCH（14 條；需求 §11.8）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-ARCH-01 | 必須且僅存在五程式集：SEE.Core/Gameplay/Integration/UI/Tests，職責符合 §11.1 | P0 | ✅ §2.1 |
| REQ-ARCH-02 | 依賴方向單向（Core ← Gameplay ← Integration；UI 只依賴 Core；Tests 依賴全部），違反即建置失敗 | P0 | ✅ §1.3（分層掃描） |
| REQ-ARCH-03 | SEE.Integration 不得建立外部模組編譯期參考；外部型別只能以字串名稱取得 | P0 | ✅ §2.1 |
| REQ-ARCH-04 | data/engines.json 為 12 檔數值唯一來源；藍圖不得手寫 | P1 | ✅ §4.10 |
| REQ-ARCH-05 | 生成器必須產生恰好 24 份藍圖（12 檔 × 2 陣營），檔名與路徑符合 §2 | P1 | ✅ §4.10 |
| REQ-ARCH-06 | 24 份藍圖必須有黃金雜湊；重新生成後與 repo 逐位元組相同（時間戳除外） | P1 | ✅ §4.10 |
| REQ-ARCH-07 | 生成器必須套用並驗算陣營乘數（輸出 1.00/1.05、燃料 0.90/1.00），結果與 §3 派生規則一致 | P1 | ✅ §4.2 |
| REQ-ARCH-08 | 生成器內建五道閘門（8× 水艙比、≤12% 誤差、每 100hp 燃料/水上限、燃料庫存與水艙對帳、禁用 spec 掃描） | P1 | ✅ §4.10 |
| REQ-ARCH-09 | 程式碼不得出現白名單外裸數值；全數集中 SEEConst 或 spec/config | P6 | ✅ §10 |
| REQ-ARCH-10 | 單檔 ≤400 行（spec ≤600）、方法 ≤60 行、圈複雜度 ≤10、巢狀 ≤3、參數 ≤5；超出建置警告 | P6 | ✅ §1.2 |
| REQ-ARCH-11 | 三套版本（modVersion/schemaVersion/configSchemaVersion）各自獨立齊備；任一缺失即載入失敗並提示 | P0 | ✅ §9 |
| REQ-ARCH-12 | 生成器與遊戲必須共用同一公式檔（BoilerFormulas 單一實作） | P2 | ✅ §2.4／§3.6 |
| REQ-ARCH-13 | 每條 REQ 至少映射一個自動化測試（命名 REQ_<域>_<序號>_*），需求覆蓋率 100% | P6 | ✅（本表 274/274；P6 測試命名） |
| REQ-ARCH-14 | Blueprints/ 產物帶「GENERATED, DO NOT EDIT」檔頭；PR 清單明列禁止手改 | P1 | ⏳ AD-7 |

### 2.10 REQ-NFR（18 條；需求 §11.9.6）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-NFR-01 | 公開型別/成員 XML doc 完整，CS1591 為錯誤（dotnet build 0 warning） | P0 | ✅ §1.2 |
| REQ-NFR-02 | 命名遵守 §11.9.1 R-1；單位必須寫進變數名（WaterPerHour／PowerHp／Seconds 等） | P0 | ✅ §2.2 |
| REQ-NFR-03 | 檔案 ≤400 行、方法 ≤60 行、複雜度 ≤10、巢狀 ≤3、參數 ≤5（與 ARCH-10 同值） | P0 | ✅ §1.2 |
| REQ-NFR-04 | docs/decision-points.md 每個決策點都標示對應 REQ 編號；100% 命中＋抽樣 20 點全對 | P6 | — |
| REQ-NFR-05 | dotnet format --verify-no-changes 通過 | P6 | — |
| REQ-NFR-06 | 編譯 0 error；nullable/analyzers 編譯警告升為錯誤；架構度量維持警告語意 | P0 | ✅ §1.2 |
| REQ-NFR-07 | SEE.Core 行覆蓋 ≥80%、分支 ≥70%；四類必測（公式/狀態機/存檔編解碼/設定驗證）齊全 | P2 | ⏳ AD-6（80/70 vs PLAN 70% 待裁決） |
| REQ-NFR-08 | 每個 bug fix 附回歸測試（修前失敗、修後通過） | P6 | — |
| REQ-NFR-09 | data/engines.json 單一來源；24 份藍圖黃金雜湊逐位元組一致 | P1 | ✅ §4.10 |
| REQ-NFR-10 | 外部依賴只存在 SEE.Integration；Core 無外部組件參考 | P6 | ✅ §2.1／§1.3 |
| REQ-NFR-11 | 1 秒 Tick、每棟 O(1)；Tick 內 0 LINQ／0 字串插值／0 配置 | P6 | ✅ §8.1 |
| REQ-NFR-12 | 1,000 棟 p95 ≤3ms、p99 ≤6ms、GC≈0、FPS↓≤2、牆鐘 ≤0.3% | P6 | ✅ §8.1／§6.10 |
| REQ-NFR-13 | UI ≤1.0 ms/幀（含徽章）、徽章 ≤0.5 ms、4 Hz 髒標記 | P4 | ✅ §7.7 |
| REQ-NFR-14 | 存檔 ≤400 KB、寫入/載入各 ≤2 s（1,000 棟） | P5 | ✅ §6.10 |
| REQ-NFR-15 | 禁用 unsafe／P/Invoke／Process.Start／外部 DLL／網路；主執行緒限定 | P6 | ✅ §10 |
| REQ-NFR-16 | Tick 例外隔離：單棟失敗不影響其他棟，且日誌節流 | P3 | ✅ §8.1 |
| REQ-NFR-17 | 壞值（NaN/Inf/越界）與壞存檔全部安全降級、0 崩潰 | P2 | ✅ §6.6 |
| REQ-NFR-18 | 交付時附 CI 報告、覆蓋率報告，以及壓測/存檔量測/故障注入的「發佈前證據報告連結」 | P6 | 🔧 P8（發佈前證據） |

### 2.11 REQ-SAFE（15 條；需求 §12.9）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-SAFE-01 | 任一 Tick 系統最外層 try/catch；例外只停用該棟並標記 Faulted，不中斷遊戲 | P3 | ✅ §8.1 |
| REQ-SAFE-02 | 例外/警告日誌須 30 秒同 key 節流，並記錄期間抑制次數 | P3 | ✅ §2.8 |
| REQ-SAFE-03 | 所有 spec/庫存/存檔存取走 TryGet 或 null 檢查；null 不得導致 NRE | P3 | ✅ §8.1 |
| REQ-SAFE-04 | 所有數值進出公式前檢查有限性；NaN/Infinity 不得寫入存檔 | P2 | ✅ §6.6 |
| REQ-SAFE-05 | condition 恆在 [0,100]、維護剩餘小時在 [0,2]；越界寫入即失敗（property-based） | P2 | ✅ §6.2（D-7） |
| REQ-SAFE-06 | 讀檔時 condition 非有限或越界 → 該棟重置 100 並關閉自動維護，其他棟不受影響 | P5 | ✅ §6.6 |
| REQ-SAFE-07 | 業務邏輯不得在每幀 Update() 執行；SEE.Gameplay 內不得存在每幀 Tick 控制器 | P6 | ✅ §8.1 |
| REQ-SAFE-08 | 1,000 棟單次 Tick p95 ≤3ms、p99 ≤6ms；GC ≈0 B/frame；FPS↓≤2；牆鐘 ≤0.3%（p95） | P6 | ✅ §8.1 |
| REQ-SAFE-09 | Tick 時間差夾在 [0,5] 秒；暫停不推進、加速按比例推進 | P2 | ✅ §8.1 |
| REQ-SAFE-10 | 超預算時自動降頻到 2 秒結算並記錄、可回復；不得改變平衡結果（以遊戲時間計） | P3 | ✅ §8.1 |
| REQ-SAFE-11 | 每棟額外狀態 ≤64 B；1,000 棟額外狀態 ≤64 KB；UI/存檔快取總預算 ≤32 MB | P3 | ✅ §6.8 |
| REQ-SAFE-12 | 設定檔不合法時備份原檔（*.invalid-<timestamp>.json）並以預設值續跑，不刪檔 | P2 | ✅ §7.2 |
| REQ-SAFE-13 | 存檔必須原子寫入＋校驗和；校驗失敗不得覆寫原存檔 | P5 | ✅ §6.4／§6.8 |
| REQ-SAFE-14 | 診斷傾印含三套版本、偵測結果、12 檔統計與近 20 筆日誌；不含存檔全文與個資 | P5 | ✅ §2.8／§6.3 |
| REQ-SAFE-15 | 不得發起網路請求、不得寫出存檔目錄與日誌以外的檔案 | P6 | ✅ §10 |

### 2.12 REQ-IMPL（18 條；需求 §13.9）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-IMPL-01 | 24 份藍圖含齊必要 spec（MechanicalNodeSpec、GoodConsumingBuildingSpec、液體 StockpileSpec、SEEBoilerSpec、SEEMaintenanceSpec、BuildingSpec） | P1 | ✅ §4.3（spec key 見 §3.1） |
| REQ-IMPL-02 | 每份藍圖的 Horsepower/FuelPerHour/WaterPerHour/FuelCapacityGoods/WaterCapacityGoods 與 engines.derived.json 逐格相等 | P1 | ✅ §4.2 |
| REQ-IMPL-03 | FuelScaling 支援 proportional（預設）與 vanilla；切換只影響消耗曲線，不影響輸出與狀態機 | P2 | ✅ §3.2 |
| REQ-IMPL-04 | 燃料與水由單一消耗路徑扣除；不得重複扣料；連續 1 日實際扣除與表定誤差 ≤ ±2% | P3 | ✅ §3.5.2／§8.3（AD-3） |
| REQ-IMPL-05 | 燃料能量換算資料驅動：FuelGoodIds/FuelValues 由 spec 提供，程式不得硬寫 Plank=2 | P2 | ✅ §4.7 |
| REQ-IMPL-06 | 水消耗以浮點餘量累積、滿 1 貨物才扣；餘量 <1 可丟棄且不影響平衡 | P2 | ✅ §4.7 |
| REQ-IMPL-07 | 狀態機依 §5 優先序；九主狀態皆可觸發/顯示/存檔；Maintaining 第 0 級覆蓋態 | P2 | ✅ §8.2 |
| REQ-IMPL-08 | 磨損速率由 50/(IntervalDays×24) 即時計算，不得另存第二份真值 | P2 | ✅ §8.5 |
| REQ-IMPL-09 | 維護材料從「同 district 有道路可達的倉庫」原子扣除；不進引擎內部庫存；不足/無路時不扣料 | P3 | ✅ §8.5 |
| REQ-IMPL-10 | 維護持續 2 遊戲小時；期間輸出 0、不耗料、不磨損；完成後 condition=100 並記 lastMaintenanceDay | P3 | ✅ §8.5 |
| REQ-IMPL-11 | 輸出調變須在 T2 閘門前完成方案決策（A 逐棟調變／B 占空比），30 日平均符合判準 | P0 | 🔬 §8.4（D-1 已取證；P8 確認） |
| REQ-IMPL-12 | SEEBoilerController 與 SEEMaintenanceController 逐棟獨立狀態；任一棟 Faulted 不影響其他棟 | P3 | ✅ §8.1 |
| REQ-IMPL-13 | 每棟每 tick 成本符合 §13.7；材料/道路查詢節流 ≤每 10 秒一次／棟 | P3 | ✅ §8.1 |
| REQ-IMPL-14 | Tick 以遊戲時間差計算（暫停不推進、加速按比例），夾在 [0,5] 秒 | P2 | ✅ §8.1 |
| REQ-IMPL-15 | 所有 SEE.Core spec 欄位可序列化（基本型別）；不得含委派、遊戲物件 | P0 | ✅ §3.6（D-11：spec 在 Gameplay） |
| REQ-IMPL-16 | SEEMaintenanceSpec.Enabled=false 時不累積磨損、不出現維護 UI、condition 固定 100 | P2 | ✅ §3.3 |
| REQ-IMPL-17 | 生成器與遊戲共用 BoilerFormulas；不得存在第二套燃料/水耗計算 | P2 | ✅ §2.4 |
| REQ-IMPL-18 | T1–T6 每階段都須有可執行的技術閘門報告（含數據），未通過不得進下一階段 | P0 | ✅ §1.4 |

### 2.13 REQ-SAVE（14 條；需求 §14.9）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-SAVE-01 | 存檔根節點為 SEE，含 schemaVersion/modVersion/checksum/buildings/compat 五個必要區塊 | P5 | ✅ §6.1（D-6 混合：世界節點 `Singletons.SEE`＋per-entity） |
| REQ-SAVE-02 | buildings[] 的 11 欄位與 §14.1 欄位表逐欄位相符；未知欄位保留；entityId 用 Guid 字串 | P5 | ✅ §6.2（世界節點 4 索引＋per-entity 7 狀態；D-8 🔬 Guid；AD-11） |
| REQ-SAVE-03 | 燃料與水貨物庫存不得寫入 SEE 節點；只存機況、維護狀態、模式與旗標 | P5 | ✅ §6.7 |
| REQ-SAVE-04 | checksum 為 SHA-256、範圍為去除 checksum 後的標準化 JSON；不符時不得拒絕載入、不得覆寫原檔 | P5 | ✅ §6.4 |
| REQ-SAVE-05 | 必須有遷移器鏈（只前進、不 throw、未知欄位保留）；schemaVersion 較高時走唯讀降級 | P5 | ✅ §6.5 |
| REQ-SAVE-06 | 單棟資料損壞時只重置該棟（condition=100、關閉自動維護），其他棟與整檔不受影響 | P5 | ✅ §6.6 |
| REQ-SAVE-07 | 找不到建築的資料列保留一版、下次存檔丟棄；重複 entityId 保留第一筆 | P5 | ✅ §6.6 |
| REQ-SAVE-08 | waterSupplyMode 必須是「意圖值」；載入時依本次偵測校正執行期值，不得造成反覆翻轉 | P5 | ✅ §6.2 |
| REQ-SAVE-09 | 存檔中不得出現任何外部模組型別、識別碼或接點資料 | P5 | ⏳ AD-5（與 compat.detected 字面張力） |
| REQ-SAVE-10 | 1,000 棟存檔 ≤400 KB（zip 後實際檔案）、寫入 ≤2 s、載入 ≤2 s | P5 | ✅ §6.10 |
| REQ-SAVE-11 | 存檔寫入必須原子完成；中途例外時原有存檔內容不得被破壞 | P5 | ✅ §6.8 |
| REQ-SAVE-12 | 讀檔後延遲到原版機械圖重建完成才套用狀態；不得在載入途中寫節點輸出 | P3 | ✅ §6.8／§6.9（pending→首 tick） |
| REQ-SAVE-13 | 連續兩次載入都判定同一棟損壞時，須在面板標記並附 entityId 供回報 | P4 | ✅ §6.6 |
| REQ-SAVE-14 | 完整存檔往返（存→讀→存）位元組穩定（除 modVersion 與時間戳外語意相同） | P5 | ✅ §6.4 |

### 2.14 REQ-UI（12 條；需求 §15.8）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-UI-01 | 建築面板必含動力、燃料、水、機況、下次維護、維護材料、供水模式、相容狀態八項 | P4 | — |
| REQ-UI-02 | 面板顯示電網供給/需求與效率%，資料來自電網快取而非 UI 自行加總 | P4 | — |
| REQ-UI-03 | 主狀態徽章只有一個、優先序照 §15.3；Maintaining 為第 0 級覆蓋態（優先於 NoFuel、不計入 9 狀態） | P4 | — |
| REQ-UI-04 | 缺燃料/缺水/需維護時面板給出一行可執行修復指引 | P4 | — |
| REQ-UI-05 | 提供暫停、立即維護、自動維護開關、供水模式切換四種操作，皆可回到前一狀態 | P4 | — |
| REQ-UI-06 | 維護材料不足與無路可達須顯示缺少清單/原因，且不消耗材料 | P4 | — |
| REQ-UI-07 | 未安裝 TP 時 tank 選項顯示不可用並解釋原因；三種可用模式皆可切換 | P4 | — |
| REQ-UI-08 | 本地化鍵 ≥54 個，前綴僅 Building.SEE./Status.SEE./UI.SEE.，不得覆寫原版鍵 | P4 | ✅ §5.2（54 鍵清單） |
| REQ-UI-09 | zhTW 不得含簡體字、不得回退 zhCN、缺鍵回退 enUS | P4 | ✅ §5.1／§5.3 |
| REQ-UI-10 | 全部字串符合 §15.5 字元上限；enUS ×1.4 假字串不溢出 | P4 | ✅ §5.3（L3） |
| REQ-UI-11 | 可及性 A1–A8 全數通過 | P4 | — |
| REQ-UI-12 | 面板折疊狀態與上次選擇在本次遊戲階段內保留；不寫入存檔 | P4 | — |

### 2.15 REQ-OPS（8 條；需求 §15.8／§17.6）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-OPS-01 | 面板資料刷新 ≤4 Hz，且只在面板開啟且選中建築時執行 | P4 | ✅ §7.7 |
| REQ-OPS-02 | 以髒標記驅動重繪；無變化時每幀零配置、零字串拼接 | P4 | — |
| REQ-OPS-03 | 世界徽章 1 Hz＋事件驅動；1,000 棟時徽章單項 ≤0.5 ms/幀 | P4 | — |
| REQ-OPS-04 | 面板關閉後立即取消所有排程，背景更新計數歸零 | P4 | — |
| REQ-OPS-05 | 1,000 棟每 Tick p95 ≤3ms、p99 ≤6ms、GC≈0、FPS↓≤2，且程式碼不得有全圖掃描 | P6 | ✅ §8.1／§10 |
| REQ-OPS-06 | 外部模組偵測 ≤2 秒、只跑一次、失敗只記一次日誌 | P5 | ✅ §8.6 |
| REQ-OPS-07 | 靜態禁止項（Update 內 FindObjects／反射／每幀字串）以 CI 規則檢查 | P6 | ✅ §10 |
| REQ-OPS-08 | 日誌分類與 30 秒節流落實；壓測不得洗版；錯誤可被玩家回報定位 | P6 | ✅ §2.8 |

### 2.16 REQ-ART（22 條；需求 §16.10）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-ART-01 | 12 種造型對應 3 尺寸 × 4 等級；24 藍圖僅對應 12 種幾何＋2 陣營材質，不得 24 套模型 | P9 | ✅ §4.6（一檔一陣營） |
| REQ-ART-02 | 去色剪影下 3 尺寸與 4 等級兩兩可辨；盲測 ≥90% 正確 | P9 | — |
| REQ-ART-03 | 尺寸/高度完全符合 §3；除煙囪與飛輪外不得超出占地與 Z | P9 | — |
| REQ-ART-04 | 兩陣營共用幾何 ≥90%，僅材質與 2 個小件不同；色票符合 §16.3 | P9 | — |
| REQ-ART-05 | LOD0/1/2 面數符合 §16.5 上限；LOD2 ≤ LOD0 的 15% 且無動畫不投影 | P9 | — |
| REQ-ART-06 | 四類端口（側面 transput、水艙接管、燃料帶端面、入口）位置與實測 spec 一致 | P9 | — |
| REQ-ART-07 | 碰撞盒 = footprint、高度 = Z，且不阻擋相鄰接口 | P9 | — |
| REQ-ART-08 | 命名與匯出（Y 上、1 單位=1 公尺、正面 -Y、原點、Apply Transform）符合 §16.7 | P9 | — |
| REQ-ART-09 | 3D 提示詞可交付外部美術，產出符合 §16.2 與 §16.5（不得增刪技術約束） | P9 | — |
| REQ-ART-10 | 煙霧掛點 3 個、飛輪/活塞可獨立動畫；靜止與運轉狀態可辨 | P9 | — |
| REQ-ART-11 | 零骨架：不得有 skinned mesh、骨架或 Animator；可動件為獨立 Transform 子物件並保留樞軸 | P9 | ✅ §7.8（C-9） |
| REQ-ART-12 | 飛輪/曲軸以原版 spec 綁定（GoodPoweredGeneratorAnimatorSpec；必要時 WindRotationAnimatorSpec），藍圖不得寫死轉速 | P9 | ✅ §7.8（C-9） |
| REQ-ART-13 | 轉速 = 等級基礎 × 尺寸乘數 × 陣營乘數 × min(1,供給/需求) × 機況乘數；Idle 為 0、不得為負 | P9 | — |
| REQ-ART-14 | 活塞以程序化 sin 驅動、LOD0 30 Hz；相位 T2 兩缸 90°、T3/T4 四缸 90°；不得用 Animator Curve | P9 | — |
| REQ-ART-15 | 狀態動畫：Starting 1.5s、Stopping 3.0s、Maintaining 2h、Paused 0.3s、Seized；程序化緩動、換狀態角度連續 | P9 | — |
| REQ-ART-16 | 機況四階回饋（61–100/41–60/21–40/1–20/0）符合 §16.9.4，不得改變碰撞盒與端口位置 | P9 | — |
| REQ-ART-17 | VFX：3 煙霧掛點沿用原版 prefab 風格；排氣脈衝與活塞下死點同步；emission ∝ 負載；不得每幀 new | P9 | — |
| REQ-ART-18 | 動畫 LOD：0–18 m 全動畫 30 Hz、18–40 m 僅飛輪 15 Hz、>40 m 靜止；與 §16.5 LOD 距離一致 | P9 | — |
| REQ-ART-19 | 動畫效能：100 棟可見 ≤0.5 ms/frame、GC 0 B/frame、離屏 0 成本、單棟 30 Hz 更新件數 ≤12 | P9 | — |
| REQ-ART-20 | 失敗隔離：動畫例外不影響輸出/燃料/水/維護，只記一次 SEE/Warn；anim.enable=false 等同靜止且無殘留 | P9 | ✅ §7.8 |
| REQ-ART-21 | 兩陣營動畫風格符合 §16.9.8（轉速乘數 0.90/1.10、傳動語彙、蒸汽色、附加粒子）；相位與啟停時長相同 | P9 | — |
| REQ-ART-22 | 三階品質門檻 P1/P2/P3 全過才可發佈；動畫子物件共用既有材質、不新增 draw call | P9 | — |

### 2.17 REQ-TEST（18 條；需求 §17.6）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-TEST-01 | T1 藍圖驗證 100% 自動化，24 份全過且與 data/engines.json 零誤差 | P1 | ✅ §4.10 |
| REQ-TEST-02 | loc 鍵完整性與原版鍵未被覆寫，列為 CI 阻擋項 | P6 | ✅ §5.4 |
| REQ-TEST-03 | T0 覆蓋輸出/燃料/耗水/磨損/維護倒數/存檔序列化六類純邏輯；覆蓋率 ≥70% | P2 | — |
| REQ-TEST-04 | 零需求停機與不耗燃料列入 T3 必測 | P2 | — |
| REQ-TEST-05 | 比例耗燃料與耗水在半載、滿載、vanilla 三種情況皆有案例 | P3 | — |
| REQ-TEST-06 | 缺燃料/缺水/兩缺依 §5 優先序顯示，順序不得由實作決定（表驅動測試） | P2 | — |
| REQ-TEST-07 | 維護四狀態皆有自動化與實機案例，且皆可存檔往返 | P3 | — |
| REQ-TEST-08 | 機況 0 停機、維護完成重置，含材料扣除順序（先驗後扣） | P3 | — |
| REQ-TEST-09 | 三模組 8 種組合各 1 輪，外加管帶同格與工具列排序 | P5 | — |
| REQ-TEST-10 | 缺席模組降級路徑須驗證「無錯誤日誌、無缺失 UI」 | P5 | — |
| REQ-TEST-11 | Power Lines 注入成功/失敗兩條路徑皆有案例 | P5 | — |
| REQ-TEST-12 | 存檔 v1 round-trip、未知欄位、modVersion 變更三案例 | P5 | — |
| REQ-TEST-13 | 單棟損壞資料隔離，其他棟不得被重置或丟失 | P5 | — |
| REQ-TEST-14 | 1,000 棟壓力測試腳本化，含 30 遊戲日模擬 | P7 | — |
| REQ-TEST-15 | 30 日平均有效輸出 ≥ 對照 ×0.95，且不得只算理想滿載 | P7 | — |
| REQ-TEST-16 | 移除任一外部模組後存檔可載入、建築不消失 | P5 | — |
| REQ-TEST-17 | 兩陣營的維護材料與建造材料皆可實際取得 | P8 | 🔧 P8 |
| REQ-TEST-18 | 每次候選版本執行 §17.5 手動腳本並簽核，未簽核不得發佈 | P8 | 🔧 P8 |

### 2.18 REQ-RISK（10 條；需求 §18.2）

| REQ ID | 一句摘要 | 負責卡 | P0 狀態 |
|---|---|---|---|
| REQ-RISK-01 | 任何平衡參數調整都必須重跑 §4 四條判準，未通過不得寫入預設值 | P7 | — |
| REQ-RISK-02 | 與原版蒸汽機的差異化說明（多尺寸/跨陣營/自動化）須出現在描述與工作坊頁 | P8 | — |
| REQ-RISK-03 | 水艙與燃料庫存硬體分離；tank 模式需 V-02 通過才可設為預設 | P5 | ✅ §4.7 |
| REQ-RISK-04 | 管網汙染、帶子斷電、缺水須顯示各自專屬原因，不得歸類為 NoFuel／NoWater | P4 | — |
| REQ-RISK-05 | 輸送帶與管道在 UI 皆有明確接面提示，且不假設玩家已裝模組 | P4 | — |
| REQ-RISK-06 | 維護系統預設開啟自動維護，且不得要求玩家建立新工種 | P3 | ✅ §8.5 |
| REQ-RISK-07 | 每檔研究點落在文件指定科技帶並在 tooltip 顯示；T1/T2 不得超過 400 | P1 | — |
| REQ-RISK-08 | 只用原版 Power 群組與 ToolOrder，不得自建工具群組 | P1 | ✅ §4.9 |
| REQ-RISK-09 | 顯示名與搜尋關鍵字不得與原版蒸汽機、SteamPlant、Choo!Choo! 混淆 | P1 | ✅ §2.7 |
| REQ-RISK-10 | 30 日平均輸出未達標時只能調維護/自動化，不得放寬燃料與水耗對照 | P7 | — |

---

## 3. P0 覆蓋小節（已凍結／已證實／待實機）

> **✅ 的定義**：P0 已把該 REQ 涉及的介面／schema／數值／規則寫入 `CONTRACT_SEE.md`；**不是**已實作。
> 實作與實機驗收分別由後續卡與 P8 負責。

### 3.1 P0 已凍結（契約條文；以 `CONTRACT_SEE.md` 章節索引）

| 契約章節 | 凍結內容 | 對應 REQ（主要） |
|---|---|---|
| §1 建置／測試／T1–T6 門檻 | 0 warning、`-m:1`、單一 ProjectReference、分層掃描、技術閘門 | ARCH-10、NFR-01/03/06、IMPL-18 |
| §2 程式集與命名 | 五程式集、依賴方向、D-11 分層偏離、凍結型別名、manifest、TemplateName、日誌 | ARCH-01/02/03/11/12、NFR-02/10、IMPL-15 |
| §3 自訂 Spec | `[SpecAlias]` key 表、兩 spec 欄位、序列化口徑、容量派生（AD-3）、容量生成公式 | BLD-09、FUEL-01/02/03/04/08、IMPL-01/03/05/06/15/16/17 |
| §4 藍圖契約 | 路徑 D-9、24 檔清單、12 檔權威值、必要 spec、禁止 spec、軸序 R-B、模型陣營 R-A、庫存雙軌、TemplateCollection、生成器閘門 | BLD-01～14、CORE-02/04/05/12、TECH-01/02/03/04/06/08、FUEL-01/03/04/08/09/11/16/19/20、POWER-01～04、COMPAT-02/03/04/07/15、ARCH-04～08、IMPL-01/02、RISK-07/08/09 |
| §5 本地化 | 54 鍵清單（A12／B5／C22／D15）、CSV 格式、L1–L6、禁止鍵 | UI-08/09/10、FUEL-18、TEST-02 |
| §6 存檔 | 世界節點 `Singletons.SEE`（版本／checksum／4 欄索引／compat）＋ per-entity 狀態載體、checksum、遷移、損壞隔離、讀寫時機、**D-6 混合形態**、體積門檻 | SAVE-01～08/10～14、MAINT-16、SAFE-05/06/13、NFR-14/17 |
| §7 設定檔 | `configSchemaVersion=1`、鍵群組、clamp、未知欄位保留、非法檔備份 | OPS-01、NFR-13、SAFE-12、POWER-11 |
| §8 執行期行為 | Tick、9 主狀態＋Maintaining、燃料／水消耗（AD-3）、λ 輸出與存讀檔重套（AD-9）、水艙允許與對帳（AD-10）、磨損維護、暫停／相容、日誌 | CORE-01/03/06/07/09、BAL-11、FUEL-05/06/07/10/17、POWER-05～14、MAINT-01～15/17/18、IMPL-04/07/08/09/10/12/13/14、SAFE-01～04/09/10/11/14、COMPAT-06/09/11 |
| §9 版本三軌 | modVersion 0.1.0／schemaVersion 1／configSchemaVersion 1、升版與遷移承諾 | ARCH-11 |
| §10 禁止項 | 12 條紅線（Harmony、外部 DLL、覆寫原版、新物品／陣營、每幀掃描、硬寫值、舊識別字…） | CORE-12、ARCH-09、COMPAT-08/12、POWER-17、NFR-15、SAFE-15、OPS-05/07 |
| §11 待裁決 | D-1～D-6、AD-1～AD-11 開放項與已裁決事實索引 | IMPL-11、SAVE-09、NFR-07、ARCH-14、ART-01 等 |

### 3.2 P0 已證實（取證；`API_FACTS_SEE.md`／`_investigate`）

| REQ | 已證實的事實 | 證據 | 契約位置 |
|---|---|---|---|
| REQ-SAVE-02 | `EntityComponent.EntityId` 是 `System.Guid`、無 int id → 存檔 `entityId` 用 Guid 字串（D-8） | IL（`gameplay-findings.md` §1） | §6.2 |
| REQ-SAVE-01 | 存檔形態可用（`ISaveableSingleton`／`ISingletonSaver`／`ISingletonLoader`／`SerializedWorldFactory`＋`EntityRegistry` 列舉）→ **D-6 定案＝混合**：世界節點只存版本／checksum／索引，狀態走 per-entity `IPersistentEntity` | `_investigate/findings-persistence-loading.md`、`_investigate/out2/save/` | §6.9 |
| REQ-FUEL-03 | 容量派生＝逐項 `CeilToInt((float)GoodPerHour × FullInventoryWorkHours)`；AD-3 定容量由表定 `fuelStock` 反推 | D-3 取證＋Lead AD-3 裁決 | §3.5.1／§3.5.2 |
| REQ-FUEL-08 | 水艙獨立（液體 StockpileSpec.MaxCapacity 表定），不經 `GoodConsumingBuildingSpec` | D-3 取證 | §3.5.1 |
| REQ-BLD-09 | 燃料容量生成公式（單燃料 `(fuelStock−0.5)/25`；雙燃料每項 `(fuelStock/2−0.5)/25`）12 檔逐格等於表定 | Lead AD-3 裁決＋float32 驗算 | §3.5.2 |
| REQ-IMPL-01 | `[SpecAlias]` 必要（`SpecTypeCache` 只用 alias＋Type.Name）；藍圖 key＝`SEE.SEEBoilerSpec`／`SEE.SEEMaintenanceSpec` | IL（`gameplay-findings.md` §5）＋Lead 補充 | §3.1 |
| REQ-IMPL-15 | 藍圖反序列化只對 `ImmutableArray<T>` 逐元素遞迴；`SEEMaintenanceSpec.Materials` 採 `ImmutableArray<GoodAmountSpec>` | IL＋Lead AD-4 裁決 | §3.4 |
| REQ-IMPL-04 | 原版消耗可永久 pause（`GetGoodConsumingToggle().PauseConsumption()` public）；`Inventory.TakeConsumed` public → 單一消耗路徑可行 | IL（Lead AD-3 補充） | §3.5.2／§8.3 |
| REQ-ARCH-02 | 分層掃描以 `Assembly.GetReferencedAssemblies()` 強制；D-11 讓 Core 可離線測試 | IL／分叉骨架 | §1.3／§2.3 |
| REQ-POWER-08／REQ-IMPL-11 | `GoodPoweredGeneratorSpec` 必須移除（原版每 tick `SetOutputMultiplier` 覆寫 λ）；λ 防回授公式取得 | `_investigate/findings-power.md` §2.4／§4.4；`out2/power/il-goodpoweredgen.txt` | §4.4／§8.4 |
| REQ-FUEL-11／REQ-COMPAT-02 | 水艙庫存需 controller `SingleGoodAllower.Allow("Water")`（`AllowedGood == null` 時所有貨物回 0）；燃料 GCB 走 `Givable`／`FillInput` | `_investigate/findings-inventory-maintenance.md` §2.2／§6 | §4.7／§8.3 |

### 3.3 P0 待取證／待裁決（開放項對映）

| 開放項 | 影響 REQ | 契約位置 |
|---|---|---|
| D-1 輸出調變 A/B 與 λ 可得性（**已取證 `_investigate/findings-power.md` §2.4／§4.4；待 P8 實機確認回授震盪**） | IMPL-11（主）、CORE-07、POWER-05～12、BAL-11 | §11.1、§8.4 |
| D-2 雙庫存補給／重複扣料（實機） | FUEL-05/06/10/12～15、IMPL-04、COMPAT-01 | §11.1、§3.5.2 |
| D-4 電網需求 0 行為／停機不耗料（**已取證 `findings-power` §4.1；待 P8 實機確認**） | CORE-08、POWER-09 | §11.1、§8.2／§8.4 |
| D-5 維護材料扣料／無路判定（實機） | MAINT-09/11/12、IMPL-09 | §11.1、§8.5 |
| AD-1 原型 icon 用原版路徑 | BLD-13、ART-01（v1 暫代） | §11.2、§4.11 |
| AD-2 FT 模型策略（R-A） | ART-01/04/05、BLD-13 | §11.2、§4.6 |
| AD-5 REQ-SAVE-09 vs `compat.detected` | SAVE-09、COMPAT-13 | §11.2、§6.3 |
| AD-6 覆蓋率 80/70 vs PLAN 70% | NFR-07、TEST-03 | §11.2 |
| AD-7 「GENERATED」檔頭形式 | ARCH-14 | §11.2、§4.10 |
| AD-8 loc CSV 編碼（BOM）與欄位數 | UI-09、TEST-02 | §11.2、§5.1 |
| D-3、D-6、AD-3、AD-4、AD-9、AD-10、AD-11 | — | **已收斂**（見 §3.2／§3.6；契約 §11.3） |

### 3.4 P0 待實機（只能由 P8 人類 gate 驗證）

| REQ | 待實機項目 |
|---|---|
| BLD-06／BLD-14 | 入口臨路可建、四向旋轉與連接面 |
| CORE-10／CORE-11 | 驅動原版機械水泵、面板 30 秒判讀 |
| TECH-07 | 原版蒸汽機共存（可建、400 hp、工具列可見） |
| BAL-05／BAL-10／MAINT-20 | 30 日平均、維護停機占比 |
| COMPAT-01/05/10/14/16/17/18 | 無模組可玩、V-02、Degraded、移除模組、12 情境 |
| TEST-17／TEST-18 | 兩陣營材料可取得、17.5 手動腳本簽核 |
| NFR-18 | 發佈前證據報告（壓測／存檔量測／故障注入） |

### 3.5 D-11 偏離註記（Core 零遊戲相依）

| 項目 | 需求字面 | 契約口徑（D-11） | 影響 |
|---|---|---|---|
| 兩個 `ComponentSpec` 位置 | 需求 §11.1 寫「`SEEBoilerSpec`／`SEEMaintenanceSpec` 在 `SEE.Core`」 | **放 `SEE.Gameplay`**；`SEE.Core` 零遊戲相依（公式／常數／列舉／狀態機／帳本／編解碼／接口） | 可離線測試、建置穩定；分層方向與依賴掃描不變 |
| 受影響 REQ | REQ-ARCH-01／REQ-IMPL-01／REQ-IMPL-15 | 以契約 §2.3／§3.1 為實作位置；**不視為需求失效** | P2 測試可 `net9` 乾淨引用 Core；P3 放 spec |
| `SEE.Tests` | 需求 §11.1 允許依賴全部 | 維持：可依賴全部 SEE 程式集（測試專用、不出貨） | 分層掃描排除 `SEE.Tests` |

### 3.6 AD-3／AD-4／AD-9／AD-10／AD-11／D-6 裁決對映（Lead 2026-11-06）

| 裁決 | 契約位置 | 受影響 REQ | 後續卡動作 |
|---|---|---|---|
| **AD-3** 容量口徑與消耗口徑分離（原版消耗永久 pause；`ConsumedGoods.GoodPerHour` 僅容量派生；實際消耗＝controller `FuelPerHour×λ`＋能量換算；水維持液體 StockpileSpec；備援方案 C 僅 P8 失敗啟用） | §3.5.2／§4.7／§8.3 | BLD-09、FUEL-01/02/03/05/06/10、IMPL-04/05/06 | P1 生成器照 §3.5.2 最終公式；P3 controller 實作 pause＋`TakeConsumed`＋對帳；P6 validator float32 驗算 |
| **AD-4** `GoodAmountSpec`＝`Id`／`Amount`；`ConsumedGoodSpec`＝`GoodId`／`GoodPerHour`，不得混淆 | §3.4 | IMPL-15、MAINT-16、SAVE-02 | P1 生成器／P6 validator 分開檢查兩種欄位名 |
| **D-6** 存檔形態＝**混合**：世界節點 `Singletons.SEE`（schemaVersion／modVersion／checksum／4 索引欄位／compat）＋逐棟 `ComponentKey("SEE")` 的 7 欄狀態（`IPersistentEntity`）；節點失敗可降級、狀態不丟 | §6.1／§6.9 | SAVE-01/02/04/05/10/11/14、MAINT-16 | P5 實作世界節點＋per-entity；P3 `IPersistentEntity`；P8 驗證 singleton 時序 |
| **AD-11** 需求 §14.1 的 7 個 `buildings[]` 狀態欄位改存 per-entity；世界節點只留 4 索引欄位；若 P8 證實節點時序穩定可搬回（`schemaVersion+1`＋遷移器） | §6.9 | SAVE-01/02、MAINT-16、SAVE-03/08 | P5 照混合實作；P3／P4 不得自行改形態 |
| **AD-9** `GoodPoweredGeneratorSpec` 移除；controller 接管輸出倍率（λ 防回授）＋燃料 toggle＋零需求邏輯 | §4.3／§4.4／§8.4 | IMPL-01/04、POWER-01/05～14、CORE-08 | P1 生成器移除＋P6 掃描；P3 以 λ 公式實作；P8 驗證動畫／煙霧（僅視覺 spec 可留） |
| **AD-10** 水艙需 controller `SingleGoodAllower.Allow("Water")`（僅未設定時、不覆寫玩家）；燃料 GCB 走 `Givable`／`FillInput`；對帳用 Inventory 整數差值 | §4.7／§8.3 | FUEL-10/11/12～15、COMPAT-02、IMPL-04/06 | P3 於完成／初始化時設定；P8 驗證搬運／管網進水 |

---

## 4. 卡→REQ 反向索引（摘要）

| 卡 | 主要 REQ 數 | P0 狀態摘要 | 備註 |
|---|---:|---|---|
| **SEE-P0** | 11 | 全部 ✅／🔬（含 1 項 ⏳ D-1） | ARCH-01/02/03/11、NFR-01/02/03/06、IMPL-11/15/18；本表與契約為 P0 交付 |
| **SEE-P1** | 57 | 多為 ✅ 契約凍結 | BLD 全、TECH 8、FUEL 9、CORE-02/04/05、POWER-01～04、COMPAT-02/03/04/07/15、ARCH-04～08、TEST-01、RISK-07/08/09 等；資料層唯一數值源 |
| **SEE-P2** | 42 | 多為 —（契約已凍結規則） | CORE 邏輯、BAL 9、MAINT 9、IMPL 8、SAFE 4、NFR-07/17、ARCH-12 |
| **SEE-P3** | 40 | 多為 ✅ | CORE-03/09/10、FUEL 7、POWER 8、MAINT 5、IMPL 5、SAFE 5、BAL-11、SAVE-12、RISK-06 |
| **SEE-P4** | 25 | 少數 ✅（§5／§7.7） | UI 12、MAINT 3、OPS 4、CORE-11、POWER-18、SAVE-13、NFR-13、RISK-04/05 |
| **SEE-P5** | 44 | 相容／存檔契約 ✅ | COMPAT 12、SAVE 12、FUEL 12～15、POWER-15～17、SAFE 3、OPS-06、MAINT-16、NFR-14、TEST 6、RISK-03 |
| **SEE-P6** | 20 | 門檻／禁止項 ✅ | ARCH-09/10/13、NFR 8、SAFE 3、OPS 3、CORE-12、TEST-02、COMPAT-12 |
| **SEE-P7** | 9 | — | BAL-05/10/13、TECH-09、MAINT-20、TEST-14/15、RISK-01/10（離線模擬） |
| **SEE-P8** | 4 | 🔧 待實機 | TECH-07、TEST-17/18、RISK-02（人類 gate） |
| **SEE-P9** | 22 | ART 契約少量 ✅ | ART 全 22（模型／LOD／icon／動畫） |

---

## 5. 維護與變更規則

1. **每條 REQ 必有卡**：新增 REQ 時同步補主要卡與 P0 狀態；不得留空。
2. **卡變更**：調整主要卡須 Lead 同意，並同步 §1 核對行與 `TASKS.md` 卡片依賴。
3. **契約變更**：REQ 對應的契約章節若變更，先改 `CONTRACT_SEE.md`（Lead 核准），再更新本表「P0 狀態」欄。
4. **裁決落地**：Lead 裁決（D-／AD- 系列）後，把相關 REQ 的 P0 狀態由 ⏳ 改為 ✅／🔬，並在 §3 對映表補一列。
5. **P0 狀態不得造假**：✅ 只代表契約凍結；實作完成以 `VERIFICATION.md` 的「需求→實作位置→驗證方式→結果」為準。
6. **實機紀律**：🔧 只能由 P8 人類簽核；AI 不得宣告通過（PLAN §7.8、TASKS.md）。

---

## 6. 版本與變更紀錄

| 版本 | 日期 | 變更 | 作者 |
|---|---|---|---|
| 1.0-P0-freeze | 2026-11-06 | 初版：274 條全對映（18 域）、數量核對、P0 覆蓋小節（含 D-11 偏離）、卡→REQ 索引 | docs-contract（P0） |
| 1.1-P0-rev | 2026-11-06 | 納入 Lead 裁決：AD-3（BLD-09／FUEL-01/02/03 改 `P1＋P3`；容量／消耗口徑）、AD-4（`GoodAmountSpec` 欄位）、D-6（純世界節點）；更新 §3.2／§3.6 與核對行 | docs-contract（P0） |
| 1.2-P0-rev | 2026-11-06 | 納入 AD-9（`GoodPoweredGeneratorSpec` 移除、λ 公式）、AD-10（水艙 `SingleGoodAllower.Allow("Water")`、`Givable`／`FillInput`、對帳口徑）、D-1／D-4 取證；POWER-08/09、IMPL-11、CORE-08、FUEL-11、COMPAT-02 狀態更新 | docs-contract（P0） |
| 1.3-P0-rev | 2026-11-06 | **D-6 最終裁決改為混合形態**＋AD-11（7 欄狀態改存 per-entity、世界節點留 4 索引）；更新 §3.1／§3.2／§3.6 與 SAVE-01/02 狀態 | docs-contract（P0） |

> 資料來源：`_investigate/doc-extract.md` §11（274 條）、§0.2（域清點）；並交叉核對 `CONTRACT_SEE.md` 各凍結章節。
