using System;
using SEE.Core.Abstractions;
using Timberborn.InventorySystem;

namespace SEE.Gameplay.Components
{
    /// <summary>
    /// Partial part: the dedicated water-tank inventory of one boiler (bug fix 2026-11-08).
    ///
    /// The v0.1.1 hotfix had moved the water good into the fuel <c>GoodConsumingBuildingSpec</c>
    /// inventory. That singled out one combined hauling target and gave up the dedicated tank.
    /// The tank is now a plain public input <see cref="Inventory"/> created by
    /// <see cref="SEEWaterInventoryInitializer"/>: haulers deliver water into it through the
    /// vanilla fill-input chain and the controller withdraws whole goods from it. The inventory is
    /// created disabled by the vanilla initializer, so the owner must enable it when the building
    /// finishes - a disabled inventory is silently skipped by the hauling layer.
    /// </summary>
    public sealed partial class SEEBoilerController
    {
        /// <summary>
        /// Finds the dedicated water-tank inventory created by <see cref="SEEWaterInventoryInitializer"/>.
        /// The primary lookup is the stable component name; an inventory that accepts water as an
        /// input good is accepted as a fallback so a renamed/older component still hauls.
        /// </summary>
        private void TryResolveWaterInventory()
        {
            if (_waterInventory != null)
            {
                return;
            }

            if (!HasComponent<Inventories>())
            {
                return;
            }

            try
            {
                Inventory? fallback = null;
                var all = GetComponent<Inventories>().AllInventories;
                for (int index = 0; index < all.Count; index++)
                {
                    Inventory? inventory = all[index];
                    if (inventory == null)
                    {
                        continue;
                    }

                    if (string.Equals(inventory.ComponentName,
                            SEEWaterInventoryInitializer.InventoryComponentName, StringComparison.Ordinal))
                    {
                        _waterInventory = inventory;
                        return;
                    }

                    if (fallback == null
                        && !ReferenceEquals(inventory, _fuelInventory)
                        && inventory.Capacity > 0
                        && inventory.Takes(GoodIds.Water))
                    {
                        fallback = inventory;
                    }
                }

                _waterInventory = fallback;
            }
            catch (Exception)
            {
                // A not-yet-initialised inventory list only delays the resolution; retry next tick.
                _waterInventory = null;
            }
        }

        /// <summary>
        /// Enables the dedicated water-tank inventory once the building is finished (vanilla
        /// inventory owners do the same in <c>OnEnterFinishedState</c>): a disabled inventory never
        /// joins <c>Inventories.EnabledInventories</c> and the hauling fill-input chain silently
        /// finds no target, so no water would ever be delivered.
        /// </summary>
        private void EnableWaterInventory()
        {
            Inventory? inventory = _waterInventory;
            if (inventory == null)
            {
                TryResolveWaterInventory();
                inventory = _waterInventory;
            }

            if (inventory == null)
            {
                return;
            }

            try
            {
                if (!inventory.Enabled)
                {
                    inventory.Enable();
                }
            }
            catch (Exception exception)
            {
                // Delivering water is a gameplay feature, never a reason to break activation.
                LogWarn("water tank inventory could not be enabled (" + exception.GetType().Name + ")");
            }
        }

        /// <summary>Disables the water tank again when the building leaves the finished state.</summary>
        private void DisableWaterInventory()
        {
            Inventory? inventory = _waterInventory;
            if (inventory == null)
            {
                return;
            }

            try
            {
                if (inventory.Enabled)
                {
                    inventory.Disable();
                }
            }
            catch (Exception exception)
            {
                LogWarn("water tank inventory could not be disabled (" + exception.GetType().Name + ")");
            }
        }
    }
}
