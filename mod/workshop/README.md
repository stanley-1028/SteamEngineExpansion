# Steam Workshop 發佈流程（本機已備好素材）

> **已上架（2026-11-08）**：<https://steamcommunity.com/sharedfiles/filedetails/?id=3836039641>
> ItemId = `3836039641`，版本 0.1.0。`workshop_data.json` 已由遊戲寫入（repo 與已安裝模組皆同步）。
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

## 之後更新

1. 重新 `powershell -NoProfile -ExecutionPolicy Bypass -File mod/build.ps1 -Clean`
2. 在 Mods 管理器選 **Update**；`workshop_data.json` 的 `Update*` 欄位控制要同步的內容。
3. 版本號與 `CHANGELOG.md` 同步。

## 注意

- 封面右下角的「豆包AI生成」浮水印已用裁切移除；**不要**上傳 `cover/ai-raw-2732x1535.jpg`（保留在 repo 僅作來源存檔）。
- 遊戲內上傳前建議先照 `docs/TEST_PROTOCOL.md` 做一輪實機檢查。
- `workshop_data.json` 的 `ItemId` 在首次上傳前為空是正常的；不影響 GitHub 發佈。
