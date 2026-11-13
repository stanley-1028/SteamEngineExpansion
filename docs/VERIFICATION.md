# SEE 驗收對照（VERIFICATION）— AC-01～AC-21、T0–T6 與 Go/No-Go

> **2026-11-08 熱修-2（搬運修根因）**：普通搬運工無法補水／補燃料的根因已修——燃料只留 `GoodConsumingBuildingSpec`，
> 水艙改為程式建立的 `WaterTank` public input `Inventory`（`SEEWaterInventoryInitializer`，容量＝`WaterCapacityGoods`），
> 控制器完成時啟用、扣水改用 `TakeExisting`；不再使用 `StockpileSpec`（避免 `NoHaulingPostStatus` 雙初始化崩潰）。
> 驗證器改為 fuel-only 契約並新增 `WaterTankSupplyTests`；離線門禁：`build.ps1 -Clean` → **BUILD PASSED（G01–G22 全綠）**、
> **PASS 495/495**、generator `CHECK PASS`、validator `VALIDATION PASSED`（12/12）、負向測試 **control + 6/6**、
> 131 檔／2,384,537 bytes、package sha `ae79448f…`。搬運工實機補水／補燃料仍需人類依 TEST_PROTOCOL 簽核。
>
> **2026-11-08 修正**：完工 `NoHaulingPostStatus` 崩潰已解決——水艙改為程式建立的獨立 `WaterTank` public input `Inventory`（`SEEWaterInventoryInitializer`），不再使用 `StockpileSpec`、也不再併入 `GoodConsumingBuildingSpec`；搬運工可補水與燃料。UI 另修正：SEE 面板預設展開、新增 zhCN 63 鍵本地化。離線門禁重跑 `build.ps1 -Clean` → **BUILD PASSED（G01–G22）**；Timber Pipes 的 tank/valve 暫不支援（`auto`→`hauler`），屬本版已知偏離。
>
> **2026-11-08 熱修-4（夜間待機運轉，Lead 裁決 AD-16，取代 D-4／REQ-CORE-08／REQ-POWER-09 的預設行為）**：
> 根因＝夜間 `WorkplacePowerConsumptionSwitch` 停用耗電端 → `MechanicalGraph.PowerDemand=0` → 原本 `Idle` 停機。
> 現在只要燃料＋水充足且未暫停／維護／機況 0，`λ_run = max(λ, balance.standbyLoadRatio)`（預設 **0.10**，可由 config
> 設 0 恢復舊行為）→ 引擎持續待機運轉（飛輪/煙霧持續、燃料水與磨損按 10% 比例）。`Idle` 只保留給未接電網或
> `standbyLoadRatio=0`。離線門禁：`build.ps1 -Clean` → **BUILD PASSED（G01–G22 全綠）**、**PASS 504/504**、
> generator `CHECK PASS（0 differences, 80 files）`、validator `VALIDATION PASSED`（12/12）、**132 檔／2,317 KB**、
> package sha `d9863428…`；夜間行為仍待人類實機簽核（AC-04、TEST_PROTOCOL 新步驟）。
>
> **2026-11-08 熱修-5（玩家回報：沒有搬運工就顯示不能運轉＋耗水太大；Lead 裁決 AD-18／AD-19／AD-20）**：
> ① 完工鍋爐原版狀態 `Status.Hauling.NoHaulingPost`（zhTW「需要搬運工才能運轉。」）由 **public `NoHaulingPostStatus.Disable()`**
> 關閉（`SEEBoilerController.HaulerStatus.cs`；區域重指派後延後一個結算 tick 再關；不刪燃料 GCB、不改搬運路徑、無 Harmony）；
> ② 全 12 檔耗水率減半（T1 0.040／T2 0.035／T3 0.020／T4 0.010 每 100 hp）與水艙重算（`waterTankHours` 仍 200）；
> ③ 生成器閘門 1 改「水艙自主時間 ≥8× 燃料庫存自主時間」（AD-20）。離線門禁：`build.ps1 -Clean` → **BUILD PASSED（G01–G22 全綠）**、
> **PASS 510/510**、generator `CHECK PASS（0 differences, 80 files）`＋`gates 5/5`、validator **VALIDATION PASSED**（12/12）、
> balance-sim **PASS 12/12（4/4 判準）**、**132 檔／2,379,665 bytes**、package sha `4a661a1e…`；負向測試 **control + 6/6**；
> 實機行為（狀態消失、耗水減半、既有存檔、搬運工補料）仍待人類 P8 簽核（TEST_PROTOCOL 新步驟 5c）。
>
> **2026-11-14 熱修-6（UI／GUI 原版化＋專屬 Steam Engines 工具組，Lead 裁決 AD-21／AD-22／AD-23）**：
> ① SEE 面板改原版外觀：九宮格卡片＋原版字體/狀態色類＋原生九宮格按鈕＋原生 `ProgressBar`／`game-toggle`／
> `game-dropdown`（`Timberborn.DropdownSystem.Dropdown`，失敗 fallback）；Unity `Foldout` 改為原版風格收合標題
> （預設展開、跨 Render 保留）。② 24 檔 `ToolGroupId` 改 `SteamEngines`，底部新增可展開／收合的分類按鈕
> （Order 72，Power 之後）；圖示 `ToolGroupIcons/SteamEngines`；不需新增 TemplateCollection。
> ③ 新增 `UI.SEE.ToolGroup.SteamEngines`，loc 63→64；新增 `NativePanelContractTests` 12 項。
> 離線門禁：`build.ps1 -Clean` → **BUILD PASSED（G01–G22 全綠）**、**PASS 522/522**、generator `CHECK PASS（83 files）`＋`gates 5/5`、
> validator **VALIDATION PASSED（13/13、1 既有 WARN、0 error）**、**135 檔／2,385,102 bytes**、
> package sha `9977ed80…`。實機外觀/收合/工具組行為仍待人類（TEST_PROTOCOL 新步驟 1b／1c）。

> 卡片：SEE-P8（打包＋安裝＋人類實機驗收 gate）｜整理日：2026-11-06｜模組版本：**0.1.0**（尚未發佈）
> 遊戲基準：Timberborn `1.1.2.8-ffc2de0-sw`｜安裝位置：`%USERPROFILE%\Documents\Timberborn\Mods\SteamEngineExpansion`
>
> **證據分類（互不替代）**
> - **離線 PASS**：本機可重跑（`mod/build.ps1 -Clean`、`SEE.Tests` 522/522、validator G01–G22、生成器 5 閘門、balance-sim）。
> - **待實機**：只能在遊戲內由人類照 `docs/TEST_PROTOCOL.md` 驗證；**AI 不得宣告通過**。
> - **人類簽核**：僅當人類在 `docs/TEST_PROTOCOL.md` §5 簽核表填寫並回傳後更新。
>
> 目前結論：**離線門檻全 PASS；T2–T6 實機項全部「待驗」；AC-21（動畫）因 P9 未交付而「未通過」。Go/No-Go＝No-Go（不可發佈）。**
> **AI 不得自行判定通過**：本表所有「待驗」都必須由人類照 `docs/TEST_PROTOCOL.md` 操作並簽核後才能改寫。

---

## 1. 本次離線證據（真實輸出摘要）

| 證據 | 真實輸出 |
|---|---|
| 建置 | 四支程式集 **0 警告 / 0 錯誤**（G01） |
| 測試 | **PASS 522/522**（G02；`dotnet run --project mod/tests/SEE.Tests -c Release -m:1`） |
| 生成器 | **CHECK PASS（0 differences, 83 files, 24 blueprints）**；資料閘門 **5/5 PASS**（G03／G04） |
| validator | **VALIDATION PASSED**；13/13 gate group、1 WARN、0 error（G05） |
| 發佈門禁 | **G01–G22 全 PASS**；套件雜湊 `9977ed808df154171913eaefc76a05087f75af1cb75989adccb2afa58699cca3` |
| 成品 | 135 檔、2,385,102 bytes、4 DLL、24 藍圖、2 TemplateCollection、1 BlockObjectToolGroup＋1 圖示、64 鍵×3 語系、24 icon＋24 meta、48 timbermesh |
| 安裝 | `Mods\SteamEngineExpansion`：135 檔、2,385,102 bytes、與來源逐檔 SHA-256 **0 mismatch**（另含 3 個工作坊更新用中繼檔，不在出貨包） |
| 缺少的離線證據 | **覆蓋率報告**（`SEE.Core` 行 ≥80%／分支 ≥70%、T0 全域 ≥70%，REQ-NFR-07／§17.1）本次未重跑 → **列發佈前待補**（不影響 G01–G22） |

離線 WARN（已記錄、非阻擋）：

```text
WARN: [table] ConstructionBase recorded deviation (contract §4.5/§4.8):
      Small uses ConstructionBase2x2#nested (contract XxZ expects 2x3)
```

> 覆蓋率是需求 §17.1 明列的驗收口徑；本次 build 只證明測試全綠與門禁，**不證明覆蓋率**。
> 在補件前，不得把 REQ-NFR-07 或 T0 覆蓋率寫成 PASS。

---

## 2. AC-01～AC-21 逐條

> 「目前證據」欄：**離線 PASS**＝有可重跑證據；**待實機**＝必須人類簽核；**未通過**＝明確未達。
> 只要含「待實機」，該 AC 就不能整體算通過。T2–T6 對應欄見 §3。

| AC | 驗收條件（摘要） | 判定方式 | 目前證據 | 對應 REQ | 負責卡 |
|---|---|---|---|---|---|
| AC-01 | 24 藍圖全部載入；12 檔數值逐格相符（誤差 0） | T1 自動比對 | **離線 PASS**：G03（24 份重生成 0 diff）、G05/G09（24×12 欄位 0 mismatch）、G14（golden hash 24/24）、G19 | REQ-TEST-01 | P1／P6 |
| AC-02 | 24 藍圖 loc 鍵全存在、未覆寫原版鍵 | 掃描腳本 | **離線 PASS**：G12（64 鍵×3、無原版覆寫、無 raw key）、G20 | REQ-TEST-02、REQ-UI-08 | P1／P6 |
| AC-03 | 免工人；`PowerOutput`＝§3、`PowerInput=0`、`IsShaft=false`；完工建築不顯示原版搬運工需求狀態（AD-18） | 藍圖檢查＋source-scan | **離線 PASS**：G11（24 份逐份檢查）、G09 權威欄位；AD-18 `HaulerWarningSuppressionTests`（public `NoHaulingPostStatus.Disable()`、區域重指派後延後再關、無 Harmony）＋**待實機**（協定步驟 5c：無搬運站時狀態不出現） | REQ-TEST-03、REQ-BLD-04、AD-18 | P1／P3／P6／P8 |
| AC-04 | 夜間電網需求 0 時：有燃料＋水→待機運轉（`λ_run≥standbyLoadRatio`，預設 0.10；飛輪/煙霧持續、燃料水按比例、磨損照算）；玩家把 `standbyLoadRatio` 設 0 或未接電網時才 `Idle`（輸出 0、不耗料、不磨損） | T3 零需求案例（白天與夜間各一次） | **離線邏輯 PASS**（`BoilerFormulas.OperationalLoadRatio`＋config clamp＋狀態機「待機 0.10→Running」測試；`StandbyLoadRatio=0` 仍走舊 `Idle` 帳本案例 booked=0）＋**待實機**（協定步驟 5／§3.2 零需求＋新增夜間待機步驟） | REQ-TEST-04、AD-16 | P2／P3／P6／P8 |
| AC-05 | 半載燃料與水耗為滿載 50%（±2%） | T0＋T2 | **離線 PASS**：`ConsumptionReconciliationTests` `fuel-half` / `water-half` bookedErr≈0.000005%／0、30 日整數差 0.25%／0.17%＋**待實機**量測（協定步驟 2） | REQ-TEST-05 | P2／P3／P8 |
| AC-06 | 水源中斷顯示 `NoWater` 且輸出 0；同時缺燃料顯示 `NoFuel` | T2／T3 | **離線邏輯 PASS**（AD-12 優先序測試）＋**待實機**（協定步驟 3、4） | REQ-TEST-06、REQ-UI-03 | P2／P3／P4／P8 |
| AC-07 | 維護四狀態可進入／離開／存檔，失敗狀態不耗材料 | T3／T5 | **離線邏輯 PASS**（`MaintenanceLedgerTests`、存檔 codec）＋**待實機**（協定步驟 6、7、8、9） | REQ-TEST-07 | P2／P3／P5／P8 |
| AC-08 | 機況 0 輸出 0；維護完成 condition=100、輸出恢復 | T2 | **離線邏輯 PASS**（狀態機／帳本／乘數曲線）＋**待實機**（協定步驟 6、12） | REQ-TEST-08 | P2／P3／P8 |
| AC-09 | 三模組 8 組合皆可建、運轉、存檔、載入 | T3 | **離線決策表 PASS**（12 情境假探針）＋**待實機**（協定步驟 10；8 組合至少 4 種） | REQ-TEST-09 | P5／P6／P8 |
| AC-10 | 缺席模組退回 MUST、`SEE/Error` 0 行 | T3＋log | **離線邏輯 PASS**（降級／日誌節流測試）＋**待實機**（協定步驟 10 的 log 檢查） | REQ-TEST-10 | P5／P8 |
| AC-11 | PL 注入失敗退回原版節點、不崩潰、只記一次日誌 | T3 | **離線 PASS**（`PowerLineInjectionTests` 成功／失敗兩路徑；非 Harmony）＋**待實機**（實際拉線） | REQ-TEST-11、REQ-OPS-06 | P5／P8 |
| AC-12 | 存檔往返一致、未知欄位保留 | T5 | **離線 PASS**（`SaveCodecTests`／`SaveNodeTests`／`IntegrationSaveTests`；round-trip、未知欄位）＋**待實機**（協定步驟 9） | REQ-TEST-12 | P2／P3／P5／P8 |
| AC-13 | 單棟資料損壞重置、其他棟不受影響 | T5 | **離線 PASS**（12 類損壞隔離測試）＋**待實機**（協定步驟 9 的可選破壞測試） | REQ-TEST-13 | P5／P8 |
| AC-14 | 1,000 棟符合 §17.3 全部效能門檻 | T4＋Profiler | **離線代理部分達標**：Core tick p95 **108.20 µs**、p99 **167.80 µs**（門檻 3 ms／6 ms）；1,000 棟存檔 gzip **31,238 B**（門檻 400 KiB）；序列化 3.05 ms／反序列化 7.30 ms（門檻各 2 s）＋**待實機**：Unity Profiler p95／p99、GC、FPS↓≤2、UI ≤1.0 ms/frame、存讀實測、記憶體 ≤32 MB | REQ-TEST-14、REQ-OPS-05、REQ-SAFE-08 | P7／P8 |
| AC-15 | 面板 ≤4 Hz、關閉後零背景更新、無每幀字串 | T4＋Profiler | **離線契約 PASS**（`BoilerPanelRefreshPolicy`／`Fingerprint`、髒標記測試、64 鍵 loc）＋**待實機**（Profiler） | REQ-OPS-01、REQ-OPS-02 | P4／P7／P8 |
| AC-16 | ≥54 鍵齊備、zhTW 無簡體、字元上限與 1.4× 假字串 | 腳本＋截圖 | **離線 PASS（鍵數／語系／placeholder）**：G12（64 鍵 ≥54、zhTW 無簡體、placeholder 一致）、G21（icon 24＋meta）＋**待實機**：1.4× 假字串與 zhTW/enUS 截圖（協定步驟 1、10） | REQ-UI-09、REQ-UI-10 | P1／P4／P6／P8 |
| AC-17 | 30 日平均有效輸出 ≥ 對照×0.95 | T4 模擬 | **離線 PASS**（`docs/BALANCE_REPORT.md`：12/12、四判準 4/4；最敏感大型 Mk.IV **+2.93%**）＋**待實機**：30 遊戲日實測（協定 §7） | REQ-TEST-15 | P7／P8 |
| AC-18 | 靜態與 Profiler 皆無每幀全圖掃描、無 `Update` 內 `FindObjects` | 程式碼審查 | **離線 PASS**：G15（133 原始檔、0 red-line）、G17（source facts）＋**待實機**：Profiler 期間審查（協定 §7） | REQ-OPS-07 | P6／P8 |
| AC-19 | 移除任一外部模組後既有存檔可載入、建築不消失 | T5 | **離線決策表 PASS**（12 情境第 12 項：模組移除後重算、不壞檔）＋**待實機**（協定步驟 10） | REQ-TEST-16 | P5／P8 |
| AC-20 | 維護材料鏈在兩陣營皆可取得（Log／Plank／Gear／MetalBlock） | T2＋V-08＋兩陣營配方／建築清單 | **離線部分**：材料全為既有物品、無新增物品（G11/G17）；**待實機**：兩陣營生產鏈與實際維護扣料（協定步驟 6、7、§7） | REQ-TEST-17（＋V-08） | P1／P8 |
| AC-21 | 動畫符合 C-9（零骨架、相位、LOD、VFX、機況四階）；100 棟 ≤0.5 ms/frame、GC 0、離屏 0 | AN-01～08＋錄影＋Profiler＋故障注入 | **未通過（未實作／未驗）**：P9 美術卡尚未交付，v1 模型與圖示為佔位（`timbermesh: 0`）；步驟 11／12 只能標 `N/A（P9 前）` | REQ-ART-11～22、REQ-OPS-05 | P9／P8 |

**AC 小結（誠實版）**

| 分類 | 數量 | AC |
|---|---:|---|
| 離線可判定且已 PASS | 5 | AC-01、02、03、16（鍵數／語系核心）、18（靜態掃描） |
| 離線 PASS、但仍需實機（T2–T6） | 15 | AC-04～15、17、19、20 |
| **未通過** | 1 | **AC-21**（P9 未交付） |

> 上表刻意不把「離線 PASS」與「實機 PASS」合併；需求 §17.4 的每一條都要在 T2–T6 有案例才算全綠。
> 因此 **AC-01～AC-21 尚未全綠**。其中 AC-16 的 1.4× 假字串、AC-18 的 Profiler 審查屬實機待確認項，
> 但核心腳本／靜態證據已離線 PASS。

---

## 3. T0–T6 測試層級現況

| 層級 | 範圍 | 自動化 | 目前狀態 | 證據／缺口 |
|---|---|---|---|---|
| **T0** | 純邏輯單元（輸出／燃料／水／磨損／維護／存檔／loc） | 100% | **PASS（522/522）**；**覆蓋率待補** | `SEE.Tests`；覆蓋率報告未重跑 |
| **T1** | 24 藍圖 schema／數值／loc／未覆寫原版 | 100% | **PASS** | G03／G05／G09／G14／G19／G20 |
| **T2** | 單棟實機（建造／供料／供水／輸出／維護／UI） | 0% | **待驗（0%）** | 僅有離線契約測試；須人類照協定步驟 1–9 |
| **T3** | 三模組 8 組合與降級 | 部分 | **待驗**（離線僅假探針 12 情境） | 協定步驟 10；12 情境見 `docs/COMPAT_MATRIX.md` |
| **T4** | 1,000 棟、30 日、存檔大小與載入 | 劇本化 | **待驗**（離線代理 ≠ 實機） | 協定 §7；`docs/PERF_BUDGET.md`、`docs/BALANCE_REPORT.md` |
| **T5** | 遷移與損壞（v1 round-trip、未知欄位、單棟損壞、modVersion） | 部分（codec） | **待驗（實機）** | 協定步驟 9；離線 codec 已 PASS |
| **T6** | 迴歸／共存（原版蒸汽機、New Building Styles、More Mines、舊 PL） | 0% | **待驗** | 協定 §7；V-10 |

> 需求 §17.1 的「三種覆蓋率不可互換」：需求覆蓋 100%、`SEE.Core` 行 ≥80%／分支 ≥70%、T0 全域 ≥70%。
> 本次只重跑功能測試，**三者都沒有新的覆蓋率報告**；發佈前必須補件，否則 Go/No-Go 第 6 條不成立。

---

## 4. 需求 §19.2 發佈 Go/No-Go 檢查表（六條）

> 判定規則：**離線可判定的門檻若全 PASS 才標 PASS；任何需實機／需簽核者一律標「待驗」，不得以推論代替。**
> 只要有一條不是 PASS，整體就是 **No-Go**；本表目前 **No-Go**。

| # | §19.2 條目 | 目前判定 | 依據／缺口 |
|---:|---|---|---|
| 1 | AC-01～AC-21 全綠，且 17.5 手動腳本由非開發者簽核 | **待驗（No-Go）** | AC-01～20 只有離線證據，T2–T6 未做；**AC-21 未通過**（P9 未交付）；17.5 十二步尚未簽核 |
| 2 | 三模組缺席與齊備四種情境下皆可完整遊玩 30 遊戲日 | **待驗** | 離線 12 情境（假探針）PASS；實機 8 組合／30 日未做（協定步驟 10、§7） |
| 3 | 24 藍圖與 loc 鍵零缺失；zhTW 無簡體；工作坊頁與文件用語一致 | **離線 PASS（部分）** | G03／G12／G19／G20／G22 全 PASS；工作坊文案仍為草稿（`docs/WORKSHOP.md`），**用語人工校對待完成**；P9 換圖前不得視為完成 |
| 4 | 無 P0／P1 缺陷；已知限制（V-01～V-10 相關）已寫入說明 | **待驗（部分）** | 離線未知 P0／P1（門禁全綠）；已知限制已寫入 README／本文件；**實機缺陷清單尚不存在**，需人類測後確認 |
| 5 | 版本號、存檔 schema、設定 schema 三者一致（0.1.0／1／1） | **PASS（離線）** | G22：manifest 0.1.0、save schema 1、config schema 1；`build-report.json` 一致 |
| 6 | §11.9.5 發佈前證據齊備（1,000 棟壓測、存檔量測、故障注入報告連結）；CI error 級門檻（T0／T1）全綠 | **待驗（部分）** | CI error 級（T0／T1）**PASS**（G01／G02／G05）；1,000 棟實機壓測、存檔實測、故障注入報告**未附**；覆蓋率報告**待補** |

### 4.1 整體結論

```text
Go / No-Go：NO-GO（不可發佈）
理由：離線門檻全 PASS，但 §19.2 第 1、2、4、6 條仍待實機／簽核；AC-21 未通過；
      17.5 十二步未由非開發者簽核；覆蓋率報告待補。
下一步：人類照 docs/TEST_PROTOCOL.md 完成 12 步＋1,000 棟壓測＋存檔量測，簽核後回填本表；
      同時 P9 交付動畫與自製模型，才能關閉 AC-21。
```

> **禁止事項**：不得把本表任何「待驗」改寫成「PASS」；不得在人類簽核前寫「已通過」或「可發佈／已發佈」；
> 不得為通過而放寬 G01–G22 或改平衡數值。這三點是需求 §19.2 與專案紅線。

---

## 5. 已知偏離與限制（必須隨版本說明）

| # | 項目 | 現況 | 影響／處置 |
|---|---|---|---|
| D-11／AD-11 | 存檔形態 | 世界節點 `Singletons.SEE` 只存 `schemaVersion`／`modVersion`／`checksum`／`buildings[]` 四索引欄位／`compat`；逐棟 7 欄存 per-entity `ComponentKey("SEE")` | 已由 Lead 有條件追認；需求 §14.1 字面 11 欄位屬登記偏離；P8 需驗證 singleton 時序（協定步驟 9） |
| D-12 | 同上（文件舊編號） | 舊文件用 D-12 指同一件事 | 以 AD-11 為準；不重複計算 |
| R-7 | 行數預算 | Core／Gameplay 超過 §11.5 的 1,200／2,500 行（含 XML doc），改列 soft target | 不作為驗收門檻；P6 只回報實際行數 |
| R-4 | 同 tick 多棟維護公平性 | v1 依註冊順序，condition 低者先的排序未做 | 已知限制；P8 觀察即可 |
| AD-1 | 圖示 | v1 為生成佔位圖示（非最終美術；非原版 steam engine icon） | P9 換自製 24 張；標「暫代」 |
| AD-2 | 模型 | FT＝`WaterWheel.Folktails`、IT＝`SteamEngine.IronTeeth` 佔位；視覺尺寸與佔地不一致、FT 無煙霧掛點 | P9 自製 39 mesh；建造以格線為準 |
| — | 動畫 | `timbermesh: 0`；`GoodPoweredGeneratorAnimatorSpec` 未輸出；P9 才交付 C-9 動畫與 LOD | **AC-21 未通過**；17.5 步驟 11／12 視覺項 N/A |
| — | `tank` 供水模式 | `WaterSupplyPolicy` 的 `TankAutoFillVerified=false`、`TankMixVolumeVerified=false`；TP 有裝時 `auto`→`valve` | V-01／V-TP2 待實機；工作坊文案與 README 已註明 |
| — | ConstructionBase | Small 用 `ConstructionBase2x2`，契約文字寫 `2x3` | validator WARN（非阻擋）；待 Lead 裁決，不影響載入 |
| — | 覆蓋率 | 本次未重跑；`SEE.Core` 80/70 與 T0 70% 無新報告 | 發佈前待補（REQ-NFR-07） |
| — | 平衡／效能 | P7 為離線模擬與離線代理；30 日實機、A/B 實測、Profiler 未做 | T4 待驗；不得把離線數字當實機 |
| — | 外部模組實機 | 12 情境只有假探針；TP 自動補水、PipeTank 混裝、PL 實際拉線、CB 帶端餵料未做 | T3 待驗；V-01～V-05 |

### 5.1 V-01～V-10 待證實清單現況

| 編號 | 內容 | 目前狀態 |
|---|---|---|
| V-01 | 自訂液體 `StockpileSpec` 是否被 Timber Pipes 自動補水 | **待實機**（協定步驟 10；v1 不預設 tank） |
| V-02 | 同棟「水艙＋燃料庫存」的 PipeTank 混裝體積誤差 | **待實機**；離線生成器閘門最大 10.5%（≤12%）僅為資料推導 |
| V-03 | `PowerLineSpec` 條件式注入與未安裝時安全性 | **離線 IL／邏輯 PASS**（非 Harmony 注入、失敗降級）；**實機拉線待驗** |
| V-04 | 輸送帶對接本建築的實際面與距離 | **待實機** |
| V-05 | 低供電時效率分配演算法（是否嚴格按比例） | **待實機**（λ 防回授公式已離線實作） |
| V-06 | 原版蒸汽機水庫存 15/16、可浸水、待機燃料行為 | **未測**（不影響本模組；T6 共存時記錄） |
| V-07 | 管網 headlift 2 m／管段 1 m³ 對高處引擎供水 | **待實機**（TP） |
| V-08 | 兩陣營是否都能生產 MetalBlock／Gear | **待實機**（AC-20；材料鏈檢查） |
| V-09 | 24 份藍圖載入與工具列排序實測 | **離線 PASS**（G19/G21）；**實機待驗**（協定步驟 1） |
| V-10 | `More Mines` 的 `TWSteamEngine`（750 hp）定位競爭 | **未測**（非 v1 阻擋；工作坊定位說明已預留） |

---

## 6. 人類簽核（空白，必須由非開發者填寫）

> 簽核前，本文件所有實機項（AC-04～AC-10、12～15、17～20 的實機部分，以及 AC-21）都維持「待驗」。
> 簽核後請回傳本表與 `docs/TEST_PROTOCOL.md` §5，由 Lead 更新狀態；**AI 不得代填**。

| 簽核項 | 範圍 | 結果（PASS／FAIL） | 證據 | 簽名／日期 |
|---|---|---|---|---|
| 17.5 十二步 | 步驟 1–10；11／12 可標 P9 前 N/A |  |  |  |
| T2 單棟 | 建造／供料／供水／輸出／維護／UI |  |  |  |
| T3 三模組 | 8 組合（至少 4）＋12 情境 |  |  |  |
| T4 壓測 | 1,000 棟、30 日、Profiler、存檔量測 |  |  |  |
| T5 存檔 | round-trip／損壞／移除模組 |  |  |  |
| T6 共存 | 原版蒸汽機、New Building Styles、More Mines、舊 PL |  |  |  |
| AC-21 動畫 | AN-01～08（P9 後） |  |  |  |

測試者（非開發者）簽名：＿＿＿＿＿＿＿＿＿＿　日期：＿＿＿＿＿＿＿＿
遊戲版本：＿＿＿＿＿＿＿＿＿＿＿＿＿＿　模組版本：0.1.0

---

## 7. 可重跑的離線證據

```powershell
cd "C:\Users\user\Downloads\timber steam engine"

# 全量門禁：clean → build → tests → generator --check → validator → stage（0 warning／PASS／22 gates）
powershell -NoProfile -ExecutionPolicy Bypass -File mod/build.ps1 -Clean

# 生成器一致性（0 diff）
dotnet run --project mod/tools/generate-blueprints -c Release -m:1 -- --check

# 平衡與效能報告（四判準、30 日、1,000 棟存檔／tick 代理）
dotnet run --project mod/tools/balance-sim -c Release -m:1

# 負向測試（控制組＋5 個故意破壞案例，全部必須 FAIL）
powershell -NoProfile -ExecutionPolicy Bypass -File mod/tools/validate/negative-tests.ps1
```

> 完整技術證據：`mod/docs/CONTRACT_SEE.md`、`mod/docs/API_FACTS_SEE.md`、`mod/docs/RELEASE_GATES.md`、
> `mod/docs/REQ_TRACEABILITY.md`；平衡／效能：`docs/BALANCE_REPORT.md`、`docs/PERF_BUDGET.md`；
> 相容：`docs/COMPAT_MATRIX.md`；打包與安裝：`docs/PACKAGE.md`；玩家文件：`README.md`、`CHANGELOG.md`、`docs/WORKSHOP.md`。
