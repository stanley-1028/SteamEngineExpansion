namespace SEE.Core.Constants
{
    /// <summary>
    /// The three independent version tracks of the mod (requirements §11.6). They are never
    /// derived from each other: a UI-only release bumps <see cref="ModVersion"/> while the save
    /// schema may stay 1, a save-field change always bumps <see cref="SaveSchemaVersion"/>.
    /// </summary>
    public static class SEEVersion
    {
        /// <summary>Shipped SemVer written into <c>manifest.json</c> and the save header.</summary>
        public const string ModVersion = "0.1.0";

        /// <summary>Current save schema version; a structural field change increments it.</summary>
        public const int SaveSchemaVersion = 1;

        /// <summary>Current <c>SEE_Config.json</c> schema version (missing fields keep defaults).</summary>
        public const int ConfigSchemaVersion = 1;

        /// <summary>Oldest config schema that still loads: missing fields simply keep defaults.</summary>
        public const int MinSupportedConfigSchemaVersion = 1;
    }
}
