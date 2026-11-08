# SEE P0 — `mod/src` source skeleton (status, TODO map, public API)

> Owner: skeleton-src｜Date: 2026-11-06｜Scope: `mod/src/**` only (4 shipping assemblies; `SEE.Tests` owned by pipeline-gates)
> Build root: `mod/`; every build uses `-m:1`; warnings are errors (`TreatWarningsAsErrors=true`).

## 1. Status (P0 done)

The previous tool-domain code was removed; the four assemblies now carry the steam-boiler SEE skeleton and build with
**0 warnings / 0 errors** each:

```
dotnet build src/SEE.Core/SEE.Core.csproj        -c Release -m:1  -> 0 警告 0 錯誤
dotnet build src/SEE.Gameplay/SEE.Gameplay.csproj -c Release -m:1  -> 0 警告 0 錯誤
dotnet build src/SEE.Integration/SEE.Integration.csproj -c Release -m:1 -> 0 警告 0 錯誤
dotnet build src/SEE.UI/SEE.UI.csproj            -c Release -m:1  -> 0 警告 0 錯誤
```

Layering facts (validator-checkable):

- `SEE.Core` is `netstandard2.1`, `PackageReference`-free and game-free: its built DLL contains **none**
  of `Timberborn`, `UnityEngine`, `Bindito`, `TimberPipes`, `PowerLines`, `ConveyorBelt`, and has **no**
  `SEE.*` assembly reference. No `<Reference>`/`ProjectReference` in its csproj.
- `SEE.Gameplay`, `SEE.Integration`, `SEE.UI` each have exactly **one** `ProjectReference` (`SEE.Core`).
  Game DLLs come from `Directory.Build.targets` with `Private=false`.
- No external-mod DLL reference and no compile-time patching-library reference anywhere in `src`.
  `SEE.Integration.Patching.OptionalPatchHost` is an inert, always-closed stub (no third-party types).
- Legacy-identifier scan over `mod/src` (old project abbreviation, old assembly id, old water-tool business words) = **0 hits** in code, csproj and README.
- Max `.cs` file = 306 lines (limit 400; spec files limit 600). No `NotImplementedException` stubs.
- The two custom specs use `[SpecAlias("SEE.SEEBoilerSpec")]` / `[SpecAlias("SEE.SEEMaintenanceSpec")]`
  and live in `namespace SEE.Gameplay` (`src/SEE.Gameplay/Specs/`). Blueprint JSON keys are therefore
  `"SEE.SEEBoilerSpec"` / `"SEE.SEEMaintenanceSpec"`.
- `SEEMaintenanceSpec.Materials` is `ImmutableArray<Timberborn.Goods.GoodAmountSpec>` (IL-verified
  `Id`/`Amount`, JSON `[{"Id":"Plank","Amount":12}]`); Core keeps `MaintenanceMaterial(string GoodId,int Amount)`
  for pure logic. This follows Lead D-11/B5 ruling, not the earlier prompt wording.

## 2. File map by card

| Area | Card | Files |
|---|---|---|
| Constants, enums, ports, formulas, state machine, ledger, save codec, config, JSON, logging | P2 (Core, pure/T0-testable) | `SEE.Core/{Constants,Abstractions,Formulas,State,Maintenance,Save,Config,Json,Logging,Compat}` |
| Blueprint specs, controller, tick system, registry, runtime/starter/DI, per-entity save | P3 | `SEE.Gameplay/Specs`, `SEE.Gameplay/Components`, `SEE.Gameplay/Save`, `SeeRuntime.cs`, `SeeModStarter.cs`, `SeeGameConfigurator.cs`, `SeeSaveKeys.cs` |
| Compat detection, 3 adapters, save serializer/checksum/migrator, optional patch host | P5 | `SEE.Integration/{Detection,Adapters,Save,Patching}`, `SeeIntegrationConfigurator.cs` |
| Entity panel fragment, status/loc mapping, UI configurator | P4 | `SEE.UI/Panels`, `SEE.UI/Status`, `SEE.UI/Localization`, `SeeUiConfigurator.cs` |
| Animation/art parameter groups | P9 | `SEE.Core/Config/AnimConfig.cs`, `ArtConfig.cs` |

## 3. TODO map (what P0 deliberately left as skeleton)

- **P3 — `SEEBoilerController.Tick.cs`** (`TickBoilers` is a guarded no-op):
  network demand λ (anti-feedback formula in comments), `MechanicalNode.SetOutputMultiplier`,
  cached `GoodConsumingBuilding` toggle Pause/Resume (no `GoodPoweredGeneratorSpec` in blueprints),
  proportional fuel/water accounting, `Inventory.TakeConsumed`, wear via ledger, maintenance
  material/road flow, `SingleGoodAllower.Allow("Water")` for the liquid stockpile, deferred
  post-load node write-back. **P3 — `SEE.Gameplay/Save/SeeSaveBridge.cs`**: world-node index
  (`checksum` + `buildings[]` entityId/tier/size/faction) and compat block; per-entity 7-field
  save is already wired (`SEEBoilerController.Save.cs`).
- **P4 — `SEE.UI/Panels/BoilerPanelFragment.cs`**: the six information blocks are grouped but the
  full §15.2 layout, icons/badges and dirty-fingerprint polish. Values only come from
  `IBoilerStatusView`.
- **P5 — `SEE.Integration`**: real adapter inject/attach behavior, `SaveMigrator` v1→vN step (chain is
  empty at v1), checksum verification wiring, `OptionalPatchHost` (stay closed unless a P0-approved
  reflective bootstrap exists), compat/water-mode re-resolution in `SEEBoilerController.SetWaterSupplyMode`.
- **P9 — art/anim**: parameters are loaded/clamped in Core; mesh/LOD/animation implementers consume them.
- **P1 (data card, not src)**: blueprints must use the field names in §4 below; materials use
  `GoodAmountSpec.Id` (not `GoodId`).

## 4. Public API contract (frozen for `SEE.Tests` / validator)

Namespaces: `SEE.Core.{Constants,Abstractions,Formulas,State,Maintenance,Save,Config,Logging,Json}`,
`SEE.Gameplay` (specs/runtime/starter/save keys), `SEE.Gameplay.Components` (controller/registry/tick),
`SEE.Integration.{Detection,Adapters,Save,Patching}`, `SEE.UI.{Panels,Status}`.

**Core**
- `SEEConst`: `ModId="SteamEngineExpansion"`, `ConfigFileName="SEE_Config.json"`, `SaveComponentKey="SEE"`,
  `TickIntervalSeconds=1.0`, `DetectBudgetSeconds=2.0`, `LogThrottleSeconds=30.0`, `UiRefreshHz=4`,
  `HoursPerDay=24.0`, `MaxCondition=100f`, `MaxMaintenanceRemainingHours=2f` (closed `[0,2]`, D-7),
  maintenance defaults, `BlueprintFolder`, `ToolGroupId="Power"`, `ToolOrderStart=110`, `LocNamespace="SEE"`.
- `SEEVersion`: `ModVersion="0.1.0"`, `SaveSchemaVersion=1`, `ConfigSchemaVersion=1`, `MinSupportedConfigSchemaVersion=1`.
- `SeeLocKeys`: 54 §15.5 keys as constants + `DisplayName(BoilerSize,BoilerTier)`, `Flavor(BoilerTier)`.
- Enums: `BoilerStatus {Paused,Maintaining,NoFuel,NoWater,NeedsMaintenance,Critical,Worn,MaintenanceDue,Idle,Running}`
  (declaration order = priority), `MaintenancePhase {None,Maintaining,MissingMaterials,NoAccess}`,
  `WaterSupplyMode {Auto,Tank,Valve,Hauler}`, `BoilerTier {T1,T2,T3,T4}`, `BoilerSize {Small,Medium,Large}`,
  `BoilerFaction {Folktails,IronTeeth}`, `FuelScalingMode {Proportional,Vanilla}`, `BeltFuelMode {Auto,Haulers}`,
  `Availability {Absent,Degraded,Available}` (Integration), `MaintenanceAction {None,Start,BlockedByMaterials,BlockedByAccess,Completed}`.
- `GoodStack(string GoodId,int Amount)` (`readonly struct`), `MaintenanceMaterial(string GoodId,int Amount)`
  (`readonly record struct`), `GoodIds {Log,Plank,Gear,MetalBlock,Water,LiquidGoodType}` (all `const string`).
- `IBoilerStatusView`: `Guid EntityId`, `BoilerStatus Status`, `MaintenancePhase MaintenancePhase`,
  `BoilerTier Tier`, `BoilerSize Size`, `BoilerFaction Faction`, `bool IsFinished/IsPaused/PowerLineAttached`,
  `int RatedHorsepower/ActualOutput/NetworkSupplyHp/NetworkDemandHp/FuelInStock/FuelCapacity/WaterInStock/WaterCapacity`,
  `float Condition/ConditionMultiplier/LoadRatio/NetworkEfficiency/FuelPerHour/WaterPerHour/MaintenanceRemainingHours/LastMaintenanceDay/MaintenanceTriggerThreshold`,
  `bool AutoMaintenance`, `WaterSupplyMode WaterSupplyMode`.
- `IBoilerLogic`: `bool SetPaused(Guid,bool)`, `bool RequestMaintenanceNow(Guid)`, `bool SetAutoMaintenance(Guid,bool)`,
  `bool SetWaterSupplyMode(Guid,WaterSupplyMode)`.
- `IBoilerQuery`: `bool TryGetView(Guid, out IBoilerStatusView? view)`.
- `ConditionCurve.Multiplier(float)->float`: NaN/<=0→0, <=20→0.40, <=40→0.85, <=60→0.93, else 1.00.
- `BoilerFormulas`: `double WearPerRunningHour(double intervalDays)`; `float LoadFactor(float loadRatio)`
  =0.4+0.6·clamp01; `int OutputHorsepower(int rated,float conditionMultiplier)`=ceil; 
  `float FuelPerHourScaled(float ratedFuelPerHour,float actualOutput,int ratedHorsepower)`;
  `int FuelStockGoods(float fuelPerHour,float fullInventoryWorkHours)`=ceil;
  `int WaterTankGoods(float waterPerHour,float waterTankHours)`=round(AwayFromZero); `float Clamp01(float)`.
- `BoilerStateInput(bool IsPaused, MaintenancePhase MaintenancePhase, float Condition, bool HasFuel, bool HasWater, float NetworkDemandRatio)`;
  `BoilerStateMachine.Evaluate(in BoilerStateInput)->BoilerStatus` (AD-12 order: Paused→Maintaining→NoFuel→NoWater→NeedsMaintenance→Idle when λ<=0, otherwise condition band; wear bands applicable only at λ>0);
  `BoilerStateMachine.ResolveConditionState(float)->BoilerStatus`; `BoilerStateMachine.Produces(BoilerStatus)->bool`.
- `MaintenanceRules(bool Enabled,bool Auto,float AutoThreshold,float DurationHours,float IntervalDays,IReadOnlyList<MaintenanceMaterial> Materials)`
  + `Default`; `MaintenanceDecision {Action, Phase}`.
- `MaintenanceLedger`: `Reset(float condition,bool auto,MaintenancePhase phase,float remainingHours,float lastDay)`;
  props `Condition/AutoMaintenance{get;set;}/Phase/RemainingHours/LastMaintenanceDay/Rules{get;set;}`;
  `AccumulateWear(double hours,double intervalDays,float loadRatio)` (no-op when unloaded/maintaining);
  `Decide(bool hasMaterials,bool hasAccess,bool paused)->MaintenanceDecision`;
  `BeginMaintenance(float durationHours)`, `AdvanceMaintenance(double hours)`,
  `CompleteMaintenance()`, `CompleteMaintenance(float currentDay)`. `RemainingHours` always `[0,2]`, 0 unless Maintaining.
- `BoilerSaveState`: `EntityId string` (Guid "D"), `Tier`, `Size`, `Faction`, `Condition`, `AutoMaintenance`,
  `MaintenancePhase`, `MaintenanceRemainingHours`, `LastMaintenanceDay`, `WaterSupplyMode`, `PowerLineAttached`, `NewGame()`.
- `SeeSaveCodec`: `CurrentSchemaVersion`, `Migrations` (empty v1), `Parse(string,ISeeLog?)->BoilerSaveState?`,
  `Sanitize(BoilerSaveState?,float currentDay,ISeeLog?)->BoilerSaveState`, `Serialize(BoilerSaveState)->string`,
  `Migrate(int fromVersion,string)->string?`. `ISaveMigration {FromVersion,ToVersion,Migrate(string)->string?}`.
- `SeeConfig` + groups `BalanceConfig/MaintenanceConfig/CompatConfig/PerfConfig/AnimConfig/ArtConfig/UiConfig`:
  `ConfigSchemaVersion`, `Default`, `RulesFor(BoilerTier,bool,IReadOnlyList<MaintenanceMaterial>?)`, `IntervalDaysFor(BoilerTier)`.
  `SeeConfigCodec.Parse(string,ISeeLog?)->SeeConfig`, `Serialize(SeeConfig)->string`; missing field=default,
  unknown ignored, out-of-range clamp+warn (`SEE/Warn/Config`). The parser accepts the shipped nested-object tables
  (`"fuelRatePer100hp": {"T1": …}`, `"faction": {"outputMultiplier": …}`, `"intervalDays": {"T1": …}`,
  `"trisLod0": {"Small": …}`, `"textureSize": {"Small": …}`) and compact arrays (test form); `Serialize` emits the
  nested §7.9 shape (fuel/water rates, intervalDays, faction, trisLod0, textureSize).
- Logging: `ISeeLog {Info,Warn,Error,Compat}`, `SeeLogKeys` (`SEE/Info/*`, `SEE/Warn/*`, `SEE/Error/*`, `SEE/Compat/*`),
  `ThrottledLog(ISeeLog,double throttleSeconds[,Func<double> clock])` + `ShouldLog(key,nowSeconds)`, `RecordingLog`.
- JSON: `SEE.Core.Json.MiniJson` (`Parse`, `Write`, `GetNumber/GetInt/GetBool/GetString`).

**Gameplay (compile-time only; tests must not load it in net9)** — `namespace SEE.Gameplay`:
- `[SpecAlias("SEE.SEEBoilerSpec")] SEEBoilerSpec : ComponentSpec` — `[Serialize]` members exactly:
  `string Tier="T2"`, `string Size="Medium"`, `string Faction="Folktails"`, `int Horsepower=460`,
  `float FuelPerHour=0.11f`, `float WaterPerHour=0.32f`, `int FuelCapacityGoods=3`, `int WaterCapacityGoods=64`,
  `ImmutableArray<string> FuelGoodIds`, `ImmutableArray<float> FuelValues`, `string FuelScaling="proportional"`,
  `bool AutoMaintenanceDefault=true` (+ helpers `ResolveTier/ResolveSize/ResolveFaction/FuelValueFor/IsFuelAllowed`).
- `[SpecAlias("SEE.SEEMaintenanceSpec")] SEEMaintenanceSpec : ComponentSpec` — `[Serialize]` members exactly:
  `bool Enabled=true`, `float IntervalDays=8f`, `float LoadFloor=0.4f`, `float LoadFactorRange=0.6f`,
  `float TriggerThreshold=50f`, `float DurationHours=2f`, `ImmutableArray<GoodAmountSpec> Materials`
  (+ helpers `ResolveMaterials()->IReadOnlyList<MaintenanceMaterial>`, `ResolveIntervalDays`, `ResolveDurationHours`,
  `ResolveTriggerThreshold`).
- `SEEBoilerController : BaseComponent, IAwakableComponent, IFinishedStateListener, IDeletableEntity,
  IPostLoadableEntity, IPersistentEntity, IBoilerLogic, IBoilerStatusView`; `SEEBoilerRegistry : IBoilerQuery,
  IBoilerLogic, ISEEBoilerRegistry`; `BoilerTickSystem : ITickableSingleton`; `SEERuntime : ILoadableSingleton`;
  `SeeModStarter : IModStarter`; `SeeSaveBridge : ILoadableSingleton, ISaveableSingleton`;
  `SeeGameConfigurator : Configurator`; `SEEMaintenanceController` (plain class over `MaintenanceLedger`).

**Integration** — `Detection.{CompatDetector : ICompatDetector, ILoadableSingleton; ExternalModStatus;
CompatAnchors; Availability}`, `Adapters.{IExternalAdapter, ExternalAdapterBase, TimberPipesAdapter,
PowerLinesAdapter, ConveyorBeltAdapter}`, `Save.{SaveSerializer, SaveChecksum, SaveMigrator, SaveMigrationResult}`,
`Patching.OptionalPatchHost`, `SeeIntegrationConfigurator : Configurator`; capability interfaces
`Adapters.IWaterSupplyAdapter/IPowerLineAdapter/IFuelSupplyAdapter` (all extend `IExternalAdapter`).

**UI** — `Panels.{BoilerPanelFragment : IEntityPanelFragment, BoilerPanelModuleProvider : IProvider<EntityPanelModule>}`,
`Status.StatusDescriptors` (`StatusLocKey/MaintenanceLocKey/WaterSupplyModeLocKey/DisplayNameLocKey/HasBadge`),
`SeeUiConfigurator : Configurator`, `Localization.SeeUiVanillaKeys`.

## 5. Decisions / deviations recorded for `CONTRACT_SEE`

1. **D-11**: specs in `SEE.Gameplay`, Core has zero game dependency (tests stay net9-clean).
2. **Lead ruling**: spec namespace is `SEE.Gameplay`; `[SpecAlias]` keys unchanged (`SEE.SEEBoilerSpec` etc.).
3. **Lead ruling (B5)**: maintenance materials use vanilla `ImmutableArray<GoodAmountSpec>` (`Id`/`Amount`);
   Core `MaintenanceMaterial` is only the pure-logic form. P3 must verify in-game that the deserializer fills it.
4. **Lead ruling D-6 (hybrid)**: world-node `SEE` index singleton (`ILoadableSingleton + ISaveableSingleton`)
   + per-entity `IPersistentEntity` component `ComponentKey("SEE")` with 7 fields. `tier/size/faction` are not
   persisted per entity. Node absence/damage only warns.
5. **D-7**: `MaintenanceLedger.RemainingHours` uses the closed interval `[0,2]`; non-Maintaining is always 0.
6. **Controller base**: `BaseComponent` (not `TickableComponent`); the single tick entry is `BoilerTickSystem`
   (1 s, registry iteration in stable registration order). If P3 prefers per-entity ticking, keep the single
   entry point contract and only change who calls `TickBoilers`.
7. **No `GoodPoweredGeneratorSpec`** in SEE blueprints; power modulation is `MechanicalNode.SetOutputMultiplier`
   (λ×condition) plus the cached `GoodConsumingBuilding` pause toggle. Details in
   `SEEBoilerController.Tick.cs` TODOs.

## 5.1 Alignment with `mod/docs/CONTRACT_SEE.md` (written by docs-contract after this skeleton)

| Contract point | Skeleton status |
|---|---|
| §2.4 frozen names | Present except `TierCatalog`/`TierDefinition`, which are P2 deliverables (not in the P0 card list). |
| §2.4 adapter capability interfaces | Added: `IWaterSupplyAdapter`, `IPowerLineAdapter`, `IFuelSupplyAdapter`. |
| §2.5 Tick | Controller is `BaseComponent` (matches §2.4 row) and the single settlement entry is `BoilerTickSystem : ITickableSingleton`. If P3 wants `TickableComponent`+`Ticker`, keep one entry point and only move who calls `TickBoilers`. |
| §3.2/§3.3 spec fields | Exact match, including the §3.2 default values (T2/Medium example row). |
| §3.4 materials | Exact match (`ImmutableArray<GoodAmountSpec>` / `Id`/`Amount`). |
| §6 hybrid save | Exact match: world node `SingletonKey("SEE")` + per-entity `ComponentKey("SEE")` 7 fields (`waterSupplyMode` written lowercase, parsed case-insensitively). |
| §7 config | Parser accepts both shipped nested-object and compact-array forms for per-tier/per-size tables; `Serialize` now emits the §7.9 nested shape. `balance.scienceCost` is intentionally not modelled in Core (P1 generator input / P7); full `anim.*`/`art.*` leaf sets are P9. |
| §8.2 priority | **Resolved by Lead AD-12 (2026-11-06)**: Paused > Maintaining > NoFuel > NoWater > NeedsMaintenance > Critical > Worn > MaintenanceDue > Idle > Running, with applicability (wear bands only at λ>0; Idle only at λ<=0). `BoilerStatus` declaration order, `BoilerStateMachine.Evaluate` and its XML summary all follow this. |
| §6.2 corrupt record | `SeeSaveCodec.Sanitize` resets a non-finite condition to 100 **and** turns `autoMaintenance` off. |

## 6. Known risks / open items

- `BoilerTickSystem`/`SeeSaveBridge` rely on Bindito auto-registering `[Singleton]`-marked interfaces
  (`ITickableSingleton`, `ILoadableSingleton`, `ISaveableSingleton`). IL evidence: `SerializedWorldFactory.Create`
  consumes `IEnumerable<ISaveableSingleton>` although no vanilla Configurator binds one explicitly, so the
  auto-registration mechanism exists. Still verify `BoilerTickSystem.Tick` runs in the T1 load smoke test.
- `ISingletonLoader` injection is safe: `WorldPersistenceConfigurator.Configure` IL binds
  `ISingletonLoader` to `WorldSingletonLoader` as a singleton.
- `SeeSaveKeys` uses `SingletonKey` for the world node and `ComponentKey` for the per-entity state; both are
  named `"SEE"`. Confirm the round-trip in the T5 save test.
- `Timberborn.WorldPersistence.SingletonKey` is a **different type** from `ComponentKey` — the world node key
  must not be passed where a component key is expected (compile-time enforced).
- UI is a functional skeleton: full §15.2 layout/icons and status badges are P4.
- `SEEMaintenanceController` is not DI-bound (it needs a per-boiler ledger); P3 may turn it into a decorator.
- Real-game behavior (build/place/power/save) remains unverified — P0 gate is offline only (PLAN R-K).
