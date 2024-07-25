using Heart_Module.Data.Scripts.HeartModule;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.Setup.Adding
{
    public class WeaponLogic_Magazines
    {
        Loading Definition;
        Audio DefinitionAudio;
        SorterWeaponLogic Weapon;
        private readonly Func<IMyInventory> GetInventoryFunc;

        private int _selectedAmmoIndex = 0;
        private int _selectedAmmo = 0;
        private int shotsPerMag = 0;

        public int SelectedAmmoId
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
                _selectedAmmoIndex = idx;
                shotsPerMag = ProjectileDefinitionManager.GetDefinition(SelectedAmmoId).Ungrouped.ShotsPerMagazine;

                if (value == _selectedAmmo)
                    return;
                EmptyMagazines();

                HeartLog.Log("Set Loaded AmmoId: " + SelectedAmmoId + " | IDX " + SelectedAmmoIndex);
            }
        }

        public int SelectedAmmoIndex
        {
            get
            {
                return _selectedAmmoIndex;
            }
            set
            {
                if (Definition.Ammos.Length <= value || value < 0)
                    return;
                _selectedAmmo = ProjectileDefinitionManager.GetId(Definition.Ammos[value]);
                _selectedAmmoIndex = value;
                shotsPerMag = ProjectileDefinitionManager.GetDefinition(SelectedAmmoId).Ungrouped.ShotsPerMagazine;

                if (value == _selectedAmmoIndex)
                    return;
                EmptyMagazines();

                HeartLog.Log("Set Loaded AmmoIdx: " + SelectedAmmoId + " | IDX " + SelectedAmmoIndex);
            }
        }

        public WeaponLogic_Magazines(SorterWeaponLogic weapon, Func<IMyInventory> getInventoryFunc, int ammoIdx, bool startLoaded = false)
        {
            Weapon = weapon;
            Definition = weapon.Definition.Loading;
            DefinitionAudio = weapon.Definition.Audio;
            GetInventoryFunc = getInventoryFunc;
            RemainingReloads = Definition.MaxReloads;
            NextReloadTime = Definition.ReloadTime;
            SelectedAmmoIndex = ammoIdx;
            if (startLoaded)
            {
                MagazinesLoaded = Definition.MagazinesToLoad;
                ShotsInMag = ProjectileDefinitionManager.GetDefinition(SelectedAmmoId).Ungrouped.ShotsPerMagazine;
            }
        }

        public int MagazinesLoaded = 0;
        public int ShotsInMag = 0;
        public float NextReloadTime = -1; // In seconds
        public int RemainingReloads;

        public void UpdateReload(float delta = 1 / 60f)
        {
            if (RemainingReloads == 0)
                return;

            if (MagazinesLoaded >= Definition.MagazinesToLoad) // Don't load mags if already at capacity
                return;

            if (NextReloadTime == -1)
                return;

            NextReloadTime -= delta;

            if (NextReloadTime <= 0)
            {
                var inventory = GetInventoryFunc();
                var ammoDefinition = ProjectileDefinitionManager.GetDefinition(SelectedAmmoId);
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
                        //MyVisualScriptLogicProvider.ShowNotification($"Unable to reload - {magazineItem} not found in inventory.", 1000 / 60, "Red");
                        return;
                    }
                }
                else
                {
                    // Notify when MagazineItemToConsume is not specified
                    // TODO: Note in debug log
                    //MyVisualScriptLogicProvider.ShowNotification("MagazineItemToConsume not specified, proceeding with default reload behavior.", 1000 / 60, "Blue");
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
                if (MyAPIGateway.Session.IsServer)
                {
                    HeartData.I.Net.SendToEveryoneInSync(new n_MagazineUpdate()
                    {
                        WeaponEntityId = Weapon.SorterWep.EntityId,
                        MillisecondsFromMidnight = (int)DateTime.UtcNow.TimeOfDay.TotalMilliseconds,
                        MagazinesLoaded = MagazinesLoaded,
                        NextMuzzleIdx = (short)Weapon.NextMuzzleIdx,
                    }, Weapon.SorterWep.GetPosition());
                }

                if (!string.IsNullOrEmpty(DefinitionAudio.ReloadSound))
                {
                    MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(DefinitionAudio.ReloadSound, muzzlePos);
                }
            }
        }

        public void EmptyMagazines(bool doSyncIfClient = false)
        {
            ShotsInMag = 0;
            MagazinesLoaded = 0;
            NextReloadTime = Definition.ReloadTime;

            if (MyAPIGateway.Session.IsServer || doSyncIfClient)
            {
                HeartData.I.Net.SendToEveryoneInSync(new n_MagazineUpdate()
                {
                    WeaponEntityId = Weapon.SorterWep.EntityId,
                    MillisecondsFromMidnight = (int)DateTime.UtcNow.TimeOfDay.TotalMilliseconds,
                    MagazinesLoaded = MagazinesLoaded,
                    NextMuzzleIdx = (short)Weapon.NextMuzzleIdx,
                }, Weapon.SorterWep.GetPosition());
            }
        }
    }
}
