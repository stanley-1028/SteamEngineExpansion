# SEE 實作契約（Implementation Contract）

> **本檔是 SEE（Steam Engine Expansion）P0 凍結的介面契約，也是 P1–P9 卡片的唯一介面事實源。**
> 未經 Lead 同意，不得更改公開型別名、Spec JSON key、藍圖路徑、存檔 key 與 schema、本地化 key 集合、
> config 鍵名與 schema、或本檔任何「凍結」表格。需要變更時：卡片先回報 Lead → Lead 更新本檔與 `PLAN.md`
> → 才可施工（`TASKS.md`：契約變更一律走本檔的「待 Lead 裁決」區）。
>
> 文件版本：`1.4-P0-rev`（2026-11-06，docs-contract 撰寫；已納入 Lead 裁決 AD-3／AD-4／AD-9／AD-10／AD-11／D-6 與 `findings-power`／`findings-persistence`／`findings-inventory-maintenance` 取證，送 Lead 最終審核）
> 搭配文件：`mod/docs/API_FACTS_SEE.md`（IL 取證，P0 產出）、`mod/docs/REQ_TRACEABILITY.md`（274 條 REQ 對映）。
>
> 分叉來源為姊妹作《機械水泵》的已驗證骨架；其專案 Id、程式集前綴、存檔 key、loc 前綴、config 檔名
> **一律不得沿用**，以本契約 §2 的 SEE 命名為唯一合法值（全庫掃描＝0，P6 門禁）。

---

## 0. 定位、權威來源與寫入邊界

### 0.1 權威順序（衝突時）

| 順位 | 來源 | 路徑 | 說明 |
|---:|---|---|---|
| 1 | 《蒸汽引擎模組需求文檔》 | `C:\Users\user\Documents\deepseek-harness\default-workspace\steam-engine\蒸汽引擎模組需求文檔.md` | 正文＋§21.2 的 **274 條 REQ** 為權威（PLAN §4.5 裁決 D-1） |
| 2 | 共享寫作簡報 | `...\steam-engine\brief\SHARED_BRIEF.md` | §2 命名、§3 12 檔權威數值、§4 平衡公式、§5 狀態機、§6 維護、§7 相容、§8 存檔、§13 裁決 C-1～C-9 |
| 3 | **本契約** | `mod/docs/CONTRACT_SEE.md` | 把上位文件凍結成可施工的介面；與上位文件衝突時不得自行解讀 |
| 4 | 規劃書 | `PLAN.md` | §1.4 已用 IL 證實的技術事實、§1.6 陷阱 R-A～R-L、§4.5 裁決 D-8～D-11、§7 品質門檻 |
| 5 | 提取報告／考察報告 | `_investigate/doc-extract.md`、`template-reuse.md`、`feasibility.md` | 只作引用與施工導引，不新增需求 |

**衝突處置**：
1. 需求文件與 IL 事實衝突 → 以 IL 為準，並在 `API_FACTS_SEE.md` 記錄證據、在本檔 §11 記錄偏離（例：EntityId 型別、loc CSV 欄位數）。
2. 需求文件內部歧異 → 依 PLAN §4.5 裁決 D-1～D-7；未裁決者列入本檔 §11.2，**不得自行拍板**。
3. 本契約與需求／簡報衝突 → 先回報 Lead，由 Lead 裁定並更新本檔；卡片不得直接改契約或改數值。
4. Lead 已裁決的 PLAN §4.5 D-8～D-11，視為需求文件的實作口徑修正，已寫入本檔對應章節。

### 0.2 本契約涵蓋與不涵蓋

| 涵蓋（凍結） | 章節 | 不涵蓋（其他文件） |
|---|---|---|
| 程式集、命名空間、型別名、依賴方向 | §2 | 平衡 30 日模擬報告（P7 `BALANCE_REPORT.md`） |
| 自訂 Spec 與 JSON key、序列化口徑 | §3 | 人類實機驗收逐條證據（P8 `VERIFICATION.md`） |
| 24 份藍圖的必要 spec、路徑、數值、模型規則 | §4 | 3D 模型／動畫資產與面數（P9 `art/**`） |
| 54 個 loc 鍵與字元上限 | §5 | IL／組件 API 事實（P0 `API_FACTS_SEE.md`） |
| 存檔 schema v1、checksum、遷移、損壞隔離 | §6 | 需求條文全文（需求文檔 §1–§21） |
| `SEE_Config.json` 鍵群組與 clamp | §7 | REQ 對映表（`REQ_TRACEABILITY.md`） |
| 執行期行為摘要（照 §4/§5/§6/§7） | §8 | 發佈工作坊文案（P8 `README.md`／`CHANGELOG.md`） |
| 版本三軌、禁止項、待裁決 | §9–§11 | — |

### 0.3 寫入範圍（互斥；PLAN §6）

| 角色／卡 | 擁有路徑 | 不得修改 |
|---|---|---|
| Lead | `PLAN.md`、`TASKS.md`、`mod/build.ps1`、`Directory.Build.*`、`mod/docs/**`、`mod/data/manifest.json`、跨卡裁決 | 卡片實作檔（除非裁決衝突） |
| P0 架構 | `mod/src/**` 骨架、5 個 `.csproj`／`.sln`、`mod/docs/API_FACTS_SEE.md`、`mod/docs/CONTRACT_SEE.md`、`mod/docs/REQ_TRACEABILITY.md` | `data/**`（manifest 除外）、需求數值 |
| P1 資料 | `mod/data/**`（manifest 由 Lead 定稿）、`mod/tools/generate-blueprints/**` | `src/**`、`build.ps1` 主體（只可追加 generate 步驟） |
| P2 Core | `mod/src/SEE.Core/**`、`mod/tests/SEE.Tests/**`（Core 部分） | Gameplay／Integration／UI／data |
| P3 Gameplay | `mod/src/SEE.Gameplay/**` | Core／UI／data／契約名 |
| P4 UI | `mod/src/SEE.UI/**` | Core／Gameplay／data／loc key 集合 |
| P5 相容 | `mod/src/SEE.Integration/**`、`docs/COMPAT_MATRIX.md` | Core／Gameplay／UI 具體型別、外部模組藍圖 |
| P6 QA／P7 平衡 | `mod/tools/**`、`mod/build.ps1`（P6）、`docs/VERIFICATION.md`、`docs/BALANCE_REPORT.md` | `src` 業務邏輯（發現 bug 另開卡） |
| P8 人類 gate | `Documents\Timberborn\Mods\SteamEngineExpansion\**` | 全部原始碼（只驗不改） |
| P9 美術 | `mod/art/**`、`data/Buildings/Power/SteamBoiler/*.timbermesh`、`data/Icons/**` | `src`、藍圖數值、loc key |

### 0.4 卡片邊界（P0 交付什麼、P1–P9 不得改什麼）

| 卡 | 交付（只此範圍） | 不得改動 |
|---|---|---|
| **P0** | 5 程式集骨架（暫留分叉遺留物可，但不得出貨）；`build.ps1`／`Directory.Build.*` 綠燈；D-1～D-6 取證結論（`API_FACTS_SEE.md`）；**本契約＋REQ_TRACEABILITY 初版**；分層掃描規則 | 不寫 24 份正式藍圖（P1）、不改需求數值、不實作 business logic、不生成 54 鍵正式 loc（P1 可留原型） |
| **P1** | `data/engines.json`（唯一數值源）、生成器 7 步＋5 閘門＋`--check`、24 藍圖、2 份 TemplateCollection、54 鍵 loc（enUS/zhTW）、icon、manifest、`SEE_Config.json`、`engines.derived.json`＋黃金雜湊 | 不改 §2 命名、§3 spec 欄位、§4.2 數值、§5.2 key 集合、§7 config 鍵名；不改 `src/**` |
| **P2** | `SEE.Core` 純邏輯（公式、常數、列舉、狀態機、維護帳本、存檔編解碼 v1、config clamp、契約介面）＋L1 測試 | 不引用遊戲 DLL（D-11）；不放 `ComponentSpec`；不改 §2/§3/§6/§7 契約；不改 Gameplay／UI |
| **P3** | `SEE.Gameplay`：`SEEBoilerSpec`／`SEEMaintenanceSpec`、兩控制器、`BoilerTickSystem`、燃料／水雙軌、比例消耗、維護、存檔橋、註冊 | 不改 Core 公開名字、不改 spec 欄位名與 JSON key、不加 Harmony patch、不引用外部 DLL |
| **P4** | `SEE.UI` 面板、徽章、54 鍵使用與對映 | 不改 key 名稱、不引用 Gameplay 具體型別、不每幀重建 |
| **P5** | `SEE.Integration` 三段式偵測、3 適配器、可選注入、`SaveSerializer`／`SaveMigrator`／`SaveChecksum` | 不改核心玩法、不改 §6 schema、不建立外部 DLL 編譯期參考 |
| **P6** | validator、`build.ps1` 門禁、黃金雜湊、禁止項靜態掃描 | 不改 `src` 業務邏輯、不改數值；發現問題開卡 |
| **P7** | 30 日模擬、四判準、效能預算（離線） | 不改預設值（只出報告，由 Lead 決定是否進 `engines.json`）；不得把實機壓測標成已通過 |
| **P8** | 打包、安裝腳本、人類實機驗收紀錄 | 不改源碼（只驗不改）、AI 不得宣告實機通過 |
| **P9** | 12×2 模型／LOD／icon／動畫 | 不改藍圖數值、不改 loc key、不改 `src` 業務邏輯 |

---

## 1. 建置、測試與驗證門檻

### 1.1 指令（每卡自檢；PLAN §7、PLAN §1.4 第 6 條）

```powershell
# 全部建置＋測試＋資料驗證＋打包（Lead／P6 執行）
pwsh -File mod/build.ps1 -Clean

# 單一專案（注意 -m:1）
dotnet build mod/src/SEE.Gameplay/SEE.Gameplay.csproj -c Release -m:1
dotnet run --project mod/tests/SEE.Tests/SEE.Tests.csproj -c Release -m:1
dotnet run --project mod/tools/validate/validate.csproj -c Release -m:1 -- mod/data --managed "<Timberborn>\Timberborn_Data\Managed"
```

來源：`_investigate/template-reuse.md` §2.3（`build.ps1` 流程）、§2.6（最少檔案清單）。

### 1.2 建置硬規則（違反即 FAIL）

| # | 規則 | 依據 |
|---:|---|---|
| 1 | 每次 `dotnet build` 必加 `-m:1` | PLAN §1.4 第 6 條 |
| 2 | **一個 `.csproj` 最多一個 `ProjectReference`**（`Core ← Gameplay／Integration／UI`） | PLAN §1.4 第 6 條 |
| 3 | `TreatWarningsAsErrors=true`：**0 warning**；公開型別 XML doc 完整（CS1591 為錯誤） | PLAN §7.1、REQ-NFR-01 |
| 4 | 遊戲端程式集 `netstandard2.1`，遊戲 DLL `Private=false`；Core 不得引用任何遊戲組件 | PLAN §1.4 第 6 條、REQ-NFR-10 |
| 5 | 不得連網、不得新增 NuGet 套件（Mono.Cecil 只走本機快取） | PLAN §7.2、`feasibility.md` §2 |
| 6 | `SEE.Tests` 目標 `net9.0`，零外部依賴，可依賴全部 SEE 程式集（D-11） | PLAN §4.5 D-11 |
| 7 | 檔案 ≤400 行（spec 定義檔 ≤600）、方法 ≤60 行、圈複雜度 ≤10、參數 ≤5、巢狀 ≤3 | REQ-ARCH-10、REQ-NFR-03 |

### 1.3 分層掃描（架構測試，P0 建立規則、P6 納入門禁）

`SEE.Tests` 以 `Assembly.GetReferencedAssemblies()` 讀取每個出貨程式集的參考，**出現禁止的簡名即 FAIL**
（REQ-ARCH-02、REQ-NFR-10）：

| 被掃描組件 | 允許參考 | 禁止出現（簡名） |
|---|---|---|
| `SEE.Core` | 僅 BCL（`netstandard`／`System.*`） | 遊戲組件、`SEE.*` 其他程式集、任何外部模組 DLL |
| `SEE.Gameplay` | `SEE.Core`＋遊戲組件 | `SEE.Integration`／`SEE.UI`／外部模組 DLL |
| `SEE.Integration` | `SEE.Core`＋`SEE.Gameplay`＋遊戲組件 | `SEE.UI`、外部模組 DLL（外部型別只用字串名） |
| `SEE.UI` | `SEE.Core`＋遊戲組件 | `SEE.Gameplay`／`SEE.Integration`、外部模組 DLL |
| `SEE.Tests` | 全部 SEE 程式集 | 無（測試專用；但不得被出貨清單收錄） |

> 出貨白名單固定為 `SEE.Core`、`SEE.Gameplay`、`SEE.Integration`、`SEE.UI` 四個 DLL；`SEE.Tests` 一律排除
> （PLAN §1.6 R-E）。`.deps.json` 不隨包出貨（R-G）。

### 1.4 技術閘門 T1–T6（前一關未過不進下一關；需求 §13.8、doc-extract §3.12）

| 階段 | 目標 | 通過條件 | 主責卡 |
|---|---|---|---|
| T1 骨架 | 五程式集編譯、生成器、1 檔可建 | 生成器 `--check` 通過；1 份藍圖可載入；build 0 warning | P0／P1 |
| T2 核心 | 燃料／水、比例消耗、狀態機、輸出 | **消耗對帳 ≤ ±2%**；V-IMPL-2／3 有答案（D-1） | P2／P3 |
| T3 維護 | 磨損、自動／手動、2 小時 | 30 遊戲日回歸：維護次數與材料消耗符合預期 | P2／P3 |
| T4 相容 | 三段式偵測、3 適配器、PL 注入 | 12 相容情境全過 | P5 |
| T5 存檔 | schema、遷移、checksum、損壞恢復 | 6 情境＋1,000 棟體積／耗時 | P5 |
| T6 調校 | 效能、平衡、1,000 棟 | 需求 §12.6／§13.7 全達標；§17 驗收清單 | P6／P7 |

### 1.5 每卡交付證據（「真實輸出」，不得只寫「已完成」）

`pwsh -File mod/build.ps1 -Clean` 的 `0 warning`、`PASS n/n`、`VALIDATION PASSED` 原文；生成器重跑 diff=0；
24 藍圖 12 欄位零誤差；51/54 鍵 loc 核對；契約偏離清單（若無寫「無」）。

---

## 2. 程式集與命名契約

### 2.1 五程式集與依賴方向（凍結）

| 程式集 | 職責 | 允許依賴 | 禁止依賴 | 主要內容 | 行數預算 |
|---|---|---|---|---|---|
| `SEE.Core` | 契約層：公式、常數、列舉、狀態機、維護帳本、存檔編解碼、config clamp、唯讀檢視介面 | 僅 BCL | 遊戲組件、任何 SEE 程式集（除自身）、外部模組 DLL | `BoilerFormulas`、`ConditionCurve`、`SEEConst`、`SEEVersion`、`TierCatalog`、`BoilerStateMachine`、`MaintenanceLedger`、`IBoilerStatusView`、`IBoilerLogic`、`MiniJson`、`ThrottledLog` | ≤1,200 行 |
| `SEE.Gameplay` | 玩法層：兩個 `ComponentSpec`、控制器、Tick 系統、燃料／水消耗、維護、存檔橋 | `SEE.Core`＋遊戲組件 | `SEE.Integration`、`SEE.UI`、外部模組 DLL | `SEEBoilerSpec`、`SEEMaintenanceSpec`、`SEEBoilerController`、`SEEMaintenanceController`、`BoilerTickSystem` | ≤2,500 行 |
| `SEE.Integration` | 邊界層：相容偵測、適配器、可選注入、存檔序列化與遷移 | `SEE.Core`、`SEE.Gameplay`＋遊戲組件 | `SEE.UI`、外部模組 DLL（只用字串型別名） | `CompatDetector`、`TimberPipesAdapter`、`PowerLinesAdapter`、`ConveyorBeltAdapter`、`OptionalPatchHost`、`SaveSerializer`、`SaveMigrator`、`SaveChecksum` | ≤2,000 行 |
| `SEE.UI` | 介面層：建築面板、狀態徽章、本地化使用 | `SEE.Core`（只讀 `IBoilerStatusView`）＋遊戲組件 | `SEE.Gameplay`、`SEE.Integration`、外部模組 DLL | `BoilerPanelFragment`、`StatusDescriptors` | ≤1,200 行 |
| `SEE.Tests` | 測試層：單元、黃金值、分層掃描、存檔遷移、config clamp | **全部 SEE 程式集** | 無（測試專用，不出貨） | xUnit 風格 runner、`Golden/blueprints.sha256`、`Layering/LayerRulesTests` | 不設限 |

依賴方向（單向）：

```
遊戲本體（1.1.2.8）
        ↓
  SEE.Core（契約／公式／常數）
   ├→ SEE.Gameplay ──→ SEE.Integration
   └→ SEE.UI（只讀；實作由 Gameplay 啟動時一次性注入）
              ↑
          SEE.Tests（可依賴全部）
```

- `Core` 不得反向引用 `Gameplay`；`UI` 不得引用 `Gameplay` 具體型別；`Integration` 對外部模組只用字串型別名。
- 來源：需求 §11.1、doc-extract §3.1、PLAN §1.4 第 9 條、SHARED_BRIEF §2。

### 2.2 命名空間與檔案配置（凍結）

| 項目 | 契約值 |
|---|---|
| 命名空間根 | `SEE` |
| Core 命名空間 | `SEE.Core`（子：`.Balance`、`.Contracts`、`.Config`、`.Json`、`.Logging`、`.Save`、`.State`） |
| Gameplay 命名空間 | `SEE.Gameplay`（子：`.Components`、`.Systems`、`.Specs`、`.Registration`） |
| Integration 命名空間 | `SEE.Integration`（子：`.Detection`、`.Adapters`、`.Patching`、`.Save`） |
| UI 命名空間 | `SEE.UI`（子：`.Panels`、`.Status`） |
| Tests 命名空間 | `SEE.Tests` |
| 目錄配置 | 照需求 §11.2／doc-extract §3.2：`Specs/`、`Formulas/`、`Constants/`、`Contracts/`、`Components/`、`Systems/`、`Registration/`、`Detection/`、`Adapters/`、`Patching/`、`Save/`、`Panels/`、`Status/` |

命名規則：Spec＝`<名稱>Spec`；控制器＝`<名稱>Controller`；介面＝`I<名稱>`；常數＝PascalCase
`static readonly`；事件／委派不得放在 spec（REQ-IMPL-15）。來源：doc-extract §3.5。

### 2.3 D-11 分層偏離（**必讀**；PLAN §4.5 D-11）

需求 §11.1 寫「`SEEBoilerSpec`／`SEEMaintenanceSpec` 放 `SEE.Core`」，但 `ComponentSpec` 必須繼承遊戲型別，
若 Core 引用遊戲 DLL，`SEE.Tests`（`net9.0` 離線）就無法乾淨引用 Core。**裁決**：

| 項目 | 契約 |
|---|---|
| `SEE.Core` | **零遊戲相依**；只放公式／常數／列舉／狀態機／維護帳本／存檔編解碼／config／接口 |
| 兩個 `ComponentSpec` | 放 **`SEE.Gameplay`**（`SEE.Gameplay/Specs/`），namespace `SEE.Gameplay`，JSON key 見 §3.1 |
| `SEE.Tests` | 可依賴全部 SEE 程式集；L1 測試（Core 純邏輯）與 L2 契約測試分開命名 |
| 影響 | `REQ-ARCH-01`、`REQ-IMPL-01/15` 的實作位置以本節為準；`REQ-TRACEABILITY` 標註此偏離（見 §11.3） |
| 不變 | 分層方向（UI 只讀 Core、Gameplay 依賴 Core）與依賴掃描規則完全照 §1.3 |

### 2.4 凍結型別名（不得改名）

| 型別名 | 程式集 | 種類 | 職責 | 來源 |
|---|---|---|---|---|
| `SEEBoilerSpec` | `SEE.Gameplay` | `ComponentSpec`（record） | 等級／尺寸／陣營標記、額定馬力、燃料與水耗率、燃料種類與能量、供水模式提示 | 需求 §13.3、PLAN §4.5 D-11 |
| `SEEMaintenanceSpec` | `SEE.Gameplay` | `ComponentSpec`（record） | 維護間隔、負載係數、觸發閾值、維護時長、材料表 | 需求 §13.3、PLAN §4.5 D-11 |
| `SEEBoilerController` | `SEE.Gameplay` | 組件（`BaseComponent`） | 每棟狀態機、燃料／水消耗、輸出調變、狀態事件 | 需求 §13.3 |
| `SEEMaintenanceController` | `SEE.Gameplay` | 組件（`BaseComponent`） | 自動／手動維護、材料檢查、道路可達、2 小時倒數 | 需求 §13.3 |
| `BoilerTickSystem` | `SEE.Gameplay` | 系統（單一入口） | 每 1 秒結算、例外邊界、O(1)、可降頻、可測試 | 需求 §13.3 |
| `IBoilerStatusView` | `SEE.Core` | 介面（唯讀） | UI 讀狀態、數字、提示（`Guid` 為鍵） | 需求 §11.1、D-11 |
| `IBoilerLogic` | `SEE.Core` | 介面（命令） | UI→Gameplay 的命令契約（暫停、維護、自動維護、供水模式） | 需求 §11.1、doc-extract §3.2 |
| `BoilerFormulas` | `SEE.Core` | 靜態類 | 燃料／水／輸出／磨損公式的**唯一實作**（生成器與遊戲共用） | REQ-ARCH-12、REQ-IMPL-17 |
| `ConditionCurve` | `SEE.Core` | 靜態類 | 機況乘數與狀態門檻（61/60/41/40/21/20/1/0） | 需求 §9.3 |
| `BoilerStateMachine` | `SEE.Core` | 靜態類 | 9 主狀態＋`Maintaining` 覆蓋態的優先序解析 | 需求 §5、C-6 |
| `MaintenanceLedger` | `SEE.Core` | 類 | condition、磨損累積、維護階段與倒數 | 需求 §9 |
| `SEEConst` | `SEE.Core` | 常數類 | 執行期常數唯一來源（`TickIntervalSeconds=1.0`、`DetectBudgetSeconds=2.0`、`LogThrottleSeconds=30` 等） | 需求 §11.4、doc-extract §3.4 |
| `SEEVersion` | `SEE.Core` | 常數類 | 三套版本號與遷移鏈入口 | 需求 §11.6、§9 |
| `TierCatalog`／`TierDefinition` | `SEE.Core` | 資料類 | 12 檔×2 陣營的派生後規格（唯一數值載體） | PLAN §1.4 第 9 條 |
| `CompatDetector`／`TimberPipesAdapter`／`PowerLinesAdapter`／`ConveyorBeltAdapter` | `SEE.Integration` | 類 | 三段式偵測與適配器（對外介面 `IWaterSupplyAdapter`／`IPowerLineAdapter`／`IFuelSupplyAdapter`，皆繼承 `IExternalAdapter`） | doc-extract §3.1 |
| `SaveSerializer`／`SaveMigrator`／`SaveChecksum` | `SEE.Integration` | 類 | 存檔序列化、遷移鏈、校驗和 | 需求 §14、doc-extract §3.1 |
| `BoilerPanelFragment`／`StatusDescriptors` | `SEE.UI` | 類 | 面板與狀態描述 | 需求 §15、doc-extract §3.1 |

> 分叉遺留的既有 `*Pump*`／`SeePump*` 型別名**不屬於本契約**；P2/P3 必須改名為上表名稱（或刪除），
> 並在 `VERIFICATION.md` 記錄「舊名 0 命中」掃描結果。`SteamEngineExpansionSpec` 為分叉遺留的單一 spec，
> P3 必須拆成 `SEEBoilerSpec`＋`SEEMaintenanceSpec`（見 §3）。

### 2.5 組件綁定與 DI（P3 實作口徑）

- spec 必須是 **public record、非抽象、繼承 `ComponentSpec`**，屬性以 `[Serialize]` 標記；否則
  `SpecTypeCache.IsSpecClass` 找不到（`API_FACTS_SEE.md`、`mod/docs/api/gameplay-findings.md` §5）。
- 綁定用 `TemplateModule.Builder().AddDecorator<TSubject, TDecorator>()`；`TDecorator` 必須是 `BaseComponent`
  且**只有一個建構子**（PLAN §1.4 第 10 條）。
- `IBoilerStatusView`／`IBoilerLogic` 由 Gameplay 以 singleton 註冊、UI 以 `ToExisting` 取同一實例
  （驗證過的相同實例模式，見 `gameplay-findings.md` §1）；UI 不得自行 new。
- 遊戲端 Tick 使用 `TickableComponent`＋`Ticker`（1 tick＝0.03125 遊戲小時），**不用 Unity `Update` 做玩法**
  （REQ-SAFE-07；`gameplay-findings.md` §2）。

### 2.6 `manifest.json` 契約（凍結）

遊戲的 `manifest.json` 只有 7 個欄位、**沒有 `Assemblies`**（`feasibility.md` §4.6）：

| 欄位 | 凍結值 | 備註 |
|---|---|---|
| `Name` | `Steam Engine Expansion` | 顯示名（英文） |
| `Version` | `0.1.0` | 三軌之一（§9）；P1 必須把分叉遺留的舊版本改為此值 |
| `Id` | `SteamEngineExpansion` | 模組資料夾與存檔診斷名 |
| `MinimumGameVersion` | `1.1.0.0` | 以 1.1.2.8 為驗證基準 |
| `Description` | 英文一句話定位＋「免工人、燃料＋水、需維護、跨陣營、外部模組皆為選配」 | 不得宣稱產水、不得沿用舊專案名 |
| `RequiredMods` | `[]` | 只依賴遊戲本體（需求 §10.1） |
| `OptionalMods` | `[]` | 軟依賴只用執行期偵測，不寫 manifest |

- 成品 stage 目錄：`build/SteamEngineExpansion/`（根目錄放 4 個 DLL＋`manifest.json`＋`Blueprints/`＋
  `Localizations/`＋`Icons/`＋`TemplateCollections/`＋`SEE_Config.json`）；不附 `.deps.json`（R-F／R-G）。
- `manifest.json` 由 Lead 定稿（P0 寫入範圍表）；P1 只可改 `Version`／`Description` 以外的內容需 Lead 同意。

### 2.7 TemplateName 與藍圖命名規則（凍結）

| 項目 | 規則 | 例 |
|---|---|---|
| TemplateName | `SteamBoiler.<Size>.<Tier>.<Faction>` | `SteamBoiler.Medium.T2.Folktails` |
| Size | `Small`／`Medium`／`Large` | — |
| Tier | `T1`／`T2`／`T3`／`T4` | — |
| Faction | `Folktails`／`IronTeeth` | — |
| 檔名 | `<TemplateName>.blueprint.json` | `SteamBoiler.Large.T4.IronTeeth.blueprint.json` |
| 總數 | **12 檔 × 2 陣營 = 24 份**，不得多、不得少 | REQ-ARCH-05 |

**禁止**使用 `SteamEngine`、`Building.SteamEngine.*`、`SteamPlant`、`TWSteamEngine` 或分叉來源專案的舊前綴
作為 TemplateName／loc key／namespace／存檔 key／config 檔名（SHARED_BRIEF §2、REQ-RISK-09）。

### 2.8 日誌契約

| 類別 | 用途 | 規則 |
|---|---|---|
| `SEE/Info` | 啟動、版本、診斷 | 同一 key 30 秒節流；只記一次 |
| `SEE/Warn` | 可恢復異常（clamp、損壞修復、cap 超限） | 記一次＋抑制次數（REQ-SAFE-02） |
| `SEE/Error` | 存檔寫入失敗、無法恢復的例外 | 例外只在該棟邊界；不得洗版 |
| `SEE/Compat` | 外部模組偵測結果／失敗 | 每模組一次；不記 stack 以外的敏感資料 |

實作入口：`SEE.Core.Logging.ISeeLog`／`ThrottledLog`（分叉保留，改名為 See 前綴）；不得在 `Update` 中字串插值
（REQ-OPS-08）。

---

## 3. 自訂 Spec 契約（已凍結）

### 3.1 Spec JSON key 與 `[SpecAlias]`（凍結；P0 取證）

**規則（IL 證實）**：`SpecTypeCache.GetSpecEntries(type)` 只用每個 `[SpecAlias]` 字串＋CLR `Type.Name` 索引
spec，**不使用命名空間全名**；缺 `[SpecAlias]` 會讓藍圖的自訂 spec **靜默失效**，建築實例化失敗
（`mod/docs/api/gameplay-findings.md` §5；`SpecTypeCache::Create/GetSpecEntries`、`SpecAliasAttribute`）。

| C# 型別 | `[SpecAlias]` 與藍圖頂層 key | 程式集／namespace | 藍圖必要 | 說明 |
|---|---|---|---|---|
| `SEEBoilerSpec` | `[SpecAlias("SEE.SEEBoilerSpec")]`；藍圖 key **`SEE.SEEBoilerSpec`** | `SEE.Gameplay`／`SEE.Gameplay` | ✅ 24 份全有 | 欄位見 §3.2 |
| `SEEMaintenanceSpec` | `[SpecAlias("SEE.SEEMaintenanceSpec")]`；藍圖 key **`SEE.SEEMaintenanceSpec`** | `SEE.Gameplay`／`SEE.Gameplay` | ✅ 24 份全有 | 欄位見 §3.3 |

- 藍圖一律使用上表的 **dotted alias**；雖然 `Type.Name`（`SEEBoilerSpec`）也可能被解析，但契約**禁止**使用，
  避免同一 spec 出現兩套 key（P6 validator 掃描）。
- 兩個 spec 的 C# namespace 是 **`SEE.Gameplay`**（不是根 `SEE`）；`[SpecAlias]` 的 dotted 字串才是藍圖 key，
  兩者不同、不得混用（已與 skeleton-src 同步）。
- alias 字串一旦出貨即為外部契約：不得改名（改名等於藍圖不相容）。
- spec 類別檔頭必須加 XML doc 說明 JSON key 與 alias（CS1591 門檻）。

### 3.2 `SEEBoilerSpec` 欄位（需求 §13.3 逐字；唯讀表）

| 欄位 | 型別 | 預設 | 備註 |
|---|---|---|---|
| `Tier` | string | `"T2"` | `T1`–`T4`；診斷與 UI 用 |
| `Size` | string | `"Medium"` | `Small`／`Medium`／`Large` |
| `Faction` | string | `"Folktails"` | 顯示與診斷用 |
| `Horsepower` | int | `460` | **已乘陣營輸出係數**（藍圖 `PowerOutput` 同值；控制器不得二次乘陣營，PLAN §4.5 裁決 D-3） |
| `FuelPerHour` | float | `0.11f` | 已乘陣營燃料係數、2 位小數；**實際消耗率＝此值 × λ**（由 controller 以能量結算，見 §3.5.2）；**不等於** `ConsumedGoods.GoodPerHour`（後者僅供容量派生） |
| `WaterPerHour` | float | `0.32f` | 未乘陣營（水耗兩陣營相同）；實際耗水＝此值 × λ |
| `FuelCapacityGoods` | int | `3` | 表定值；兩陣營相同。容量由 `ConsumedGoods.GoodPerHour` 派生（§3.5.2），此欄位供生成器／validator 對帳 |
| `WaterCapacityGoods` | int | `64` | 表定值為權威（液體 Stockpile 容量） |
| `FuelGoodIds` | string[] | `{ "Log" }` | 預設只允許 Log；T3／T4 由生成器逐檔寫入 `{ "Log", "Plank" }`（C-7） |
| `FuelValues` | float[] | `{ 1.0f }` | 能量值：Log＝1.0、Plank＝2.0（1 木板＝2 原木能量） |
| `FuelScaling` | string | `"proportional"` | `proportional`（預設）／`vanilla` |
| `AutoMaintenanceDefault` | bool | `true` | 新建時自動維護預設值 |

### 3.3 `SEEMaintenanceSpec` 欄位（需求 §13.3 逐字；唯讀表）

| 欄位 | 型別 | 預設 | 備註 |
|---|---|---|---|
| `Enabled` | bool | `true` | 總開關；`false` 時不累積磨損、UI 不出現維護區、condition 固定 100（REQ-IMPL-16） |
| `IntervalDays` | float | `8f` | T1–T4：6／8／10／12；磨損速率由 `50/(IntervalDays×24)` **即時計算**，不另存真值 |
| `LoadFloor` | float | `0.4f` | 負載係數下限 |
| `LoadFactorRange` | float | `0.6f` | 負載係數＝`LoadFloor + LoadFactorRange × 負載比` |
| `TriggerThreshold` | float | `50f` | `condition ≤ TriggerThreshold` 觸發自動維護 |
| `DurationHours` | float | `2f` | 維護 2 遊戲小時（≈38.4 真實秒，REQ-MAINT-09） |
| `Materials` | 見 §3.4 | `[]` | 每次維護材料；v1 單一材料組 |

- `wearPerRunningHour` **不寫入 spec**（由 `IntervalDays` 即時算；REQ-IMPL-08）。
- spec 一律只用可序列化型別（`int`／`float`／`string`／陣列／`ImmutableArray<GoodAmountSpec>`／record struct），
  **不得**放委派、遊戲物件、`List<T>`／`T[]` 的自訂物件（REQ-IMPL-15、§3.4）。

### 3.4 序列化型別口徑（Lead 補充；與需求字面的偏離）

需求 §13.3 字面寫 `MaintenanceMaterial[]`（`readonly record struct MaintenanceMaterial(string GoodId, int Amount)`）。
IL 證實藍圖反序列化器**只對 `ImmutableArray<T>` 逐元素遞迴**；`T[]`／`List<T>` 會走 primitive 序列化而損壞自訂物件
（`BasicDeserializer::DeserializeArray`；`gameplay-findings.md` §5）。因此：

| 層 | 型別 | 說明 |
|---|---|---|
| 藍圖／`SEEMaintenanceSpec` 屬性 | **`ImmutableArray<GoodAmountSpec>`（vanilla `Timberborn.Goods`）** | 與原版 `BuildingSpec.BuildingCost` 同型別、同序列化路徑 |
| 藍圖 JSON 格式 | `[ { "Id": "Plank", "Amount": 12 }, { "Id": "Gear", "Amount": 2 } ]` | **欄位名為 `Id`／`Amount`**（`GoodAmountSpec` 的 CLR 屬性；原版 SteamEngine `BuildingCost` 實證 `{"Id":"Log","Amount":20}`） |
| `SEE.Core` 邏輯 | 可保留 `MaintenanceMaterial`（`readonly record struct`；`GoodId`／`Amount`） | 由 Gameplay 在 spec → 邏輯之間映射 |
| 偏離登記 | 需求 §13.3 的 `MaintenanceMaterial[]` 視為**邏輯型別**而非藍圖序列化型別 | **已由 Lead 裁決（2026-11-06，AD-4）**；藍圖欄位名採 `Id`／`Amount`（見下方兩型別區分） |

**`GoodAmountSpec` 與 `ConsumedGoodSpec` 不得混淆（AD-4 裁決）**

| 型別 | 用途 | JSON 欄位 | 證據 |
|---|---|---|---|
| `Timberborn.Goods.GoodAmountSpec` | `SEEMaintenanceSpec.Materials`、`BuildingSpec.BuildingCost` 等「物品＋數量」 | **`Id`**（string）、`Amount`（int） | IL：backing fields `<Id>`／`<Amount>`；原版 SteamEngine `BuildingCost`＝`{"Id":"Log","Amount":20}` |
| `Timberborn.Goods.ConsumedGoodSpec`（`GoodConsumingBuildingSpec.ConsumedGoods` 元素） | 燃料消耗宣告（本契約下僅供容量派生，見 §3.5.2） | **`GoodId`**（string）、`GoodPerHour`（float） | 原版 SteamEngine `ConsumedGoods`＝`{"GoodId":"Log","GoodPerHour":0.1}`、`{"GoodId":"Water","GoodPerHour":0.3}` |

> Lead 2026-11-06 訊息原寫「`GoodId`」為誤植，已由 Lead 以 IL 更正：**`GoodAmountSpec` 是 `Id`／`Amount`**；
> `GoodId`／`GoodPerHour` 屬於 `ConsumedGoodSpec`。P1 生成器與 P6 validator 必須分開檢查兩種欄位名。

### 3.5 燃料庫存容量與消耗口徑（D-3 已證實＋AD-3 Lead 裁決）

#### 3.5.1 容量派生（D-3 已證實）

**IL 取證結論**：`GoodConsumingBuildingSpec` 的庫存容量派生為**逐項**
`Mathf.CeilToInt((float)GoodPerHour × FullInventoryWorkHours)`，多項時**容量總和＝各項之和**。

| 項目 | 契約 |
|---|---|
| 燃料庫存容量 | `Σ ceil(GoodPerHour_i × FullInventoryWorkHours)`；每項獨立 ceil 後相加 |
| 水艙容量 | **不經 `GoodConsumingBuildingSpec`**；由程式建立的 `WaterTank` public input `Inventory` 以 `WaterCapacityGoods` 為容量（表定值權威；REQ-FUEL-08、REQ-BLD-09）。**不用液體 `StockpileSpec`**：Stockpile＋GCB 會雙初始化 `NoHaulingPostStatus` 而崩潰（2026-11-08 修根因） |
| 生成器驗算 | 必須以 **float32 模擬** `CeilToInt` 後與表定 `fuelStock`／`waterTankGoods` 逐格相等（含 `Large.T1 = 58`）；生成器五道閘門之一 |
| 容量來源 | 由**表定 `fuelStock` 反推** `GoodPerHour`（見 §3.5.2）；**不是**由表定燃料率正向派生，避免陣營取整造成容量差 1 |

- IL 補充（AD-10）：`GoodConsumingBuilding.MaximumWorkingTime = FullInventoryWorkHours + 1 / Max(GoodPerHour)`
  （**不是**除法；此為原版參考值，本契約平衡不使用，僅供診斷）。
- `GoodConsumingBuildingSpec` 的容量派生只決定「庫存上限」；實際填滿由搬運工 `FillInput` 路徑完成（AD-10）。

#### 3.5.2 燃料消耗口徑（AD-3 裁決，2026-11-06；容量口徑與消耗口徑分離）

1. **燃料庫存仍用 `GoodConsumingBuildingSpec`**（`FullInventoryWorkHours = 25`），保留原版搬運／輸送帶／
   `EnterableSpec` 物流路徑（方案 A 骨架不動）。
2. **原版消耗永久暫停**：`SEEBoilerController` 在建築完成時取 `GetGoodConsumingToggle().PauseConsumption()`
   （public，已由 IL 證實）；燃料／水消耗一律由 controller 以 `Inventory.TakeConsumed`（public）結算，λ 見
   `API_FACTS_SEE.md` §1.2。`fuelScaling="vanilla"` 也由 controller 以 **λ=1** 實現，**不恢復**原版線性消耗，
   以免 T3/T4 雙燃料同時扣料。
3. **`ConsumedGoods.GoodPerHour` 只作容量派生**（因消耗已由 controller 驅動）。**最終生成公式（Lead 2026-11-06；
   取代早前的 floor2 版本，P1 生成器／P3 控制器／P6 validator 一律照此）**：

   | 檔位 | `ConsumedGoods` | `GoodPerHour` 生成公式 | 驗算（`CeilToInt(rate×25)`） |
   |---|---|---|---|
   | T1／T2 | 單一 `Log` | **`(fuelStock − 0.5) / 25`** | fuelStock 1/2/3/4/5/6 → rate 0.02/0.06/0.10/0.14/0.18/0.22 → 容量 1/2/3/4/5/6 |
   | T3／T4 | `Log`＋`Plank` 兩項 | **每項 `(fuelStock/2 − 0.5) / 25`** | fuelStock 2/4/6（皆偶數）→ 各 0.02/0.06/0.10 → 容量＝`2×ceil(rate×25)` = 2/4/6 |

   - 範例：`fuelStock=5` → rate 0.18 → `ceil(0.18×25)=ceil(4.5)=5`；封閉式讓 `rate×25` 恆落在半整數（x.5），
     與整數邊界相距 0.5，**避開** `0.2f×25 = 5.0000001 → CeilToInt=6` 的 float 陷阱。
   - 原型藍圖（Medium.T2，fuelStock 3）→ rate = **0.10**（已通知 data-see）；**不得寫 0.12**（表定燃料率 ≠ 容量速率）。
   - 生成器與 P6 validator 必須以 float32 模擬 `Mathf.CeilToInt` 驗算，並與表定 `fuelStock` 逐格比對；
     不得用 double 心算或直接寫燃料率取代。
   - 早前的「`floor2(fuelStock/25 − 0.01)`」版本已作廢，不得再出現在生成器、validator 或文件中（除本行歷史註記）。
4. **實作口徑偏離（契約記錄）**：藍圖 `ConsumedGoods.GoodPerHour` **≠ 表定燃料率**（僅容量用途）；
   實際消耗＝`SEEBoilerSpec.FuelPerHour × λ`、能量換算 **Log=1.0／Plank=2.0**（需求 §13.4 的 E/value），
   由 `SEEBoilerController` 實作。此與需求 §13.4「`GoodConsumingBuildingSpec` 只作燃料庫存宣告」方向一致，
   但速率欄位語意依本節；P1 生成器與 P3 控制器照此產生／實作。
5. **燃料品項優先序（P3 實作口徑）**：以能量 E 結算，先消耗 Log；Log 不足時以能量等值消耗 Plank
   （1 Plank 抵 2 Log 能量，對應 REQ-FUEL-02 的 ×0.5 速率）。順序不影響平衡判準，但 P8 消耗對帳必須涵蓋
   Log-only 與 Plank-fallback 兩條路徑。
6. **解掉 PLAN §4.5 D-6**：容量不再由陣營衍生燃料率取整決定；FT／IT 容量逐格相同且等於表定。
   `engines.derived.json` 仍保留 `factionFuelCollapsed` 供平衡報告（實際 `FuelPerHour` 2 位小數可能塌陷，
   但**不影響容量**）。
7. **水**：`Water` **不得**列進 `GoodConsumingBuildingSpec`；水艙由 `SEEWaterInventoryInitializer`
   以 dedicated decorator 建立 `WaterTank` public input `Inventory`（`StorableGood.CreateGiveableAndTakeable("Water")`、
   `HasPublicInput()`、容量＝`WaterCapacityGoods`），controller 在完工時 `Enable()`、離開完成態時 `Disable()`、
   扣水以 `TakeExisting`（浮點餘量、滿 1 扣整數，§4.7）。燃料與水各自是獨立搬運目標，普通搬運工都能送。

#### 3.5.3 備援（僅 P8 實機失敗才啟用，不得提前使用）

| 觸發 | 備援 | 風險／記錄要求 |
|---|---|---|
| P8 消耗對帳 >±2%，或 `PauseConsumption` 實機無效 | 自訂 Inventory decorator（方案 C）：`InventoryInitializerFactory`／`InventoryInitializer.Initialize(componentName, capacity, goods, publicInput, …)` 皆 public（IL 已證實） | **V-04 風險**：搬運／輸送帶是否接受自訂庫存需重驗；偏離方案 A 骨架，需 Lead 同意並記錄 |
| P8 對帳 >±2%／pause 無效（替代方案，與方案 C 並列） | **toggle 佔空比**（`_investigate/findings-power.md` §4.4 B 機制）：以 `PauseConsumption()`／`ResumeConsumption()` 的佔空比近似 λ，替代逐 tick 手動扣料；30 日平均口徑驗收 | 僅 P8 失敗才啟用；由 Lead 在方案 C 與本方案間選擇；轉速／輸出有節奏抖動，須補償並記錄偏離 |
| 方案 C 亦不可行 | 退回單一 Log 燃料 | 更大偏離：失去 Plank 雙燃料（REQ-FUEL-01/02），需 Lead 裁決 |
| 啟用程序 | 由 P8 實機證據觸發 → Lead 裁決 → 更新本節與 `REQ_TRACEABILITY.md` → P1/P3 才可改 | 不得由 P1／P3 自行切換 |

### 3.6 spec 通用規則

1. 一個 spec 對應一個 controller 的**建議**綁定（P3 可依實機調整，但 spec 型別名、alias、欄位名不得改）。
2. spec 是**共享唯讀**；每棟可變狀態只能放 controller／`MaintenanceLedger`（需求 §13.3 的理由）。
3. spec 欄位值由藍圖注入；程式碼不得硬寫平衡值（§10）。
4. 新增欄位＝藍圖 schema 變更：必須 Lead 同意＋更新本節＋生成器同步；既有欄位不得改名或改語意。
5. 預設值僅供缺欄位時使用；24 份藍圖必須明寫全部欄位（P6 validator 檢查）。

---

## 4. 藍圖契約（24 檔）

### 4.1 路徑、檔名與 24 檔清單（PLAN §4.5 裁決 D-9）

| 項目 | 契約值 |
|---|---|
| 成品路徑 | `Blueprints/Buildings/Power/SteamBoiler/SteamBoiler.<Size>.<Tier>.<Faction>.blueprint.json` |
| 生成物路徑 | `mod/data/Buildings/Power/SteamBoiler/...`（由 `build.ps1` stage 到成品根） |
| 原始碼路徑 | `mod/data/engines.json`（唯一數值源）→ `tools/generate-blueprints` 產生 |
| 份數 | 24＝12（Size×Tier）×2（Faction） |
| TemplateCollection | `TemplateCollections/TemplateCollection.Buildings.{Folktails,IronTeeth}.blueprint.json`（照原版同名，`Blueprints#append`） |
| 禁止 | 不得覆寫 `Blueprints/Buildings/Power/SteamEngine/**`（原版）；不得改原版 `${Faction}` 的 collection 內容 |

**D-9 理由**：需求 §11.2 寫 `Blueprints/Buildings/...`，簡報 §2 寫 `Buildings/Power/...`；模組包內以簡報路徑為準
（與原版 SteamEngine 同層），生成物與成品路徑分離，避免「生成物被遊戲直接載入」的半套狀態。

### 4.2 權威 12 檔規格表（SHARED_BRIEF §3；生成器唯一輸入）

| # | Blueprint（省略 `.Faction`） | 尺寸 | 占地 X×Y | 高 Z | hp | 研究點 | 建造費 | 燃料 Log/h | 水/h | 燃料庫存 | 水艙 |
|---:|---|---|---|---:|---:|---:|---|---:|---:|---:|---:|
| 1 | `SteamBoiler.Small.T1` | 小型 | 2×2 | 3 | 130 | 90 | Log 20 | 0.04 | 0.10 | 1 | 20 |
| 2 | `SteamBoiler.Medium.T1` | 中型 | 3×3 | 3 | 230 | 150 | Log 30 | 0.07 | 0.18 | 2 | 36 |
| 3 | `SteamBoiler.Large.T1` | 大型 | 4×4 | 4 | 360 | 225 | Log 45 | 0.12 | 0.29 | 3 | 58 |
| 4 | `SteamBoiler.Small.T2` | 小型 | 2×2 | 3 | 260 | 240 | Plank 25＋Log 5 | 0.07 | 0.18 | 2 | 36 |
| 5 | `SteamBoiler.Medium.T2` | 中型 | 3×3 | 3 | 460 | 400 | Plank 40＋Log 10 | 0.12 | 0.32 | 3 | 64 |
| 6 | `SteamBoiler.Large.T2` | 大型 | 4×4 | 4 | 720 | 600 | Plank 60＋Log 15 | 0.18 | 0.50 | 5 | 100 |
| 7 | `SteamBoiler.Small.T3` | 小型 | 2×2 | 3 | 380 | 420 | MetalBlock 30＋Plank 10 | 0.08 | 0.15 | 2 | 30 |
| 8 | `SteamBoiler.Medium.T3` | 中型 | 3×3 | 3 | 680 | 700 | MetalBlock 50＋Plank 20 | 0.14 | 0.27 | 4 | 54 |
| 9 | `SteamBoiler.Large.T3` | 大型 | 4×4 | 4 | 1060 | 1050 | MetalBlock 75＋Plank 30 | 0.22 | 0.42 | 6 | 84 |
| 10 | `SteamBoiler.Small.T4` | 小型 | 2×2 | 3 | 520 | 900 | MetalBlock 45 | 0.08 | 0.09 | 2 | 18 |
| 11 | `SteamBoiler.Medium.T4` | 中型 | 3×3 | 3 | 920 | 1500 | MetalBlock 70 | 0.15 | 0.17 | 4 | 34 |
| 12 | `SteamBoiler.Large.T4` | 大型 | 4×4 | 4 | 1500 | 2250 | MetalBlock 105 | 0.24 | 0.27 | 6 | 54 |

> 表列為**基礎值（未乘陣營）**；研究點逐列加總 **8,525**。表中數值均為初版可調（需求 §20），但**任何調整
> 只能改 `engines.json` 並重跑生成器＋判準**（PLAN §2.2、REQ-RISK-01）。

**派生與陣營規則（凍結）**

| 項目 | 規則 | 例 |
|---|---|---|
| 燃料率 | `round(ratedHorsepower/100 × tierFuelRate, 2)`；`T1 0.032／T2 0.025／T3 0.021／T4 0.016` | Medium.T2：460/100×0.025=0.115→0.12 |
| 水耗率 | `round(ratedHorsepower/100 × tierWaterRate, 2)`；`T1 0.080／T2 0.070／T3 0.040／T4 0.018` | Medium.T2：0.161→0.16？**不**：表定 0.32 → 以 §4.2 表為準，生成器逐格驗算 |
| 水艙 | `round(waterPerHour × 200)`（以 2 位小數水耗率算） | Large.T1：0.29×200=58 |
| 燃料庫存 | 表定值為權威（兩陣營相同）；容量由 `GoodPerHour` 反推生成（§3.5.2 最終公式：T1/T2 `(fuelStock−0.5)/25`；T3/T4 每項 `(fuelStock/2−0.5)/25`） | Medium.T2：fuelStock 3、容量速率 0.10 |
| 陣營輸出 | `ceil(ratedHorsepower × factionOutput)`；FT 1.00／IT 1.05，**無條件進位** | Medium.T2 IT：ceil(483)=483 |
| 陣營燃料 | `round(fuelPerHour × factionFuel, 2)`；FT 0.90／IT 1.00 | Medium.T2 FT：0.12×0.90=0.108→0.11 |
| 其餘 | 水耗、水艙、燃料庫存、研究點、建造費、維護間隔／材料**兩陣營完全相同** | REQ-BLD-12 |
| 2 位小數塌陷 | 低耗量檔陣營燃料差異可能消失（Small.T1 FT 0.04×0.90=0.036→0.04＝IT）；生成器照做，`engines.derived.json` 標 `factionFuelCollapsed: true`，**不得改 3 位小數**；此僅影響實際 `FuelPerHour` 與平衡報告，**不影響容量**（容量由表定 `fuelStock` 反推，AD-3） | PLAN §4.5 裁決 D-6（容量面已由 AD-3 收斂） |

### 4.3 必要 spec 清單（24 份藍圖必備，凍結）

| # | Spec（JSON 頂層 key） | 必要 | 凍結值／規則 | 來源 |
|---:|---|---|---|---|
| 1 | `BuildingSpec` | ✅ | `BuildingCost`＝§4.2 建造費、`ScienceCost`＝§4.2、`PlaceFinished=false`、`FinishableWithBeaversOnSite=false`、`DrawRangeBoundsOnIt=false` | 原版模板 |
| 2 | `BuildingModelSpec` | ✅ | `FinishedModelName="#Finished"`、`UnfinishedModelName="#Unfinished"`、`ConstructionModeModel="Finished"` | 原版模板 |
| 3 | `TemplateSpec` | ✅ | `TemplateName`＝§2.7 | 原版模板 |
| 4 | `BlockObjectSpec` | ✅ | `Size{X,Y,Z}` 照 §4.5；`Blocks` 長度＝X*Y*Z；`Entrance.HasEntrance=true`、`Flippable=true` | R-B、R-C |
| 5 | `EnterableSpec` | ✅ | 所有含庫存建築一律帶（否則取水／庫存需求 NRE、模擬中止，R-C） | PLAN §1.6 R-C |
| 6 | `BuildingAccessibleSpec` | ✅ | 入口／搬運可達；維護道路判定依此 | 需求 §4、PLAN §1.5 D-5 |
| 7 | `PlaceableBlockObjectSpec` | ✅ | `ToolGroupId="Power"`、`ToolOrder` 於 **110–142**（排原版蒸汽機 50 之後）、`ToolShape="Square"`、`Layout="Single"`、`CustomPivot.HasCustomPivot=false` | 需求 §10.10、PLAN P1 |
| 8 | `LabeledEntitySpec` | ✅ | `DisplayNameLocKey`／`DescriptionLocKey`／`FlavorDescriptionLocKey` 照 §5.2；`Icon` 不含副檔名 | 需求 §15.5 |
| 9 | `ConstructionSiteProgressVisualizerSpec` | ✅ | `ProgressThresholds=[0.0]` | 原版模板 |
| 10 | `MechanicalNodeSpec` | ✅ | `PowerOutput`＝`SEEBoilerSpec.Horsepower`（已含陣營）、`PowerInput=0`、`IsShaft=false` | 需求 §8、REQ-BLD-05 |
| 11 | `MechanicalBuildingSpec` | ✅ | `{}`（空物件）；**不得掛 `GoodPoweredGeneratorSpec`**（AD-9，見 §4.4） | 原版模板、AD-9 |
| 12 | `TransputProviderSpec` | ✅ | 左／右／後三側面＋底面傳輸點；`IgnoreRotation=true`；**不得宣告 Up／Top transput** | 需求 §8.9、doc-extract §3.9 |
| 13 | `GoodConsumingBuildingSpec` | ✅ | `FullInventoryWorkHours=25`；`ConsumedGoods` **只列 Log（T1/T2）或 Log＋Plank（T3/T4）**；`GoodPerHour` **只作容量派生**、生成公式見 §3.5.2（**不是實際消耗率**）；**不得列 Water**（水走獨立 `WaterTank` 庫存，R-I） | 需求 §13.4 D-10、AD-3 |
| 14 | 水艙（程式建立，不在 JSON） | ✅ | `SEEWaterInventoryInitializer`（`IDedicatedDecoratorInitializer<SEEBoilerSpec, Inventory>`）建立 `WaterTank` public input `Inventory`：容量＝`WaterCapacityGoods`、貨物 `Water` giveable-and-takeable；**不得**用 `StockpileSpec`（雙 `NoHaulingPostStatus` 崩潰） | 需求 §7、REQ-FUEL-11、2026-11-08 修根因 |
| 15 | `SEE.SEEBoilerSpec` | ✅ | §3.2 全欄位 | §3 |
| 16 | `SEE.SEEMaintenanceSpec` | ✅ | §3.3 全欄位 | §3 |
| 17 | `Children` | ✅ | `#Finished`（`TimbermeshSpec.Model`＋`CollidersSpec`）／`#Unfinished`（`ConstructionBase<XxZ>#nested`＋`ConstructionStage0`），見 §4.8 | 原版模板、template-reuse §3.3 |

> **重要（AD-9）**：原「第 12 項 `GoodPoweredGeneratorSpec`」已由 Lead 裁決**移除**，並改列 §4.4 禁止項。
> 原版 `PowerGenerationConfigurator` 會把 `GoodPoweredGenerator` 裝到任何掛該 spec 的建築上，其 `UpdateState` 每 tick
> 覆寫 `node.SetOutputMultiplier(...)` 並依 `Graph.RequiresPower` 自動 `PauseConsumption()/ResumeConsumption()`，
> 會覆寫我方 λ 且與燃料 toggle 互搶；控制器完整接管輸出倍率＋toggle＋零需求邏輯（見 §8.4）。
> 證據：`_investigate/findings-power.md` §1.5／§4.1、`_investigate/out2/power/il-goodpoweredgen.txt`、
> `_investigate/out2/power/il-powergen-configurator.txt`。

**選配 spec（可照原版蒸汽機模板帶入，非契約必要；IL 已證只依賴 `MechanicalNode`）**：
`GoodPoweredGeneratorAnimatorSpec`、`PowerGeneratorParticleControllerSpec`（3 個煙霧掛點）、
`PowerGeneratorSoundsSpec`、`MechanicalNodeIlluminatorSpec`、`TemplateAttachmentsSpec`、
`ConstructionSiteProgressVisualizerSpec`（已列必要第 9 項）、`ConstructionSiteBuildTimeSpec`（若 P1 需要施工時間）。
**`GoodPoweredGeneratorSpec` 不在選配內：一律禁止（AD-9，見 §4.4）**；動畫／煙霧是否可用待 P8 實機確認（P9 前可先關）。
來源：PLAN §1.4 第 1 條、template-reuse §3.2、`_investigate/findings-power.md` §1.5／§4.1。

### 4.4 禁止 spec／元素（紅線；P6 靜態掃描）

| 禁止項 | 原因 | 依據 |
|---|---|---|
| `BuildingPipeSpec`／`BuildingPipeTargetPortsSpec` | 會讓建築變成管道節點，反而不會被補水 | REQ-COMPAT-07、SHARED_BRIEF §7 |
| `PowerLineSpec` | 主藍圖不得含；只在 P5 偵測到 PL 後**執行期條件式注入** | REQ-POWER-16、REQ-COMPAT-11 |
| `WorkplaceSpec`／`WorkplaceSlotManagerSpec` | 本模組免工人；誤加會要求工人站位 | REQ-BLD-04、REQ-CORE-02 |
| Up／Top transput | 會與煙囪／上方空間衝突；軸向錯誤是已知陷阱 R-B | doc-extract §3.9 |
| `GoodConsumingBuildingSpec` 列 `Water` | 與水艙混裝、重複扣料、失去獨立搬運目標（R-I；2026-11-08 修根因後列為硬錯誤） | PLAN §1.6 R-I |
| `StockpileSpec`（含液體水艙） | Stockpile＋GCB 同棟會各加一個 `NoHaulingPostStatus` 初始化器，完工時崩潰；水艙改用程式建立的 public input 庫存 | Player.log 2026-11-07、2026-11-08 修根因 |
| `GoodPoweredGeneratorSpec` | 原版 `PowerGenerationConfigurator` 會掛 `GoodPoweredGenerator`，其 `UpdateState` **每 tick** 執行 `node.SetOutputMultiplier(IsConsuming ? 1 : 0)` 並依 `Graph.RequiresPower` 自動 `PauseConsumption()/ResumeConsumption()` → 覆寫我方 λ、與我方燃料 toggle 互搶（AD-9） | `_investigate/findings-power.md` §1.5／§4.1；`_investigate/out2/power/il-goodpoweredgen.txt`、`il-powergen-configurator.txt` |
| `PoweredGoodConsumingBuildingSpec` | 原版自動消耗／恢復元件，會與 controller 的「永久 pause＋手動扣料」互搶（AD-3／AD-9） | Lead 2026-11-06 指示；IL 待補 |
| 自訂 `InventorySpec`／自訂庫存型別 | `InventorySpec` 不存在；水艙用原版 `StockpileSpec` | PLAN §1.4 第 10 條 |
| 覆寫原版藍圖路徑／loc key | 需求紅線 | SHARED_BRIEF §2 |

### 4.5 軸序、Blocks 與碰撞（R-B，必讀）

| 規則 | 內容 |
|---|---|
| 軸序 | `BlockObjectSpec.Size` 的 **X＝寬、Y＝深、Z＝高**（Timberborn 是 Z-up） |
| 12 檔的 Size | 小型 `{X:2,Y:2,Z:3}`；中型 `{X:3,Y:3,Z:3}`；大型 `{X:4,Y:4,Z:4}` |
| Blocks | 長度必須＝`X*Y*Z`；row-major：`n → (x=n%X, y=(n/X)%Y, z=n/(X*Y))` |
| 佈局 | 入口那一排實心（`MatterBelow:"GroundOrStackable"`、`Occupations:"Bottom, Top, Corners, Path, Middle"`）；內部 `MatterBelow:"Any"`＋`Occupations:"Floor, Bottom, Corners, Path, Middle"`；懸空層一律 `Any`；**不得整塊 footprint 全實心** |
| 入口 | `Entrance.Coordinates` 的 Y＝**-1**（前方門面外一格） |
| ConstructionBase | `ConstructionBase<XxZ>` 的兩數＝**X 寬 × Z 高**（不是占地 X×Y） |
| 碰撞盒 | `CollidersSpec` 的 Size 需與引擎的 (寬, 高, 深) 對應關係一致；P1 首份藍圖先以 validator＋單棟實機確認佔地 |

來源：PLAN §1.6 R-B、template-reuse §3.9、需求 §4。

### 4.6 模型與陣營綁定（R-A，必讀）

| 規則 | 內容 |
|---|---|
| 一檔一陣營 | 24 份藍圖各只屬於一個陣營；`TemplateCollection` 只 append 該陣營的 12 檔 |
| 材質是陣營綁定 | 誤用其他陣營材質會在建預覽時 `ArgumentException: Material … not found`，**整個建造選單載入失敗**（不是單棟） |
| FT 藍圖 | **只能**引用 Folktails 模型／中立（`MaterialCollection.Common`）材質；v1 可暫用 Folktails 既有模型（如水車類同尺寸模型） |
| IT 藍圖 | 可引用原版 `Buildings/Power/SteamEngine/SteamEngine.IronTeeth.Model` |
| P9 之後 | 自製中立材質／模型通過驗證後才可統一；P9 **不得改藍圖數值** |
| 不得 | 打包原版資產、複製原版材質到自製模型、一檔引用兩陣營模型 |

來源：PLAN §1.6 R-A、template-reuse §3.4、SHARED_BRIEF §3（外觀與陣營）。FT 模型策略列 §11.2 AD-2 待裁決。

### 4.7 庫存雙軌（D-10 方案 A＋AD-3 裁決）

| 軌 | 機制 | 契約 |
|---|---|---|
| 燃料庫存 | 原版 `GoodConsumingBuildingSpec`（保留原版搬運／輸送帶／`EnterableSpec` 物流） | `FullInventoryWorkHours=25`；`ConsumedGoods` 只列 Log（T1/T2）或 Log＋Plank（T3/T4）；**`GoodPerHour` 只作容量派生**（§3.5.2 最終公式，不等於實際消耗率）；`InputGoods` 納入 Log／Plank 供輸送帶餵料（REQ-FUEL-16） |
| 燃料消耗 | **原版消耗永久暫停**：`SEEBoilerController` 完成時呼叫 `GetGoodConsumingToggle().PauseConsumption()`；由 controller 以 `Inventory.TakeConsumed` 結算 `SEEBoilerSpec.FuelPerHour × λ` | AD-3 裁決；能量換算 Log=1.0／Plank=2.0；先 Log 後 Plank（1 Plank 抵 2 Log 能量）；`fuelScaling="vanilla"` 也由 controller 以 λ=1 實現，不恢復原版線性消耗 |
| 水 | 我方 `SEEBoilerSpec`＋液體 `StockpileSpec`（容量 200 工時） | 由 controller 扣水；浮點餘量累積、滿 1 貨物才扣庫存；餘量不存檔（REQ-IMPL-06） |
| 水艙貨物允許（AD-10） | `SEEBoilerController` 在初始化／完成時執行：`var allower = GetComponent<SingleGoodAllower>(); if (!allower.HasAllowedGood) allower.Allow("Water");`；**只在尚未設定時**設定，**不覆寫玩家選擇**（玩家選 Badwater 時引擎顯示 `NoWater`，屬預期） | IL：`StockpileInventoryInitializer.Initialize` 只呼叫 `AddAllowedGoodType(spec.WhitelistedGoodType)`＋`AddGoodDisallower(SingleGoodAllower)`；`SingleGoodAllower.AllowedAmount(good)` 在 `AllowedGood == null` 時對任何貨物回 0 → **不做 `Allow` 則水艙永遠收不到水**（搬運工／管網全被擋）。`Allow`／`HasAllowedGood` 皆 public（已 IL 證實） |
| 燃料庫存貨物允許（AD-10 補充） | `GoodConsumingBuildingSpec` 的 allowed goods 為 `Givable`（非 takeable），搬運工走 `FillInput` 路徑；**不需** `SingleGoodAllower` | `_investigate/findings-inventory-maintenance.md` §2.2／§6 |
| 消耗對帳（AD-10 補充） | 原版消耗永久暫停後 `GoodConsumingBuilding._suppliesLeft` **不會累積**；燃料對帳只算 **Inventory 整數差值** vs `FuelPerHour×24`，不需緩衝項 | `_investigate/findings-inventory-maintenance.md` §6 |
| 消耗對帳 | 連續 1 遊戲日：實際扣除量 vs 表定 ≤ **±2%**；須分別涵蓋燃料（Log-only／Plank-fallback 兩路徑）與水 | REQ-IMPL-04；T2 門檻（§1.4）；P8 實機 gate |
| 重複扣料 | 原版消耗已永久 pause；若實機仍重複扣料或 pause 無效 → 走 §3.5.3 備援（方案 C／單一 Log），**不得自行改契約** | AD-3；PLAN §4.5 D-10 |
| 混裝 | 水艙／燃料庫存比 **≥8×**；混裝體積誤差上界 **≤12%** | REQ-COMPAT-04/05；生成器閘門 |
| 能量換算 | `FuelGoodIds`／`FuelValues` 由 spec 提供；程式不得硬寫 `Plank=2` | REQ-IMPL-05；需求 §13.4 E/value |

### 4.8 `Children` 結構（固定三層；template-reuse §3.3）

```jsonc
"Children": {
  "#Finished": {
    "TimbermeshSpec": { "Model": "<成品模型路徑，不含 .timbermesh>" },
    "CollidersSpec": { "BoxColliders": [ { "Size": {…}, "Center": {…} } ] }
  },
  "#Unfinished": {
    "Children": {
      "ConstructionBase<XxZ>#nested": {
        "BlueprintPath": "ConstructionBases/ConstructionBase<X>x<Z>/ConstructionBase<X>x<Z>.blueprint"
      },
      "ConstructionStage0": {
        "TimbermeshSpec": { "Model": "<施工階段模型路徑>" },
        "CollidersSpec": { … }
      }
    }
  }
}
```

- `ConstructionBase` 若無完全對應尺寸，改用最接近且可包覆者並在 `VERIFICATION.md` 記錄（沿用姊妹作做法）。
- `#FinishedUncovered` 只在需要「無屋頂」變體時才寫；本模組預設不寫。
- 模型路徑是**遊戲資源路徑字串**，可指向 mod 內或原版；路徑不得含副檔名。

### 4.9 TemplateCollection 與工具列

| 項目 | 契約 |
|---|---|
| 檔案 | `TemplateCollections/TemplateCollection.Buildings.Folktails.blueprint.json`、`.IronTeeth.blueprint.json`（與原版同名，靠 `Blueprints#append` 修改原版 collection，官方跨 mod 機制） |
| 內容 | 各 append 該陣營 12 檔路徑（`Buildings/Power/SteamBoiler/…`） |
| 一檔一陣營 | 不得把 IT 藍圖 append 到 FT collection（反之亦然） |
| 工具組 | 沿用原版 `Power`；**不新增工具群組**；`ToolOrder` 110–142 且排在原版蒸汽機（50）之後、跨陣營一致 |
| 顯示名 | 見 §5.2 A 段（「小型蒸汽引擎 Mk.I」…「大型蒸汽引擎 Mk.IV」） |

### 4.10 生成器流程與閘門（凍結；PLAN §7.4、doc-extract §3.3）

`tools/generate-blueprints` 單一命令（7 步）：

1. 載入 `engines.json` 並做結構驗證（欄位齊全、型別、列舉、12 筆 `(Size,Tier)` 不重複）。
2. 派生驗算（燃料率、水耗率、水艙、燃料庫存），不直接寫值。
3. 陣營展開（輸出 ceil、燃料 round；容量／成本／研究點不變）。
4. 產生 24 份藍圖（路徑與 TemplateName 照 §4.1／§2.7）。
5. 五道閘門（任一失敗即中止，不產生半套）：水艙／燃料庫存 ≥8×；混裝誤差 ≤12%；每 100hp 燃料與水 T2–T4 ≤ 原版
   （0.025 Log/h、0.075 Water/h）、T1 燃料 ≤T2×1.3；`fuelStock`／`waterTankGoods` 與表定逐格相等（`ConsumedGoods.GoodPerHour`
   依 §3.5.2 最終公式生成，並以 float32 模擬 `CeilToInt` 驗算容量）；藍圖不含 `BuildingPipeSpec`／`PowerLineSpec`。
6. 輸出黃金檔：`SEE.Tests/Golden/blueprints.sha256`（24 檔雜湊）與 `engines.derived.json`（派生對帳表）。
7. `--check` 模式：只驗證不寫檔；CI 雜湊不符即 FAIL。

硬規則：24 份藍圖**不得手改**（REQ-ARCH-14 的「GENERATED, DO NOT EDIT」形式待 P1 定案：JSON 不支援註解，
候選為同目錄 README／黃金雜湊；列 §11.2 AD-7）。

### 4.11 原型藍圖與 icon（P0／P1 先行）

| 項目 | 契約 |
|---|---|
| 原型用途 | P0 取證 1 份（1 棟可建可運轉）；P1B 先行可玩包可含 12 檔但可暫代 spec |
| icon（v1 佔位） | 沿用原版 icon 路徑（如 `Buildings/Power/SteamEngine/SteamEngineIcon`）是否可接受 → **待裁決**（§11.2 AD-1）；P9 之前可暫代，但必須在 `VERIFICATION.md` 標「暫代」 |
| 正式 icon | `data/Icons/<Name>.png`（128×128）＋`<Name>.png.meta.json`（內容 `{"isSprite": true}`）；`LabeledEntitySpec.Icon` 不含副檔名 |
| icon 數量 | 24 個（12 檔×2 陣營）或 12 個共用（P9 定案）；**不得**含文字 |

---

## 5. 本地化契約（54 基準，P4B 擴充）

### 5.1 檔案、格式與編碼（凍結）

| 項目 | 契約 |
|---|---|
| 檔案 | `mod/data/Localizations/enUS.csv`、`zhTW.csv` **必要**；`zhCN.csv` 選配（不得缺席 enUS／zhTW） |
| 格式 | **3 欄** `ID,Text,Comment`，含 header；每行恰好 3 欄；`Text` 含逗號必須用雙引號包住、值內不得換行 |
| 編碼 | UTF-8；以原版與姊妹作實機成品為準為**無 BOM**（原版 `enUS.csv` 前三 bytes＝`49 44 2C`＝`ID,`）；需求 §15.5 L1 的「key,text 單行」解讀為「每鍵一行、值不換行」 |
| 一致性 | 三語 key 集合完全相同、placeholder 相同；`zhTW` 不得含簡體字、不得回退 `zhCN`；缺鍵一律 fallback **enUS** |
| 產生 | A 段 12 鍵由生成器產生（`DisplayName`）；B／C／D 段寫在 data 檔；**不得手抄**、不得出現 raw key |

來源：需求 §15.5 L1–L6、doc-extract §5.6、template-reuse §3.7、`feasibility.md` §1.3。

### 5.2 63 鍵清單（A 12／B 5／C 22／D 15＋D2 9；需求 §15.5 逐字＋P4B 擴充）

#### A. 顯示名（12 鍵，由生成器產生）

| Key | zhTW | enUS |
|---|---|---|
| `Building.SEE.SteamBoiler.Small.T1.DisplayName` | 小型蒸汽引擎 Mk.I | Small Steam Engine Mk.I |
| `Building.SEE.SteamBoiler.Medium.T1.DisplayName` | 中型蒸汽引擎 Mk.I | Medium Steam Engine Mk.I |
| `Building.SEE.SteamBoiler.Large.T1.DisplayName` | 大型蒸汽引擎 Mk.I | Large Steam Engine Mk.I |
| `Building.SEE.SteamBoiler.Small.T2.DisplayName` | 小型蒸汽引擎 Mk.II | Small Steam Engine Mk.II |
| `Building.SEE.SteamBoiler.Medium.T2.DisplayName` | 中型蒸汽引擎 Mk.II | Medium Steam Engine Mk.II |
| `Building.SEE.SteamBoiler.Large.T2.DisplayName` | 大型蒸汽引擎 Mk.II | Large Steam Engine Mk.II |
| `Building.SEE.SteamBoiler.Small.T3.DisplayName` | 小型蒸汽引擎 Mk.III | Small Steam Engine Mk.III |
| `Building.SEE.SteamBoiler.Medium.T3.DisplayName` | 中型蒸汽引擎 Mk.III | Medium Steam Engine Mk.III |
| `Building.SEE.SteamBoiler.Large.T3.DisplayName` | 大型蒸汽引擎 Mk.III | Large Steam Engine Mk.III |
| `Building.SEE.SteamBoiler.Small.T4.DisplayName` | 小型蒸汽引擎 Mk.IV | Small Steam Engine Mk.IV |
| `Building.SEE.SteamBoiler.Medium.T4.DisplayName` | 中型蒸汽引擎 Mk.IV | Medium Steam Engine Mk.IV |
| `Building.SEE.SteamBoiler.Large.T4.DisplayName` | 大型蒸汽引擎 Mk.IV | Large Steam Engine Mk.IV |

#### B. 文案（5 鍵）

| Key | zhTW | enUS |
|---|---|---|
| `Building.SEE.SteamBoiler.Description` | 消耗燃料與水產生動能。需定期維護。 | Consumes fuel and water to produce power. Requires periodic maintenance. |
| `Building.SEE.SteamBoiler.Flavor.T1` | 謙卑的小鍋爐，只要一點木頭和一點水。 | A humble boiler that asks for a little wood and a little water. |
| `Building.SEE.SteamBoiler.Flavor.T2` | 壓力、木材與黃銅——現在還多了壓力錶。 | Pressure, timber and brass—now with a gauge. |
| `Building.SEE.SteamBoiler.Flavor.T3` | 鑄鐵低鳴，鉚釘撐起動能的承諾。 | Cast iron rumbles; rivets hold the promise of power. |
| `Building.SEE.SteamBoiler.Flavor.T4` | 每個閥門都精準，每一轉都有代價。 | Precision in every valve, and a price in every turn. |

#### C. 狀態（22 鍵）

| # | Key | zhTW | enUS |
|---:|---|---|---|
| 13 | `Status.SEE.Running` | 運轉中 | Running |
| 14 | `Status.SEE.Idle` | 待機（無負載） | Idle (no demand) |
| 15 | `Status.SEE.Paused` | 已暫停 | Paused |
| 16 | `Status.SEE.NoFuel` | 缺燃料 | No fuel |
| 17 | `Status.SEE.NoWater` | 缺水 | No water |
| 18 | `Status.SEE.NeedsMaintenance` | 需要維護 | Needs maintenance |
| 19 | `Status.SEE.Critical` | 機況危急：輸出 ×0.40 | Condition critical: output ×0.40 |
| 20 | `Status.SEE.Worn` | 機件磨損：輸出 ×0.85 | Worn: output ×0.85 |
| 21 | `Status.SEE.MaintenanceDue` | 保養到期：輸出 ×0.93 | Maintenance due: output ×0.93 |
| 22 | `Status.SEE.Maintaining` | 維護中（剩 {0}） | Maintaining ({0} left) |
| 23 | `Status.SEE.MissingMaintenanceMaterials` | 缺少維護材料 | Missing maintenance materials |
| 24 | `Status.SEE.NoAccessForMaintenance` | 無道路可達，無法維護 | No access for maintenance |
| 25 | `Status.SEE.LowFuel` | 燃料將盡（剩 {0} 小時） | Fuel low ({0} h left) |
| 26 | `Status.SEE.LowWater` | 水將盡（剩 {0} 小時） | Water low ({0} h left) |
| 27 | `Status.SEE.PipeSupply` | 管路供水 | Water supplied by pipes |
| 28 | `Status.SEE.BeltSupply` | 燃料由輸送帶供應 | Fuel supplied by conveyor |
| 29 | `Status.SEE.BeltNoPower` | 輸送帶斷電 | Conveyor has no power |
| 30 | `Status.SEE.PipeContaminated` | 管網受汙染 | Pipe network contaminated |
| 31 | `Status.SEE.WaterMode.Tank` | 供水：水艙（管路） | Water supply: tank (pipes) |
| 32 | `Status.SEE.WaterMode.Valve` | 供水：閥門 | Water supply: valve |
| 33 | `Status.SEE.WaterMode.Hauler` | 供水：搬運工 | Water supply: haulers |
| 34 | `Status.SEE.WaterMode.Auto` | 供水：自動 | Water supply: auto |

#### D. 面板與操作（15 鍵）

| # | Key | zhTW | enUS |
|---:|---|---|---|
| 35 | `UI.SEE.Panel.Power` | 動力 | Power |
| 36 | `UI.SEE.Panel.Network` | 電網供給／需求：{0}／{1} | Network power supply / demand: {0} / {1} |
| 37 | `UI.SEE.Panel.Output` | 輸出：{0}／{1} 馬力 | Output: {0} / {1} hp |
| 38 | `UI.SEE.Panel.Fuel` | 燃料 | Fuel |
| 39 | `UI.SEE.Panel.Water` | 水 | Water |
| 40 | `UI.SEE.Panel.Condition` | 機況：{0}／100 | Condition: {0} / 100 |
| 41 | `UI.SEE.Panel.NextMaintenance` | 下次維護：{0} 日後 | Next maintenance in {0} days |
| 42 | `UI.SEE.Panel.Materials` | 維護材料：{0} | Maintenance materials: {0} |
| 43 | `UI.SEE.Panel.WaterSupplyMode` | 供水模式 | Water supply mode |
| 44 | `UI.SEE.Action.Pause` | 暫停 | Pause |
| 45 | `UI.SEE.Action.Resume` | 繼續 | Resume |
| 46 | `UI.SEE.Action.MaintainNow` | 立即維護 | Maintain now |
| 47 | `UI.SEE.Action.AutoMaintenance` | 自動維護 | Auto maintenance |
| 48 | `UI.SEE.Error.MaterialsMissing` | 無法維護：缺少 {0} | Cannot maintain: {0} missing |
| 49 | `UI.SEE.Error.BeltNoPower` | 輸送帶網路沒有電力 | Conveyor network has no power |

#### D2. P4B 新增（9 鍵）

| # | Key | zhTW | enUS |
|---:|---|---|---|
| 50 | `UI.SEE.Panel.WaterBeaverEquivalent` | 用水量：約 {0} 隻海狸的每日飲水 | Water use: ≈{0} beavers' drinking water per day |
| 51 | `UI.SEE.Panel.Efficiency` | 電網效率：{0}% | Network efficiency: {0}% |
| 52 | `UI.SEE.Panel.MaterialsDetail` | 維護材料：{0}（缺 {1} 件） | Maintenance materials: {0} (missing {1}) |
| 53 | `UI.SEE.Panel.MaterialEntry` | {0}：{1}／{2} | {0}: {1}/{2} |
| 54 | `UI.SEE.Panel.NextMaintenanceDay` | 下次維護：第 {0} 日 | Next maintenance: day {0} |
| 55 | `UI.SEE.Hint.FixNoFuel` | 補充燃料（原木或木板）或接通輸送帶。 | Refill fuel (logs or planks) or connect a conveyor. |
| 56 | `UI.SEE.Hint.FixNoWater` | 補滿水艙或接上管路。 | Fill the water tank or connect a pipe. |
| 57 | `UI.SEE.Hint.FixNeedsMaintenance` | 進行維護：確保地區內備有維護材料。 | Run maintenance: keep the required materials in the district. |
| 58 | `UI.SEE.WaterModeUnavailable` | 所選供水模式無法使用：{0} | Selected water mode unavailable: {0} |

**清點核對**：12（A）＋5（B）＋22（C）＋24（D）＝ **63 鍵** ✓（P4B 擴充；AC-16 只要求 ≥54，54 為需求基準）。
B 段 5 鍵未編號，A／C／D 共 58 個編號鍵。

> 補充（SHARED_BRIEF §13 末）：低燃料提示統一為「剩餘燃料 <8 遊戲小時的消耗量」，鍵名 `Status.SEE.LowFuel`；
> 廢除舊名 `UI.SEE.FuelLow`。供水模式四鍵（Auto／Tank／Valve／Hauler）必須四種齊全。

### 5.3 字元上限與用語（L1–L6）

| # | 規則 | 內容 |
|---:|---|---|
| L1 | 檔案 | `Localizations/` 下 `enUS.csv`、`zhTW.csv`（選配 `zhCN.csv`）；UTF-8；每鍵一行、值不得換行 |
| L2 | 佔位符 | 只用 `{0}`、`{1}`，由程式代入並套用遊戲數字格式；字串內不得寫死數值 |
| L3 | 字元上限 | 顯示名 ≤18 zh／30 en；狀態 ≤12／22；按鈕 ≤8／16；Tooltip ≤80／160；超限即驗收失敗 |
| L4 | 用語 | 動能／馬力／馬力小時沿原版；原木、木板、金屬塊、齒輪、水沿官方物品名；**zhTW 不得出現簡體字** |
| L5 | Fallback | 缺鍵顯示 enUS，不得顯示空字串或 key 名；`zhTW` 不得回退 `zhCN` |
| L6 | 不可覆寫 | 不得新增／修改任何 `Building.SteamEngine.*` 等原版鍵（REQ-UI-08） |

### 5.4 驗證規則（P6 門禁）

1. 三語 key 集合完全相同（若出 zhCN）；≥54 鍵齊全（目前 63）、無多餘 key、無 raw key。
2. placeholder 集合一致；`{0}`／`{1}` 數量與 UI 呼叫一致。
3. 字元上限逐鍵檢查（L3）；`zhTW` 簡體字掃描＝0；原版鍵未被覆寫。
4. A 段 12 鍵由生成器產生，不得手改（`--check` 失敗即擋）。
5. enUS ×1.4 假字串不得溢出（REQ-UI-10 的版面測試）。

### 5.5 禁止鍵與命名

- **禁止**：`SteamEngine`、`Building.SteamEngine.*`、`SteamPlant`、`TWSteamEngine`、分叉來源舊前綴作為
  TemplateName 或 loc key。
- 前綴只允許：`Building.SEE.*`、`Status.SEE.*`、`UI.SEE.*`（REQ-UI-08）。
- 顯示名用「蒸汽引擎」，刻意與原版 zhTW「蒸汽機」區隔。

---

## 6. 存檔契約（schema v1）

### 6.1 世界節點與必要區塊（**D-6 混合形態**；需求 §14.1）

> **D-6 最終裁決（Lead 2026-11-06；理由與備援見 §6.9）**：**混合形態**——世界節點 `SEE` 只存「索引／版本／
> 校驗／compat」，逐棟可變狀態走**逐實體元件 `ComponentKey("SEE")`**（已實機驗證的路徑）。偏離登記＝AD-11。

世界節點落點：`world.json → "Singletons": { "SEE": { … } }`（`SerializedWorldFactory.Create →
GetSingletons<ISaveableSingleton>() → Save`；存讀用 `SingletonKey("SEE")`＋`ISingletonSaver.GetSingleton`／
`ISingletonLoader.TryGetSingleton`）。

```json
{
  "Singletons": {
    "SEE": {
      "schemaVersion": 1,
      "modVersion": "0.1.0",
      "checksum": "sha256:<hex>",
      "buildings": [ /* 索引，見 §6.2 */ ],
      "compat": { "detected": [], "enabled": [], "failed": [] }
    }
  }
}
```

| 區塊 | 必要 | 型別 | 說明 |
|---|---|---|---|
| `Singletons.SEE` | ✅ | object | 世界節點；`SingletonKey("SEE")`；`[Context("Game")]` Configurator 註冊 `ILoadableSingleton`＋`ISaveableSingleton` |
| `schemaVersion` | ✅ | int | 目前 `1`；**世界節點與 per-entity 元件共用此 schema 版本** |
| `modVersion` | ✅ | string | 寫入時模組版本（診斷用；SemVer `0.1.0`） |
| `checksum` | ✅ | string | `sha256:<hex>`；**只涵蓋世界節點**（去 checksum 後標準化 JSON），**不含 per-entity 狀態**（原版存檔機制自帶完整性） |
| `buildings` | ✅ | array | **只存索引 4 欄位**（`entityId`／`tier`／`size`／`faction`；孤兒偵測與診斷用） |
| `compat` | ✅ | object | 三個字串陣列（§6.3） |

- 世界節點寫入失敗或缺席 → 記 `SEE/Warn`，**狀態仍由 per-entity 完整保存**（降級不丟資料）。
- 世界節點不承載 `condition`／維護狀態／模式／旗標（那些在 §6.2 的逐實體元件）。

### 6.2 索引（世界節點 `buildings[]` 4 欄位）＋狀態（逐實體元件 7 欄位）

**(a) 世界節點索引（孤兒偵測／診斷用；D-8：`entityId` 為 Guid 字串）**

| 欄位 | 型別 | 必填 | 範圍／列舉 | 說明 |
|---|---|---|---|---|
| `entityId` | **string（Guid）** | ✅ | `Guid.ToString("D")` 格式（小寫 hex、含連字號）、全檔唯一 | Guid 是遊戲原生存檔 primitive，JSON 自動寫成字串（IL：`Guid.ToString()`／`Guid.Parse`）→ 支援 D-8；需求 §14.1 的 `long` 視為筆誤 |
| `tier` | string | ✅ | `T1`–`T4` | 等級（診斷／孤兒比對） |
| `size` | string | ✅ | `Small`／`Medium`／`Large` | 尺寸 |
| `faction` | string | ✅ | `Folktails`／`IronTeeth` | 顯示／診斷用；不影響載入正確性 |

**(b) 逐實體元件 `ComponentKey("SEE")`（`SEEBoilerController : IPersistentEntity`；狀態本體）**

| 欄位 | 型別 | 必填 | 範圍／列舉 | 預設 | 說明 |
|---|---|---|---|---|---|
| `condition` | float | ✅ | `[0,100]`；非有限值 → 重置 | `100` | 機況 |
| `autoMaintenance` | bool | ✅ | — | `true` | 自動維護開關 |
| `maintenanceState` | string | ✅ | `None`／`Maintaining`／`MissingMaterials`／`NoAccess` | `None` | 維護狀態（enum 以字串存，新增成員不誤讀舊檔） |
| `maintenanceRemainingHours` | float | ✅ | **閉區間 `[0,2]`**（PLAN §4.5 裁決 D-7）；`Maintaining` 時 >0、其餘為 0 | `0` | 維護倒數 |
| `lastMaintenanceDay` | float | ✅ | ≥0 | `0` | 上次完成維護的遊戲日 |
| `waterSupplyMode` | string | ✅ | `tank`／`valve`／`hauler` | `auto` 解析結果 | **玩家意圖值**（不存 `auto`）；執行期有效值另算 |
| `powerLineAttached` | bool | ✅ | — | `false` | 上次注入成功與否；載入時重算 |

- `tier`／`size`／`faction` **不存 per-entity**（可由藍圖／template 重建）。
- 讀寫規則：`IObjectLoader.Has` 守衛（缺 key `Get` 會 throw `ArgumentOutOfRangeException`）＋逐欄位 try/catch；
  載入 sanitize（`condition` 非有限／越界 → 100 並關閉自動維護；未知 enum → 預設；缺欄位 → 預設）；不因一棟壞掉拒絕整檔。
- **列舉建築的方式**：`EntityRegistry.Entities`（`AsSingleton`）＋`GetComponent<T>()`，或我方自持
  `Dictionary<Guid, SEEBoilerController>` registry；**不得**用 `EntityComponentRegistry.GetAll<T>()` 列舉我方 controller
  （它受 `IRegisteredComponent` 限制，列不到任意元件）。
- 燃料與水的貨物庫存是原版貨物，**不寫入**世界節點或元件（§6.7）。

### 6.3 `compat` 區塊

| 欄位 | 型別 | 必要 | 說明 |
|---|---|---|---|
| `detected` | string[] | ✅ | 本次載入偵測到的模組簡名（診斷快照；例如 `"TimberPipes"`） |
| `enabled` | string[] | ✅ | 實際啟用的適配器 Id（如 `SEE.Adapter.TimberPipes`） |
| `failed` | string[] | ✅ | 失敗原因鍵（只記鍵，不記堆疊） |

- 三個陣列皆為**診斷快照**，不是承諾；任何不一致以本次偵測為準（需求 §14.6）。
- 只記模組簡名／適配器 Id；**不得**記外部模組型別名、組件 Id、接點／管網 id（REQ-COMPAT-13）。
  `compat.detected` 與 REQ-SAVE-09「不得出現外部識別碼」的字面張力，以 §14.1 schema 為準（§11.2 AD-5）。

### 6.4 校驗和（checksum）

| 項目 | 規則 |
|---|---|
| 演算法 | **SHA-256**，輸出 `sha256:<hex>`（小寫 hex） |
| 範圍 | 世界節點 `Singletons.SEE`**去除 `checksum` 欄位後**的標準化 JSON（不含 per-entity 狀態） |
| 標準化 | 鍵依字典序排序；無多餘空白；數字用 invariant culture；**浮點保留 4 位小數**；`buildings` 依 `entityId` 升序 |
| 寫入時機 | 序列化完成、寫入節點前計算一次 |
| 不符時 | 記 `SEE/Warn`（一次）→ **不覆寫原存檔** → 逐棟驗證 → 損壞棟重置（§6.6） |
| 不做 | 不當防作弊、不因此拒絕載入、不用於跨版本比對 |

- **只涵蓋世界節點**：`checksum` 不含 per-entity 狀態（per-entity 元件由原版存檔機制保存，自帶完整性）。
  世界節點 checksum 不符 → 只降級節點（重建索引，記 `SEE/Warn`），**per-entity 狀態不受影響**。

### 6.5 遷移規則（只前進；需求 §14.4）

| 規則 | 內容 |
|---|---|
| 只前進 | 不提供降版遷移器；舊模組讀到新存檔 → 唯讀降級並提示升級 |
| 只加不減 | 遷移器優先新增欄位並給預設值；移除欄位必須升 modVersion MAJOR 並在需求 §21 記錄 |
| 未知欄位保留 | 反序列化為 `JsonNode`（或等價字典）而非強型別 DTO；未知欄位原樣保留 |
| 冪等 | 同一段遷移重複套用結果相同（以 `schemaVersion` 守門） |
| 不可 throw | 任一段失敗 → `null` → 唯讀降級；**不得**清空資料 |
| 可測試 | 每個 `vN → vN+1` 都有一份 `tests/saves/vN.json` 與期望輸出 |
| 版本過高 | `current > target` → `readOnly: true`，不寫回 |
| v1 現況 | **無遷移器**（Chain 為空）；v2 起逐段加入（v2 範例：新增 `lastFuelResupplyDay` 僅為預留，非 v1 承諾） |
| 混合形態 | 世界節點與 per-entity 元件**共用 `schemaVersion`**；升版時兩者同步遷移（節點走 `ISingletonLoader`、元件走 `IObjectLoader`），任一缺欄位用預設 |
| 形態遷移 | 若 P8 證實世界節點時序穩定、要把狀態搬回節點：`schemaVersion+1`＋遷移器（v1→v2）；**不得在 P3／P4 自行改動**（AD-11） |
| 損壞邊界 | 世界節點損壞不影響 per-entity 狀態；per-entity 元件損壞只重置該棟（§6.6） |

### 6.6 損壞隔離（12 類；需求 §14.5）

| 損壞類型 | 偵測 | 處理 | 玩家可見 |
|---|---|---|---|
| 節點型別錯（不是物件） | 解析 | 建空節點（**索引重建；per-entity 狀態不受影響**） | `SEE/Warn`＋面板無資料 |
| `schemaVersion` 缺失 | 解析 | 視為 1（向後相容） | 無 |
| `schemaVersion` 過高 | 版本比對 | 唯讀降級：可玩、不可寫回、提示升級 | 面板提示「存檔版本較新」 |
| 校驗和不符 | 雜湊比對 | **世界節點**重建索引（不整檔拒絕；per-entity 不受影響） | `SEE/Warn` |
| `buildings` 缺失 | 解析 | 視為空陣列 | 無 |
| 單棟缺欄位 | 逐欄位驗證 | 套預設（`condition=100`、`autoMaintenance=false`、`maintenanceState=None`） | `SEE/Warn` |
| 單棟型別錯／越界／`NaN` | 逐欄位驗證 | 重置該棟（`condition=100`、關閉自動維護、`maintenanceState=None`） | 該棟顯示「資料已修復」 |
| `entityId` 找不到建築 | 載入後比對 | 保留一版供除錯，下次存檔丟棄 | 無 |
| `entityId` 重複 | 建快取時 | 保留第一筆，其餘丟棄並記 `SEE/Warn` | 無 |
| `maintenanceState` 未知值 | 列舉驗證 | 視為 `None` | 無 |
| `maintenanceRemainingHours` 越界 | 範圍驗證 | 夾到 `[0,2]`；非 `Maintaining` 時歸零 | 無 |
| `waterSupplyMode` 非法值 | 列舉驗證 | 以 `auto` 重新解析 | 面板顯示實際模式 |

- 隔離原則：所有修復都是「單棟、單欄位」層級；不得因一棟壞掉拒絕整檔或清空全部。
- 修復一律**先記日誌再套用**；修復後資料只在玩家下次存檔時寫回（不在載入時主動覆寫）。
- 連續兩次載入同一棟損壞 → 面板標記「此棟資料反覆損壞，請回報」並附 `entityId`（REQ-SAVE-13）。
- **世界節點與 per-entity 分開隔離**：節點損壞 → 重建索引（記 `SEE/Warn`），per-entity 狀態不受影響；元件損壞 →
  單棟重置，其他棟與世界節點不受影響。
- per-entity 元件**缺席**＝新棟（以 spec 預設建立狀態），不是損壞。
- 索引孤兒（`entityId` 找不到建築）保留一版供除錯、下次存檔丟棄（§6.7）；重複 `entityId` 保留第一筆。

### 6.7 明確不存清單

| 不存的資料 | 存放位置／原因 | 重算來源 |
|---|---|---|
| 世界節點：7 個可變狀態欄位 | 改存逐實體元件（AD-11）；節點只留 4 索引欄位 | per-entity 元件 |
| 逐實體元件：`tier`／`size`／`faction` | 可由藍圖／template 重建，不需存 | 藍圖 TemplateName／spec |
| 逐實體元件：output multiplier／燃料 toggle | 執行期衍生；存讀檔後重套（§8.4） | 第一個 tick 重算 |
| 燃料與水的貨物庫存 | 原版貨物，由遊戲本體存讀；不進我方節點／元件 | 原版 `Inventories` |
| 磨損小數餘量、水的 fractional remainder | <1 貨物，最大損失可忽略 | 載入後歸零 |
| 機械圖／網路 id、鄰接關係 | 由原版重建 | 原版機械圖 |
| 外部模組接點、管網 id、帶端 id | 外部資料不得進我方節點 | 載入後重算 |
| UI 狀態（面板開關、選取） | 不影響玩法 | 預設值 |

### 6.8 讀寫時機與生命週期（需求 §14.2）

| 時機 | 動作 | 失敗行為 |
|---|---|---|
| 世界載入完成（singleton `Load()`） | 讀世界節點 → 驗證版本 → 遷移鏈 → checksum → 建 `Guid → 索引` 快取 | 節點缺席 → 建空節點＋`SEE/Warn`（狀態仍由 per-entity 保存）；解析失敗 → 唯讀降級（不寫回） |
| 實體載入（`SEEBoilerController.Load`） | `Has` 守衛逐欄位讀取 → sanitize → 建 pending 狀態；不依賴實體建立順序 | 單棟損壞 → 重置該棟（§6.6） |
| 建築建造完成 | 以 spec 預設建狀態（`condition=100`、`autoMaintenance=spec 預設`、`waterSupplyMode=auto` 解析）；世界節點補索引列 | 記 `SEE/Warn` 一次 |
| 建築拆除 | 從 registry 移除；索引列標「孤立」，下次存檔丟棄 | 無 |
| 每 1 秒 Tick | **不寫檔**；只更新記憶體 | 例外只停該棟 |
| 存檔事件 | 各 controller `IPersistentEntity.Save` 寫自身元件；最後 singleton `Save` 寫世界節點（索引＋checksum，原子） | per-entity 失敗只影響該棟；節點失敗 → `SEE/Warn`，狀態仍在 |
| 讀檔後首次 Tick | pending 狀態；延遲到原版機械圖重建完成後，由 controller 第一個 tick 才套用 `waterSupplyMode`／`autoMaintenance`，並重套 multiplier／toggle，補發一次狀態事件 | 預設值 |
| 玩家手動維護／切換模式 | 立即更新記憶體並標記「存檔時需寫入」 | UI 顯示失敗原因 |

- **讀檔後必須延遲一幀**（或等機械圖重建完成）再套用狀態，避免載入途中寫節點輸出（REQ-SAVE-12）。
- 記憶體以 `struct` 保持緊湊（≤64 B／棟預算），存檔時才轉 DTO。
- 列舉建築：`EntityRegistry.Entities`（`AsSingleton`）＋`GetComponent<T>()`，或自持 `Dictionary<Guid, SEEBoilerController>`
  registry；**不得**用 `EntityComponentRegistry.GetAll<T>()`（受 `IRegisteredComponent` 限制）。
- 存讀檔後必須重套 `node.OutputMultiplier` 與燃料 toggle（兩者都不存檔；§8.4）。

### 6.9 存檔形態：**已裁決＝混合形態**（世界節點索引＋逐實體狀態；D-6／AD-11，2026-11-06）

> **D-6 最終裁決（Lead 2026-11-06）**：採**混合形態**——世界節點 `SEE` 保留需求 §14.1 的骨架
> （`schemaVersion`／`modVersion`／`checksum`／`buildings[]` 索引／`compat`），但**逐棟可變狀態改存逐實體元件**
> `ComponentKey("SEE")`（`IPersistentEntity`，已實機驗證的路徑）。
> **理由**：世界節點機制已證可行，但 singleton 建立時序有實機風險（須在 `LoadAll → SingletonListener.Collect`
> 前存在，否則漏節點或丟 `InvalidOperationException`；繞法 `MultiBind<ISceneInitializer>` 未實機驗證）；
> per-entity `IPersistentEntity`＋`ComponentKey("SEE")` 是已實機驗證路徑，且節點失敗可降級、狀態不丟。
> **偏離登記（AD-11）**：需求 §14.1 的 7 個 `buildings[]` 狀態欄位改存 per-entity 元件；世界節點只留 4 索引欄位。
> **證據**：`_investigate/findings-persistence-loading.md`（353 行）、`_investigate/out2/save/`。

**世界節點（`world.json` → `Singletons.SEE`）**

| 欄位 | 內容 |
|---|---|
| `schemaVersion` | 1（世界節點與 per-entity 元件共用此版本） |
| `modVersion` | `0.1.0` |
| `checksum` | `sha256:<hex>`；範圍＝世界節點（去 checksum 後標準化 JSON）；**不含 per-entity 狀態** |
| `buildings[]` | 索引：`entityId`（Guid 字串）／`tier`／`size`／`faction`（4 欄；孤兒偵測與診斷） |
| `compat` | `detected[]`／`enabled[]`／`failed[]` |

- 機制：`ILoadableSingleton`＋`ISaveableSingleton { void Save(ISingletonSaver) }`；`SingletonKey("SEE")`；
  `ISingletonSaver.GetSingleton(SingletonKey)`／`ISingletonLoader.TryGetSingleton`；
  `[Context("Game")]` Configurator 註冊；`SerializedWorldFactory.Create → GetSingletons<ISaveableSingleton>() → Save`。
- 節點寫入失敗或缺席 → 記 `SEE/Warn`，**狀態仍由 per-entity 完整保存**（降級不丟資料）。

**逐實體元件 `ComponentKey("SEE")`（`SEEBoilerController : IPersistentEntity`）**

- 7 欄位：`condition`／`autoMaintenance`／`maintenanceState`／`maintenanceRemainingHours`／`lastMaintenanceDay`／
  `waterSupplyMode`／`powerLineAttached`（§6.2）。
- 讀寫：`Has` 守衛、載入 sanitize、單棟損壞重置、未知 enum→預設、缺欄位→預設（§6.6）；`tier`／`size`／`faction`
  不存 per-entity（可由藍圖／template 重建）。
- 列舉建築：`EntityRegistry.Entities`（`AsSingleton`）＋`GetComponent<T>()`，或自持 `Dictionary<Guid, controller>`
  registry；**不得**用 `EntityComponentRegistry.GetAll<T>()`（受 `IRegisteredComponent` 限制，列不到任意 controller）。
- 讀檔後延遲一幀套用（pending 狀態；controller 第一個 tick 套用；不依賴實體建立順序）。

**備援與遷移**

- 若 P8 證實世界節點時序穩定、要把狀態搬回節點：`schemaVersion+1`＋遷移器（v1→v2）；**P3／P4 不得自行改動**。
- 若世界節點完全不可用：可只保留 per-entity（純 per-entity 降級）；世界節點缺席不影響狀態，只失去索引／診斷。
- 任何形態變更由 Lead 更新本節與 `REQ_TRACEABILITY.md`；證據正式條目由 P0 寫入 `API_FACTS_SEE.md`。

### 6.10 體積與耗時門檻（權威；SHARED_BRIEF §13 C-2）

| 指標 | 門檻 | 量測口徑 |
|---|---|---|
| 1,000 棟存檔大小 | **≤400 KB（zip 後實際檔案）** | 存檔前後檔案大小差比對 |
| 寫入耗時 | ≤2 s（1,000 棟） | 存檔事件起算至落盤；3 次取中位數 |
| 載入耗時 | ≤2 s（1,000 棟） | 至世界可操作；3 次取中位數 |
| 單棟估算 | ≈350 B／棟（compact ≈250 B）；100 棟 ≈35 KB、1,000 棟 ≈350 KB 未壓縮 | 需求 §14.7；**估算值不是門檻** |

---

## 7. 設定檔契約（`SEE_Config.json`）

### 7.1 檔案與版本（凍結）

| 項目 | 契約 |
|---|---|
| 位置 | `mod/data/SEE_Config.json`（stage 到成品根） |
| `configSchemaVersion` | **`1`**（分叉遺留的舊值必須由 P1 改為 1；需求 §20、SHARED_BRIEF §2） |
| 型別 | UTF-8 JSON；物件根；不得註解 |
| 鍵群組 | `balance`／`maintenance`／`compat`／`perf`／`anim`／`art`／`ui`（§7.3） |
| 載入者 | `SEE.Core` 的 config codec（`SEE_Config` 解析＋clamp＋驗證）；Gameplay 只讀取結果 |
| 平衡參數 | **不寫入存檔**，讀取時套用，避免存檔鎖死數值 |

### 7.2 載入、clamp 與容錯規則（凍結）

| 規則 | 內容 |
|---|---|
| 缺檔 | 用預設值（等同內建預設），記 `SEE/Info` 一次；不生成檔案 |
| 缺欄位 | 用該欄位預設值；不視為錯誤 |
| 未知欄位 | **保留原樣**（寫回時不得丟棄；schema 升級時可由遷移器處理） |
| 超出範圍 | **clamp 到範圍**並記 `SEE/Warn`（只記一次）；不得因 clamp 改變其他欄位 |
| 型別錯誤／`NaN`／`Infinity`／負值（不允許者） | 回退預設＋`SEE/Warn`；`amount>=0`、`waterPerHour>0`、`intervalDays>=1`、`durationHours>=0` |
| 檔案不合法（整體 JSON 壞） | 備份原檔為 `SEE_Config.invalid-<timestamp>.json`，以預設值續跑，**不刪檔**（REQ-SAFE-12） |
| `maintenance.enabled=false` | 不得留下 `Maintaining` 狀態；condition 回 100（REQ-MAINT-17） |
| `configSchemaVersion` 過高 | 唯讀沿用已知欄位、其餘用預設，記 `SEE/Warn`（不拒絕載入） |
| 不變量 | clamp 後的值必須落在範圍內；`ui.lowSupplyHours` 與 `compat.logThrottleSeconds` 等常數由 `SEEConst` 提供預設 |

**AD-13 裁決（Lead 2026-11-06）：`SEE_Config.json` 的檔案 canonical 形狀＝nested 物件群組**（照需求 §20.7 範例）。
- 例：`balance.fuelRatePer100hp.T1` 寫成 `"balance": { "fuelRatePer100hp": { "T1": 0.032, ... }, "faction": { "outputMultiplier": { "Folktails": 1.0, ... } }, "lodDistances": [18,40] }`；`maintenance.intervalDays.T1` 同理。不得用 dotted key 當實際 JSON key。
- `mod/data/SEE_Config.json` 已符合此形狀（P0 `data-see` 產物）。
- P0 骨架的 `SEE.Core.Config.SeeConfigCodec` 目前以 flat-dotted key 解析（分叉遺留）→ **P2 必須改為讀 nested 物件**；`SEE.Tests` 的 config 測試亦同。此落差列 P2 首批工作，P0 不阻塞（值皆為預設，validator 兩種形狀都收）。

### 7.3 鍵群組總表

| 群組 | 鍵數（需求 §20） | 主責卡 | 本契約完整表 |
|---|---:|---|---|
| `balance.*` | 17 | P2（clamp）／P7（判準） | §7.4 |
| `maintenance.*` | 10 | P2／P3 | §7.5 |
| `compat.*` | 6 | P5 | §7.6 |
| `perf.*`／`ui.*` | 7 | P4／P6 | §7.7 |
| `anim.*` | 17 列／21 葉值 | P9 | §7.8（P9 開工前逐條對照需求 §20） |
| `art.*` | 7 列／11 葉值 | P9 | §7.8（同上） |

### 7.4 `balance.*`（17 鍵＋1 裁決新增；預設值與範圍照需求 §20）

| # | 參數鍵 | 預設 | 範圍 |
|---:|---|---|---|
| 1 | `balance.fuelRatePer100hp.T1` | 0.032 | 0.010–0.040 Log/h |
| 2 | `balance.fuelRatePer100hp.T2` | 0.025 | 0.010–0.040 |
| 3 | `balance.fuelRatePer100hp.T3` | 0.021 | 0.010–0.040 |
| 4 | `balance.fuelRatePer100hp.T4` | 0.016 | 0.010–0.040 |
| 5 | `balance.waterRatePer100hp.T1` | 0.080 | 0.010–0.100 Water/h |
| 6 | `balance.waterRatePer100hp.T2` | 0.070 | 0.010–0.100 |
| 7 | `balance.waterRatePer100hp.T3` | 0.040 | 0.010–0.100 |
| 8 | `balance.waterRatePer100hp.T4` | 0.018 | 0.010–0.100 |
| 9 | `balance.faction.outputMultiplier.Folktails` | 1.00 | 0.95–1.10 |
| 10 | `balance.faction.outputMultiplier.IronTeeth` | 1.05 | 0.95–1.10 |
| 11 | `balance.faction.fuelMultiplier.Folktails` | 0.90 | 0.80–1.10 |
| 12 | `balance.faction.fuelMultiplier.IronTeeth` | 1.00 | 0.80–1.10 |
| 13 | `balance.fullInventoryWorkHours` | 25 | 10–50 |
| 14 | `balance.waterTankHours` | 200 | 100–300 |
| 15 | `balance.fuelScaling` | `proportional` | `proportional`／`vanilla` |
| 15a | `balance.standbyLoadRatio` | 0.10 | 0.00–1.00（**2026-11-08 裁決新增，需求 §20 表外**；0＝恢復「需求 0→Idle」舊行為） |
| 16 | `balance.scienceCost` | 依 §4.2 | 各檔表值 ±20%（clamp 至科技帶；T4 上限 2,250） |
| 17 | `balance.buildCostMultiplier` | 1.00 | 0.80–1.20 |

### 7.5 `maintenance.*`（10 鍵）

| # | 參數鍵 | 預設 | 範圍 |
|---:|---|---|---|
| 18 | `maintenance.enabled` | `true` | true／false |
| 19 | `maintenance.intervalDays.T1` | 6 | 4–20 日 |
| 20 | `maintenance.intervalDays.T2` | 8 | 4–20 |
| 21 | `maintenance.intervalDays.T3` | 10 | 4–20 |
| 22 | `maintenance.intervalDays.T4` | 12 | 4–20 |
| 23 | `maintenance.loadFactorBase` | 0.4 | 0.2–0.8 |
| 24 | `maintenance.autoThreshold` | 50 | 20–80 |
| 25 | `maintenance.manualWarnAbove` | 90 | 50–100（**提示**門檻；不得禁用按鈕，C-5） |
| 26 | `maintenance.durationHours` | 2 | 1–8 遊戲小時 |
| 27 | `maintenance.manualMaterialsEnabled` | `true` | true／false |

### 7.6 `compat.*`（6 鍵）

| # | 參數鍵 | 預設 | 範圍 |
|---:|---|---|---|
| 28 | `compat.waterSupplyMode` | `auto` | `auto`／`tank`／`valve`／`hauler` |
| 29 | `compat.detectionBudgetSeconds` | 2 | 1–5 |
| 30 | `compat.enablePowerLineInjection` | `true` | true／false（失敗即退回 MUST） |
| 31 | `compat.beltFuelMode` | `auto` | `auto`／`haulers` |
| 32 | `compat.logThrottleSeconds` | 30 | 10–120 |
| 33 | `compat.pipePollutionWarn` | `true` | true／false |

### 7.7 `perf.*`／`ui.*`（7 鍵）

| # | 參數鍵 | 預設 | 範圍 |
|---:|---|---|---|
| 34 | `perf.tickSeconds` | 1.0 | 0.5–2.0（**不得低於 0.5 秒**） |
| 35 | `perf.uiRefreshHz` | 4 | 1–10 |
| 36 | `perf.badgeRefreshHz` | 1 | 0.5–2 |
| 37 | `perf.maxBadgesVisible` | 64 | 16–256 |
| 38 | `perf.lodDistances` | 18／40 | 10–80（公尺） |
| 39 | `ui.lowSupplyHours` | 8 | 2–24 |
| 40 | `ui.showIdleBadge` | `false` | true／false |

### 7.8 `anim.*`／`art.*`（P9／P4 契約範圍）

| 群組 | 契約 |
|---|---|
| `anim.*` | 17 列／21 葉值：`enable`、`updateHz`、`lod0MaxDistance`、`lod1MaxDistance`、`baseRevPerSec.T1–T4`、`sizeSpeed.*`、`factionSpeed.*`、`vfxEmissionScale`、`startSeconds`、`stopSeconds`、`pistonPhaseDeg`、`jitterDeg`、`maxParticlesPerBuilding`、`exhaustPulseSeconds`；預設與範圍照需求 §20（C-9 動畫九條） |
| `art.*` | 7 列／11 葉值：`trisLod0.Small/Medium/Large`、`lod1Ratio`、`lod2Ratio`、`textureSize.*`、`smokeAttachmentCount`、`chimneyOverhangTiles`；預設與範圍照需求 §20 |
| 規則 | `anim.enable=false` 時等同靜止、不得殘留 VFX／Transform 偏移；`anim.lod0MaxDistance` 與 `perf.lodDistances` 的 LOD0 同源（改一處即可）；動畫純視覺，失敗不影響輸出（REQ-ART-20） |

> 註：`_investigate/doc-extract.md` §9.5／§9.6 的「列數」標題與實際條目數不一致（17/21 vs 20 列；7/11 vs 9 列）；
> P9 開工前必須以**需求 §20 原文**逐條核對並在 `BALANCE_REPORT.md`／P9 交付中列出最終鍵集合。

### 7.9 設定檔範例（需求 §20.7 逐字，節錄主群組）

```json
{
  "configSchemaVersion": 1,
  "balance": { "fuelScaling": "proportional", "standbyLoadRatio": 0.10, "fullInventoryWorkHours": 25 },
  "maintenance": { "enabled": true, "autoThreshold": 50, "durationHours": 2 },
  "compat": { "waterSupplyMode": "auto", "enablePowerLineInjection": true, "logThrottleSeconds": 30 },
  "perf": { "tickSeconds": 1.0, "uiRefreshHz": 4, "badgeRefreshHz": 1 },
  "anim": { "enable": true, "updateHz": 30, "lod0MaxDistance": 18, "lod1MaxDistance": 40, "vfxEmissionScale": 1.0 },
  "ui": { "lowSupplyHours": 8, "showIdleBadge": false }
}
```

> ⚠️ 風險提示（需求 §20 末）：可調參數是雙面刃。玩家把 `maintenance.intervalDays` 調到上限、`fuelRatePer100hp`
> 調到下限，就能做出「永不維護、幾乎不耗燃料」的版本。緩解：全部參數載入時 clamp 並記 `SEE/Warn`；平衡參數
> 變更需在 M4 重跑 §4 判準才進預設；工作坊頁明確標示「超出範圍的調整不受支援、可能破壞平衡」。

### 7.10 與存檔的關係

- 平衡參數**不寫入存檔**；存檔只用於 `condition`／維護狀態／模式／旗標（§6.2）。
- 讀檔時以「當前 config」重算所有速率，因此改 config 後舊存檔不需遷移。
- `configSchemaVersion` 與存檔 `schemaVersion` 互不推導（§9）。

---

## 8. 執行期行為契約（摘要；Gameplay 照此實作）

> 完整數值與狀態優先序以 SHARED_BRIEF §4–§6、需求 §5／§7／§8／§9 為準；本節只凍結**介面與不可偏離的規則**。

### 8.1 Tick 與時間

| 規則 | 契約 |
|---|---|
| 頻率 | 1 秒結算（`perf.tickSeconds`，下限 0.5）；不得每幀計算（REQ-SAFE-07） |
| 時間基準 | 以**遊戲時間**累積（`IDayNightCycle.FixedDeltaTimeInHours`，1 tick＝0.03125 遊戲小時）；暫停不推進、加速按比例（REQ-SAFE-09、REQ-IMPL-14） |
| 成本 | 每棟每 tick **O(1)**、≤3 µs；1,000 棟 p95 ≤3 ms／p99 ≤6 ms；GC≈0；Tick 內 0 LINQ／0 字串插值／0 配置（SHARED_BRIEF C-1、REQ-SAFE-08） |
| 降頻 | 超預算時可自動降頻到 2 秒結算並記錄、可回復；不得改變平衡結果（以遊戲時間計，REQ-SAFE-10） |
| 例外邊界 | `BoilerTickSystem` 最外層 try/catch；例外只停用該棟並標記 `Faulted`，不中斷遊戲（REQ-SAFE-01、REQ-IMPL-12） |

### 8.2 狀態機（9 主狀態＋`Maintaining` 第 0 級覆蓋態）

優先序（SHARED_BRIEF §5、C-4／C-6）：

| 順位 | 狀態 | 條件 | 行為 |
|---:|---|---|---|
| 0 | `Paused` | 玩家暫停 | 停止輸出與消耗；不磨損 |
| 1 | `NoFuel` | 燃料庫存為 0 | 輸出 0；不磨損 |
| 2 | `NoWater` | 水艙為 0 | 輸出 0；不磨損 |
| 3 | `NeedsMaintenance` | 機況＝0 | 輸出 0；等待維護（材料＋道路） |
| 4 | `Critical` | 機況 1–20 | 輸出 ×0.40 |
| 5 | `Worn` | 機況 21–40 | 輸出 ×0.85 |
| 6 | `MaintenanceDue` | 機況 41–60 | 輸出 ×0.93 |
| 7 | `Idle` | 實際運轉負載 λ_run＝0（未接電網／`standbyLoadRatio=0` 且電網需求 0） | 輸出 0；不耗燃料／水；不磨損 |
| 8 | `Running` | 正常（λ_run>0） | 依負載輸出、耗燃料與水、累積磨損 |
| 覆蓋 | `Maintaining` | 維護進行中 | **第 0 級覆蓋態**：優先於 `NoFuel`，不列入九主狀態計數；UI 顯示「維護中」 |

- 磨損只發生在**帶載運轉**（實際輸出 > 0，即 `Running`／`MaintenanceDue`／`Worn`／`Critical`）；其餘狀態不磨損（C-4）。
- 狀態門檻 100/60/40/20/0 與乘數同段、無重疊；邊界逐一斷言（REQ-MAINT-05）。

**AD-12 裁決（Lead 2026-11-06）：狀態優先序以本表為準，並以「適用性」解決 Idle／磨損帶的文件內部矛盾。**
需求 §13.5 偽代碼（其註解其實寫 `Paused > NoFuel > NoWater > NeedsMaintenance`）的程式碼把 `condition<=0` 放在燃料之前、`Idle` 放在磨損帶之前，與 §3.3 表／§15.3（「不得改寫」）／REQ-CORE-06（「依 3.3 表優先序判定」）衝突。裁定：

1. **適用性**：`Paused`＝玩家暫停；`Maintaining`＝維護進行中；`NoFuel`＝燃料庫存 0；`NoWater`＝水艙 0；`NeedsMaintenance`＝condition = 0；`Critical`／`Worn`／`MaintenanceDue`／`Running` **只在 λ_run>0（帶載運轉）時適用**；`Idle` **只在 λ_run<=0 時適用**（`λ_run = max(λ, balance.standbyLoadRatio)`，2026-11-08 裁決 AD-16；因此「機況 15＋夜間電網需求 0」在預設 `standbyLoadRatio=0.10` 下是 `Critical` 而**不是** `Idle`，因為鍋爐仍在待機運轉；只有 `standbyLoadRatio=0` 或鍋爐未接電網時才是 `Idle`）。
2. **優先序（高→低）**：`Paused > Maintaining > NoFuel > NoWater > NeedsMaintenance > Critical > Worn > MaintenanceDue > Idle > Running`（`Maintaining` 為第 0 級覆蓋態）。
3. **行為閘門與狀態解耦**：最終狀態 ∈ {`Paused`, `Maintaining`, `NoFuel`, `NoWater`, `NeedsMaintenance`, `Idle`} → 輸出 0、不耗燃料／水、不磨損；其餘 → 輸出＝ceil(Horsepower × ConditionCurve.Multiplier)、耗燃料／水＝表定 × λ_run、磨損累積。
4. 必測組合：缺水＋需維護→`NoWater`；缺燃料＋缺水→`NoFuel`；機況 15＋有需求→`Critical`；正常→`Running`；維護中＋缺燃料→`Maintaining`；暫停＋缺燃料→`Paused`；condition=0＋缺燃料→`NoFuel`（補燃料後下一 tick → `NeedsMaintenance`）；`standbyLoadRatio=0.10`＋電網需求 0＋燃料與水充足→`Running`（待機運轉，2026-11-08）。
5. P0 骨架若仍照 §13.5 偽代碼順序，P2 必須照本條改 `BoilerStateMachine.Evaluate` 與測試期望；P0 的 `StateMachineTests` 以本條為準者為準（見 `mod/tests/SEE.Tests/StateMachineTests.cs`）。

### 8.3 燃料與水消耗

| 項目 | 契約 |
|---|---|
| 輸出 | `實際輸出 = 額定 × 機況乘數`（額定已含陣營；**不得二次乘陣營**，PLAN §4.5 D-3） |
| 燃料（預設） | controller 以 `SEEBoilerSpec.FuelPerHour × min(1, 實際輸出/額定馬力)` 結算；原版 `GoodConsumingBuildingSpec` 消耗**永久 pause**（`GetGoodConsumingToggle().PauseConsumption()`），不參與扣料（AD-3、REQ-POWER-10） |
| 燃料（vanilla 模式） | `fuelScaling="vanilla"` 由 controller 以 **λ=1** 實現「需求 >0 即全速耗」；**不恢復**原版線性消耗（AD-3、REQ-POWER-11） |
| 燃料品項 | 以能量 E 結算：先 Log、Log 不足時以能量等值消耗 Plank（1 Plank 抵 2 Log 能量）；扣料用 `Inventory.TakeConsumed`（public）；P8 對帳須涵蓋兩條路徑 |
| 水 | `表定耗水率 × min(1, 實際輸出/額定)`；無水 → 輸出 0、`NoWater`、燃料停止扣除（REQ-FUEL-07） |
| 扣除時機 | 只在帶載運轉扣除（λ_run>0，含夜間待機運轉）；`Idle`／`Paused`／`NoFuel`／`NoWater`／`NeedsMaintenance`／`Maintaining` 不扣（REQ-FUEL-05；2026-11-08 AD-16） |
| 扣料路徑 | 單一消耗路徑（controller）、不得重複扣料；連續 1 遊戲日對帳 ≤±2%（REQ-IMPL-04、AD-3） |
| 水艙允許（AD-10） | controller 初始化時確保 `SingleGoodAllower` 已 `Allow("Water")`（僅未設定時；不覆寫玩家選擇）；否則液體貨物被 disallower 擋住、永不進水，所有水相關 REQ 失效 |
| 對帳口徑（AD-10） | 原版消耗永久暫停 → `_suppliesLeft` 不累積；燃料／水對帳以 **Inventory 整數差值** 計算，不含浮點緩衝項 |
| 水貨物整數化 | 浮點餘量累積、滿 1 貨物才扣庫存；餘量不存檔、載入歸零（REQ-IMPL-06） |
| 燃料能量 | `FuelGoodIds`／`FuelValues` 資料驅動；Log 1.0／Plank 2.0；程式不得硬寫（REQ-IMPL-05） |

### 8.4 動力輸出

| 項目 | 契約 |
|---|---|
| 節點 | `MechanicalNodeSpec.PowerOutput`＝表定馬力（含陣營）、`PowerInput=0`、`IsShaft=false`；`PowerOutput` 必須等於 `SEEBoilerSpec.Horsepower`（P1 生成器逐格驗算） |
| 效率 | 讀原版機械圖結果（`MechanicalBuilding.Efficiency`），**不自行重算、不自建網路快取**（REQ-POWER-08） |
| **λ 防回授公式（AD-9）** | `D = Graph.PowerDemand`；`ΣN_SEE = Σ 我方額定`；`S_other = max(0, Graph.PowerSupply − Σ 我方節點.Actuals.PowerOutput)`；**`λ = clamp01(D / max(1, S_other + ΣN_SEE))`**；**`λ_run = max(λ, balance.standbyLoadRatio)`（鍋爐已接電網時；未接電網時 `λ_run = λ`）**；`multiplier_i = clamp01(λ_run × ConditionCurve.Multiplier(condition_i))`；每 tick `node.SetOutputMultiplier(multiplier_i)`（`MechanicalNode.SetOutputMultiplier` 已證實存在） |
| 燃料 fraction（AD-3） | 用**我方寫入的 `node.OutputMultiplier`** 作比例（fraction），**不要**用 `Actuals.PowerOutput / 額定` 反推（ceil 會造成最大 +1 W 偏差）；水使用同一 fraction |
| 零需求（AD-16，2026-11-08 取代 D-4 的「需求 0→Idle」） | 夜晚製造業收工時，原版 `WorkplacePowerConsumptionSwitch` 會對耗電端呼叫 `MechanicalBuilding.SetConsumptionDisabled(true)`，使 `Graph.PowerDemand` 變 0。若 `balance.standbyLoadRatio>0`（預設 0.10），鍋爐**不停機**：`multiplier_i = clamp01(λ_run × ConditionCurve)`、燃料／水按待機比例扣除、磨損照算，飛輪與煙霧持續（`GoodPoweredGeneratorAnimator` 只看 `OutputMultiplier>0`）。`standbyLoadRatio=0` 才恢復「需求 0→Idle、不耗料、不磨損」的舊行為。未接電網（`Graph==null`）仍走 λ_run=λ=0，避免無電網空轉燒料 |
| 供不應求 | 沿用原版「全網一起降效率」，不新增優先序或配額（REQ-POWER-07） |
| 存讀檔後 | multiplier 與燃料 toggle **都不存檔**；必須在 `InitializeEntity`／第一個 tick 重新套用（REQ-SAVE-12；P3 實作、P8 驗證） |
| 輸出調變方案 | A 案（逐棟 `SetOutputMultiplier`）＝正式方案（D-1 已取證）；B 案（占空比，findings-power §4.4）僅 P8 失敗時備援（§3.5.3）；不得讓低負載超耗 |
| 上限 | 實際輸出不得超過 `PowerOutput`；乘數只在最後一次進位（REQ-POWER-13） |
| 暫停 | `Paused` 優先於所有輸出狀態（REQ-POWER-14） |

### 8.5 磨損與維護

| 項目 | 契約 |
|---|---|
| condition | 每棟 0–100；新建＝100（REQ-MAINT-01） |
| 磨損 | `50/(IntervalDays×24)` 每運轉小時 × `(LoadFloor + LoadFactorRange × 負載比)`；滿載 24 小時 T1 掉 8.33 點（±0.1，REQ-MAINT-03） |
| 間隔 | T1 6／T2 8／T3 10／T4 12 運轉日；滿載 100→50 恰為一間隔（REQ-MAINT-04） |
| 自動維護 | `condition ≤ TriggerThreshold(50)` 且 `autoMaintenance=true` → 檢查材料／道路 → `Maintaining` 2 小時 → condition=100 |
| 手動維護 | `condition<100` 且有料有路即可；>90 仍可按但提示（`manualWarnAbove=90`）；僅 condition=100 時禁用（C-5） |
| 材料 | T1 Log 8；T2 Plank 12＋Gear 2；T3 MetalBlock 10＋Plank 10；T4 MetalBlock 12＋Gear 6；由同 district 有路可達庫存**原子扣除**、不放入引擎內部庫存（REQ-IMPL-09） |
| 缺料／無路 | `MissingMaintenanceMaterials`／`NoAccessForMaintenance`：不扣料、不崩潰、補料接路後自動恢復（REQ-MAINT-11/12） |
| 不新增工種 | 不得新增「維修工」或工人站位（REQ-MAINT-18） |
| 決定性 | 無隨機故障；相同輸入重複兩次得相同機況（REQ-MAINT-15） |
| 整體關閉 | config `maintenance.enabled=false`：機況固定 100、無維護 UI、無材料需求（REQ-MAINT-17） |

### 8.6 暫停、切換與外部整合

- 暫停：停止輸出與消耗、不磨損；恢復後回前一狀態（REQ-POWER-14）。
- 供水模式：`auto` 依偵測解析為 `tank`／`valve`／`hauler`；三模式皆可玩、可存檔、可即時切換，切換不清空水艙、
  不改 condition（REQ-COMPAT-06、REQ-FUEL-15）。
- 外部模組：三段式偵測（模組清單 → 組件簡名 → 型別錨點）、全程 try/catch、總預算 ≤2 秒、單次快取、
  每模組只記一次 `SEE/Compat`；失敗一律退回 MUST（REQ-COMPAT-09/10）。
- PL 注入：只在偵測到 Power Lines 且 config 開啟時執行期注入 `PowerLineSpec`；失敗退回原版節點、不影響存檔
  （REQ-COMPAT-11、REQ-POWER-15/16）。注入不是 Harmony patch 機械圖。
- 主藍圖**不得**含 `PowerLineSpec`、`BuildingPipeSpec`。

### 8.7 日誌與診斷

- 分類與 30 秒節流照 §2.8；壓測不得洗版（REQ-OPS-08）。
- 選配診斷（預設關）：`features` 等級的 `uiDiagnostics` 對應鍵未在本契約 §7 凍結；如需加入，須走 §11 變更流程且
  預設 `false`、零額外成本。
- 日誌內容不得含存檔全文或個資（REQ-SAFE-14）。

---

## 9. 版本三軌（互不推導；需求 §11.6）

| 版本 | 所在位置 | 當前值 | 升版規則 | 相容承諾 |
|---|---|---|---|---|
| `modVersion` | `manifest.json`、存檔 `SEE.modVersion` | **`0.1.0`** | SemVer：MAJOR＝存檔或藍圖不相容；MINOR＝新增功能；PATCH＝修正 | 玩家可見；供診斷與遷移判斷 |
| `schemaVersion` | 存檔 `SEE.schemaVersion` | **`1`** | 任何欄位新增／移除／語意變更 → +1，並新增一支遷移器 | 舊存檔必可載入（遷移鏈） |
| `configSchemaVersion` | `SEE_Config.json` | **`1`** | 同上，但**未知欄位保留**、缺欄位用預設 | 舊設定檔必可載入 |

- 三者互不推導：`modVersion` 0.2.0 可仍是 `schemaVersion` 1（只加 UI）；`schemaVersion` 2 必伴隨 `modVersion`
  MINOR 以上（doc-extract §3.6）。
- 建置期另有兩個非玩家版本：`data/engines.json` 的 `dataVersion`（示例 `1`）與藍圖黃金雜湊，只用於 CI 對帳。
- 任一版本缺失 → 載入失敗並提示（REQ-ARCH-11）。
- 遷移器鏈 `SaveMigrator`：`v1 → v2 → v3 …` 逐段套用、只前進；不認識的更高版本 → 唯讀降級，不得丟棄資料。

---

## 10. 禁止項清單（需求紅線；違反即 FAIL，不得以「可調參數」繞過）

| # | 禁止 | 依據 | 檢查方式 |
|---:|---|---|---|
| 1 | Harmony patch 原版機械圖／庫存搬運／建造流程／全域存檔序列化（PL 注入為唯一可選例外，預設關，且不是 patch 機械圖） | REQ-COMPAT-12、PLAN §7.6 | 原始碼掃描 0 命中 |
| 2 | 外部模組 DLL 的**編譯期引用**；manifest 不得列為必要依賴 | REQ-COMPAT-08 | csproj／參考／manifest 檢查 |
| 3 | 覆寫或修改原版藍圖、原版路徑、原版 loc 鍵 | SHARED_BRIEF §2、REQ-UI-08 | 路徑／key 掃描、diff |
| 4 | 新增物品／流體／新陣營／新工種 | SHARED_BRIEF §1、REQ-MAINT-18 | 資料掃描＋人工審查 |
| 5 | 每幀全圖掃描、`Update` 內 `FindObjects`／反射／字串插值；業務邏輯不得在每幀 `Update` | REQ-SAFE-07、REQ-OPS-05/07 | Roslyn／文字掃描（P6） |
| 6 | 硬寫平衡值（馬力／燃料／水／成本／維護材料只能來自 `engines.json`、spec、config、`SEEConst`） | REQ-AFR-09（ARCH-09）、REQ-IMPL-05 | 白名單外裸數字掃描 |
| 7 | 分叉來源專案的舊識別字（模組 Id／程式集前綴／namespace／存檔 key／loc 前綴／config 檔名） | PLAN §8 風險 5 | 全庫 grep＝0（P0 自查） |
| 8 | 主藍圖含 `BuildingPipeSpec`／`BuildingPipeTargetPortsSpec`／`PowerLineSpec`／`WorkplaceSpec`／Up transput；`GoodConsumingBuildingSpec` 列 Water | REQ-COMPAT-07、R-I、R-B | 藍圖 JSON 靜態掃描（P1 生成器＋P6） |
| 9 | `unsafe`／P/Invoke／`Process.Start`／背景執行緒碰遊戲物件／網路請求 | REQ-NFR-15、REQ-SAFE-15 | 程式碼掃描 |
| 10 | 寫出存檔目錄與日誌以外的檔案；覆寫存檔時非原子寫入 | REQ-SAFE-15、REQ-SAVE-11 | 程式碼審查＋實機測試 |
| 11 | 卡片自行改需求數值／`PLAN.md`／本契約；發現數值或契約衝突未回報 Lead 就施工 | TASKS.md 紀律 | Lead 門禁 |
| 12 | 把實機未驗證的行為寫成「已通過」；AI 宣告人類 gate 通過 | PLAN §7.8、§9 | 驗收紀錄檢查 |

---

## 11. 待 Lead 裁決與取證項

> 本節為 P0 的**開放項清單**；下列未定案前，相關卡片不得自行拍板。**已收斂**：D-3（容量派生）、D-6（存檔形態＝混合
> 形態）、AD-3／AD-4／AD-9／AD-10／AD-11（Lead 2026-11-06 裁決，詳 §11.3）；D-1／D-4 已由 `_investigate/findings-power.md`
> 取證（待 P8 實機確認）。其餘 D-2／D-5 與 AD-1／AD-2／AD-5～AD-8 待取證或 Lead 裁定後更新本節與
> `REQ_TRACEABILITY.md`。

### 11.1 技術取證項 D-1～D-6（PLAN §1.5）

| # | 未知 | 契約暫定口徑（未定案） | 現況 | 影響卡 |
|---:|---|---|---|---|
| D-1 | 發電端如何取得網路實際需求／負載 λ；能否逐棟調變輸出（V-IMPL-2／V-IMPL-3） | A 案（`MechanicalNode.SetOutputMultiplier`）；**λ 防回授公式**：`λ = clamp01(Graph.PowerDemand / max(1, S_other + ΣN_SEE))`、`multiplier = clamp01(λ × ConditionCurve)`（§8.4）；`GoodPoweredGeneratorSpec` 必須移除（AD-9）；B 案（占空比）僅 P8 失敗備援；不得低負載超耗 | **已取證**（`_investigate/findings-power.md` §2.4／§4.4）；P8 實機確認回授震盪 | P0／P3 |
| D-2 | 雙庫存（固體燃料＋液體水）是否都能被搬運／輸送帶／管網補給；`GoodConsumingBuildingSpec` 是否與我方扣料**重複扣燃料** | 方案 A 骨架＋AD-3（原版消耗永久 pause、controller 單一扣料路徑、水自管）；對帳 ≤±2%；若 pause 無效 → §3.5.3 備援 | **部分已解（AD-3）／待實機**（`forensics-inventory`、P8） | P3 |
| D-3 | 水消耗貨物整數化；容量派生取整（0.11×25=2.75 的 floor／ceil／round） | **已證實**：逐項 `Mathf.CeilToInt((float)GoodPerHour × FullInventoryWorkHours)`、總容量＝各項之和；AD-3 再定「容量由表定 `fuelStock` 反推 `GoodPerHour`」的最終公式（§3.5.2），12 檔逐格等於表定；水艙獨立表定（§3.5.1） | **已收斂**（P0 取證＋Lead AD-3 裁決） | P1／P3 |
| D-4 | 電網需求 0 時發電端行為；如何不 patch 而「停機不耗燃料／不磨損」 | **已取證（findings-power §4.1）**：`D==0`（且電池不要求充電）→ multiplier 0＋`PauseConsumption()`＋不磨損／不扣水；不需 patch。因 `GoodPoweredGeneratorSpec` 已移除（AD-9），原版不會自動 Pause/Resume，由 controller 完整接管；`MechanicalBuilding.SetConsumptionDisabled` 語意僅作參考。**2026-11-08 裁決 AD-16 取代本列的停機行為**：需求 0 時改為 `standbyLoadRatio` 待機運轉（預設 0.10）；只有 `standbyLoadRatio=0` 或未接電網才回到 multiplier 0／Idle | **已取證**（`_investigate/findings-power.md` §4.1）；P8 實機確認 | P3 |
| D-5 | 維護系統在發電建築的路徑：材料從 district 庫存扣、無路判定 | 已找到 `DistrictInventoryRegistry`／`AccessibleIsOnDistrictRoad`；材料檢查照做、道路判定以 `BuildingAccessible` 通過；**實機待驗** | **部分已證實／待實機** | P3 |
| D-6 | 存檔形態：世界節點 `SEE.buildings[]`（含 checksum） vs per-entity 元件 | ✅ **已由 Lead 裁決（2026-11-06）：混合形態**——世界節點 `Singletons.SEE` 存 `schemaVersion`／`modVersion`／`checksum`／`buildings[]`（4 索引欄位）／`compat`；逐棟狀態存 `ComponentKey("SEE")`（7 欄位，`IPersistentEntity`）。理由：singleton 時序有實機風險、per-entity 已實機驗證且節點失敗可降級。偏離＝AD-11；證據見 `_investigate/findings-persistence-loading.md`、`_investigate/out2/save/` | **已裁決**（`forensics-persistence` 取證） | P0／P5 |

### 11.2 設計與文件待裁決

| # | 項目 | 內容 | 契約暫定 | 需誰決定 |
|---:|---|---|---|---|
| AD-1 | 原型藍圖 icon 用原版路徑是否可接受 | v1 暫代 icon（如原版蒸汽機 icon）可能造成辨識混淆；P9 前必須換成自製 icon | 允許暫代，須在 `VERIFICATION.md` 標「暫代」；P9 前不可出正式版 | Lead／P9 |
| AD-2 | FT 模型策略（R-A） | v1 用原版 Folktails 同尺寸模型（如水車類）vs 全自製；FT 不得引用 IronTeeth 模型 | v1 允許 Folktails 原版模型暫代；P9 自製中立材質後統一 | Lead／P9 |
| AD-3 | T3／T4 雙燃料的容量口徑 | ✅ **已由 Lead 裁決（2026-11-06）**：容量口徑與消耗口徑分離——`GoodConsumingBuildingSpec` 只作庫存宣告＋容量派生；原版消耗永久 pause（`GetGoodConsumingToggle().PauseConsumption()`）；實際消耗由 controller 以 `FuelPerHour × λ`＋能量換算（Log 1.0／Plank 2.0）結算；`GoodPerHour` 生成公式見 §3.5.2（最終版：單燃料 `(fuelStock−0.5)/25`；雙燃料每項 `(fuelStock/2−0.5)/25`）；12 檔容量逐格等於表定，並解掉 PLAN §4.5 D-6 | 已裁決；P1 生成器／P3 控制器／P6 validator 照 §3.5.2 施工；備援（方案 C）僅 P8 失敗才啟用 | Lead（已決） |
| AD-4 | `SEEMaintenanceSpec.Materials` 藍圖欄位名 | ✅ **已由 Lead 裁決（2026-11-06）**：`GoodAmountSpec` 欄位是 **`Id`／`Amount`**（IL backing fields `<Id>`／`<Amount>`，原版 `BuildingCost`＝`{"Id":"Log","Amount":20}`）；`GoodId`／`GoodPerHour` 屬 `ConsumedGoodSpec`（燃料），兩者不得混淆 | 已裁決；契約 §3.4 已更新，P1／P6 分開檢查 | Lead（已決） |
| AD-5 | REQ-SAVE-09 與 `compat.detected` 的字面張力 | REQ-SAVE-09 說存檔不得出現外部模組識別碼；需求 §14.1 又要求 `compat.detected` 記模組簡名 | 採 §14.1 schema；REQ-SAVE-09 限縮解讀為「型別／組件 Id／接點資料」 | Lead |
| AD-6 | 覆蓋率門檻 | REQ-NFR-07 要求 Core 行覆蓋 ≥80%、分支 ≥70%；PLAN P2 驗收寫 ≥70% | 暫以 REQ 為準（80/70），請 P2 補齊或 Lead 裁決 | Lead |
| AD-7 | REQ-ARCH-14「GENERATED, DO NOT EDIT」檔頭形式 | 藍圖是 JSON、不支援註解；不能加檔頭欄位（會成為未知 spec） | 未定案；候選：同目錄 README＋黃金雜湊＋生成器 `--check` | Lead／P1／P6 |
| AD-8 | loc CSV 編碼與欄位 | 需求 §15.5 L1 寫「UTF-8（含 BOM）、key,text 單行」；原版 `enUS.csv` 實測為 3 欄 `ID,Text,Comment`、無 BOM | 契約採 **3 欄、無 BOM**（實證優先，§5.1） | Lead 備查 |
| AD-9 | `GoodPoweredGeneratorSpec` 是否保留 | ✅ **已由 Lead 裁決（2026-11-06，原 AD-5 編號讓給存檔張力；本項為 AD-9）**：**24 份藍圖一律移除**。IL 證實 `PowerGenerationConfigurator` 會掛 `GoodPoweredGenerator`，其 `UpdateState` 每 tick `SetOutputMultiplier(IsConsuming?1:0)` 並依 `RequiresPower` Pause/Resume → 覆寫我方 λ、與燃料 toggle 互搶。改由 controller 完整接管輸出倍率＋toggle＋零需求邏輯；視覺／音效 spec 可保留（P8 確認） | 已裁決；P1 生成器移除＋P6 掃描、P3 接管、P8 驗證動畫／煙霧 | Lead（已決） |

> 注意：`doc-extract §5.6 L1` 的「key,text」是語意描述，不是實際欄位數；原版 CSV 與姊妹作實機成品皆為 3 欄，
> 此為**已裁決事實**（非待裁決），列此僅供追溯。

### 11.3 已由本契約裁決／收斂的事實（不需再裁決；供追溯）

| # | 事實 | 依據 | 契約位置 |
|---:|---|---|---|
| 1 | `entityId` 用 **Guid 字串**（不是 `long`） | PLAN §4.5 D-8；IL：`EntityComponent.EntityId` 是 `System.Guid` | §6.2 |
| 2 | 藍圖路徑採 `Buildings/Power/SteamBoiler/…` | PLAN §4.5 D-9 | §4.1 |
| 3 | 燃料方案 A（燃料交原版 spec、水自管） | PLAN §4.5 D-10；需求 §13.4 | §4.7 |
| 4 | Core 零遊戲相依、兩個 ComponentSpec 放 Gameplay | PLAN §4.5 D-11 | §2.3 |
| 5 | 輸出已在藍圖含陣營係數；控制器只乘機況、不二次乘陣營 | PLAN §4.5 D-3；需求 §13.5 | §4.2／§8.3 |
| 6 | loc 採 §15.5 的 54 鍵（A12/B5/C22/D15） | PLAN §4.5 D-2 | §5.2 |
| 7 | `maintenanceRemainingHours` 閉區間 `[0,2]` | PLAN §4.5 D-7 | §6.2 |
| 8 | `fuelStock` 閘門只比對基礎值，不比陣營衍生值 | PLAN §4.5 D-6 | §4.10 |
| 9 | 30 Hz 動畫更新件數 ≤13（美術卡用） | PLAN §4.5 D-4 | §7.8（P9） |
| 10 | 12 相容情境＝必測集合；8 組合＝DI 維度 | PLAN §4.5 D-5 | §1.4（T4） |
| 11 | 容量派生：逐項 ceil 相加；水艙獨立表定 | P0 取證（D-3 收斂） | §3.5 |
| 12 | spec 必須有 `[SpecAlias]`；藍圖 key＝dotted alias | P0 取證（IL） | §3.1 |
| 13 | D-6：存檔形態＝**混合形態**（世界節點 `Singletons.SEE` 存版本／checksum／4 欄索引／compat；逐棟 7 欄狀態存 `ComponentKey("SEE")` 的 `IPersistentEntity`）；節點失敗可降級、狀態不丟 | Lead 2026-11-06 裁決＋`forensics-persistence` 取證 | §6.1／§6.9 |
| 14 | AD-3：容量口徑與消耗口徑分離（原版消耗永久 pause；`ConsumedGoods.GoodPerHour` 僅容量派生；實際消耗由 controller 以 `FuelPerHour×λ`） | Lead 2026-11-06 裁決 | §3.5.2、§4.7、§8.3 |
| 15 | AD-3 最終容量生成公式：T1/T2 單燃料 `(fuelStock−0.5)/25`；T3/T4 每項 `(fuelStock/2−0.5)/25`（12 檔容量逐格等於表定） | Lead 2026-11-06 最終版（取代 floor2 版） | §3.5.2 |
| 16 | AD-4：`GoodAmountSpec`＝`Id`／`Amount`；`ConsumedGoodSpec`＝`GoodId`／`GoodPerHour`；兩者不得混淆 | Lead 2026-11-06 更正＋IL 證據 | §3.4 |
| 17 | AD-9：`GoodPoweredGeneratorSpec` **移除**（24 份藍圖）；controller 接管輸出倍率＋燃料 toggle＋零需求邏輯；視覺／音效 spec 可保留 | Lead 2026-11-06 裁決＋`_investigate/findings-power.md` §1.5／§4.1 | §4.3／§4.4／§8.4 |
| 18 | λ 防回授公式（`λ=clamp01(D/max(1,S_other+ΣN_SEE))`）、`multiplier=clamp01(λ×ConditionCurve)`、燃料 fraction 取 `node.OutputMultiplier`、存讀檔後重套 multiplier／toggle（不存檔） | `_investigate/findings-power.md` §2.4／§4.4 | §8.4 |
| 19 | AD-10：水艙必須由 controller `SingleGoodAllower.Allow("Water")`（僅未設定時、不覆寫玩家選擇）；否則液體永不進水；燃料 GCB 走 `Givable`／`FillInput` 不需 allower；對帳只算 Inventory 整數差值（`_suppliesLeft` 不累積） | Lead 2026-11-06 裁決＋`_investigate/findings-inventory-maintenance.md` §2.2／§6 | §4.7／§8.3 |
| 20 | AD-11：需求 §14.1 的 7 個 `buildings[]` 狀態欄位改存 **per-entity `ComponentKey("SEE")`**；世界節點只留 4 索引欄位（混合形態）。若 P8 證實節點時序穩定可搬回（`schemaVersion+1`＋遷移器），P3／P4 不得自行改動 | Lead 2026-11-06 裁決＋`_investigate/findings-persistence-loading.md` | §6.9 |

---

## 12. 驗收對照（Lead 最終檢查）

| 檢查項 | 通過條件 | 證據位置 |
|---|---|---|
| 契約完整性 | 本檔 §2–§9 的凍結表齊備；P1–P9 卡片提示均引用本檔章節 | 本檔＋卡片 |
| 命名一致 | 全庫舊識別字掃描＝0；`SEEBoilerSpec`／`SEEMaintenanceSpec`／四個出貨程式集名一致 | P6 掃描報告 |
| 藍圖 | 24 份、路徑／TemplateName／12 欄位零誤差；必要 spec 齊全、禁止 spec 0 命中 | P1 生成器＋P6 validator |
| loc | 54 鍵齊全、三語一致、字元上限、zhTW 無簡體、原版鍵未覆寫 | P6 `Localizations` 檢查 |
| 存檔 | schema v1 round-trip、checksum、遷移、損壞隔離、1,000 棟體積／耗時 | P5 測試＋`VERIFICATION.md` |
| config | `configSchemaVersion=1`、全部鍵 clamp、未知欄位保留、非法檔備份 | P6 `ConfigValidator` |
| 禁止項 | §10 全部 0 命中（或唯一 PL 注入例外已記錄） | P6 靜態掃描 |
| 偏離登錄 | 本檔 §11 的偏離與待裁決在 `REQ_TRACEABILITY.md` 均有對映 | 兩份文件交叉檢查 |

---

## 附錄 A：引用來源索引

| 來源 | 章節 | 用途 |
|---|---|---|
| 《蒸汽引擎模組需求文檔》 | §4.1、§5、§7、§8、§9、§11、§13.3、§13.4、§14、§15.5、§16、§17、§20、§21.2 | 權威數值、schema、loc、config |
| `brief/SHARED_BRIEF.md` | §2 命名、§3 12 檔數值、§4 公式、§5 狀態機、§6 維護、§7 相容、§8 存檔、§13 C-1～C-9 | 本契約主要來源 |
| `PLAN.md` | §1.4 技術事實、§1.5 D-1～D-6、§1.6 R-A～R-L、§4.5 D-1～D-11、§6 寫入範圍、§7 品質門檻 | 裁決與陷阱 |
| `_investigate/doc-extract.md` | §0.2（域清點）、§3.1–§3.19、§5.5、§9、§11 | 提取事實與 REQ 清單 |
| `_investigate/template-reuse.md` | §2.3（build 流程）、§3.2–§3.9（藍圖／spec／軸序）、R-A 材質陣營綁定 | 藍圖與建置事實 |
| `_investigate/feasibility.md` | §1.3（manifest）、§4（載入／DLL／JSON-only）、§6（型別名） | 環境與 API 事實 |
| `mod/docs/API_FACTS_SEE.md` | 全部（P0 取證） | D-1～D-6 定案與 IL 證據 |
| `mod/docs/api/gameplay-findings.md` | §1（身分／存檔／DI）、§2（Tick）、§5（spec 反序列化） | SpecAlias、ImmutableArray、Guid 證據 |
| `_investigate/findings-power.md` | §1.5／§2.4／§4.1／§4.4 | λ 防回授、`GoodPoweredGeneratorSpec` 移除、零需求 |
| `_investigate/findings-persistence-loading.md`（＋`_investigate/out2/save/`） | 全部 | D-6 混合形態、世界節點／per-entity、A5 載入事實 |
| `_investigate/findings-inventory-maintenance.md` | §2.2／§6 | AD-10（水艙 allower、GCB `Givable`、對帳口徑） |

## 附錄 B：變更紀錄

| 版本 | 日期 | 變更 | 作者 |
|---|---|---|---|
| 1.0-P0-freeze | 2026-11-06 | P0 初版：§2 命名／§3 spec／§4 藍圖／§5 loc／§6 存檔（D-8、D-6 決策槽）／§7 config／§9 版本／§10 禁止項／§11 待裁決（含 Lead 補充的 SpecAlias、ImmutableArray、D-3） | docs-contract（P0） |
| 1.1-P0-rev | 2026-11-06 | 納入 Lead 裁決：**AD-3** 燃料容量／消耗口徑（最終公式 `(fuelStock−0.5)/25`／`(fuelStock/2−0.5)/25`、原版消耗永久 pause、controller `FuelPerHour×λ`）；**AD-4** `GoodAmountSpec`＝`Id`／`Amount`、與 `ConsumedGoodSpec` 區分；**D-6** 存檔形態＝純世界節點 `SEE`（§6.9 改為已裁決＋證據索引）；spec namespace 明示 `SEE.Gameplay` | docs-contract（P0） |
| 1.2-P0-rev | 2026-11-06 | 納入 `findings-power` 取證與 Lead 裁決 **AD-9**：`GoodPoweredGeneratorSpec` 從必要清單移除並列禁止（原版每 tick 覆寫 λ、與燃料 toggle 互搶）；§4.3 選配清單調整；§8.4 新增 λ 防回授公式、`multiplier=clamp01(λ×ConditionCurve)`、燃料 fraction 取 `node.OutputMultiplier`、存讀檔後重套 multiplier／toggle；D-1／D-4 證據更新；§3.5.3 增 toggle 佔空比備援 | docs-contract（P0） |
| 1.3-P0-rev | 2026-11-06 | 納入 `findings-inventory-maintenance` 取證與 Lead 裁決 **AD-10**：§4.7／§8.3 新增「水艙必須由 controller `SingleGoodAllower.Allow("Water")`（僅未設定時、不覆寫玩家）」、燃料 GCB `Givable`／`FillInput` 說明、對帳只算 Inventory 整數差值；§3.5.1 補 `MaximumWorkingTime = FullInventoryWorkHours + 1/Max(GoodPerHour)` 診斷註記 | docs-contract（P0） |
| 1.4-P0-rev | 2026-11-06 | 依 Lead D-6 最終裁決改為**混合形態**：§6.1／§6.2／§6.4–§6.9 改寫（世界節點 `Singletons.SEE` 只存版本／checksum／4 欄索引／compat；逐棟 7 欄狀態存 `ComponentKey("SEE")` 的 `IPersistentEntity`）＋**AD-11** 偏離登記；補列舉限制（不得 `EntityComponentRegistry.GetAll<T>()`）、Guid 原生存檔 primitive、附錄 C（A5 載入／格式事實） | docs-contract（P0） |
| 1.5-P0-rev | 2026-11-08 | 納入 Lead 裁決 **AD-16（夜間待機運轉，取代 D-4 的「需求 0→Idle」）**：新增 `balance.standbyLoadRatio`（預設 0.10、範圍 0.00–1.00）與 `λ_run = max(λ, standbyLoadRatio)`；§5.2 狀態表 `Idle`／`Running` 適用性、§8.3 扣除時機、§8.4 λ／零需求列同步；根因＝`WorkplacePowerConsumptionSwitch` 在非工作時間停用耗電端使 `Graph.PowerDemand=0`（玩家回報「晚上引擎停機」） | Lead（本輪修根因） |

> 後續變更必須由 Lead 核准並在此表累加；任何卡片不得直接改本檔。

## 附錄 C：A5 定案（載入與格式事實；`forensics-persistence`）

| 項目 | 定案事實 | 依據 |
|---|---|---|
| `manifest.json` | 7 欄、PascalCase；`Version.Create` 解析版本 | `_investigate/findings-persistence-loading.md` |
| 組件載入 | `LoadAssemblies` 逐 mod 根**遞迴** `*.dll` → `Assembly.Load(bytes)`（含 `Scripts/`、**無排序**） | 同上 |
| 本地化 | 經 `ModSystemFileProvider<TextAsset>`；CSV `ID,Text,Comment`；語系取「檔名前第一個 `_` 之前」的前綴判定 | 同上 |
| icon meta | 由 `<圖檔>.meta.json` 讀 `isSprite`／`width`／`height` | 同上 |
| config | 用 `IModEnvironment.ModPath`＋`System.IO` 讀寫（不經遊戲序列化） | 同上 |

> 用途：P1（資料載入／manifest／loc／icon／config）與 P5（存檔）依此實作；與本契約 §2.6／§5.1／§7 的凍結值一致。
> 若與實機行為衝突，以實機為準並回報 Lead。
