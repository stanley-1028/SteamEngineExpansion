# Steam Workshop 發佈流程（本機已備好素材）

> **已上架（2026-11-08）／已更新至熱修-7（2026-11-14/15）**：<https://steamcommunity.com/sharedfiles/filedetails/?id=3836039641>
> ItemId = `3836039641`，版本 0.1.0。熱修-7 已於 2026-11-14/15（Asia/Macau）以 `steamcmd +workshop_build_item`
> 用擁有者帳號上傳成功：內容＝已安裝資料夾（135 檔＋`workshop_data.json`＋`thumbnail.png`）、Description 加入熱修-7 段、
> changenote＝熱修-7；標題／預覽／可見性／標籤保留。熱修-6 的記錄保留在下方歷史。
> `workshop_data.json` 已由遊戲寫入（repo 與已安裝模組皆同步）。
> 本檔其餘內容保留作「更新／重新上傳」的參考。

## 已備素材

| 檔案 | 用途 |
|---|---|
| `thumbnail.png`（1280×720） | 遊戲內 Mods 管理器選用的封面圖（對應 `thumbnail.png.meta.json`） |
| `cover/mockup-1920x1080.jpg` | 工作坊頁面大圖（壓字版） |
| `cover/clean-1920x1080.jpg` | 無字版（想改用 Steam 標題列時使用） |
| `cover/qa-322x181.png` | 縮圖可讀性檢查 |
| `cover/qa-square-1080.jpg` | 中心 1:1 方裁檢查 |
| `DESCRIPTION.md` | 工作坊標題／描述（可直接貼上，含 BBCode） |
| `UPDATE-hotfix-6.md` | 熱修-6 更新時可直接貼上的 Name／Description／Changelog／Tags |
| `workshop_data.template.json` | 上傳設定模板（**首次上傳前不要**把它改名成 `workshop_data.json`，見下方「常見錯誤」） |
| `COVER_PROMPTS.md` | 封面 AI 提示詞與規格（可重生成） |

> **常見錯誤（2026-11-08 實測）**：把 `workshop_data.json` 的 `ItemId` 留成空字串（`""`）放進 Mods 資料夾，
> 遊戲在上傳面板開啟時會 `UInt64.Parse("")` → `FormatException` 崩潰。**首次上傳前不要有
> `workshop_data.json`**；發佈成功後由遊戲寫入真正的 ItemId，或手動填入數字 ItemId。

## 發佈步驟（首次）

1. 啟動 Timberborn → 主選單 **Mods** → 找到 **Steam Engine Expansion**（已安裝）。
2. 點該模組的 **Upload / 發佈到 Steam 工作坊**（不同版本按鈕名稱可能是 `Publish`）。
3. 第一次會要求你同意 Steam 工作坊條款並建立物品；標題貼 `DESCRIPTION.md` 的標題。
4. 封面選擇 `mod/workshop/thumbnail.png`（或先把 `cover/mockup-1920x1080.jpg` 轉成 1280×720 PNG 後選它）。
5. 描述貼 `DESCRIPTION.md` 的「詳細描述」（Steam BBCode 可直接用）。
6. 標籤：`Update 1.1`、`New content`、`Buildings`。
7. 發佈後，把新物品的 **ItemId** 填回 `workshop_data.json` 的 `ItemId`（供之後更新使用）。
8. 把工作坊連結補到 repo README 與 GitHub About。

## 之後更新（v0.1.0 熱修-6 之後）

> **重要**：Steam 工作坊**沒有公開的 Web API**，但擁有者帳號可用 `steamcmd +workshop_build_item` 更新
> （2026-11-14 熱修-6 即以此方式完成：內容／標題／描述／changenote／預覽／可見性皆更新，標籤保留）。
> 遊戲內 **Mods → Steam Engine Expansion → Update** 仍是替代路徑。

**更新前置（A／B 兩條路徑共用，已安裝模組資料夾內必須有）**：
- `workshop_data.json`（`ItemId = 3836039641`，`UpdateDescription/UpdateVisibility/UpdatePreview/UpdateTags = true`）
- `thumbnail.png` ＋ `thumbnail.png.meta.json`（工作坊預覽，1280×720）
- 以上檔案不在 `mod/build/SteamEngineExpansion/` 的出貨白名單內；更新前請從 `mod/workshop/` 複製到
  `%USERPROFILE%\Documents\Timberborn\Mods\SteamEngineExpansion\`（不要覆蓋其他 135 檔）。

### A. steamcmd（2026-11-14 實測成功，可重複使用）

1. 準備 VDF 暫存檔（不要放進 repo；`description`／`changenote` 取 `UPDATE-hotfix-6.md` 的 bbcode 區塊全文）：
   ```
   "workshopitem"
   {
       "appid"           "1062090"
       "publishedfileid" "3836039641"
       "contentfolder"   "%USERPROFILE%\Documents\Timberborn\Mods\SteamEngineExpansion"
       "previewfile"     "%USERPROFILE%\Documents\Timberborn\Mods\SteamEngineExpansion\thumbnail.png"
       "visibility"      "0"
       "title"           "<UPDATE-hotfix-6.md 的 Name 區塊>"
       "description"     "<Description bbcode 全文>"
       "changenote"      "<Changelog bbcode 全文>"
   }
   ```
   用 UTF-8 BOM 存檔，多行值可直接換行；**不要**加 `tags`（steamcmd 不支援；實測更新後原標籤仍在）。
2. 上傳（沿用 steamcmd 已存帳號；若手機跳出 Steam Guard 確認按同意即可）：
   ```
   C:\Users\<user>\steamcmd\steamcmd.exe +login <owner_account> +workshop_build_item "<vdf 路徑>" +quit
   ```
3. 驗證：POST `https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/`
   （`itemcount=1&publishedfileids[0]=3836039641`），確認 `time_updated`、`file_size`、`title`、`description`
   已更新，且 `tags`／`preview_url` 未消失。

### B. 遊戲內 Update 按鈕（替代路徑）步驟
1. 完成安裝（見 `docs/PACKAGE.md`）：把 `Documents\Timberborn\Mods\SteamEngineExpansion\` 更新為最新建置（135 檔；含 `BlockObjectToolGroups/`、`ToolGroupIcons/`）。
2. 將 `mod/workshop/workshop_data.json`、`thumbnail.png`、`thumbnail.png.meta.json` 複製到同一資料夾。
3. 啟動 Timberborn → 主選單 **Mods** → **Steam Engine Expansion** → **Update**（按鈕可能顯示 `Update`／`Publish`）；
   標題、描述、封面、標籤照 `DESCRIPTION.md`（中文）／`DESCRIPTION.en.md`（English）。`Update*` 欄位會同步描述／可見性／預覽／標籤。
4. 上傳完成後，把 `CHANGELOG.md` 的 hotfix-5 摘要貼到工作坊公告（可選）。
5. 確認 `Player.log` 無 `FormatException`／上傳錯誤；`workshop_data.json` 的 ItemId 不應改變。

**熱修-6 已備內容（2026-11-14）**：① 面板改用原版外觀（九宮格卡片、原生按鈕/開關/進度條/下拉框；收合標題；AD-21）；
② 24 檔改專屬底部工具組「Steam Engines」（Order 72，可展開/收合，圖示 `ToolGroupIcons/SteamEngines`，AD-22）；
③ loc 63→64（AD-23）。`DESCRIPTION.md`／`DESCRIPTION.en.md` 已加入「更新（熱修-6）」段並更新安裝說明（Power → Steam Engines 分類）。
先前的熱修-5 內容（完工引擎不再顯示搬運工需求狀態、耗水減半）已一併包含在本版。

## 注意

- 封面右下角的「豆包AI生成」浮水印已用裁切移除；**不要**上傳 `cover/ai-raw-2732x1535.jpg`（保留在 repo 僅作來源存檔）。
- 遊戲內上傳前建議先照 `docs/TEST_PROTOCOL.md` 做一輪實機檢查。
- `workshop_data.json` 的 `ItemId` 在首次上傳前為空是正常的；不影響 GitHub 發佈。

## 發佈記錄（2026-11-15，熱修-7）

- **熱修-7 = 面板隱藏不再關掉全遊戲共用的下拉清單**：修復 `BoilerPanelView.SetVisible(false)` 無條件
  `_waterMode.Hide()`（原版 `Dropdown.Hide()` 關的是單例 drawer）——玩家回報 HDBF 作物選擇器在運行時約 0.2 秒
  自己關掉。現在只在真正 visible→hidden 轉換時才關自己的下拉（`_visible` 守衛）。只動 `src/SEE.UI` + 1 條
  `NativePanelContractTests`；資料／藍圖／數值／存檔不變。
- **上傳**：2026-11-15 00:17:34–00:17:39（Asia/Macau）成功，`ManifestID 8352811448294687049`；
  `C:\Users\user\steamcmd\logs\workshop_log.txt`：
  `Uploaded new content ( ManifestID 8352811448294687049 ) for item 3836039641.` →
  `Upload finished for workshop item 3836039641 : OK`（AppID 1062090；`steamcmd +workshop_build_item`，
  擁有者帳號 `laustanley` 使用 cached credentials 登入）。
- **上傳內容** = `Documents\Timberborn\Mods\SteamEngineExpansion`（135 檔／2,385,102 bytes；package sha
  `51646eb0d0aa7fc2c0e575559fde21dd924e97c4b21853172b438c50a202c3ab`）＋`workshop_data.json`＋`thumbnail.png`。
  Description／Change Note 取自 `UPDATE-hotfix-7.md`；Preview／Visibility／Tags 不改。
- **API 核對**：`ISteamRemoteStorage/GetPublishedFileDetails` → `result=1`、title 正確、
  `file_size 2,823,975`、`tags = Mod / Update 1.1 / New content`、`preview_url` 未變；
  `time_updated` 對應 2026-11-15 00:17（Asia/Macau）。公開創作頁
  `profiles/76561199800253923/myworkshopfiles/?appid=1062090` 已列出本 item 與新預覽縮圖。
  （上傳後數分鐘內直接 `filedetails` 頁曾回「hidden or no permission」，屬更新後的 Steam 傳播/快取狀態；
  公開列表頁與 API 均已正常，記錄於此不重傳。）
- **離線門禁**：`build.ps1 -Clean -Install` → **BUILD PASSED（G01–G22 全綠）**、四程式集 0 warning、
  **PASS 523/523**、generator `CHECK PASS（0 differences, 83 files, 24 blueprints）`、
  validator **VALIDATION PASSED（13/13 gate groups, 1 warning, 0 error）**、135 檔／2,385,102 bytes。
- **實機**：2026-11-15 00:11，SEE hotfix-7 + HDBF 2.6.1 同場（HDBFTest 存檔，`Load time: 119925ms`）：
  選中 HDBF 農場後打開作物下拉，彈窗保持打開 5.0 秒、成功選「土豆」；唯一關閉事件來自選取動作。
  證據 `docs/evidence-hotfix7/`（截圖 + 日誌節錄）。T2–T6 人類簽核項狀態不變（仍待驗）。
- **GitHub**：commit／tag `v0.1.0-hotfix-7` 與 release asset 見 GitHub；本記錄 commit 隨後 push。
