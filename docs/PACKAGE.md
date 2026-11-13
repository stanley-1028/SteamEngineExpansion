# SEE 可重現安裝包（Steam Engine Expansion v0.1.0）

> **2026-11-14 熱修-6（UI／GUI 原版化＋Steam Engines 工具組）**：SEE 面板改原版外觀（九宮格卡片、原生按鈕/開關/進度條/下拉框；Unity Foldout 改原版風格收合標題，AD-21）；24 檔改專屬底部工具組 `SteamEngines`（Order 72，可展開/收合，圖示 `ToolGroupIcons/SteamEngines`，AD-22）；loc 63→64（AD-23）。
> **2026-11-08 熱修-5（搬運工需求狀態＋耗水）**：玩家回報「沒有搬運工就顯示不能運轉、耗水太大」——完工鍋爐改以 public `NoHaulingPostStatus.Disable()` 關閉原版搬運工需求狀態（AD-18；無 Harmony、不刪燃料 GCB）；全 12 檔耗水率減半（AD-19）並依新耗水率重算水艙（`waterTankHours` 仍 200）；生成器閘門 1 改為「水艙自主時間 ≥8× 燃料庫存自主時間」（AD-20）。
> **2026-11-08 熱修-4（夜間待機運轉）**：玩家回報「steam engine 晚上會停機」——根因＝入夜後原版 `WorkplacePowerConsumptionSwitch` 停用耗電端使 `Graph.PowerDemand=0`，控制器原本判為 `Idle`。新增 `balance.standbyLoadRatio`（預設 0.10、範圍 0–1），只要燃料＋水充足且未暫停／維護／機況 0，夜間以 10% 待機運轉、不再熄火；設 0 可恢復舊行為。
> **2026-11-08 熱修-3（UI）**：SEE 面板預設展開（標題含狀態）、新增 zhCN 63 鍵本地化；build gate 改為 enUS／zhTW／zhCN 三語系驗證。
> **2026-11-08 熱修-2**：搬運修根因——燃料只留 `GoodConsumingBuildingSpec`，水艙改為程式建立的 `WaterTank` public input 庫存（`SEEWaterInventoryInitializer`），普通搬運工可補水＋補燃料；不再使用 `StockpileSpec`（避開 `NoHaulingPostStatus` 雙初始化崩潰），tank/valve 暫不支援（`auto`→`hauler`）。
> 最新離線門禁（2026-11-14 熱修-6）：`build.ps1 -Clean` → **BUILD PASSED（G01–G22 全綠）**、**135 檔、2,385,102 bytes、48 `.timbermesh`**、package sha256 `9977ed808df154171913eaefc76a05087f75af1cb75989adccb2afa58699cca3`；loc 64 鍵 × enUS/zhTW/zhCN；staged 成品與來源逐檔 SHA-256 0 mismatch。

> 卡片：SEE-P8（打包／安裝／人類實機驗收 gate）｜產出日：2026-11-06｜遊戲基準：Timberborn 1.1.2.8（manifest 最低 1.1.0.0）
> 權威來源：`mod/build/build-report.json`、`mod/build/SteamEngineExpansion.sha256`、`mod/data/engines.json`
> **本文件只描述「已建置、已離線驗證、已安裝」的成品，不宣告任何實機行為通過，也不宣告已發佈。**
> 實機驗收見 `docs/TEST_PROTOCOL.md` 與 `docs/VERIFICATION.md`（AI 不得自行判定通過）。

---

## 1. 可重現建置

```powershell
# 本機沒有 PowerShell 7，使用 Windows PowerShell 5.1（build.ps1 明示支援 5.1、保持純 ASCII）
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Users\user\Downloads\timber steam engine\mod\build.ps1" -Clean

# 若環境有 pwsh 7，等價指令：
pwsh -File "C:\Users\user\Downloads\timber steam engine\mod\build.ps1" -Clean
```

本次（2026-11-08 熱修-5）真實輸出摘要：

| 項目 | 結果 |
|---|---|
| 四支出貨程式集建置 | **0 個警告、0 個錯誤**（G01） |
| 離線測試 runner | **PASS 522/522**（G02；2026-11-14 熱修-6 後） |
| 生成器 `--check` | **CHECK PASS（0 differences, 83 files, 24 blueprints）**（G03） |
| 生成器 5 道資料閘門 | **5/5 PASS**（G04；gate 1＝水艙自主時間 ≥8×，AD-20） |
| validator | **VALIDATION PASSED**；13/13 gate group（1 WARN、0 error）（G05） |
| 發佈門禁 G01–G22 | **22/22 PASS** |
| 成品 | **BUILD PASSED**：135 檔、2,385,102 bytes（2026-11-14 熱修-6 後） |
| 套件雜湊（`build-report.json` 的 `packageHash`） | `9977ed808df154171913eaefc76a05087f75af1cb75989adccb2afa58699cca3` |
| 逐檔雜湊清單 | `mod/build/SteamEngineExpansion.sha256`（135 行） |
| 負向測試 | **NEGATIVE TESTS PASSED（control + 6/6）**（2026-11-08 熱修-5 重跑） |
| 平衡模擬 | **BALANCE SIM PASS 12/12（四判準 4/4）**（AD-19 新耗水率） |

已知非阻擋警告（已記錄、不影響 G01–G22）：

```text
WARN: [table] ConstructionBase recorded deviation (contract §4.5/§4.8):
      Small uses ConstructionBase2x2#nested (contract XxZ expects 2x3)
```

> 說明：小型鍋爐使用原版 `ConstructionBase2x2`（佔地 2×2），契約文字寫 `2x3`；此為 P1 已回報的
> 文件口徑差異，`ART_PLACEHOLDER.md` 與 `RELEASE_GATES.md` 均有記錄，待 Lead 裁決，不阻擋載入。

---

## 2. 成品位置與結構

```text
C:\Users\user\Downloads\timber steam engine\mod\build\SteamEngineExpansion\
├── manifest.json
├── SEE_Config.json
├── SEE.Core.dll
├── SEE.Gameplay.dll
├── SEE.Integration.dll
├── SEE.UI.dll
├── Buildings\Power\SteamBoiler\
│   ├── README.md（GENERATED 告示）
│   ├── SteamBoiler.<Small|Medium|Large>.<T1|T2|T3|T4>.<Folktails|IronTeeth>.blueprint.json（24 份）
│   └── SteamBoiler.<...>.Model.timbermesh（24 完成）＋ .ConstructionStage0.Model.timbermesh（24 施工灰盒）
├── TemplateCollections\
│   ├── TemplateCollection.Buildings.Folktails.blueprint.json
│   └── TemplateCollection.Buildings.IronTeeth.blueprint.json
├── BlockObjectToolGroups\
│   └── BlockObjectToolGroup.SteamEngines.blueprint.json（專屬建造分類，AD-22）
├── ToolGroupIcons\
│   ├── SteamEngines.png（128×128）
│   └── SteamEngines.png.meta.json（{"isSprite": true}）
├── Localizations\
│   ├── enUS.csv（64 鍵）
│   ├── zhTW.csv（64 鍵）
│   └── zhCN.csv（64 鍵）
└── Icons\
    ├── SteamBoiler<Size><Tier><Faction>.png（24 張）
    └── 同名 .png.meta.json（24 份，內容 {"isSprite": true}）
```

| 目錄 | 檔案數 | bytes |
|---|---:|---:|
| 根目錄（manifest.json、SEE_Config.json、4 DLL） | 6 | 233,082 |
| `Buildings\Power\SteamBoiler\` | 73 | 1,597,778 |
| `Icons\` | 48 | 538,821 |
| `Localizations\` | 3 | 12,299 |
| `TemplateCollections\` | 2 | 2,106 |
| `BlockObjectToolGroups\` | 1 | 204 |
| `ToolGroupIcons\` | 2 | 812 |
| **合計** | **135** | **2,385,102** |

**出貨白名單**（G16／G18）：恰 4 個 DLL（`SEE.Core`／`SEE.Gameplay`／`SEE.Integration`／`SEE.UI`）；
不含 `SEE.Tests.dll`、`.deps.json`、`.pdb`、`src/`、`tools/`、`tests/`、`engines.json`、`engines.derived.json`、`loc_keys.json`。
本次已用檔案清單核對：**src/tools/tests 命中 0**。

### 2.1 manifest.json（原樣）

```json
{
  "Name": "Steam Engine Expansion",
  "Version": "0.1.0",
  "Id": "SteamEngineExpansion",
  "MinimumGameVersion": "1.1.0.0",
  "Description": "Twelve worker-free steam engines: three sizes (Small/Medium/Large) across four tiers (Mk.I-Mk.IV), buildable by both Folktails and Iron Teeth. Each engine consumes Log fuel and Water, produces mechanical power, and needs periodic maintenance. Fuel stock, water tank and maintenance values are data-driven; Timber Pipes, Power Lines and Conveyor Belts are detected at runtime and never required.",
  "RequiredMods": [],
  "OptionalMods": []
}
```

### 2.2 四個 DLL（大小與 SHA-256）

| DLL | bytes | SHA-256 |
|---|---:|---|
| `SEE.Core.dll` | 82,432 | `0fabb709cc790f8dd3fca39f3726b09ad6d7f82711a7dc67041b57e54fa1c8ba` |
| `SEE.Gameplay.dll` | 57,856 | `36e4c099741317bf0b08519b194473103cb37454b6d758de6db7d6d1e1f425a1` |
| `SEE.Integration.dll` | 37,888 | `ad65600e80b414d0ed649ec319fd4e6f698e3e4242631e4a7f7517158f939282` |
| `SEE.UI.dll` | 51,712 | `81a54028b2dc20489d39290687684ffc5160b5842542488e6dff9ac7051d422d` |

### 2.3 版本三軌（G22）

| 軌道 | 值 | 出處 |
|---|---|---|
| 模組版本 | **0.1.0** | `manifest.json`、`build-report.json` |
| 存檔 schema | **1** | `SEEVersion.cs`／存檔節點 `schemaVersion` |
| 設定 schema | **1** | `SEE_Config.json` 的 `configSchemaVersion` |

---

## 3. 依賴說明

| 依賴 | 狀態 | 說明 |
|---|---|---|
| 遊戲本體 | **必要** | Timberborn 1.1.2.8 實測；manifest 最低 1.1.0.0 |
| Timber Pipes（工作坊 3801726769） | **選用** | 執行期偵測；缺席仍然可玩（搬運工補水）。v1 因 V-01／V-TP2 未實測，`auto` 在有 TP 時解析為 `valve`，不使用 `tank` |
| Power Lines（工作坊 3766893705） | **選用** | 執行期偵測；成功時以非 Harmony 方式注入 `PowerLineSpec`，失敗退回原版節點並只記一次 `SEE/Compat` |
| Conveyor Belts v2（工作坊 3815376360） | **選用** | 執行期偵測；燃料庫存列入 `InputGoods`，帶子可自動餵料 |
| Harmony | **不需要** | 無編譯期／執行期依賴；`RequiredMods` 與 `OptionalMods` 皆為空 |
| 其他 DLL／NuGet／網路 | **不需要** | 四支 DLL 只依賴遊戲程式集；無 `.deps.json` |

> 四個外部模組（含舊版 `Opolame.PowerLines`）皆為「有則增強、無則降級」，任一組合缺席都不會讓 SEE 變成硬依賴。

---

## 4. 安裝步驟（已於本機完成，逐檔 SHA-256 比對 0 mismatch）

目標：`C:\Users\user\Documents\Timberborn\Mods\SteamEngineExpansion\`

1. **完全退出 Timberborn**（含背景程序）。
2. 確認 `Mods\SteamEngineExpansion` 不存在，或是舊版（舊版先改名備份，例如 `SteamEngineExpansion.bak-<日期>`；**不得刪除其他模組目錄**）。
3. 複製 `mod\build\SteamEngineExpansion\` 的**內容**到 `Mods\SteamEngineExpansion\`（不是把資料夾再包一層）。
4. 驗證：檔案數 135、總大小 2,385,102 bytes、`manifest.json` 在資料夾根、4 個 DLL 在根。
5. 啟動遊戲 → 主選單 Mods（模組清單）→ 應看到 **Steam Engine Expansion v0.1.0**，且無載入錯誤。
6. 進入沙盒後照 `docs/TEST_PROTOCOL.md` 驗收（含 5b 夜間待機、5c 搬運工需求狀態與新耗水）；實機結果由人類簽核。

本次一鍵安裝用的等效命令（給需要重裝的人）：

```powershell
$src = "C:\Users\user\Downloads\timber steam engine\mod\build\SteamEngineExpansion"
$dst = "$env:USERPROFILE\Documents\Timberborn\Mods\SteamEngineExpansion"
if (Test-Path $dst) { Rename-Item $dst "$dst.bak-$(Get-Date -Format yyyyMMdd-HHmmss)" }
New-Item -ItemType Directory -Path $dst -Force | Out-Null
Copy-Item -Path (Join-Path $src '*') -Destination $dst -Recurse -Force
(Get-ChildItem $dst -Recurse -File | Measure-Object Length -Sum)   # 應為 Count=135, Sum=2385102
```

### 4.1 本機安裝結果（真實輸出）

| 檢查 | 結果 |
|---|---|
| 安裝方式 | `build.ps1 -Clean -Install`（2026-11-14 熱修-6） |
| 安裝前目標 | 本機 `Mods\SteamEngineExpansion` 不存在（前次環境已重置）；本次新建 |
| 安裝後檔案數 | **135** |
| 安裝後總大小 | **2,385,102 bytes** |
| 與來源逐檔 SHA-256 比對 | **0 mismatch（135/135）**；另有 3 個工作坊中繼檔（`workshop_data.json`、`thumbnail.png`、`thumbnail.png.meta.json`）不在出貨白名單內，屬更新用檔案 |
| 安裝路徑 | `C:\Users\user\Documents\Timberborn\Mods\SteamEngineExpansion` |
| 其他模組目錄 | 未更動、未刪除 |

> 本輪安裝在 DSH danger-full-access 檔案政策下完成；安裝後以逐檔 SHA-256 確認與 build/SteamEngineExpansion/ 成品一致（0 mismatch）。

### 4.2 安裝後目錄（實機檢查用）

```text
C:\Users\user\Documents\Timberborn\Mods\SteamEngineExpansion\
├── Buildings\      （24 藍圖＋README＋48 timbermesh）
├── Icons\          （24 PNG＋24 meta）
├── BlockObjectToolGroups\（1 份 SteamEngines group 藍圖）
├── ToolGroupIcons\ （SteamEngines.png＋meta）
├── Localizations\  （enUS.csv／zhTW.csv／zhCN.csv，各 64 鍵）
├── TemplateCollections\（2 份 append）
├── manifest.json
├── SEE_Config.json
├── SEE.Core.dll
├── SEE.Gameplay.dll
├── SEE.Integration.dll
└── SEE.UI.dll
```

### 4.3 安裝後請使用者執行（四步）

1. **啟動遊戲**（Steam 已登入、正常桌面工作階段）→ 主選單 Mods 清單確認 **Steam Engine Expansion v0.1.0**。
2. **新開 Folktails 沙盒**（或使用既有可建造 Power 的存檔）。
3. **照 `docs/TEST_PROTOCOL.md` 執行**：需求 §17.5 的 12 步＋2026-11-08 的**步驟 5b 夜間待機運轉**、**步驟 5c 搬運工需求狀態／新耗水**與 2026-11-14 的**步驟 1b 面板原版化／1c Steam Engines 工具組**逐項勾選；每一步留下截圖／數值／`Player.log`。
4. **回報結果**：把簽核表與失敗證據回傳；AI 不會代為判定通過，也不會在簽核前寫「可發佈」。

> 若安裝授權不可用（沙盒拒絕且無法升級），請在一般使用者 PowerShell 貼上 §4 的等效命令；
> 一行版：`New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\Documents\Timberborn\Mods\SteamEngineExpansion" | Out-Null; Copy-Item -Recurse -Force "C:\Users\user\Downloads\timber steam engine\mod\build\SteamEngineExpansion\*" "$env:USERPROFILE\Documents\Timberborn\Mods\SteamEngineExpansion"`。

---

## 5. 移除與降級

```powershell
# 先完全退出遊戲
Remove-Item -Recurse -Force "$env:USERPROFILE\Documents\Timberborn\Mods\SteamEngineExpansion"
```

- 也可在遊戲 Mods 介面停用，效果等同不載入；本模組不寫入遊戲本體檔。
- 存檔含 SEE 的混合形態資料（世界節點索引＋per-entity 狀態）；移除後載入不應壞檔（AC-19，實機待驗），
  原版燃料／水貨物由遊戲本體保存，不依賴 SEE。
- 重新安裝同一版本可直接覆蓋；跨版本升級先備份舊資料夾。

---

## 6. 完整檔案清單與雜湊

以下為 `mod/build/SteamEngineExpansion/` 全部 135 個檔案（依路徑排序）。**權威清單**為
`mod/build/SteamEngineExpansion.sha256`（`sha256sum -c` 格式：`<sha256>  <相對路徑>`），
本表另附位元組數供人工核對。

| # | relative path | bytes | SHA-256 |
|---:|---|---:|---|
| 1 | `BlockObjectToolGroups/BlockObjectToolGroup.SteamEngines.blueprint.json` | 204 | `1c76b235679ddd93c797419d18e807c6ac56fd83592e855c64ff8eacf7d86203` |
| 2 | `Buildings/Power/SteamBoiler/README.md` | 776 | `374463aa0d09f2df26837305371671b2d26462d49772b8f66a68a00b153385fd` |
| 3 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T1.Folktails.blueprint.json` | 19574 | `8f69e80f1393be28063f54f700a1c5db255937240a92b3ef19c34ea95a2846db` |
| 4 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T1.Folktails.ConstructionStage0.Model.timbermesh` | 2222 | `df85257bd760b6d8b4e43ab2c023ccffcd18fbf2ba396f8dec25c73bab2249e6` |
| 5 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T1.Folktails.Model.timbermesh` | 45834 | `7adceab5ff8cfc9f77a47ba700e99ccf3f11de8ed5eaa35df2aaffe454540aa3` |
| 6 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T1.IronTeeth.blueprint.json` | 19574 | `8c4267f264bb1a874ae3953db9573b2bd1fe0de8c3819371968b7a434f6f05af` |
| 7 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T1.IronTeeth.ConstructionStage0.Model.timbermesh` | 2226 | `c7ef77e80758dfb84d2ee81d66d6e1409ec7257497961d97d2e28c69d685949a` |
| 8 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T1.IronTeeth.Model.timbermesh` | 45915 | `abad9673b45f42f8996de1a7e670bda08edb8c4a3d68c529884a92e667510999` |
| 9 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T2.Folktails.blueprint.json` | 19698 | `63d3da5a8a37a5ad844f68bf8926d7d91e6650cf979dd09f5fee4087782a26fb` |
| 10 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T2.Folktails.ConstructionStage0.Model.timbermesh` | 2223 | `4df7b7589c828b27005e81f93159493d0d350dc79a3c1be4d324b585062a5b5c` |
| 11 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T2.Folktails.Model.timbermesh` | 54439 | `e7d429f0c8905a348fd3387989d4130b21b2f1da7a26c21a6fddedc436063274` |
| 12 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T2.IronTeeth.blueprint.json` | 19698 | `65d6727455a5403b3e98b7eb3c23ad9c53e79c8fd2cf749cbadcc5063eedd797` |
| 13 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T2.IronTeeth.ConstructionStage0.Model.timbermesh` | 2225 | `50beda7e00fefa8302e2d0e09da459091ce6816045907a9acb73528d44b96c30` |
| 14 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T2.IronTeeth.Model.timbermesh` | 54554 | `11e37ef4c17600e64f64bb9b0deb9aa7c7a9f5aa4919bae59f76fc0cfb3a77ba` |
| 15 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T3.Folktails.blueprint.json` | 19809 | `deff081f3fc1297637bab11e9e9afa93b62f334cae6420ecdca19682717d98e4` |
| 16 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T3.Folktails.ConstructionStage0.Model.timbermesh` | 2308 | `f77ea116c001c6f157a50e2106f2f38f3d6c1f21817f6a988fce3ba2e580e190` |
| 17 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T3.Folktails.Model.timbermesh` | 64702 | `6afd5b57f93d7250919b49fb20c709ee384be61483b486b832a17f14db265bda` |
| 18 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T3.IronTeeth.blueprint.json` | 19810 | `0159d3440e9441419754aa9501a2ae83eb80e43eaed84c3f808328c35fb7917d` |
| 19 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T3.IronTeeth.ConstructionStage0.Model.timbermesh` | 2312 | `17735917ea6c9b098ae5bf48a581bf6a991f3878501f383585a3d6db5b5c9081` |
| 20 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T3.IronTeeth.Model.timbermesh` | 64880 | `36675bcad52a20a78e269b917f7bbbea240292de1c0a82ab28d5ef194ba8735d` |
| 21 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T4.Folktails.blueprint.json` | 19748 | `b40b21cacb0554e78c2d74fbf2062bedbdad31d53b069fc4aad1ad86b5066fb2` |
| 22 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T4.Folktails.ConstructionStage0.Model.timbermesh` | 2309 | `f8b090f8020b55ed5e970645fcfb3892f17b48a5e4ee5d6b4f046c5a6e6e26d8` |
| 23 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T4.Folktails.Model.timbermesh` | 61910 | `9404e6f572c596eb503cb8f1bd22bb7d4cadc5cb27aab02c43692bf585b1bc70` |
| 24 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T4.IronTeeth.blueprint.json` | 19748 | `fad079d46b02e43fab1cde6c17b9448b9ca5d160879724e92801bf3502696625` |
| 25 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T4.IronTeeth.ConstructionStage0.Model.timbermesh` | 2312 | `7ff89d1b29d0c4db478565337768c197a03886010adf0c1ca156aed1f3ff2514` |
| 26 | `Buildings/Power/SteamBoiler/SteamBoiler.Large.T4.IronTeeth.Model.timbermesh` | 62067 | `6e7bb2d2970191f81689a606b6058e1b25b82b38c954bd5fc5e81ab30ea96f95` |
| 27 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T1.Folktails.blueprint.json` | 12780 | `9ef70885df727dac8f74659de37bf8a5d34e262783c382f09b8625a13241cb9e` |
| 28 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T1.Folktails.ConstructionStage0.Model.timbermesh` | 2222 | `934327bc6f93a803a00cbdabf1938bcb190e95d5c41c2b2e4aadc2aaa6ed883b` |
| 29 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T1.Folktails.Model.timbermesh` | 40683 | `10549d2cdfc97f8ca81504c2712d7dfecdadf82cf2fe27908c0b4fef10be6c59` |
| 30 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T1.IronTeeth.blueprint.json` | 12780 | `e364390f561c7fb1228273900f4264c15a6e6ff79f69a3816cd5efe130e32140` |
| 31 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T1.IronTeeth.ConstructionStage0.Model.timbermesh` | 2234 | `268ae4b1d6e6477d402dcecbf998b5522a56c0e93cab32d73e20667c8253d48c` |
| 32 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T1.IronTeeth.Model.timbermesh` | 40826 | `c7f67f37cba647c3b6c85ed05b2825aed1f2e63ab823523b00a9432aa7f2f4f4` |
| 33 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T2.Folktails.blueprint.json` | 12902 | `01d7da2ff3758636ea20e7c82e70022549f47d6a3fb86966a905942a491eb7f2` |
| 34 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T2.Folktails.ConstructionStage0.Model.timbermesh` | 2222 | `ce0747c908f526171e43172b8c792c802098afbdc5fe9b2d0844642a4a155a6d` |
| 35 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T2.Folktails.Model.timbermesh` | 49171 | `3938cc314aa58cbcc7cf65f44537d5ececadd913f505793eb9f6d48b8172f1dc` |
| 36 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T2.IronTeeth.blueprint.json` | 12902 | `86d70e2e3fdcfd7492e125b0ea9e361391546cf63b27de3212664c79ea88e63c` |
| 37 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T2.IronTeeth.ConstructionStage0.Model.timbermesh` | 2233 | `4e225b8a88c190d830cd350de352cfaa0e45df1e762797c41da2e299a5f17428` |
| 38 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T2.IronTeeth.Model.timbermesh` | 49313 | `fc974dca6769c4cba0114852c01fa748c4e5b3ff0a81f91769ec5b02d413cd1e` |
| 39 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T3.Folktails.blueprint.json` | 13014 | `867c0184f9b5d86a1d85204ebb6b3057c71962afeeff9e187cefabed3203d98c` |
| 40 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T3.Folktails.ConstructionStage0.Model.timbermesh` | 2316 | `f94ae6f7016ccd3adb3b50ca75090f0f628304d5bd9622f9677267e383b3e360` |
| 41 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T3.Folktails.Model.timbermesh` | 57883 | `2259816bbb0a1523452a2c22faea0feba333c5d33564daa18aa2de477692e729` |
| 42 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T3.IronTeeth.blueprint.json` | 13014 | `6cc408c1249a36951d120c2ad58b658944b48187b4cf141de614807f527b3b70` |
| 43 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T3.IronTeeth.ConstructionStage0.Model.timbermesh` | 2321 | `188ccee67524f3c94ee899a34f0423199c685788b186da0ee4ec0e6bfd34b196` |
| 44 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T3.IronTeeth.Model.timbermesh` | 58028 | `d64f80d522e3e04f6c90462142e44d78d6715c6e28d32f0984244aab432cabab` |
| 45 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T4.Folktails.blueprint.json` | 12952 | `a92674543f79d202b36441bf4c39303bd2d816bf8b36e98dd4392521b52a1120` |
| 46 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T4.Folktails.ConstructionStage0.Model.timbermesh` | 2316 | `e818e3b0054f5c6fbb3458271a4570ce93596dc6de6ca4c99262d273a3c3d9de` |
| 47 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T4.Folktails.Model.timbermesh` | 54591 | `b65ca478fdb6f8e8082edd83cbde25c5ae2f5e0be3fe6692e85324dbe0d9bced` |
| 48 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T4.IronTeeth.blueprint.json` | 12952 | `bf234df493f03dfc25451c08b38c67465ac0639451ecb687cd62890303ed5a9d` |
| 49 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T4.IronTeeth.ConstructionStage0.Model.timbermesh` | 2322 | `f84df8c3b93050d0404d6c3c44c562fc108c215a4a211616d7953e92958a12a9` |
| 50 | `Buildings/Power/SteamBoiler/SteamBoiler.Medium.T4.IronTeeth.Model.timbermesh` | 54738 | `0d267171688e4e7ab7c01ed92f89c0fead51bdda9db1a4030d71eaa85fda0cc5` |
| 51 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T1.Folktails.blueprint.json` | 9855 | `76eed66966e8d27f4c5290ca0e23807f0bdfa3d97d821861a459c6ea89070bac` |
| 52 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T1.Folktails.ConstructionStage0.Model.timbermesh` | 2237 | `e5c3db536859851062aa495c98d378aaad397047de20a672196bd6d8e685a58d` |
| 53 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T1.Folktails.Model.timbermesh` | 34071 | `549ccbc2bcb5474cebf55906523d3796570b77ae398fa046c0c6270466868444` |
| 54 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T1.IronTeeth.blueprint.json` | 9855 | `e6c0eecc480ae8c74f7d4937ff609f74a04d5fefc74c0dd7a7eb531bb4b6279b` |
| 55 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T1.IronTeeth.ConstructionStage0.Model.timbermesh` | 2239 | `f3ef0e11c5c9d9d316e89c385fde9d98dcc5112fa1b4a4ff4ed9c9bcf5b4ae71` |
| 56 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T1.IronTeeth.Model.timbermesh` | 34214 | `46f6595c3d94bf09e86888d331d3086b74fdc3d13a3b7581c8c34ef4d6c4ba51` |
| 57 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T2.Folktails.blueprint.json` | 9978 | `3bb5f36bced88877296b8a7c694020fb837da04ff37b3397e0a4f10de69dcc89` |
| 58 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T2.Folktails.ConstructionStage0.Model.timbermesh` | 2236 | `ca81725aac3cb3b50f3d5e7a43d82de064fc4d7dc52f4ba6b7c77069b05d7170` |
| 59 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T2.Folktails.Model.timbermesh` | 43370 | `e75fd3a5baaef2b590ee4e0a0ac54b74da318d3d4439f9fd4ec0e30e15978d68` |
| 60 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T2.IronTeeth.blueprint.json` | 9978 | `f6fc860d9461caf77dc4a2604c8f8ca481ea7c07799d255cd0ad9bb2b5607648` |
| 61 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T2.IronTeeth.ConstructionStage0.Model.timbermesh` | 2240 | `f2a2dabbc13e3b9bf42e94aedd6bb5064f55f1632e76b79fb618a3822dde995d` |
| 62 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T2.IronTeeth.Model.timbermesh` | 43511 | `f381e52899e45b7a1d8b1bc9b016077d079c26f4a8f20baebd94f848811f3814` |
| 63 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T3.Folktails.blueprint.json` | 10090 | `82995057b1f4d94ab541dc9b6899282f466f4ce3c3fa1dcb007ad2b1d4307c2c` |
| 64 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T3.Folktails.ConstructionStage0.Model.timbermesh` | 2320 | `3b038b70fa1bf2cbe708da4443d79b381b23776cd26aa5ebbff3992e8fb5fb0f` |
| 65 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T3.Folktails.Model.timbermesh` | 48310 | `634cebdb68c1e7eab4e9646a44ea6596fa5bac456798af008afeddbb04d956ae` |
| 66 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T3.IronTeeth.blueprint.json` | 10090 | `d3031695a480380bf78abf07c4d7a4a76561b7b05fc320826f49e76e26a0fc6a` |
| 67 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T3.IronTeeth.ConstructionStage0.Model.timbermesh` | 2324 | `4440fec4500f019d2497260d4d50e0e3ed981b4875dc853a289750f121ca8b3d` |
| 68 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T3.IronTeeth.Model.timbermesh` | 48367 | `864daf8a9e1ebd5fd0fcf2c3af0bcbb0236765fc09c5d9f6d04e1eb8b0613245` |
| 69 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T4.Folktails.blueprint.json` | 10027 | `87ccca5634d4d234ab8134824a78f8fb51bedeb99f38e3ff1b559f4da71c88b2` |
| 70 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T4.Folktails.ConstructionStage0.Model.timbermesh` | 2320 | `f9f59a67f84ef5f284bc3c813f14cfa1f32fd3ee7c90174d35c610c218033d5a` |
| 71 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T4.Folktails.Model.timbermesh` | 45026 | `c3859c3e7260e866fdd9297305d97cb6810a99fecc5917b792ab601c7adac65f` |
| 72 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T4.IronTeeth.blueprint.json` | 10027 | `065013def900bafc56e98758360e857b6d237e12e6cce7d7379dd5cb1fbad064` |
| 73 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T4.IronTeeth.ConstructionStage0.Model.timbermesh` | 2324 | `205799f488d517f8e17c476076405347c020d2106a3d51178ed21f8e2f1c90db` |
| 74 | `Buildings/Power/SteamBoiler/SteamBoiler.Small.T4.IronTeeth.Model.timbermesh` | 45181 | `5e12623e601944591c423498d89ae2d193869e5c0804546e4ab30fb0fc51bf7c` |
| 75 | `Icons/SteamBoilerLargeT1Folktails.png` | 23508 | `8d35e5dd604ed9771dbbfa10419171814e31f2039772d0022f37888103a9bc58` |
| 76 | `Icons/SteamBoilerLargeT1Folktails.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 77 | `Icons/SteamBoilerLargeT1IronTeeth.png` | 23174 | `d40589bf8880f3f8d3614b745088a0cc5f7b6c90cf630a0a35f2e50602246eb1` |
| 78 | `Icons/SteamBoilerLargeT1IronTeeth.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 79 | `Icons/SteamBoilerLargeT2Folktails.png` | 23068 | `266432f9259bd26046660b19efe9906fb2ef3657776745788c1d529fc02946a1` |
| 80 | `Icons/SteamBoilerLargeT2Folktails.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 81 | `Icons/SteamBoilerLargeT2IronTeeth.png` | 22602 | `7995fdac219d2ef9287e0d9b5500f1bdffe1989b783866d3d8cda393e8460977` |
| 82 | `Icons/SteamBoilerLargeT2IronTeeth.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 83 | `Icons/SteamBoilerLargeT3Folktails.png` | 23596 | `06faf05088f2665482d36fd1e980592ed164e8650a895450c09348bed3233f25` |
| 84 | `Icons/SteamBoilerLargeT3Folktails.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 85 | `Icons/SteamBoilerLargeT3IronTeeth.png` | 22948 | `832f89fffb534359a311564ff0693f460c10cc418b54a4b9df748bfd80af1cfd` |
| 86 | `Icons/SteamBoilerLargeT3IronTeeth.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 87 | `Icons/SteamBoilerLargeT4Folktails.png` | 24243 | `86e71ab2fb0f673f74233eb84bf3722348838ae750c7d64f1482908b402a091d` |
| 88 | `Icons/SteamBoilerLargeT4Folktails.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 89 | `Icons/SteamBoilerLargeT4IronTeeth.png` | 23751 | `9a96c9fc357c4ab2bee9b384f98846bd270e4cfb15ac97841ea80b1fbea01a37` |
| 90 | `Icons/SteamBoilerLargeT4IronTeeth.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 91 | `Icons/SteamBoilerMediumT1Folktails.png` | 22622 | `14df019120be50f129c2245aa6cab9be3ae6808338a7d2f31551adfd6db0c433` |
| 92 | `Icons/SteamBoilerMediumT1Folktails.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 93 | `Icons/SteamBoilerMediumT1IronTeeth.png` | 22302 | `937b6e9bc48ea79a460728c3e528d7c0ddd20286f0ebf0edfcd1e3cd5c2ac406` |
| 94 | `Icons/SteamBoilerMediumT1IronTeeth.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 95 | `Icons/SteamBoilerMediumT2Folktails.png` | 23163 | `d5bcd4b96afbdeacd35dcfc738b6582229cc035043c58c15b8b7a6aef9a44693` |
| 96 | `Icons/SteamBoilerMediumT2Folktails.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 97 | `Icons/SteamBoilerMediumT2IronTeeth.png` | 22693 | `262d315b163fadd9751f6f0fcdfd9e41178add0594d038309a28f0fda918fb7f` |
| 98 | `Icons/SteamBoilerMediumT2IronTeeth.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 99 | `Icons/SteamBoilerMediumT3Folktails.png` | 23297 | `901d323c8b4a4ec3aa8d73d2620907e96e65ac5d8b33975858e1c749f6ac4fd4` |
| 100 | `Icons/SteamBoilerMediumT3Folktails.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 101 | `Icons/SteamBoilerMediumT3IronTeeth.png` | 22629 | `6ada8ee4e81c83e78635543192f5d88da57b4802fac8a408e1ade62db7fbb355` |
| 102 | `Icons/SteamBoilerMediumT3IronTeeth.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 103 | `Icons/SteamBoilerMediumT4Folktails.png` | 23595 | `0ef552f224d383c9deec226b89388ef575756f14c2c7d229ef40dd857a21fbce` |
| 104 | `Icons/SteamBoilerMediumT4Folktails.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 105 | `Icons/SteamBoilerMediumT4IronTeeth.png` | 23077 | `f81374e49edc0d759c4509346b68242dbe41c356e2f02cd7b4a33ef3e6446623` |
| 106 | `Icons/SteamBoilerMediumT4IronTeeth.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 107 | `Icons/SteamBoilerSmallT1Folktails.png` | 21368 | `defd4f6708efc05bc2efb1b218e21957f0b55194362647af6d9f705d54fa7f75` |
| 108 | `Icons/SteamBoilerSmallT1Folktails.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 109 | `Icons/SteamBoilerSmallT1IronTeeth.png` | 21043 | `0c4494f83ff91896b916b26b79f607f07beb4702dcc554f6e2fd047160afeb49` |
| 110 | `Icons/SteamBoilerSmallT1IronTeeth.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 111 | `Icons/SteamBoilerSmallT2Folktails.png` | 21031 | `81d6884571d2520deda54f4e566a924d6561ce84812da84e90721eb0c1cd703f` |
| 112 | `Icons/SteamBoilerSmallT2Folktails.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 113 | `Icons/SteamBoilerSmallT2IronTeeth.png` | 20650 | `0398cf0b37391f06f8d125dd757b47def16a6d366199c0324b0e9955ee90f3c2` |
| 114 | `Icons/SteamBoilerSmallT2IronTeeth.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 115 | `Icons/SteamBoilerSmallT3Folktails.png` | 21089 | `b631d144296ea29d0b264c3e35204e41c4f846ac591a16fd6d3fee8bf7700c5c` |
| 116 | `Icons/SteamBoilerSmallT3Folktails.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 117 | `Icons/SteamBoilerSmallT3IronTeeth.png` | 20450 | `0951025669d5ca0e62808177669d48cffae8aff0e5cd510d5b08adcd963eef18` |
| 118 | `Icons/SteamBoilerSmallT3IronTeeth.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 119 | `Icons/SteamBoilerSmallT4Folktails.png` | 21474 | `b8f68054e8a90bf73a478b1d886c6a58a6f2345dcd26a21e1a447b4ae69a2a31` |
| 120 | `Icons/SteamBoilerSmallT4Folktails.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 121 | `Icons/SteamBoilerSmallT4IronTeeth.png` | 20896 | `4522f1e9c2b23170f239257aec12230efe3a6f8101f21c65067f4dc22ef0175f` |
| 122 | `Icons/SteamBoilerSmallT4IronTeeth.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
| 123 | `Localizations/enUS.csv` | 4172 | `af3cd17bc807d2742366d93ad3795cb73d78694d6e44dde9caaea67546d5ffdd` |
| 124 | `Localizations/zhCN.csv` | 4065 | `8045c1e3e07b55ecb27037a39ce08ee06e50b3b5d18008ddc3622ec1483b1886` |
| 125 | `Localizations/zhTW.csv` | 4062 | `9a377064e18c49872365d264db485ede90965bb284c6749c65ba53827543a0cb` |
| 126 | `manifest.json` | 670 | `5614a793fc1fab36eae1fc49f99e1e2bac8fb0133487eb9b5dd2482a5967ec13` |
| 127 | `SEE.Core.dll` | 82432 | `0fabb709cc790f8dd3fca39f3726b09ad6d7f82711a7dc67041b57e54fa1c8ba` |
| 128 | `SEE.Gameplay.dll` | 57856 | `36e4c099741317bf0b08519b194473103cb37454b6d758de6db7d6d1e1f425a1` |
| 129 | `SEE.Integration.dll` | 37888 | `ad65600e80b414d0ed649ec319fd4e6f698e3e4242631e4a7f7517158f939282` |
| 130 | `SEE.UI.dll` | 51712 | `81a54028b2dc20489d39290687684ffc5160b5842542488e6dff9ac7051d422d` |
| 131 | `SEE_Config.json` | 2524 | `e04be6c8b8662f9f7b9fd43b9d8e58fc6ff0b3f29e9221214164b2564ac7554a` |
| 132 | `TemplateCollections/TemplateCollection.Buildings.Folktails.blueprint.json` | 1053 | `73b5a2a105ba72362870680fb32fb60fd61d7f86688bdd6e704c95a10dab0763` |
| 133 | `TemplateCollections/TemplateCollection.Buildings.IronTeeth.blueprint.json` | 1053 | `4d1b8fdbe233dc033535f52c2efbea9cb6b1873a0fb165b87bb885de7e7464c9` |
| 134 | `ToolGroupIcons/SteamEngines.png` | 789 | `2ec2a380d36af8b4e012798edb870b5adc601a12340b45c741467cc9264998a6` |
| 135 | `ToolGroupIcons/SteamEngines.png.meta.json` | 23 | `a3905f78694d6f42fb7158cb70b57b99fb97c923d45cc4fd8093f4b203fa66db` |
---

## 7. 驗收對照

| 交付要求 | 本文件位置 | 狀態 |
|---|---|---|
| 可重現建置（0 warning／測試／validator 全綠） | §1 | **PASS（離線）** |
| 成品不含 src/tools/tests | §2、§4 | **PASS（離線）** |
| 4 個 DLL 存在 | §2.2 | **PASS（離線）** |
| 檔案清單＋大小＋雜湊 | §6 | **PASS（離線）** |
| manifest 內容 | §2.1 | **PASS（離線）** |
| 安裝結果 | §4.1 | **已安裝（實機行為待人類驗收）** |
| 實機 17.5 十二步＋1b/1c＋5b/5c | `docs/TEST_PROTOCOL.md` | **待驗（人類簽核）** |
| AC-01～AC-21 | `docs/VERIFICATION.md` | 離線項 PASS、T2–T6 待驗 |
| Go/No-Go（§19.2） | `docs/VERIFICATION.md` §4 | **No-Go（未可發佈）** |
