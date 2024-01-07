using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRageMath;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Hiding;

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "TestWeapon")]
    public class SorterWeaponLogic : MyGameLogicComponent
    {
        IMyConveyorSorter SorterWep;
        public readonly Guid HeartSettingsGUID = new Guid("06edc546-3e42-41f3-bc72-1d640035fbf2");
        public const int HeartSettingsUpdateCount = 60 * 1 / 10;
        int SyncCountdown;

        public MySync<bool, SyncDirection.BothWays> ShootState; //temporary (lmao) magic bullshit in place of an actual

        public readonly Heart_Settings Settings = new Heart_Settings();

        //the state of shoot
        bool shoot = false;

        public Dictionary<string, IMyModelDummy> modeldummy { get; set; } = new Dictionary<string, IMyModelDummy>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            ShootState.ValueChanged += OnShootStateChanged; // Attach the handler
        }

        private void OnShootStateChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            // Accessing the boolean value using .Value property
            bool newValue = obj.Value;
            MyAPIGateway.Utilities.ShowNotification($"Shoot State changed to: {newValue}", 2000, "White");
        }


        public override void UpdateOnceBeforeFrame()
        {
            HideSorterControls.DoOnce();
            SorterWeaponTerminalControls.DoOnce(ModContext);

            SorterWep = (IMyConveyorSorter)Entity;

            if (SorterWep.CubeGrid?.Physics == null)
                return; // ignore ghost/projected grids

            // the bonus part, enforcing it to stay a specific value.
            if (MyAPIGateway.Multiplayer.IsServer) // serverside only to avoid network spam
            {
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }

            SorterWep.Model.GetDummies(modeldummy);
            MyAPIGateway.Utilities.ShowNotification($"Model Dummies: {modeldummy.Count}", 2000, "White");

        }

        float fireRate = 15; // per-second
        float lastShoot = 0;
        public override void UpdateAfterSimulation()
        {
            if (lastShoot < 60)
                lastShoot += fireRate;

            if (ShootState.Value && lastShoot >= 60)
            {

                MatrixD matrix = SorterWep.WorldMatrix + (MatrixD)modeldummy["muzzle01"].Matrix;
                ProjectileManager.I.AddProjectile(new Projectile(0, matrix.Translation, matrix.Forward, SorterWep));
                lastShoot -= 60;



            }
        }

        public float Terminal_ExampleFloat { get; set; }

        public bool Terminal_Heart_Shoot
        {
            get
            {

                return Settings.ShootState;
            }

            set
            {
                Settings.ShootState = true;

                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            }
        }

        public override void Close()
        {
            base.Close();
            // Unsubscribe from the event when the component is closed
            if (ShootState != null)
                ShootState.ValueChanged -= OnShootStateChanged;
        }
    }
}
