# SEE — Lead 裁決紀錄（2026-11-06）

> 本檔是 Lead 對 P0 契約 §11「待 Lead 裁決」項與 P0 取證結論的正式裁定；效力與 `CONTRACT_SEE.md` 相同。
> P0 完成後由 P6／P8 把本檔內容合併回 `CONTRACT_SEE.md` §11（或保持交叉引用），不得再自行改寫已裁定內容。
> 取證證據索引見 `mod/docs/API_FACTS_SEE.md` 與 `_investigate/findings-*.md`；任何裁定與 IL 事實衝突，先回報 Lead。
>
> **歷史出處標註**：本文的「MWP」一律指分叉來源姊妹作《Mechanical Water Pump》，僅為歷史出處／已實機驗證路徑的引用，不是本模組識別字；`mod/src`、`mod/tests`、`mod/tools` 與出貨資料對 `MWP|Mwp|mwp|MechanicalWaterPump` 的 grep 驗收為 0 命中。

## 一、正式裁定（AD 系列）

| # | 裁定 | 生效範圍 |
|---|---|---|
| **AD-1** icon 暫代 | **允許** v1 用原版 steam engine icon 路徑暫代；`VERIFICATION.md` 必須標「暫代」，P9 完成前不得出「正式版」字樣 | P1／P4／P9 |
| **AD-2** FT 模型策略 | **v1 允許** Folktails 陣營原版模型暫代（優先 Power 群組、尺寸盡量接近；**不得引用 IronTeeth 模型**）；一檔一陣營；P9 自製中立材質後統一 | P1／P9 |
| **AD-3** 容量口徑分離 | 燃料用 `GoodConsumingBuildingSpec` 只作庫存宣告／容量派生；原版消耗**永久 pause**；實際消耗由 controller 以 `FuelPerHour × λ`＋能量換算（Log=1／Plank=2）結算。容量速率生成公式：T1/T2 單一 Log `(fuelStock−0.5)/25`；T3/T4 Log＋Plank 每項 `(fuelStock/2−0.5)/25`（12 檔容量逐格等於表定；避開 float32 ceil 陷阱）。備援（自訂 Inventory／單一 Log）僅 P8 失敗才啟用 | P1／P3／P6 |
| **AD-4** 序列化欄位名 | `GoodAmountSpec`＝**`Id`／`Amount`**（`SEEMaintenanceSpec.Materials`、`BuildingCost`）；`ConsumedGoodSpec`＝**`GoodId`／`GoodPerHour`**（燃料）。兩者不得混淆 | P1／P6 |
| **AD-5** `compat.detected` 與 REQ-SAVE-09 | 採需求 §14.1 schema（可記模組簡名）；REQ-SAVE-09 限縮解讀為「存檔不得寫外部模組的型別／組件 Id／接點資料」 | P5 |
| **AD-6** 覆蓋率門檻 | 權威＝**REQ-NFR-07：`SEE.Core` 行 ≥80%、分支 ≥70%**；`SEE.Tests` 全域行 ≥70% 作為第二指標，兩者分欄回報，不得互相代替 | P2／P6 |
| **AD-7** GENERATED 檔頭 | 藍圖 JSON 不得加註解或未知欄位；採「同目錄 `README.md` 標註＋`engines.derived.json` 記來源雜湊＋生成器 `--check` 零 diff＋`Golden/blueprints.sha256`」四件套實現 REQ-ARCH-14 | P1／P6 |
| **AD-8** loc CSV | 採 **3 欄 `ID,Text,Comment`、UTF-8 無 BOM**（原版與姊妹作實檔為準；需求 L1 的「含 BOM」以實證優先）；含逗號欄位加引號 | P1／P6 |
| **AD-9** `GoodPoweredGeneratorSpec` | **24 份藍圖一律移除**（IL：`GoodPoweredGenerator.UpdateState` 每 tick `SetOutputMultiplier(IsConsuming?1:0)` 並依 `RequiresPower` Pause/Resume → 覆寫我方 λ、與燃料 toggle 互搶）。改由 controller 完整接管輸出倍率＋燃料 toggle＋零需求邏輯；視覺／音效 spec（`GoodPoweredGeneratorAnimatorSpec`／`PowerGeneratorParticleControllerSpec`／`PowerGeneratorSoundsSpec`／`MechanicalNodeIlluminatorSpec`／`TemplateAttachmentsSpec`）可保留（P8 驗證動畫／煙霧） | P1／P3／P6／P8 |
| **AD-10** 水艙自動允許 Water | `SingleGoodAllower` 預設 `AllowedGood=null` → 所有貨物 `AllowedAmount=0`（水進不去）。控制器完成／初始化時：`if (!allower.HasAllowedGood) allower.Allow("Water");`（`Allow`/`HasAllowedGood` public）；不覆寫玩家已選 Water/Badwater | P3 |
| **AD-11** D-6 存檔偏離 | 採**混合形態**：世界節點 `SEE`（`Singletons.SEE`）只存 `schemaVersion`／`modVersion`／`checksum`／`buildings[]` 4 索引欄位／`compat`；逐棟 7 個可變欄位存 per-entity `ComponentKey("SEE")`（`IPersistentEntity`）。需求 §14.1 的 `buildings[]` 狀態欄位改存 per-entity，屬登記偏離；理由＝singleton 載入時序風險＋per-entity 已實機驗證；P8 若證實節點時序穩定，P5 可搬回（schemaVersion+1＋遷移器） | P3／P5 |
| **AD-12** 狀態優先序 | 以需求 §3.3／§15.3 表為權威，**並以適用性解決 Idle／磨損帶矛盾**：磨損帶（Critical/Worn/MaintenanceDue/Running）只在 λ>0 適用、`Idle` 只在 λ<=0 適用；優先序 `Paused > Maintaining > NoFuel > NoWater > NeedsMaintenance > Critical > Worn > MaintenanceDue > Idle > Running`；行為閘門與狀態解耦（前六者→輸出 0／不耗料／不磨損）。必要測試組合見 CONTRACT §8.2 | P2／P3／P6 |
| **AD-13** config 檔案形狀 | `SEE_Config.json` 的 canonical 形狀＝**nested 物件群組**（照需求 §20.7 範例；`balance.fuelRatePer100hp.T1` 寫成嵌套物件；`lodDistances` 用陣列）；P0 骨架 `SeeConfigCodec` 的 flat-dotted 解析列 P2 必修。`mod/data/SEE_Config.json` 已符合 | P2／P3／P6 |

## 二、追認 P0 取證結論（D-1～D-6；詳見 `API_FACTS_SEE.md`）

| # | 追認內容 |
|---|---|
| D-1 | 輸出調變 A 案可行：public `MechanicalNode.SetOutputMultiplier(float)`；輸出＝`Active ? ceil(nominal × multiplier) : 0`；λ 用防回授公式 `λ=clamp01(PowerDemand / max(1, S_other + ΣN_SEE))`；燃料 fraction 取 `node.OutputMultiplier`（非 Actuals 反推）。前提＝移除 `GoodPoweredGeneratorSpec`（AD-9） |
| D-2 | 燃料 GCB＋液體 Stockpile 同棟機制上可並存（兩個獨立 Inventory、容量分立、無原版先例）；搬運工/工人經 FillInput 補燃料、輸送帶只搬固體、管網只理液體 Stockpile；合法停扣＝快取 `GetGoodConsumingToggle().PauseConsumption()`；`SetConsumptionDisabled` 對發電端 no-op、spec 速率歸零不可行。R-C：含 Stockpile/SimpleOutput 的建築必須 `EnterableSpec`＋`BuildingAccessibleSpec`＋entrance。PipeTank 混裝誤差 ≤11.76%≤12% gate（待實機） |
| D-3 | 燃料容量＝`Σ ceil(GoodPerHour×25)`（float32；12/12 等於表定 fuelStock）；水艙容量**必須藍圖寫死**（原版無派生；`0.3f×50→ceil 16` 實證）；貨物整數化＝浮點餘量滿 1 扣整數 |
| D-4 | 需求 0：原版靠 `GoodPoweredGenerator` 二值停料；移除該 spec 後由 controller 實作（multiplier 0＋Pause＋不磨損＋不扣水）；`SetConsumptionDisabled` 對發電端 no-op；存讀檔後必須重套 multiplier/toggle（不存檔） |
| D-5 | 維護材料／道路 API 全部存在且簽名一致，MWP `MaintenanceLedger`＋`IMaintenanceStore` 原樣可複製（`DistrictInventoryRegistry` 在 `Timberborn.InventorySystem`，非 GameDistricts） |
| D-6 | 混合形態（AD-11）：`ISaveableSingleton`／`ISingletonLoader` 世界節點機制已證可行，落點 `world.json → Singletons.SEE`；`EntityRegistry.Entities`＋`GetComponent<T>()` 列舉；`EntityComponent.EntityId`＝Guid、原生持久化 primitive → Guid 字串；單一 singleton 時序風險以 per-entity 降級吸收 |

## 三、仍開放（僅能由 P8 人類實機關閉；AI 不得宣告通過）

| # | 項目 | 驗證 |
|---|---|---|
| 1 | `PauseConsumption`＋controller 扣料的消耗對帳 | 1 遊戲日燃料／水 ≤±2%（REQ-IMPL-04） |
| 2 | 移除 `GoodPoweredGeneratorSpec` 後動畫／煙霧／音效 | P8 建造檢查 |
| 3 | 同棟雙庫存（GCB 燃料＋液體 Stockpile）的 UI／清空／多系統行為 | P8 |
| 4 | PipeTank 對燃料體積的混裝與 `FluidGoodId` 污染 | P8＋TP |
| 5 | 世界節點 singleton 時序／存讀往返 | P8 存檔測試矩陣 |
| 6 | 同網多引擎／電池的 λ 分攤 | P8 |
| 7 | 水艙 `Allow("Water")` 後搬運工／管網實際補水 | P8 |

## 四、契約紀律

- 上述裁定後，`CONTRACT_SEE.md` §11 的對應項標記為「已裁決（見 `LEAD_RULINGS.md`）」。
- 任何卡片若發現裁定與 IL 事實衝突，**先回報 Lead**，不得自行改契約或數值。
- P0 自己的骨架允許與 P2～P5 最終實作有落差，但**落差必須列在 `mod/src/README_P0_SKELETON.md` 的 TODO／已知落差表**；不得默默偏離。
