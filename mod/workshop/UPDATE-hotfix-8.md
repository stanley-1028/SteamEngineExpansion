# Steam Workshop 更新（hotfix-8，2026-11-15）— 可直接貼上的欄位

> 用法：Timberborn → 主選單 **Mods** → **Steam Engine Expansion** → **Update / Publish**，
> 或使用擁有者帳號以 `steamcmd +workshop_build_item`（流程見 `mod/workshop/README.md` A 節）。
> 已安裝的 `%USERPROFILE%\Documents\Timberborn\Mods\SteamEngineExpansion` 已包含熱修-8 的 132 檔
> ＋`workshop_data.json`（ItemId 3836039641）＋`thumbnail.png`。
> 熱修-8 完成後**尚未上傳**（線上仍是熱修-7）；上傳時 Title／Preview／Visibility／Tags 可維持不變。

## Name（Title 欄，維持不變）

```
Steam Engine Expansion — 12 worker-free steam engines for Folktails & Iron Teeth
```

## Description（描述欄）

直接使用 `mod/workshop/DESCRIPTION.md` 全文（簡中 BBCode）；英文頁面用 `mod/workshop/DESCRIPTION.en.md`。
兩檔已改為「24 檔併入原版 Power 分類」的熱修-8 內容。

## Changelog（更新說明欄，簡中 BBCode）

```bbcode
[h1]更新（2026-11-15，热修-8）[/h1]
[list]
[*][b]建筑菜单整合回原版动力分类[/b]：12 档蒸汽引擎不再使用专属「蒸汽引擎」分类，改列在建造菜单的 [b]Power／动力[/b] 分类内（该分类可展开／收合），与原版动力建筑同列、按尺寸/等级排序。
[*][b]三语界面同步[/b]：本地化回到 63 个键（移除专属分类名称），简中／繁中／英文一致。
[*][b]保留[/b]：原版外观面板（九宫格卡片、原版按钮/开关/进度条/下拉框、标题可收合）与热修-7 的下拉修复（选中非引擎建筑时不再关闭其它面板的下拉清单）。
[/list]
```

## Tags（標籤）

維持線上既有值（steamcmd 不支援改 tags；目前為 `Mod`／`Update 1.1`／`New content`）。

## 待辦（發佈後）

- 於 GitHub 建立 tag／release `v0.1.0-hotfix-8`（release asset：`dist/SteamEngineExpansion-0.1.0.zip`）。
- 更新 `CHANGELOG.md` 的發佈記錄與 README 的「線上版本」狀態。
