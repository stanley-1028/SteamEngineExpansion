# SEE.UI — 鍋爐建築面板與狀態徽章（P4＋P4B）

本程式集負責玩家選取蒸汽引擎時看到的實體面板：狀態列、六個資訊區（動力／燃料／水／維護／相容／操作）、
狀態徽章與四個操作。實作只依賴 `SEE.Core` 的唯讀契約（`IBoilerQuery`／`IBoilerStatusView`／`IBoilerLogic`），
不引用 Gameplay／Integration 的任何型別（CONTRACT_SEE §2.1、§1.3）。

P4B 依 Lead 裁決擴充唯讀契約並關閉 G1／G3／G4／G5：合約新增 `WaterBeaverEquivalent`、`FuelGoods`、
`MaintenanceMaterials`（含 have/need）、`NextMaintenanceDay`、`MissingMaterialCount`、
`WaterSupplyAvailability`、`PipeSupplyActive`、`BeltSupplyActive`、`BeltHasPower`；loc 由 54 鍵增加到 63 鍵
（AC-16 只要求 ≥54）。

## 分層與檔案

| 檔案 | 性質 | 職責 |
|---|---|---|
| `Status/StatusDescriptors.cs` | 純資料 | 狀態→loc key／顏色帶／優先序、維護子狀態、供水模式、顯示名 |
| `Status/BoilerBadgePolicy.cs` | 純資料 | 徽章可見性（預設不顯示 Idle）、優先序、`badgeRefreshHz`／`maxBadgesVisible` 夾限 |
| `Panels/BoilerPanelOptions.cs` | 純資料 | `SeeConfig` → 面板選項（4 Hz、低庫存門檻、維護開關、T1–T4 維護間隔） |
| `Panels/PanelNumbers.cs` | 純資料 | NaN/Infinity 安全格式化、m³、海狸飲水換算、剩餘時數 |
| `Panels/BoilerPanelModel.cs`／`BoilerPanelModelBuilder.cs` | 純資料 | `IBoilerStatusView` → 全部列的 loc key＋已格式化參數（可離線測試） |
| `Panels/BoilerPanelFingerprint.cs` | 純資料 | 髒標記指紋（依顯示精度取整） |
| `Panels/BoilerPanelRefreshPolicy.cs` | 純資料 | ≤ `perf.uiRefreshHz` 節流、面板關閉零查詢 |
| `Panels/BoilerPanelData.cs`／`BoilerPanelDataFactory.cs` | 需 `ILoc` | 只在髒標記成立時把 key 在地化 |
| `Panels/BoilerPanelStyles.cs`／`BoilerPanelView.cs` | UIElements | 元素樹只建一次；Render 只改 text／顏色／一條進度條／兩個控制值 |
| `Panels/BoilerPanelFragment.cs` | UIElements | `IEntityPanelFragment` 生命週期、4 Hz 節流、髒標記、命令轉發 |
| `Panels/BoilerPanelModuleProvider.cs`／`SeeUiConfigurator.cs` | DI | 把面板掛進原版 `EntityPanelModule` |
| `Panels/SeeUiConfigSource.cs` | 啟動期 | 以 Core codec 讀 `SEE_Config.json`，失敗回 `SeeConfig.Default` |

## 面板列與 loc key（CONTRACT_SEE §5.2）

| 區塊 | 列 | key |
|---|---|---|
| 狀態列 | 可折疊區塊標題（顯示名）／tooltip | `Building.SEE.SteamBoiler.<Size>.<Tier>.DisplayName`／`Building.SEE.SteamBoiler.Description` |
| 狀態列 | 主狀態徽章（顏色方塊＋文字） | `Status.SEE.*`（10 態；優先序 `Paused > Maintaining > NoFuel > NoWater > NeedsMaintenance > Critical > Worn > MaintenanceDue > Idle > Running`） |
| 狀態列 | 維護原因（缺料／無路） | `Status.SEE.MissingMaintenanceMaterials`／`Status.SEE.NoAccessForMaintenance` |
| 狀態列 | 次要提示（≤2） | `Status.SEE.LowFuel`／`Status.SEE.LowWater`（`{0}` 小時，門檻 `ui.lowSupplyHours`） |
| 狀態列 | 修復指引（一行，P4B） | `UI.SEE.Hint.FixNoFuel`／`FixNoWater`／`FixNeedsMaintenance` |
| 動力 | 輸出、電網 | `UI.SEE.Panel.Output` `{0}/{1}`、`UI.SEE.Panel.Network` `{0}/{1}` |
| 動力 | 電網效率 %（P4B，G3） | `UI.SEE.Panel.Efficiency`（`{0}` 為 0–100） |
| 燃料 | 庫存／上限、`/h`、剩餘 h | `UI.SEE.Panel.Fuel` 標頭＋數值列 |
| 水 | 庫存／上限、m³、`/h`、剩餘 h | `UI.SEE.Panel.Water` 標頭＋數值列 |
| 水 | ≈N 隻海狸飲水（P4B，G1） | `UI.SEE.Panel.WaterBeaverEquivalent`（`{0}` 為 N） |
| 維護 | 機況＋進度條、下次維護日、材料 | `UI.SEE.Panel.Condition`、`UI.SEE.Panel.NextMaintenanceDay`、`UI.SEE.Panel.MaterialsDetail`＋`UI.SEE.Panel.MaterialEntry` |
| 相容 | 供水模式 | `UI.SEE.Panel.WaterSupplyMode`＋`Status.SEE.WaterMode.{Auto,Tank,Valve,Hauler}` |
| 相容 | 供水模式不可用原因（P4B 契約欄位） | `UI.SEE.WaterModeUnavailable`（`{0}` 為模式名） |
| 操作 | 暫停／繼續、立即維護、自動維護、供水模式下拉 | `UI.SEE.Action.{Pause,Resume,MaintainNow,AutoMaintenance}`、四個 `Status.SEE.WaterMode.*` |

- 面板不自創 key；未凍結的字串一律不顯示（見下方缺口）。
- 材質名稱沿用原版 `Good.<id>.DisplayName`（原木／木板），缺翻譯才回退 good id，**不得**顯示 raw loc key。
- 維護系統 `maintenance.enabled=false` 時整個維護區塊隱藏（REQ-MAINT-17），修復指引也不顯示。

## 效能（REQ-OPS-01～04）

- `UpdateFragment` 每幀被呼叫，但面板關閉（`ClearFragment`）時在任何查詢前返回 → 關閉後零更新。
- 開啟時 `BoilerPanelRefreshPolicy` 把讀取壓到 `perf.uiRefreshHz`（預設 4 Hz＝250 ms）。
- 每次讀取先算 `BoilerPanelFingerprint`；沒變就跳過，**不產生任何字串**（髒標記）。
- 元素樹只在 `InitializeFragment` 建一次；Render 不做字串拼接以外的工作。
- 徽章政策把 `perf.badgeRefreshHz` 夾在 0.5–2 Hz、`perf.maxBadgesVisible` 夾在 16–256；`ui.showIdleBadge` 預設 `false`。

## 與契約的已知缺口（P4 發現；P4B 收口四項）

P4 當時凍結的 `IBoilerStatusView`／54 鍵集合不足以顯示需求 §15.2 的全部欄位。P4B 依 Lead 裁決
「只加唯讀 snapshot 欄位、不改玩法」擴充契約與 loc（63 鍵），關閉 G1／G3／G4／G5；其餘缺口保留為
已記錄限制，等對應卡片（P5 相容接線、P9 美術）處理：

| # | 缺口 | 現況（P4B 後） |
|---|---|---|
| P4-G1 | 需求 §15.2/§7.3「≈N 隻海狸飲水」 | **已關閉**：`UI.SEE.Panel.WaterBeaverEquivalent`；契約新增 `WaterBeaverEquivalent`（Gameplay 以 `SEEConst.BeaverWaterPerDay` 計算，UI 亦保留 `PanelNumbers.BeaverEquivalent` 後備） |
| P4-G2 | 燃料／水列的在地化格式（庫存、`/h`、`h`）無 key | **保留**：仍以數字＋單位符號組合（L2 完整化需再增 key，非本卡範圍） |
| P4-G3 | 電網效率（REQ-UI-02）無 key | **已關閉**：`UI.SEE.Panel.Efficiency`；契約既有 `NetworkEfficiency` 直接顯示為 % |
| P4-G4 | 缺燃料／缺水／需維護的修復指引（REQ-UI-04）無 key | **已關閉**：`UI.SEE.Hint.FixNoFuel`／`FixNoWater`／`FixNeedsMaintenance`，依最高優先狀態顯示一行 |
| P4-G5 | 維護材料清單、下次維護日、缺料件數不在 view | **已關閉**：契約新增 `MaintenanceMaterials`（id＋required＋inStock）、`NextMaintenanceDay`、`MissingMaterialCount`；顯示 `UI.SEE.Panel.MaterialsDetail`／`MaterialEntry`／`NextMaintenanceDay`；無資料顯示 `—` |
| P4-G6 | `tank` 不可用原因（REQ-UI-07）需相容偵測結果 | **保留**：下拉四模式皆可選；偵測接線屬 P5（G7 未關閉） |
| P4-G7 | 供水模式可用性旗標（TP 未安裝）不在 view | **保留（契約已就位）**：`WaterSupplyAvailability`／`PipeSupplyActive`／`BeltSupplyActive`／`BeltHasPower` 已加入唯讀契約，UI 也能顯示 `UI.SEE.WaterModeUnavailable`；但 Gameplay 目前一律回 `Available`＋未接線，等 P5 把偵測結果寫入這四個 slot |
| P4-G8 | `PipeSupply`／`BeltSupply`／`BeltNoPower`／`PipeContaminated`／`UI.SEE.Error.*` 無 view 欄位 | **保留（契約已就位）**：view 欄位已加（見 G7），但值未接線；6 個 key 仍列為 reserved |
| P4-G9 | 世界空間徽章需 Gameplay 把實體／`StatusSubject` 交給 UI（或藍圖加 spec） | **保留**：面板徽章與 `BoilerBadgePolicy` 已完成；世界徽章待 P3／P1 配合 |
| P4-G10 | `SeeConfig` 未在 DI 註冊，UI 以 Core codec 重讀同一 `SEE_Config.json` | **保留**：單次延遲載入、失敗回預設 |
| P4-G11 | 燃料種類（Log／Plank）不在 view | **保留（契約已就位）**：契約新增 `FuelGoods`，但面板仍只顯示數量、不顯示種類；顯示與燃料種類提示另卡處理 |
| P4-G12 | `UI.SEE.Error.MaterialsMissing` 需要缺料件數 | **保留**：缺料件數已進 view／材料列，但該錯誤 key 仍是 reserved（目前以狀態列原因表示） |

## 驗證

```powershell
dotnet build mod/src/SEE.UI/SEE.UI.csproj -c Release -m:1
dotnet run --project mod/tests/SEE.Tests -c Release -m:1
```

`SEE.Tests/UiContractTests.cs` 離線覆蓋：63 鍵（AC-16 要求 ≥54）齊全／兩語一致／placeholder 一致／zhTW 無簡體、
狀態→key／顏色／優先序、每個面板列（含 P4B 的 G1/G3/G4/G5 四列與指紋）、徽章政策、4 Hz 節流、面板關閉零查詢、
`SEE.UI` 原始碼不出現 Gameplay／Integration 字樣，以及封裝 key 集合與（UI＋藍圖標籤＋保留）集合的差異為空。
`dotnet run --project mod/tests/SEE.Tests -c Release -- --evidence` 會直接印出 G1／G3／G4／G5 四列顯示字串。
