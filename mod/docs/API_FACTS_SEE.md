# SEE — verified Timberborn API facts (1.1.2.8-ffc2de0-sw)

> 本檔是 Steam Engine Expansion（SEE）的遊戲 API ground truth，屬 SEE-P0 交付（PLAN §1.5 D-1～D-6）。
> 所有結論均取自**本機安裝遊戲組件**的 Mono.Cecil metadata／IL dump，不是文件推測；每條均附型別全名、成員簽名或 IL 片段與證據檔路徑。
> 取證工具：`_investigate\probe\ApiIndex`（Mono.Cecil 0.11.5，離線、唯讀遊戲目錄）。
> 遊戲版本：`1.1.2.8-ffc2de0-sw`；Managed 目錄：`C:\Program Files (x86)\Steam\steamapps\common\Timberborn\Timberborn_Data\Managed`。
> 取證日期：2026-11-06（P0）。
> 狀態標記：**已證實**＝離線 IL/metadata 證據；**待實機**＝需遊戲內量測，AI 不得宣告通過；**不可行＋備援**＝已證實無公開 API，改用替代方案。
> 本檔是 `CONTRACT_SEE.md` 的證據附錄：契約寫「要做什麼」，本檔寫「遊戲 API 允許什麼」。
>
> **歷史出處標註**：本文出現的「MWP」一律指分叉來源姊妹作《Mechanical Water Pump》，僅作為「證據來源／可複用性／已實機驗證」的歷史引用，**不是**本模組的識別字；`mod/src`、`mod/tests`、`mod/tools` 與出貨資料對 `MWP|Mwp|mwp|MechanicalWaterPump` 的 grep 驗收為 0 命中。

## 0. 證據索引與重現方式

### 0.1 證據檔

| 證據檔 | 內容 | 產生 |
|---|---|---|
| `_investigate\out2\lead\mech-graph.txt` | `MechanicalGraph`/`MechanicalGraphManager`/`MechanicalNodeActuals`/`IBattery`/`MechanicalBuilding`/`MechanicalNodeSpec` 成員 dump | Lead |
| `_investigate\out2\lead\il-node-key.txt` | `MechanicalNode` 的 `UpdatePowerOutput`/`UpdateActiveState`/`SetOutputMultiplier`/`get_PowerEfficiency`/`get_Active`/`get_Powered`/`InitializeActuals`/`InitializeConstantParameters` IL | Lead |
| `_investigate\out2\lead\il-graph.txt` | `MechanicalGraph` 全方法 IL（`get_PowerEfficiency`/`get_Powered`/`get_PowerSurplus`/`AddNodeToCounters`/節點事件） | Lead |
| `_investigate\out2\lead\il-mechanicalbuilding.txt`、`mech-graph.txt` | `MechanicalBuilding.ActiveAndPowered/Efficiency/SetConsumptionDisabled`；`MechanicalNode.SetInputMultiplier/UpdatePowerInput` IL | Lead |
| `_investigate\out2\lead\dump-goodconsuming.txt`、`il-goodconsuming-body.txt` | `GoodConsumingBuilding`/`Spec`/`GoodConsumingToggle`/`InventoryInitializer`/`PoweredGoodConsumingBuilding` 成員與 IL（容量派生、暫停 API） | Lead |
| `_investigate\out2\lead\dump-goodamount.txt` | `GoodAmountSpec`（`Id`/`Amount`）與 `GoodAmount`（`GoodId`/`Amount`）欄位 | Lead |
| `_investigate\out2\lead\dump-entity-registry.txt` | `EntityComponent.EntityId`（Guid）、`EntityRegistry.Entities`、`EntityComponentRegistry.GetAll<T>`、`ISaveableSingleton`、`SerializedWorldFactory` | Lead |
| `_investigate\out2\power\**` | D-1／D-4 補強（`forensics-power`） | 隊友 |
| `_investigate\out2\save\**` | D-6／A5 存檔與載入（`forensics-persistence`；`findings-persistence-loading.md`） | 隊友 |
| `_investigate\out2\inventory\**` | D-2／D-3／A4 庫存與維護（`forensics-inventory`；`findings-inventory-maintenance.md`） | 隊友 |
| `mod\docs\api\**` | 由 MWP 專案繼承的歷史 IL dump（交叉比對用；本檔不以其為唯一證據） | MWP |

### 0.2 重現指令（唯讀）

```powershell
$tool    = 'C:\Users\user\Downloads\timber steam engine\_investigate\probe\ApiIndex\bin\Release\net9.0\ApiIndex.dll'
$managed = 'C:\Program Files (x86)\Steam\steamapps\common\Timberborn\Timberborn_Data\Managed'
dotnet $tool dump $managed out.txt 'Timberborn\.MechanicalSystem\.(MechanicalGraph|MechanicalNode|MechanicalNodeActuals|MechanicalBuilding)$'
dotnet $tool il   $managed out.txt 'Timberborn\.MechanicalSystem\.MechanicalNode$' 'UpdatePowerOutput|SetOutputMultiplier|get_ActiveAndPowered'
dotnet $tool il   $managed out.txt 'Timberborn\.GoodConsumingBuildingSystem\.GoodConsumingBuilding$' 'ConsumeSupplies|UpdateIsConsuming|Awake'
dotnet $tool index $managed type-index.txt   # ApiIndex 沒有 find 模式；先產全型別索引再 grep
```

## 1. D-1 ＝ 動力輸出與網路負載（V-IMPL-2／V-IMPL-3）

### 1.1 (a) 發電端能否逐棟寫入輸出值？— **已證實：可以；方案 A（逐棟節點調變）可行**

`Timberborn.MechanicalSystem.MechanicalNode`（public class，`BaseComponent`）公開成員：

| 成員 | 簽名 | 證據 |
|---|---|---|
| `SetOutputMultiplier` | `public void SetOutputMultiplier(float value)` | `il-node-key.txt` |
| `OutputMultiplier` | `float { get; }`（setter 為私有；`SetOutputMultiplier` 先比對舊值） | 同上 |
| `Actuals` | `MechanicalNodeActuals { get; }`；其 `PowerOutput`/`PowerInput`/`BatteryCharge`/`BatteryCapacity` 有 public getter，setter 為內部 `SetPowerOutput` 等 | `mech-graph.txt` |
| `Graph` | `MechanicalGraph { get; }`（可為 null） | `mech-graph.txt` |
| `Active` | `bool { get; }`＝`_blockableObject.IsUnblocked()` | `il-node-key.txt` |

實際輸出公式（`UpdatePowerOutput` IL，`il-node-key.txt`）：

```
Actuals.SetPowerOutput( Active
    ? Mathf.CeilToInt( _nominalPowerOutput * OutputMultiplier )
    : 0 )
```

- `_nominalPowerOutput` 於 `InitializeConstantParameters` 由 `MechanicalNodeSpec.PowerOutput` 寫入（同檔 IL）。
- `SetOutputMultiplier` IL：值有變 → `set_OutputMultiplier(value)` → **立即 `UpdatePowerOutput()`**（同檔 IL）。
- 結論：**V-IMPL-2 首選 A 案已證實可行**。控制器每 tick 可對單棟寫入乘數，實際輸出 = `ceil(藍圖 PowerOutput × 乘數)`；乘數無內建 clamp（IL 只有乘法），實作自行限制在 `[0,1]`。B 案（占空比）僅保留為實機發現電網抖動時的備援。
- 建議用法（P2／P3）：`multiplier = ConditionCurve.Multiplier(condition) × loadFactor`；建築被暫停／阻擋（`Active=false`）時輸出自然為 0。

**⚠ 關鍵衝突（已證實，`forensics-power` findings §1.5）：原版 `GoodPoweredGeneratorSpec` 會每 tick 覆寫輸出倍率。**
- `PowerGenerationConfigurator` 以 `AddDecorator<GoodPoweredGeneratorSpec, GoodPoweredGenerator>()` 掛元件；`GoodPoweredGenerator.UpdateState` 每個 tick 最後呼叫 `node.SetOutputMultiplier(gcb.IsConsuming ? 1f : 0f)`，並依 `Graph.RequiresPower` 自動 `PauseConsumption()/ResumeConsumption()`（IL：`_investigate\out2\power\il-goodpoweredgen.txt`、`il-powergen-configurator.txt`）。
- 因此 **24 份 SEE 藍圖不得掛 `GoodPoweredGeneratorSpec`**（否則我方 λ 被覆寫、燃料 toggle 被搶）。視覺／音效 spec（`GoodPoweredGeneratorAnimatorSpec`、`PowerGeneratorParticleControllerSpec`、`PowerGeneratorSoundsSpec`、`MechanicalNodeIlluminatorSpec`、`TemplateAttachmentsSpec`）經 IL 證實只依賴 `MechanicalNode`，可保留（待實機確認動畫／煙霧）。
- 原版「需求 0 自動停燒＋輸出 0」的好處由 SEE 控制器自行實作（§1.3）。

### 1.2 (b) 能否取得網路需求／供給／單棟負載 λ？— **已證實：可讀網路需求與供給；單棟 λ 需 P3 定義**

`Timberborn.MechanicalSystem.MechanicalGraph`（public class）成員（`mech-graph.txt`、`il-graph.txt`）：

| 成員 | 簽名 | 語意（IL 證據） |
|---|---|---|
| `PowerSupply` | `int { get; }` | Σ 各節點 `Actuals.PowerOutput`；`AddNodeToCounters` 初始化＋`OnNodePowerOutputChanged` 增量維護 |
| `PowerDemand` | `int { get; }` | Σ 各節點 `Actuals.PowerInput`；同上增量維護 |
| `PowerEfficiency` | `float { get; }` | `PowerDemand <= 0 ? (PowerSupply > 0 ? 1 : 0) : Mathf.Min(1, (PowerSupply + BatteryPower) / PowerDemand)` |
| `PowerSurplus` | `int { get; }` | `PowerSupply - PowerDemand` |
| `Powered` | `bool { get; }` | `PowerSupply > 0 \|\| BatteryCharge > 0` |
| `RequiresPower` | `bool { get; }` | `PowerDemand > 0 \|\| BatteryCharge < BatteryCapacity` |
| `Nodes`／`Generators`／`Batteries`／`NumberOfGenerators` | 集合／int | 圖內節點清單 |
| `BatteryCharge`／`BatteryCapacity`／`BatteryChargeLevel`／`BatteryPower` | 電量 | `BatteryPower` IL：`PowerSurplus < 0 ? Min(-PowerSurplus, MaxBatteryPowerThisTick) : 0` |

單棟可讀 `MechanicalNode.Actuals.PowerOutput/PowerInput`（public getter）與 `MechanicalNode.PowerEfficiency`（＝`Graph == null ? 0 : Graph.PowerEfficiency`）。

**結論與限制**：
1. 「網路需求」「網路供給」可直接讀（符合 REQ-POWER-08「讀原版圖結果、不自建網路快取」）。
2. 原版**沒有**逐棟分配需求 API：圖只提供全域加總與全域效率。
3. P0 建議（`forensics-power` §2.4 防回授公式；待 P3 定案＋實機 V-IMPL-3 驗證）：
   - `D = Graph.PowerDemand`；`ΣN_SEE = Σ 我方發電機額定（MechanicalNodeSpec.PowerOutput）`；
     `S_other = max(0, Graph.PowerSupply − Σ 我方節點.Actuals.PowerOutput)`；
   - `λ = 0` if `D <= 0`（→ `Idle`，見 §1.3）；否則 `λ = clamp01(D / max(1, S_other + ΣN_SEE))`；
   - `multiplier_i = clamp01(λ × ConditionCurve.Multiplier(condition_i))`，每 tick `node.SetOutputMultiplier(multiplier_i)`；
   - **燃料比例 fraction 取我方寫入的 `node.OutputMultiplier`**（不要用 `Actuals.PowerOutput/額定` 反推：`ceil` 會造成最大 +1 W／1.5% 偏差）；
   - `fuelPerHour = spec.FuelPerHour × fraction`、`waterPerHour = spec.WaterPerHour × fraction`；`fuelScaling="vanilla"` 時 fraction 改為「需求>0 即 1」。
   - 分母用「額定」而非即時 supply，避免「我方降輸出→supply 降→λ 升」的回授震盪；同網多棟 SEE 引擎共用同一 λ（按額定等比）。
   - 電池邊界：`D=0` 但 `BatteryCharge < BatteryCapacity` 時 `RequiresPower=true`（原版會繼續充電）；SEE 電網含電池時的 λ 策略列 P8 實機（建議先 λ=1 或依電量比例）。
4. 低需求時 `Actuals.PowerOutput` **不會**自動下降；本模組要「低需求少耗燃料」就必須靠控制器把 `OutputMultiplier` 依 condition／λ 下調（§1.1），這是輸出與燃料連動的唯一合法手段。

### 1.3 (c)＝D-4 需求 0 時節點與消耗行為；如何停機不耗燃料／不磨損

**節點行為（已證實）**：
- `MechanicalNode.ActiveAndPowered` IL＝`Active && Powered`；`Powered`＝`Graph.Powered`（`il-mechanicalbuilding.txt`）。
- 發電端 `PowerOutput > 0` ⇒ 只要它自己 Active，`Graph.PowerSupply > 0` ⇒ `Graph.Powered == true` ⇒ 這台發電端自身 `ActiveAndPowered == true`。需求為 0 時節點**不會**自動 inactive，輸出仍存在。
- `MechanicalBuilding.SetConsumptionDisabled(bool)` IL：**只在 `MechanicalNode.IsConsumer`（PowerInput > 0）時**才把 `InputMultiplier` 設 0/1；對發電端（PowerInput = 0）**完全沒有效果**。→ 不能用它停發電端。

**燃料消耗（已證實，`il-goodconsuming-body.txt`）**：
- 原版耗燃料由 `Timberborn.GoodConsumingBuildingSystem.GoodConsumingBuilding`（`TickableComponent`）負責；`Tick()` → `UpdateIsConsuming()` → `ConsumeSupplies()`。
- `UpdateIsConsuming` IL：`IsConsuming = !ConsumptionPaused && _blockableObject.IsUnblocked() && HasSupplies()`。
- `ConsumeSupplies` IL：只扣浮點 `_suppliesLeft[i] -= FixedDeltaTimeInHours × GoodPerHour`；整數貨物在 `HasSupplies` 才 `TakeConsumed`（見 §3.2）。
- **公開的暫停消耗 API（不需 Harmony）**：`GoodConsumingBuilding.GetGoodConsumingToggle()` → `GoodConsumingToggle.PauseConsumption()`／`ResumeConsumption()`／`Paused`／`StateChanged` 皆 public；`GetGoodConsumingToggle()`、`HoursUntilNoSupply()`、`IsConsuming`、`ConsumptionPaused`、`MaximumWorkingTime` 皆 public。
- `PoweredGoodConsumingBuilding.UpdateState` IL：`SetConsumptionDisabled(!CanUse)`；且 `ActiveAndPowered ? ResumeConsumption() : PauseConsumption()`。但**原版 SteamEngine 藍圖沒有 `PoweredGoodConsumingBuildingSpec`**（只有 `GoodPoweredGeneratorSpec`）。
- **原版零需求行為（已證實，`forensics-power` §4.1）**：`GoodPoweredGenerator.UpdateState` IL 等價：
  ```
  if (graph == null)                                  toggle.PauseConsumption();
  else if (!graph.RequiresPower)                      { if (!gcb.ConsumptionPaused) toggle.PauseConsumption(); }
  else                                               { if (gcb.ConsumptionPaused)  toggle.ResumeConsumption(); }
  node.SetOutputMultiplier(gcb.IsConsuming ? 1f : 0f);
  ```
  其中 `Graph.RequiresPower = PowerDemand > 0 || BatteryCharge < BatteryCapacity`。→ **保留 `GoodPoweredGeneratorSpec` 時**，需求 0（且電池已滿/無電池）會自動停燒＋輸出 0；但這是二值行為，不滿足比例耗燃料，且每 tick 覆寫我方 λ（§1.1 關鍵衝突）。
- **SEE 決策：移除 `GoodPoweredGeneratorSpec`，由控制器自己實作等價的零需求邏輯**（`Graph.PowerDemand == 0` → multiplier 0＋`PauseConsumption()`；需求恢復 → 重算 λ＋`ResumeConsumption()`）。

**P0 決策（D-4）— 已證實可行、待實機確認**：

| 需求 | 實作手段 | 證據等級 |
|---|---|---|
| 需求 0 → `Idle`（輸出 0） | 讀 `Graph.PowerDemand == 0`；`node.SetOutputMultiplier(0)` | 已證實（IL） |
| 需求 0 → 不耗燃料 | 取 `GoodConsumingBuilding.GetGoodConsumingToggle()`，呼叫 `PauseConsumption()`；需求恢復時 `ResumeConsumption()` | API 已證實；燃料真停扣**待實機**（P8） |
| 需求 0 → 不磨損 | 磨損由 Core `MaintenanceLedger` 自行累積；λ=0 不呼叫 `AccumulateWear` | 已證實（設計） |
| 不 patch 機械圖 | 全部使用上述公開 API；`SetConsumptionDisabled` 對發電端無效、不使用 | 已證實 |

- 實作注意：`PauseConsumption`/`ResumeConsumption` 只在狀態跨邊界時呼叫（快取 toggle，勿每 tick 呼叫）；訂閱 `StateChanged` 需在 `Dispose` 對稱解除。

### 1.4 已作廢的假設（P1～P9 不得重用）
- 「`SetConsumptionDisabled` 可停發電端」→ 錯（僅 consumer 生效）。
- 「需求 0 時原版機械圖會自動把發電端**節點**停掉」→ 節點本身不會 inactive；原版的停燒/輸出 0 是 `GoodPoweredGenerator` 元件每 tick 寫 multiplier+切 toggle 的結果（移除該 spec 後由我方控制器負責）。
- 「`MechanicalGraph.PowerEfficiency` 供過於求時 <1」→ 錯（demand<=0 時 supply>0 就回 1；只有供不應求 <1）。比例耗燃料不能直接用 PowerEfficiency 當 λ。
- 「保留 `GoodPoweredGeneratorSpec` 也能做 A 案」→ 錯（每 tick 被覆寫；移除該 spec 是 A 案前提）。

## 2. D-2 ＝ 固體燃料＋液體水艙雙庫存與重複扣料

> 本節由 `forensics-inventory` 取證（完整 IL 與檔案路徑見 `_investigate\findings-inventory-maintenance.md`、`_investigate\out2\inventory\`）。

### 2.1 雙庫存在同一棟建築「機制上可以並存」— **已證實機制；無原版同棟先例（待實機）**
- 兩個庫存是**兩個獨立 `Inventory` 元件**：template 對同一 decorator 型別重複 `Add` 時 `BaseInstantiator` 逐一實例化；`Inventories` 元件原生支援多庫存。
- 原版沒有同棟同時使用 `GoodConsumingBuildingSpec` 與 `StockpileSpec` 的先例（10 份 GCB 檔 ∩ 17 份 Stockpile 檔＝空集合）→ 玩家可見的 UI／清空／多系統行為列**待實機**。
- 實作限制：`GoodConsumingBuildingSpec` 只能有一個（spec 型別為 key），燃料與水**不能**都放 GCB。
- **2026-11-08 修根因（取代上句「水必須液體 StockpileSpec」）**：同棟 `Stockpile`＋GCB 的組合確實會
  雙初始化 `NoHaulingPostStatus` 並在完工時崩潰（Player.log 2026-11-07）。現行做法：燃料只留 GCB；
  水艙改由 `IDedicatedDecoratorInitializer<SEEBoilerSpec, Inventory>`（`SEEWaterInventoryInitializer`）
  在程式建立 `WaterTank` public input `Inventory`（`StorableGood.CreateGiveableAndTakeable("Water")`、
  `HasPublicInput()`、容量＝`WaterCapacityGoods`）。**不需** `StockpileSpec`、不會有第二個 status initializer；
  搬運工經 GCB 既有的 `FillInputHaulBehaviorProvider`（逐 `EnabledInventories`）補水，owner 在完工時 `Enable()`。

### 2.2 補給路徑（搬運工／輸送帶／管網）— **已證實**
| 庫存 | 補給機制 |
|---|---|
| 燃料（GCB `Inventory`） | 搬運工／工人經 `FillInputHaulBehaviorProvider`＋`FillInputWorkplaceBehavior` 逐「EnabledInventories」補料；**不需 public input**。 |
| 水（`WaterTank` public input `Inventory`，2026-11-08 起） | `InventoryInitializer.HasPublicInput()` 進 district registry；貨物 `Water` 以 `GiveableAndTakeable` 建立（輸入＋可取），搬運工經 `FillInputHaulBehaviorProvider` 補水、controller 以 `TakeExisting` 扣水。管網（`TankPipe`/`PipeTank`）只接 `Stockpile`，v1 不接管此庫存。 |
| 輸送帶（CB v2） | `BuildingBeltTarget` 以 `[AddTemplateModule2(typeof(BuildingSpec))]` **自動掛到所有建築**；`TryProvide` 需 `Inventories`；只搬非 Liquid 固體（`IsCarryable`）→ 可補燃料、不碰水。 |

### 2.3 重複扣料風險 — **已證實條件；本專案以「永久暫停原版消耗」避開**
- 原版扣料：`HasSupplies()` 內整數 `Inventory.TakeConsumed(GoodAmount(1))` ＋ `_suppliesLeft` 浮點緩衝；`UpdateIsConsuming` 只看 `ConsumptionPaused`／`IsUnblocked`／`HasSupplies`。
- **重複扣料只在「我方控制器也從同一個 GCB Inventory 扣 Log/Plank」時發生**；若控制器只呼叫 `TakeConsumed` 而原版消耗仍開著，即雙倍消耗。
- 合法停扣：`GetGoodConsumingToggle().PauseConsumption()`（public，**須快取 toggle、勿每 tick 呼叫**）。`MechanicalBuilding.SetConsumptionDisabled` 只調電力輸入倍率，**不會**停扣燃料。`PoweredGoodConsumingBuildingSpec` 才會依 `ActiveAndPowered` 自動 pause/resume，但原版 SteamEngine 沒有此 spec。
- **兩種合法實作（`forensics-power` §4.4／§5.2 提供 B 機制）**：
  1. **P0 選定：原版消耗永久暫停＋控制器手動扣料** — 取 `GetGoodConsumingToggle().PauseConsumption()` 一次；每 tick 依 `fraction=node.OutputMultiplier` 以能量結算 `Inventory.TakeConsumed`（Log=1、Plank=2；浮點餘量）。deterministic、可直接對帳；本專案 AD-3 採此。
  2. 備援（僅 P8 對帳失敗改用）：**toggle 佔空比** — 以信用累加器 `fuelCredit += fraction`；`fuelCredit>=1` 時 `ResumeConsumption()` 並扣 1，否則 `PauseConsumption()`；靠原版 `_suppliesLeft` 浮點緩衝讓時間平均＝比例。風險：控制器 tick 與 `GoodConsumingBuilding.Tick` 的先後順序（findings-power U1）。
- P0 決策（與契約 AD-3 一致）：**採 1**；`fuelScaling="vanilla"` 也由控制器 fraction=1 實現，不恢復原版線性消耗。消耗對帳 ≤±2% 列 P8 實機。

### 2.4 管網混裝（Timber Pipes）— **已證實機制；誤差在門檻內（待實機）**
- `PipeTank.get_StoredGoods` 會加總「**所有 `EnabledInventories`** 的 `TotalAmountInStock`」，且 `FluidGoodId` 取「任一庫存中 amount>0 的 good」—**沒有液體過濾**。
- 影響：同棟燃料庫存的 Log 會被算進水體積，且可能被當成 fluid id；混裝誤差 ≈ `fuelStock / waterTankGoods`。
- 12 檔中最大誤差＝Medium.T4 的 **11.76%**（≤ PLAN 12% 驗收線）；其餘皆更低。→ 8× 水艙硬門檻成立。
- 結論：**不得**在藍圖宣告 `BuildingPipeSpec`；水艙維持液體 `StockpileSpec`。實際管網行為列 P8 實機 V-01／V-02。

### 2.5 R-C（EnterableSpec）— **已證實 NRE 來源**
- `InventoryNeedBehavior.ActionPosition/Decide` 直接解參考 `_enterable`；`Stockpile`（`StockpileInventoryBehaviorInitializer`→`AddNeedBehavior`）與 `SimpleOutputInventory` 都會初始化它。
- 因此：**所有含 `StockpileSpec` 或 `SimpleOutputInventorySpec` 的建築必須帶 `EnterableSpec`＋`BuildingAccessibleSpec`**（17/17 Stockpile 檔、15/15 SimpleOutput 檔皆有；MWP 藍圖同樣有）。燃料 GCB 單獨不觸發，但水艙會。

### 2.5b 水艙必須自動允許 Water（AD-10，已證實）
- `StockpileInventoryInitializer.Initialize`：`AddAllowedGoodType(spec.WhitelistedGoodType)`＋`AddGoodDisallower(SingleGoodAllower)`；但 `Timberborn.InventorySystem.SingleGoodAllower` 的 `AllowedGood` 初始為 `null`，`AllowedAmount(good)` IL＝`AllowedGood == good && !HasOtherGoods ? int.MaxValue : 0` → **null 時所有貨物都回 0（水進不去）**。原版只由 UI（`ModifyInventoryBox`／`GoodSelectionController`）或地圖編輯器呼叫 `Allow(...)`。
- SEE 控制器必須在完成／初始化時：`var a = GetComponent<SingleGoodAllower>(); if (!a.HasAllowedGood) a.Allow("Water");`（`Allow`／`HasAllowedGood` 皆 public，IL 已證實）；不覆寫玩家已選 Water/Badwater。
- 不做＝水艙永遠收不到水（搬運工與管網都會被 disallower 擋掉）。
- `BuildingAccessible.Awake` 若 `BlockObjectSpec.Entrance.HasEntrance == false` 會 `throw InvalidOperationException` → `BlockObjectSpec` 必須有 entrance。

### 2.6 P0 決策（D-2）
- 方案 A 維持（2026-11-08 修正水側）：燃料＝`GoodConsumingBuildingSpec`（只列 Log／Plank）＋原版消耗**永久暫停**＋控制器 `TakeConsumed`；水＝程式建立的 `WaterTank` public input `Inventory`＋控制器 `TakeExisting` 扣水。不再用液體 `StockpileSpec`。
- 容量派生與雙燃料速率見 §3 與 CONTRACT_SEE §3.5.2（AD-3）。
- 備援（僅 P8 失敗）：自訂 Inventory decorator（`InventoryInitializerFactory`/`InventoryInitializer.Initialize` 皆 public，方案 C；V-04 風險）。

## 3. D-3 ＝ 容量派生取整與貨物整數化 — **已證實**

### 3.1 燃料庫存容量（原版派生）
`GoodConsumingBuildingInventoryInitializer.Initialize` IL（`il-goodconsuming-body.txt`）：

```
for each ConsumedGoodSpec g:
    cap(g) = Mathf.CeilToInt( g.GoodPerHour * spec.FullInventoryWorkHours )   // IL_003f
totalCapacity = Enumerable.Sum(cap)                                           // IL_0085
inventory = InventoryInitializerFactory.Create(inventory, totalCapacity, "GoodConsumingBuilding")
inventoryInitializer.AddAllowedGoods(各項 StorableGoodAmount(GoodId, cap))
```

結論：
- 容量派生＝**逐項無條件進位（CeilToInt）後加總**，不是 floor、不是 round。
- 單一燃料（只列 Log 或只列 Plank）時：`capacity == ceil(FuelPerHour × 25)`，與需求 §11.3 的 `fuelStock == ceil(燃料×25)` 一致。
- T3／T4 若同時列 Log＋Plank，容量是 `Σ ceil(rate×25)` 而非單一表格值 → 由 AD-3 的「容量速率公式」反推（`rate = (fuelStock−0.5)/25` 或雙燃料每項 `(fuelStock/2−0.5)/25`），保證逐檔等於表定 `fuelStock`；此 rate **只供容量派生**，實際消耗見 §3.2。
- **float32 警訊（已證實）**：原版 SteamEngine 的 `Water 0.3f × FullInventoryWorkHours 50 = 15.000001f → CeilToInt = 16`（不是 15）→ **水艙容量不得由「耗水率×工時」派生，必須藍圖直接寫死表定 `WaterCapacityGoods`**（本專案水艙走液體 `StockpileSpec.MaxCapacity`，正確）。燃料 12 檔以 float32 模擬 `ceil` 逐檔驗算 12/12 等於表定 `fuelStock`。
- `GoodConsumingBuilding.Awake` IL：`MaximumWorkingTime = FullInventoryWorkHours + 1 / Max(GoodPerHour)`（浮點，僅 UI/參考用；早前若寫成除式是誤讀）。

### 3.2 貨物整數化與餘量
`ConsumeSupplies`／`HasSupplies` IL（同檔）：
- 每 tick 對每個 good：`_suppliesLeft[i] -= FixedDeltaTimeInHours × GoodPerHour`（float 累積）。
- 當 `_suppliesLeft[i] <= 0` 且 `Inventory.UnreservedAmountInStock(GoodId) > 0`：`Inventory.TakeConsumed(new GoodAmount(GoodId, 1))`，並把 `_suppliesLeft[i] = 1`（重設為 1，不是加 1）。
- 每項貨物各自有 `_suppliesLeft`；多項消耗互不干擾。
- **水消耗（我方控制器）必須複製此模式**：浮點餘量累積、滿 1 才扣整數貨物；餘量不存檔（需求 §13.4）。

### 3.3 水艙容量
`StockpileSpec.MaxCapacity` 是**貨物數整數**、由藍圖直接指定（原版 SmallTank 用 `MaxCapacity:30, WhitelistedGoodType:"Liquid"`）；不受 `FullInventoryWorkHours` 影響。需求表定 `waterTankGoods = round(waterPerHour × 200)`，與 Stockpile 機制不衝突（`round` 由生成器算，非遊戲派生；不得由耗水率×工時派生，理由見 §3.1 float 警訊）。

## 4. D-6 ＝ 存檔形態 — **已證實：純世界節點可行（採為 P0 決策）**

### 4.1 官方世界節點機制（已證實）
- `Timberborn.WorldPersistence.ISaveableSingleton`（public interface，`[Singleton]`）：
  `void Save(ISingletonSaver singletonSaver)`。
- `ISingletonSaver.GetSingleton(SingletonKey)`、`ISingletonLoader.TryGetSingleton(...)`；`WorldSingletonLoader`；`SerializedWorldFactory.Create(IEnumerable<ISaveableSingleton>)`／`SaveSingletons(...)`（`SerializedWorldFactory` 建構子注入 `EntityRegistry` 與 `ISingletonRepository`）。
- 載入：`ILoadableSingleton.Load()`＋建構子注入 `ISingletonLoader`（遊戲 `DayNightCycle` 為實例，IL 已抓；證據 `_investigate\out2\save\`）。
- DI：mod 可新增 `[Context("Game")]` `Configurator`；`ContainerDefinition.InstallAll` 會掃 `AppDomain.CurrentDomain.GetAssemblies()` 全部型別；mod 載入早於 Game 容器建立（載入順序最終以 P8 實機確認）。

### 4.2 建築列舉與 entityId（已證實）
- `Timberborn.EntitySystem.EntityComponent.EntityId`：**`System.Guid`**（`get_EntityId` public；`set` private）。
- `EntityRegistry`（`Timberborn.EntitySystem`，`AsSingleton` 綁定）：`ReadOnlyList<EntityComponent> Entities { get; }`、`EntityComponent GetEntity(Guid)`。
- `EntityComponentRegistry`：`IEnumerable<T> GetAll<T>()`／`GetEnabled<T>()`（可列舉所有 `SEEBoilerController`）。`IEntitySaver`/`IEntityLoader`/`IObjectSaver`/`IObjectLoader` 為 per-entity 路徑（備援用）。
- **P0 決策：純世界節點 `SEE`**（需求 §14.1 原樣；不用混合）：
  - 一個 `ILoadableSingleton`＋`ISaveableSingleton` singleton（`[Context("Game")]` 綁定）：
    - `Save(ISingletonSaver)`：序列化 `schemaVersion/modVersion/checksum/buildings[]/compat`（欄位見 CONTRACT_SEE §6；`buildings[].entityId` 為 Guid 字串，D-8）。
    - `Load()`：以 `ISingletonLoader` 取 `SEE` 節點 → 驗證版本／校驗和 → 建 `entityId → 狀態` 快取。
  - 建築列舉：`EntityRegistry.Entities` 或 `EntityComponentRegistry.GetAll<SEEBoilerController>()`；`EntityId` 取 `EntityComponent.EntityId`。
  - 讀檔後**延遲一幀**套用（需求 §14.2）：singleton Load 只建 pending 狀態，controller 第一個 tick 才套用；不依賴實體建立順序。
  - 損壞隔離、checksum、遷移鏈照 CONTRACT_SEE §6.4–§6.6。
- **備援（僅 P8 實機發現 singleton 存讀不可用才啟用）**：逐實體 `IPersistentEntity`＋`ComponentKey("SEE")`（MWP 已實機驗證）+ 世界節點只存 schema/checksum/compat 的混合形態；屆時記錄偏離（PLAN §1.5 D-6 備援）。
- 已知限制：`IObjectLoader.Get` 缺 key 會丟 `ArgumentOutOfRangeException`（per-entity 備援時必須 `Has` 守衛）；`IObjectSaver` 無 dictionary setter（用平行 `ListKey<string>`）。

## 5. D-5 ＝ 維護材料扣料與道路判定（A4）— **已證實 API 可直接複用**

`forensics-inventory` 以 IL 確認下列成員存在且簽名一致（命名空間 `Timberborn.InventorySystem`，非 GameDistricts）：

| 成員 | 簽名 | 用途 |
|---|---|---|
| `DistrictInventoryRegistry.ActiveInventoriesWithStock` | `ReadOnlyHashSet<Inventory> ActiveInventoriesWithStock(string goodId)` | 跨多個庫存湊維護材料 |
| `Inventory.UnreservedAmountInStock` | `int UnreservedAmountInStock(string goodId)` | 只扣未被保留的貨物 |
| `Inventory.HasUnreservedStock` | `bool HasUnreservedStock(GoodAmount good)` / `(string goodId)` | 先驗後扣（all-or-nothing） |
| `Inventory.TakeExported` | `void TakeExported(GoodAmount good)` | 原子扣料 |
| `DistrictCenter.AccessibleIsOnDistrictRoad` | `bool AccessibleIsOnDistrictRoad(Accessible accessible)` | 道路可達判定 |
| `BuildingAccessible.Accessible` | `Accessible { get; }` | 取得可及點 |
| `DistrictBuilding.District`／`InstantDistrict` | `DistrictCenter` | 同 district 判定 |

→ MWP 的 `MaintenanceLedger`＋`IMaintenanceStore`（`HasMaterials`／`TryConsume` all-or-nothing／`IsReachable`）**原樣可複製**，只需改名；SEE 直接沿用（A 級可複用）。來源：`C:\Users\user\Downloads\Mechanical Water Pump\mod\src\MWP.Gameplay\Components\MwpPumpController.Ports.cs`＋`_investigate\findings-inventory-maintenance.md`。

## 6. A5 ＝ 載入與格式 — **已證實**

| 項目 | 結論 | 證據 |
|---|---|---|
| Manifest | `manifest.json` 7 欄位：`Name, Description, Version, Id, MinimumGameVersion, RequiredMods, OptionalMods`；`Version` 是字串（讀入後 `Version.Create`）；**沒有 `Assemblies` 欄位** | `_investigate\out2\save\dump-B-modding-localization.txt`、`il-manifestloader.txt` |
| DLL 載入 | `ModCodeStarter.LoadAssemblies`：**mod 根目錄遞迴 `*.dll` → `Assembly.Load(File.ReadAllBytes(path))`**（root 與 `Scripts/` 都會被遞迴掃到；出貨只要放根目錄） | `il-modcodestarter.txt` |
| Localizations | `Localizations/*.csv`（mod-local），格式與 fallback 細節由 `finding-persistence-loading.md` 補；P0 以 MWP/HDBF 實機成品為準（`ID,Text,Comment`、UTF-8、每行 3 欄） | `_investigate\out2\save\`、MWP 成品 |
| Icons | `Icons/*.png`＋`<name>.png.meta.json`，meta 讀 `isSprite`（可含 `width/height`）；MWP/HDBF 成品為 `{"isSprite":true}` | `il-moddingassets.txt`、已安裝成品 |
| Config | mod 自行以 `IModEnvironment.ModPath` 讀檔（MWP 實證；失敗用編譯期預設） | MWP `MwpModStarter` |

## 7. P0 決策摘要（給 P1～P9 的凍結輸入）

| 項 | 決策 | 依據 |
|---|---|---|
| D-1 輸出調變 | **方案 A（逐棟 `SetOutputMultiplier`）可行並採用**；B 案僅備援 | §1.1 IL |
| D-1 前提 | **24 份藍圖移除 `GoodPoweredGeneratorSpec`**（否則原版每 tick 覆寫）；視覺/音效 spec 可留 | §1.1、§1.4 |
| D-1 λ 定義 | 防回授：`λ = clamp01(D / max(1, S_other + ΣN_SEE))`；`D<=0` → Idle；燃料 fraction 取 `node.OutputMultiplier`；多棟同網共用 λ | §1.2、findings-power §2.4 |
| D-2 燃料／水 | 方案 A：燃料只列 Log／Plank 走 `GoodConsumingBuildingSpec`＋**原版消耗永久暫停**＋控制器手動扣料；水走液體 `StockpileSpec`＋我方控制器扣水；不重複扣料 | §2、§3 |
| D-3 容量 | 燃料容量＝原版 `Σ ceil(rate×25)`，rate 以 `(fuelStock−0.5)/25`（單）／`(fuelStock/2−0.5)/25`（雙）反推，逐檔等於表定；水艙＝藍圖寫死 `MaxCapacity`（**不可**由耗水率派生，float32 誤差已證實）；扣料浮點餘量＋滿 1 扣整數 | §3 IL |
| D-4 零需求 | `Idle`＝`Graph.PowerDemand==0`；`SetOutputMultiplier(0)`＋`GoodConsumingToggle.PauseConsumption()`；恢復時 Resume；**不用** `SetConsumptionDisabled`（對發電端無效）；原版等價邏輯見 §1.3 | §1.3 IL |
| D-5 維護 | 直接複用 MWP `MaintenanceLedger`＋`DistrictInventoryRegistry` 扣料＋`BuildingAccessible`/`DistrictCenter` 道路判定 | §5 |
| D-6 存檔 | **純世界節點 `SEE`**（`ILoadableSingleton`＋`ISaveableSingleton`＋`EntityRegistry.Entities` 列舉）；備援 per-entity＋`ComponentKey("SEE")` 僅 P8 失敗才啟用；entityId 用 Guid 字串 | §4 |

## 8. 待實機清單（AI 不得宣告通過）

| # | 待驗項目 | 對應 REQ／V | 驗證方式 |
|---|---|---|---|
| 1 | `PauseConsumption()` 後燃料是否真的停止扣減、恢復後是否續扣；`TakeConsumed` 原子性 | REQ-CORE-08／POWER-09、V-IMPL-4 | P8：零需求 24 遊戲小時，燃料/水/condition 不變；消耗對帳 ±2% |
| 2 | `SetOutputMultiplier` 寫入後電網 UI 與原版效率分配是否同步、有無抖動 | V-IMPL-2／V-IMPL-3 | P8：1 遊戲日輸出取樣＋30 日平均 |
| 3 | 同網多發電機時 λ 的實際分攤 | V-IMPL-3 | P8：2 台引擎＋不同負載 |
| 4 | 同棟「GCB 燃料庫存＋液體 Stockpile 水艙」並存的 UI／清空／多系統行為；PipeTank 混裝 11.76% | REQ-FUEL-10/11、V-01／V-02、COMPAT-04/05 | P8 + TP 實機（V-01～V-04 為發佈門檻） |
| 5 | 搬運工／輸送帶對 GCB 燃料庫存與 Stockpile 水艙的實際補給 | REQ-FUEL-10/16、REQ-BLD-07/08 | P8：各補一次至滿 |
| 6 | 世界節點存檔在真實存檔事件中的可用性、load 順序、1,000 棟體積 | SAVE 全組、AC-12/13 | P8：存讀往返＋遷移＋損壞隔離 |
| 7 | `GoodConsumingBuildingSpec` 與控制器消耗對帳（無重複扣料） | REQ-IMPL-04、D-2 | P8：連續 1 遊戲日 ±2% |

## 9. P5 相容層取證（2026-11-06，外部 Workshop DLL + 遊戲 BlueprintSystem IL）

> 本節是 SEE-P5 的離線取證，全部來自**實際安裝的 DLL metadata/IL**，不是文件推測。
> 證據檔位於 `_investigate\p5\`（`dump-*.txt`／`il-*.txt`）；重現工具與 §0.2 相同，`managedDir` 分別指向
> `C:\Program Files (x86)\Steam\steamapps\workshop\content\1062090\<id>\version-1.1`（外部模組）
> 或遊戲 `Timberborn_Data\Managed`（遊戲端）。

### 9.1 三個外部模組實測（路徑、版本、錨點）

| 模組 | DLL（實測 1.1 版資料夾） | manifest `Version` | `AssemblyInformationalVersion` | 組件簡名 | 已證實錨點（`ApiIndex dump`） |
|---|---|---|---|---|---|
| Timber Pipes | `...\3801726769\version-1.1\TimberPipes.dll`（225,792 B） | `11.4.0` | `1.0.0+<hash>` | `TimberPipes` | `TimberPipes.Specs.BuildingPipeSpec`、`TimberPipes.Specs.PipePortState`、`TimberPipes.Models.PipeFluids` |
| Power Lines | `...\3766893705\version-1.1\PowerLines.dll`（122,880 B） | `11.1.1` | `1.0.0+<hash>` | `PowerLines` | `PowerLines.Specs.PowerLineSpec`、`PowerLines.Components.PowerLineComponent` |
| Conveyor Belts v2 | `...\3815376360\version-1.1\ConveyorBelt.dll`（173,056 B） | `11.4.0` | `1.0.0+<hash>` | `ConveyorBelt`（另有 `ConveyorBelt.Algorithm`） | `ConveyorBelt.Specs.BeltSystemSpec`、`ConveyorBelt.Components.BuildingBeltTarget`（另 `Services.BeltRegistry`、`Components.BeltInventory` 存在） |
| 舊版 Power Lines（不支援） | 實測工作坊另有 `Opolame.PowerLines` 命名變體 | — | — | `Opolame.PowerLines` | 只做程式集名偵測；不整合、不 patch（REQ-COMPAT-16） |

**版本閘門結論（重要）**：三個 DLL 的 `AssemblyVersion` 都是 `1.0.0.0`、informational version 都是
`1.0.0+<hash>`，與 manifest 的 `11.x` 屬於不同版本方案，**不可直接比較**。因此 P5 的版本階段設計為：
- 同 major band（例如 `11.3.0` vs 最低 `11.4.0`）→ `Degraded`（視同未安裝，記 `version-below-minimum`）；
- 不同方案（`1.0.0+hash`）→ `Available`，只記 `assembly-metadata-version` 提示；
- 解析失敗 → `NotComparable`（不阻擋）。
此與需求 §10.6「最低門檻僅作提示不阻擋」一致；若未來外部模組改回 manifest 版本號，閘門會自動生效。

### 9.2 PowerLineSpec 的實際形狀（注入片段的欄位來源）

`PowerLines.Specs.PowerLineSpec : ComponentSpec`（public，無 `[SpecAlias]` → 藍圖 key 為 `PowerLineSpec`）：

| 成員 | 型別 |
|---|---|
| `ConnectionLocations` | `System.Collections.Immutable.ImmutableArray<UnityEngine.Vector3>` |
| `MaxConnections` | `System.Nullable<System.Int32>` |
| `MaxConnectionLength` | `System.Nullable<System.Single>` |

- `Vector3` 在藍圖 JSON 的既有形狀（原版 `SteamEngine.IronTeeth.blueprint.json` 的 `TransputProviderSpec` 實證）
  是 `{"X":…,"Y":…,"Z":…}`；P5 產生的片段照此序列化（`_investigate\p5\dump-powerlines.txt`）。
- 座標採需求 §10.4：`(0,S/2,0.5)`／`(S,S/2,0.5)`／`(S/2,S,0.5)`，S＝2／3／4。

### 9.3 PL 注入方式：**已證實可用非 Harmony 路徑（方法②）**

完整 IL 鏈（遊戲 `Timberborn.BlueprintSystem`，證據 `il-specservice.txt`、`il-blueprintbundle.txt`、
`il-blueprintdeserializer.txt`、`il-readjsons.txt`、`il-jsonmerger.txt`、`il-blueprint.txt`、`il-factioncfg.txt`）：

1. `SpecService.Load` 為每個 blueprint bundle 建 `Lazy<Blueprint>` 並放進 `_cachedBlueprintsByPath`；
   真正 deserialize 發生在第一次 `GetBlueprint(path)`（lazy）。
2. `SpecService.Deserialize(BlueprintFileBundle)` 逐一列舉 `ImmutableArray<IBlueprintModifierProvider> _blueprintModifierProviders`，
   對每個 provider 呼叫 `GetModifiers(bundle.Path)`；每個回傳字串以 `bundle.AddJson(json, provider.ModifierName)` 併入 bundle。
3. `BlueprintFileBundle.AddJson(string json, string source)` 把**字串**加入 `Jsons`（不是檔案路徑）。
4. `BlueprintDeserializer.FileBundleToSerializedObject` → `SerializedObjectReaderWriter.ReadJsons(Jsons)` →
   `JsonMerger.Merge`（Newtonsoft `JObject.Merge` + `ReplaceSettings`）→ 合併後的物件再 deserialize 成 `Blueprint`。
5. 原版**已經有**可用的 multi-binding：`GameFactionSystemConfigurator.Configure` 內
   `MultiBind<IBlueprintModifierProvider>().ToExisting<FactionBlueprintModifierProvider>()`；
   `IBlueprintModifierProvider` 是 public 介面（`{ string ModifierName; IEnumerable<string> GetModifiers(string blueprintPath); }`），
   `FactionBlueprintModifierProvider` 的 `GetModifiers` 就是回傳 modifier JSON 字串。
6. `Blueprint` 的 `Specs` 是 `ImmutableArray<ComponentSpec>`（`get_Specs` 只讀，`GetSpec<T>` 走 `isinst T`），
   **沒有**執行期 append/mutate 公開 API → 需求 §10.4 方法①「對 spec 實例追加欄位」在此版本不可行；
   方法②（provider 回傳 JSON 片段）是唯一非 Harmony 的已證實路徑。
7. `PowerLines.Components.PowerLineComponent.Awake/InitializeEntity` 以
   `BaseComponent.GetComponent<PowerLineSpec>()` 取得 spec（`il-powerlinecomponent.txt`）；`ConnectionLocations`
   為空時不會有可拉線的連接點。因此 spec 必須在 **blueprint deserialize 時**存在 → 方法②正確。
8. PL 自己的 Harmony patch 只覆蓋 `MechanicalGraphManagerPatches`／`MechanicalGraphReorganizerPatches`
   （`dump-powerlines.txt`）；SEE **不新增任何 patch**，也不 patch 機械圖（REQ-COMPAT-12）。

**結論**：P5 以 `PowerLineInjectionProvider : IBlueprintModifierProvider` 實作執行期注入，
只對 24 個 `Buildings/Power/SteamBoiler/SteamBoiler.<Size>.<Tier>.<Faction>` 路徑回傳
`{"PowerLineSpec":{...}}`，前提是 PL `Available` 且 `compat.enablePowerLineInjection=true`；
主藍圖檔案不含 `PowerLineSpec`。**未證實/待實機**：實際可拉線與供電（V-PL1／V-03）、
`GetModifiers` 在 mod 提供的 provider 上是否與原版 faction provider 同樣被容器收集（離線無法跑 DI）；
若實機失敗，降級為原版節點（MUST），不影響存檔。

### 9.4 三段式偵測與 12 情境的離線驗證

- 偵測實作：`AppDomainCompatProbe`（組件簡名→型別錨點→版本）＋`CompatDetectionSession`（單次快取、2 秒預算、30 秒節流日誌）。
- 12 情境決策表與結果見 `docs/COMPAT_MATRIX.md`；自動化測試 `CompatScenarioTests`（假探針，不載入真 DLL）。
- 離線已證：12 情境的 `detected/enabled/failed`、供水模式、PL 注入開關、legacy PL 只提示、degraded 視同未安裝、
  probe 例外/預算耗盡不崩潰、版本閘門邊界、12 類存檔損壞恢復、unknown 欄位保留、1,000 棟 <400 KB。
- **待實機（P8）**：V-01/V-TP1（液體 Stockpile 自動補水）、V-TP2（PipeTank 混裝體積）、V-PL1/V-03（拉線供電）、
  CB 帶端餵料與斷電（C5/C6、V-04）、舊存檔移除模組後載入。

---
*本檔由 SEE-P0 產出（Lead 彙整：`forensics-power` / `forensics-inventory` / `forensics-persistence` 取證）；§9 由 SEE-P5 追加（外部 DLL + BlueprintSystem IL 取證）。若程式碼與本檔衝突，以 IL 重新取證為準並回報 Lead。*

