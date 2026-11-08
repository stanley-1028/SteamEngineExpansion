# SEE 相容矩陣（12 情境 × 期望結果 × 實測狀態）

> 對應：需求 §10.8／§17.2、REQ-COMPAT-01～18；CONTRACT_SEE §6.3（save `compat` 區塊）、§8.6。
> 自動化：`mod/tests/SEE.Tests/CompatScenarioTests.cs`（以 `ICompatProbe` 假探針驅動，**不載入真 DLL**）；
> 離線存檔：`IntegrationSaveTests.cs`；注入片段：`PowerLineInjectionTests.cs`。
> 證據：`mod/docs/API_FACTS_SEE.md` §9、`_investigate/p5/`。
> 圖例：**PASS（離線）**＝自動化測試已通過；**待實機**＝需 P8 人類在遊戲內驗證，AI 不得宣告通過。
> 執行日：2026-11-06｜測試結果：`dotnet run --project mod/tests/SEE.Tests -c Release -m:1` → `PASS 483/483`。

## 1. 12 情境表

| # | 情境 | 偵測（TP／PL／CB） | 供水（effective） | 供燃料 | 拉線 | 核心可玩 | 自動化結果 | 待實機 |
|---:|---|---|---|---|---|---|---|---|
| 1 | 無任何外部模組 | Absent／Absent／Absent | `hauler`（`auto-hauler:no-timber-pipes`） | 搬運工（燃料 GCB＋獨立 `WaterTank` public input） | 原版軸／相鄰 | ✅ | **PASS（離線）** | 12 檔可建可運轉、搬運工補料補水 |
| 2 | 只裝 Timber Pipes | Available／Absent／Absent | `valve`（`auto-valve:tp-evidence-pending`；V-01/V-TP2 未過前不給 `tank`） | 搬運工 | 原版軸 | ✅ | **PASS（離線）** | V-01 自動補水、V-TP2 混裝體積 |
| 3 | 只裝 Conveyor Belts | Absent／Absent／Available | `hauler` | 帶子或搬運工（`InputGoods` 已含 Log／Plank） | 原版軸 | ✅ | **PASS（離線）** | 帶端餵料、帶網斷電→`NoFuel` |
| 4 | 只裝 Power Lines | Absent／Available／Absent | `hauler` | 搬運工 | 執行期注入成功→可拉線；失敗→原版軸 | ✅ | **PASS（離線）** | V-PL1／V-03：實際可拉線與供電 |
| 5 | TP＋CB | Available／Absent／Available | `valve` | 帶子 | 原版軸 | ✅ | **PASS（離線）** | 水／燃料接口面不衝突；同格鋪設 |
| 6 | TP＋PL | Available／Available／Absent | `valve` | 搬運工 | 電纜 | ✅ | **PASS（離線）** | 管線＋電纜同棟 |
| 7 | CB＋PL | Absent／Available／Available | `hauler` | 帶子 | 電纜 | ✅ | **PASS（離線）** | 帶子耗電與引擎共享機械圖 |
| 8 | 三者皆裝 | Available／Available／Available | `valve` | 帶子＋搬運工 | 電纜 | ✅ | **PASS（離線）** | 全增強；工具列排序 |
| 9 | 另裝舊版 `Opolame.PowerLines` | 只提示；PL 為 Absent | `hauler` | 搬運工 | 不支援、不 patch | ✅ | **PASS（離線）** | 面板只提示、不整合 |
| 10 | TP／CB 前置缺失導致未載入 | Absent／Absent／Absent | `hauler`（`auto-hauler:no-timber-pipes`） | 搬運工 | 原版軸 | ✅ | **PASS（離線）** | 無錯誤彈窗、自動降級 |
| 11 | 模組在但錨點缺失（版本不符） | Degraded（視同 Absent） | `hauler`（`auto-hauler:integration-degraded`） | 搬運工 | 不注入 | ✅ | **PASS（離線）** | 面板顯示「整合不可用」 |
| 12 | 存檔帶模組、載入時模組已移除 | 全部 Absent（重新偵測） | 重算→`hauler`；玩家 `tank` 意圖保留但顯示原因 | 照舊（原版庫存） | 重算 | ✅ | **PASS（離線）** | 不壞檔；水／燃料貨物保留 |

> 12 情境的「核心可玩」皆依賴原版 MUST 路徑（燃料 `GoodConsumingBuildingSpec` 庫存、程式建立的
> `WaterTank` public input 庫存、`MechanicalNodeSpec`＋transput），外部模組只增加 SHOULD 便利；
> 任一情境都不會讓 SEE 變成硬依賴。2026-11-08 修根因後水艙不再用液體 `StockpileSpec`（避開
> `NoHaulingPostStatus` 雙初始化崩潰），因此 TP 的液體管網仍無法接管水艙。

## 2. 偵測與降級規格（實作對照）

| 項目 | 實作 | 狀態 |
|---|---|---|
| 三段式偵測 | 程式集簡名（容忍 `Timber Pipes`／`Timber_Pipes` 正規化）→ 型別錨點（`Assembly.GetType`，不建構）→ 版本閘門 | 已實作 |
| 錨點（以 DLL 證實） | TP：`TimberPipes.Specs.BuildingPipeSpec`／`PipePortState`／`Models.PipeFluids`；PL：`PowerLines.Specs.PowerLineSpec`／`Components.PowerLineComponent`；CB：`ConveyorBelt.Specs.BeltSystemSpec`／`Components.BuildingBeltTarget` | **已證實（離線 metadata）** |
| 版本 | 同 major band 低於 manifest 最低 → `Degraded`；`1.0.0+hash` 與 `11.x` 不同方案 → 只記提示 | 已證實；待實機無 |
| 預算／快取 | 總預算 `compat.detectionBudgetSeconds`（預設 2s）；一次載入一次掃描；手動 `Refresh()` 重掃 | 已實作＋測試 |
| 日誌 | 每模組只記一次 `SEE/Compat`（30 秒節流）：`SEE/Compat/Scan` 摘要；失敗鍵只記鍵不記堆疊 | 已實作 |
| 失敗語意 | `Absent`＝未安裝；`Degraded`＝視同未安裝；`Available` 才啟用；全程 try/catch、不拋例外 | 已實作＋測試 |
| 舊版 PL | `Opolame.PowerLines` 只提示、不整合、不 patch | 已實作＋測試 |
| 注入方式 | 非 Harmony：`IBlueprintModifierProvider.GetModifiers` 回傳 JSON 片段，`SpecService.Deserialize`→`AddJson`→`ReadJsons`→`JsonMerger.Merge` 併入藍圖 | **IL 已證實**；待實機（DI provider 收集＋拉線） |
| Harmony | **未引入**：無編譯期參考、無 `Harmony` 實例；`OptionalPatchHost` 單次嘗試後永久關閉（`closed:non-harmony-injection-proven`） | 已實作＋測試 |

### 2.1 錨點／版本 dump 摘要

| 模組 | DLL | manifest 版本 | assembly informational | 組件 |
|---|---|---|---|---|
| Timber Pipes | `...\3801726769\version-1.1\TimberPipes.dll` | 11.4.0 | 1.0.0+hash | `TimberPipes` |
| Power Lines | `...\3766893705\version-1.1\PowerLines.dll` | 11.1.1 | 1.0.0+hash | `PowerLines` |
| Conveyor Belts v2 | `...\3815376360\version-1.1\ConveyorBelt.dll` | 11.4.0 | 1.0.0+hash | `ConveyorBelt`、`ConveyorBelt.Algorithm` |

## 3. 供水模式決策（§7.7／§10.3.1）

| 玩家意圖 | 目前偵測 | 生效模式 | 原因鍵 | 玩家提示 |
|---|---|---|---|---|
| `auto` | TP 未安裝／Degraded | `hauler` | `auto-hauler:no-timber-pipes`／`auto-hauler:integration-degraded` | 否 |
| `auto` | TP Available（V-01/V-TP2 未過） | `valve` | `auto-valve:tp-evidence-pending` | 否 |
| `auto` | TP Available 且 V-01/V-TP2 通過（P8 後） | `tank` | `auto-tank` | 否 |
| `tank` | TP 不可用 | `hauler`（意圖保留） | `intent-tank-unavailable:fallback-hauler` | **是** |
| `valve` | TP 不可用 | `hauler`（意圖保留） | `intent-valve-unavailable:fallback-hauler` | **是** |
| `valve` | TP Available | `valve` | `intent-valve` | 否 |
| `hauler` | 任意 | `hauler` | `intent-hauler` | 否 |

- 逐棟意圖值以 P3 的 per-entity `waterSupplyMode` 持久化（CONTRACT_SEE §6.2）；P5 只計算「生效值」與原因，**不回寫意圖**，避免存檔反覆翻轉。
- 切換模式不清空水艙、不重建建築、不改 `condition`（由 P3 控制器保證）。
- `tank` 目前**不開啟**：`WaterSupplyPolicy.TankAutoFillVerified=false`、`TankMixVolumeVerified=false`，等 P8 的 V-01／V-TP2。

## 4. 存檔相容（P5 橋接，D-6／AD-11）

| 項目 | 結果 | 測試 |
|---|---|---|
| 世界節點 round-trip（schemaVersion／modVersion／checksum／buildings[] 4 欄／compat） | 位元組穩定 | `IntegrationSaveTests` |
| checksum 不符 | 記 Warn、不拒絕載入、不覆寫 | 同上 |
| `schemaVersion=99` | 唯讀降級、資料保留 | 同上 |
| 12 類損壞 | 單棟／單欄位修復，不整檔拒絕 | 同上＋P2/P3 codec 測試 |
| 未知欄位 | top-level／row 皆保留 | 同上 |
| 重複／孤兒 entityId | 保留第一筆／跳過非 GUID | 同上 |
| 增減外部模組 | `compat` 由本次偵測重算（`CompatSaveIntegrator.PostLoad` → `RecordCompatSnapshot`） | 同上 |
| 1,000 棟 | 未壓縮 JSON <400 KB，round-trip 無損 | 同上 |

## 5. 重現指令

```powershell
dotnet build mod/src/SEE.Integration/SEE.Integration.csproj -c Release -m:1
dotnet run --project mod/tests/SEE.Tests/SEE.Tests.csproj -c Release -m:1
# 只看 P5 測試
dotnet run --project mod/tests/SEE.Tests/SEE.Tests.csproj -c Release -m:1 -- --list | Select-String "Scenario|P5 |PowerLine|Water "
```
