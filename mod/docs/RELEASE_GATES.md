# SEE 發佈門禁（Release Gates）

> 本檔是人類可讀的門禁表；機器可讀的權威清單位於 `mod/tools/release/release-gates.json`。
> `mod/build.ps1` 每次執行都會載入該 JSON，**逐條要求每個 `blocking: true` 的門禁被評估且通過**；
> 未評估或未通過即非零退出。需求依據：需求文檔 §17.4（AC-01～AC-21）、§19.2（Go/No-Go）、
> §11.9.5（發佈前證據）、`PLAN.md` §7（品質門檻）、`CONTRACT_SEE.md` §1（建置與驗證門檻）。
>
> 文件版本：SEE-P6（2026-11-06）。**本檔不宣告實機通過**；P8 人類 gate 仍須簽核。

---

## 1. 執行方式

```powershell
# 全流程（建置 → 測試 → 生成器 --check → validator → 門禁稽核 → 打包）
pwsh -File mod/build.ps1 -Clean

# 沒有 PowerShell 7 的環境（本機 CI 實測；build.ps1 保持 5.1 相容、純 ASCII）
powershell -NoProfile -ExecutionPolicy Bypass -File mod/build.ps1 -Clean

# 負向測試（控制組＋5 個故意破壞案例，全部必須 FAIL 且指出項目）
pwsh -File mod/tools/validate/negative-tests.ps1
```

前置條件：

| 項目 | 要求 |
|---|---|
| .NET SDK | 9.0（`net9.0` 工具與測試 runner；遊戲端 `netstandard2.1`） |
| Timberborn | 1.1.2.8 已安裝；build 只**讀取** `Timberborn_Data/Managed` 與 `StreamingAssets/Modding/Blueprints.zip` |
| 網路 | 不需要（離線；Mono.Cecil 走本機 NuGet 快取，`tools/validate/validate.csproj` 以 HintPath 引用） |
| PowerShell | 7（`pwsh`）優先；Windows PowerShell 5.1 等價可跑（`build.ps1` 以 `\u` 轉義處理中文警告輸出） |
| 參數 | `-TimberbornDir <資料夾>` 可覆寫遊戲路徑；`-SkipTests` 只供開發回路，**G02 會標成 SKIP 而非 PASS** |

---

## 2. 門禁清單（22 條；id 與 `release-gates.json` 一致）

| ID | 門禁內容（可判定） | 執行者 | 通過證據（真實輸出樣式） | 對映 |
|---|---|---|---|---|
| **G01** | 4 個出貨程式集 0 warning（`TreatWarningsAsErrors=true`） | `build.ps1` | 每個專案 `0 个警告 / 0 Warning(s)` | REQ-NFR-01/02/06、19.2-6 |
| **G02** | 離線測試 runner 全綠 | `build.ps1` | `PASS 510/510` | REQ-TEST-01、AC-05/07/08/12/13、19.2-6 |
| **G03** | 生成器 `--check` 與磁碟檔案逐 byte 相同 | `build.ps1` | `CHECK PASS (0 differences, 80 files, 24 blueprints)` | REQ-ARCH-04/06、REQ-NFR-09、AC-01 |
| **G04** | 生成器 5 道資料閘門全過（水艙自主時間 ≥8× 燃料庫存自主時間（AD-20）、水艙獨立（ConsumedGoods 無 Water、容量＝表定）、效率判準、燃料容量逐格、無禁用 spec） | `build.ps1` | `gates : 5/5 PASS` | REQ-BLD-09、FUEL-08/09、COMPAT-04/05、ARCH-08 |
| **G05** | validator 全部 gate group PASS | `build.ps1` | `VALIDATION PASSED`、`12/12 gate group(s)` | REQ-TEST-01/02/03、AC-01/02/03/16 |
| **G06** | `manifest.json` 契約（7 欄位、Id、空 mod 清單、Version 0.1.0） | `tools/validate` | `PASS  manifest.json` | REQ-ARCH-11、19.2-5 |
| **G07** | `engines.json` 為 12 列唯一數值源（`dataVersion=1`、列舉/派生驗算） | `tools/validate` | `PASS  engines.json (12 rows)` | REQ-ARCH-04/05/06、AC-01 |
| **G08** | 26 份 blueprint 文件對遊戲＋SEE spec metadata 解析成功、欄位型別正確 | `tools/validate` | `PASS  blueprint spec schema` | REQ-IMPL-01/15、AC-01/03 |
| **G09** | **24 份藍圖 × 12 個權威欄位** 與 `engines.json`＋`engines.derived.json` **零誤差** | `tools/validate` | `PASS  blueprint table (24 x 12 authoritative fields)`；`24/24 files x 12/12 fields, 0 mismatch` | REQ-TEST-01、ARCH-14、BLD-09、FUEL-01/08、AC-01/03 |
| **G10** | 燃料容量與獨立水艙逐格等於表定（`float32 ceil(GoodPerHour×25)`、`SEE.SEEBoilerSpec.WaterCapacityGoods`；藍圖不得有 StockpileSpec／Water consumed good） | `tools/validate` | `PASS  fuel/water capacity derivation` | REQ-BLD-09、FUEL-08/11、AC-01/03 |
| **G11** | 鍋爐藍圖紅線：無 `GoodPoweredGeneratorSpec`／`BuildingPipeSpec`／`PowerLineSpec`／`WorkplaceSpec`；`PowerOutput=Horsepower`、`PowerInput=0`、`IsShaft=false`；燃料白名單 Log（T1/T2）／Log+Plank（T3/T4）；無 Up/Top transput | `tools/validate` | `PASS  boiler blueprint rules (AD-9 / AD-5)` | REQ-BLD-04/05、POWER-16、COMPAT-07/11 |
| **G12** | enUS／zhTW 各恰 63 鍵（≥54 基準）、無 raw key、無簡體、未覆寫原版鍵、placeholder 一致 | `tools/validate` | `PASS  localization (63 keys >= 54, enUS/zhTW)` | REQ-UI-04/05/08/09、AC-02/16 |
| **G13** | `SEE_Config.json` schema 1、六組齊全、每個預設值在 clamp 範圍 | `tools/validate` | `PASS  config schema and clamp` | REQ-SAFE-04/12、19.2-5/6 |
| **G14** | **黃金雜湊**：24 份藍圖 byte 等於 `tests/SEE.Tests/Golden/blueprints.sha256`（validator＋build.ps1 各自獨立重算） | `tools/validate`＋`build.ps1` | `PASS  golden hash (24 blueprints)`；`24/24 golden entries verified` | REQ-ARCH-14、NFR-09、AC-01 |
| **G15** | 禁止項靜態掃描 0 命中：`Update` 內 `FindObjects`／每幀全圖掃描／每幀字串插值或讀 config、`unsafe`/P-Invoke/`Process.Start`/網路/背景執行緒、Harmony 出現在 `SEE.Integration` 以外、硬寫平衡值表、`SteamEngine`/`SteamPlant` 作 TemplateName/loc key | `tools/validate` | `PASS  forbidden static scan`；`0 red-line hit(s)` | REQ-SAFE-07/15、NFR-15、OPS-05/07、AFR-09、RISK-09、AC-18 |
| **G16** | 出貨程式集白名單與分層：恰 `SEE.Core`/`Gameplay`/`Integration`/`UI`（`SEE.Tests` 不入包）、無外部模組／NuGet／Harmony 引用、Core 零遊戲相依、凍結 spec 欄位與 `[SpecAlias]` 完整 | `tools/validate` | `PASS  assembly whitelist and layering` | REQ-ARCH-02/03、NFR-10、COMPAT-08、AC-09/10 |
| **G17** | 原始碼事實：無分叉舊識別字、無編譯期外部模組參考、無 NuGet `PackageReference`、檔案行數上限（400／spec 600） | `tools/validate` | `PASS  source facts` | REQ-ARCH-10、NFR-03/05、RISK-09 |
| **G18** | 成品包恰 4 個 DLL；無 `.deps.json`、`.pdb`、`SEE.Tests.dll`、`src/`、`tools/`、`tests/` | `build.ps1` | staged inventory 檢查 | R-E/R-F/R-G、19.2-3/4 |
| **G19** | 成品包含 24 份藍圖（12 檔×2 陣營）＋2 份 TemplateCollection | `build.ps1` | staged inventory 檢查 | REQ-ARCH-05、AC-01/02 |
| **G20** | 成品 enUS／zhTW 各 ≥54 鍵（目前 63，兩語相同；header 3 欄） | `build.ps1` | staged key count 檢查 | REQ-UI-04/05/09、AC-02/16 |
| **G21** | 成品 24 個 PNG icon ＋ 24 個 `.png.meta.json`（`isSprite: true`），一一對應 | `build.ps1` | staged inventory 檢查 | REQ-UI-07、AC-16 |
| **G22** | 版本三軌一致（manifest `0.1.0`／save schema `1`／config schema `1`）＋寫出套件雜湊檔 | `build.ps1` | `SEEVersion.cs`／manifest／config 檢查；`build/SteamEngineExpansion.sha256` | REQ-ARCH-11、SAVE-01、19.2-5 |

> **G09 的 12 個權威欄位**（與生成器 `Verification.cs` 同一組）：TemplateName、footprint(X/Y)+height(Z)、
> Horsepower、FuelPerHour、WaterPerHour、FuelCapacityGoods、WaterCapacityGoods、MechanicalNodeSpec.PowerOutput、
> BuildingSpec.ScienceCost、BuildingSpec.BuildingCost、PlaceableBlockObjectSpec.ToolOrder
> （水艙容量改由 `WaterCapacityGoods` 驗證，不再有 `StockpileSpec.MaxCapacity`）
> （另附維護間隔／材料、燃料品項與能量、Loc 三鍵＋icon、`Entrance.HasEntrance`、`Blocks` 長度、`Children` 結構）。

---

## 3. 產物（成功建置後）

| 路徑 | 內容 |
|---|---|
| `mod/build/SteamEngineExpansion/` | 遊戲用成品：4 DLL＋`manifest.json`＋`SEE_Config.json`＋`Buildings/`（24 藍圖＋1 份「GENERATED, DO NOT EDIT」告示 README，AD-7＋48 `.timbermesh`）＋`Localizations/`（enUS/zhTW/zhCN）＋`Icons/`（24 PNG＋24 meta）＋`TemplateCollections/`（2）；共 132 個檔案 |
| `mod/build/SteamEngineExpansion.sha256` | 逐檔 `sha256  relative/path`（排序），供 P8 安裝前比對 |
| `mod/build/build-report.json` | 版本、檔案數、大小、套件雜湊、每條門禁 PASS 狀態（機器可讀） |
| `mod/build/negative-tests/` | 負向測試暫存（預設結束即刪；`-KeepTemp` 保留） |

**不進成品**：`engines.json`、`engines.derived.json`、`loc_keys.json`、`README.md`、`ART_PLACEHOLDER.md`、
任何原始碼／工具／測試、`.deps.json`、`.pdb`。

---

## 4. 負向測試（證明門禁真的會擋）

`tools/validate/negative-tests.ps1` 先把 `data/`、4 個 DLL、黃金雜湊複製到 `build/negative-tests/` 的暫存 mod root：

1. **控制組**：未修改的副本必須 `VALIDATION PASSED`（排除 harness 自身造成的假失敗）。
2. **N1**：把 `SteamBoiler.Medium.T2.Folktails` 的 `SEE.SEEBoilerSpec.FuelPerHour` 改成 `0.13` → G09 必須 FAIL 並指出欄位（0.12）。
3. **N2**：從 `enUS.csv` 刪掉 `Status.SEE.Running` → G12 必須 FAIL 並指出缺鍵。
4. **N3**：用 validator 的 `--fixture-add-reference`（僅測試用）把 `SEE.UI.dll` 副本加上 `SEE.Gameplay` 參考 → G16 必須 FAIL（`SEE.UI must not reference SEE.Gameplay`）。
5. **N4**：把 `BuildingPipeSpec` 塞回一份藍圖 → G11/G08 必須 FAIL（`forbidden spec 'BuildingPipeSpec'`）。
6. **N5**：把 `SEE_Config.json` 的 `perf.tickSeconds` 設為 `0.1`（範圍 0.5–2.0）→ G13 必須 FAIL（`outside the supported range`）。

腳本逐案檢查「退出碼非 0 ＋ `VALIDATION FAILED` ＋ 命中預期訊息」，全數符合才印 `NEGATIVE TESTS PASSED`。

---

## 5. 與需求 §19.2 Go/No-Go 的對映

| §19.2 條目 | 內容 | 自動化門禁 | 仍須人類證據（P8） |
|---|---|---|---|
| 1 | AC-01～AC-21 全綠＋17.5 手動腳本由非開發者簽核 | 部分：AC-01/02/03/16（G03/G05/G09/G12/G19/G20/G21） | AC-04～AC-21 的實機行為、17.5 12 步簽核 |
| 2 | 三模組缺席／齊備四種情境可玩 30 遊戲日 | 間接：G11/G16（藍圖紅線＋無外部引用） | P5/P8 12 相容情境、三模組實機 |
| 3 | 24 藍圖與 loc 零缺失、zhTW 無簡體、用語一致 | **是**：G03/G04/G09/G12/G14/G19/G20/G21 | 工作坊頁與文件用語人工校對 |
| 4 | 無 P0/P1 缺陷、已知限制寫入說明 | 部分：G15/G17/G18（紅線與打包純淨） | 缺陷清單、已知限制（V-01～V-10）審閱 |
| 5 | 版本號／存檔 schema／設定 schema 一致（0.1.0／1／1） | **是**：G06/G13/G22 | — |
| 6 | §11.9.5「發佈前證據」＋CI error 級門檻（T0/T1）全綠 | 部分：G01/G02/G05（T0/T1 自動化） | 1,000 棟壓測、存檔量測、故障注入報告（M4/P8） |

> **不得混淆**：G01～G22 只證明「資料、組件、分層、紅線、打包」在離線環境可判定通過；
> 效能（AC-14/15/18 的 Profiler 部分）、30 日平衡（AC-17）、實機行為（AC-04～AC-13、AC-19～AC-21）
> 一律標「待 P8 人類驗收」，AI 不得宣告實機通過。

---

## 6. 維護規則

1. 新增／修改門禁：先改 `mod/tools/release/release-gates.json`，再同步本檔表格；`build.ps1` 不需改碼
   （validator 類門禁以 `validatorCheck` 欄位驅動、成品類門禁以 `mod/build.ps1` 評估）。
2. `validatorCheck` 字串＝validator 輸出的 gate group 名稱（`PASS  <name>`），凍結後不得任意改名；
   改名時要同步 JSON 與 `tools/validate/Program.cs`。
3. 任何 `blocking` 門禁未被評估 → build 直接失敗（避免「門禁靜默消失」）。
4. 數值／藍圖只改 `data/engines.json` 後重跑生成器；不得手改 24 份藍圖（G03/G14 會擋）。
5. 本檔與 `release-gates.json` 由 Lead／P6 維護；P8 不得為通過而放寬門禁。

---

## 7. 已知記錄偏離（不阻擋 G01～G22，需 Lead 追蹤）

| 項目 | 現況 | 處置 |
|---|---|---|
| `ConstructionBase` 尺寸 | 小型藍圖用 `ConstructionBase2x2`；`CONTRACT_SEE` §4.5 寫「兩數＝X 寬×Z 高」（小型應為 `2x3`，原版存在該檔）。validator 以 **WARN** 記錄偏離（任務口徑：存在或已記錄），G05 仍 PASS | P1 生成器 `engines.json.artPlaceholders.constructionBases.Small` 是否改 `ConstructionBase2x3` 由 Lead 裁決；改動需重跑生成器＋G03/G14 |
| 實機驗證 | 未執行 | 全部標「待 P8」；AI 不得宣告通過 |
