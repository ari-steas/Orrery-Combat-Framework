using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using System;
using VRage.Game;
using VRage.Game.ModAPI;

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding
{
    public class WeaponLogic_Magazines
    {
        Loading Definition;
        private readonly Func<IMyInventory> GetInventoryFunc;

        public WeaponLogic_Magazines(Loading definition, Func<IMyInventory> getInventoryFunc, bool startLoaded = false)
        {
            Definition = definition;
            GetInventoryFunc = getInventoryFunc;
            RemainingReloads = Definition.MaxReloads;
            NextReloadTime = Definition.ReloadTime;
            if (startLoaded)
            {
                MagazinesLoaded = Definition.MagazinesToLoad;
                ShotsInMag = 10; // TODO tie into ammo
            }
        }

        public int MagazinesLoaded = 0;
        public int ShotsInMag = 0;
        public float NextReloadTime = -1; // In seconds
        public int RemainingReloads;

        public void UpdateReload()
        {
            if (RemainingReloads == 0)
                return;

            if (MagazinesLoaded > Definition.MagazinesToLoad) // Don't load mags if already at capacity
                return;

            if (NextReloadTime == -1)
                return;

            NextReloadTime -= 1 / 60f;

            if (NextReloadTime <= 0)
            {
                MagazinesLoaded++;
                RemainingReloads--;
                NextReloadTime = Definition.ReloadTime;
                ShotsInMag = 10; // TODO tie into ammo
            }
        }

        public bool IsLoaded => ShotsInMag > 0;

        /// <summary>
        /// Mark a bullet as fired.
        /// </summary>
        public void UseShot()
        {
            ShotsInMag--;
            if (ShotsInMag <= 0)
            {
                MagazinesLoaded--;

                // Check and remove a steel plate from the inventory
                var inventory = GetInventoryFunc();
                var steelPlate = new MyDefinitionId(typeof(MyObjectBuilder_Component), "SteelPlate");
                if (inventory.ContainItems(1, steelPlate))
                {
                    inventory.RemoveItemsOfType(1, steelPlate);
                }
            }
        }
    }
}
