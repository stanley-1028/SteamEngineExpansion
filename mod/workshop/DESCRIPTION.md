[h1]Steam Engine Expansion — 蒸汽引擎扩充[/h1]
[b]12 座免人力蒸汽引擎 · 两大阵营 · 用燃料与水换稳定动力[/b]

在 Timberborn 里，动力和饮水都绑在人力上：多盖一台水泵、多盖一座踩轮，其实是在抢同一批海狸。这个模组把「动力」变成不需要工人、也不需要你盯着的基建——烧木头、喝水，换来稳定马力，再把马力送进你的机械水泵、锯木厂和生产线。

[h1]更新（2026-11-08，热修-5）[/h1]
[list]
[*][b]不再显示「需要搬运工才能运转」[/b]：完工的引擎不会再出现原版那条红色状态；没有搬运站、附近没有搬运工也不会误报（燃料可由搬运工或输送带补给，水由搬运工送入水舱）。
[*][b]耗水减半[/b]：12 档引擎的耗水全部减半，面板水量与「≈N 只海狸饮水」换算同步更新（大型 Mk.II 由 0.50/h 降到 0.25/h，约 6 水/日 ≈ 2.8 只海狸）。
[*][b]水舱同步重算[/b]：水舱容量按新耗水率缩小，但满舱续航仍是 200 游戏小时，每次补水只需原来一半的水。
[/list]

[h1]你会得到什么[/h1]
[list]
[*][b]12 种蒸汽引擎[/b]：小型 2×2、中型 3×3、大型 4×4，各四档，共 12 种；Folktails 与 Iron Teeth 都能建造（输出、燃料系数按阵营微调）。
[*][b]免工人[/b]：不需要雇员；燃料由搬运工或输送带补给，水由搬运工送进独立水舱。
[*][b]比例耗燃料[/b]：低负载自动省料；夜间电网需求归零时以 10% 待机运转（可在配置里关掉），不会整夜熄火。
[*][b]可见的长期代价[/b]：机况 0–100、6–12 日维护周期、维护材料从地区库存扣除；缺料或修不了路会停机，但不会崩档。
[*][b]状态与面板[/b]：机况、燃料剩余时数、水剩余时数（含「≈N 只海狸饮水」换算）、电网效率、下次维护与材料、供水模式；可暂停、立即维护、开关自动维护。
[*][b]三语界面[/b]：简体中文 / 繁體中文 / English（游戏语言自动切换）。
[/list]

[h1]12 档规格（额定马力 / 研究点）[/h1]
[list]
[*][b]Mk.I[/b]（基础级）：小型 130／90 · 中型 230／150 · 大型 360／225
[*][b]Mk.II[/b]（中级）：小型 260／240 · 中型 460／400 · 大型 720／600
[*][b]Mk.III[/b]（高级）：小型 380／420 · 中型 680／700 · 大型 1060／1050
[*][b]Mk.IV[/b]（旗舰）：小型 520／900 · 中型 920／1500 · 大型 1500／2250
[/list]
Mk.III／Mk.IV 可以用木板当燃料（1 木板＝2 原木能量）。所有数值由数据表生成，档位之间是「研究点买等级、材料买尺寸、空间买密度」的三维取舍，没有免费加成。

[h1]维护系统[/h1]
[list]
[*]机况 0–100，只有带载运转时才磨损；机况越低输出乘数越低（×0.93 / ×0.85 / ×0.40）。
[*]维护间隔 6／8／10／12 游戏日；自动维护在机况 50 触发，需要维护材料与道路，停机 2 游戏小时。
[*]材料不足或道路不通时显示明确原因、不扣材料；修好后自动恢复。
[*]没有随机故障——所有代价都可以提前算出来。
[/list]

[h1]兼容性（全部是可选软依赖）[/h1]
[list]
[*][b]Timber Pipes[/b]：水舱是独立的输入库存，普通搬运工可补水；管网的 tank／valve 模式暂未开放（后续版本）。
[*][b]Power Lines[/b]：检测到时可选择注入电缆接点；注入失败自动退回原版传动轴，不影响存档。
[*][b]Conveyor Belts v2[/b]：输送带可以直接把燃料送进引擎。
[*]三个模组都没有时，核心玩法完整可玩。
[/list]

[h1]安装[/h1]
[list]
[*]在 Steam 创意工坊订阅本模组。
[*]启动 Timberborn → [b]Mods[/b] → 启用 [b]Steam Engine Expansion[/b] → 重启游戏。
[*]在建造菜单的 [b]Power[/b] 分类里找到引擎（排在原版蒸汽机之后）。
[*]建好后用搬运工补原木与水，接上传动轴或电缆即可发电。
[/list]

[h1]配置（可选）[/h1]
模组资料夹内有 [b]SEE_Config.json[/b]，常用项：
[list]
[*][b]balance.standbyLoadRatio[/b]：夜间待机负载（默认 0.10，设 0 则夜间完全待机不耗料）。
[*][b]balance.fuelScaling[/b]：proportional（按负载耗料，默认）或 vanilla（有需求就全速耗）。
[*][b]maintenance.enabled / maintenance.intervalDays.T1–T4[/b]：维护开关与周期。
[*][b]compat.waterSupplyMode[/b]：auto／hauler（tank、valve 待后续版本）。
[*][b]perf.uiRefreshHz[/b]：面板刷新率（默认 4 Hz）。
[/list]

[h1]已知限制（v0.1.0）[/h1]
[list]
[*]Timber Pipes 的 tank／valve 供水模式暂未支持，水请用搬运工补（auto 会自动选搬运工）。
[*]摆放预览时原版界面仍会显示「需要搬运工。」说明列；完工建筑不会显示，也不影响运作（原版预览 UI）。
[*]自订模型／动画仍在调整；若外观与占地有出入，请以格线为准。
[*]部分游戏内验收项（T2–T6）仍在进行；遇到问题时请附上 Player.log 与截图。
[/list]

[h1]反馈与源代码[/h1]
[list]
[*]GitHub（源码 / 问题回报 / 版本发布）：[url=https://github.com/stanley-1028/SteamEngineExpansion]github.com/stanley-1028/SteamEngineExpansion[/url]
[*]Release v0.1.0：[url=https://github.com/stanley-1028/SteamEngineExpansion/releases/tag/v0.1.0]下载与变更[/url]
[*]采用 MIT License。
[/list]

[h1]Credits[/h1]
本模组与 [b]DeepSeek V4.1 Flash 1017[/b] 共同完成：程序、数据与平衡、自订模型／动画管线、离线验证、文件与封面皆为人机协作产出；需求、方向与游戏内验收由专案拥有者负责。封面由 Seedream 生成、人工排版去浮水印。感谢 Timberborn 社群与所有开源工具。

[h1]English[/h1]
Twelve worker-free steam engines for Timberborn (3 sizes × 4 tiers, both factions). Burn logs or planks and water to produce 130–1500 hp; fuel use scales with load, engines idle at 10% at night, and every tier needs periodic maintenance. Soft-compatible with Timber Pipes, Power Lines and Conveyor Belts v2 — fully playable without them. Made in collaboration with DeepSeek V4.1 Flash 1017. Source and issues: https://github.com/stanley-1028/SteamEngineExpansion
