namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Read-only snapshot of one maintenance material of one boiler: the whole amount the next
    /// cycle needs and how much of it the district currently holds (requirements §9.4, §15.2).
    ///
    /// This is a display value only. Gameplay fills it from the cached district-inventory snapshot
    /// it already takes for the maintenance decision; the UI never queries a game component.
    /// </summary>
    public readonly struct MaintenanceMaterialStatus
    {
        /// <summary>Creates one material row.</summary>
        /// <param name="goodId">Vanilla good id, for example <c>Log</c>.</param>
        /// <param name="required">Whole items the next maintenance cycle consumes.</param>
        /// <param name="inStock">Whole unreserved items currently available in the district.</param>
        public MaintenanceMaterialStatus(string goodId, int required, int inStock)
        {
            GoodId = goodId ?? string.Empty;
            Required = required > 0 ? required : 0;
            InStock = inStock > 0 ? inStock : 0;
        }

        /// <summary>Vanilla good id, for example <c>Log</c> or <c>Plank</c>.</summary>
        public string GoodId { get; }

        /// <summary>Whole items required for the next maintenance cycle; never negative.</summary>
        public int Required { get; }

        /// <summary>Whole unreserved items currently in the district; never negative.</summary>
        public int InStock { get; }

        /// <summary>How many items are still missing for this material (0 when satisfied).</summary>
        public int Missing
        {
            get
            {
                int missing = Required - InStock;
                return missing > 0 ? missing : 0;
            }
        }

        /// <summary>True when the district currently holds every required item of this material.</summary>
        public bool IsSatisfied => InStock >= Required;
    }
}
