using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Sync;
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

        public MySync<bool, SyncDirection.BothWays> FUCK = null; //temporary (lmao) magic bullshit in place of an actual

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

           // LoadSettings(); // artifact from chets meme
        }

        public override void UpdateAfterSimulation10()
        {

            MyAPIGateway.Utilities.ShowNotification("Syncing Settings");
            //SyncSettings();

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

                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                MyAPIGateway.Utilities.ShowNotification("Shoot State: " + Settings.ShootState.ToString());

            }
        }


        public float Terminal_ExampleFloat { get; set; }


    }
}
