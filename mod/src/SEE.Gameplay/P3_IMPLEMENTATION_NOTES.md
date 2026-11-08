# SEE-P3 Gameplay 實作備註（2026-11-06）

> 範圍：`mod/src/SEE.Gameplay/**`。本檔記錄 P3 的落地方式、與契約的偏離／待裁決事項，供 P4／P5／P6／P8 與 Lead 使用。
> 權威來源：`mod/docs/CONTRACT_SEE.md`、`mod/docs/API_FACTS_SEE.md`、`mod/docs/LEAD_RULINGS*.md`、PLAN §4.5。

## 1. 交付對映

| 契約／裁決 | 實作位置 | 落地方式 |
|---|---|---|
| `SEEBoilerSpec`（12 欄位、`[SpecAlias]`） | `Specs/SEEBoilerSpec.cs`、`SeeGameConfigurator.cs` | `TemplateModule.Builder().AddDecorator<SEEBoilerSpec, SEEBoilerController>()` |
| `SEEMaintenanceSpec`（7 欄位） | `Specs/SEEMaintenanceSpec.cs` | 控制器讀 `Enabled`／`Materials`；`IntervalDays`／`Threshold`／`DurationHours` 由 config 層 `SeeConfig.RulesFor` 覆寫（見 §4.3） |
| D-1 輸出調變 A 案 | `SEEBoilerController.Tick.cs::ResolvePowerAndState` | `node.SetOutputMultiplier(clamp01(λ × ConditionCurve.Multiplier))`；λ 防回授公式與 ΣN_SEE／S_other 由 registry 每批聚合 |
| D-2／AD-3 燃料＋水（2026-11-08 修根因） | `SEEBoilerController.Lifecycle.cs::EnsureVanillaConsumptionPaused`、`SEEBoilerController.WaterTank.cs`、`SEEBoilerController.Consumption.cs` | `GetGoodConsumingToggle().PauseConsumption()` 永久暫停原版扣料；燃料由控制器以能量結算 `Inventory.TakeConsumed`（Log 優先、Plank 能量 2.0）；水由獨立 `WaterTank` public input Inventory（`SEEWaterInventoryInitializer`）`TakeExisting` 整數扣貨＋浮點餘量 |
| AD-14 水艙（取代 AD-10；2026-11-08） | `SEEWaterInventoryInitializer.cs`、`SeeGameConfigurator.cs::BuildTemplateModule`、`WaterTank.cs` | dedicated decorator 建立 `WaterTank` public input Inventory（`Water` giveable-and-takeable、容量＝`WaterCapacityGoods`）；owner 完工 `Enable()`／離開完成態 `Disable()`；扣水 `TakeExisting`。不再需要 `SingleGoodAllower` |
| D-5 維護 | `SEEBoilerController.Maintenance.cs` | `DistrictInventoryRegistry` 先驗後扣（排除自身庫存）＋`BuildingAccessible`＋`DistrictCenter.AccessibleIsOnDistrictRoad`；`MaintenanceLedger` 管 phase／倒數／完成 |
| D-6／AD-11 存檔 | `Save/SeeSaveBridge.cs`、`SEEBoilerController.Save.cs`、`SeeSaveKeys.cs` | 逐實體 `ComponentKey("SEE")` 7 欄位為唯一狀態源；世界節點 `SingletonKey("SEE")` 存 Core codec 的 canonical JSON document（內含 schemaVersion／modVersion／checksum／buildings[]4 欄／compat）＋mirror 標頭 key |
| 1 秒結算、O(1) | `Components/BoilerTickSystem.cs`＋`SEEBoilerRegistry.TickAll` | `ITickableSingleton` 累積 `FixedDeltaTimeInHours` 與 tick 秒數，達 `perf.tickSeconds`（預設 1 s）才結算；兩個 O(n) pass（λ 聚合＋逐棟結算） |
| GUID 命令／查詢 | `SEEBoilerRegistry`、`SEEBoilerController` | registry 同實例 `ToExisting` 綁定 `IBoilerQuery`／`IBoilerLogic`／`ISEEBoilerRegistry` |
| 存檔延遲套用 | `Tick.cs::ApplyDeferredAfterLoad` | Load 只記狀態；第一個結算 tick 重套 toggle／multiplier（`_nodeMultiplier = NaN` 強制寫回） |

## 2. 離線門檻（P3 自檢實測）

- `dotnet build mod/src/SEE.Gameplay -c Release -m:1` → **0 warning / 0 error**。
- `dotnet run --project mod/tests/SEE.Tests -c Release -m:1` → **PASS 383/383**（P2 基線 339＋P3 新增 44）。
- `tools/validate` → **VALIDATION PASSED**（4 個出貨組件、54 loc、24 藍圖、0 warning）。
- `grep -ri "Harmony|TimberPipes.dll|PowerLines.dll|ConveyorBelt" mod/src/SEE.Gameplay` → **0 命中**；無 LINQ、無字串插值、無 `Update`。

## 3. 消耗對帳（`ConsumptionReconciliationTests`，reflection 呼叫純邏輯）

模擬 1 遊戲日（24 × 1 h）與 30 日視窗；`booked`＝整數扣貨（能量/件數）＋期末小數餘量變化：

| 情境 | 期望 | booked 誤差 | 純整數差誤差 |
|---|---:|---:|---:|
| 燃料 滿載 1 日（0.11/h） | 2.64 | 0.000005% | 24.24% |
| 燃料 半載 1 日 | 1.32 | 0.000005% | 24.24% |
| 燃料 零需求／缺料 | 0 | 0% | 0% |
| 燃料 vanilla（半載） | 2.64 | 0.000005% | 24.24% |
| 水 滿載 1 日（0.32/h） | 7.68 | 0.000000% | 8.85% |
| 水 半載 1 日 | 3.84 | 0.000000% | 21.88% |
| 水 零需求／缺料 | 0 | 0% | 0% |
| 水 30 日 | 230.4 | 0.000000% | **0.1736%** |
| 燃料 30 日 | 79.2 | 0.000003% | **0.2525%** |
| 燃料 Plank fallback 3 日 | 7.92 | 0.000002% | 11.62% |

**口徑說明**：契約 §8.3「≤±2%」以「表定速率 × 時間」對「實際 booked 消耗」計；booked 含期末小數餘量，故誤差 ≈ float epsilon。若只比較 **Inventory 整數差**，單日 2.64 燃料只能扣 2～3 件，粒度誤差必然 >2%（24.24%）；30 日視窗即收斂至 0.25%（燃料）／0.17%（水），符合契約 §6.7「<1 貨物餘量不存檔、可忽略」。P8 實機對帳建議同時記錄兩欄並以整數差＋遊戲內庫存讀值為準。

## 4. 偏離、待裁決與 P5／P8 接口

1. **世界節點格式（AD-11）**：遊戲 singleton persistence 無 nested-object primitive，故 node 以 Core codec 的 canonical JSON document 字串（`document` key）儲存；`schemaVersion`／`modVersion`／`checksum` 另以 primitive mirror。文件內仍含 `buildings[]`（4 欄）與 `compat`，與 CONTRACT §6.1 邏輯形狀一致。P5 若要寫 compat，呼叫公開的 `SeeSaveBridge.RecordCompatSnapshot(detected, enabled, failed)`；Integration 需引用 Gameplay（單一 ProjectReference 至 Gameplay，Core 由 transitive 取得）或自行以反射橋接 —— **P5 待裁決**。
2. **read-only 降級**：載入較新 schema 的 node 時，`SeeSaveBridge` 不覆寫世界節點（記 `SEE/Warn/SaveReadOnly`）；per-entity 仍以 v1 形狀寫入。若 P5 要求 per-entity 也停寫，需在 P5 加旗標（P3 不自行擴充）。
3. **維護參數來源**：`IntervalDays`／`TriggerThreshold`／`DurationHours` 以 config（`SeeConfig.RulesFor`）為執行值（§7.10「以當前 config 重算」），藍圖 spec 同值僅作資料對帳；`LoadFloor`／`LoadFactorRange` 目前由 Core `BoilerFormulas` 常數（0.4/0.6）實作，spec 同值但未參與計算。若 Lead 要「spec 優先」，需 P2 改 Core 介面（P3 未自行改契約）。
4. **維護順序（需求 §9.10）**：同 tick 多棟觸發時目前依註冊（元件建立）順序結算，未實作「condition 低者先」的排序（避免每批 O(n log n)）。列待裁決／P6 評估；材料競爭的公平性影響極小。
5. **電池邊界（API_FACTS §1.2-3）**：目前 `Graph.PowerDemand <= 0` 一律 λ=0（Idle，不扣料）。含電池且未充飽時原版 `RequiresPower=true`；v1 依卡片「需求 0 不扣料不磨損」處理，電池充電策略列 P8 實機裁定。
6. **燃料係數**：燃料 fraction 取 `node.OutputMultiplier`（=λ×condition），依 AD-3/D-1；`vanilla` 模式 fraction=1（需求 >0）。水同乘 `OutputMultiplier`（契約 §8.3「表定×min(1,實際輸出/額定)」）。
7. **Paused 取得途徑**：藍圖無 `PausableBuildingSpec`，玩家暫停由 `IBoilerLogic.SetPaused`（P4 面板）驅動；未實作 `IFinishedPausable`（marker，需 PausableBuilding 配套）。若 P4 要接原版暫停按鈕，需 P1 加 `PausableBuildingSpec` 並在本控制器訂閱 `PausedChanged`（P3 未擴充）。
8. **行數預算**：CONTRACT §2.1 標 `SEE.Gameplay ≤2,500` 行；目前約 3.7k 行（含 XML doc；P2 的 `SEE.Core` 亦為 5,868 行 vs 預算 1,200）。行數未列於卡片驗收；建議 Lead／P6 決定是否把「行數預算」改列 soft target。
9. **P8 實機（AI 不得宣告通過）**：`PauseConsumption` 後燃料／水是否真停扣、`SetOutputMultiplier` 寫入後電網 UI／效率同步、同網多引擎 λ 分攤、同棟雙庫存補給、`SingleGoodAllower.Allow` 後搬運工補水、世界節點時序與存讀往返。

## 5. 效能審查（1,000 棟估算）

- 每棟每結算（≈1 s）純邏輯實測（Release, net9）：燃料＋水結算 **25～31 ns**、狀態機＋輸出公式 **31～36 ns**（測試輸出 `PERF|...`）。
- 熱路徑操作數（每棟每結算）：狀態機 1 次（struct，無配置）、`ConditionCurve` 1 次、`OutputHorsepower` 1 次、燃料／水 ledger 各 1 次（struct）、`Inventory` 讀取約 6～8 次、`SetOutputMultiplier` ≤1 次（值不變則跳過）、`GoodAmount` 2 個 struct；**0 LINQ、0 字串插值、0 每 tick 配置、0 每幀全圖掃描**。
- 批次聚合：registry 每批 `O(n)` 兩 pass（λ 聚合＋逐棟結算），`GraphAggregate` 物件重用，steady state 無配置；`SettlementCount`／`LastSettlementBoilers` 為 O(1) 計數器。
- 估算：1,000 棟 ×（純邏輯 ~0.06 µs＋遊戲屬性讀取 ~0.5–1.5 µs＋節點寫入 ~0.3–1 µs）≈ **1–2.5 µs/棟**，低於 3 µs 預算；**此為靜態＋離線估算，實機 Profiler 由 P7／P8 量測**。
