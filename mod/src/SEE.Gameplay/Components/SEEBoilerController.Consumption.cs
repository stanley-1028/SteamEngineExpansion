using System;
using SEE.Core.Abstractions;
using SEE.Core.Formulas;
using Timberborn.Goods;
using Timberborn.InventorySystem;

namespace SEE.Gameplay.Components
{
    /// <summary>
    /// Partial part: the single fuel/water withdrawal path of one boiler (AD-3, requirements
    /// §13.4/§8.3). The vanilla <c>GoodConsumingBuilding</c> consumer is paused permanently, so the
    /// whole consumption is booked here:
    /// <list type="bullet">
    /// <item>fuel is booked as energy (Log 1.0, Plank 2.0; Log first, Plank as the fallback) from the
    /// vanilla GoodConsuming inventory;</item>
    /// <item>water is booked as whole goods with a float remainder smaller than one item from the
    /// dedicated water-tank inventory (the public input inventory haulers deliver water into);</item>
    /// <item>nothing is withdrawn unless the state actually produces and carries load (AD-12).</item>
    /// </list>
    /// The pure arithmetic lives in <see cref="SeeConsumptionLedger"/> so the 1-game-day
    /// reconciliation test (REQ-IMPL-04) runs without the game.
    /// </summary>
    public sealed partial class SEEBoilerController
    {
        /// <summary>Refreshes the cached inventory counters and the two supply flags.</summary>
        private void RefreshStock()
        {
            int fuelTotal = 0;
            bool hasFuel = false;
            Inventory? fuel = _fuelInventory;
            if (fuel != null)
            {
                try
                {
                    if (_allowsLog)
                    {
                        if (fuel.UnreservedAmountInStock(GoodIds.Log) > 0)
                        {
                            hasFuel = true;
                        }

                        fuelTotal += fuel.AmountInStock(GoodIds.Log);
                    }

                    if (_allowsPlank)
                    {
                        if (fuel.UnreservedAmountInStock(GoodIds.Plank) > 0)
                        {
                            hasFuel = true;
                        }

                        fuelTotal += fuel.AmountInStock(GoodIds.Plank);
                    }
                }
                catch (Exception)
                {
                    // Keep the previous observation; a damaged inventory only delays the state.
                    hasFuel = _hasFuel;
                    fuelTotal = _fuelInStock;
                }
            }

            int waterTotal = 0;
            bool hasWater = false;
            Inventory? water = _waterInventory;
            if (water != null)
            {
                try
                {
                    hasWater = water.UnreservedAmountInStock(GoodIds.Water) > 0;
                    waterTotal = water.AmountInStock(GoodIds.Water);
                }
                catch (Exception)
                {
                    hasWater = _hasWater;
                    waterTotal = _waterInStock;
                }
            }

            _hasFuel = hasFuel;
            _fuelInStock = fuelTotal;
            _hasWater = hasWater;
            _waterInStock = waterTotal;
        }

        /// <summary>
        /// Withdraws fuel/water for the hours of one loaded settlement and accumulates wear. The
        /// caller guarantees <see cref="BoilerStateMachine.IsLoaded"/> for the current state.
        /// </summary>
        /// <param name="hours">Game hours of this settlement step.</param>
        private void SettleSupplies(double hours)
        {
            float outputFraction = BoilerFormulas.Clamp01(_nodeMultiplier);

            Inventory? fuel = _fuelInventory;
            if (fuel != null && _fuelPerHour > 0f)
            {
                var input = new FuelStepInput
                {
                    FuelPerHour = _fuelPerHour,
                    Hours = hours,
                    LoadFraction = outputFraction,
                    Vanilla = _fuelVanilla,
                    LogStock = _allowsLog ? UnreservedAmount(fuel, GoodIds.Log) : 0,
                    PlankStock = _allowsPlank ? UnreservedAmount(fuel, GoodIds.Plank) : 0,
                    LogEnergy = _logEnergy,
                    PlankEnergy = _plankEnergy,
                    Carry = _fuelCarry
                };
                FuelStepResult result = SeeConsumptionLedger.FuelStep(input);
                if (result.LogGoods > 0)
                {
                    TakeFuel(GoodIds.Log, result.LogGoods);
                }

                if (result.PlankGoods > 0)
                {
                    TakeFuel(GoodIds.Plank, result.PlankGoods);
                }

                _fuelCarry = result.Carry;
            }

            Inventory? water = _waterInventory;
            if (water != null && _waterPerHour > 0f)
            {
                var input = new WaterStepInput
                {
                    WaterPerHour = _waterPerHour,
                    Hours = hours,
                    LoadFraction = outputFraction,
                    WaterStock = UnreservedAmount(water, GoodIds.Water),
                    Carry = _waterCarry
                };
                WaterStepResult result = SeeConsumptionLedger.WaterStep(input);
                if (result.WaterGoods > 0)
                {
                    TakeWater(result.WaterGoods);
                }

                _waterCarry = result.Carry;
            }

            // Wear only while the boiler really carries load (requirements §9.1/§9.2, AD-12).
            if (_rules.Enabled && _actualOutput > 0 && _ratedHorsepower > 0)
            {
                float loadRatio = BoilerFormulas.Clamp01((float)_actualOutput / _ratedHorsepower);
                _ledger.AccumulateWear(hours, _rules.IntervalDays, loadRatio);
            }
        }

        /// <summary>Drops unpayable remainders while the boiler is idle/paused/blocked (no debt).</summary>
        private void ClampCarries()
        {
            if (double.IsNaN(_fuelCarry) || _fuelCarry < 0.0)
            {
                _fuelCarry = 0.0;
            }
            else if (_fuelCarry >= 1.0)
            {
                _fuelCarry = 1.0 - 1e-6;
            }

            if (double.IsNaN(_waterCarry) || _waterCarry < 0.0)
            {
                _waterCarry = 0.0;
            }
            else if (_waterCarry >= 1.0)
            {
                _waterCarry = 1.0 - 1e-6;
            }
        }

        private void TakeFuel(string goodId, int amount)
        {
            Inventory? fuel = _fuelInventory;
            if (fuel == null || amount <= 0)
            {
                return;
            }

            try
            {
                int available = fuel.UnreservedAmountInStock(goodId);
                if (available < amount)
                {
                    amount = available;
                }

                if (amount > 0)
                {
                    fuel.TakeConsumed(new GoodAmount(goodId, amount));
                }
            }
            catch (Exception exception)
            {
                LogWarn("fuel withdrawal failed (" + exception.GetType().Name + ")");
            }
        }

        private void TakeWater(int amount)
        {
            Inventory? water = _waterInventory;
            if (water == null || amount <= 0)
            {
                return;
            }

            try
            {
                int available = water.UnreservedAmountInStock(GoodIds.Water);
                if (available < amount)
                {
                    amount = available;
                }

                if (amount > 0)
                {
                    // The water good of the dedicated tank is giveable-and-takeable (haulers must
                    // be able to bring it in), so a withdrawal is the normal inventory take, not
                    // the GoodConsuming-only TakeConsumed bookkeeping call.
                    water.TakeExisting(new GoodAmount(GoodIds.Water, amount));
                }
            }
            catch (Exception exception)
            {
                LogWarn("water withdrawal failed (" + exception.GetType().Name + ")");
            }
        }

        private static int UnreservedAmount(Inventory inventory, string goodId)
        {
            try
            {
                return inventory.UnreservedAmountInStock(goodId);
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
