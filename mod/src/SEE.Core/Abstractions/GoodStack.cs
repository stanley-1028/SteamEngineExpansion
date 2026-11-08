using System;

namespace SEE.Core.Abstractions
{
    /// <summary>
    /// Immutable (good id, amount) pair. Amounts are whole items; the good id is the vanilla good
    /// id string (for example "Log", "Plank", "Gear", "MetalBlock", "Water").
    /// </summary>
    public readonly struct GoodStack : IEquatable<GoodStack>
    {
        /// <summary>Creates a pair.</summary>
        /// <param name="goodId">Vanilla good id.</param>
        /// <param name="amount">Non-negative item count.</param>
        public GoodStack(string goodId, int amount)
        {
            GoodId = goodId ?? throw new ArgumentNullException(nameof(goodId));
            Amount = amount;
        }

        /// <summary>Vanilla good id.</summary>
        public string GoodId { get; }

        /// <summary>Item count; callers clamp it to a non-negative value.</summary>
        public int Amount { get; }

        /// <inheritdoc />
        public bool Equals(GoodStack other) =>
            string.Equals(GoodId, other.GoodId, StringComparison.Ordinal) && Amount == other.Amount;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is GoodStack other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (StringComparer.Ordinal.GetHashCode(GoodId) * 397) ^ Amount;

        /// <inheritdoc />
        public override string ToString() => Amount + " " + GoodId;

        /// <summary>Equality operator.</summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True when both members are equal.</returns>
        public static bool operator ==(GoodStack left, GoodStack right) => left.Equals(right);

        /// <summary>Inequality operator.</summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True when the members differ.</returns>
        public static bool operator !=(GoodStack left, GoodStack right) => !left.Equals(right);
    }
}