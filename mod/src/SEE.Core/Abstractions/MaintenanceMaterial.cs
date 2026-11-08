namespace SEE.Core.Abstractions
{
    /// <summary>
    /// One maintenance material requirement: a vanilla good id plus the whole amount consumed per
    /// maintenance cycle (requirements §9.4/§13.3).
    ///
    /// This is the pure Core form used by the ledger and the tests. Blueprints carry the game's
    /// own <c>GoodAmountSpec</c> and convert to this record in the gameplay layer, because the
    /// blueprint deserializer only recurses into vanilla <c>ImmutableArray&lt;GoodAmountSpec&gt;</c>
    /// element-wise (trap B5).
    /// </summary>
    /// <param name="GoodId">Vanilla good id, for example "Log".</param>
    /// <param name="Amount">Whole items consumed per cycle.</param>
    public readonly record struct MaintenanceMaterial(string GoodId, int Amount);
}
