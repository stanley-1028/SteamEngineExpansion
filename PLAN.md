# Steam Engine Expansion（SEE）— 實作任務規劃書

> 角色：任務規劃師（Lead）｜日期：2026-11-06｜狀態：**考察完成，待辦任務已建立，可開始執行**
> 需求來源：`C:\Users\user\Documents\deepseek-harness\default-workspace\steam-engine\蒸汽引擎模組需求文檔.md`（21 章／274 REQ，v1.0-draft）
> 權威數值：`...\steam-engine\brief\SHARED_BRIEF.md`（命名契約、12 檔數值、公式、維護規則、存檔 schema）
> 考察證據：`_investigate/doc-extract.md`、`_investigate/template-reuse.md`、`_investigate/feasibility.md`（本目錄）
> 本規劃書只談「怎麼做、誰做、何時算完成」；玩法細節一律以需求文檔與簡報為準。

---

## 0. 一句話結論

需求文檔已把**規格、公式、驗收**寫到可直接施工的程度；本機有**同作者姊妹作《機械水泵》的完整可建置原始碼**（維護系統、存檔、UI、Timber Pipes 適配器都已驗證過）與**已發布的 HDBF 驗證管線**，因此最短路徑不是從零寫，而是：**以 MWP 為骨架分叉 → 資料驅動產生 24 份藍圖 → 重寫「發電＋雙庫存＋比例耗燃料」領域層 → 沿用 MWP 的維護／存檔／UI／驗證管線 → 先重用原版模型出可玩版，再補自製美術**。

預估（AI 工作階段實際耗時，非人日）：**P0 取證＋骨架 1 輪、資料層 1 輪、Core 1 輪、Gameplay 1–2 輪、UI／相容各 1 輪、驗證與平衡 1–2 輪、打包 1 輪＋人類實機 1 輪、美術 2–3 輪**。可平行處見 §5。

---

## 1. 專案現況考察

### 1.1 需求側（已完成，不需重做）

| 項目 | 狀態 | 位置 |
|---|---|---|
| 需求文檔 21 章／274 REQ，含 12 檔權威規格、平衡四判準、維護系統、三模組相容、存檔 schema、UI／loc、測試矩陣 AC-01～AC-21、里程碑 M0–M5、可調參數、V-01～V-10 待證實清單 | ✅ 已完成 | `steam-engine\蒸汽引擎模組需求文檔.md` |
| 唯一事實源（命名、數值、公式、維護、相容、存檔、裁決 C-1～C-9） | ✅ 已完成 | `steam-engine\brief\SHARED_BRIEF.md` |
| 原版動力／蒸汽機取證、三外部模組接口取證 | ✅ 已完成 | `steam-engine\research\01-vanilla-power.md`、`02-mod-integration.md` |
| 需求提取（章節→實作產出、維護／存檔／UI／測試／參數／V 清單、REQ 對照、v1 切割建議） | ✅ 已完成 | `_investigate/doc-extract.md` |

> 注意：`steam-engine\` 是**唯讀事實源**，本次施工不修改它；本專案所有產物寫在 `C:\Users\user\Downloads\timber steam engine\`。

### 1.2 環境側（已確認）

| 事實 | 值 | 影響 |
|---|---|---|
| 遊戲 | Timberborn `1.1.2.8-ffc2de0-sw`，`C:\Program Files (x86)\Steam\steamapps\common\Timberborn` | 藍圖／API 一律以此版本為準 |
| .NET SDK | 9.0.305（另有 runtime 8/9/10） | 工具／測試用 `net9.0`；遊戲端程式集用 `netstandard2.1` |
| 模組載入 | `Documents\Timberborn\Mods\<ModId>\`（manifest.json＋Blueprints＋Scripts＋Localizations＋Icons＋AssetBundles） | 成品直接裝這裡即可被遊戲載入 |
| 已安裝參考成品 | `Mods\MechanicalWaterPump`（v0.2.0，四程式集＋8 藍圖＋16 timbermesh）、`Mods\HighDensityBoxFarm`、`Mods\TreePlanterTray` | 目錄結構、manifest、loc、icon meta 的活樣本 |
| 外部模組 | Steam 工作坊：Timber Pipes `3801726769`、Power Lines `3766893705`、Conveyor Belts v2 `3815376360` | 相容層的實際反射錨點來源 |
| 原版藍圖參考 | `high-density-box-farm\mod\.ref\Blueprints\`（1,067 份原版藍圖解壓副本） | 藍圖 schema 的 ground truth |

> **可行性考察修正（`_investigate/feasibility.md`）**：1.1.2.8 **沒有** `Timberborn.Core.dll`／`Timberborn.Gameplay.dll`（660 個 DLL 中不存在）；`IModStarter` 在 `Timberborn.ModManagerScene`、`Configurator`／`IProvider<T>` 在 `Bindito.Core`、藍圖 spec 系統在 `Timberborn.BlueprintSystem`，其餘系統各自獨立 DLL。無 XML 文件、無官方 API 清單；可用 `_investigate/probe/GameApiScan`（Mono.Cecil）重跑型別掃描。`manifest.json` 只有 7 欄（**無 `Assemblies`**）；loc CSV 必須 3 欄、含逗號要引號。
>
> **最省時的第一個可玩版**：原版蒸汽機是**純資料驅動**（無 `SteamEngineSpec` 型別），複製藍圖→改 TemplateName／模型／數值→append TemplateCollection 即可（無 C#）；C# 只在需要「比例耗燃料／維護／自訂 UI」時才加。因此施工順序可先出 **JSON-only 可玩切片**（SEE-P1B）給人類早驗，再接程式層。

### 1.3 可複用資產（決定「最短工期」的關鍵）

| 資產 | 內容 | 對 SEE 的價值 |
|---|---|---|
| **MWP 原始碼** `C:\Users\user\Downloads\Mechanical Water Pump\mod` | 4 程式集（Core／Gameplay／Integration／UI）＋tests＋tools/validate＋ApiIndex＋build.ps1；**維護系統（condition／磨損／2h／材料／缺料／無路）、存檔編解碼＋遷移、SE風面板、Timber Pipes 偵測、1 秒 tick、日誌節流、config clamp** 全部已實作且建置綠燈 | **直接分叉成 SEE 骨架**；要改的是「泵→鍋爐」的領域語意，不是管線 |
| MWP 文件 `mod/docs/` | `CONTRACT.md`（凍結介面／藍圖契約／存檔 key／loc 契約）、`API_FACTS_MWP.md`（已用 IL 證實的遊戲 API 事實）、`VERIFICATION.md`、`REQ_TRACEABILITY.md` | 契約與 API 事實可直接沿用／改名；省掉一輪 API 考古 |
| **HDBF 原始碼** `high-density-box-farm\mod` | tools/validate（藍圖／loc／config／assembly／source facts 驗證）、release gates、Blender→.timbermesh 管線（官方插件 v1.2.0）、原版模型重用範例 | 驗證門禁與後期美術管線直接抄 |
| **TPT 專案**（已完成卡流程） | 需求→卡片（goalRun、max 推理）→逐卡執行→人類實機 gate 的節奏 | 本規劃的卡片切法沿用同一節奏 |

> 結論：**不需要從零建置環境**；P0 的第一步就是把 MWP 專案複製進來並改名（見任務卡 SEE-P0）。
>
> 補充（模板重用考察 `_investigate/template-reuse.md`）：`Documents\Timberborn\Mods\HighDensityBoxFarm` 現在是 **v2.5.6 開發樹**（含 Indoor／600+ 測試／`MaintenanceDefaults`／**實機驗證過的 `CompatAnchorTable`**／bug 報告），比 workspace 的 HDBF v0.1.0 更適合當相容層與品質門檻參考（報告中標為 `HDBF2`）。注意：開發樹不可整個放進 `Mods/`（同 Id 會啟動崩潰），SEE 只放 `build/` staged 輸出。

### 1.6 已知實作陷阱（模板重用考察，施工前必看；P0 寫入 CONTRACT_SEE）

| # | 陷阱 | 後果 | 對策 |
|---|---|---|---|
| R-A | **材質陣營綁定**：原版蒸汽機只有 IronTeeth 版；把 IT 模型抄進 Folktails 藍圖 | 建造選單建預覽時 `ArgumentException: Material … not found`，**整個建造選單載入失敗** | 一檔一陣營；FT 藍圖**只能**引用 Folktails 模型（如 WaterWheel／LargeWaterWheel），IT 用 SteamEngine.IronTeeth；P9 自製中立材質後才可統一模組 |
| R-B | **`BlockObjectSpec.Size` 軸序 = X 寬、Y 深、Z 高**（不是文件慣例的 X×Y 平面＋Z 高） | 佔地／高度寫反（HDBF v1 曾把 3×2×1 寫成 `{3,1,2}`） | 2×2×3 → `{2,2,3}`；3×3×3 → `{3,3,3}`；4×4×4 → `{4,4,4}`；Blocks 長度 = X*Y*Z，row-major |
| R-C | **`SimpleOutputInventory` 必須配 `EnterableSpec`**（含庫存建築） | 比佛喝水需求取不到 Enterable → **NRE、整個模擬中止**（MWP 實機 BUG-01） | 所有含庫存的 SEE 藍圖一律帶 `EnterableSpec`（原版 SteamEngine／SmallTank 皆有） |
| R-D | **維護模型選錯會踩「載入即逾期」bug**（dueDay 模型、缺欄位設成載入當日） | 首日即逾期、產量乘數被砍半（HDBF bug-06） | SEE 採 **MWP 的 condition 模型**（本規劃 §1.4/§6 已定），不用 dueDay |
| R-E | validator 的 assemblies 口徑 bug（把資料夾名／測試 DLL 當出貨組件） | 門禁假綠或假紅 | P6 用白名單（Core／Gameplay／Integration／UI），缺件 FAIL、`SEE.Tests` 排除 |
| R-F | 開發樹直接放 `Mods/` | 遊戲遞迴載入所有 DLL／藍圖，同 Id 與工作坊衝突可能啟動崩潰 | 只放 `build/SteamEngineExpansion` staged 輸出到 `Mods/SteamEngineExpansion` |
| R-G | `.deps.json` 出貨／`.png.meta.json` 內容錯 | 載入器行為異常或圖示不顯示 | 照 HDBF／MWP：不附 `.deps.json`；meta 只需 `{"isSprite":true}`（以實機成品為準） |
| R-H | 第三方程式集偵測錨點用「想像的型別名」 | 偵測 0 命中、相容層白做 | P5 以實際 workshop DLL 的 IL dump 為準；參考 HDBF2 已實機驗證的 `CompatAnchorTable` 與 MWP `TimberPipesProbe` |
| R-I | 燃料方案 A 的 `GoodConsumingBuildingSpec` 若同時列 Water，會與水艙 Stockpile 衝突 | 混裝體積／重複扣料 | 燃料 spec **只列 Log／Plank**；水一律走液體 `StockpileSpec`＋我方控制器；P3 做消耗對帳 |
| R-J | **第一個可玩版其實可以零 C#**（原版蒸汽機是純資料驅動） | 若一路等程式層，會延後第一次人類驗證 | 先出 **SEE-P1B JSON-only 切片**（剝掉自訂 spec、保留原版 spec 行為）給人類做 5 項檢查（建造／供電／燃料消耗／存讀檔／拆除），藍圖問題在寫 C# 前先暴露 |
| R-K | **本工作階段無法自動進遊戲**（Steam 未執行、sandbox low integrity；batchmode 只到掃描 Mods 就退出） | 若卡片嘗試自動實機驗證會白耗時間 | 離線門檻（build／tests／validator／balance）全自動；實機一律走 P8「人類桌面 gate」＋`Invoke-LoadSmoke.ps1`（需正常桌面與已登入 Steam） |
| R-L | 型別名以「想像的 API」寫入會編譯失敗或偵測 0 命中 | 卡住整個程式層 | 一律以 `_investigate/feasibility.md` §6 與實際 IL dump 為準；`InventorySpec`／`IPanelModuleProvider`／`BlueprintModifier` 三個名字**都不存在**（見 §1.4 第 10 條） |

### 1.4 已確認的關鍵技術事實（施工時直接引用）

1. **原版蒸汽機藍圖**（`SteamEngine.IronTeeth`，3×3×3、400 hp、研究 400）已含：`GoodConsumingBuildingSpec { FullInventoryWorkHours: 50, ConsumedGoods: [Log 0.1/h, Water 0.3/h] }`、`MechanicalNodeSpec { PowerOutput: 400, PowerInput: 0, IsShaft: false }`、`TransputProviderSpec`（上／左／右／底 2 點，`IgnoreRotation: true`）、`GoodPoweredGeneratorSpec`、`MechanicalBuildingSpec`、`PowerGeneratorParticleControllerSpec`、`TemplateAttachmentsSpec`。→ 24 份藍圖以此為模板，**不可覆寫**它，只用不同 TemplateName／路徑／loc key。
2. **液體庫存**：原版 `SmallTank` 用 `StockpileSpec { MaxCapacity: 30, WhitelistedGoodType: "Liquid" }`。→ 水艙照此宣告（REQ-FUEL-11），**不得**加 `BuildingPipeSpec`。
3. **燃料／水雙軌方案（需求 §13.4 已拍板，方案 A）**：燃料＝`GoodConsumingBuildingSpec`（`ConsumedGoods` 只列 Log／Plank、`FullInventoryWorkHours = 25`）走原版搬運／輸送帶；水＝我方 `SEEBoilerSpec`＋液體 `StockpileSpec`（容量 200 工時）並由我方控制器扣水。→ 施工時必須做「消耗對帳」（連續 1 遊戲日，實際扣除量 vs 表定 ≤ ±2%，REQ-IMPL-04）；若原版 spec 與我方同時扣燃料，必須停用其一（P0 決策）。
4. **機械動力 API**：`MechanicalBuilding.Efficiency` = `MechanicalNode.PowerEfficiency` = `Graph.PowerEfficiency`（0..1）；`MechanicalNodeSpec` 序列化欄位只有 `PowerOutput:int`／`PowerInput:int`／`IsShaft:bool`；**`MechanicalNode.SetOutputMultiplier` 存在**（輸出調變 A 案可行）、`MechanicalBuilding.SetConsumptionDisabled` 存在。→ 發電端「實際負載 λ」與「輸出調變」仍需 P0 取證（§1.5 D-1、V-IMPL-2），但 A 案（寫倍率）已有 API 依據。
5. **存檔 API**：實體 id 是 `System.Guid`（`EntityComponent.EntityId`），**不是 int**；`IPersistentEntity`＋`ComponentKey("SEE")`；`IObjectLoader.Get` 缺欄位會丟 `ArgumentOutOfRangeException`（必須先 `Has`）；無 Int64 primitive（用字串）；DI 型別只能有一個建構子。→ 需求 §14.1 的 `entityId: long` 視為筆誤，實作用 Guid 字串（見 §4.5 裁決）。
6. **建置環境硬規則**（沿用 MWP README）：每次 `dotnet build` 必加 `-m:1`；每個 `.csproj` 最多一個 `ProjectReference`（Core ← Gameplay／Integration／UI）；不得連網（只用本機 NuGet 快取）；`TreatWarningsAsErrors=true`（0 warning）；遊戲端 `netstandard2.1` 且遊戲 DLL `Private=false`；Core 不得引用遊戲組件。
7. **兩陣營 vs 模型**：借用原版模型時必須**一藍圖一陣營**（材質是 faction-scoped），因此 12 檔 × 2 陣營 = 24 份藍圖是硬需求，不是可選項。
8. **測試現實**：遊戲內行為（建造、供電、管網補水、實機 FPS）**無法在本機自動化**；離線可自動化的是 T0 單元、T1 藍圖／loc／config 驗證、編譯門禁。因此流程必須有「人類實機 gate 卡」（見 SEE-P8）。
9. **五程式集架構（需求 §11.1）**：`SEE.Core`（spec／公式／常數／契約，≤1,200 行）→ `SEE.Gameplay`（控制器／Tick，≤2,500）→ `SEE.Integration`（偵測／適配器／可選注入／存檔序列化，≤2,000）＋ `SEE.UI`（只讀 `IBoilerStatusView`，≤1,200）＋ `SEE.Tests`（可分層掃描）。依賴掃描由 `SEE.Tests` 以 `Assembly.GetReferencedAssemblies()` 強制。MWP 的程式集切法近似，可直接沿用骨架。
10. **實際型別名（以 1.1.2.8 IL 為準，不要再猜）**：`InventorySpec` **不存在**→用 `SimpleOutputInventorySpec`；`IPanelModuleProvider` **不存在**→`IProvider<EntityPanelModule>`＋`EntityPanelModule.Builder`（`AddTopFragment`／`AddMiddleFragment`／`Build`）；`BlueprintModifier` **不存在**→`IBlueprintModifierProvider`＋`BlueprintModifierSpec`；**原版無 Maintenance 型別**（只有 `DeteriorableSpec` 老化）→維護系統必須自製（照 MWP condition 模型）；`AddDecorator<TSubject,TDecorator>()` 的 TDecorator 必須是 `BaseComponent` 且只有一個建構子；netstandard2.1 用 `record` 的 `init` 需自備 `IsExternalInit`。

### 1.5 技術取證項（P0 先釘死，否則後面會返工）

| # | 未知 | 為何關鍵 | 取證方法 | 失敗備援 |
|---|---|---|---|---|
| D-1 | 發電端如何取得「網路實際需求／負載 λ」以實現**比例耗燃料**，以及能否**逐棟調變輸出**（V-IMPL-2／V-IMPL-3） | 需求 CORE-07、BAL-11、§8.4 與 §13.6 的核心機制；輸出調變 A 案（寫節點）不可行就要改占空比 B 案 | **`MechanicalNode.SetOutputMultiplier` 已確認存在**（feasibility §6，A 案可行）；P0 續查 λ 可得性：ApiIndex／IL dump `MechanicalGraph`、`MechanicalNode`、`PowerEfficiency`／`PowerDemand`／`PowerSupply`；做 1 棟原型在遊戲內讀值（P0 取證腳本） | λ 以「圖效率／節點倍率」近似（供不應求才降載）並由 Lead 裁定文檔偏離（不得讓低負載超耗）；輸出調變仍用 A 案 |
| D-2 | 自訂建築「固體燃料庫存＋液體水艙」是否都能被搬運工／輸送帶／管網補給；`GoodConsumingBuildingSpec` 是否會與我方扣料**重複扣燃料** | REQ-BLD-08／FUEL-10／11、V-01／V-02；混裝會破壞 PipeTank 體積；重複扣料直接壞平衡 | IL 查 `StockpileSpec`／`InventorySpec`／hauling 綁定與 `GoodConsumingBuilding` 的 tick；做 1 棟原型（離線證據＋P8 實機） | 水艙維持液體 Stockpile；燃料若不接受搬運則改自訂庫存（方案 C）；重複扣料則把 spec 速率設 0、全由控制器扣（記錄偏離） |
| D-3 | 水消耗的**貨物整數化**與餘量處理；`GoodConsumingBuildingSpec` 的容量派生取整（0.11×25=2.75 → floor／ceil／round） | REQ-FUEL-03／08 要求燃料庫存 = ceil(燃料×25)、水艙 = round(水耗×200) 零誤差 | IL dump 原版 `GoodConsumingBuilding` 容量初始化；以 12 檔逐檔驗算 | 容量不由原版派生，改由藍圖／spec 直接寫死表定值（`fuelStock`／`waterTankGoods` 已是權威值） |
| D-4 | 電網需求 0 時發電端行為；如何在不 patch 的情況下「停機不耗燃料／不磨損」 | REQ-CORE-08、POWER 待機；原版發電端沒有關閉 API 保證 | IL 查 `MechanicalNode` 的 active／consumption-disable；`MechanicalBuilding.SetConsumptionDisabled` 對發電端的語意 | 控制器自行判定 λ=0 時不扣料；節點輸出仍存在（無需求不影響電網） |
| D-5 | 維護系統在「發電建築」上的路徑：材料從 district 庫存扣除、無路判定 | MAINT 全組；MWP 已做過，可直接照抄 | 直接複用 MWP `MaintenanceLedger`＋Gameplay 扣料程式；以 `DistrictInventoryRegistry` 取證 | 材料檢查照做、道路判定以 `BuildingAccessible` 存在與否通過（記錄待驗） |
| D-6 | 存檔形態：需求 §14.1 的世界節點 `SEE.buildings[]`（含 checksum） vs MWP 已驗證的**逐實體** `IPersistentEntity` key | SAVE 全組、AC-12／13；選錯會返工 | P0 用 MWP 經驗＋IL 驗證世界節點能否在存檔事件取得全部建築清單 | 混合：世界節點只存 schema／modVersion／checksum／compat，逐棟狀態用 per-entity 元件（記錄偏離並在 CONTRACT_SEE 凍結）。**2026-11-06 第二輪裁決**：P0 取證後採**混合形態**（世界節點 `SEE` 只存 schema／version／checksum／`buildings[]` 四索引欄位／compat；逐棟可變狀態存 per-entity 元件，AD-11），已由 Lead 有條件追認，見 `mod/docs/LEAD_RULINGS_ADDENDUM.md` |

> 以上 5 項全部在 **SEE-P0** 卡內處理並寫入 `mod/docs/API_FACTS_SEE.md`＋`mod/docs/CONTRACT_SEE.md` 的決策記錄；未釘死前不進 P3。

---

## 2. 製作策略（為什麼這樣切）

1. **Fork-and-Adapt，不從零**：`mod/` = MWP 專案複製 → 全量改名（`MWP`→`SEE`、`MechanicalWaterPump`→`SteamEngineExpansion`、命名空間、存檔 key、loc 前綴、config 檔名、manifest Id）→ 刪除泵業務 → 保留被驗證過的管線（build／validate／tests／save／UI／maintenance）。這一步是機械性的，風險低、省下最多時間。
2. **資料驅動**：12 檔權威值只存在 `data/engines.json`；24 份藍圖、陣營乘數、庫存容量、維護材料、loc、TemplateCollection 全部由 `tools/generate-blueprints` 產生。**禁止手抄**（REQ-BLD-02、REQ-ARCH 的風險緩解）。
3. **先重用原版模型**：v1 可玩版直接用原版蒸汽機／風車／水車等同尺寸模型（`TimbermeshSpec.Model` 指向原版路徑）＋自製圖示，把「玩法正確」與「美術」解耦；自製 `.timbermesh`（12×2）與動畫列為 P9，不阻塞可玩與驗收。
4. **驗證分層**：離線可自動化的一律進 `build.ps1`（0 warning、T0 測試、T1 驗證、禁止項靜態檢查）；遊戲內行為集中到一張「人類實機 gate」卡，AI 只準備腳本與彙整結果，**不得宣告實機通過**（沿用 TPT/HDBF 的紀律）。
5. **平衡先算後測**：30 日平均輸出、四條判準用離線計算器先跑（P7），實機量測放 P8；任何數值調整只動 `engines.json` 並重跑生成器＋判準。
6. **不碰紅線**：不覆寫原版 SteamEngine、不新增物品／流體／陣營、不硬依賴外部模組、不每幀掃圖、不 Harmony patch 機械圖（需求紅線；違反即退回）。

---

## 3. 工作分解（WBS）

> 卡片＝自動化待辦任務（已建立在任務板，見 §5）。每卡有：寫入範圍（互斥）、前置、交付、驗收、需求覆蓋。
> 卡片由獨立 session 執行（goalRun），Lead 只做裁決、整合、門禁與最終回報。

| ID | 任務 | 角色 | 寫入範圍（僅此） | 前置 | 主要交付 | 驗收門檻 | 需求覆蓋 |
|---|---|---|---|---|---|---|---|
| **SEE-P0** | 技術取證＋骨架分叉＋契約凍結 | Lead／架構 | `mod/**`（骨架）、`mod/docs/API_FACTS_SEE.md`、`mod/docs/CONTRACT_SEE.md`、`build.ps1`、`Directory.Build.*`、5 個 csproj（含 `SEE.Tests`）／sln | 無 | MWP→SEE 改名骨架（5 程式集分層依 §11.1）；6 項取證（D-1～D-6）與決策記錄；契約凍結（spec 欄位、藍圖 schema、存檔 key、loc key）；`REQ_TRACEABILITY.md` 初版 | `pwsh -File mod/build.ps1 -Clean` 0 warning；測試 runner PASS；validate PASS；D-1～D-6 有結論或明確備援；分層掃描（`Assembly.GetReferencedAssemblies`）PASS | ARCH／NFR／IMPL 前置 |
| **SEE-P1** | 資料層：engines.json＋生成器＋24 藍圖＋loc／icon／manifest | 資料 | `mod/data/**`、`mod/tools/generate-blueprints/**`（新增）、`data/Localizations`、`data/Icons`、`TemplateCollections` | P0 | `data/engines.json`（12 筆唯一數值源）、生成器（7 步＋5 道閘門＋`--check`）、24 份藍圖（`Blueprints/Buildings/Power/SteamBoiler/`）、3 語 loc（54 鍵）、icon、manifest、`SEE_Config.json`、`engines.derived.json`、黃金雜湊 | 生成器重跑零 diff；5 道閘門全過（水艙/燃料≥8×、混裝誤差≤12%、效率判準、容量逐格相等、無 `BuildingPipeSpec`／`PowerLineSpec`）；validator 逐格比對 §4.1 零誤差；`ToolOrder` 110–142 且排在原版蒸汽機之後 | CORE-04/05、BLD 全、TECH、FUEL-01/03/09、UI-08 |
| **SEE-P2** | Core：spec／公式／常數／契約＋L1 測試（無遊戲相依） | Core | `mod/src/SEE.Core/**`、`mod/tests/SEE.Tests/**`（Core 部分） | P0 | `SEEBoilerSpec`／`SEEMaintenanceSpec`、`BoilerFormulas`／`ConditionCurve`、`SEEConst`／`SEEVersion`、`IBoilerStatusView`／`IBoilerLogic`、TierCatalog（12×2）、BoilerStateMachine（9 主狀態＋`Maintaining` 覆蓋）、MaintenanceLedger、config clamp | 測試全綠且覆蓋 CORE-06/07/08、BAL-01～12、MAINT 全、SAVE v1 編解碼、config clamp；**Core 行 ≥80%／分支 ≥70%**（REQ-NFR-07；T0 全域 ≥70% 另列，兩者分欄）；測試案例可追溯到 REQ | CORE、BAL、MAINT、SAVE、TECH、FUEL |
| **SEE-P3** | Gameplay：控制器＋燃料／水雙軌＋動力輸出＋維護＋存檔橋 | Gameplay | `mod/src/SEE.Gameplay/**` | P2、P1 | `SEEBoilerController`／`SEEMaintenanceController`／`BoilerTickSystem`（1 秒、O(1)、最外層 try/catch）；燃料＝`GoodConsumingBuildingSpec`（Log／Plank，庫存走原版搬運）；水＝液體 `StockpileSpec`＋我方控制器扣水（浮點餘量、整數扣貨）；`MechanicalNodeSpec.PowerOutput`；比例消耗（依 P0 決策）；磨損；自動維護（扣 district 材料）；暫停；`IPersistentEntity` 存檔（key `SEE`） | 編譯 0 warning；**消耗對帳 ≤ ±2%**（燃料與水各 1 遊戲日）；contract 測試（spec 綁定、庫存鍵、存檔鍵）PASS；輸出調變 A／B 案已由 P0 決策並實作；無 Harmony、無外部 DLL 引用 | CORE-01/02/03/09/10/12、FUEL、POWER、MAINT、SAVE、IMPL |
| **SEE-P4** | UI：面板＋狀態徽章＋loc 契約 | UI | `mod/src/SEE.UI/**` | P3（`IBoilerStatusView` 契約） | `BoilerPanelFragment` 六資訊區（狀態列／輸出與電網／燃料／水／機況與維護／供水模式）＋操作區；`StatusDescriptors`；54 鍵 loc 對映；≤4 Hz 髒標記 | 編譯；UI 契約測試；loc 鍵完整（54）、無 raw key、zhTW 無簡體；面板關閉零成本；REQ-UI-01～12 | UI、CORE-11、MAINT、COMPAT |
| **SEE-P5** | 相容層：三段式偵測＋3 適配器＋供水模式＋存檔序列化 | Integration | `mod/src/SEE.Integration/**`、`docs/COMPAT_MATRIX.md` | P3 | `CompatDetector`（組件簡名＋型別錨點、2 秒預算、單次快取、30 秒節流）、`TimberPipesAdapter`／`PowerLinesAdapter`／`ConveyorBeltAdapter`、`OptionalPatchHost`（僅 PL 注入）、`SaveSerializer`／`SaveMigrator`／`SaveChecksum`；`waterSupplyMode auto/tank/valve/hauler`；12 相容情境 | 12 情境決策表測試（模擬探針）；無外部 DLL 編譯期引用；注入失敗回退且只記一次 `SEE/Compat`；存檔 round-trip／損壞隔離／`schemaVersion 99` 唯讀降級測試 PASS | COMPAT 全、FUEL-11～16、POWER-08、SAVE |
| **SEE-P6** | 驗證器／CI 門禁／build 管線最終化 | QA／工具 | `mod/tools/validate/**`、`mod/build.ps1`、`tools/release/**` | P1、P3、P4、P5 | SEE 版 validator（藍圖值／loc／config／assembly 白名單／禁止項／分層掃描／golden 雜湊）、build.ps1 全流程、release-gates、負向測試 | 正反測試皆通過；`-Clean` 全綠；4 個出貨程式集（Core／Gameplay／Integration／UI）＋`SEE.Tests` 分層；故意破壞藍圖／loc／分層必須 FAIL | TEST、OPS、SAFE、NFR |
| **SEE-P7** | 平衡與效能（離線）：30 日模擬、四判準、預算 | 平衡／效能 | `mod/tools/balance-sim/**`（新增）、`docs/BALANCE_REPORT.md`、`docs/PERF_BUDGET.md` | P2 | 12 檔 30 日平均輸出表（§5.5 公式）、A/B 對照表、四條判準 PASS/FAIL、1,000 棟狀態／存檔體積估算、tick 微基準 | 12 檔全 PASS；大型 Mk.IV 餘裕 +2.9% 記錄；估算方法與假設寫明；不得把實機壓測標成已通過 | BAL、TEST-14/15、OPS |
| **SEE-P8** | 打包＋安裝＋人類實機驗收 gate | Lead＋人類 | `Documents\Timberborn\Mods\SteamEngineExpansion\**`（需人工授權）、`docs/TEST_PROTOCOL.md`、`docs/VERIFICATION.md`、`README.md`、`CHANGELOG.md` | P6、P7 | 可安裝包（`build/SteamEngineExpansion`）、17.5 手動腳本 12 步、實機驗收紀錄、工作坊文案 | 人類簽核表（AI 不宣告通過）；AC-01～AC-21 逐條有證據或待驗標記；三模組缺席／齊備皆可玩 | AC-01～AC-21、M5 |
| **SEE-P9** | 美術（可延後）：Body＋TierKit＋FactionKit（39 mesh）＋LOD＋圖示＋動畫 | 美術 | `mod/art/**`、`mod/data/Buildings/Power/SteamBoiler/*.timbermesh`、`data/Icons/**` | P8（可與 P8 並行） | Blender 4.2.1＋官方 Timbermesh 插件（HDBF 已驗證）產出模型＋LOD1/2＋圖示；spec 綁定動畫（C-9） | 面數預算（LOD0 3.5k／6k／9k）、LOD 比例、材質只用原版 MaterialCollection；6 檔抽樣截圖 | ART 全、C-9 |

### 3.1 卡片依賴與平行波次

```
Wave 0：SEE-P0（取證＋骨架＋契約）              ← 必須先完成
Wave 1：SEE-P1（資料） ∥ SEE-P2（Core）        ← 檔案互斥，可平行
Wave 1.5：SEE-P1B（JSON-only 可玩包＋人類 5 項檢查）← P1 完成即可，最早的實機回饋點
Wave 2：SEE-P3（Gameplay）→ SEE-P4（UI）∥ SEE-P5（相容）
Wave 3：SEE-P6（驗證門禁）∥ SEE-P7（平衡離線）
Wave 4：SEE-P8（打包＋人類實機 gate）→ SEE-P9（美術，可並行）
```

> 平行規則：同一波次只允許**寫入範圍互斥**的卡片同時跑；任何跨卡契約變更由 Lead 更新 `CONTRACT_SEE.md` 後才放行。

---

## 4. 里程碑（對映需求文檔 §19）

| 里程碑 | 定義（可判定） | 本規劃對映 | 備註 |
|---|---|---|---|
| M0 考察 | 研究／簡報／12 檔數值／待證實清單完成 | ✅ 已存在（需求文檔） | 本次只做「實作取證」P0，不重寫 M0 |
| M1 技術原型 | 1 棟中型 T2 可建、可發電、比例耗燃料、最小面板 | P0＋P2＋P3 的最小垂直切片 | P0 的 D-1～D-5 是 M1 的真門檻 |
| M2 可玩 | 24 藍圖、維護、存檔、loc、完整面板 | P1＋P3＋P4＋P6 | AC-01/02/06/07/08/12/16 |
| M3 相容 | 三模組 8 組合、PL 注入、V-TP2 閘門 | P5＋P8 實機 | AC-09/10/11/19 |
| M4 平衡與效能 | 30 日模擬、1,000 棟、參數鎖定 | P7（離線）＋P8（實機 Profiler） | AC-14/15/17/18 |
| M5 發佈 | 美術終稿、工作坊頁、AC 全綠 | P8＋P9 | AC-01～AC-21；人類簽核 |

> 工期換算：需求文檔以「單人全職人日」估 70 人日；本專案以 AI 卡片並行壓縮到約 **5–8 小時實際執行**（不含人類實機與美術外部依賴）。這不是把人日打折，而是把可複用資產與平行化算進去。

### 4.5 文件內部歧異的裁決（Lead 已拍板，施工照此；詳見 `_investigate/doc-extract.md` §0.3）

| # | 歧異 | 裁決 |
|---|---|---|
| D-1 | 標頭 v1.0-draft vs §21.6 修訂到 v1.2、REQ 數 274 | 以**正文＋§21.2 的 274 條**為權威；版本標籤不影響實作 |
| D-2 | loc key 結構（§15.5 共用描述＋T1–T4 Flavor vs §13.2 逐檔 key） | **採 §15.5 的 54 鍵**（A 12／B 5／C 22／D 15）；§13.2 藍圖示例視為示意，不列入驗收 |
| D-3 | 輸出順序（額定×機況×陣營→進位 vs `PowerOutput` 已含陣營） | **採 §13.5 實作口徑**：藍圖 `PowerOutput` 已含陣營係數；控制器只乘機況乘數，**不得二次乘陣營** |
| D-4 | 30 Hz 更新件數 ≤13 vs ≤12 | 依 §16.9.1 表格為 **≤13**（美術卡用；不影響 v1 可玩） |
| D-5 | 測試組合「8 組」vs 相容矩陣「12 情境」 | **12 相容情境＝必測集合；8 組合＝DI 維度集合**，兩者並存（P5／P8 都列） |
| D-6 | `fuelStock == ceil(燃料×25)` 對 FT 衍生值可能差 1 | 閘門只比對**基礎值**（表定 `fuelStock`），不比對陣營衍生值；`engines.derived.json` 標記 `factionFuelCollapsed` |
| D-7 | `maintenanceRemainingHours` 範圍 `(0,2]` vs `[0,2]` | 採**閉區間 `[0,2]`**；`Maintaining` 時 `>0`，其餘為 0 |
| D-8（本規劃新增） | 需求 §14.1 `entityId: long` | 依 IL 事實（`EntityComponent.EntityId` 是 `Guid`、無 Int64 primitive）改為 **Guid 字串**；存檔形態預設照 §14.1 世界節點，P0 驗證失敗則改 per-entity 元件（D-6 備援） |
| D-9（本規劃新增） | 需求 §11.2 藍圖路徑 `Blueprints/Buildings/...` vs 簡報 §2 `Buildings/Power/...` | 模組包內路徑採 **`Buildings/Power/SteamBoiler/SteamBoiler.<Size>.<Tier>.<Faction>.blueprint.json`**；原始生成物放 `mod/data/Buildings/...`，`build.ps1` 複製到成品根 |
| D-10（本規劃新增） | 燃料消耗機制（`GoodConsumingBuildingSpec` 與自訂控制器可能同時扣料） | 採**方案 A**（燃料交原版 spec、水自管）；P0／P3 必須做「消耗對帳 ≤ ±2%」；若重複扣料，把 spec 速率歸零、全由控制器扣，並在 `CONTRACT_SEE.md` 記錄偏離 |
| D-11（本規劃新增） | 需求 §11.1 說 `SEE.Core` 含 `SEEBoilerSpec`，但 spec 需繼承遊戲 `ComponentSpec`；若 Core 引用遊戲 DLL，`SEE.Tests`（net9 離線）就無法乾淨引用 Core | 採 **MWP 已驗證的分層**：`SEE.Core` 保持**零遊戲相依**（公式／常數／列舉／契約）；兩個 `ComponentSpec` 放 `SEE.Gameplay`。理由是可離線測試與建置穩定；分層方向（UI 只讀 Core、Gameplay 依賴 Core）不變。此偏離寫入 `CONTRACT_SEE.md`，並在 `REQ_TRACEABILITY.md` 標註 |

> 以上裁決寫入 `mod/docs/CONTRACT_SEE.md`；卡片若發現裁決與 IL 事實衝突，**先回報 Lead**，不得自行改契約。

---

## 5. 自動化待辦任務（任務板）

已建立的卡片（`task_board`，狀態 `todo`，`goalRun=true`，依賴順序見 §3.1；卡片提示已含需求路徑、寫入範圍、驗收指令與禁止事項）：

| 卡片 | 標題 | 推理檔 | 依賴 |
|---|---|---|---|
| SEE-P0 | 取證＋MWP→SEE 骨架＋契約凍結 | max | — |
| SEE-P1 | engines.json＋生成器＋24 藍圖＋loc／icon／manifest | max | P0 |
| SEE-P1B | JSON-only 先行可玩包＋人類 5 項檢查清單 | high | P1 |
| SEE-P2 | SEE.Core 純邏輯＋L1 測試 | max | P0 |
| SEE-P3 | SEE.Gameplay 控制器＋雙庫存＋動力／維護／存檔 | max | P2、P1 |
| SEE-P4 | SEE.UI 面板＋徽章 | high | P3 |
| SEE-P5 | 相容層 TP／PL／CB＋供水模式 | high | P3 |
| SEE-P6 | tools/validate＋build.ps1＋發布門禁 | high | P1、P3、P4、P5 |
| SEE-P7 | 30 日平衡模擬＋效能預算（離線） | max | P2 |
| SEE-P8 | 打包／安裝／人類實機驗收 gate | high | P6、P7 |
| SEE-P9 | 美術 12×2 timbermesh＋LOD＋動畫（可延後） | high | P8（可並行） |

> 每卡完成時必交付「真實輸出」證據（`PASS n/n`、`0 warning`、`VALIDATION PASSED`、diff=0），不得只寫「已完成」。
> 人類實機 gate（P8）由人簽核；AI 只能準備腳本、彙整結果、標記待驗。

---

## 6. 分工與寫入互斥

| 角色 | 擁有路徑 | 不得修改 |
|---|---|---|
| Lead（我） | `PLAN.md`、`mod/build.ps1`、`Directory.Build.*`、`mod/docs/**`、`mod/data/manifest.json`、`mod/docs/CONTRACT_SEE.md`、跨卡裁決 | 卡片實作檔（除非裁決衝突） |
| 架構卡 P0 | `mod/src/**` 骨架、`mod/docs/API_FACTS_SEE.md`、`mod/sln`、5 個 csproj（含 `SEE.Tests`） | `data/**`（除 manifest） |
| 資料卡 P1 | `mod/data/**`（manifest 除外）、`mod/tools/generate-blueprints/**` | `src/**`、`build.ps1` |
| Core 卡 P2 | `mod/src/SEE.Core/**`、`mod/tests/SEE.Tests/**` | Gameplay／UI／data |
| Gameplay 卡 P3 | `mod/src/SEE.Gameplay/**` | Core／UI／data |
| UI 卡 P4 | `mod/src/SEE.UI/**` | Core／Gameplay／data |
| 相容卡 P5 | `mod/src/SEE.Integration/**` | Core／Gameplay／UI |
| QA 卡 P6／P7 | `mod/tools/**`、`docs/VERIFICATION.md`、`docs/BALANCE_REPORT.md` | src 業務邏輯（發現 bug 開新卡） |
| 人類 gate P8 | `Documents\Timberborn\Mods\SteamEngineExpansion\**` | 全部原始碼（只驗不改） |
| 美術 P9 | `mod/art/**`、`data/Buildings/**/*.timbermesh`、`data/Icons/**` | src／藍圖數值 |

---

## 7. 品質門檻（每卡必須自檢）

1. `pwsh -File mod/build.ps1 -Clean`：**0 warning**（`TreatWarningsAsErrors=true`）、測試 `PASS n/n`、`VALIDATION PASSED`。
2. 每次 `dotnet build` 加 `-m:1`；`.csproj` 最多一個 `ProjectReference`；不新增 NuGet 依賴；不連網。
3. 藍圖數值 24 份 × 12 欄位與需求 §4.1 零誤差（由 `engines.json` 生成，重跑 diff=0；黃金雜湊 `blueprints.sha256`）。
4. 生成器 5 道閘門（水艙/燃料 ≥8×、混裝誤差 ≤12%、效率判準、容量逐格相等、藍圖不含 `BuildingPipeSpec`／`PowerLineSpec`）全過。
5. 技術閘門依需求 §13.8：T1 骨架（生成器 `--check`＋1 檔可載入）→ T2 核心（**消耗對帳 ≤ ±2%**、V-IMPL-2／3 有答案）→ T3 玩法（狀態機／維護）→ T4 相容（12 情境）→ T5 存檔（往返／損壞／遷移）→ T6 平衡效能（四判準＋預算）。前一關未過不進下一關。
6. 禁止項：無 Harmony patch 機械圖（PL 注入為唯一可選例外，預設關）、無硬引用 TimberPipes／PowerLines／ConveyorBelt DLL、無 `BuildingPipeSpec`、無每幀全圖掃描、無 `Update` 內 `FindObjects`／字串插值、無硬寫平衡數值。
7. 每卡輸出「需求 → 實作位置 → 驗證方式 → 結果」表，累積到 `docs/VERIFICATION.md`；文字用繁體中文、zhTW 不得混入簡體；loc 54 鍵齊全、無 raw key。
8. 違反需求紅線（簡報 §0、§1、§13 裁決）即 FAIL，不論功能是否可用。

---

## 8. 風險與緩解（本專案的實務版）

| # | 風險 | 觸發徵兆 | 緩解 |
|---|---|---|---|
| 1 | **D-1 比例耗燃料拿不到負載值** | P0 的 IL 查不到需求／供給成員 | 先實作「圖效率驅動」，把 `fuelScaling` 做成兩段：`proportional`（若能取需求）／`efficiency`（退化）；Lead 裁定並記錄偏離；不得讓引擎因低負載而超耗（可接受退化，不可反過來） |
| 2 | **雙庫存（固體燃料＋液體水）混裝體積誤差**（V-02） | 管道接上後水位異常／提前停機 | 兩庫存分離；水艙 ≥ 燃料×8；`tank` 需離線證據＋實機驗證，否則預設 `valve`／`hauler`；存檔只存自家旗標 |
| 3 | **24 藍圖手抄漂移** | 某檔燃料率 0.13 而非 0.12 | 只改 `engines.json`；生成器＋validator 逐格比對；CI 阻擋 diff |
| 4 | **實機無法自動驗證** | 卡片宣稱「實機通過」但無證據 | P8 人類 gate；AI 最多給「待驗」；17.5 腳本化；錄影／截圖清單 |
| 5 | **分叉改名漏改**（存檔 key／loc／manifest 撞名） | 存檔衝突、loc 顯示 raw key、mod 載入失敗 | P0 契約凍結＋`grep` 全庫掃描舊識別字（MWP／MechanicalWaterPump）；validator 檢查禁字 |
| 6 | **效能超標（1,000 棟）** | tick 累加／每幀 UI 重建 | Core 純邏輯先做微基準；tick 1s＋O(1)；UI 4Hz 髒標記；P7 預算、P8 Profiler |
| 7 | **美術拖垮時程** | 等模型才敢驗收 | v1 重用原版模型；P9 獨立可延後；不把 ART 當 M2 門檻 |
| 8 | **相容層反射錨點失效**（版本差異） | 偵測不到或注入例外 | 三段式偵測＋try/catch＋2s 預算＋單次快取；失敗降級不崩潰；以實際 workshop DLL IL 取證 |
| 9 | **平衡與原版比較口徑錯誤** | 只看額定輸出 | P7 用 §5.5 含維護停機算法；四條判準逐條；大型 Mk.IV +2.9% 餘裕列敏感項 |
| 10 | **材質陣營綁定**（R-A） | 建造選單載入失敗（不是單棟） | 一檔一陣營；FT 只引用 Folktails 模型；P9 自製中立材質後才統一 |
| 11 | **BlockObjectSpec 軸序寫反**（R-B） | 佔地／高度錯誤、放置失敗 | 以 `{X=寬,Y=深,Z=高}` 驗算 Blocks 數量；P1 首份藍圖先以 validator＋單棟實機確認 |
| 12 | **開發樹誤放 Mods／.deps.json 出貨**（R-F/R-G） | 遞迴載入、同 Id 崩潰 | 只放 staged `build/SteamEngineExpansion`；出貨清單由 P6 validator 白名單檢查 |

---

## 9. 立即下一步（執行順序）

1. **開始 SEE-P0**（任務板第一張卡）：分叉 MWP→SEE（5 程式集）、釘死 D-1～D-6、凍結契約（spec／藍圖／存檔／loc）、`build.ps1` 綠燈。
2. P0 完成後同時開 **SEE-P1（資料）** 與 **SEE-P2（Core）**（檔案互斥）。
3. 依 §3.1 波浪推進；每波結束由 Lead 跑門禁並更新 `docs/VERIFICATION.md`；技術閘門 T1→T6 逐關推進。
4. P8 前安排一次人類實機時段（30 分鐘，照 17.5 腳本）；P8 通過後才進 P9 美術終稿。

> 本規劃書是「可執行」的：卡片提示已包含需求路徑、寫入範圍、驗收指令與禁止事項；任何新發現先回到 `CONTRACT_SEE.md`／`API_FACTS_SEE.md`，再改卡片範圍，不直接改數值或跳卡。
