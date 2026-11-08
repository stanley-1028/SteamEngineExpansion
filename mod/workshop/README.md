# Steam Workshop 發佈流程（本機已備好素材）

> 為何以手動為主：Steam Workshop 的「建立／更新物品」只能由 Steam 用戶端在 Timberborn 的
> Mods 管理器內完成（需要你的 Steam 帳號與遊戲授權）；GitHub Actions／SteamCMD 不能代為發佈
> Timberborn 模組。本資料夾已把可上傳的素材與文案準備好。

## 已備素材

| 檔案 | 用途 |
|---|---|
| `thumbnail.png`（1280×720） | 遊戲內 Mods 管理器選用的封面圖（對應 `thumbnail.png.meta.json`） |
| `cover/mockup-1920x1080.jpg` | 工作坊頁面大圖（壓字版） |
| `cover/clean-1920x1080.jpg` | 無字版（想改用 Steam 標題列時使用） |
| `cover/qa-322x181.png` | 縮圖可讀性檢查 |
| `cover/qa-square-1080.jpg` | 中心 1:1 方裁檢查 |
| `DESCRIPTION.md` | 工作坊標題／描述（可直接貼上，含 BBCode） |
| `workshop_data.json` | 首次上傳後由 Mods 管理器寫入 ItemId；現為空值模板 |
| `COVER_PROMPTS.md` | 封面 AI 提示詞與規格（可重生成） |

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
