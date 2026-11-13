# SEE.UI — 鍋爐建築面板與狀態徽章（P4＋P4B）

本程式集負責玩家選取蒸汽引擎時看到的實體面板：狀態列、六個資訊區（動力／燃料／水／維護／相容／操作）、
狀態徽章與四個操作。實作只依賴 `SEE.Core` 的唯讀契約（`IBoilerQuery`／`IBoilerStatusView`／`IBoilerLogic`），
不引用 Gameplay／Integration 的任何型別（CONTRACT_SEE §2.1、§1.3）。

P4B 依 Lead 裁決擴充唯讀契約並關閉 G1／G3／G4／G5：合約新增 `WaterBeaverEquivalent`、`FuelGoods`、
`MaintenanceMaterials`（含 have/need）、`NextMaintenanceDay`、`MissingMaterialCount`、
`WaterSupplyAvailability`、`PipeSupplyActive`、`BeltSupplyActive`、`BeltHasPower`；loc 由 54 鍵增加到 63 鍵
（AC-16 只要求 ≥54）。2026-11-14 AD-23 再新增底部工具組標籤 `UI.SEE.ToolGroup.SteamEngines` → 共 **64 鍵**。

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
| `Panels/BoilerPanelNativeStyles.cs`／`BoilerPanelView.cs` | UIElements | 只用原版 USS 類別的原生樣式 helper、元素樹只建一次；Render 只改 text／類別／顯示／一條進度條／控制值 |
| `Panels/PanelCollapseState.cs` | 純邏輯 | SEE 區塊收合狀態（預設展開、跨 Render 保留、不寫存檔） |
| `Panels/BoilerPanelFragment.cs` | UIElements | `IEntityPanelFragment` 生命週期、4 Hz 節流、髒標記、命令轉發、注入原版 UI 服務 |
| `Panels/BoilerPanelModuleProvider.cs`／`SeeUiConfigurator.cs` | DI | 把面板掛進原版 `EntityPanelModule` |
| `Panels/SeeUiConfigSource.cs` | 啟動期 | 以 Core codec 讀 `SEE_Config.json`，失敗回 `SeeConfig.Default` |

## 原版（vanilla）外觀與收合區塊（REQ-UI-01／05／12）

原則：組件不帶 UXML／USS，面板元素一律使用遊戲原版 entity-panel fragment 的同一批元件與 USS 類別。
遊戲把 CoreStyle／CommonStyle／EntityPanelCommonStyle／EntityPanelGameStyle／GameStyle 掛在 entity panel
階層上，所以這些類別的外觀與原版建築面板一致。**不設 inline 顏色、字級、寬高**，唯一例外是 A1 狀態色塊。

### 區塊與類別

- 卡片：`NineSliceVisualElement` + `entity-sub-panel` + `bg-sub-box--green`（九宮格底圖）。共四張：狀態、
  動力＋燃料＋水、維護、操作。
- 文字：`entity-panel__text`（13 px、可換行）、`entity-panel__sub-text`（12 px 提示）、
  `entity-panel__heading-text`（14 px 區塊標頭）、`text--bold`；狀態文字用 `game-text-normal` 作底色、
  嚴重度用 `game-text--red`／`text--yellow`（刻意不加 `entity-panel__text`，否則 EntityPanelCommonStyle
  的顏色會蓋掉嚴重度色類）。
- 列：`entity-panel__row`；按鈕列 `entity-panel__button-wrapper`。
- 進度條：`Timberborn.CoreUI.ProgressBar` + `progress-bar`／`progress-bar--green`，機況標籤放在 bar 內
  （原版 DeteriorableFragment 慣例），以 `SetProgress(0..1)` 更新；機況 < 50% 改掛 `progress-bar--red`。
- Toggle：`Toggle` + `game-toggle entity-panel__text entity-panel__toggle`。
- 按鈕：原生九宮格按鈕工廠 —— 以 `VisualElementLoader.LoadVisualElement("Game/EntityPanel/DemolishableFragment")`
  取 `entity-fragment__button` 的九宮格按鈕（原版 hover／press skin 與點擊音效）；取不到就退回
  `new Button()` 並掛同一組類別。暫停＝`--red`、繼續＝`--green`；立即維護隨 `CanMaintain` 切換
  `--green`／`--red` 並 `SetEnabled`（全部用 class membership，不用 inline 背景色）。
- A1 形狀提示：狀態列前的色塊 swatch 保留。它是**唯一**允許 inline `backgroundColor` 的地方
  （`BoilerPanelNativeStyles.CreateBadgeSwatch`／`SetBadge`）：需求要求在不看顏色的灰階截圖也能分辨狀態，
  原版 USS 沒有對應的七段類別；文字仍走原版色類（`game-text--red`／`text--yellow`／`game-text-normal`）。

### 供水模式下拉：原生 `Timberborn.DropdownSystem.Dropdown`

- `SEE.UI.csproj` 新增 `Timberborn.DropdownSystem` 參考（`Private=false`）。
- `BoilerPanelFragment` 由 Bindito 注入 `VisualElementLoader`、`DropdownListDrawer`、`DropdownItemsSetter`
  （都是原版 Configurator 綁定的 singleton，IL 取證：`_investigate/ui2/il-configurators.txt`），再傳給 View。
- 建構：`new Dropdown()` → `Initialize(dropdownListDrawer)` → 掛 `game-dropdown` → `Localize(loc)`
  （樣板內建 header label 隱藏；面板另用原版 `entity-panel__text` 標頭顯示 `data.WaterModeHeader`，
  語言切換由 Render 更新）→ `dropdownItemsSetter.SetItems(dropdown, provider)`。
- Provider 實作原版 `IDropdownProvider`（`Items`／`GetValue`／`SetValue`，四個已在地化標籤，索引 0..3）。
- 使用者選取：`dropdown.ValueChanged` 只在值真的改變時觸發；View 讀 provider 索引，有變才呼叫
  `waterModeChanged(index)`。程式刷新走 `provider.SetIndex` + `dropdown.UpdateSelectedValue()`
  （IL 證實不會觸發 `ValueChanged`），所以 Render 永遠不會重送命令。標籤因語系變動而更新時重呼
  `SetItems`（`Dropdown` 會 cache items）。
- Fallback：整個 native 建構包在 try/catch；任何資產／API 差異都退回 `DropdownField`（掛
  `entity-panel__text`），面板不會因此整個壞掉。

### 可收合區塊（REQ-UI-12）

- 不再使用 Unity 預設 `Foldout`；改成原版可點擊名稱列：`Button` + `entity-panel__name-wrapper`，內含
  chevron 標籤（`▾`／`▸`，`entity-panel__text`）與標題（`entity-panel__name entity-panel__heading-text
  text--bold`），hover 時標題變黃（原版 entity name 行為）。
- 點 header 切換 body 顯示；**預設展開**（曾因預設收合讓玩家以為面板不見），狀態存在
  `PanelCollapseState`（View 實例欄位），跨 Render 與面板開關保留，不寫存檔；`Render` 不碰收合狀態。

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
- 元素樹只在 `InitializeFragment` 建一次；Render 只改文字、class membership、顯示、一條進度值與兩個控制值，
  不新增每幀工作、不重建元素。
- 程式刷新不會觸發命令：auto-maintenance 用 `SetValueWithoutNotify`；供水模式下拉用 provider +
  `UpdateSelectedValue()`，只有玩家在原生下拉清單點選才會呼叫命令。
- 徽章政策把 `perf.badgeRefreshHz` 夾在 0.5–2 Hz、`perf.maxBadgesVisible` 夾在 16–256；`ui.showIdleBadge` 預設 `false`。

## 與契約的已知缺口（P4 發現；P4B 收口四項）

P4 當時凍結的 `IBoilerStatusView`／54 鍵集合不足以顯示需求 §15.2 的全部欄位。P4B 依 Lead 裁決
「只加唯讀 snapshot 欄位、不改玩法」擴充契約與 loc（63 鍵；AD-23 後 64 鍵），關閉 G1／G3／G4／G5；其餘缺口保留為
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

`SEE.Tests/UiContractTests.cs` 離線覆蓋：64 鍵（AC-16 要求 ≥54）齊全／三語一致／placeholder 一致／zhTW 無簡體、
狀態→key／顏色／優先序、每個面板列（含 P4B 的 G1/G3/G4/G5 四列與指紋）、徽章政策、4 Hz 節流、面板關閉零查詢、
`SEE.UI` 原始碼不出現 Gameplay／Integration 字樣，以及封裝 key 集合與（UI＋藍圖標籤＋保留）集合的差異為空。

`SEE.Tests/NativePanelContractTests.cs`（原版化契約，純 source-scan ＋ `PanelCollapseState` 純邏輯）鎖住：
沒有 `Foldout`、原版 USS 類別字串存在且被使用（`entity-sub-panel`／`bg-sub-box--green`／`entity-panel__text`／
`entity-fragment__button`／`progress-bar`／`game-toggle`／`game-dropdown` 等）、native 按鈕／ProgressBar／Toggle／
Dropdown 路徑存在且 Dropdown fallback 只在 try/catch 內、除 A1 swatch 外沒有任何 inline 顏色／字級、
收合預設展開且 `Render` 不碰收合狀態。

## 待人工實機驗證（離線開發環境無法證明）

本卡只能在離線環境以編譯、source-scan 與 IL/metadata 取證，**不得宣稱實機通過**。進遊戲後需人工確認：

1. 面板外觀：四張 `bg-sub-box--green` 九宮格卡片、文字 13/12/14 px 與原版建築面板一致，無 Unity 預設皮膚殘留。
2. 原版九宮格按鈕（立即維護／暫停／繼續）的材質、hover／pressed 變化與點擊音效；`DemolishableFragment`
   樣板取用失敗時 fallback 是否仍可點。
3. native Toggle（自動維護）外觀與勾選行為。
4. native Dropdown：點擊展開清單、四模式可選、選中後命令生效、清單樣式與原版一致、Esc／點擊外部關閉。
5. 收合 header：點擊可展開／收合、chevron 方向正確、hover 變黃；預設為展開。
6. 收合狀態在面板刷新／關閉再開後保留，且不寫入存檔。
7. 150%／200% UI 縮放與 zhTW 長字串：文字換行不裁切、325 px 面板寬不被撐開。
8. 面板不得拋例外：切換不同建築、藍圖模式、拆除建築、語言切換（zhTW↔enUS）後仍正常。
9. 程式刷新不會重送命令：面板停留時供水模式／自動維護不被自動改動。

`dotnet run --project mod/tests/SEE.Tests -c Release -- --evidence` 會直接印出 G1／G3／G4／G5 四列顯示字串。
