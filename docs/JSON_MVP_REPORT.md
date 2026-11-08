# SEE JSON MVP — 生成報告

> 卡片：SEE-P1B（JSON-only 先行可玩包）｜日期：2026-11-06｜工作區：`C:\Users\user\Downloads\timber steam engine`
> 角色：本卡只**讀取** `mod\data\**` 並生成獨立包；不改 `data/**`、`src/**`、`build.ps1`、PLAN.md、需求文件與 MWP/HDBF。
> 狀態：**離線生成／驗證全綠；實機載入與 5 項檢查待人類桌面執行**（AI 不宣告實機通過）。

## 1. 摘要

- 產物：`mod\build\SEE-JSON-MVP\`，獨立可安裝包（Id `SteamEngineExpansionMVP`），**77 檔、330,086 bytes（約 322 KB）、0 DLL**。
- 生成器：`mod\tools\stage-json-mvp\stage-json-mvp.ps1`（Windows PowerShell 5.1、純 ASCII、零網路、零 NuGet）。
- 內容：manifest＋24 份藍圖（12 size/tier × 2 陣營）＋2 份 TemplateCollection＋2 份 loc（各 54 鍵）＋24 PNG＋24 meta。
- 驗證：**13/13 檢查 PASS、0 errors**；`-Check` 重跑 **CHECK PASS: 0 differences（77 檔 SHA256 逐一比對）**。
- 自訂 spec：24 份藍圖 `SEE.SEEBoilerSpec`／`SEE.SEEMaintenanceSpec` 命中數 **0**。
- 供電：24/24 `MechanicalNodeSpec.PowerOutput` 與 `engines.derived.json` 逐檔一致。
- 實機：載入冒煙**未執行**（Steam 主程序未在執行、MVP 尚未安裝、沙盒不可寫 `Documents`）→ 待人類桌面執行，指令見 §4.8。

## 2. 生成方式與可重現指令

生成器從 P1 資料推導，**沒有任何手抄數值**：

- 來源（唯讀）：`mod\data\engines.json`、`mod\data\engines.derived.json`、`mod\data\Buildings\Power\SteamBoiler\*.blueprint.json`、`Localizations`、`TemplateCollections`、`Icons`。
- 輸出：`mod\build\SEE-JSON-MVP\`（`-OutDir` 可覆寫）。
- 藍圖轉換只有兩件事：
  1. 移除 `SEE.SEEBoilerSpec`、`SEE.SEEMaintenanceSpec`（以及任何非原版 spec；轉出後驗證只剩 18 種原版 spec＋`Children`）。
  2. `GoodConsumingBuildingSpec.ConsumedGoods` 只留 `Log`（T3/T4 的 `Plank` 移除）；`FullInventoryWorkHours`、水艙 `StockpileSpec`、`TransputProviderSpec`、`BuildingSpec`、`PlaceableBlockObjectSpec`、`LabeledEntitySpec`、`Children` 全部照 P1。
- 輸出 JSON 由腳本內自製的 deterministic writer 產生（不用 `ConvertTo-Json`，避免 PowerShell 主機版本改動格式讓 `-Check` 失去意義）；每檔 UTF-8 無 BOM，結尾單一 `\n`。
- `-Check`：重新生成到 `mod\build\.SEE-JSON-MVP.staging`，與已 staged 包逐檔 `SHA256` 比對；不一致列出差異並 exit 1，**不改動已 staged 包**。
- `-Install`：把 staged 包複製到 `Documents\Timberborn\Mods\SteamEngineExpansionMVP`；若目標已存在會先確認其 `manifest.json.Id` 是本包才刪除重建，否則拒絕；沙盒外寫入失敗時印出精確 `robocopy` 指令並 exit 4（不強行）。`-InstallTarget` 可覆寫目標（本卡用它做了工作區內自測）。

指令：

```powershell
# 生成 + 驗證 + 印出檔案清單
powershell -NoProfile -ExecutionPolicy Bypass -File "mod\tools\stage-json-mvp\stage-json-mvp.ps1" -List

# 重跑比對（不會改 staged 包）
powershell -NoProfile -ExecutionPolicy Bypass -File "mod\tools\stage-json-mvp\stage-json-mvp.ps1" -Check

# 安裝（一般桌面；沙盒會失敗並印出 robocopy 指令）
powershell -NoProfile -ExecutionPolicy Bypass -File "mod\tools\stage-json-mvp\stage-json-mvp.ps1" -Install

# 只生成到自訂資料夾（自測）
powershell -NoProfile -ExecutionPolicy Bypass -File "mod\tools\stage-json-mvp\stage-json-mvp.ps1" -OutDir "mod\build\_mvp-self-test"
```

## 3. 包內容清單

| 路徑 | 檔數 | 說明 |
|---|---:|---|
| `manifest.json` | 1 | 7 欄：Name `SEE JSON MVP`、Version `0.1.0-mvp`、Id `SteamEngineExpansionMVP`、MinimumGameVersion `1.1.0.0`、Description、RequiredMods `[]`、OptionalMods `[]` |
| `Buildings/Power/SteamBoiler/` | 24 | `SteamBoiler.<Small/Medium/Large>.<T1-T4>.<Folktails/IronTeeth>.blueprint.json` |
| `TemplateCollections/` | 2 | `TemplateCollection.Buildings.Folktails/IronTeeth.blueprint.json`，各 append 12 份該陣營藍圖 |
| `Localizations/` | 2 | `enUS.csv`、`zhTW.csv`（各 54 鍵、3 欄、無 BOM；自 P1 原樣複製） |
| `Icons/` | 48 | 24 個 `*.png`＋24 個 `*.png.meta.json`（`{"isSprite":true}`） |
| **合計** | **77** | **0 DLL／0 `.deps.json`／0 `.pdb`／0 config／0 engines.json** |

24 份藍圖逐檔值（皆與 `engines.derived.json`、`engines.json` 對齊；`FuelCap` 是原版由 `ceil(LogRate × 25)` 派生，非 P1 表格值）：

| Size.Tier | Faction | PowerOutput (hp) | FWH | Log/h | FuelCap（派生） | WaterTank | ToolOrder | 模型（同陣營佔位） |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Small.T1 | Folktails | 130 | 25 | 0.02 | 1 | 20 | 110 | WaterWheel.Folktails |
| Small.T1 | IronTeeth | 137 | 25 | 0.02 | 1 | 20 | 110 | SteamEngine.IronTeeth |
| Medium.T1 | Folktails | 230 | 25 | 0.06 | 2 | 36 | 112 | WaterWheel.Folktails |
| Medium.T1 | IronTeeth | 242 | 25 | 0.06 | 2 | 36 | 112 | SteamEngine.IronTeeth |
| Large.T1 | Folktails | 360 | 25 | 0.10 | 3 | 58 | 114 | WaterWheel.Folktails |
| Large.T1 | IronTeeth | 378 | 25 | 0.10 | 3 | 58 | 114 | SteamEngine.IronTeeth |
| Small.T2 | Folktails | 260 | 25 | 0.06 | 2 | 36 | 116 | WaterWheel.Folktails |
| Small.T2 | IronTeeth | 273 | 25 | 0.06 | 2 | 36 | 116 | SteamEngine.IronTeeth |
| Medium.T2 | Folktails | 460 | 25 | 0.10 | 3 | 64 | 118 | WaterWheel.Folktails |
| Medium.T2 | IronTeeth | 483 | 25 | 0.10 | 3 | 64 | 118 | SteamEngine.IronTeeth |
| Large.T2 | Folktails | 720 | 25 | 0.18 | 5 | 100 | 120 | WaterWheel.Folktails |
| Large.T2 | IronTeeth | 756 | 25 | 0.18 | 5 | 100 | 120 | SteamEngine.IronTeeth |
| Small.T3 | Folktails | 380 | 25 | 0.02 | 1 | 30 | 122 | WaterWheel.Folktails |
| Small.T3 | IronTeeth | 399 | 25 | 0.02 | 1 | 30 | 122 | SteamEngine.IronTeeth |
| Medium.T3 | Folktails | 680 | 25 | 0.06 | 2 | 54 | 124 | WaterWheel.Folktails |
| Medium.T3 | IronTeeth | 714 | 25 | 0.06 | 2 | 54 | 124 | SteamEngine.IronTeeth |
| Large.T3 | Folktails | 1060 | 25 | 0.10 | 3 | 84 | 126 | WaterWheel.Folktails |
| Large.T3 | IronTeeth | 1113 | 25 | 0.10 | 3 | 84 | 126 | SteamEngine.IronTeeth |
| Small.T4 | Folktails | 520 | 25 | 0.02 | 1 | 18 | 128 | WaterWheel.Folktails |
| Small.T4 | IronTeeth | 546 | 25 | 0.02 | 1 | 18 | 128 | SteamEngine.IronTeeth |
| Medium.T4 | Folktails | 920 | 25 | 0.06 | 2 | 34 | 130 | WaterWheel.Folktails |
| Medium.T4 | IronTeeth | 966 | 25 | 0.06 | 2 | 34 | 130 | SteamEngine.IronTeeth |
| Large.T4 | Folktails | 1500 | 25 | 0.10 | 3 | 54 | 132 | WaterWheel.Folktails |
| Large.T4 | IronTeeth | 1575 | 25 | 0.10 | 3 | 54 | 132 | SteamEngine.IronTeeth |

> 佔地／高度：Small `{X:2,Y:2,Z:3}`、Medium `{X:3,Y:3,Z:3}`、Large `{X:4,Y:4,Z:4}`；`Blocks` 長度 = X×Y×Z（R-B 軸序）。

## 4. 真實輸出（驗收證據）

### 4.1 生成＋驗證（節錄真實輸出）

```
== SEE JSON MVP ==========================================================
  source       : ...\mod\data
  output       : ...\mod\build\SEE-JSON-MVP
  P1 variants  : 24 (12 size/tier rows x 2 factions)

== Generate =============================================================
  blueprints       : 24
  template colls   : 2
  localizations    : 2
  icons            : 24 png + 24 meta

== Validate =============================================================
  manifest.json                  PASS  Id SteamEngineExpansionMVP, Version 0.1.0-mvp, 7 fields, no required/optional mods
  blueprints (24)                PASS  12 size/tier rows x 2 factions; 24 building blueprints + 2 template collections
  vanilla specs only             PASS  24 files, 0 SEEBoilerSpec/SEEMaintenanceSpec hits, all top-level specs vanilla
  power output                   PASS  24/24 PowerOutput equal engines.derived.json (faction-adjusted hp), PowerInput 0
  fuel / water                   PASS  Log only (T3/T4 Plank dropped), FullInventoryWorkHours 25, liquid StockpileSpec = P1 tank
  cost / tool order              PASS  ScienceCost/BuildingCost = engines.json, ToolGroup Power, ToolOrder 110-132 (> vanilla 50)
  block object                   PASS  Size X/Y/Z = footprint/height; Blocks = X*Y*Z
  models / children              PASS  same-faction P1 placeholder model + matching ConstructionBase
  blueprint loc keys             PASS  every LabeledEntitySpec key present in enUS and zhTW
  localizations (54 keys x 2)    PASS  enUS/zhTW 3-column CSV, identical 54-key sets, no BOM
  template collections           PASS  2 factions x 12 appendix entries, every entry resolves to a generated blueprint
  icons (24 png + 24 meta)       PASS  every LabeledEntitySpec.Icon resolves to a PNG and an isSprite meta file
  shipping shape                 PASS  77 files, no DLL / .deps.json / config / generator inputs

VALIDATION PASSED: 13/13 checks, 0 errors

Staged JSON MVP package: ...\mod\build\SEE-JSON-MVP
  blueprints    : 24
  collections   : 2
  localizations : enUS.csv, zhTW.csv
  icons         : 24 png + 24 meta
  dll / deps    : 0 / 0
  files         : 77
  size          : 322 KB
```

### 4.2 `-Check`（重跑一致性）

```
== Check (staged package vs fresh generation) ============================
CHECK PASS: 0 differences (77 files, SHA256 per file)
```

負向測試（證明 gate 不是空轉）：對 staged 的 `SteamBoiler.Small.T1.Folktails.blueprint.json` 追加 1 個空白位元組後跑 `-Check`：

```
CHECK FAILED: 1 difference(s)
exit=1
```

之後已重新生成乾淨包，再跑 `-Check` 回到 `CHECK PASS: 0 differences (77 files)`。

### 4.3 自訂 spec 命中數 = 0

```powershell
Get-ChildItem 'mod\build\SEE-JSON-MVP' -Recurse -File |
  Select-String -Pattern 'SEEBoilerSpec|SEEMaintenanceSpec' -SimpleMatch
# 輸出：空（hits = 0）
```

### 4.4 PowerOutput vs `engines.derived.json`（24/24）

```
KEY                     PowerOutput  derived.hp  match
Small.T1.Folktails              130         130  YES
Small.T1.IronTeeth              137         137  YES
Medium.T2.IronTeeth             483         483  YES
Large.T2.IronTeeth              756         756  YES
Large.T3.IronTeeth             1113        1113  YES
Large.T4.IronTeeth             1575        1575  YES
... 其餘 18 檔同樣 YES（腳本 gate：power output PASS 24/24）
```

### 4.5 與 P1 藍圖的語意差異（24/24）

```
SEMANTIC DIFF CHECK: 24/24 identical after removing SEE.* specs and dropping non-Log fuel; 0 differ
```

（逐檔把 P1 藍圖移除兩個自訂 spec、把 `ConsumedGoods` 濾成 Log 後，與 MVP 輸出做深層 JSON 比對；唯一差異就是這兩件事。）

### 4.6 原版 spec 型別掃描（離線，Mono.Cecil）

用 `_investigate\probe\GameApiScan` 掃 1.1.2.8 的 660 個 Managed 組件：

```
VANILLA SPEC SCAN: 18/18 spec names resolve to a ComponentSpec subclass in 1.1.2.8
（BuildingSpec…TemplateAttachmentsSpec 全部 base = Timberborn.BlueprintSystem.ComponentSpec）
```

→ MVP 藍圖的每個 top-level spec key 都是原版可解析的 `ComponentSpec` 子類；沒有需要外掛組件才能解析的 key。
掃描輸出：`mod\build\_mvp-spec-scan.txt`。

### 4.7 `-Install` 路徑自測（工作區內，非 Documents）

```
Installed SEE JSON MVP to mod\build\.install-smoke\SteamEngineExpansionMVP
installed files: 77
installed manifest Id: SteamEngineExpansionMVP
```

Documents 內的正式安裝需要人工（沙盒唯讀）；沙盒失敗時腳本會印出：

```powershell
New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\Documents\Timberborn\Mods\SteamEngineExpansionMVP" | Out-Null
robocopy "<staged>" "$env:USERPROFILE\Documents\Timberborn\Mods\SteamEngineExpansionMVP" /MIR /NFL /NDL /NJH /NJS
```

### 4.8 載入冒煙（L3）：**待人類桌面執行**

本階段**未執行**，原因與環境證據（皆為實際查詢）：

```
game exe present        : True
Timberborn running      : False
steam.exe process       : False        # Steam 主程序未執行（只有 5 個殘留 steamwebhelper）
Documents Mods dir      : True
MVP installed           : False        # 尚未安裝；沙盒不可寫 Documents
existing mod folders    : HighDensityBoxFarm, MechanicalWaterPump, TreePlanterTray
```

`Invoke-LoadSmoke.ps1` 的通過條件是「Mods 清單列出本模組且 0 exception」，前提是模組已安裝、
Steam 已登入、且為互動桌面；三者目前不成立（安裝本身即人工步驟）。**不得偽造載入結果。**

人類桌面執行（安裝後）：

```powershell
# 1) 安裝
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Users\user\Downloads\timber steam engine\mod\tools\stage-json-mvp\stage-json-mvp.ps1" -Install

# 2) 載入冒煙（Steam 已登入、正常桌面；遊戲需先關閉）
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Users\user\Downloads\timber steam engine\_investigate\probe\Invoke-LoadSmoke.ps1" `
  -ModName "SEE JSON MVP" `
  -LogPath "C:\Users\user\Downloads\timber steam engine\mod\build\smoke-mvp.log" `
  -TimeoutSeconds 180
```

回報欄位：`Steam connected`／`mod list shown`／`mod listed (SEE JSON MVP)`／`exceptions`（應 0）／exit code（0）。
真正的玩法 5 項檢查見 `docs\JSON_MVP_CHECKLIST.md`。

## 5. 與正式 SEE 的差異

| 項目 | 正式 SEE（P3+） | 本 MVP |
|---|---|---|
| 程式組件 | 4 個 DLL（Core/Gameplay/Integration/UI） | **0 DLL**（純 JSON） |
| 自訂 spec | `SEE.SEEBoilerSpec`、`SEEMaintenanceSpec` | **全部移除** |
| 維護 | 磨損／condition／2h 維護／材料扣料／缺料停機 | **無**（不磨損、不維護） |
| 燃料 | 比例耗燃料（λ／toggle，控制器） | 原版 `GoodConsumingBuilding` 定率消耗；T3/T4 只列 Log |
| 燃料庫存 | P1 表格 `fuelStock`（雙料加總） | 原版派生 `ceil(Log/h × 25)`；T3/T4 較 P1 小（見 §6） |
| 水 | 控制器扣水；水艙為液體 Stockpile | **不消耗水**；只保留 Stockpile（觀察用，見 §6） |
| 輸出 | `MechanicalNode.SetOutputMultiplier(λ)`＋零需求停機 | 恆為額定 hp、無需求也運轉（無 `GoodPoweredGeneratorSpec`，AD-9） |
| 面板／狀態列 | SEE 自訂面板、狀態徽章、操作區 | **無**（只有原版建築面板） |
| 存檔 | per-entity 狀態＋世界節點索引 | 只用原版存檔（建築／庫存） |
| 相容層 | TP／PL／CB 三段式偵測＋供水模式 | **無**（不引用任何外部 DLL） |
| 模型／圖示 | P9 自製 timbermesh＋LOD＋動畫 | 沿用原版同陣營模型＋P1 佔位圖示 |
| loc | 54 鍵（面板／狀態） | 54 鍵照 P1 出貨，但 MVP 只用到 17 個 `Building.SEE.*`；37 個 Status/UI 鍵為惰性資料 |
| 安裝 Id | `SteamEngineExpansion` | `SteamEngineExpansionMVP` |

## 6. 已知限制與風險（誠實清單）

1. **未經實機驗證**：本報告只證明「離線可生成、可解析性有證據、`-Check` 零差異」；載入／建造／供電／補料／存讀檔一律待人類（§4.8、checklist）。
2. **T3/T4 燃料庫存比 P1 小**：MVP 只留 Log，原版容量派生 `ceil(Log/h×25)`：
   T3/T4 為 1/2/3，而 P1 表格 `fuelStock` 是 2/4/6（P1 是 Log＋Plank 雙料、各自 ceil 後相加）。T1/T2 共 12 檔與 P1 完全相等；T3/T4 的**熱值與庫存都低於 P1**，屬 MVP 已知簡化。
3. **水艙可能 0 容量／不補水**：`StockpileSpec` 的 `SingleGoodAllower` 預設 `AllowedGood=null`，需外部 `Allow("Water")`；MVP 無此控制器。原版 SteamEngine 也沒有同時掛 GCB＋Stockpile 的先例，此組合行為只能實機觀察（V-01）。若接 Timber Pipes，`PipeTank` 會把同棟燃料算進液體體積（P1 研究測得最大混裝誤差 11.76%），MVP 未做防護。
4. **無 `GoodPoweredGeneratorSpec`（AD-9 的刻意移除）**：MVP 沒有控制器接手，因此「需求 0 → 輸出 0／停燃料」的原版行為不存在；鍋爐會持續耗燃料並輸出額定 hp。這在 checklist ③④已寫明，不算失敗。
5. **模型尺寸與佔地不一致**：FT 沿用 `WaterWheel.Folktails`、IT 沿用 `SteamEngine.IronTeeth`（同陣營，避免 R-A 材質崩壞），所以視覺大小可能大於/小於 2×2、3×3、4×4 佔地；以格線與 `BlockObjectSpec.Size` 為準，P9 才會補自製模型。
6. **不能與正式 SEE 同時安裝**：MVP 沿用 P1 的 `TemplateName` 與藍圖路徑；同機同時放 `SteamEngineExpansion` 會與 MVP 撞名（重複 Id／TemplateName／Blueprint 路徑）。安裝 MVP 前請先移除正式包（或反之）。
7. **`Version = "0.1.0-mvp"` 的解析**：依 `mod\docs\api\il-version-parse.txt`，`Version.ExtractVersionNumber` 以 `-` 切分取第一段再以 `.` 切分整數（→ Numeric `0.1.0`；`GetExperimental` 只在第 3 段以 `x` 開頭時為 true，故不觸發），靜態上可解析；但「載入器實際接受」仍需 §4.8 的冒煙確認。
8. **只適用 1.1.x**：manifest `MinimumGameVersion = 1.1.0.0`；原版 spec 欄位是 1.1.2.8 掃描結果。
9. **`-Install` 未在 `Documents` 驗證**：沙盒唯讀，正式安裝需人工（§4.7 只自測了工作區內目標資料夾）。
10. **MVP 不維護**：後續數值／藍圖修正一律回 P1 生成器與正式 SEE，**不要手改 `mod\build\SEE-JSON-MVP`**（它會被下次生成覆蓋；正確性以 `mod\data` 為準）。

## 7. 下一步建議

1. **人類執行 `docs\JSON_MVP_CHECKLIST.md` 的 ①–⑤（＋⑥）**，把截圖與 `Player.log` 帶回；這是本專案最早的實機回饋點（SEE-P1B gate）。
2. 若載入失敗：先看 `Player.log` 的 `Blueprint`／`spec`／`Exception` 段落；MVP 的設計允許「移除 `StockpileSpec` 再試」作為二分（此變更需另開卡，不在本卡範圍）。
3. 回饋分流：藍圖幾何／工具列／材質 → 回 P1 生成器；供電／燃料行為 → P3；水艙 `SingleGoodAllower` → P3／P5；驗證規則 → P6。
4. **P6 可選**：把 `stage-json-mvp.ps1` 的 13 道檢查（尤其 spec 白名單、PowerOutput 對帳）收斂進正式 validator／release gate；本卡依規定不改 `build.ps1`。
5. **P8 仍以 `build\SteamEngineExpansion`（含 DLL）為正式包**；MVP 只是先行驗證切片，不取代正式包，也不需要「維護 MVP」。

## 8. 重現與證據檔

| 項目 | 位置／指令 |
|---|---|
| 生成器 | `mod\tools\stage-json-mvp\stage-json-mvp.ps1` |
| staged 包 | `mod\build\SEE-JSON-MVP\`（77 檔） |
| 完整檔案清單 | 生成器 `-List`（見附錄 A） |
| 原版 spec 掃描 | `mod\build\_mvp-spec-scan.txt`（GameApiScan，18/18 ComponentSpec） |
| 檢查清單 | `docs\JSON_MVP_CHECKLIST.md` |
| 載入冒煙 | `_investigate\probe\Invoke-LoadSmoke.ps1`（待人類，指令見 §4.8） |

## 附錄 A：完整檔案清單（77 檔，依路徑排序）

```
   17283  Buildings/Power/SteamBoiler/SteamBoiler.Large.T1.Folktails.blueprint.json
   18788  Buildings/Power/SteamBoiler/SteamBoiler.Large.T1.IronTeeth.blueprint.json
   17346  Buildings/Power/SteamBoiler/SteamBoiler.Large.T2.Folktails.blueprint.json
   18851  Buildings/Power/SteamBoiler/SteamBoiler.Large.T2.IronTeeth.blueprint.json
   17353  Buildings/Power/SteamBoiler/SteamBoiler.Large.T3.Folktails.blueprint.json
   18858  Buildings/Power/SteamBoiler/SteamBoiler.Large.T3.IronTeeth.blueprint.json
   17293  Buildings/Power/SteamBoiler/SteamBoiler.Large.T4.Folktails.blueprint.json
   18798  Buildings/Power/SteamBoiler/SteamBoiler.Large.T4.IronTeeth.blueprint.json
   10486  Buildings/Power/SteamBoiler/SteamBoiler.Medium.T1.Folktails.blueprint.json
   11991  Buildings/Power/SteamBoiler/SteamBoiler.Medium.T1.IronTeeth.blueprint.json
   10546  Buildings/Power/SteamBoiler/SteamBoiler.Medium.T2.Folktails.blueprint.json
   12051  Buildings/Power/SteamBoiler/SteamBoiler.Medium.T2.IronTeeth.blueprint.json
   10554  Buildings/Power/SteamBoiler/SteamBoiler.Medium.T3.Folktails.blueprint.json
   12059  Buildings/Power/SteamBoiler/SteamBoiler.Medium.T3.IronTeeth.blueprint.json
   10494  Buildings/Power/SteamBoiler/SteamBoiler.Medium.T4.Folktails.blueprint.json
   11999  Buildings/Power/SteamBoiler/SteamBoiler.Medium.T4.IronTeeth.blueprint.json
    7564  Buildings/Power/SteamBoiler/SteamBoiler.Small.T1.Folktails.blueprint.json
    9069  Buildings/Power/SteamBoiler/SteamBoiler.Small.T1.IronTeeth.blueprint.json
    7625  Buildings/Power/SteamBoiler/SteamBoiler.Small.T2.Folktails.blueprint.json
    9130  Buildings/Power/SteamBoiler/SteamBoiler.Small.T2.IronTeeth.blueprint.json
    7633  Buildings/Power/SteamBoiler/SteamBoiler.Small.T3.Folktails.blueprint.json
    9138  Buildings/Power/SteamBoiler/SteamBoiler.Small.T3.IronTeeth.blueprint.json
    7572  Buildings/Power/SteamBoiler/SteamBoiler.Small.T4.Folktails.blueprint.json
    9077  Buildings/Power/SteamBoiler/SteamBoiler.Small.T4.IronTeeth.blueprint.json
     806  Icons/SteamBoilerLargeT1Folktails.png
      23  Icons/SteamBoilerLargeT1Folktails.png.meta.json
     805  Icons/SteamBoilerLargeT1IronTeeth.png
      23  Icons/SteamBoilerLargeT1IronTeeth.png.meta.json
     802  Icons/SteamBoilerLargeT2Folktails.png
      23  Icons/SteamBoilerLargeT2Folktails.png.meta.json
     800  Icons/SteamBoilerLargeT2IronTeeth.png
      23  Icons/SteamBoilerLargeT2IronTeeth.png.meta.json
     797  Icons/SteamBoilerLargeT3Folktails.png
      23  Icons/SteamBoilerLargeT3Folktails.png.meta.json
     797  Icons/SteamBoilerLargeT3IronTeeth.png
      23  Icons/SteamBoilerLargeT3IronTeeth.png.meta.json
     816  Icons/SteamBoilerLargeT4Folktails.png
      23  Icons/SteamBoilerLargeT4Folktails.png.meta.json
     814  Icons/SteamBoilerLargeT4IronTeeth.png
      23  Icons/SteamBoilerLargeT4IronTeeth.png.meta.json
     769  Icons/SteamBoilerMediumT1Folktails.png
      23  Icons/SteamBoilerMediumT1Folktails.png.meta.json
     768  Icons/SteamBoilerMediumT1IronTeeth.png
      23  Icons/SteamBoilerMediumT1IronTeeth.png.meta.json
     764  Icons/SteamBoilerMediumT2Folktails.png
      23  Icons/SteamBoilerMediumT2Folktails.png.meta.json
     760  Icons/SteamBoilerMediumT2IronTeeth.png
      23  Icons/SteamBoilerMediumT2IronTeeth.png.meta.json
     764  Icons/SteamBoilerMediumT3Folktails.png
      23  Icons/SteamBoilerMediumT3Folktails.png.meta.json
     761  Icons/SteamBoilerMediumT3IronTeeth.png
      23  Icons/SteamBoilerMediumT3IronTeeth.png.meta.json
     779  Icons/SteamBoilerMediumT4Folktails.png
      23  Icons/SteamBoilerMediumT4Folktails.png.meta.json
     778  Icons/SteamBoilerMediumT4IronTeeth.png
      23  Icons/SteamBoilerMediumT4IronTeeth.png.meta.json
     756  Icons/SteamBoilerSmallT1Folktails.png
      23  Icons/SteamBoilerSmallT1Folktails.png.meta.json
     752  Icons/SteamBoilerSmallT1IronTeeth.png
      23  Icons/SteamBoilerSmallT1IronTeeth.png.meta.json
     751  Icons/SteamBoilerSmallT2Folktails.png
      23  Icons/SteamBoilerSmallT2Folktails.png.meta.json
     746  Icons/SteamBoilerSmallT2IronTeeth.png
      23  Icons/SteamBoilerSmallT2IronTeeth.png.meta.json
     750  Icons/SteamBoilerSmallT3Folktails.png
      23  Icons/SteamBoilerSmallT3Folktails.png.meta.json
     746  Icons/SteamBoilerSmallT3IronTeeth.png
      23  Icons/SteamBoilerSmallT3IronTeeth.png.meta.json
     766  Icons/SteamBoilerSmallT4Folktails.png
      23  Icons/SteamBoilerSmallT4Folktails.png.meta.json
     762  Icons/SteamBoilerSmallT4IronTeeth.png
      23  Icons/SteamBoilerSmallT4IronTeeth.png.meta.json
    3441  Localizations/enUS.csv
    3358  Localizations/zhTW.csv
     462  manifest.json
    1053  TemplateCollections/TemplateCollection.Buildings.Folktails.blueprint.json
    1053  TemplateCollections/TemplateCollection.Buildings.IronTeeth.blueprint.json
```

> 合計：77 檔、330,086 bytes（322.3 KB）。此清單由 `-List` 同源（直接掃描 staged 包）產生。
