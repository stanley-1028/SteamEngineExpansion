# SEE 自動化待辦任務（Task Board）索引

> 更新：2026-11-06 13:35（Asia/Macau）｜Board revision 見 `task_board_list`｜工作區 workspaceId＝`3e2a23d7-e6d5-415d-9e98-5fca7a14af97`（本 session workspace）
> 所有卡片狀態 `todo`、`goalRun=true`、權限 `workspace-write`；依下方順序逐卡 `task_board_run`，前一卡驗收未過不得跑下一卡。

## 目前狀態

| 卡片 | 狀態 |
|---|---|
| SEE-P0 | ✅ 完成（Lead 複驗 0 warning、204/204） |
| SEE-P1 | ✅ 完成：24 藍圖、生成器 5/5 閘門、CHECK PASS 0 diff、54 鍵×2 語系、24 icon |
| SEE-P2 | ✅ 完成：PASS 339/339 |
| SEE-P3 | ✅ 完成：控制器＋消耗帳本＋維護＋存檔橋；複驗 0 warning、PASS 383/383、VALIDATION PASSED；消耗對帳 booked≈0、30 日整數差 0.25%/0.17% |
| SEE-P4 | ✅ **完成（Lead 複驗收斂）**：面板六區＋四操作＋4 Hz；UI 只依賴 Core；loc 54 鍵；缺口 G1/G3/G4/G5 轉 P4B |
| SEE-P5 | ✅ **完成（Lead 複驗收斂）**：偵測＋3 適配器＋非 Harmony 的 `PowerLineSpec` 注入＋存檔整合；12 情境；`COMPAT_MATRIX.md` |
| SEE-P1B | ✅ 完成：JSON-only 包 77 檔 0 DLL、自訂 spec 0 命中、`docs/JSON_MVP_CHECKLIST.md` |
| **Lead 基線（20:32 複驗）** | ✅ `build.ps1 -Clean`：**0 warning**、**PASS 483/483**、generator 5/5、**VALIDATION PASSED**、staged 87 檔 595 KB |
| SEE-P4B | ✅ **完成（succeeded）**：view 擴充＋新 loc 鍵（54→**63**，enUS/zhTW diff 0）＋面板 G1/G3/G4/G5 關閉；UI 仍只引用 Core |
| SEE-P6 | ✅ **完成**：validator G01–G22 全 PASS、5 項負向測試、`tools/release/release-gates.json` |
| SEE-P7 | ✅ **完成**：`docs/BALANCE_REPORT.md`（12 檔全 PASS、四判準 4/4、大型 Mk.IV +2.93%）＋`docs/PERF_BUDGET.md` |
| **Lead 基線（21:15 複驗）** | ✅ `build.ps1 -Clean`：**BUILD PASSED**（G01–G22 全 PASS）、83 檔、561 KB、7 次總檢查、package sha256 `4c294c6a…`、loc 63×2、24 藍圖、22 項 gate 全綠 |
| SEE-P8 | ✅ **完成（Lead 接管交付）**：`docs/TEST_PROTOCOL.md`（17.5 12 步）、`docs/VERIFICATION.md`（AC-01～21）、`docs/PACKAGE.md`、`docs/WORKSHOP.md`、`README.md`、`CHANGELOG.md`；**已實裝到 `Documents\Timberborn\Mods\SteamEngineExpansion`**（83 檔、4 DLL、24 藍圖、63 鍵 loc） |
| SEE-P9 | todo（美術 39 mesh＋動畫；v1 目前用原版模型佔位） |

> **目前只剩人類實機 gate**：照 `docs/TEST_PROTOCOL.md` 跑一輪並回報；實機 AC（T2–T6，含 AC-14 1,000 棟）通過後才算 0.1.0 驗收完成。
> P4／P5 的 board 狀態曾是 `failed`（執行器 turn error），但其 session 續跑完成且 Lead 已用 `record-external-outcome` 收斂為 `done`（證據見卡片 summary）。
> 流程提醒：卡片不得改 `TASKS.md`（Lead 擁有）；P5 曾在 TASKS.md 追加狀態列，已由 Lead 合併。

## 卡片 ID 與依賴

| 順序 | 卡片 ID | 標題 | 前置 | 推理檔 |
|---:|---|---|---|---|
| 1 | `a199e4ca-9342-46e3-8cce-52c52f230a6d` | SEE-P0 技術取證＋MWP→SEE 骨架＋契約凍結 | — | max |
| 2 | `a803d445-ea3b-4272-8047-bc7033acac11` | SEE-P1 engines.json＋生成器＋24 藍圖＋loc／icon／manifest | P0 | max |
| 2.5 | `16b97d6f-11a5-42b3-8910-5045b66937bf` | SEE-P1B JSON-only 先行可玩包＋人類 5 項檢查 | P1 | high |
| 3 | `75892f61-2d7f-47bc-afeb-1aaae637538e` | SEE-P2 SEE.Core 純邏輯＋L1 測試 | P0 | max |
| 4 | `03295a83-1d64-4548-bb51-c9feac9ee4ca` | SEE-P3 SEE.Gameplay 控制器＋燃料／水雙軌＋動力／維護／存檔 | P1、P2 | max |
| 5 | `bf2d7d00-1d37-4237-91ff-ddfaf4c14bf3` | SEE-P4 SEE.UI 面板＋狀態徽章＋54 鍵 loc | P3 | high |
| 6 | `5eb907ac-f71b-4138-8fd5-3748713443de` | SEE-P5 相容層：TP／PL／CB 偵測＋供水模式＋存檔序列化 | P3 | high |
| 7 | `1913d1e7-c5f9-46d5-a37d-35ee16a72d41` | SEE-P6 驗證器＋build.ps1 門禁＋發布 gates | P1、P3、P4、P5 | high |
| 8 | `b4054b1d-7306-4884-a885-5ff49b8cd898` | SEE-P7 30 日平衡模擬＋四判準＋效能預算（離線） | P2 | max |
| 9 | `02da857d-73ce-40a7-804f-ad5176d37c2d` | SEE-P8 打包＋安裝＋人類實機驗收 gate | P6、P7 | high |
| 10 | `10e92dce-1a50-423c-808d-010c648a6723` | SEE-P9 美術：39 mesh＋LOD＋24 圖示＋spec 綁定動畫 | P8（可並行） | high |

## 平行波次（寫入範圍互斥才可同時跑）

```
Wave 0：P0
Wave 1：P1 ∥ P2
Wave 1.5：P1B（P1 完成即可；最早的人類實機回饋）
Wave 2：P3
Wave 3：P4 ∥ P5 ∥ P7（P7 只需 P2）
Wave 4：P6
Wave 5：P8（＋P9 可並行）
```

## 每卡完成的「真實輸出」要求

- `pwsh -File mod/build.ps1 -Clean`：0 warning、`PASS n/n`、`VALIDATION PASSED`
- 生成器 `--check` 零 diff；24 藍圖 12 欄位零誤差；loc 54 鍵
- 消耗對帳 ≤ ±2%；12 相容情境；8 組狀態優先序；30 日四判準
- 契約變更一律走 `mod/docs/CONTRACT_SEE.md` 的「待 Lead 裁決」區；卡片不得自行改 PLAN.md／需求數值

## 人類 gate（不可由 AI 宣告通過）

- **SEE-P1B**：JSON-only 包安裝後的人類 5 項檢查（工具列／佔地／供電／補料／存讀檔）——最早回饋點。
- **SEE-P8**：AC-01～AC-21 實機項（T2–T6）必須由人類照 `mod/docs/TEST_PROTOCOL.md`（需求 17.5 的 12 步）執行並簽核。
- P8 安裝到 `C:\Users\user\Documents\Timberborn\Mods\SteamEngineExpansion` 需一次性權限升級（沙盒外寫入）。
- 本工作階段 batchmode 無法進遊戲（feasibility §4.7 證實）；實機一律用正常桌面＋已登入 Steam，並可用 `_investigate/probe/Invoke-LoadSmoke.ps1` 做 L3 載入冒煙。

## 已知修正（feasibility）

- 1.1.2.8 **沒有** `Timberborn.Core.dll`／`Timberborn.Gameplay.dll`；`IModStarter` 在 `Timberborn.ModManagerScene`、`Configurator` 在 `Bindito.Core`。
- 沒有 `InventorySpec`（用 `SimpleOutputInventorySpec`）、沒有 `IPanelModuleProvider`（用 `IProvider<EntityPanelModule>`）、沒有 `BlueprintModifier`（用 `IBlueprintModifierProvider`）。
- `MechanicalNode.SetOutputMultiplier` 存在 → 輸出調變 A 案可行；`MechanicalBuilding.SetConsumptionDisabled` 存在。
- `manifest.json` 只有 7 欄（無 `Assemblies`）；loc CSV 必須 3 欄、含逗號要引號。
