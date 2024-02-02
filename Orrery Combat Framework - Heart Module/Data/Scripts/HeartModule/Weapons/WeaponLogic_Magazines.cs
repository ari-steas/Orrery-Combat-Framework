using Heart_Module.Data.Scripts.HeartModule;
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

        private int _ammoIndex = 0;
        private int _selectedAmmo = 0;
        private int shotsPerMag = 0;

        public int SelectedAmmo
        {
            get
            {
                return _selectedAmmo;
            }
            set
            {
                int idx = Array.IndexOf(Definition.Ammos, value);
                if (idx == -1)
                    return;
                _selectedAmmo = value;
                _ammoIndex = idx;
                shotsPerMag = ProjectileDefinitionManager.GetDefinition(SelectedAmmo).Ungrouped.ShotsPerMagazine;
            }
        }

        public int AmmoIndex
        {
            get
            {
                return _ammoIndex;
            }
            set
            {
                if (Definition.Ammos.Length <= value || value < 0)
                    return;
                _ammoIndex = value;
                _selectedAmmo = ProjectileDefinitionManager.GetId(Definition.Ammos[value]);
                shotsPerMag = ProjectileDefinitionManager.GetDefinition(SelectedAmmo).Ungrouped.ShotsPerMagazine;
            }
        }

        public WeaponLogic_Magazines(Loading definition, Audio definitionaudio, Func<IMyInventory> getInventoryFunc, int ammoIdx, bool startLoaded = false)
        {
            Definition = definition;
            DefinitionAudio = definitionaudio;
            GetInventoryFunc = getInventoryFunc;
            RemainingReloads = Definition.MaxReloads;
            NextReloadTime = Definition.ReloadTime;
            AmmoIndex = ammoIdx;
            if (startLoaded)
            {
                MagazinesLoaded = Definition.MagazinesToLoad;
                ShotsInMag = ProjectileDefinitionManager.GetDefinition(SelectedAmmo).Ungrouped.ShotsPerMagazine;
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

            if (MagazinesLoaded >= Definition.MagazinesToLoad) // Don't load mags if already at capacity
                return;

            if (NextReloadTime == -1)
                return;

            NextReloadTime -= 1 / 60f;

            if (NextReloadTime <= 0)
            {
                var inventory = GetInventoryFunc();
                var ammoDefinition = ProjectileDefinitionManager.GetDefinition(SelectedAmmo);
                string magazineItem = ammoDefinition.Ungrouped.MagazineItemToConsume;

                // Check and remove the specified item from the inventory
                if (!string.IsNullOrWhiteSpace(magazineItem))
                {
                    var itemToConsume = new MyDefinitionId(typeof(MyObjectBuilder_Component), magazineItem);
                    if (inventory.ContainItems(1, itemToConsume))
                    {
                        inventory.RemoveItemsOfType(1, itemToConsume);

                        // Notify item consumption
                        MyVisualScriptLogicProvider.ShowNotification($"Consumed 1 {magazineItem} for reloading.", 1000 / 60, "White");

                        // Reload logic
                        MagazinesLoaded++;
                        RemainingReloads--;
                        NextReloadTime = Definition.ReloadTime;
                        ShotsInMag += shotsPerMag;

                        if (!string.IsNullOrEmpty(DefinitionAudio.ReloadSound))
                        {
                            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(DefinitionAudio.ReloadSound, Vector3D.Zero); // Assuming Vector3D.Zero as placeholder
                        }
                    }
                    else
                    {
                        // Notify item not available
                        MyVisualScriptLogicProvider.ShowNotification($"Unable to reload - {magazineItem} not found in inventory.", 1000 / 60, "Red");
                        return;
                    }
                }
                else
                {
                    // Notify when MagazineItemToConsume is not specified
                    MyVisualScriptLogicProvider.ShowNotification("MagazineItemToConsume not specified, proceeding with default reload behavior.", 1000 / 60, "Blue");
                }

                MagazinesLoaded++;
                RemainingReloads--;
                NextReloadTime = Definition.ReloadTime;
                ShotsInMag += shotsPerMag;
            }
        }

        public bool IsLoaded => ShotsInMag > 0;

        /// <summary>
        /// Mark a bullet as fired.
        /// </summary>
        public void UseShot(Vector3D muzzlePos)
        {
            ShotsInMag--;
            if (ShotsInMag % shotsPerMag == 0)
            {
                MagazinesLoaded--;

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
