# CHANGELOG

## 0.1.0 hotfix-6 — 2026-11-14（UI／GUI 原版化＋專屬 Steam Engines 工具組；版本號不變）

- **面板原版化（AD-21）**：`SEE.UI` 不再自帶 inline 顏色/字級，也不再使用 Unity 預設皮膚；改為遊戲原生的建築面板語彙：
  - 原生九宮格卡片（`entity-sub-panel`＋`bg-sub-box--green`）、`entity-panel__text`／`__sub-text`／`__heading-text`、`text--bold`、狀態色 `game-text--red`／`text--yellow`。
  - 原生九宮格 fragment 按鈕（`VisualElementLoader` 取 `Game/EntityPanel/DemolishableFragment` 的 `entity-fragment__button`，取不到有 fallback）、原生 `Timberborn.CoreUI.ProgressBar`、`game-toggle`、`game-dropdown`（`Timberborn.DropdownSystem.Dropdown`＋`IDropdownProvider`＋`DropdownItemsSetter`，建構失敗 fallback `DropdownField`）。
  - 原本的 Unity `Foldout` 改為原版風格的收合標題（箭頭＋建築名）；預設展開、跨 Render 保留、不寫存檔（REQ-UI-12）。
  - 刷新仍是 4 Hz 髒標記；程式刷新不會重送命令（dropdown 走 `UpdateSelectedValue`、toggle 走 `SetValueWithoutNotify`）；元素樹只建一次。
  - 需求 §15.6 A1 的形狀/顏色 swatch 是唯一保留的 inline 顏色（在 `BoilerPanelNativeStyles` 註解說明）。
- **專屬底部工具組「Steam Engines」（AD-22，玩家要求的「收縮型菜單」）**：新增 `BlockObjectToolGroupSpec`：
  `Id=SteamEngines`、`Order=72`（原版 Power 70 之後、Science 80 之前）、`NameLocKey=UI.SEE.ToolGroup.SteamEngines`、
  `Icon=ToolGroupIcons/SteamEngines`、`FallbackGroup=false`；24 份藍圖 `ToolGroupId` 由 `Power` 改 `SteamEngines`
  （`ToolOrder` 110–142 不變）。底部列因此多一個可展開／收合的分類按鈕，不再把 24 檔塞進原版 Power 分類。
  生成器輸出 group 藍圖與 128×128 placeholder 圖示；validator 新增 `PASS tool group (SteamEngines spec, icon, 24 members)`；
  `build.ps1` 的 stage 加入 `BlockObjectToolGroups/`、`ToolGroupIcons/`。group 藍圖不需列入 TemplateCollection
  （mod 內所有 `*.blueprint.json` 由 `ModSystemFileProvider<BlueprintAsset>` 扫描載入；IL 取證見 PR/commit notes）。
- **本地化 63→64 鍵（AD-23）**：新增 `UI.SEE.ToolGroup.SteamEngines`（enUS "Steam Engines"／zhTW、zhCN「蒸汽引擎」）；
  三語 CSV、生成器、validator、測試與文件同步。
- **測試**：新增 `NativePanelContractTests`（12 項 source-scan：無 Foldout/DropdownField 主路徑、收合預設展開且 Render 不碰狀態、
  原生類別/控制項齊備、唯一 inline swatch、View 零 inline style）；`ContractSurfaceTests`、`UiContractTests`（64 鍵）更新；測試總數 **510→522**。
- **文件**：`mod/src/SEE.UI/README.md`（原版化說明＋待實機清單）、`mod/docs/CONTRACT_SEE.md`（§4.3／§4.9／§5.2）、
  `mod/docs/LEAD_RULINGS_ADDENDUM.md` §九（AD-21～AD-23）、`mod/docs/RELEASE_GATES.md`、`docs/PACKAGE.md`、
  `docs/VERIFICATION.md`、`docs/TEST_PROTOCOL.md`（步驟 1b／1c）、`mod/workshop/*`。
- **離線門禁（2026-11-14 實測）**：`build.ps1 -Clean` → **BUILD PASSED（G01–G22 全綠）**、四程式集 **0 warning**、**PASS 522/522**、
  generator `CHECK PASS（0 differences, 83 files, 24 blueprints）`＋`gates 5/5 PASS`、validator **VALIDATION PASSED**
  （13/13 gate group、1 既有 WARN、0 error）、staged **135 檔、2,385,102 bytes**、
  package sha256 `9977ed808df154171913eaefc76a05087f75af1cb75989adccb2afa58699cca3`。
- **待實機（AI 不得宣告通過）**：面板原版外觀、▾/▸ glyph、原生 dropdown 展開收合、150% UI 縮放不裁切、
  底部「Steam Engines」按鈕出現/位置、展開後 24 檔排序、圖示顯示、重載不重複；照 `docs/TEST_PROTOCOL.md` 步驟 1b／1c 由人類簽核。

## 0.1.0 hotfix-5 — 2026-11-08（搬運工需求狀態移除＋耗水減半；離線門禁全綠）

- **修正**：完工蒸汽引擎不再顯示原版狀態「需要搬運工才能運轉。」（`Status.Hauling.NoHaulingPost`）。
  根因：本模組鍋爐刻意免工人（無 `WorkplaceSpec`），原版 `GoodConsumingBuildingStatusInitializer` 對「沒有 `Workplace` 的 GCB 建築」
  一律初始化 `NoHaulingPostStatus`（IL：`_investigate/out5-bugfix/il-nohaulingpost.txt`、`_investigate/out2/inventory/il-goodconsuming.txt`）。
  修法：`SEEBoilerController.HaulerStatus.cs` 在完工／讀檔啟用後以 public `NoHaulingPostStatus.Disable()` 關閉；區域重指派後延後一個結算 tick 再關。
  燃料庫存、搬運路徑與我方狀態不變（不用 Harmony、不 patch 原版 UI）。擺放預覽時原版說明列仍會出現，列已知限制。
- **平衡調整（玩家回報「耗水太大」，AD-19）**：全 12 檔耗水率減半——每 100 hp T1 0.080→0.040、T2 0.070→0.035、
  T3 0.040→0.020、T4 0.018→0.010；逐檔 `waterPerHour` 與水艙 `waterTankGoods` 同步重算（例：Small.T1 0.10/20→0.05/10、
  Large.T1 0.29/58→0.14/28、Large.T2 0.50/100→0.25/50）。`waterTankHours` 維持 200 → 滿水艙自主時間不變、每次補水只需一半貨物。
  燃料、hp、研究／建造費、維護與 30 日平衡判準全部不變（四判準仍 4/4 PASS）。
- **生成器閘門 1（AD-20）**：舊「水艙貨物 ≥8× 燃料貨物」是只在舊費率成立的代理量，改為「水艙自主時間 ≥8× 燃料庫存自主時間」
  （`waterTankHours ≥ 8×fullInventoryWorkHours`，200 ≥ 8×25）；`release-gates.json` G04 evidence 同步。
- **測試**：新增 `HaulerWarningSuppressionTests`（source-scan 契約）；`TierCatalogTests` 表定 fixture 更新為新耗水值；
  其餘資料驅動測試（藍圖／golden／validator／balance-sim）全數重生成並重跑。
- **文件**：`mod/docs/LEAD_RULINGS_ADDENDUM.md` §八（AD-18/19/20）、`CONTRACT_SEE` §3.2／§4.2／§4.7／§4.10／§7.4／§8.2、
  `API_FACTS_SEE` §2.7、`README` 12 檔表與 §3.2／§6、`REQ_TRACEABILITY`、`docs/BALANCE_REPORT.md`、`docs/VERIFICATION.md`、`docs/PACKAGE.md`。
- **離線門禁（最終，2026-11-08 熱修-5 實測）**：`build.ps1 -Clean` → **BUILD PASSED（G01–G22 全綠）**、0 warning ×4、**PASS 510/510**、
  generator `CHECK PASS（0 differences, 80 files, 24 blueprints）`＋`gates 5/5 PASS`、validator **VALIDATION PASSED**（12/12 gate groups、1 既有 WARN、0 error）；
  balance-sim **PASS 12/12（四判準 4/4）**；staged **132 檔、2,379,665 bytes**；package sha256 `4a661a1ed86301131b267ec7253c7972f36deeabcbd70d206bc29fd7023142c5`；
  已安裝至 `Documents\Timberborn\Mods\SteamEngineExpansion`（132 檔、與 `build/` 逐檔 SHA-256 0 mismatch）。
- **實機未驗**：完工建築狀態消失、面板耗水換算減半、既有存檔載入、搬運工仍可補燃料／水——皆待人類 P8 簽核（AI 不得宣告通過）。

## 0.1.0 發佈 — 2026-11-08（GitHub 開源發佈）

- 新增 Steam Workshop 封面素材：`mod/workshop/cover/`（去浮水印的 1920×1080、壓字版、1280×720 縮圖、322×181／1:1 QA）與 `mod/workshop/thumbnail.png`；提示詞見 `mod/workshop/COVER_PROMPTS.md`。
- 新增工作坊文案與上傳說明：`mod/workshop/DESCRIPTION.md`、`mod/workshop/README.md`、`mod/workshop/workshop_data.json`。
- GitHub 發佈：<https://github.com/stanley-1028/SteamEngineExpansion>（MIT License；README 增列製作與致謝）。
- **修正工作坊上傳崩潰**：`workshop_data.json` 的 `ItemId` 留空（`""`）會讓遊戲 `UInt64.Parse("")` 拋 `FormatException`；已從遊戲 Mods 資料夾移除該檔，repo 改為 `workshop_data.template.json`（不含 ItemId）。首次上傳前不要放 `workshop_data.json`，發佈成功後由遊戲寫入數字 ItemId。
- **Steam 工作坊已上架（2026-11-08）**：<https://steamcommunity.com/sharedfiles/filedetails/?id=3836039641>（ItemId `3836039641`）。
- 新增完整工作坊介绍文（简中 BBCode＋English summary）：`mod/workshop/DESCRIPTION.md`；README/工作坊说明同步更新上架链接与 ItemId。
- 致謝：本模組與 **DeepSeek V4.1 Flash 1017** 共同完成。
- Steam 工作坊：素材與文案已備，需在遊戲內 Mods 管理器以擁有者 Steam 帳號發佈（Steam 不提供可代發的公開 API）。

## 0.1.0 hotfix-4 — 2026-11-08（夜間待機運轉：有燃料與水就不再熄火；版本號不變）

- **修正**：玩家回報「steam engine 一旦有燃料和水，就應該一直運作，而不是到了晚上就停機」。
- **根因**：入夜後製造業收工，原版 `WorkplacePowerConsumptionSwitch.Tick` → `UpdatePowerConsumption()` 以
  `!Workplace.AnyWorkerHasJobRunning()` 呼叫 `MechanicalBuilding.SetConsumptionDisabled(true)` → 耗電端
  `PowerInput=0` → `MechanicalGraph.PowerDemand` 變 0。我方控制器原本把 `λ=0` 判為 `Idle`（輸出 0、不耗料、不磨損），
  於是**所有** SEE 引擎在夜間一起停機（飛輪與煙霧停止）。
- **修法（根因）**：新增 `balance.standbyLoadRatio`（預設 **0.10**、範圍 0.00–1.00）與
  `BoilerFormulas.OperationalLoadRatio`；控制器把實際運轉負載改為 `λ_run = max(λ, standbyLoadRatio)`，
  輸出／燃料／水／磨損全部沿用 `λ_run`。因此只要燃料與水充足且未暫停／維護／機況 0，夜間電網需求 0 時
  引擎仍以 **10% 待機**運轉（`GoodPoweredGeneratorAnimator` 只看 `OutputMultiplier>0`，畫面與滿載同為持續轉動）。
  `standbyLoadRatio = 0` 可恢復舊的「需求 0 → `Idle`、不耗料、不磨損」；**未接電網**（`Graph==null`）仍為 `Idle`，
  避免無電網空轉燒料。代價：夜間仍耗 10% 燃料／水並累積磨損（磨損係數下限 0.40 → 約滿載 0.46 倍）。
- **測試／資料**：`BoilerFormulasTests`（`OperationalLoadRatio`）、`ConfigTests`／`ConfigClampTests`（預設與 0..1 clamp）、
  `StateMachineTests`（待機 0.10 → `Running`）共新增 9 項；`tools/validate` 新增 `balance.standbyLoadRatio` 規則。
- **文件**：README §3.1／§3.2／§3.3、`docs/TEST_PROTOCOL.md`（新增步驟 5b 夜間待機）、`docs/VERIFICATION.md`（AC-04）、
  `mod/docs/CONTRACT_SEE.md`（AD-16、config 表、§8.2／§8.3／§8.4）、`mod/docs/LEAD_RULINGS_ADDENDUM.md` §七。
- **離線門禁（2026-11-08 實測）**：`build.ps1 -Clean -Install` → **BUILD PASSED（G01–G22 全綠）**、**PASS 504/504**、
  generator `CHECK PASS（0 differences, 80 files, 24 blueprints）`＋5/5 資料閘門、validator **VALIDATION PASSED**
  （12/12 gate group、1 個既有 ConstructionBase WARN）；成品 **132 檔、2,378,127 bytes**、
  package sha256 `6d6ee3858cb93987a3355fdcf449d65951828cc3dc95c4db293e926e0b3ed57c`；已安裝至
  `Documents\Timberborn\Mods\SteamEngineExpansion`（132 檔、逐檔 SHA-256 0 mismatch）。
- **仍待實機**：夜間待機運轉（狀態 `Running`、飛輪持續、約 10% 輸出與消耗）照 `docs/TEST_PROTOCOL.md` 步驟 5b 由人類簽核。

## 0.1.0 hotfix-3 — 2026-11-08（UI：面板預設展開＋zhCN 本地化；版本號不變）

- **SEE 面板預設展開**：原本 `Foldout` 預設收合，玩家只看到一行「▸ Small Steam Engine Mk.I」，誤以為面板沒有實作。現在預設展開，標題顯示「建築名 · 目前狀態」。
- **新增 zhCN 本地化**：遊戲以簡體中文執行，先前只出 enUS／zhTW，`Player.log` 每次載入出現 63 條 `Missing or empty localization key ... in zhCN`，SEE 的狀態／燃料／水／維護列因此空白。現在 `Localizations/zhCN.csv` 63 鍵齊備；生成器與 build gate 改為 enUS／zhTW／zhCN 三語系驗證（G12／G20）。
- 離線門禁：`build.ps1 -Clean` → **BUILD PASSED（G01–G22）**、132 檔、2,316 KB、package sha256 `cadd7162bba84742f5b820fe42df6ebc4745ef7121cc303ac0fa7cb7bf7782bf`。

## 0.1.0 hotfix-2 — 2026-11-08（水艙獨立＋搬運工可補水與燃料；版本號不變）

- **修正**：普通搬運工（Hauling Post）無法把**水**與**燃料**送進蒸汽引擎（玩家 2026-11-08 回報）。
  根因：v0.1.1 熱修把水併進 `GoodConsumingBuildingSpec` 的燃料庫存——水艙失去獨立的 public input
  庫存宣告、容量與燃料共用同一個搬運目標；而原本的液體 `StockpileSpec` 又會和 GCB 一起
  雙初始化 `NoHaulingPostStatus` 而崩潰，兩條路都不能走。
- **修法（根因）**：24 份藍圖的 `ConsumedGoods` 恢復只列燃料（Log／Plank）；水艙改由
  `SEEWaterInventoryInitializer` 在程式建立 `WaterTank` **public input** `Inventory`
  （容量＝`SEE.SEEBoilerSpec.WaterCapacityGoods`，貨物 `Water` 為 giveable-and-takeable），
  鍋爐完成時由控制器 `Enable()`、離開完成態時 `Disable()`，扣水改用 `TakeExisting`；
  燃料維持原版 `GoodConsumingBuildingSpec`（原版消耗永久 pause、控制器單一扣料路徑不變）。
  這樣水與燃料各自有獨立的搬運目標，普通搬運工都能送進來，且不需要 `StockpileSpec`。
- **生成器／驗證器**：gate 2 由「混裝體積誤差」改為「水艙獨立（ConsumedGoods 不得含 Water、
  水艙容量＝表定值）」；`FuelCapacityChecks`／`BlueprintTableChecks`／`BoilerBlueprintChecks`
  同步改為燃料-only 契約，並新增 `WaterTankSupplyTests` 鎖住程式與庫存 wiring。
- **存檔**：舊存檔中原本存在燃料庫存裡的水不會自動搬到新水艙（引擎重新補水即可；
  建築物的 `SEE` 狀態欄位不變）。
- **離線門禁（2026-11-08 實測）**：`build.ps1 -Clean` → **BUILD PASSED（G01–G22 全綠）**、**PASS 495/495**、
  generator `CHECK PASS（0 differences, 79 files, 24 blueprints）`＋5/5 資料閘門、validator **VALIDATION PASSED**
  （12/12 gate group、1 個既有 ConstructionBase WARN）、負向測試 **control + 6/6**；成品 **131 檔、2,384,537 bytes**、
  package sha256 `ae79448f6231910fd541f192594ae0ece8d6b9d836f51899d573173a2cfd24ac`。
- **仍待實機**：搬運工實際補水／補燃料、面板數字、TP 降級照 `docs/TEST_PROTOCOL.md` 由人類簽核。


## 0.1.0 hotfix — 2026-11-07（NoHaulingPostStatus 完工崩潰修正；版本號不變）

- **修正**：`InvalidOperationException: Field of NoHaulingPostStatus named _activePredicate isn't null`。
  根因：同棟同時宣告液體 `StockpileSpec`（水艙）與 `GoodConsumingBuildingSpec`（燃料），
  `Stockpile` 與 `GoodConsumingBuilding` 各加一個 `NoHaulingPostStatus` 初始化器，完工時初始化兩次。
- 24 份藍圖**移除 `StockpileSpec`**，水改列為 `GoodConsumingBuildingSpec.ConsumedGoods` 的一項
  （`GoodPerHour = (waterTankGoods − 0.5) / 25`，容量維持表定 18–100 件）；controller 改讀同一 Inventory。
- **限制**：Timber Pipes `tank`／`valve` 供水模式暫不支援（沒有獨立液體庫存）；`auto` 一律 `hauler`，
  手動選 tank／valve 會顯示不可用並退回搬運工。v0.2 恢復獨立水艙。
- 舊存檔若留有未完工的 SEE 工地，更新後請拆除重建。
- 離線門禁：`build.ps1 -Clean` **BUILD PASSED（G01–G22 全綠）**、PASS n/n、131 檔、2,329 KB、
  package sha256 `180ebdc40c9789fae9c2c260c036e296e4f0e7f80749f3cf77274014265a11db`。

## 0.1.0 — 2026-11-06（首個可安裝版；**尚未發佈**）

> 狀態：離線門禁全綠、已安裝至本機 `Mods`；T2–T6 實機與 AC-21 動畫待驗，**未上架工作坊**。
> 套件：`mod/build/SteamEngineExpansion/`｜83 檔、574,665 bytes｜package sha256 `4c294c6a83208cd2debca041f5094c4d2dffa62721ddf4cee1f696a38805c257`。

### 新增

- **12 種蒸汽引擎建築**（小型 2×2／中型 3×3／大型 4×4 × Mk.I–Mk.IV），兩陣營（Folktails／Iron Teeth）共 **24 份藍圖**＋2 份 TemplateCollection。
- **資料驅動生成器** `tools/generate-blueprints`（7 步＋5 道閘門＋`--check`）；唯一數值源 `data/engines.json`；24 藍圖 12 欄位零誤差。
- **維護系統**：機況 0–100、磨損只在帶載運轉、6／8／10／12 日維護間隔、自動／手動維護、2 遊戲小時停機、
  材料由 district 庫存扣除、缺料／無路不扣料不崩潰、四狀態可存檔。
- **動力輸出**：`MechanicalNode.SetOutputMultiplier` 逐棟調變；λ 防回授；比例耗燃料（`proportional`，可切 `vanilla`）；
  需求 0 待機不耗料、不磨損；移除 `GoodPoweredGeneratorSpec` 以避免覆寫（AD-9）。
- **雙軌補給**：燃料（原版 `GoodConsumingBuildingSpec` 庫存＋控制器結算，原版消耗 pause）；水（液體 `StockpileSpec`＋`Allow("Water")`）。
- **UI 面板**：狀態列＋動力／燃料／水／維護／相容／操作；狀態徽章；4 Hz 髒標記；**63 鍵** enUS／zhTW（≥54 基準）。
- **相容層**：Timber Pipes／Power Lines／Conveyor Belts v2 三段式偵測；`auto/tank/valve/hauler` 供水模式；
  Power Lines 條件注入（非 Harmony）；失敗一律降級、只記一次 `SEE/Compat`；舊版 `Opolame.PowerLines` 只提示。
- **存檔**：schema v1；世界節點索引＋per-entity 狀態混合形態（AD-11）；checksum；12 類損壞恢復；遷移鏈。
- **驗證工具**：`tools/validate` 22 項 gate（G01–G22）、5 項負向測試、`tools/release/release-gates.json`。
- **平衡／效能報告**：12 檔 30 日平均全 PASS、四判準 4/4（最敏感大型 Mk.IV +2.93%）；1,000 棟離線估算。
- **交付文件**：`docs/PACKAGE.md`、`docs/TEST_PROTOCOL.md`（17.5 十二步）、`docs/VERIFICATION.md`（AC-01～21＋Go/No-Go）、
  `docs/WORKSHOP.md`（工作坊草稿）、`README.md`。

### 驗證結果（離線）

- `build.ps1 -Clean`：**0 warning／PASS 487/487／CHECK PASS 0 diff／VALIDATION PASSED／G01–G22 全 PASS**。
- 成品：4 DLL、24 藍圖、63 鍵×2、24 icon＋24 meta；不含 `src/`、`tools/`、`tests/`、`.deps.json`、`.pdb`。
- 已知非阻擋 WARN：Small 的 `ConstructionBase2x2` 與契約文字 `2x3` 不一致（已記錄待裁決）。

### 已知限制

- 模型與圖示為原版／生成佔位（FT=WaterWheel、IT=SteamEngine）；自製 39 mesh、LOD、動畫為 **P9** 後續交付，
  因此 **AC-21 未通過**、17.5 步驟 11／12 視覺項為 `N/A（P9 前）`。
- 實機驗收（T2–T6，含 AC-14 1,000 棟、30 日實機、存檔實測、共存）**尚未由人類簽核**；不得寫「已通過」。
- 管路 `tank` 模式與混裝體積（V-01／V-02）待實機；v1 有 TP 時 `auto`→`valve`。
- 存檔混合形態（AD-11）為已追認偏離；P8 需驗證 singleton 時序。
- 覆蓋率報告（`SEE.Core` 80/70、T0 70%）本次未重跑，列發佈前待補。
- 同 tick 多棟維護依註冊順序（R-4）；行數預算改 soft target（R-7）。

### 升級注意

- 這是首個版本；尚無舊版存檔遷移需求。移除模組後載入既有存檔的行為待實機確認（AC-19）。
- 不要與 `SteamEngineExpansionMVP`（JSON-only 先行包）同時安裝：TemplateName 相同會撞名。
