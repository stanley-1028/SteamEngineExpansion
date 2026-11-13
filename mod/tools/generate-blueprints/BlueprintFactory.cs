namespace SEE.Tools.GenerateBlueprints;

/// <summary>
/// Step 4 of the generator: turns one faction expansion into the shipped blueprint JSON.
///
/// The structure follows the vanilla SteamEngine.IronTeeth template (CONTRACT_SEE 4.3, 4.8):
/// the required specs are present, the banned ones (GoodPoweredGeneratorSpec, BuildingPipeSpec,
/// PowerLineSpec, WorkplaceSpec, Top transput) are absent. From P9 on every variant references the
/// self-made neutral-art models
/// <c>Buildings/Power/SteamBoiler/SteamBoiler.&lt;Size&gt;.&lt;Tier&gt;.&lt;Faction&gt;.Model</c> (finished) and
/// <c>...ConstructionStage0.Model</c> (construction stage); those 48 <c>.timbermesh</c> files are the
/// P9 deliverable owned by the art card. The blueprint JSON never carries the <c>.timbermesh</c>
/// extension, matching the vanilla convention.
/// </summary>
internal static class BlueprintFactory
{
    private const string SelectionSound = "Engine";
    private const string LoopingSound = "PrintingPress";
    private const string SmokePrefab = "Buildings/Power/SteamEngine/SteamEngineSmoke";

    private static readonly string[] SmokeIds = { "Smoke1", "Smoke2", "Smoke3" };

    /// <summary>Builds the complete JSON tree of one blueprint.</summary>
    public static JsonObject Build(FactionVariant variant, Catalog catalog)
    {
        var engine = variant.Engine;
        var root = new JsonObject();
        root.Add("BuildingSpec", BuildingSpec(engine));
        root.Add("BuildingModelSpec", BuildingModelSpec());
        root.Add("EnterableSpec", EnterableSpec());
        root.Add("TemplateSpec", TemplateSpec(variant));
        root.Add("BlockObjectSpec", BlockObjectSpec(engine));
        root.Add("BuildingAccessibleSpec", BuildingAccessibleSpec(engine));
        root.Add("MechanicalNodeSpec", new JsonObject()
            .Add("PowerOutput", Numbers.Integer(variant.Horsepower))
            .Add("PowerInput", Numbers.Integer(0))
            .Add("IsShaft", new JsonBool(false)));
        root.Add("TransputProviderSpec", TransputProviderSpec(engine));

        var consumer = new JsonObject()
            .Add("FullInventoryWorkHours", Numbers.Integer(engine.FullInventoryWorkHours));
        var consumed = new JsonArray();
        foreach (var rate in variant.ConsumedGoods)
        {
            consumed.Add(new JsonObject()
                .Add("GoodId", new JsonString(rate.GoodId))
                .Add("GoodPerHour", Numbers.Decimal(rate.GoodPerHour)));
        }

        consumer.Add("ConsumedGoods", consumed);
        root.Add("GoodConsumingBuildingSpec", consumer);

        // No StockpileSpec: a Stockpile + GoodConsumingBuilding on the same building registers two
        // NoHaulingPostStatus initializers and crashes on finish (Player.log evidence, 2026-11-07).
        // Bug fix 2026-11-08: Water is not a GoodConsuming good either; the dedicated tank is a
        // public input Inventory created in code by SEEWaterInventoryInitializer, so this JSON only
        // declares the fuel goods and the tank stays independent.

        root.Add("SEE.SEEBoilerSpec", BoilerSpec(variant));
        root.Add("SEE.SEEMaintenanceSpec", MaintenanceSpec(engine));
        root.Add("MechanicalBuildingSpec", new JsonObject());
        root.Add("GoodPoweredGeneratorAnimatorSpec", new JsonObject());
        root.Add("PowerGeneratorParticleControllerSpec", new JsonObject()
            .Add("AttachmentIds", new JsonArray().AddStrings(SmokeIds)));

        root.Add("PlaceableBlockObjectSpec", PlaceableBlockObjectSpec(variant));
        root.Add("PowerGeneratorSoundsSpec", new JsonObject());
        root.Add("LabeledEntitySpec", LabeledEntitySpec(variant));
        root.Add("ConstructionSiteProgressVisualizerSpec", new JsonObject()
            .Add("ProgressThresholds", new JsonArray().Add(new JsonNumber("0"))));
        root.Add("MechanicalNodeIlluminatorSpec", new JsonObject());
        root.Add("TemplateAttachmentsSpec", TemplateAttachmentsSpec());

        root.Add("Children", Children(variant, catalog));
        return root;
    }

    /// <summary>Renders a built blueprint to the canonical generated text.</summary>
    public static string Serialize(JsonObject blueprint) => JsonText.Serialize(blueprint);

    private static JsonObject BuildingSpec(EngineRow engine)
    {
        var cost = new JsonArray();
        foreach (var item in engine.BuildCost)
        {
            cost.Add(new JsonObject()
                .Add("Id", new JsonString(item.Good))
                .Add("Amount", Numbers.Integer(item.Amount)));
        }

        return new JsonObject()
            .Add("SelectionSoundName", new JsonString(SelectionSound))
            .Add("LoopingSoundName", new JsonString(LoopingSound))
            .Add("BuildingCost", cost)
            .Add("ScienceCost", Numbers.Integer(engine.ScienceCost))
            .Add("PlaceFinished", new JsonBool(false))
            .Add("FinishableWithBeaversOnSite", new JsonBool(false))
            .Add("DrawRangeBoundsOnIt", new JsonBool(false));
    }

    private static JsonObject BuildingModelSpec() =>
        new JsonObject()
            .Add("FinishedModelName", new JsonString("#Finished"))
            .Add("UnfinishedModelName", new JsonString("#Unfinished"))
            .Add("FinishedUncoveredModelName", new JsonString(string.Empty))
            .Add("UndergroundModelName", new JsonString(string.Empty))
            .Add("ConstructionModeModel", new JsonString("Finished"))
            .Add("UndergroundModelDepth", Numbers.Integer(0));

    private static JsonObject EnterableSpec() =>
        new JsonObject()
            .Add("OperatingState", new JsonString("Finished"))
            .Add("LimitedCapacityFinished", new JsonBool(false))
            .Add("CapacityFinished", Numbers.Integer(0))
            .Add("LimitedCapacityUnfinished", new JsonBool(false))
            .Add("CapacityUnfinished", Numbers.Integer(0));

    private static JsonObject TemplateSpec(FactionVariant variant) =>
        new JsonObject()
            .Add("TemplateName", new JsonString(variant.TemplateName))
            .Add("BackwardCompatibleTemplateNames", new JsonArray())
            .Add("RequiredFeatureToggle", new JsonString(string.Empty))
            .Add("DisablingFeatureToggle", new JsonString(string.Empty));

    private static JsonObject BlockObjectSpec(EngineRow engine)
    {
        var blocks = new JsonArray();
        for (var z = 0; z < engine.Height; z++)
        {
            for (var y = 0; y < engine.FootprintY; y++)
            {
                for (var x = 0; x < engine.FootprintX; x++)
                {
                    blocks.Add(Block(z, engine.Height));
                }
            }
        }

        return new JsonObject()
            .Add("Size", new JsonObject()
                .Add("X", Numbers.Integer(engine.FootprintX))
                .Add("Y", Numbers.Integer(engine.FootprintY))
                .Add("Z", Numbers.Integer(engine.Height)))
            .Add("Blocks", blocks)
            .Add("Entrance", new JsonObject()
                .Add("HasEntrance", new JsonBool(true))
                .Add("Coordinates", new JsonObject()
                    .Add("X", Numbers.Integer(engine.FootprintX / 2))
                    .Add("Y", Numbers.Integer(-1))
                    .Add("Z", Numbers.Integer(0))))
            .Add("BaseZ", Numbers.Integer(0))
            .Add("Overridable", new JsonBool(false))
            .Add("Flippable", new JsonBool(true));
    }

    /// <summary>
    /// Ground floor is walkable/solid (the entrance row must be), the middle floors are open and the
    /// top floor keeps the original "Floor, Bottom, Corners, Path, Middle" pattern. The footprint is
    /// therefore not solid through its height (CONTRACT_SEE 4.5 / R-B).
    /// </summary>
    private static JsonObject Block(int z, int height)
    {
        if (z == 0)
        {
            return BlockEntry("GroundOrStackable", "Bottom, Top, Corners, Path, Middle");
        }

        return z == height - 1
            ? BlockEntry("Any", "Floor, Bottom, Corners, Path, Middle")
            : BlockEntry("Any", "All");
    }

    private static JsonObject BlockEntry(string matterBelow, string occupations) =>
        new JsonObject()
            .Add("MatterBelow", new JsonString(matterBelow))
            .Add("Occupations", new JsonString(occupations))
            .Add("Stackable", new JsonString("None"))
            .Add("OccupyAllBelow", new JsonBool(false))
            .Add("Underground", new JsonBool(false));

    private static JsonObject BuildingAccessibleSpec(EngineRow engine) =>
        new JsonObject()
            .Add("LocalAccess", new JsonObject()
                .Add("X", Numbers.Approximation(engine.FootprintX / 2.0))
                .Add("Y", Numbers.Approximation(0.05))
                .Add("Z", Numbers.Approximation(0.6)))
            .Add("ForceOneFinalAccess", new JsonBool(false));

    /// <summary>
    /// Left/right side points, the back point and the two bottom points of the vanilla engine, scaled
    /// per footprint (requirements 8.2).
    ///
    /// The vanilla SteamEngine.IronTeeth declares its back face as <c>Directions: "Up"</c> at
    /// <c>(1, 2, 0)</c>. <c>TransputSpec.Directions</c> is a <c>Timberborn.Coordinates.Directions3D</c>
    /// whose members are Down/Left/Up/Right/Bottom/Top, so <c>"Up"</c> is the horizontal +Y (back) face,
    /// not the vertical top face; the vanilla 3x3 engine proves it. The frozen validator therefore only
    /// bans <c>"Top"</c> and requires the single <c>"Up"</c> transput to sit on the back row
    /// (<c>X = floor(Size.X / 2), Y = Size.Y - 1, Z = 0</c>).
    /// </summary>
    private static JsonObject TransputProviderSpec(EngineRow engine)
    {
        var x = engine.FootprintX;
        var y = engine.FootprintY;
        var midX = x / 2;
        var midY = y / 2;
        var transputs = new JsonArray()
            .Add(Transput(0, midY, "Left"))
            .Add(Transput(x - 1, midY, "Right"))
            .Add(Transput(midX, y - 1, "Up"));

        // The 2x2 footprint cannot place the bottom points on the centre column without sharing a
        // block with a side point, so they run along the front row instead (doc-extract 3.9 table).
        if (x == 2)
        {
            transputs.Add(Transput(0, 0, "Bottom"));
            transputs.Add(Transput(1, 0, "Bottom"));
        }
        else
        {
            transputs.Add(Transput(midX, midY, "Bottom"));
            transputs.Add(Transput(midX, 0, "Bottom"));
        }

        return new JsonObject()
            .Add("Transputs", transputs)
            .Add("IgnoreRotation", new JsonBool(true));
    }

    private static JsonObject Transput(int x, int y, string direction) =>
        new JsonObject()
            .Add("Coordinates", new JsonObject()
                .Add("X", Numbers.Integer(x))
                .Add("Y", Numbers.Integer(y))
                .Add("Z", Numbers.Integer(0)))
            .Add("Directions", new JsonString(direction))
            .Add("ReverseRotation", new JsonBool(false));

    private static JsonObject BoilerSpec(FactionVariant variant) =>
        new JsonObject()
            .Add("Tier", new JsonString(variant.Engine.Tier))
            .Add("Size", new JsonString(variant.Engine.Size))
            .Add("Faction", new JsonString(variant.Faction.Name))
            .Add("Horsepower", Numbers.Integer(variant.Horsepower))
            .Add("FuelPerHour", Numbers.Decimal(variant.FuelPerHour))
            .Add("WaterPerHour", Numbers.Decimal(variant.WaterPerHour))
            .Add("FuelCapacityGoods", Numbers.Integer(variant.FuelStock))
            .Add("WaterCapacityGoods", Numbers.Integer(variant.WaterTankGoods))
            .Add("FuelGoodIds", new JsonArray().AddStrings(variant.FuelGoodIds))
            .Add("FuelValues", FuelValues(variant.FuelValues))
            .Add("FuelScaling", new JsonString("proportional"))
            .Add("AutoMaintenanceDefault", new JsonBool(true));

    private static JsonArray FuelValues(IReadOnlyList<decimal> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(Numbers.Decimal(value));
        }

        return array;
    }

    private static JsonObject MaintenanceSpec(EngineRow engine)
    {
        var materials = new JsonArray();
        foreach (var item in engine.MaintenanceMaterials)
        {
            materials.Add(new JsonObject()
                .Add("Id", new JsonString(item.Good))
                .Add("Amount", Numbers.Integer(item.Amount)));
        }

        return new JsonObject()
            .Add("Enabled", new JsonBool(true))
            .Add("IntervalDays", Numbers.Integer(engine.MaintenanceIntervalDays))
            .Add("LoadFloor", Numbers.Decimal(0.4m))
            .Add("LoadFactorRange", Numbers.Decimal(0.6m))
            .Add("TriggerThreshold", Numbers.Integer(50))
            .Add("DurationHours", Numbers.Integer(2))
            .Add("Materials", materials);
    }

    /// <summary>
    /// Build menu placement of one variant: every boiler joins the dedicated SEE "Steam Engines" group
    /// (<see cref="ToolGroup"/>, order 72, seated between vanilla Power 70 and Science 80) instead of
    /// the vanilla Power group. <c>ToolOrder</c> still orders the 24 buttons inside that group.
    /// </summary>
    private static JsonObject PlaceableBlockObjectSpec(FactionVariant variant) =>
        new JsonObject()
            .Add("ToolGroupId", new JsonString(ToolGroup.Id))
            .Add("ToolOrder", Numbers.Integer(variant.ToolOrder))
            .Add("ToolShape", new JsonString("Square"))
            .Add("Layout", new JsonString("Single"))
            .Add("CustomPivot", new JsonObject()
                .Add("HasCustomPivot", new JsonBool(false))
                .Add("Coordinates", new JsonObject()
                    .Add("X", Numbers.Approximation(0))
                    .Add("Y", Numbers.Approximation(0))
                    .Add("Z", Numbers.Approximation(0))))
            .Add("CanBeAttachedToTerrainSide", new JsonBool(false))
            .Add("DevModeTool", new JsonBool(false));

    private static JsonObject LabeledEntitySpec(FactionVariant variant) =>
        new JsonObject()
            .Add("DisplayNameLocKey", new JsonString(
                "Building.SEE.SteamBoiler." + variant.Engine.Size + "." + variant.Engine.Tier + ".DisplayName"))
            .Add("DescriptionLocKey", new JsonString("Building.SEE.SteamBoiler.Description"))
            .Add("FlavorDescriptionLocKey", new JsonString(
                "Building.SEE.SteamBoiler.Flavor." + variant.Engine.Tier))
            .Add("Icon", new JsonString("Icons/" + variant.IconName));

    private static JsonObject TemplateAttachmentsSpec()
    {
        var attachments = new JsonArray();
        foreach (var id in SmokeIds)
        {
            attachments.Add(new JsonObject()
                .Add("AttachmentId", new JsonString(id))
                .Add("Prefab", new JsonString(SmokePrefab))
                .Add("Parent", new JsonString("#Empty" + id))
                .Add("Position", Vector(0, 0, 0))
                .Add("Rotation", Vector(0, 0, 0))
                .Add("Scale", Vector(1, 1, 1))
                .Add("CreateInstantly", new JsonBool(true)));
        }

        return new JsonObject().Add("Attachments", attachments);
    }

    private static JsonObject Vector(double x, double y, double z) =>
        new JsonObject()
            .Add("X", Numbers.Approximation(x))
            .Add("Y", Numbers.Approximation(y))
            .Add("Z", Numbers.Approximation(z));

    private static JsonObject Children(FactionVariant variant, Catalog catalog)
    {
        var engine = variant.Engine;
        var constructionBase = catalog.ConstructionBases[engine.Size];
        var constructionBaseName = Path.GetFileName(constructionBase).Replace(".blueprint", string.Empty);
        var unfinishedChildren = new JsonObject()
            .Add(constructionBaseName + "#nested", new JsonObject()
                .Add("BlueprintPath", new JsonString(constructionBase)))
            .Add("ConstructionStage0", new JsonObject()
                .Add("TimbermeshSpec", new JsonObject()
                    .Add("Model", new JsonString(ArtModelPath(variant, construction: true))))
                .Add("CollidersSpec", UnfinishedColliders(engine)));

        return new JsonObject()
            .Add("#Finished", new JsonObject()
                .Add("TimbermeshSpec", new JsonObject()
                    .Add("Model", new JsonString(ArtModelPath(variant, construction: false))))
                .Add("CollidersSpec", FinishedColliders(engine)))
            .Add("#Unfinished", new JsonObject()
                .Add("Children", unfinishedChildren));
    }

    /// <summary>
    /// P9 model path of one variant (no <c>.timbermesh</c> extension, as in the vanilla blueprints):
    /// <c>Buildings/Power/SteamBoiler/SteamBoiler.&lt;Size&gt;.&lt;Tier&gt;.&lt;Faction&gt;.Model</c> for the
    /// finished building and <c>...ConstructionStage0.Model</c> for the construction stage.
    /// </summary>
    private static string ArtModelPath(FactionVariant variant, bool construction)
    {
        var stem = "Buildings/Power/SteamBoiler/" + variant.TemplateName;
        return construction ? stem + ".ConstructionStage0.Model" : stem + ".Model";
    }

    private static JsonObject FinishedColliders(EngineRow engine)
    {
        var x = engine.FootprintX;
        var y = engine.FootprintY;
        var z = engine.Height;
        var boxes = new JsonArray().Add(new JsonObject()
            .Add("Center", Vector(x / 2.0, 0.4438355 * z / 3.0, y / 2.0))
            .Add("Size", Vector(x, 0.887671 * z / 3.0, y)));
        var capsules = new JsonArray().Add(new JsonObject()
            .Add("Center", Vector(x / 2.0, 1.38 * z / 3.0, y / 2.0))
            .Add("Radius", Numbers.Approximation(x / 3.0))
            .Add("Height", Numbers.Approximation(z))
            .Add("Axis", new JsonString("Y")));
        return new JsonObject()
            .Add("BoxColliders", boxes)
            .Add("SphereColliders", new JsonArray())
            .Add("CapsuleColliders", capsules);
    }

    private static JsonObject UnfinishedColliders(EngineRow engine)
    {
        var x = engine.FootprintX;
        var y = engine.FootprintY;
        var z = engine.Height;
        var boxes = new JsonArray()
            .Add(new JsonObject()
                .Add("Center", Vector(x / 2.0, 0.05, y / 2.0))
                .Add("Size", Vector(x, 0.1, y)))
            .Add(new JsonObject()
                .Add("Center", Vector(x / 2.0, 0.597428739 * z / 3.0, y / 2.0))
                .Add("Size", Vector(1.26884508 * x / 3.0, 0.994603038 * z / 3.0, 1.22155428 * y / 3.0)));
        return new JsonObject()
            .Add("BoxColliders", boxes)
            .Add("SphereColliders", new JsonArray())
            .Add("CapsuleColliders", new JsonArray());
    }
}
