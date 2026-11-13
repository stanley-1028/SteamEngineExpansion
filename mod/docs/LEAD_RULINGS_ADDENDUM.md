# Lead 裁決增補（2026-11-06 第二輪）

> 用途：P0 在 `LEAD_RULINGS.md` 內追加了 AD-9～AD-13 與 D-1～D-6 結論（以 P0 取證為依據）。本檔為第二輪追認稿，**已由 Lead（2026-11-06）確認採用**；效力與 `LEAD_RULINGS.md`、`CONTRACT_SEE.md` 相同。
> 流程註記：後續卡片若要新增裁定，只能寫「待 Lead 裁決」，不得再自行擴充 `LEAD_RULINGS.md`。

## 一、追認（內容與 IL／本機實證一致）

| # | 裁定 | 狀態 |
|---|---|---|
| **AD-9** | 24 份藍圖**移除 `GoodPoweredGeneratorSpec`**（其 `UpdateState` 每 tick `SetOutputMultiplier` 並依 `RequiresPower` Pause/Resume，會覆寫我方 λ）；輸出倍率／燃料 toggle／零需求全由 controller 接管；視覺／音效 spec 可保留 | ✅ 追認（P1／P3／P6／P8） |
| **AD-10** | 水艙初始化時 `SingleGoodAllower` 若無 AllowedGood → `Allow("Water")`；不覆寫玩家已選 Water／Badwater | ✅ 追認（P3） |
| **AD-12** | 狀態優先序 `Paused > Maintaining > NoFuel > NoWater > NeedsMaintenance > Critical > Worn > MaintenanceDue > Idle > Running`；磨損帶僅在 λ>0 適用、`Idle` 僅在 λ≤0 適用；行為閘門與狀態解耦 | ✅ 追認（P2／P3／P6） |
| **AD-13** | `SEE_Config.json` canonical 形狀＝**nested 物件群組**（照需求 §20.7）；P0 骨架的 flat-dotted 解析列 P2 必修 | ✅ 追認（P2／P3／P6） |
| D-1 | 輸出調變 A 案可行（public `MechanicalNode.SetOutputMultiplier(float)`）；λ 用防回授公式 `λ=clamp01(PowerDemand / max(1, S_other + ΣN_SEE))`；燃料 fraction 取 `node.OutputMultiplier` | ✅ 追認為**實作假設**；P3 必須加保護（clamp、不得低載超耗、S_other 邊界）、P8 實機驗證，若失敗依原備援改占空比 B 案 |
| D-3 | 燃料容量＝`Σ ceil(GoodPerHour×25)`（float32）；水艙容量藍圖寫死；貨物整數化用浮點餘量 | ✅ 追認 |
| D-4 | 移除 `GoodPoweredGeneratorSpec` 後由 controller 實作零需求停料；`SetConsumptionDisabled` 對發電端 no-op；存讀檔後重套 multiplier／toggle | ✅ 追認 |
| D-5 | 維護材料／道路 API 與 MWP 實作可直接複用 | ✅ 追認（P8 實機驗） |

## 二、有條件追認（重要偏離）

| # | 項目 | Lead 裁定 |
|---|---|---|
| **AD-11 / D-6 存檔形態** | P0 將原「純世界節點 `SEE.buildings[]`」改為**混合形態**：世界節點只存 `schemaVersion`／`modVersion`／`checksum`／`buildings[]` 四索引欄位（entityId／tier／size／faction）／`compat`；逐棟 7 個可變欄位存 per-entity `ComponentKey("SEE")` | **有條件追認（v1）**：①世界節點仍照需求 §14.1 存在且含 `buildings[]` 索引與 `compat`；②逐棟完整狀態以 per-entity 為**唯一真實來源**，世界節點不得再存可變狀態（避免雙寫不一致）；③`schemaVersion` 維持 1；④此偏離必須登錄在 `CONTRACT_SEE.md` §11、`REQ_TRACEABILITY.md`、`docs/VERIFICATION.md`，並在 P8 回報；⑤只有在 P8 證實世界節點時序穩定後，P5 才「可選」搬回單一世界節點（需 schemaVersion+1＋遷移器）；⑥不得為追需求字面而犧牲存檔安全。 |

## 三、維持我的原裁定（不因 P0 追加而改變）

- AD-1／AD-2／AD-5～AD-8：維持 `LEAD_RULINGS.md` 內容（icon 暫代、FT 模型、compat 口徑、80/70 覆蓋率、GENERATED 四件套、loc 3 欄無 BOM）。
- D-6 的「純世界節點」**已被 AD-11 取代**；本檔第二節為現行有效裁定。
- 所有「AI 不得宣告實機通過」的紀律不變。

## 四、下一步

- P1 必須把 AD-9（移除 `GoodPoweredGeneratorSpec`）、AD-11（世界節點索引欄位）、AD-13（config nested）、AD-4（欄位名）、AD-8（loc）全部落進生成器與資料。
- P2 必修 flat-dotted config 解析、狀態機 AD-12、覆蓋率 80/70。
- P3 必須實作 AD-10、D-1 的 λ 保護與消耗對帳 ≤±2%。
- P5 依 AD-11 實作存檔橋（世界節點＋per-entity），並寫遷移器骨架。

## 五、P3 回報的介面／偏離裁定（2026-11-06 第三輪，Lead）

> 來源：`mod/src/SEE.Gameplay/P3_IMPLEMENTATION_NOTES.md` §4。以下為正式裁定，P4／P5／P6／P8 照此執行。

| # | 項目 | 裁定 |
|---|---|---|
| R-1 | P5 存檔橋需呼叫 Gameplay 的 `SeeSaveBridge.RecordCompatSnapshot`；目前 Integration 只引用 Core | **允許 `SEE.Integration` 對 `SEE.Gameplay` 建單一 `ProjectReference`**（Core 由 transitive 取得）。需求 §11.1 的依賴方向本就允許 Integration→Gameplay；CONTRACT §2 註明。P6 需驗證仍只有 4 個出貨組件、無循環依賴 |
| R-2 | 讀到較新 schema 時的唯讀降級：世界節點不覆寫，per-entity 仍寫 v1 | **v1 照 P3 現狀**（per-entity 照寫）；P5 可選擇加旗標停寫 per-entity，但非必須；不得因此拒絕載入或清空資料 |
| R-3 | 維護參數來源（config vs 藍圖 spec） | **以 config 為執行值**（需求 §7.10「以當前 config 重算」）；藍圖 spec 同值僅作資料對帳。`LoadFloor`／`LoadFactorRange` 由 Core 常數實作、spec 同值，v1 接受（P6 記錄） |
| R-4 | 同 tick 多棟維護的公平性（condition 低者先） | **v1 接受註冊順序**，不做 O(n log n) 排序；列為已知限制，P6 記錄、P8 觀察即可 |
| R-5 | 電池／`PowerDemand <= 0` 時 λ=0（Idle、不扣料） | **接受**（符合 REQ-CORE-08）；電池充電策略列 P8 實機觀察，不列入 v1 阻擋 |
| R-6 | 玩家暫停的取得途徑 | **v1 由 P4 面板命令 `IBoilerLogic.SetPaused` 為權威**；**不新增 `PausableBuildingSpec`**（避免藍圖與生成器再改一輪）。P4 必須提供暫停／繼續按鈕；P8 驗證暫停時確實不扣料不磨損 |
| R-7 | 行數預算（Core 1,200／Gameplay 2,500 行） | **改列 soft target**，不作驗收門檻；P6 只回報實際行數與理由，不因此 FAIL |
| R-8 | 消耗對帳口徑 | 接受 P3 的「booked（含期末小數餘量）」為 ≤±2% 主口徑，並**同時回報 Inventory 整數差**；30 日整數差 0.25%／0.17% 可接受。P8 實機必須兩欄都記 |

> P3 離線數據（Lead 複驗）：`build.ps1 -Clean` 0 warning、**PASS 383/383**、generator 5/5、`VALIDATION PASSED`、staged 87 檔 550 KB。

## 六、2026-11-08 搬運修根因（Lead 裁定，取代 D-2／AD-10 的水側做法）

> 來源：玩家回報「普通搬運工無法把水與燃料搬進 steam engine」；`build.ps1 -Clean` 離線門禁全綠後由 Lead 記錄。
> 本節取代 `LEAD_RULINGS.md` D-2「水＝液體 Stockpile」與 AD-10「`SingleGoodAllower.Allow("Water")`」的實作方式，
> 其餘（燃料 GCB＋永久 pause＋controller 扣料、λ、維護、存檔）不變。

| # | 裁定 | 生效範圍 |
|---|---|---|
| **AD-14** 水艙獨立為程式建立的 public input 庫存 | 燃料只留 `GoodConsumingBuildingSpec`（`ConsumedGoods` 不得含 Water）；水艙由 `SEEWaterInventoryInitializer`（`IDedicatedDecoratorInitializer<SEEBoilerSpec, Inventory>`）建立 `WaterTank` public input `Inventory`：`StorableGood.CreateGiveableAndTakeable("Water")`、`HasPublicInput()`、容量＝`SEE.SEEBoilerSpec.WaterCapacityGoods`。controller 在完工時 `Enable()`、離開完成態 `Disable()`、扣水 `TakeExisting`。**不得**恢復 `StockpileSpec`（Stockpile＋GCB 雙 `NoHaulingPostStatus` 崩潰）或把 Water 放回 GCB。 | P1／P3／P6／P8 |
| **AD-15** 搬運驗收口徑 | 離線：generator gate 2 改為「水艙獨立（ConsumedGoods 無 Water、水艙容量＝表定）」、validator 硬錯誤、`WaterTankSupplyTests` 鎖 wiring。實機：建造 1 棟＋Hauling Post，確認搬運工把 **Log 與 Water 都送進引擎**、面板兩列數字上升、存讀檔水艙保留；未經人類簽核前不得宣稱通過。 | P6／P8 |

## 七、2026-11-08 夜間待機運轉（Lead 裁定，取代 D-4 的「需求 0→Idle」）

> 來源：玩家回報「steam engine 一旦有燃料和水應該一直運作，而不是到了晚上就停機」；`build.ps1 -Clean` 離線門禁全綠後由 Lead 記錄。
> 根因證據：`_investigate/out4-night/README.md`（`WorkplacePowerConsumptionSwitch` IL＋`GoodPoweredGeneratorAnimator` IL）。
> 根因（已取證）：夜間製造業收工 → 原版 `WorkplacePowerConsumptionSwitch.Tick` →
> `UpdatePowerConsumption()` 以 `!AnyWorkerHasJobRunning()` 呼叫 `MechanicalBuilding.SetConsumptionDisabled(true)`
> → 耗電端 `PowerInput=0` → `MechanicalGraph.PowerDemand=0` → 我方原本 `λ=0 → Idle → multiplier 0`，飛輪與煙霧停止。
> 本節取代 D-4／REQ-CORE-08／REQ-POWER-09 的「需求 0 → Idle、不耗料、不磨損」為預設行為；其餘（λ 防回授、比例耗燃料、
> 維護、存檔）不變。

| # | 裁定 | 生效範圍 |
|---|---|---|
| **AD-16** 夜間待機運轉 | 只要燃料與水充足且未暫停／維護／機況 0，鍋爐**不因電網需求 0 而停機**：`λ_run = max(λ, balance.standbyLoadRatio)`（預設 0.10、範圍 0.00–1.00；`0` 可恢復舊行為）。輸出、耗燃料、耗水、磨損一律使用 `λ_run`；`Idle` 只保留給「未接電網（`Graph==null`）」或玩家把 `standbyLoadRatio` 設 0 的情況。視覺依據：原版 `GoodPoweredGeneratorAnimator.UpdateAnimation` 只看 `MechanicalNode.OutputMultiplier > 0`（動畫速度為常數），因此待機 10% 與滿載在畫面上同為「持續運轉」。 | P2（`BoilerFormulas.OperationalLoadRatio`）／P3（controller `λ_run`）／P6（config validator）／P8 |
| **AD-17** 待機運轉的取捨與驗收 | 取捨：夜間仍按待機比例燒燃料／水，且磨損公式有 0.40 下限（待機磨損≈0.46×滿載速率），維護會比純待機略頻繁；要完全省料可把 `balance.standbyLoadRatio` 設 0。離線驗收：`OperationalLoadRatio` 公式＋config clamp＋狀態機（待機 0.10 → `Running`）測試。實機驗收：白天照常按需求調節；**入夜後**引擎仍 `Running`、飛輪/煙霧持續、燃料與水緩慢下降、輸出約額定 10%（× 機況）；把 `standbyLoadRatio` 設 0 並重載後應恢復舊的 `Idle` 行為。未經人類簽核前不得宣稱通過。 | P6／P8 |

## 八、2026-11-08 玩家回報：搬運工需求狀態＋耗水（Lead 裁定）

> 來源：玩家回報「即便是沒有搬運工也好，也不會顯示『沒有搬運工就不能運作』，以及耗水量不要太大」。
> 取證：本次以 `ApiIndex il` 重新 dump 遊戲 IL（`_investigate/out5-bugfix/il-nohaulingpost.txt`、`il-gcb-describer.txt`），
> 並沿用 `_investigate/out2/inventory/il-goodconsuming.txt`、`il-gcb-config.txt`。離線門禁全綠後由 Lead 記錄。
> 本節新增 AD-18～AD-20；其餘（燃料 GCB＋原版消耗永久 pause、λ、維護、存檔、AD-16 待機運轉）不變。

| # | 裁定 | 生效範圍 |
|---|---|---|
| **AD-18** 完工鍋爐不得顯示原版「需要搬運工才能運轉」狀態 | 玩家要求：即使沒有搬運工，引擎也不顯示 `Status.Hauling.NoHaulingPost`（zhTW「需要搬運工才能運轉。」／enUS「Requires haulers to function.」）。根因（IL 已證實）：任何含 `GoodConsumingBuildingSpec` 的建築都由 `GoodConsumingBuildingSystemConfigurator` 掛上 `GoodConsumingBuildingStatusInitializer`＋`NoHaulingPostStatus`；該 initializer 的 `OnEnterFinishedState` **只在建築沒有 `Workplace` 時**呼叫 `NoHaulingPostStatus.Initialize(...)`。本模組鍋爐刻意免工人（生成器閘門 5 禁止 `WorkplaceSpec`），因此被原版判定為「需要搬運工」。修法：`SEEBoilerController` 在完工／讀檔啟用後以 public `NoHaulingPostStatus.Disable()` 關閉該狀態（**不刪除燃料 GCB、不改原版庫存／搬運路徑、不用 Harmony**）。`Disable()` 會移除 `HaulCandidate.InHaulingCenterRangeChanged` 訂閱並停用 toggle，但 `DistrictBuilding.ReassignedDistrict` 仍會呼叫 `UpdateStatus()`，故區域重指派後延後一個 settlement tick 再關一次；每次啟用連續關 3 個 tick，涵蓋 `IFinishedStateListener` 順序不確定。完工建築燃料／水耗盡仍以我方面板 `NoFuel`／`NoWater` 呈現；原版 `LackOfResourcesStatus`（區域內完全沒有該貨物的紅色示警）保留。擺放預覽（`BlockObject.IsPreview`）時原版說明列「需要搬運工。」屬原版預覽 UI、不是完工狀態，v1 不改，列 P8 觀察。 | P3（`SEEBoilerController.HaulerStatus.cs`）／P6（source-scan 測試）／P8 |
| **AD-19** 全 12 檔耗水率減半（玩家要求「耗水量不要太大」） | `tierWaterRatePer100Hp`：T1 0.080→**0.040**、T2 0.070→**0.035**、T3 0.040→**0.020**、T4 0.018→**0.010**（每 100 hp 每小時；T4 取 0.010 以維持 `balance.waterRatePer100hp` 的 0.010 下限）。逐檔 `waterPerHour = round(hp/100 × rate, 2)`、水艙 `waterTankGoods = round(waterPerHour × 200)` **同步重算**（例：Small.T1 0.10→0.05、水艙 20→10；Large.T1 0.29→0.14、水艙 58→28；Large.T2 0.50→0.25、水艙 100→50）。`waterTankHours` 維持 200（**滿水艙自主時間不變**，每次補水只需一半貨物）；燃料、hp、研究／建造費、維護不變。水曲線仍嚴格遞減且 Mk.IV／Mk.I = 4.0×（REQ-BAL-09 下限）；T2–T4 仍 ≤ 原版 0.075（REQ-BAL-03）。需求文檔 §4.1／§6.2／§8 的舊耗水數值由本裁定取代。 | P1（`data/engines.json`、`SEE_Config.json`、生成器）／P3／P6（validator／balance-sim）／P7（BALANCE_REPORT）／P8 |
| **AD-20** 生成器閘門 1 由「水艙貨物 ≥ 8× 燃料貨物」改為「水艙自主時間 ≥ 8× 燃料庫存自主時間」 | 舊閘門是「貨物比」代理量，只在舊耗水率成立；耗水減半後貨物比必然下降，但設計不變量並不是貨物比。新閘門：`waterTankHours ≥ 8 × fullInventoryWorkHours`（現值 200 ≥ 8×25 = 200），直接檢查 `engines.json` 的兩個契約常數，與費率無關；語意＝「滿水艙可撐滿燃料庫存 8 倍的時間」。生成器 gate 1 名稱／detail 改為 autonomy；`tools/release/release-gates.json` G04 evidence 文字同步。 | P1／P6 |

> 離線門禁與實機待簽核項見 `docs/VERIFICATION.md`、`CHANGELOG.md`。本輪（2026-11-08 熱修-5）實測：
> `build.ps1 -Clean` → **BUILD PASSED（G01–G22）**、**PASS 510/510**、generator `CHECK PASS（80 files）`＋`gates 5/5`、
> validator **VALIDATION PASSED**（12/12）、balance-sim **PASS 12/12（4/4）**、負向測試 **control + 6/6**、
> staged **132 檔／2,379,665 bytes**、package sha256 `4a661a1ed86301131b267ec7253c7972f36deeabcbd70d206bc29fd7023142c5`。
> 實機驗收（AI 不得宣告通過）：完工鍋爐不再出現搬運工需求狀態、面板耗水換算約減半、既有存檔可正常載入、搬運工仍可補燃料與水。

## 九、2026-11-14 UI／GUI 原版化＋專屬 Steam Engines 工具組（Lead 裁定；玩家要求）

> 來源：玩家要求「把 UI 和 GUI 都原版化，收縮型菜單也是，同時為他們的建築菜單做收縮型菜單」。
> 考察：原版 `BlockObjectToolGroupSpec` 只有 Id/Order/NameLocKey/Icon/FallbackGroup（無嵌套）；`ToolGroupButton`
> 點分類＝`EnterToolGroup` 展開該組建築列、再點／切換＝收合（即「收縮型菜單」）。原版面板語彙取自遊戲內建
> `StreamingAssets/Modding/UI.zip` 的 UXML/USS（`entity-sub-panel`／`entity-panel__*`／`progress-bar`／`game-toggle`／
> `game-dropdown`），按鈕以 `VisualElementLoader` 取原版 `Game/EntityPanel/DemolishableFragment` 的
> `entity-fragment__button`（`NineSliceButton` 為 internal，不能直接 new）。離線門禁全綠後由 Lead 記錄。

| # | 裁定 | 生效範圍 |
|---|---|---|
| **AD-21** 面板原版化（不新增 UXML/USS 資產） | SEE 面板改用原版 USS 類別與原生控制項（九宮格卡片、原版字體/狀態色類、原生九宮格按鈕、`Timberborn.CoreUI.ProgressBar`、`game-toggle`、`Timberborn.DropdownSystem.Dropdown`）；原本 Unity `Foldout` 改為原版風格收合標題（預設展開、跨 Render 保留、不寫存檔）。禁止 inline 顏色/字級，唯一例外是需求 §15.6 A1 的形狀/顏色 swatch。原生 dropdown/按鈕取用失敗時必須 fallback（`DropdownField`／plain `Button`＋原版 class），不得讓面板初始化拋例外。行為、4 Hz 髒標記、命令與 loc key 集合不變（本輪 loc 只 +1 工具組鍵，見 AD-23）。 | P4／P6／P8 |
| **AD-22** 24 檔改用專屬底部工具組（覆寫需求 §15.1 P3「不新增工具組」） | 新增 `BlockObjectToolGroupSpec`：`Id=SteamEngines`、`Order=72`（原版 Power 70 之後、Science 80 之前）、`NameLocKey=UI.SEE.ToolGroup.SteamEngines`、`Icon=ToolGroupIcons/SteamEngines`、`FallbackGroup=false`；24 份藍圖 `PlaceableBlockObjectSpec.ToolGroupId` 由 `Power` 改 `SteamEngines`（`ToolOrder` 110–142 不變）。玩家可見效果＝底部分類列多一個可展開／收合的分類按鈕，24 檔不再混進原版 Power。group 藍圖與圖示放 `data/BlockObjectToolGroups/`、`data/ToolGroupIcons/`，**不**列入 TemplateCollection（IL 已證 mod 內所有 `*.blueprint.json` 由 `ModSystemFileProvider<BlueprintAsset>` 掃描載入）。此裁定取代需求 §15.1 P3「不新增工具組」與 CONTRACT §4.9 舊文；需求其餘 UI 條文不變。 | P1／P6／P8 |
| **AD-23** 本地化 63→64 鍵 | 新增 `UI.SEE.ToolGroup.SteamEngines`（enUS "Steam Engines"／zhTW、zhCN「蒸汽引擎」），作為 AD-22 分類按鈕顯示名；三語 CSV、生成器 `ExpectedKeyCount=64`、validator、`UiContractTests`、文件同步。前綴仍為允許的 `UI.SEE.`；不得覆寫任何原版鍵。 | P1／P4／P6 |

> 本輪（2026-11-14）離線實測：`build.ps1 -Clean` → **BUILD PASSED（G01–G22 全綠）**、四程式集 0 warning、
> **PASS 522/522**（含新增 `NativePanelContractTests` 12 項）、generator `CHECK PASS（0 differences, 83 files, 24 blueprints）`＋
> `gates 5/5`、validator **VALIDATION PASSED（13/13 gate group、1 既有 WARN、0 error）**、
> staged **135 檔／2,385,102 bytes**、package sha256 `9977ed808df154171913eaefc76a05087f75af1cb75989adccb2afa58699cca3`。
> 實機待簽核：面板原版外觀、收合 glyph、原生 dropdown、150% 縮放、底部 Steam Engines 按鈕/圖示/展開收合/24 檔排序、
> 重載不重複（`docs/TEST_PROTOCOL.md` 步驟 1b／1c；`mod/src/SEE.UI/README.md` 待人工清單）。
