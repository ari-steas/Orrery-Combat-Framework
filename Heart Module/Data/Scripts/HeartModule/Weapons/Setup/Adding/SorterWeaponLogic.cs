using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using YourName.ModName.Data.Scripts.HeartModule.Utility;

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding
{
    // For more info about the gamelogic comp see https://github.com/THDigi/SE-ModScript-Examples/blob/master/Data/Scripts/Examples/BasicExample_GameLogicAndSession/GameLogic.cs
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

            // stuff and things
        }

        // these are going to be set or retrieved by the terminal controls (as seen in the terminal control's Getter and Setter).

        // as mentioned in the other .cs file, the terminal stuff are only GUI.
        // if you want the values to persist over world reloads and be sent to clients you'll need to implement that yourself.
        // see: https://github.com/THDigi/SE-ModScript-Examples/wiki/Save-&-Sync-ways

        public bool Terminal_Heart_Shoot
        {
            get
            {
                MyAPIGateway.Utilities.ShowNotification("Terminal_Heart_Shoot Getter called");
                return shoot;             
            }
            set
            {
                shoot = value;
                MyAPIGateway.Utilities.ShowNotification("Terminal_Heart_Shoot Getter called");
                MyAPIGateway.Utilities.ShowNotification("Terminal_Heart_Shoot" + value);

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
                    Settings.CringeSetting = loadedSettings.CringeSetting;
                    Settings.BasedSetting = loadedSettings.BasedSetting;
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

                SorterWep.Storage.SetValue(HeartSettingsGUID, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings)));
            }
            catch (Exception e)
            {
                Log.Error($"Error saving settings!\n{e}");
            }
        }

        void SettingsChanged()
        {
            if (shoot == false)
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

                    //Mod.CachedPacketSettings.Send(SorterWep.EntityId, Settings);
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
                SaveSettings();
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