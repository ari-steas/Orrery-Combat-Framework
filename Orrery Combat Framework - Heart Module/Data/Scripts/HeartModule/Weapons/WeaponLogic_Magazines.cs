using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.ModAPI;

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding
{
    public class WeaponLogic_Magazines
    {
        Loading Definition;
        public WeaponLogic_Magazines(Loading definition, bool startLoaded = false)
        {
            Definition = definition;
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
            MyAPIGateway.Utilities.ShowNotification("Shots: " + ShotsInMag, 1000/60);
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
            }
        }
    }
}
