using Heart_Module.Data.Scripts.HeartModule;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRage.Utils;
using VRageMath;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Hiding;
using static VRage.Game.MyObjectBuilder_BehaviorTreeDecoratorNode;

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding
{
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "TestWeapon")]
    public class SorterWeaponLogic : MyGameLogicComponent
    {
        internal IMyConveyorSorter SorterWep;
        internal SerializableWeaponDefinition Definition;
        public readonly Guid HeartSettingsGUID = new Guid("06edc546-3e42-41f3-bc72-1d640035fbf2");
        public const int HeartSettingsUpdateCount = 60 * 1 / 10;
        int SyncCountdown;

        public MySync<bool, SyncDirection.BothWays> ShootState; //temporary (lmao) magic bullshit in place of an actual
        public MySync<float, SyncDirection.BothWays> AiRange; //temporary (lmao) magic bullshit in place of an actual

        public readonly Heart_Settings Settings = new Heart_Settings();

        //the state of shoot
        bool shoot = false;

        public Dictionary<string, IMyModelDummy> modeldummy { get; set; } = new Dictionary<string, IMyModelDummy>();

        public SorterWeaponLogic(IMyConveyorSorter sorterWeapon, SerializableWeaponDefinition definition)
        {
            sorterWeapon.GameLogic = this;
            Init(sorterWeapon.GetObjectBuilder());
            this.Definition = definition;
        }

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
          
            LoadSettings();

            // Implement weapon UI defaults here

            SaveSettings();
        }

        public override void UpdateAfterSimulation()
        {
            TryShoot();
        }

        float lastShoot = 0;
        public void TryShoot()
        {
            if (lastShoot < 60)
                lastShoot += Definition.Loading.RateOfFire;

            if (ShootState.Value && lastShoot >= 60)
            {
                MatrixD muzzleMatrix = CalcMuzzleMatrix();
                Vector3D muzzlePos = muzzleMatrix.Translation;

                ProjectileManager.I.AddProjectile(0, muzzlePos, RandomCone(muzzleMatrix.Forward, Definition.Hardpoint.ShotInaccuracy), SorterWep);
                lastShoot -= 60;

                if (Definition.Visuals.HasShootParticle)
                {
                    MatrixD matrix = MatrixD.CreateTranslation(muzzlePos);
                    MyParticleEffect hitEffect;
                    if (MyParticlesManager.TryCreateParticleEffect(Definition.Visuals.ShootParticle, ref matrix, ref muzzlePos, uint.MaxValue, out hitEffect))
                    {
                        //MyAPIGateway.Utilities.ShowNotification("Spawned particle at " + hitEffect.WorldMatrix.Translation);
                        hitEffect.Velocity = SorterWep.CubeGrid.LinearVelocity;

                        if (hitEffect.Loop)
                            hitEffect.Stop();
                    }
                }
            }
        }

        public virtual MatrixD CalcMuzzleMatrix()
        {
            MatrixD worldMatrix = SorterWep.WorldMatrix; // Block's world matrix
            MatrixD dummyMatrix = modeldummy[Definition.Assignments.Muzzles[0]].Matrix; // Dummy's local matrix

            // Combine the matrices by multiplying them to get the transformation of the dummy in world space
            return dummyMatrix * worldMatrix;

            // Now combinedMatrix.Translation is the muzzle position in world coordinates,
            // and combinedMatrix.Forward is the forward direction in world coordinates.
        }



        public bool Terminal_Heart_Shoot
        {
            get
            {

                return Settings.ShootState;
            }

            set
            {
                Settings.ShootState = value;
                ShootState.Value = value;
                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            }
        }

        public float AI_Range
        {
            get
            {

                return Settings.AiRange;
            }

            set
            {
                Settings.AiRange = value;
                AiRange.Value = value;
                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            }
        }



        #region Saving


        void SaveSettings()
        {
            if (SorterWep == null)
                return; // called too soon or after it was already closed, ignore

            if (Settings == null)
                throw new NullReferenceException($"Settings == null on entId={Entity?.EntityId}; Test log 1");

            if (MyAPIGateway.Utilities == null)
                throw new NullReferenceException($"MyAPIGateway.Utilities == null; entId={Entity?.EntityId}; Test log 2");

            if (SorterWep.Storage == null)
                SorterWep.Storage = new MyModStorageComponent();

            SorterWep.Storage.SetValue(HeartSettingsGUID, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings)));

            //MyAPIGateway.Utilities.ShowNotification(SettingsBlockRange.ToString(), 1000, "Red");
        }

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
                    ShootState.Value = Settings.ShootState;
                    return true;


                }
            }
            catch (Exception e)
            {
                //should probably log this tbqh
            }

            return false;
        }

        public override bool IsSerialized()
        {
            try
            {
                SaveSettings();
                MyAPIGateway.Utilities.ShowNotification("AAAHH I'M SERIALIZING AAAHHHHH", 2000, "Red");
            }
            catch (Exception e)
            {
                //should probably log this tbqh
            }

            return base.IsSerialized();
        }

        #endregion


        internal Vector3D RandomCone(Vector3D center, double radius)
        {
            Vector3D Axis = Vector3D.CalculatePerpendicularVector(center).Rotate(center, Math.PI * 2 * HeartData.I.Random.NextDouble());

            return center.Rotate(Axis, radius * HeartData.I.Random.NextDouble());
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