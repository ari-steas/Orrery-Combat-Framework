using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.Game;
using System;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding
{
    public class WeaponLogic_Magazines
    {
        Loading Definition;
        Audio DefinitionAudio;
        private readonly Func<IMyInventory> GetInventoryFunc;
        private readonly SorterWeaponLogic sorterWeaponLogic;

        public WeaponLogic_Magazines(Loading definition, Audio definitionaudio, Func<IMyInventory> getInventoryFunc, bool startLoaded = false)
        {
            Definition = definition;
            DefinitionAudio = definitionaudio;
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

        public void UpdateReload(int ammoId)
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
                ShotsInMag = ProjectileDefinitionManager.GetDefinition(ammoId).Ungrouped.ShotsPerMagazine; // TODO tie into ammo
            }
        }

        public bool IsLoaded => ShotsInMag > 0;

        /// <summary>
        /// Mark a bullet as fired.
        /// </summary>
        public void UseShot(Vector3D muzzlePos)
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

                if (!string.IsNullOrEmpty(DefinitionAudio.ReloadSound))
                {
                    MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(DefinitionAudio.ReloadSound, muzzlePos);
                }
            }
        }

        public void EmptyMagazines()
        {
            ShotsInMag = 0;
            MagazinesLoaded = 0;
            NextReloadTime = Definition.ReloadTime;
        }
    }
}
