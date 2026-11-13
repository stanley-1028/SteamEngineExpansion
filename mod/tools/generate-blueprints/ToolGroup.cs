namespace SEE.Tools.GenerateBlueprints;

/// <summary>
/// The single source of the SEE bottom-bar building group (the collapsible "Steam Engines" menu).
///
/// <para>
/// <c>Order = 72</c> places the group between the vanilla Power group (70) and Science group (80).
/// No nested group spec exists in Timberborn 1.1.2.8: a <c>BlockObjectToolGroupSpec</c> whose Id is
/// referenced by at least one <c>PlaceableBlockObjectSpec.ToolGroupId</c> becomes a bottom-bar button
/// (<c>GameBlockObjectButtons.CreateRegularBlockObjectToolGroups</c> skips only FallbackGroup specs and
/// groups without members), and that button expands/collapses its building row through
/// <c>EnterToolGroup</c>.
/// </para>
///
/// <para>
/// The spec lives at <c>data/BlockObjectToolGroups/</c> and the sprite at
/// <c>data/ToolGroupIcons/</c> on purpose: every <c>*.blueprint.json</c> anywhere under the mod root
/// is converted by the game's <c>ModBlueprintConverter</c> (file name ends with
/// <c>.blueprint.json</c>) and every <c>.png</c> by <c>ModSpriteConverter</c>, so neither folder has
/// to be listed in a TemplateCollection. The asset reference is the mod-relative path without the
/// extension, like every other SEE icon.
/// </para>
/// </summary>
internal static class ToolGroup
{
    /// <summary>Group id referenced by all 24 boiler blueprints (<c>PlaceableBlockObjectSpec.ToolGroupId</c>).</summary>
    public const string Id = "SteamEngines";

    /// <summary>Sort order of the bottom-bar button: after vanilla Power (70), before Science (80).</summary>
    public const int Order = 72;

    /// <summary>Localization key of the button label.</summary>
    public const string NameLocKey = "UI.SEE.ToolGroup.SteamEngines";

    /// <summary>Mod-relative sprite path without extension; the PNG sits next to it.</summary>
    public const string IconPath = "ToolGroupIcons/" + Id;

    /// <summary>Mod-relative path of the generated group blueprint.</summary>
    public const string BlueprintRelativePath = "data/BlockObjectToolGroups/BlockObjectToolGroup." + Id + ".blueprint.json";

    /// <summary>Mod-relative path of the generated 128x128 group icon.</summary>
    public const string IconRelativePath = "data/ToolGroupIcons/" + Id + ".png";

    /// <summary>Mod-relative path of the generated sprite sidecar (<c>{"isSprite": true}</c>).</summary>
    public const string IconMetaRelativePath = IconRelativePath + ".meta.json";

    /// <summary>Builds the complete <c>BlockObjectToolGroupSpec</c> blueprint tree.</summary>
    public static JsonObject Blueprint() =>
        new JsonObject()
            .Add("BlockObjectToolGroupSpec", new JsonObject()
                .Add("Id", new JsonString(Id))
                .Add("Order", Numbers.Integer(Order))
                .Add("NameLocKey", new JsonString(NameLocKey))
                .Add("Icon", new JsonString(IconPath))
                .Add("FallbackGroup", new JsonBool(false)));
}
