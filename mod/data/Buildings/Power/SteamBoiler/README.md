# Generated steam boiler blueprints

GENERATED, DO NOT EDIT - produced by mod/tools/generate-blueprints from mod/data/engines.json

The 24 `SteamBoiler.<Size>.<Tier>.<Faction>.blueprint.json` files in this folder are
generated from `mod/data/engines.json` (the single number source). Do not edit them by
hand: change the data file and re-run the generator.

    dotnet run --project mod/tools/generate-blueprints -c Release -m:1
    dotnet run --project mod/tools/generate-blueprints -c Release -m:1 -- --check

`--check` rebuilds every artifact in memory and fails when one byte differs from the
checked-in files. The golden hash of the 24 blueprints lives in
`tests/SEE.Tests/Golden/blueprints.sha256`; the derived reconciliation table lives in
`data/engines.derived.json`.
