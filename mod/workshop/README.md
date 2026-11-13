# Steam Workshop 發佈流程（本機已備好素材）

> **已上架（2026-11-08）**：<https://steamcommunity.com/sharedfiles/filedetails/?id=3836039641>
> ItemId = `3836039641`，版本 0.1.0（2026-11-14 熱修-6 的更新內容已備好，待擁有者在遊戲內按 Update）。
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

> **重要**：Steam 工作坊**沒有可供外部程式發佈／更新的公開 API**。更新只能由擁有者在 Timberborn 遊戲內
> 的 **Mods → Steam Engine Expansion → Update（發佈到工作坊）** 按鈕完成（需要擁有者 Steam 帳號）。
> AI 已把更新所需的檔案與文案備好，下列步驟可由任何人執行。

**更新前置（已安裝模組資料夾內必須有）**：
- `workshop_data.json`（`ItemId = 3836039641`，`UpdateDescription/UpdateVisibility/UpdatePreview/UpdateTags = true`）
- `thumbnail.png` ＋ `thumbnail.png.meta.json`（工作坊預覽，1280×720）
- 以上檔案不在 `mod/build/SteamEngineExpansion/` 的出貨白名單內；更新前請從 `mod/workshop/` 複製到
  `%USERPROFILE%\Documents\Timberborn\Mods\SteamEngineExpansion\`（不要覆蓋其他 135 檔）。

**更新步驟**：
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
