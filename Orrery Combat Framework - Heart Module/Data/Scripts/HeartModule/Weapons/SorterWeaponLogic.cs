﻿using Heart_Module.Data.Scripts.HeartModule;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
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
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "TestWeapon")]
    public partial class SorterWeaponLogic : MyGameLogicComponent
    {
        internal IMyConveyorSorter SorterWep;
        internal SerializableWeaponDefinition Definition;
        public readonly Guid HeartSettingsGUID = new Guid("06edc546-3e42-41f3-bc72-1d640035fbf2");
        public const int HeartSettingsUpdateCount = 60 * 1 / 10;
        int SyncCountdown;

        public MySync<bool, SyncDirection.BothWays> ShootState; //temporary (lmao) magic bullshit in place of actual packet sending
        //insert ammo loaded state here (how the hell are we gonna do that)
        public MySync<float, SyncDirection.BothWays> AiRange;
        public MySync<bool, SyncDirection.BothWays> TargetGridsState;
        public MySync<bool, SyncDirection.BothWays> TargetProjectilesState;
        public MySync<bool, SyncDirection.BothWays> TargetCharactersState;
        public MySync<bool, SyncDirection.BothWays> TargetLargeGridsState;
        public MySync<bool, SyncDirection.BothWays> TargetSmallGridsState;
        public MySync<bool, SyncDirection.BothWays> TargetFriendliesState;
        public MySync<bool, SyncDirection.BothWays> TargetNeutralsState;
        public MySync<bool, SyncDirection.BothWays> TargetEnemiesState;
        public MySync<bool, SyncDirection.BothWays> TargetUnownedState;
        public MySync<long, SyncDirection.BothWays> AmmoLoadedState;          //dang this mysync thing is pretty cool it will surely not bite me in the ass when I need over 32 entries      
        public MySync<long, SyncDirection.BothWays> ControlTypeState;

        public readonly Heart_Settings Settings = new Heart_Settings();

        public WeaponLogic_Magazines Magazines;

        //the state of shoot
        bool shoot = false;

        public Dictionary<string, IMyModelDummy> MuzzleDummies { get; set; } = new Dictionary<string, IMyModelDummy>();
        public SubpartManager SubpartManager = new SubpartManager();
        public MatrixD MuzzleMatrix { get; internal set; } = MatrixD.Identity;
        public readonly uint Id;

        public SorterWeaponLogic(IMyConveyorSorter sorterWeapon, SerializableWeaponDefinition definition, uint id)
        {
            sorterWeapon.GameLogic = this;
            Init(sorterWeapon.GetObjectBuilder());
            this.Definition = definition;
            Magazines = new WeaponLogic_Magazines(definition.Loading);
            Id = id;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            ShootState.ValueChanged += OnShootStateChanged;
            AiRange.ValueChanged += OnAiRangeChanged;
            TargetGridsState.ValueChanged += OnTargetGridsStateChanged;
            TargetProjectilesState.ValueChanged += OnTargetProjectilesStateChanged;
            TargetCharactersState.ValueChanged += OnTargetCharactersStateChanged;
            TargetLargeGridsState.ValueChanged += OnTargetLargeGridsStateChanged;
            TargetSmallGridsState.ValueChanged += OnTargetSmallGridsStateChanged;
            TargetFriendliesState.ValueChanged += OnTargetFriendliesStateChanged;
            TargetNeutralsState.ValueChanged += OnTargetNeutralsStateChanged;
            TargetEnemiesState.ValueChanged += OnTargetEnemiesStateChanged;
            TargetUnownedState.ValueChanged += OnTargetUnownedStateChanged;
            ControlTypeState.ValueChanged += OnControlTypeStateChanged;
        }

        #region Event Handlers

        private void OnShootStateChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            // Accessing the boolean value using .Value property
            bool newValue = obj.Value;
            MyAPIGateway.Utilities.ShowNotification($"Shoot State changed to: {newValue}", 2000, "White");
        }

        private void OnAmmoLoadedStateChanged(MySync<long, SyncDirection.BothWays> obj)
        {
            // Accessing the ammo type value using .Value property
            long newAmmoType = obj.Value;

            // Implement logic based on the new ammo type
            // For example, changing the characteristics of the shots fired, 
            // or reloading a different type of ammunition, etc.

            MyAPIGateway.Utilities.ShowNotification($"Ammo Type changed to: {newAmmoType}", 2000, "White");
        }

        private void OnControlTypeStateChanged(MySync<long, SyncDirection.BothWays> obj)
        {
            // Accessing the ammo type value using .Value property
            long newControlType = obj.Value;

            // Implement logic based on the new ammo type
            // For example, changing the characteristics of the shots fired, 
            // or reloading a different type of ammunition, etc.

            MyAPIGateway.Utilities.ShowNotification($"Control Type changed to: {newControlType}", 2000, "White");
        }


        private void OnAiRangeChanged(MySync<float, SyncDirection.BothWays> obj)
        {
            // Accessing the float value using .Value property
            float newValue = obj.Value;
            MyAPIGateway.Utilities.ShowNotification($"AI Range changed to: {newValue}", 2000, "White");
        }

        private void OnTargetGridsStateChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            // Accessing the boolean value using .Value property
            bool newValue = obj.Value;
            MyAPIGateway.Utilities.ShowNotification($"Target Grids State changed to: {newValue}", 2000, "White");
        }

        private void OnTargetProjectilesStateChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            // Accessing the boolean value using .Value property
            bool newValue = obj.Value;
            MyAPIGateway.Utilities.ShowNotification($"Target Projectiles State changed to: {newValue}", 2000, "White");
        }

        private void OnTargetCharactersStateChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            // Accessing the boolean value using .Value property
            bool newValue = obj.Value;
            MyAPIGateway.Utilities.ShowNotification($"Target Characters State changed to: {newValue}", 2000, "White");
        }

        private void OnTargetLargeGridsStateChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            // Accessing the boolean value using .Value property
            bool newValue = obj.Value;
            MyAPIGateway.Utilities.ShowNotification($"Target LargeGrids State changed to: {newValue}", 2000, "White");
        }

        private void OnTargetSmallGridsStateChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            // Accessing the boolean value using .Value property
            bool newValue = obj.Value;
            MyAPIGateway.Utilities.ShowNotification($"Target SmallGrids State changed to: {newValue}", 2000, "White");
        }

        private void OnTargetFriendliesStateChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            // Accessing the boolean value using .Value property
            bool newValue = obj.Value;
            MyAPIGateway.Utilities.ShowNotification($"Target Friendlies State changed to: {newValue}", 2000, "White");
        }

        private void OnTargetNeutralsStateChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            // Accessing the boolean value using .Value property
            bool newValue = obj.Value;
            MyAPIGateway.Utilities.ShowNotification($"Target Neutrals State changed to: {newValue}", 2000, "White");
        }

        private void OnTargetEnemiesStateChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            // Accessing the boolean value using .Value property
            bool newValue = obj.Value;
            MyAPIGateway.Utilities.ShowNotification($"Target Enemies State changed to: {newValue}", 2000, "White");
        }

        private void OnTargetUnownedStateChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            // Accessing the boolean value using .Value property
            bool newValue = obj.Value;
            MyAPIGateway.Utilities.ShowNotification($"Target Unowned State changed to: {newValue}", 2000, "White");
        }

        #endregion

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

            if (Definition.Assignments.HasMuzzleSubpart) // Get muzzle dummies
                ((IMyEntity)SubpartManager.RecursiveGetSubpart(SorterWep, Definition.Assignments.MuzzleSubpart))?.Model?.GetDummies(MuzzleDummies);
            else
                SorterWep.Model.GetDummies(MuzzleDummies); // From base model if muzzle subpart is not set

            MyAPIGateway.Utilities.ShowNotification($"Model Dummies: {MuzzleDummies.Count}", 2000, "White");

            SorterWep.SlimBlock.BlockGeneralDamageModifier = Definition.Assignments.DurabilityModifier;
            SorterWep.ResourceSink.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, Definition.Hardpoint.IdlePower);
            //SorterWep.ResourceSink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, Definition.Hardpoint.IdlePower); // TODO: Set max power to include projectiles and RoF

            LoadSettings();

            // Implement weapon UI defaults here

            SaveSettings();
        }

        public override void UpdateAfterSimulation()
        {
            MuzzleMatrix = CalcMuzzleMatrix(0); // Set stored MuzzleMatrix
            Magazines.UpdateReload();

            if (!SorterWep.IsWorking) // Don't try shoot if the turret is disabled
                return;
            TryShoot();
        }

        const float GridCheckRange = 200;
        /// <summary>
        /// Checks if the turret would intersect the grid.
        /// </summary>
        /// <returns></returns>
        private bool HasLineOfSight()
        {
            if (!Definition.Hardpoint.LineOfSightCheck) // Ignore if LoS check is disabled
                return true;

            List<IHitInfo> intersects = new List<IHitInfo>();
            MyAPIGateway.Physics.CastRay(MuzzleMatrix.Translation, MuzzleMatrix.Translation + MuzzleMatrix.Forward * GridCheckRange, intersects);
            foreach (var intersect in intersects)
                if (intersect.HitEntity.EntityId == SorterWep.CubeGrid.EntityId)
                    return false;
            return true;
        }

        float lastShoot = 0;
        internal bool AutoShoot = false;
        int nextBarrel = 0; // For alternate firing
        public virtual void TryShoot()
        {
            if (lastShoot < 60)
                lastShoot += Definition.Loading.RateOfFire;

            if ((ShootState.Value || AutoShoot) && Magazines.IsLoaded && lastShoot >= 60 && HasLineOfSight())
            {
                int barrelIndex = 0;
                for (int i = nextBarrel; i < Definition.Loading.BarrelsPerShot + nextBarrel; i++)
                {
                    barrelIndex = i % Definition.Assignments.Muzzles.Length;

                    MatrixD muzzleMatrix = CalcMuzzleMatrix(barrelIndex);
                    Vector3D muzzlePos = muzzleMatrix.Translation;

                    for (int j = 0; j < Definition.Loading.ProjectilesPerBarrel; j++)
                        ProjectileManager.I.AddProjectile(0, muzzlePos, RandomCone(muzzleMatrix.Forward, Definition.Hardpoint.ShotInaccuracy), SorterWep);
                    lastShoot -= 60f;

                    if (Definition.Visuals.HasShootParticle)
                    {
                        MatrixD localMuzzleMatrix = CalcMuzzleMatrix(barrelIndex, true);

                        MyParticleEffect hitEffect;
                        if (MyParticlesManager.TryCreateParticleEffect(Definition.Visuals.ShootParticle, ref localMuzzleMatrix, ref muzzlePos, SorterWep.Render.GetRenderObjectID(), out hitEffect))
                        {
                            //MyAPIGateway.Utilities.ShowNotification("Spawned particle at " + hitEffect.WorldMatrix.Translation);
                            //hitEffect.Velocity = SorterWep.CubeGrid.LinearVelocity;

                            if (hitEffect.Loop)
                                hitEffect.Stop();
                        }
                    }
                }
                nextBarrel = barrelIndex + 1;
                Magazines.UseShot();
            }
        }



        public virtual MatrixD CalcMuzzleMatrix(int id, bool local = false)
        {
            MatrixD dummyMatrix = MuzzleDummies[Definition.Assignments.Muzzles[id]].Matrix; // Dummy's local matrix
            if (local)
                return dummyMatrix;

            MatrixD worldMatrix = SorterWep.WorldMatrix; // Block's world matrix

            // Combine the matrices by multiplying them to get the transformation of the dummy in world space

            return dummyMatrix * worldMatrix;

            // Now combinedMatrix.Translation is the muzzle position in world coordinates,
            // and combinedMatrix.Forward is the forward direction in world coordinates.
        }

        #region Terminal controls

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

        // In SorterWeaponLogic class, you should implement IncreaseAIRange and DecreaseAIRange methods
        public void IncreaseAIRange()
        {
            // Increase AI Range within limits
            Terminal_Heart_Range_Slider = Math.Min(Terminal_Heart_Range_Slider + 100, 1000);
        }

        public void DecreaseAIRange()
        {
            // Decrease AI Range within limits
            Terminal_Heart_Range_Slider = Math.Max(Terminal_Heart_Range_Slider - 100, 0);
        }

        public long Terminal_Heart_AmmoComboBox
        {
            get
            {
                return Settings.AmmoLoadedState;
            }

            set
            {
                Settings.AmmoLoadedState = value;
                if (AmmoLoadedState != null)
                {
                    AmmoLoadedState.Value = value;
                }
                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
        }

        public void CycleAmmoType(bool forward)
        {
            // Assuming you have a predefined list of ammo types
            long[] ammoTypes = { 0, 1, 2 }; // Replace with actual ammo type keys
            int currentIndex = Array.IndexOf(ammoTypes, Terminal_Heart_AmmoComboBox);

            if (forward)
            {
                currentIndex = (currentIndex + 1) % ammoTypes.Length;
            }
            else
            {
                currentIndex = (currentIndex - 1 + ammoTypes.Length) % ammoTypes.Length;
            }

            Terminal_Heart_AmmoComboBox = ammoTypes[currentIndex];
        }

        public long Terminal_ControlType_ComboBox
        {
            get
            {
                return Settings.ControlTypeState;
            }

            set
            {
                Settings.ControlTypeState = value;
                if (ControlTypeState != null)
                {
                    ControlTypeState.Value = value;
                }
                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
        }

        public void CycleControlType(bool controltype)
        {
            // Assuming you have a predefined list of ammo types
            long[] controlTypes = { 0, 1, 2 }; // Replace with actual ammo type keys
            int currentIndex = Array.IndexOf(controlTypes, Terminal_ControlType_ComboBox);

            if (controltype)
            {
                currentIndex = (currentIndex + 1) % controlTypes.Length;
            }
            else
            {
                currentIndex = (currentIndex - 1 + controlTypes.Length) % controlTypes.Length;
            }

            Terminal_ControlType_ComboBox = controlTypes[currentIndex];
        }

        public float Terminal_Heart_Range_Slider
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

        public bool Terminal_Heart_TargetGrids
        {
            get
            {

                return Settings.TargetGridsState;
            }

            set
            {
                Settings.TargetGridsState = value;
                TargetGridsState.Value = value;
                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            }
        }

        public bool Terminal_Heart_TargetProjectiles
        {
            get
            {

                return Settings.TargetProjectilesState;
            }

            set
            {
                Settings.TargetProjectilesState = value;
                TargetProjectilesState.Value = value;
                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            }
        }

        public bool Terminal_Heart_TargetCharacters
        {
            get
            {

                return Settings.TargetCharactersState;
            }

            set
            {
                Settings.TargetCharactersState = value;
                TargetCharactersState.Value = value;
                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            }
        }

        public bool Terminal_Heart_TargetLargeGrids
        {
            get
            {

                return Settings.TargetLargeGridsState;
            }

            set
            {
                Settings.TargetLargeGridsState = value;
                TargetLargeGridsState.Value = value;
                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            }
        }

        public bool Terminal_Heart_TargetSmallGrids
        {
            get
            {

                return Settings.TargetSmallGridsState;
            }

            set
            {
                Settings.TargetSmallGridsState = value;
                TargetSmallGridsState.Value = value;
                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            }
        }

        public bool Terminal_Heart_TargetFriendlies
        {
            get
            {

                return Settings.TargetFriendliesState;
            }

            set
            {
                Settings.TargetFriendliesState = value;
                TargetFriendliesState.Value = value;
                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            }
        }

        public bool Terminal_Heart_TargetNeutrals
        {
            get
            {

                return Settings.TargetNeutralsState;
            }

            set
            {
                Settings.TargetNeutralsState = value;
                TargetNeutralsState.Value = value;
                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            }
        }

        public bool Terminal_Heart_TargetEnemies
        {
            get
            {

                return Settings.TargetEnemiesState;
            }

            set
            {
                Settings.TargetEnemiesState = value;
                TargetEnemiesState.Value = value;
                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            }
        }

        public bool Terminal_Heart_TargetUnowned
        {
            get
            {

                return Settings.TargetUnownedState;
            }

            set
            {
                Settings.TargetUnownedState = value;
                TargetUnownedState.Value = value;
                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            }
        }

        #endregion

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

                    //insert ammo selection state here

                    // Set the AI Range from loaded settings
                    Settings.AiRange = loadedSettings.AiRange;
                    AiRange.Value = Settings.AiRange;

                    Settings.AmmoLoadedState = loadedSettings.AmmoLoadedState;
                    AmmoLoadedState.Value = Settings.AmmoLoadedState;

                    // Set the TargetGrids state from loaded settings
                    Settings.TargetGridsState = loadedSettings.TargetGridsState;
                    TargetGridsState.Value = Settings.TargetGridsState;

                    Settings.TargetProjectilesState = loadedSettings.TargetProjectilesState;
                    TargetProjectilesState.Value = Settings.TargetProjectilesState;

                    Settings.TargetCharactersState = loadedSettings.TargetCharactersState;
                    TargetCharactersState.Value = Settings.TargetCharactersState;

                    Settings.TargetLargeGridsState = loadedSettings.TargetLargeGridsState;
                    TargetLargeGridsState.Value = Settings.TargetLargeGridsState;

                    Settings.TargetSmallGridsState = loadedSettings.TargetSmallGridsState;
                    TargetSmallGridsState.Value = Settings.TargetSmallGridsState;

                    Settings.TargetFriendliesState = loadedSettings.TargetFriendliesState;
                    TargetFriendliesState.Value = Settings.TargetFriendliesState;

                    Settings.TargetNeutralsState = loadedSettings.TargetNeutralsState;
                    TargetNeutralsState.Value = Settings.TargetNeutralsState;

                    Settings.TargetEnemiesState = loadedSettings.TargetEnemiesState;
                    TargetEnemiesState.Value = Settings.TargetEnemiesState;

                    Settings.TargetUnownedState = loadedSettings.TargetUnownedState;
                    TargetUnownedState.Value = Settings.TargetUnownedState;

                    Settings.ControlTypeState = loadedSettings.ControlTypeState;
                    ControlTypeState.Value = Settings.ControlTypeState;

                    return true;
                }
            }
            catch (Exception e)
            {
                // Log the exception
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

            if (AiRange != null)
                AiRange.ValueChanged -= OnAiRangeChanged;

            if (TargetGridsState != null)
                TargetGridsState.ValueChanged -= OnTargetGridsStateChanged;

            if (TargetProjectilesState != null)
                TargetProjectilesState.ValueChanged -= OnTargetProjectilesStateChanged;

            if (TargetCharactersState != null)
                TargetCharactersState.ValueChanged -= OnTargetCharactersStateChanged;

            if (TargetLargeGridsState != null)
                TargetLargeGridsState.ValueChanged -= OnTargetLargeGridsStateChanged;

            if (TargetSmallGridsState != null)
                TargetSmallGridsState.ValueChanged -= OnTargetSmallGridsStateChanged;

            if (TargetFriendliesState != null)
                TargetFriendliesState.ValueChanged -= OnTargetFriendliesStateChanged;

            if (TargetNeutralsState != null)
                TargetNeutralsState.ValueChanged -= OnTargetNeutralsStateChanged;

            if (TargetEnemiesState != null)
                TargetEnemiesState.ValueChanged -= OnTargetEnemiesStateChanged;

            if (TargetUnownedState != null)
                TargetUnownedState.ValueChanged -= OnTargetUnownedStateChanged;

            if (AmmoLoadedState != null)
                AmmoLoadedState.ValueChanged -= OnAmmoLoadedStateChanged;

            if (ControlTypeState != null)
                ControlTypeState.ValueChanged -= OnControlTypeStateChanged;
            // Add any additional cleanup logic here if necessary
        }

    }
}