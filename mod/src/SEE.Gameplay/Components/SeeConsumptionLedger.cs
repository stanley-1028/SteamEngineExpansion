using System;
using System.Diagnostics;
using SEE.Core.Formulas;

namespace SEE.Gameplay.Components
{
    /// <summary>
    /// Input of one fuel settlement step (CONTRACT_SEE §8.3, AD-3). Energy is booked in Log-equivalent
    /// items: <see cref="FuelPerHour"/> is the blueprint rate, <see cref="LogEnergy"/>/
    /// <see cref="PlankEnergy"/> are the data-driven energy values (Log 1.0, Plank 2.0).
    /// </summary>
    public struct FuelStepInput
    {
        /// <summary>Rated fuel items per game hour at full load (already faction-adjusted).</summary>
        public float FuelPerHour;

        /// <summary>Game hours of this settlement step.</summary>
        public double Hours;

        /// <summary>Output fraction 0..1 (the multiplier written to the mechanical node).</summary>
        public float LoadFraction;

        /// <summary>True for <c>fuelScaling="vanilla"</c>: burn the rated rate whenever the load is positive.</summary>
        public bool Vanilla;

        /// <summary>Takeable Log items in the fuel inventory.</summary>
        public int LogStock;

        /// <summary>Takeable Plank items in the fuel inventory.</summary>
        public int PlankStock;

        /// <summary>Energy value of one Log (1.0 when the data is missing).</summary>
        public float LogEnergy;

        /// <summary>Energy value of one Plank (2.0 when the data is missing).</summary>
        public float PlankEnergy;

        /// <summary>Unspent energy carried over from the previous step; never persisted.</summary>
        public double Carry;
    }

    /// <summary>Result of one fuel settlement step; the caller performs the inventory withdrawal.</summary>
    public readonly struct FuelStepResult
    {
        /// <summary>Creates one result.</summary>
        /// <param name="logGoods">Whole Log items to withdraw.</param>
        /// <param name="plankGoods">Whole Plank items to withdraw.</param>
        /// <param name="consumed">Energy booked by the withdrawal (goods times their energy value).</param>
        /// <param name="carry">Unspent energy after the step.</param>
        public FuelStepResult(int logGoods, int plankGoods, double consumed, double carry)
        {
            LogGoods = logGoods;
            PlankGoods = plankGoods;
            Consumed = consumed;
            Carry = carry;
        }

        /// <summary>Whole Log items to withdraw.</summary>
        public int LogGoods { get; }

        /// <summary>Whole Plank items to withdraw.</summary>
        public int PlankGoods { get; }

        /// <summary>Energy booked by the withdrawal.</summary>
        public double Consumed { get; }

        /// <summary>Unspent energy after the step.</summary>
        public double Carry { get; }
    }

    /// <summary>Input of one water settlement step (CONTRACT_SEE §8.3: float remainder, whole goods).</summary>
    public struct WaterStepInput
    {
        /// <summary>Rated water items per game hour at full load.</summary>
        public float WaterPerHour;

        /// <summary>Game hours of this settlement step.</summary>
        public double Hours;

        /// <summary>Output fraction 0..1.</summary>
        public float LoadFraction;

        /// <summary>Takeable Water items in the tank inventory.</summary>
        public int WaterStock;

        /// <summary>Unspent water carried over from the previous step; never persisted.</summary>
        public double Carry;
    }

    /// <summary>Result of one water settlement step; the caller performs the inventory withdrawal.</summary>
    public readonly struct WaterStepResult
    {
        /// <summary>Creates one result.</summary>
        /// <param name="waterGoods">Whole Water items to withdraw.</param>
        /// <param name="carry">Unspent water after the step.</param>
        public WaterStepResult(int waterGoods, double carry)
        {
            WaterGoods = waterGoods;
            Carry = carry;
        }

        /// <summary>Whole Water items to withdraw.</summary>
        public int WaterGoods { get; }

        /// <summary>Unspent water after the step.</summary>
        public double Carry { get; }
    }

    /// <summary>
    /// Pure fuel/water integerisation and accounting shared by the boiler controller and the offline
    /// reconciliation tests (requirements §13.4/§8.3, REQ-IMPL-04/06). No game type is referenced, so
    /// the same code is unit-testable without the game.
    ///
    /// Rules:
    /// <list type="bullet">
    /// <item>fuel is booked as energy: Log = 1.0, Plank = 2.0; Log is consumed first, then Plank;</item>
    /// <item>water is booked as whole items: the float remainder accumulates and items are withdrawn
    /// when it reaches 1.0 (idle/blocked time never accumulates debt);</item>
    /// <item>a step never withdraws more goods than the stock offers; the leftover debt is capped so
    /// a long blocked period cannot burst-consume after resupply.</item>
    /// </list>
    /// </summary>
    public static class SeeConsumptionLedger
    {
        /// <summary>Hard bound of goods withdrawn in one step (guards corrupt rates; 8x the shipped maximum).</summary>
        public const int MaxGoodsPerStep = 64;

        /// <summary>Energy value used when the blueprint data is missing: Log.</summary>
        public const float DefaultLogEnergy = 1.0f;

        /// <summary>Energy value used when the blueprint data is missing: Plank.</summary>
        public const float DefaultPlankEnergy = 2.0f;

        /// <summary>
        /// Output fraction of one step: the multiplier the controller wrote to the node (AD-3). A
        /// vanilla fuel model burns the rated rate whenever the load is positive (REQ-POWER-11).
        /// </summary>
        /// <param name="nodeMultiplier">Multiplier written to the mechanical node, 0..1.</param>
        /// <param name="vanilla">True for the vanilla scaling model.</param>
        /// <returns>Fraction in 0..1.</returns>
        public static float FuelFraction(float nodeMultiplier, bool vanilla)
        {
            float fraction = BoilerFormulas.Clamp01(nodeMultiplier);
            if (!vanilla)
            {
                return fraction;
            }

            return fraction > 0f ? 1f : 0f;
        }

        /// <summary>Settles one fuel step and returns the withdrawal the caller must perform.</summary>
        /// <param name="input">Rates, stocks, energy values and the carried remainder.</param>
        /// <returns>The withdrawal plus the new remainder; never throws.</returns>
        public static FuelStepResult FuelStep(FuelStepInput input)
        {
            double carry = Sanitize(input.Carry);
            float fraction = FuelFraction(input.LoadFraction, input.Vanilla);
            double rate = Sanitize(input.FuelPerHour);
            double hours = SanitizeHours(input.Hours);
            double demand = rate * fraction * hours;
            if (demand <= 0.0)
            {
                return new FuelStepResult(0, 0, 0.0, carry);
            }

            double logValue = EnergyValue(input.LogEnergy, DefaultLogEnergy);
            double plankValue = EnergyValue(input.PlankEnergy, DefaultPlankEnergy);
            int logStock = ClampStock(input.LogStock);
            int plankStock = ClampStock(input.PlankStock);
            double energy = carry + demand;
            int logGoods = 0;
            int plankGoods = 0;
            int taken = 0;
            while (taken < MaxGoodsPerStep)
            {
                if (logStock > 0 && energy + Epsilon >= logValue)
                {
                    energy -= logValue;
                    logStock--;
                    logGoods++;
                }
                else if (plankStock > 0 && energy + Epsilon >= plankValue)
                {
                    energy -= plankValue;
                    plankStock--;
                    plankGoods++;
                }
                else
                {
                    break;
                }

                taken++;
            }

            energy = ClampCarry(energy, logValue, plankValue, logStock + plankStock);
            double consumed = (logGoods * logValue) + (plankGoods * plankValue);
            return new FuelStepResult(logGoods, plankGoods, consumed, energy);
        }

        /// <summary>Settles one water step and returns the withdrawal the caller must perform.</summary>
        /// <param name="input">Rate, stock, fraction and the carried remainder.</param>
        /// <returns>The withdrawal plus the new remainder; never throws.</returns>
        public static WaterStepResult WaterStep(WaterStepInput input)
        {
            double carry = Sanitize(input.Carry);
            float fraction = BoilerFormulas.Clamp01(input.LoadFraction);
            double demand = Sanitize(input.WaterPerHour) * fraction * SanitizeHours(input.Hours);
            if (demand <= 0.0)
            {
                return new WaterStepResult(0, carry);
            }

            int stock = ClampStock(input.WaterStock);
            double amount = carry + demand;
            int goods = 0;
            if (amount >= 1.0)
            {
                double whole = Math.Floor(amount);
                goods = whole >= MaxGoodsPerStep ? MaxGoodsPerStep : (int)whole;
            }

            if (goods > stock)
            {
                goods = stock;
            }

            double remainder = amount - goods;
            if (remainder >= 1.0)
            {
                // The stock could not pay the whole debt (partially blocked step): keep at most one
                // item worth so a resupply cannot burst-consume a long stopped period.
                remainder = 1.0 - Epsilon;
            }

            return new WaterStepResult(goods, remainder < 0.0 ? 0.0 : remainder);
        }

        /// <summary>
        /// Offline benchmark entry of the pure accounting path (REQ-SAFE-08 budget evidence). Not used
        /// by the game; the test runner invokes it once and asserts the per-step cost is far below the
        /// 3 µs/boiler budget.
        /// </summary>
        /// <param name="iterations">Number of settlement steps to run.</param>
        /// <returns>Average nanoseconds per step, or -1 for an invalid iteration count.</returns>
        public static double BenchmarkStepNanoseconds(int iterations)
        {
            if (iterations <= 0)
            {
                return -1.0;
            }

            var fuel = new FuelStepInput
            {
                FuelPerHour = 0.11f,
                Hours = 1.0 / 30.0,
                LoadFraction = 0.85f,
                LogStock = 3,
                PlankStock = 2,
                LogEnergy = DefaultLogEnergy,
                PlankEnergy = DefaultPlankEnergy,
                Carry = 0.2
            };
            var water = new WaterStepInput
            {
                WaterPerHour = 0.32f,
                Hours = 1.0 / 30.0,
                LoadFraction = 0.85f,
                WaterStock = 30,
                Carry = 0.4
            };
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < iterations; i++)
            {
                FuelStepResult fuelResult = FuelStep(fuel);
                WaterStepResult waterResult = WaterStep(water);
                // Consume the results so the loop cannot be optimised away.
                fuel.Carry = fuelResult.Carry + fuelResult.Consumed * 0.0;
                water.Carry = waterResult.Carry + waterResult.WaterGoods * 0.0;
            }

            double elapsed = (Stopwatch.GetTimestamp() - start) * 1_000_000_000.0 / Stopwatch.Frequency;
            return elapsed / iterations;
        }

        private const double Epsilon = 1e-9;

        private static double EnergyValue(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                return fallback;
            }

            return value;
        }

        private static double Sanitize(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0 ? 0.0 : value;
        }

        private static double SanitizeHours(double hours)
        {
            return double.IsNaN(hours) || double.IsInfinity(hours) || hours <= 0.0 ? 0.0 : hours;
        }

        private static int ClampStock(int stock)
        {
            if (stock < 0)
            {
                return 0;
            }

            return stock > MaxGoodsPerStep ? MaxGoodsPerStep : stock;
        }

        /// <summary>
        /// Keeps the energy remainder below one takable good. When both stock piles are empty the
        /// debt is capped at one item so a long blocked period cannot burst-consume after resupply.
        /// </summary>
        private static double ClampCarry(double energy, double logValue, double plankValue, int remainingStock)
        {
            if (double.IsNaN(energy) || energy <= 0.0)
            {
                return 0.0;
            }

            if (remainingStock > 0)
            {
                double maxValue = logValue > plankValue ? logValue : plankValue;
                return energy >= maxValue ? maxValue - Epsilon : energy;
            }

            double minValue = logValue < plankValue ? logValue : plankValue;
            return energy >= minValue ? minValue - Epsilon : energy;
        }
    }
}
