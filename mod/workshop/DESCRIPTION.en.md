[h1]Steam Engine Expansion[/h1]
[b]12 worker-free steam engines · both factions · turn logs and water into reliable power[/b]

In Timberborn, power and drinking water both compete for the same beavers: every extra water pump and every extra power wheel is another worker you don't have. This mod turns power into infrastructure. Feed it logs and water, and get steady horsepower for your mechanical pumps, sawmills, and production lines — without hiring a single beaver.

[h1]What you get[/h1]
[list]
[*][b]12 steam engines[/b]: Small 2×2, Medium 3×3, Large 4×4, four tiers each. Buildable by both Folktails and Iron Teeth, with small faction modifiers to output and fuel use.
[*][b]No workers[/b]: haulers or conveyor belts refill the fuel inventory; haulers fill a dedicated water tank.
[*][b]Load-scaled fuel use[/b]: burn less fuel at partial load. At night, when grid demand drops to zero, engines keep running at a 10% standby load (configurable; set it to 0 to turn this off), so they don't shut down overnight.
[*][b]A visible long-term cost[/b]: condition 0–100, 6–12 day maintenance cycles, and materials pulled from your district inventory. Missing materials or a blocked path stops maintenance but never corrupts a save.
[*][b]A real panel[/b]: condition, fuel hours left, water hours left (plus an “≈N beavers’ daily drinking water” conversion), grid efficiency, next maintenance, materials, and water mode — with Pause, Maintain Now, and Auto Maintenance controls.
[*][b]Three UI languages[/b]: English, Simplified Chinese, and Traditional Chinese, switching automatically with the game language.
[/list]

[h1]The 12 tiers (rated hp / science)[/h1]
[list]
[*][b]Mk.I[/b]: Small 130 / 90 · Medium 230 / 150 · Large 360 / 225
[*][b]Mk.II[/b]: Small 260 / 240 · Medium 460 / 400 · Large 720 / 600
[*][b]Mk.III[/b]: Small 380 / 420 · Medium 680 / 700 · Large 1060 / 1050
[*][b]Mk.IV[/b]: Small 520 / 900 · Medium 920 / 1500 · Large 1500 / 2250
[/list]
Mk.III and Mk.IV can also burn planks (1 plank = 2 logs of energy). All values are generated from a single data table, and the family is built around a three-way trade-off: science buys efficiency, materials buy size, and space buys power density. No free lunch.

[h1]Maintenance[/h1]
[list]
[*]Condition only wears down while the engine is running under load. Lower condition reduces output (×0.93, ×0.85, ×0.40 at the lower bands).
[*]Maintenance intervals are 6 / 8 / 10 / 12 game days. Auto Maintenance kicks in at condition 50, needs the required materials and road access, and takes 2 game hours.
[*]If materials are missing or the building has no access, the panel says why and no materials are consumed. Fix the problem and the engine recovers.
[*]No random breakdowns — every cost is something you can plan for.
[/list]

[h1]Compatibility (all optional)[/h1]
[list]
[*][b]Timber Pipes[/b]: the water tank is a normal input inventory, so haulers can fill it. Pipe tank/valve supply modes are not supported in v0.1.0 yet.
[*][b]Power Lines[/b]: when detected, the mod can inject cable connection points. If injection fails, it falls back to vanilla power shafts and saves are unaffected.
[*][b]Conveyor Belts v2[/b]: belts can feed logs and planks straight into the engine.
[*]With none of the three installed, the core experience is fully playable.
[/list]

[h1]Install[/h1]
[list]
[*]Subscribe on the Steam Workshop.
[*]Launch Timberborn → [b]Mods[/b] → enable [b]Steam Engine Expansion[/b] → restart the game.
[*]Find the engines in the [b]Power[/b] build category, after the vanilla Steam Engine.
[*]Fill them with logs and water via haulers, then connect shafts or cables. That's it.
[/list]

[h1]Configuration (optional)[/h1]
The mod folder includes [b]SEE_Config.json[/b]. Useful keys:
[list]
[*][b]balance.standbyLoadRatio[/b]: nighttime standby load (default 0.10; set 0 to shut engines fully down at night).
[*][b]balance.fuelScaling[/b]: proportional (load-scaled fuel use, default) or vanilla (full burn whenever there is demand).
[*][b]maintenance.enabled / maintenance.intervalDays.T1–T4[/b]: maintenance toggle and intervals.
[*][b]compat.waterSupplyMode[/b]: auto / hauler (tank and valve are future work).
[*][b]perf.uiRefreshHz[/b]: panel refresh rate (default 4 Hz).
[/list]

[h1]Known limitations (v0.1.0)[/h1]
[list]
[*]Timber Pipes tank/valve supply modes are not supported yet. Water is supplied by haulers; auto mode always resolves to haulers.
[*]Custom models and animations are still being polished. If the visual footprint looks off, trust the build grid.
[*]Some in-game acceptance checks (T2–T6) are still in progress. If you run into a bug, please attach Player.log and a screenshot.
[/list]

[h1]Feedback and source[/h1]
[list]
[*]GitHub (source, issues, releases): [url=https://github.com/stanley-1028/SteamEngineExpansion]github.com/stanley-1028/SteamEngineExpansion[/url]
[*]Release v0.1.0: [url=https://github.com/stanley-1028/SteamEngineExpansion/releases/tag/v0.1.0]downloads and changelog[/url]
[*]MIT License.
[/list]

[h1]Credits[/h1]
Made in collaboration with [b]DeepSeek V4.1 Flash 1017[/b]: programming, data and balance, custom model/animation pipeline, offline verification, documentation, and cover art were produced together; requirements, direction, and in-game acceptance are the project owner's responsibility. Cover art generated with Seedream and laid out by hand; watermark removed before upload. Thanks to the Timberborn community and the open-source tools that made this possible.

[h1]At a glance[/h1]
[b]Built for Timberborn 1.1.2.8 (Update 1.1) · 12 engines · 2 factions · 3 languages · MIT License[/b]
