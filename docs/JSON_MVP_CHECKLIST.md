# SEE JSON MVP — 人類 5–8 分鐘實機檢查清單

> 卡片：SEE-P1B（JSON-only 先行可玩包）｜日期：2026-11-06｜遊戲：Timberborn 1.1.2.8
> 這份清單給「人類」用：AI 不宣告實機通過，5 項（＋1 可選）結果由你回報後才算數。
> 套件位置：`mod\build\SEE-JSON-MVP\`（77 檔、約 322 KB、**零 DLL**）

## 這是什麼（30 秒）

「SEE JSON MVP」是**零 C#** 的先行切片：24 份資料驅動藍圖（3 種尺寸 × 4 檔位 × 2 陣營），
拿掉所有需要程式才能解析的自訂 spec，只留原版 spec。目的是在寫程式前先驗證：
**藍圖幾何／陣營材質／工具列排序／供電／補料**。它不是正式 SEE：沒有維護、沒有比例耗燃料、
沒有水消耗、沒有自訂面板。**請不要用「面板沒出現」判定失敗。**

## 先決條件（30 秒）

- [ ] Steam 已登入，遊戲可正常進主選單。
- [ ] `Documents\Timberborn\Mods` 內**不要**同時放 `SteamEngineExpansion`（正式 SEE）；本 MVP 沿用同一組
      `TemplateName` 與藍圖路徑，兩個一起裝會撞名。
- [ ] 準備一個「已解鎖 Power 分類」的存檔（或開發者模式），以及至少 20 個 Log、2 名搬運工。
      沒有解鎖的話，建造選單只會看到鎖頭，仍可先檢查清單①。

## 安裝（1 分鐘）

**方式 A（建議，一般桌面 PowerShell）：**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Users\user\Downloads\timber steam engine\mod\tools\stage-json-mvp\stage-json-mvp.ps1" -Install
```

**方式 B（手動複製）：**

```powershell
$src = "C:\Users\user\Downloads\timber steam engine\mod\build\SEE-JSON-MVP"
$dst = "$env:USERPROFILE\Documents\Timberborn\Mods\SteamEngineExpansionMVP"
New-Item -ItemType Directory -Force -Path $dst | Out-Null
Copy-Item "$src\*" $dst -Recurse -Force
```

- [ ] 啟動遊戲 → Mods（模組清單）→ 看得到 **SEE JSON MVP（v0.1.0-mvp）** 且沒有錯誤訊息。
- [ ] 若清單沒有它，先確認資料夾名是 `SteamEngineExpansionMVP`、`manifest.json` 在資料夾根。

## 移除（30 秒）

先完全退出遊戲，再二選一：

```powershell
# 直接刪資料夾（或先在遊戲 Mods 介面停用）
Remove-Item -Recurse -Force "$env:USERPROFILE\Documents\Timberborn\Mods\SteamEngineExpansionMVP"
```

- [ ] 重啟遊戲，Mods 清單不再出現 SEE JSON MVP。

## 五項檢查（每項約 1 分鐘）

> 每一項都寫「操作／預期／失敗要記錄什麼」。任何一項失敗先不要刪包，直接回報。

### ① 工具列：Power 群組與排序

**操作**：進一局已解鎖 Power 的遊戲 → 下方建造列 → Power（動力）分類。
**預期**：
- 當前陣營看得到 **12 檔**（另一陣營要另開一局，合計 24 檔）：Small/Medium/Large × Mk.I～Mk.IV。
- 新項目排在**原版蒸氣機（Steam Engine）之後**（ToolOrder 110–132，原版是 50）。
- 名稱顯示正常（例：Small Steam Engine Mk.I／小型蒸汽引擎 Mk.I），不是 raw key。
**失敗記錄**：Mods 清單截圖、Power 群組截圖、`Player.log` 中含 `SteamBoiler`／`Exception`／`Blueprint` 的行。

### ② 建造：兩陣營、佔地與高度

**操作**：
- Folktails 局：建 **Small T1** 與 **Large T1**。
- Iron Teeth 局：建 **Medium T2**。
- 用格線（Grid）目視底部佔地與高度，不要只看模型外型。
**預期**：
- 兩陣營都能成功放置、施工、完工（無 red box、無 exception）。
- 佔地／高度：Small **2×2×3**、Medium **3×3×3**、Large **4×4×4**（X 寬 × Y 深 × Z 高）。
- ⚠ 模型是原版佔位（FT＝WaterWheel、IT＝SteamEngine），**視覺大小可能與佔地不一致**；以格線為準。
**失敗記錄**：放置紅框／格線截圖、建造選單縮圖、`Player.log` 的 `ArgumentException` 或 `Blueprint` 段落。

### ③ 供電：接耗電建築後出現額定 hp

**操作**：用傳動軸把鍋爐接到任一耗電建築（例：伐木廠、磨坊），打開「動力」覆蓋層或用滑鼠看電網。
**預期**：電網供給出現該檔 hp（例：Small T1 FT=130／IT=137；Medium T2 FT=460／IT=483；Large T1 FT=360／IT=378）。
- MVP 沒有輸出調變，供給恆為額定值；**需求為 0 時也不會停機**（已知限制，不算失敗）。
**失敗記錄**：動力覆蓋層截圖（供給／需求數字）、`Player.log`。

### ④ 補料：搬運工補 Log、庫存滿即停收

**操作**：確認該區有 Log；觀察搬運工把 Log 送進鍋爐，等一段時間看是否停止補給。
**預期**：
- Log 被搬進燃料庫存，庫存滿後不再有搬運工送料。
- 燃料會持續被消耗（原版 `GoodConsumingBuilding` 行為）；MVP 的燃料庫存容量較小（T1 1–3、T2 2–5、T3/T4 1–3），很快滿。
- MVP 沒有自訂面板；請看建築本身的庫存顯示／搬運動畫，不要期待 SEE 面板。
**失敗記錄**：建築庫存顯示或搬運動畫截圖、`Player.log` 中 `GoodConsumingBuilding`／`Inventory`／`Exception`。

### ⑤ 存檔往返：建築與庫存還在

**操作**：存檔 → 回主選單 → 載入同一存檔。
**預期**：建築位置、完工狀態、燃料庫存都保留；電網供給恢復。
**失敗記錄**：重進前後對照截圖、`Player.log` 載入段落（`Exception`／`Blueprint`／`Inventory`）。

### ⑥（可選）水艙與 Timber Pipes：V-01 第一次觀察

**操作**：先裝 Steam 工作坊的 Timber Pipes（3801726769），把管線接到鍋爐，看水艙是否進水、容量顯示多少。
**預期（兩種結果都要記錄）**：
- MVP 沒有 `SingleGoodAllower.Allow("Water")` 控制器，水艙**可能顯示 0 容量、不被補水**；這是預期限制，不算失敗。
- 若真的進水，記錄容量數字與來源（管路／搬運工），作為 V-01 的第一筆實機資料。
**失敗記錄**：水艙面板／管線截圖、是否進水、容量數字、`Player.log`。

## 失敗時請帶回（四件）

1. Mods 清單截圖（要有版本字樣）。
2. 失敗那一項的畫面截圖（建造選單／格線／動力覆蓋層／庫存）。
3. `Player.log` 尾段（預設：`%USERPROFILE%\AppData\LocalLow\Mechanistry\Timberborn\Player.log`）；
   可先複製到桌面再附上：
   ```powershell
   Copy-Item "$env:USERPROFILE\AppData\LocalLow\Mechanistry\Timberborn\Player.log" "$env:USERPROFILE\Desktop\SEE_MVP_Player.log" -Force
   ```
4. 一行結果：`①OK ②OK ③OK ④OK ⑤OK ⑥(可選)…`，失敗項註明「預期 vs 實際」。

## 時間預算（總計 5–8 分鐘）

| 步驟 | 時間 |
|---|---|
| 安裝＋進 Mods 清單 | 1 分 |
| ① 工具列 | 30 秒 |
| ② 建造（3 棟） | 2 分 |
| ③ 供電 | 1 分 |
| ④ 補料 | 1–2 分 |
| ⑤ 存讀檔 | 1 分 |
| ⑥（可選）水艙 | 1 分（沒有 Timber Pipes 可跳過） |
