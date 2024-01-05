using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using YourName.ModName.Data.Scripts.HeartModule.Utility;

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "TestWeapon")]
    public class SorterWeaponLogic : MyGameLogicComponent
    {
        IMyConveyorSorter SorterWep;
        public readonly Guid HeartSettingsGUID = new Guid("06edc546-3e42-41f3-bc72-1d640035fbf2");
        public const int HeartSettingsUpdateCount = 60 * 1 / 10;
        int SyncCountdown;

        public readonly Heart_Settings Settings = new Heart_Settings();

        Heart_Utility Mod => Heart_Utility.Instance;

        //the state of shoot
        bool shoot = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            SorterWeaponTerminalControls.DoOnce(ModContext);

            SorterWep = (IMyConveyorSorter)Entity;
            if (SorterWep.CubeGrid?.Physics == null)
                return; // ignore ghost/projected grids

            LoadSettings(); // Load the settings including shoot option state
        }

        public override void UpdateAfterSimulation10()
        {

            MyAPIGateway.Utilities.ShowNotification("Syncing Settings");
            SyncSettings();

        }


        public bool Terminal_Heart_Shoot
        {
            get
            {
                MyAPIGateway.Utilities.ShowNotification("Shoot State: " + Settings.ShootState.ToString());

                return Settings.ShootState;
            }

            set
            {
                Settings.ShootState = true;

                SettingsChanged();

                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                MyAPIGateway.Utilities.ShowNotification("Shoot State: " + Settings.ShootState.ToString());

            }
        }


        public float Terminal_ExampleFloat { get; set; }

        #region Settings
        bool LoadSettings()
        {
            if (SorterWep.Storage == null)
                return false;

            string rawData;
            if (!SorterWep.Storage.TryGetValue(HeartSettingsGUID, out rawData))
                return false;

            try
            {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<Heart_Settings>(Convert.FromBase64String(rawData));

                if (loadedSettings != null)
                {
                    Settings.ShootState = loadedSettings.ShootState;
                    Settings.BasedSetting = loadedSettings.BasedSetting;
                    // Load the shoot state
                    return true;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error loading settings!\n{e}");
            }

            return false;
        }

        void SaveSettings()
        {
            try
            {
                if (SorterWep == null)
                    return; // called too soon or after it was already closed, ignore

                if (Settings == null)
                    throw new NullReferenceException($"Settings == null on entId={Entity?.EntityId}; modInstance={Heart_Utility.Instance != null}");

                if (MyAPIGateway.Utilities == null)
                    throw new NullReferenceException($"MyAPIGateway.Utilities == null; entId={Entity?.EntityId}; modInstance={Heart_Utility.Instance != null}");

                if (SorterWep.Storage == null)
                    SorterWep.Storage = new MyModStorageComponent();

                // Save the shoot state
                Settings.ShootState = shoot;

                SorterWep.Storage.SetValue(HeartSettingsGUID, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings)));
            }
            catch (Exception e)
            {
                Log.Error($"Error saving settings!\n{e}");
            }
        }

        void SettingsChanged()
        {
            if (SyncCountdown == 0)
            {
                SyncCountdown = HeartSettingsUpdateCount;
            }
        }

        void SyncSettings()
        {
            try
            {
                if (SyncCountdown > 0 && --SyncCountdown <= 0)
                {
                    SaveSettings();
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error syncing settings!\n{e}");
            }
        }

        public override bool IsSerialized()
        {
            try
            {
                SaveSettings(); // Ensure settings are saved when world is saved
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return base.IsSerialized();
        }
        #endregion

    }
}
