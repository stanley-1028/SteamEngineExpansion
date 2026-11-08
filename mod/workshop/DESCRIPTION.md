# Steam Workshop 頁面文案（Steam Engine Expansion）

> 用途：Steam 工作坊物品標題／描述／標籤。實際發佈需在遊戲內 Mods 管理器操作（見 `mod/workshop/README.md`）。

## 標題

**Steam Engine Expansion — 12 worker-free boilers, 2 factions, maintenance**

## 簡短描述

Twelve worker-free steam engines for Timberborn (3 sizes × 4 tiers, both factions): burn logs or planks and water to produce 130–1500 hp, with load-scaled fuel use and periodic maintenance. Soft-compatible with Timber Pipes, Power Lines and Conveyor Belts v2 — fully playable without them.

## 詳細描述（Steam BBCode 可用 [b] [i] [list] [*] [h1]）

[h1]Steam Engine Expansion[/h1]

在 Timberborn 的中後期，動力與飲水都綁在人力上：多蓋水泵與多蓋踩輪其實在搶同一批海狸。本模組把「動力」變成不需要工人、也不需要盯著的基礎設施——用燃料與水換取穩定動能，再接到你的機械水泵與工廠。

[b]12 種建築[/b]
[list]
[*]小型 2×2：130（Mk.I）／260（Mk.II）／380（Mk.III）／520（Mk.IV）
[*]中型 3×3：230／460／680／920
[*]大型 4×4：360／720／1060／1500
[*]兩陣營皆可建：Folktails 輸出 ×1.00、燃料 ×0.90；Iron Teeth 輸出 ×1.05、燃料 ×1.00
[/list]

[b]核心機制[/b]
[list]
[*][b]免工人[/b]：不需要雇員；燃料與水由搬運工、輸送帶、管網補給。
[*][b]比例耗燃料[/b]：低負載按比例省料；夜間以 10% 待機運轉（可在 SEE_Config.json 調整），不再整夜熄火。
[*][b]可見的長期代價[/b]：機況 0–100、6–12 日維護週期、材料由地區庫存扣除、缺料／無路停機但不會崩潰。
[*][b]雙輸入[/b]：燃料（原木；Mk.III／IV 可用木板，1 木板＝2 原木能量）＋獨立水艙（搬運工補水）。
[/list]

[b]軟依賴（全部缺席也能玩）[/b]
[list]
[*]Timber Pipes：目前以搬運工補水（tank／valve 模式待後續版本）。
[*]Power Lines：偵測到時可選擇注入電纜接點，失敗自動退回原版傳動軸。
[*]Conveyor Belts v2：輸送帶可直接餵燃料。
[/list]

[b]不覆寫原版[/b]：原版蒸汽機（Iron Teeth、400 hp）與原版數值保持不變；本模組只新增自己的模板與本地化鍵。

[h1]Credits[/h1]
本模組與 [b]DeepSeek V4.1 Flash 1017[/b] 共同完成：程式、資料與平衡、自製模型／動畫管線、離線驗證、文件與封面皆為人機協作產出；需求、方向與遊戲內驗收由專案擁有者負責。

[h1]Known limitations (v0.1.0)[/h1]
[list]
[*]模型與動畫仍在調整；以格線為準。
[*]Timber Pipes 的 tank／valve 供水模式暫不支援，請用搬運工（auto→hauler）。
[*]P8 人類實機驗收清單（T2–T6）尚未全部簽核；詳見 repo 的 docs/TEST_PROTOCOL.md。
[/list]

## 建議標籤

`Update 1.1`、`New content`、`Buildings`

## 變更說明（首次上傳可留空）

初版：12 檔蒸汽引擎、維護系統、燃料／水雙補給、UI 面板、三外部模組軟依賴。
