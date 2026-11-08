# SEE blueprint generator

Build-time tool (P1) that turns `mod/data/engines.json` into every data artifact the mod ships:

- 24 blueprints `data/Buildings/Power/SteamBoiler/SteamBoiler.<Size>.<Tier>.<Faction>.blueprint.json`
- 2 template collections `data/TemplateCollections/TemplateCollection.Buildings.{Folktails,IronTeeth}.blueprint.json`
- 2 localizations `data/Localizations/{enUS,zhTW}.csv` (63 keys: 54-key requirements baseline + 9 P4B keys)
- 24 icons `data/Icons/<Name>.png` + 24 `<Name>.png.meta.json` (128x128 placeholder art)
- `data/engines.derived.json` (derived reconciliation table + source hash + gate results)
- `tests/SEE.Tests/Golden/blueprints.sha256` (24 hashes)
- `data/Buildings/Power/SteamBoiler/README.md` (GENERATED marker; JSON has no comments)

The tool runs on net9.0 with **zero NuGet packages** and never touches the game.

## Usage

```powershell
# write mode: regenerate every artifact from data/engines.json
dotnet run --project mod/tools/generate-blueprints -c Release -m:1

# check mode (CI gate): rebuild in memory, write nothing, fail on the first byte difference
dotnet run --project mod/tools/generate-blueprints -c Release -m:1 -- --check
```

`build.ps1` runs check mode before the validator. Exit codes: `0` pass, `1` any structure /
derivation / gate / self-verification / diff failure.

## The seven steps (requirements 11.3, CONTRACT_SEE 4.10)

1. Load and structurally validate `data/engines.json` (fields, types, enumerations, 12 unique
   (size,tier) rows, footprint/height, maintenance interval and materials).
2. Re-derive every rate/capacity instead of trusting the file: fuel/water rates from
   `ratedHorsepower / 100 x tier rate`, water tank from `round(water x 200)`, fuel stock from
   `ceil(fuel x 25)`; a mismatch aborts before any file is produced.
3. Expand Folktails and IronTeeth: `hp = ceil(base x faction output)`, `fuel = round(base fuel x
   faction fuel, 2)`; capacity/cost/science/maintenance are faction-independent.
4. Build the 24 blueprint JSON trees from the vanilla SteamEngine template (required specs only;
   banned specs absent).
5. Run the five gates; any failure aborts the run.
6. Emit `data/engines.derived.json` and the 24-entry golden hash.
7. `--check` verifies every byte on disk plus the absence of stale blueprint files.

## The five gates (all must pass)

1. Water tank / fuel stock >= 8x (minimum 8.5 at Medium.T4).
2. Mixing volume error upper bound `fuelStock / (fuelStock + tank)` <= 12% (maximum 10.5%).
3. Efficiency: T2-T4 fuel <= 0.025 and water <= 0.075 per 100 hp; T1 fuel <= 1.3 x T2.
4. Capacity equals the table cell by cell, including the vanilla float32
   `CeilToInt(GoodPerHour x FullInventoryWorkHours)` derivation.
5. No `BuildingPipeSpec`, `PowerLineSpec` or `WorkplaceSpec` (plus the other banned specs).

## Notes

- `GoodConsumingBuildingSpec.ConsumedGoods.GoodPerHour` only declares the **capacity derivation**
  rate (AD-3 final formula); the SEE controller owns actual consumption.
- Faction fuel rates that collapse to the same 2-decimal value are listed in
  `engines.derived.json` (`factionFuelCollapsed`), for example Small.T1.
- Placeholder models/icons and the pending back-face transput ruling are documented in
  `data/ART_PLACEHOLDER.md`.
