# balance-sim（SEE-P7 離線平衡與效能模擬器）

`mod/tools/balance-sim` 是 SEE 的**建置期證據工具**，不是遊戲程式：它不會被打包、不會被遊戲載入，
也不寫入任何檔案。目的是用**與模組同一份公式**（`SEE.Core` 的 `BoilerFormulas`／`ConditionCurve`／
`BoilerStateMachine`）重算需求文檔 §5.5 的 30 日平均輸出、四條 §5.2 判準、§5.1 A/B 對照與
1,000 棟存檔／效能的離線預算。正式報告見 `docs/BALANCE_REPORT.md`、`docs/PERF_BUDGET.md`。

## 執行

```powershell
# 從工作區根目錄
dotnet run --project mod/tools/balance-sim -c Release -m:1

# 指定模組根目錄（含 data/engines.json 的資料夾）
dotnet run --project mod/tools/balance-sim -c Release -m:1 -- --mod mod
```

- `-m:1`：沿用本專案建置規則（平行 MSBuild 在此機器會靜默失敗）。
- 退出碼：四條判準全過 **0**；任一判準 FAIL 或資料對帳失敗 **非 0**。
- 零 NuGet、`net9.0`、唯一 `ProjectReference` 指向 `SEE.Core`（因此不會出現「工具一套公式、
  遊戲另一套公式」的漂移；requirements §11.7）。

## 輸入／輸出

| 輸入 | 用途 |
|---|---|
| `mod/data/engines.json` | 12 檔權威數值（唯一數值源） |
| `mod/data/engines.derived.json` | P1 產物：24 個陣營變體、5 道閘門、`sourceSha256` |

工具會先做資料對帳（`engines.json` 的 SHA-256 與 derived 的 `sourceSha256`、tier 費率推導、
容量公式、維護間隔、陣營推導、24 變體與 5 道閘門），任一不符即中止並以非 0 退出；**永不修改**
`data/**`（P7 只讀；數值調整屬資料卡）。

輸出章節（Console）：

0. 資料來源與對帳
1. §4.3 派生指標＋REQ-BLD-09 容量（含大型 Mk.I 水艙 58）
2. 判準 ① 滿載輸出
3. 判準 ② 每 100 hp 燃料／水（含 ±8% 捨入、T1 燃料上限 1.6% 餘裕）
4. 判準 ③ 30 日平均（L=1.0、維護停機 2 h、6/8/10/12 日）
5. 判準 ④ 規模效應（研究點/hp、BCI/hp 單調下降且 ≥ 小型 Mk.I 的 40%）
6. A/B 對照（含大型 Mk.IV：1.00×、1.56× 燃料優勢、64 vs 101 格、+2.9% 餘裕）
7. 陣營 2 位小數量化偏差（文件值 2.0%–10.9%；實算範圍與差異說明）
8. 1,000 棟存檔估算（350B/250B → zip 1/5、≤400 KB、寫入／載入 ≤2 s、64 B/棟）
9. 效能微基準與 1,000 棟 tick proxy（p95/p99 對照 3 ms／6 ms，**離線代理**）
10. 總結與退出碼

## 邊界（不可宣稱）

- 章節 8／9 是**離線估算／代理量測**：`System.Text.Json`、桌面 .NET 9 JIT、無 Unity、無遊戲系統、
  無 Profiler marker。1,000 棟的真實 p95／p99、GC、FPS、存檔 zip 大小與耗時，一律以 **P8 人類
  gate** 的 Unity Profiler 量測為準；工具輸出已標 `OFFLINE PROXY ONLY`。
- 平衡判準 ③ 的「對照方案」以額定輸出計、不替原版打折（需求刻意保守）；真實 30 日對照量測
  （水車枯水、風機無風）屬 P8。
