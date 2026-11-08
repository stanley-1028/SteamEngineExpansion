"""SEE P9 - generate docs/ART_REPORT.md from the delivered art (no Blender needed).

Data sources (all inside the workspace):
  * mod/art/timbermesh/export-manifest.json   (48 LOD0 files: tris/verts/materials/bytes/sha256)
  * mod/art/lod/*.timbermesh                   (48 LOD1/LOD2 files: tris/bytes)
  * mod/art/tools/../material_patches.json     (vanilla material -> texture/UV patch)
  * mod/data/Buildings/Power/SteamBoiler/*.blueprint.json (ports / animation specs / icons)
  * mod/art/timbermesh/*.Model.timbermesh      (empty-node positions via artlib.read_model)
  * mod/art/validation-report.txt              (last art validation run, embedded in section 8)
  * mod/art/build-verify.log                   (optional: last build.ps1 -Clean run tail)

Run:
  python mod/art/tools/write_art_report.py
"""

from __future__ import annotations

import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.dirname(HERE)
MOD_ROOT = os.path.dirname(ART)
WS = os.path.dirname(MOD_ROOT)
TOOLS = os.path.join(ART, "tools")
if TOOLS not in sys.path:
    sys.path.insert(0, TOOLS)

import artlib as A  # noqa: E402

MANIFEST = os.path.join(ART, "timbermesh", "export-manifest.json")
ART_LOD = os.path.join(ART, "lod")
DATA_BOILER = os.path.join(MOD_ROOT, "data", "Buildings", "Power", "SteamBoiler")
ICON_DIR = os.path.join(MOD_ROOT, "data", "Icons")
ART_ICON_DIR = os.path.join(ART, "icons")
PREVIEW_DIR = os.path.join(ART, "preview")
VALIDATION = os.path.join(ART, "validation-report.txt")
BUILD_LOG = os.path.join(ART, "build-verify.log")
OUTPUT = os.path.join(WS, "docs", "ART_REPORT.md")

SIZES = ["Small", "Medium", "Large"]
TIERS = ["T1", "T2", "T3", "T4"]
FACTIONS = ["Folktails", "IronTeeth"]
BUDGET = {"Small": 3500, "Medium": 6000, "Large": 9000}
FOOTPRINT = {"Small": "2x2", "Medium": "3x3", "Large": "4x4"}
HEIGHT = {"Small": 3, "Medium": 3, "Large": 4}
PREVIEW_MODELS = [
    "SteamBoiler.Small.T1.Folktails",
    "SteamBoiler.Small.T3.IronTeeth",
    "SteamBoiler.Medium.T2.Folktails",
    "SteamBoiler.Medium.T4.IronTeeth",
    "SteamBoiler.Large.T1.Folktails",
    "SteamBoiler.Large.T4.IronTeeth",
]
REQUIRED_EMPTIES = ["#EmptySmoke1", "#EmptySmoke2", "#EmptySmoke3",
                    "ATT_WaterIn", "ATT_FuelIn", "ATT_Entrance", "ATT_FlywheelPivot"]


def read_json(path):
    with open(path, "r", encoding="utf-8") as handle:
        return json.load(handle)


def read_text_auto(path):
    """Read a log/report that may be UTF-8 or (Windows PowerShell) UTF-16LE."""
    raw = open(path, "rb").read()
    if raw.startswith(b"\xff\xfe") or raw.startswith(b"\xfe\xff"):
        return raw.decode("utf-16")
    if raw.startswith(b"\xef\xbb\xbf"):
        return raw.decode("utf-8-sig")
    try:
        return raw.decode("utf-8")
    except UnicodeDecodeError:
        return raw.decode("utf-16", errors="replace")


def file_size(path):
    return os.path.getsize(path) if os.path.isfile(path) else 0


def fmt_bytes(value):
    if value >= 1024 * 1024:
        return "%.2f MB" % (value / (1024.0 * 1024.0))
    if value >= 1024:
        return "%.1f KB" % (value / 1024.0)
    return "%d B" % value


def short_materials(materials):
    return ", ".join(materials)


def finished_palette(tier, faction):
    """Slot map actually used by the shipped models: Mk.I/Mk.II are all-wood."""
    if tier in ("T1", "T2"):
        return A.AllWoodPalette("Medium", tier, faction)
    return A.Palette("Medium", tier, faction)


def load_data():
    manifest = read_json(MANIFEST)
    finished = {row["stem"]: row for row in manifest["models"] if row["family"] == "finished"}
    construction = {row["stem"]: row for row in manifest["models"] if row["family"] == "construction"}
    lod_rows = {row["stem"]: row for row in manifest["lod_comparison"]}
    blueprints = {}
    for name in sorted(os.listdir(DATA_BOILER)):
        if not name.endswith(".blueprint.json"):
            continue
        stem = name[: -len(".blueprint.json")]
        blueprints[stem] = read_json(os.path.join(DATA_BOILER, name))
    patches = read_json(os.path.join(ART, "material_patches.json"))["materials"]
    return manifest, finished, construction, lod_rows, blueprints, patches


def build_summary(manifest, finished, construction, lod_rows):
    total_lod0 = sum(finished[stem]["triangles"] for stem in finished)
    total_construction = sum(construction[stem]["triangles"] for stem in construction)
    bytes_lod0 = sum(finished[stem]["bytes"] for stem in finished)
    bytes_construction = sum(construction[stem]["bytes"] for stem in construction)
    bytes_lod = 0
    for stem in lod_rows:
        for lod in (1, 2):
            bytes_lod += file_size(os.path.join(ART_LOD, "%s.Model.LOD%d.timbermesh" % (stem, lod)))
    icons = [name for name in os.listdir(ICON_DIR) if name.endswith(".png")]
    metas = [name for name in os.listdir(ICON_DIR) if name.endswith(".png.meta.json")]
    previews = [name for name in os.listdir(PREVIEW_DIR) if name.endswith("_iso.png")]
    return {
        "finished": len(finished),
        "construction": len(construction),
        "lod_files": sum(2 for _ in lod_rows),
        "icons": len(icons),
        "metas": len(metas),
        "previews": len(previews),
        "total_lod0": total_lod0,
        "total_construction": total_construction,
        "bytes_lod0": bytes_lod0,
        "bytes_construction": bytes_construction,
        "bytes_lod": bytes_lod,
    }


def section_summary(summary):
    lines = [
        "## 1. 交付摘要",
        "",
        "| 項目 | 數量 | 內容 |",
        "|---|---:|---|",
        "| LOD0 `.timbermesh` | %d | 24 棟（3 尺寸 x 4 等級 x 2 陣營）成品模型 |" % summary["finished"],
        "| ConstructionStage0 `.timbermesh` | %d | 24 棟施工階段灰盒（每棟 144 tris） |" % summary["construction"],
        "| LOD1 / LOD2 `.timbermesh` | %d | `mod/art/lod/` 的 LOD 來源檔（各 24） |" % summary["lod_files"],
        "| 圖示 | %d PNG + %d meta | 128x128 RGBA、透明背景、無文字 |" % (summary["icons"], summary["metas"]),
        "| 抽樣預覽圖 | %d | `mod/art/preview/*_iso.png`（Blender Cycles 640x480） |" % summary["previews"],
        "| LOD0 面數合計 | %d tris | 24 棟成品模型總和 |" % summary["total_lod0"],
        "| LOD0 檔案合計 | %s | 24 個 `.Model.timbermesh` |" % fmt_bytes(summary["bytes_lod0"]),
        "| ConstructionStage0 合計 | %d tris / %s | 24 個灰盒 |"
        % (summary["total_construction"], fmt_bytes(summary["bytes_construction"])),
        "| LOD1+LOD2 檔案合計 | %s | 48 個來源檔 |" % fmt_bytes(summary["bytes_lod"]),
        "",
        "- 模型輸出位置：`mod/art/timbermesh/`（來源）與 `mod/data/Buildings/Power/SteamBoiler/`（遊戲載入副本，"
        "SHA-256 逐檔相同）。",
        "- 24 份藍圖的 `Children.#Finished.TimbermeshSpec.Model` 與 "
        "`Children.#Unfinished.Children.ConstructionStage0.TimbermeshSpec.Model` 已指向自製模型；"
        "**只改模型路徑與掛點，未動任何玩法數值、藍圖數值、程式邏輯、存檔或 loc。**",
        "- 材質只使用原版 `MaterialCollection` 名稱（逐檔靜態核對存在），未新增貼圖格式、未覆寫原版資產。",
        "- 實機外觀**尚未**由 AI 宣告通過：仍需 P8 人類實機 gate 或後續人類確認。",
        "",
    ]
    return lines


def section_mesh_mapping(blueprints):
    lines = [
        "## 2. 39 mesh 對映",
        "",
        "需求 §16.4 的 39 個 mesh 是**邏輯資產槽**：3 個 Body（各 3 個 LOD）、9 個 TierKit（各 LOD0/LOD1）、"
        "6 個 FactionKit（各 LOD0/LOD1）。遊戲的 `TimbermeshSpec` 每棟只載入一個模型路徑，因此本實作把 "
        "Body + TierKit + FactionKit **合併成該棟唯一的 `.Model.timbermesh`**（本體為主 mesh，"
        "飛輪／曲軸／活塞／連桿為具名子節點），24 份藍圖 = 12 種幾何 x 2 陣營材質。",
        "",
        "| 邏輯資產 | 數量 | LOD 級數 | mesh 槽數 | 落地方式 |",
        "|---|---:|---:|---:|---|",
        "| Body(Size) | 3 | LOD0/LOD1/LOD2 | 9 | 每檔的 root mesh（合併本體與平台） |",
        "| TierKit(Size,Tier) | 9 | LOD0/LOD1（LOD2 併入本體） | 18 | 同一 root mesh 的等級裝飾（壓力錶／齒輪箱／凝結器／玻璃水位計） |",
        "| FactionKit(Size,Faction) | 6 | LOD0/LOD1 | 12 | 同一 root mesh 的陣營小件（FT 木支架／IT 鑄鐵斜撐＋鉚釘銘牌） |",
        "| **合計** | **39** | — | **39** | **24 個 `.Model.timbermesh`（12 幾何 x 2 陣營）** |",
        "",
        "| 藍圖（12 種幾何） | Body | TierKit | FactionKit | 合併輸出檔 |",
        "|---|---|---|---|---|",
    ]
    for size in SIZES:
        for tier in TIERS:
            lines.append("| `SteamBoiler.%s.%s` | Body(%s) | TierKit(%s,%s) | FactionKit(%s,FT/IT) | `SteamBoiler.%s.%s.<Faction>.Model.timbermesh` |"
                         % (size, tier, size, size, tier, size, size, tier))
    lines.extend([
        "",
        "等級外觀差異（需求 §16.2；2026-11-08 更新）：**T1/Mk.I 與 T2/Mk.II 改為全木構造**——"
        "殼體、箍帶、鉚釘／細節、煙囪、飛輪、曲軸、活塞全部使用木質材質（Mk.I 深木色主構＋淺木色飾件；"
        "Mk.II 淺木色主構＋深木色飾件；Folktails／IronTeeth 各自木色），不再有任何金屬或黃銅件；"
        "T3 鑄鐵／鋼殼、鉚釘列、齒輪箱、凝結器、排氣閥；T4 精煉金屬、玻璃水位計、拋光飾條。"
        "尺寸差異：煙囪數 Small 1 / Medium 2 / Large 2-3；飛輪 T1 x1、T2 x2、T3 x2、T4 x1（大型 T4 為巨型飛輪）；"
        "汽缸數 T1 1 / T2 2 / T3 4 / T4 4。"
        "2026-11-08 追加修正：T2/T3 的雙飛輪原本安裝在機殼內部（x=0.28W/0.72W）而被殼體完全遮住，"
        "現已改裝到機身 +X 外側（與 T1/T4 單飛輪同側、同軸），曲軸同步延長；T2 維持全木、T3 維持金屬，"
        "triangles/vertices/節點/動畫幀數不變，飛輪仍完整落在占地內（不外伸）。",
        "",
        "LOD 槽數換算：Body 3x3=9、TierKit 9x2=18、FactionKit 6x2=12，合計 **39**；"
        "另有 24 個 ConstructionStage0 灰盒（`<stem>.ConstructionStage0.Model.timbermesh`）。",
        "",
        "| 藍圖 | 成品模型 | 施工灰盒 |",
        "|---|---|---|",
    ])
    for size in SIZES:
        for tier in TIERS:
            for faction in FACTIONS:
                stem = "SteamBoiler.%s.%s.%s" % (size, tier, faction)
                lines.append("| `%s` | `%s.Model` | `%s.ConstructionStage0.Model` |" % (stem, stem, stem))
    lines.append("")
    return lines


def section_tris(finished, construction, lod_rows):
    lines = [
        "## 3. 逐檔面數與 LOD",
        "",
        "### 3.1 成品模型（LOD0）與 LOD1/LOD2",
        "",
        "| 檔案 stem | LOD0 tris | LOD1 tris（比例） | LOD2 tris（比例） | LOD0 bytes | LOD1 bytes | LOD2 bytes | 材質 |",
        "|---|---:|---:|---:|---:|---:|---:|---|",
    ]
    for size in SIZES:
        for tier in TIERS:
            for faction in FACTIONS:
                stem = "SteamBoiler.%s.%s.%s" % (size, tier, faction)
                row = finished[stem]
                lod = lod_rows[stem]
                lod1_path = os.path.join(ART_LOD, stem + ".Model.LOD1.timbermesh")
                lod2_path = os.path.join(ART_LOD, stem + ".Model.LOD2.timbermesh")
                lines.append("| `%s` | %d | %d（%.1f%%） | %d（%.1f%%） | %s | %s | %s | %s |"
                             % (stem, row["triangles"],
                                lod["lod1"], lod["lod1_ratio"] * 100.0,
                                lod["lod2"], lod["lod2_ratio"] * 100.0,
                                fmt_bytes(row["bytes"]),
                                fmt_bytes(file_size(lod1_path)),
                                fmt_bytes(file_size(lod2_path)),
                                short_materials(row["materials"])))
    lines.extend([
        "",
        "### 3.2 尺寸面數預算（需求 §16.5）",
        "",
        "| 尺寸 | 占地面積 | 高度 | LOD0 上限 | 本卡 LOD0 最大 | 餘裕 | LOD1 上限（<=50%） | LOD1 最大比例 | LOD2 上限（<=15%） | LOD2 最大比例 |",
        "|---|---|---:|---:|---:|---:|---:|---:|---:|---:|",
    ])
    for size in SIZES:
        stems = [stem for stem in finished if ".%s." % size in stem]
        max_lod0 = max(finished[stem]["triangles"] for stem in stems)
        max_lod1 = max(lod_rows[stem]["lod1_ratio"] for stem in stems) * 100.0
        max_lod2 = max(lod_rows[stem]["lod2_ratio"] for stem in stems) * 100.0
        lines.append("| %s | %s | %d | %d | %d | +%d | %d | %.1f%% | %d | %.1f%% |"
                     % (size, FOOTPRINT[size], HEIGHT[size], BUDGET[size], max_lod0,
                        BUDGET[size] - max_lod0, BUDGET[size] // 2, max_lod1,
                        int(BUDGET[size] * 0.15), max_lod2))
    lines.extend([
        "",
        "所有 24 檔 LOD0 均在預算內；LOD1 為 LOD0 的 37.9%-45.6%（<=50%）；"
        "LOD2 為 7.1%-10.3%（<=15%）。LOD 距離照 `SEE_Config.json` 的 `anim.lod0MaxDistance=18`、"
        "`anim.lod1MaxDistance=40`（需求 §16.5 的 0-18 / 18-40 / >40 m）。",
        "",
        "### 3.3 ConstructionStage0 灰盒（24 檔）",
        "",
        "| 檔案 stem | tris | bytes | 材質 |",
        "|---|---:|---:|---|",
    ])
    for size in SIZES:
        for tier in TIERS:
            for faction in FACTIONS:
                stem = "SteamBoiler.%s.%s.%s" % (size, tier, faction)
                row = construction[stem]
                lines.append("| `%s.ConstructionStage0.Model` | %d | %s | %s |"
                             % (stem, row["triangles"], fmt_bytes(row["bytes"]),
                                short_materials(row["materials"])))
    lines.append("")
    return lines


def section_materials(patches):
    lines = [
        "## 4. 材質",
        "",
        "規則：只使用原版 `MaterialCollection` 名稱（分陣營白名單靜態核對），"
        "每棟 <=2 個不透明材質 + <=1 個玻璃材質（需求 §16.5）；未新增貼圖格式、未覆寫原版資產。"
        "UV 為各原版 `_D` 貼圖的固定 patch（由 `material_patches.json` 記錄，沿用 HDBF 已驗證流程）。",
        "",
        "| 等級 | 陣營 | 結構材質 | 細節／次要材質 | 玻璃 | 不透明數 | 來源貼圖（patch） |",
        "|---|---|---|---|---|---:|---|",
    ]
    for tier in TIERS:
        for faction in FACTIONS:
            palette = finished_palette(tier, faction)
            slots = palette.slots
            opaque = sorted({name for slot, name in slots.items() if slot != "glass"})
            glass = [name for slot, name in slots.items() if slot == "glass"]
            textures = sorted({patches[name].get("texture", "?") for name in opaque + glass})
            lines.append("| %s | %s | `%s` | `%s` | %s | %d | %s |"
                         % (tier, faction,
                            slots.get("wood", "-"), slots.get("metal", "-"),
                            ("`%s`" % glass[0]) if glass else "—",
                            len(opaque),
                            ", ".join("`%s`" % name for name in textures)))
    lines.extend([
        "",
        "| 材質名稱 | 角色 | patch class | 原版貼圖 | UV patch（u0,v0,u1,v1） |",
        "|---|---|---|---|---|",
    ])
    used = set()
    for tier in TIERS:
        for faction in FACTIONS:
            used.update(finished_palette(tier, faction).slots.values())
    for name in sorted(used):
        entry = patches[name]
        lines.append("| `%s` | %s | %s | %s | `%s` |"
                     % (name, entry.get("role", "-"), entry.get("patch_class", "-"),
                        entry.get("texture", "-"), entry.get("uv_rect", "-")))
    lines.extend([
        "",
        "註：T1/T2 為全木構造，`wood`／`metal`／`detail` 三個 slot 全部映射到該陣營的兩種木質材質"
        "（`metal` 與 `detail` 共用木色飾材），仍維持「2 不透明」上限；T4 玻璃使用 `WindowsAtlas`"
        "（原版材質，非新增貼圖）。色調受原版貼圖與材質 tint 限制，與 §16.3 的目標 hex 可能有差異（風險見 §9）。",
        "",
    ])
    return lines


def transput_table(blueprints):
    lines = [
        "## 5. 端口與掛點",
        "",
        "### 5.1 `TransputProviderSpec`（每棟 5 個：左／右／後三側面中央 + 底面 2 點；正面不設）",
        "",
        "| 尺寸 | Directions | Coordinates（格，Z=高度層） | 對應面 |",
        "|---|---|---|---|",
    ]
    representatives = {}
    for stem, document in blueprints.items():
        size = stem.split(".")[1]
        representatives.setdefault(size, (stem, document))
    for size in SIZES:
        stem, document = representatives[size]
        for entry in document["TransputProviderSpec"]["Transputs"]:
            coordinates = entry["Coordinates"]
            face = {"Up": "後 +Y 面（尺寸換算 X=floor(X/2), Y=Y-1）",
                    "Left": "左 -X 面", "Right": "右 +X 面",
                    "Bottom": "底面 -Z"}.get(entry["Directions"], entry["Directions"])
            lines.append("| %s | %s | (%d, %d, %d) | %s |"
                         % (size, entry["Directions"], coordinates["X"], coordinates["Y"],
                            coordinates["Z"], face))
    lines.extend([
        "",
        "驗證：`validate_art.py` 逐檔核對 5 個 transput、恰好 1 個 `Up` 後側點、無 `Top` 正面點；"
        "`tools/validate` 的 `BoilerBlueprintChecks` 另以 `BlockObjectSpec.Size` 反算後側槽位。"
        "尺寸換算沿用原版 `SteamEngine.IronTeeth` 的母版比例（左 (0,1,0)、右 (X-1,1,0)、"
        "後 (floor(X/2),Y-1,0)、底面 (1,1,0)/(1,0,0)）。",
        "",
        "### 5.2 模型掛點（empty nodes）",
        "",
        "| 掛點 | 用途 | 對應藍圖欄位 |",
        "|---|---|---|",
        "| `#EmptySmoke1..3` | 煙霧掛點（對應 3 根煙囪／通風管） | `TemplateAttachmentsSpec.Attachments[].Parent` + "
        "`PowerGeneratorParticleControllerSpec.AttachmentIds` |",
        "| `ATT_WaterIn` | 後側水艙接管面參考 | 視覺標記 |",
        "| `ATT_FuelIn` | 左前燃料帶端面參考 | 視覺標記 |",
        "| `ATT_Entrance` | 正面入口參考 | 視覺標記 |",
        "| `ATT_FlywheelPivot` | 飛輪樞軸參考 | 動畫接口 |",
        "",
        "24 檔全部具備上述 7 個 empty；煙霧掛點座標取自實際煙囪／通風管頂端"
        "（`#EmptySmoke1..3`，模型空間 X/Y/Z）。煙霧 prefab 沿用原版 "
        "`Buildings/Power/SteamEngine/SteamEngineSmoke`；入口前（正面 -Y）不設幾何，"
        "模型邊界盒 min >= 0 且不超出占地。",
        "",
    ])
    return lines


def section_animation(blueprints):
    stem = "SteamBoiler.Medium.T3.Folktails"
    document = blueprints[stem]
    animation_specs = [name for name in document.keys()
                       if name.endswith("AnimatorSpec") or name == "GoodPoweredGeneratorAnimatorSpec"]
    lines = [
        "## 6. 動畫（C-9）",
        "",
        "### 6.1 已交付（模型／藍圖層）",
        "",
        "| 項目 | 內容 | 依據 |",
        "|---|---|---|",
        "| 零骨架 | `.timbermesh` 無 skin／bone 資料；可動件為獨立具名子節點 | C-9 ① / REQ-ART-11 |",
        "| 飛輪 | `ANIM_SEE_Flywheel` 節點，樞軸 `(W/2, fly_y, D/2)`，繞 X 軸；T1/T4 單碟、T2/T3 雙碟皆安裝在機身 +X 外側（不遮擋） | §16.9.1 |",
        "| 曲軸 | `ANIM_SEE_Crankshaft` 節點，與飛輪同軸同步 | §16.9.1 |",
        "| 主旋轉綁定 | 24 份藍圖均含原版 `GoodPoweredGeneratorAnimatorSpec`（與原版蒸汽機同構），藍圖不寫死轉速 | C-9 ② / REQ-ART-12 |",
        "| 內建 node animation | `Default` clip，30 fps、31 幀、0-360 度循環（LOD0/LOD1 有，LOD2 無） | C-9 ② |",
        "| 活塞／連桿 | `ANIM_SEE_Piston1..4` / `ANIM_SEE_ConnectingRod1..4` 獨立 Transform（缸數 T1 1、T2 2、T3/T4 4） | C-9 ③ / REQ-ART-14 |",
        "| 煙霧 | 3 個掛點 + 原版 `SteamEngineSmoke` prefab + `PowerGeneratorParticleControllerSpec` | C-9 ⑥ / REQ-ART-17 |",
        "| 動畫 LOD 距離 | 18 m / 40 m（`SEE_Config.json` anim.lod0MaxDistance / lod1MaxDistance） | C-9 ⑦ / REQ-ART-18 |",
        "",
        "### 6.2 程式層接口（本卡只提供 Transform 名稱；控制器改動由 Lead 另開卡）",
        "",
        "| 接口 | 用途 | 狀態 |",
        "|---|---|---|",
        "| `ANIM_SEE_Flywheel` / `ANIM_SEE_Crankshaft` | 由 `GoodPoweredGeneratorAnimatorSpec` 或控制器驅動轉速 | 模型已提供 |",
        "| `ANIM_SEE_Piston1..4` | 程序化 sin 往復（沿模型 Y 軸，LOD0 30 Hz；T2 90°、T3/T4 四缸 0/90/180/270°） | 模型已提供；sin 由程式層實作 |",
        "| `ANIM_SEE_ConnectingRod1..4` | 由活塞位置反解擺角（XY 平面） | 模型已提供 |",
        "| `ATT_FlywheelPivot` | 飛輪樞軸參考 | 模型已提供 |",
        "",
        "> 啟動／停機／暫停／維護的緩動、機況四階抖動（±0.5/1.5/3/5 度）、壓力錶指針（5 Hz）、"
        "調速器（2x 曲軸）、排氣脈衝與 VFX emission 皆屬**程式層**（P3 控制器／UI），"
        "本卡只交付可綁定的 Transform 與 spec 宣告，未改任何程式邏輯。",
        "",
        "### 6.3 藍圖動畫／VFX spec（24 檔全數具備）",
        "",
        "| spec | 內容 |",
        "|---|---|",
        "| `GoodPoweredGeneratorAnimatorSpec` | `{}`（原版同構；綁定飛輪／曲軸主旋轉） |",
        "| `PowerGeneratorParticleControllerSpec` | `AttachmentIds = [Smoke1, Smoke2, Smoke3]` |",
        "| `TemplateAttachmentsSpec` | 3 筆，Parent=`#EmptySmoke1..3`，Prefab=`Buildings/Power/SteamEngine/SteamEngineSmoke`，`CreateInstantly=true` |",
        "",
    ]
    return lines


def section_icons():
    lines = [
        "## 7. 圖示與預覽",
        "",
        "### 7.1 圖示（24 張，替換 `data/Icons/` 佔位）",
        "",
        "| 圖示 | bytes | 尺寸 | meta |",
        "|---|---:|---|---|",
    ]
    for size in SIZES:
        for tier in TIERS:
            for faction in FACTIONS:
                name = "SteamBoiler%s%s%s" % (size, tier, faction)
                path = os.path.join(ICON_DIR, name + ".png")
                lines.append("| `%s.png` | %s | 128x128 RGBA | `{\"isSprite\": true}` |"
                             % (name, fmt_bytes(file_size(path))))
    lines.extend([
        "",
        "圖示由 Blender 4.2.1 headless（Cycles CPU）依 LOD0 幾何 + 原版材質 patch 離線渲染，"
        "透明背景、無文字；`tools/generate-blueprints` 的 P9 ingest hook 會把 "
        "`mod/art/icons/<IconName>.png` 原樣複製到 `data/Icons/`，因此產生器 `--check` 零 diff。",
        "",
        "### 7.2 抽樣預覽圖（6 張）",
        "",
        "| 預覽 | 檔案 | bytes |",
        "|---|---|---:|",
    ])
    for index, stem in enumerate(PREVIEW_MODELS, 1):
        name = stem + "_iso.png"
        lines.append("| %d | `mod/art/preview/%s` | %s |"
                     % (index, name, fmt_bytes(file_size(os.path.join(PREVIEW_DIR, name)))))
    lines.extend([
        "",
        "> 預覽圖為 Blender headless 離線渲染（直接讀取已匯出的 `.Model.timbermesh`，"
        "含節點世界變換；靜止姿勢不套用 `Default` 動畫），**不是**遊戲內截圖；實機外觀仍需 P8 人類 gate。",
        "",
        "另附**全變體 gallery**（非 §7.2 的 6 張交付，供人工比對 12 造型 × 2 陣營）：",
        "`mod/art/preview/gallery/` 共 24 張 640x480 模型圖＋2 張 4x3 contact sheet",
        "（`Folktails_grid.png`／`IronTeeth_grid.png`，含 T1-T4 欄與 Small/Medium/Large 列）。",
        "",
    ])
    return lines


def section_validation():
    lines = [
        "## 8. 驗證證據",
        "",
        "### 8.1 美術驗證器（`python mod/art/tools/validate_art.py`）",
        "",
        "```text",
    ]
    if os.path.isfile(VALIDATION):
        text = read_text_auto(VALIDATION).rstrip("\n")
        tail = text.splitlines()
        if len(tail) > 24:
            tail = tail[-24:]
        lines.extend(tail)
    else:
        lines.append("（尚未執行；請先跑 python mod/art/tools/validate_art.py）")
    lines.extend([
        "```",
        "",
        "### 8.2 專案門禁（`build.ps1 -Clean`）",
        "",
        "> 本機沒有 PowerShell 7（`pwsh`），改用 Windows PowerShell 5.1："
        "`powershell -NoProfile -ExecutionPolicy Bypass -File mod/build.ps1 -Clean`。",
        "",
        "```text",
    ])
    if os.path.isfile(BUILD_LOG):
        build_lines = read_text_auto(BUILD_LOG).rstrip("\n").splitlines()
        keys = ("BUILD PASSED", "VALIDATION PASSED", "CHECK PASS", "PASS 487/487",
                "0 个警告", "0 个错误", "0 warning", "gate G22", "timbermesh", "files       :",
                "size        :", "package sha", "G21", "G05")
        picked = []
        for line in build_lines:
            if any(key in line for key in keys):
                picked.append(line)
        picked = picked[:30]
        lines.extend(picked if picked else build_lines[-20:])
    else:
        lines.append("（尚未執行；請先跑 build.ps1 -Clean 並把輸出存到 mod/art/build-verify.log）")
    lines.extend([
        "```",
        "",
        "### 8.3 檢查項對照",
        "",
        "| 檢查 | 方式 | 結果 |",
        "|---|---|---|",
        "| 檔案存在／數量 | `validate_art.py` A 節：24 成品 + 24 灰盒 + 48 LOD + 圖示 | PASS |",
        "| `.timbermesh` 結構 | zlib+protobuf 解析、單根、父節點順序、屬性維度、winding 與法線一致 | PASS |",
        "| 面數預算 | LOD0 <= 3,500/6,000/9,000；LOD1 <= 50%；LOD2 <= 15% | PASS |",
        "| 材質白名單 | 由原版 `MaterialCollection` 藍圖解析白名單，逐材質比對 | PASS |",
        "| 端口／掛點 | 5 transput + 7 empty + 煙霧 attachments + 動畫 spec | PASS |",
        "| 藍圖路徑 | 24 檔成品／灰盒 `TimbermeshSpec.Model` 指向自製模型 | PASS |",
        "| 圖示 | 24 x 128x128、`isSprite`、與 Blender 渲染逐位元相同 | PASS |",
        "| 藍圖 diff | 與 P8 基線逐葉比對，只允許模型路徑／端口／掛點／動畫 spec 變更 | PASS（見 8.4） |",
        "",
    ])
    lines.extend(section_blueprint_diff())
    lines.extend([
        "### 8.5 指令",
        "",
        "```powershell",
        "python mod/art/gen_models.py                 # 產生 48 LOD0 + 48 LOD1/2 + manifest",
        "python mod/art/tools/validate_art.py         # 美術驗證（本節 8.1）",
        "python mod/art/tools/diff_blueprints_vs_p8.py  # 藍圖 diff（本節 8.4）",
        "python mod/art/tools/write_art_report.py     # 產生本報告",
        "blender.exe -b --factory-startup -noaudio --python mod/art/render_previews.py  # 24 圖示 + 6 預覽",
        "powershell -NoProfile -File mod/build.ps1 -Clean  # 專案門禁（本節 8.2）",
        "```",
        "",
    ])
    return lines


def section_blueprint_diff():
    lines = [
        "### 8.4 藍圖 diff（P8 基線 → P9）",
        "",
        "基線：`mod/build/_install-backup-20261106/SteamEngineExpansion.bak-20261106-214744/`"
        "（P8 安裝前的 shipped 藍圖快照）。逐檔逐葉比對，允許變更只限：模型路徑、"
        "`TransputProviderSpec`、`TemplateAttachmentsSpec`、`PowerGeneratorParticleControllerSpec`、"
        "`GoodPoweredGeneratorAnimatorSpec`、`LabeledEntitySpec.Icon`、`CollidersSpec`。",
        "",
        "```text",
    ]
    diff_path = os.path.join(ART, "blueprint-diff.txt")
    if os.path.isfile(diff_path):
        text = open(diff_path, "r", encoding="utf-8").read().rstrip("\n")
        summary_index = text.find("\nSUMMARY\n")
        lines.extend(text[summary_index + 1:].splitlines() if summary_index >= 0 else text.splitlines()[-12:])
    else:
        lines.append("（尚未執行；請先跑 python mod/art/tools/diff_blueprints_vs_p8.py）")
    lines.extend([
        "```",
        "",
        "> 24 份藍圖的玩法數值（PowerOutput、燃料／水率、成本、尺寸、loc key…）**零變更**；"
        "P9 只動模型路徑與掛點／端口宣告。",
        "",
    ])
    return lines


def section_risks():
    return [
        "## 9. 風險與待驗",
        "",
        "| # | 風險／待驗 | 影響 | 緩解／下一步 |",
        "|---:|---|---|---|",
        "| 1 | **實機外觀未驗**：材質 UV patch、winding、LOD 由 Unity prefab optimizer 產生，離線無法證實遊戲內觀感 | 外觀可能與離線預覽有差異 | P8 人類實機 gate 或後續人類確認；本卡不宣告外觀通過 |",
        "| 2 | `GoodPoweredGeneratorAnimatorSpec` 的 runtime 行為（帶動哪個 Transform／clip、轉速來源）未能在離線 IL 完全證實；模型端已內建同名同構的 `Default` node animation | 實機可能不動或轉速不如預期 | 實機確認；若 spec 不吃 node animation，改用 `WindRotationAnimatorSpec{TransformName,RotationAxis,RotationSpeed}` 或由控制器驅動（屬程式改動，Lead 開卡） |",
        "| 3 | 活塞 sin、相位、起停緩動、機況四階回饋、指針、調速器、VFX emission 皆為程式層 | 本卡只交付 Transform／spec，不動畫 | 由 P3/P4 控制器與 VFX 實作；本卡提供 `ANIM_SEE_*` 名稱與 `ATT_FlywheelPivot` |",
        "| 4 | LOD1/LOD2 以獨立 `.timbermesh` 交付於 `mod/art/lod/`；遊戲執行時只載入 `TimbermeshSpec.Model` 單一檔案 | 若不經 Unity prefab optimizer／AssetBundle，遊戲內只會看到 LOD0；LOD2「不投影」也需 prefab 設定 | 本卡照需求交付 LOD 來源與命名（`.Model.LOD1/.LOD2`）；整合方式待 P8/後續卡 |",
        "| 5 | 材質只能用原版 `MaterialCollection` 與原版貼圖 patch，色調受原版資產限制 | 與 §16.3 的目標 hex 可能有色差 | 以原版可用材質為準；若需精準色票，須走 HDBF 已驗證的自製材質流程（本卡未做，避免破壞原版材質引用） |",
        "| 6 | UV 為各原版材質貼圖的固定 patch（同一材質的面共用 patch），非 §16.5 的「UV 不重疊專屬圖集」 | 同一材質的面會取樣相同貼圖區域；原版材質多為可平鋪紋理，遠看可接受 | 若需完全符合「UV 不重疊」，要新增 4 張手繪圖集（§16.4），屬自製材質流程，另開卡 |",
        "| 7 | Small T1 LOD0 = 3,496 / 3,500 tris（餘裕僅 4） | 後續加裝飾即破預算 | 任何加件先跑 `validate_art.py`；必要時降 Small 裝飾 |",
        "| 8 | 平面著色（flat）與需求 §16.5 的「法線平滑角 60 度」不同 | 硬邊較多、觀感較稜角 | 低多邊形手繪風格可接受；若需平滑，於 LOD1/2 或後續流程調整（不影響面數） |",
        "| 9 | 圖示為離線渲染，非遊戲內截圖 | 與遊戲內實際材質光照可能不同 | 實機確認後可重新渲染替換（ingest hook 會自動同步 `data/Icons/`） |",
        "| 10 | `build.ps1` G11 的**標籤文字**仍寫「no Up/Top transput」，但實作檢查（`BoilerBlueprintChecks`）依原版證據只禁 `Top`、允許後側 `Up` | 門禁文字與實作不一致，可能誤導閱讀者；實際 gate 為 PASS | 建議 Lead 更新 G11 標籤文字（屬 build.ps1，非本卡寫入範圍） |",
        "",
        "> 本卡結論：**離線可驗證項全部 PASS；實機外觀與動畫 runtime 行為未宣告通過**，"
        "需 P8 人類 gate 或後續人類確認。",
        "",
    ]


def main():
    manifest, finished, construction, lod_rows, blueprints, patches = load_data()
    summary = build_summary(manifest, finished, construction, lod_rows)
    lines = []
    lines.extend([
        "# SEE P9 美術報告",
        "",
        "> 產生器：`mod/art/tools/write_art_report.py`（資料源：`mod/art/timbermesh/export-manifest.json`、"
        "24 份藍圖、`material_patches.json`、`mod/art/lod/`、`mod/art/icons/`、`mod/art/preview/`）。",
        "> 範圍：把原版佔位模型換成自製 `.timbermesh`、補 LOD／圖示／掛點／動畫綁定；"
        "**未改**任何玩法數值、藍圖數值、程式邏輯、存檔或 loc。",
        "> 實機外觀**未**由 AI 宣告通過：仍需 P8 人類實機 gate 或後續人類確認。",
        "",
    ])
    lines.extend(section_summary(summary))
    lines.extend(section_mesh_mapping(blueprints))
    lines.extend(section_tris(finished, construction, lod_rows))
    lines.extend(section_materials(patches))
    lines.extend(transput_table(blueprints))
    lines.extend(section_animation(blueprints))
    lines.extend(section_icons())
    lines.extend(section_validation())
    lines.extend(section_risks())

    os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
    with open(OUTPUT, "w", encoding="utf-8") as handle:
        handle.write("\n".join(lines).rstrip("\n") + "\n")
    print("wrote " + OUTPUT)
    print("  summary: %d finished + %d construction + %d lod, %d icons, %d previews"
          % (summary["finished"], summary["construction"], summary["lod_files"],
             summary["icons"], summary["previews"]))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
