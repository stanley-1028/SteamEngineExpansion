# Steam Engine Expansion — Steam Workshop 封面生成提示詞（給 AI 生圖用）

> 用途：產生 Steam Workshop 封面主圖（16:9）與頁面內第二、三張插畫。
> 模組：Steam Engine Expansion（Timberborn 1.1.2.8）｜版本 0.1.0
> 原則：**AI 只生圖，不在圖裡生字**；標題由編輯器壓字（見 §6）。封面是風格化插畫，不冒充遊戲截圖。

---

## 0. 30 秒結論

1. 用 **Variant A** 生 16:9／2K，一次抽 2–4 張，挑「一眼看得出是 Timberborn + 一台大型蒸汽引擎」的那張。
2. 保持**無字**；用 §6 的規格壓上 `STEAM ENGINE`／`EXPANSION` 與副標。
3. 縮到 **322×181** 檢查：大型引擎輪廓、煙囪蒸氣、至少一隻海狸要能辨識。
4. 中心裁 **1:1** 檢查（Timberborn 工坊使用方形縮圖）。
5. 第二、三張用 Variant B（維護特寫）與 C（兩陣營對照），不要全擠在封面。

---

## 1. 生圖模型與參數

| 模型 | 建議參數 | 備註 |
|---|---|---|
| **Seedream 5.0 Lite**（豆包／火山方舟／fal `bytedance/seedream/v5/lite`） | Landscape **16:9**、2K、每次 2–4 張 | 沒有負面提示詞欄位；用正面描述；**不要**寫 masterpiece／8K／ultra-detailed |
| **Midjourney v7** | `--ar 16:9 --v 7 --style raw` | 可用 `--no` 排除文字／UI／水印 |
| **Flux 1.1 Pro / Kontext** | 16:9、guidance 2.5–3.5 | 可用負面提示詞（§5） |
| **DALL·E 3 / GPT Image** | 16:9、自然語言長句 | 會嘗試加字，務必寫 "no text" |

提示詞語言：英文最穩；中文模型可直接貼 §3 的中文版。**不要**在提示詞裡要求文字、Logo、UI 面板、水印。

---

## 2. 生圖前的世界觀錨點（避免模型畫錯）

- **物種**：全部是**海狸（beavers）**，不是人類、不是老鼠；卡通比例、戴工作帽、手拿工具。
- **風格**：Timberborn 的低多邊形（chunky low-poly）＋手繪貼圖、溫暖童話感；不是寫實工業、不是蒸氣龐克黑暗風。
- **材質**：木材（原木、厚木板、木樑）＋黃銅／銅管＋鉚釘鐵板；蒸氣、煙、齒輪、飛輪。
- **世界**：綠色草地、河流、松樹、石階地、木造工業小鎮。
- **建築**：蒸汽引擎是**木構鍋爐＋黃銅箍＋圓形壓力錶＋大型輻條飛輪＋煙囪**，占地是 2×2／3×3／4×4 的方形多層建築。
- **品牌**：**不要**畫 Timberborn 官方 Logo、遊戲 UI、其他模組素材。

---

## 3. Variant A — 主封面（推薦）

**目標**：一眼看出「Timberborn 的海狸城鎮裡，有一台大型免人力蒸汽引擎在供電給工廠」。
**參數**：16:9 / 2K。

### 3.1 英文提示詞（Seedream／Flux／DALL·E）

```
Chunky low-poly game-art illustration in the cheerful Timberborn style: a large steam engine building in a beaver timber town, three storeys tall, built from dark wooden beams, riveted iron plates and polished brass bands, with a round pressure gauge, a big spoked flywheel on its side and two smokestacks puffing soft white steam. Two small beaver engineers in work caps operate a brass valve and carry a log; one more beaver pushes a cart of logs. A copper pipe runs from a round wooden water tank on the right into the boiler, and a thick power shaft with wooden gears leads to a sawmill on the left. Late-afternoon golden light from the left, teal river and pine forest behind, warm dust in the air, hand-painted flat textures, soft brush strokes, isometric three-quarter view, the engine sits slightly right of centre with open sky and a grassy clearing on the left, no text, no logos, no user interface.
```

### 3.2 中文提示詞（豆包／即夢／Seedream 中文框）

```
低多邊形、手繪貼圖的可愛遊戲插畫，Timberborn 風格的海狸木造城鎮：畫面中央偏右是一棟三層高的蒸汽引擎建築，
深色木樑、鉚釘鐵板與拋光黃銅箍，側面有圓形壓力錶與大型輻條飛輪，兩根煙囪冒著柔和白煙。兩隻戴工作帽的
小海狸在轉黃銅閥門、搬運原木；右側圓木水箱用銅管接到鍋爐，左側粗傳動軸與木齒輪連到鋸木廠。午後金光
從左側照入，背景是青色河流與松林，空氣中有暖色微塵；畫面左側留出草地與天空放標題，不要文字、不要 Logo、
不要遊戲 UI。
```

### 3.3 必須檢查（不合格就重抽）

- [ ] 主角是**一台大型蒸汽引擎**，不是火車、不是蒸氣船、不是工廠廠房；
- [ ] 有**海狸**在操作（至少 2 隻），不是人類；
- [ ] 有煙／蒸氣、飛輪、壓力錶、木＋黃銅材質；
- [ ] 左側留白足夠壓標題（約 1/3 畫面）；
- [ ] 沒有文字、Logo、UI、浮水印；
- [ ] 縮到 322×181 時，引擎巨大輪廓＋白煙＋海狸仍可辨識；
- [ ] 中心 1:1 方裁時主體不丟。

### 3.4 常見失敗與補一句修正

| 失敗 | 補丁（加在提示詞尾） |
|---|---|
| 畫成寫實工業／蒸氣龐克黑暗風 | `flat hand-painted textures, soft brush strokes, cheerful storybook mood, low-poly shapes` |
| 出現人類或工人 | `all characters are small chunky beavers with flat tails and work caps, no humans` |
| 建築比例奇怪、像教堂 | `the building is a square timber industrial hall, wider than tall, with a visible round flywheel on its side` |
| 沒有蒸汽 | `two smokestacks with soft white steam and a faint whistle of vapour around the flywheel` |
| 左側太滿沒法放字 | `the engine sits slightly right of centre, open sky and a grassy clearing on the left` |
| 飛輪／齒輪像現代機械 | `wooden gears and brass fittings, simple chunky shapes, no modern machinery` |

---

## 4. Variant B — 維護特寫（頁面第二張，強烈建議）

**目標**：解釋「有機況、有維護材料、有停機」這個核心差異；不是封面候選。
**參數**：16:9 / 2K。

```
Close-up game-art illustration in the cheerful Timberborn style: a beaver mechanic with a work cap and a wooden wrench stands on a small platform beside a huge brass steam boiler, tightening a bolt on a circular pressure gauge while the big spoked flywheel turns behind. A wooden crate of planks and gears sits on the platform, a copper water pipe with a valve feeds the boiler, soft white steam escapes from a seam. Warm workshop lighting at dusk, dark timber beams, riveted iron and polished brass, hand-painted flat textures, chunky low-poly shapes, shallow depth of field, no text, no logos, no user interface.
```

檢查：海狸拿工具、看得到壓力錶與飛輪、有材料箱；不要出現扳手變形到無法辨識。

---

## 5. Variant C — 兩陣營對照（頁面第三張）

**目標**：同一台引擎在 Folktails 與 Iron Teeth 的材質差異（木＋銅 vs 灰鐵＋鉚釘）。
**參數**：16:9 / 2K。

```
Two steam engines of the same size stand side by side on a green meadow in the cheerful Timberborn style, same isometric three-quarter view. The left one is Folktails: warm honey wood, rounded copper bands, leafy green accents, a wooden water barrel. The right one is Iron Teeth: grey riveted iron plates, dark steel pipes, heavy gears, a metal water tank. Both have a big spoked flywheel, a round pressure gauge, two smokestacks with soft white steam, and a small beaver engineer at each door. Late-afternoon sun from the left, pine forest behind, flat hand-painted textures, chunky low-poly shapes, equal size and spacing, no text, no logos, no user interface.
```

---

## 6. 封面文字：在編輯器壓，不要交給 AI

**規格（1920×1080 畫布）**

| 元素 | 規格 |
|---|---|
| 主標 | `STEAM ENGINE`（第一行）＋ `EXPANSION`（第二行），左上或左中，佔寬 45–55%，字高 110–130 px |
| 副標 | `12 BOILERS · 2 FACTIONS · MAINTENANCE`，字高 40–50 px，最多一行 |
| 字體 | 木刻／粗襯線：Alfa Slab One、Rye、Bungee、Cabin Sketch；不要細無襯線 |
| 顏色 | 奶油白 `#F5E7C6`＋深棕描邊 `#241610`＋淡投影；或壓在深木色橫條上 |
| 安全區 | 四周留 ≥8%；文字不壓引擎主體；**中心 1:1 方裁**後標題不丟 |
| 禁止 | 中文大字（322 px 時會糊）、AI 生成字、浮水印、"AI generated" |

**素材**
- `cover-A-master.png`：無字原圖（換版本、換字不用重生成）
- `workshop-preview-1920x1080.jpg`：壓字上傳版，JPG 品質 ~90、<1 MB

---

## 7. 負面提示詞（Flux／Midjourney 用；Seedream 不需要）

```
text, watermark, logo, user interface, HUD, screenshot, photorealistic, dark grimdark, coal smoke pollution, human workers, train, steamship, factory chimney skyline, modern machinery, blurry, deformed beavers, extra limbs, low contrast
```

---

## 8. 上傳前 QA

1. **比例**：1920×1080，16:9，<1 MB。
2. **小圖**：縮到 322×181，3 秒內能讀出「大型蒸汽引擎＋白煙＋海狸」。
3. **方裁**：中心裁 1080×1080，主體與標題都在。
4. **合規**：無官方 Logo、無其他模組素材、無浮水印（豆包 App 常帶角標，建議用 API 或裁掉）。
5. **誠實**：封面是插畫；頁面內第二張起請放**真實遊戲截圖**（引擎外觀、面板、燃料／水補給、維護中的機況）。
6. **一致性**：封面出現的元素（飛輪、壓力錶、煙囪、木水箱）在頁面截圖中也要看得到。
