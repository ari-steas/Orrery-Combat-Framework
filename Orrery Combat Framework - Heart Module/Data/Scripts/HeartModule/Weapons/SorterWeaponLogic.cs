using Heart_Module.Data.Scripts.HeartModule;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.ResourceSystem;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.Game;
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

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding
{
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "TestWeapon")]
    public partial class SorterWeaponLogic : MyGameLogicComponent
    {
        internal IMyConveyorSorter SorterWep;
        internal WeaponDefinitionBase Definition;
        public readonly Guid HeartSettingsGUID = new Guid("06edc546-3e42-41f3-bc72-1d640035fbf2");
        public const int HeartSettingsUpdateCount = 60 * 1 / 10;

        public MySync<bool, SyncDirection.BothWays> ShootState; //temporary (lmao) magic bullshit in place of actual packet sending
        //insert ammo loaded state here (how the hell are we gonna do that)
        public MySync<long, SyncDirection.BothWays> AmmoLoadedState = null;          //dang this mysync thing is pretty cool it will surely not bite me in the ass when I need over 32 entries      
        public MySync<long, SyncDirection.BothWays> ControlTypeState = null;
        public MySync<bool, SyncDirection.BothWays> HudBarrelIndicatorState = null;

        public readonly Heart_Settings Settings = new Heart_Settings();

        public WeaponLogic_Magazines Magazines;

        public Dictionary<string, IMyModelDummy> MuzzleDummies { get; set; } = new Dictionary<string, IMyModelDummy>();
        public SubpartManager SubpartManager = new SubpartManager();
        public MatrixD MuzzleMatrix { get; internal set; } = MatrixD.Identity;
        public bool HasLoS = false;
        public readonly uint Id = uint.MaxValue;
        private WeaponResourceSystem _resourceSystem;

        public SorterWeaponLogic(IMyConveyorSorter sorterWeapon, WeaponDefinitionBase definition, uint id)
        {
            if (definition == null)
                return;

            sorterWeapon.GameLogic = this;
            Init(sorterWeapon.GetObjectBuilder());
            this.Definition = definition;

            // Provide a function to get the inventory
            Func<IMyInventory> getInventoryFunc = () => sorterWeapon.GetInventory();

            // You need to provide the missing arguments for WeaponLogic_Magazines constructor here
            Magazines = new WeaponLogic_Magazines(definition.Loading, definition.Audio, getInventoryFunc, Terminal_Heart_AmmoComboBox);

            // Initialize the WeaponResourceSystem
            _resourceSystem = new WeaponResourceSystem(definition, this);

            Id = id;
        }



        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        #region Event Handlers


        #endregion

        public override void UpdateOnceBeforeFrame()
        {
            SorterWep = (IMyConveyorSorter)Entity;

            if (SorterWep.CubeGrid?.Physics == null)
                return; // ignore ghost/projected grids

            // the bonus part, enforcing it to stay a specific value.
            //if (MyAPIGateway.Multiplayer.IsServer) // serverside only to avoid network spam
            //{
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            //}

            if (Definition.Assignments.HasMuzzleSubpart) // Get muzzle dummies
                ((IMyEntity)SubpartManager.RecursiveGetSubpart(SorterWep, Definition.Assignments.MuzzleSubpart))?.Model?.GetDummies(MuzzleDummies);
            else
                SorterWep.Model.GetDummies(MuzzleDummies); // From base model if muzzle subpart is not set

            SorterWep.SlimBlock.BlockGeneralDamageModifier = Definition.Assignments.DurabilityModifier;
            SorterWep.ResourceSink.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, Definition.Hardpoint.IdlePower);
            //SorterWep.ResourceSink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, Definition.Hardpoint.IdlePower); // TODO: Set max power to include projectiles and RoF

            LoadSettings();

            // Implement weapon UI defaults here

            SaveSettings();
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (MarkedForClose || Id == uint.MaxValue)
                    return;

                MuzzleMatrix = CalcMuzzleMatrix(0); // Set stored MuzzleMatrix
                Magazines.UpdateReload();
                HasLoS = HasLineOfSight();

                if (!SorterWep.IsWorking) // Don't try shoot if the turret is disabled
                    return;
                TryShoot();
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(SorterWeaponLogic));
            }
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

            List<Vector3I> intersects = new List<Vector3I>();
            SorterWep.CubeGrid.RayCastCells(MuzzleMatrix.Translation, MuzzleMatrix.Translation + MuzzleMatrix.Forward * GridCheckRange, intersects);

            foreach (var intersect in intersects)
                if (SorterWep.CubeGrid.CubeExists(intersect) && SorterWep.CubeGrid.GetCubeBlock(intersect) != SorterWep.SlimBlock)
                    return false;
            return true;
        }

        float lastShoot = 0;
        internal bool AutoShoot = false;
        int nextBarrel = 0; // For alternate firing
        public float delayCounter = 0f;
        private Random random = new Random();

        public virtual void TryShoot()
        {
            float modifiedRateOfFire = Definition.Loading.RateOfFire;

            // Only apply variance if RateOfFireVariance is not zero
            if (Definition.Loading.RateOfFireVariance != 0)
            {
                modifiedRateOfFire += (float)((random.NextDouble() * 2 - 1) * Definition.Loading.RateOfFireVariance);
            }

            if (lastShoot < 60)
                lastShoot += modifiedRateOfFire; // Use the modified rate of fire

            // Manage fire delay. If there is an easier way to do this, TODO implement
            if ((ShootState.Value || AutoShoot) && Magazines.IsLoaded && delayCounter > 0)
            {
                if (delayCounter == Definition.Loading.DelayUntilFire && !string.IsNullOrEmpty(Definition.Audio.ShootSound))
                    MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(Definition.Audio.PreShootSound, SorterWep.GetPosition());
                delayCounter -= 1 / 60f;
            }
            else if (!((ShootState.Value || AutoShoot) && Magazines.IsLoaded) && delayCounter <= 0 && Definition.Loading.DelayUntilFire > 0) // Check for the initial delay only if not already applied
            {
                delayCounter = Definition.Loading.DelayUntilFire;
            }

            if ((ShootState.Value || AutoShoot) &&          // Is allowed to shoot
                Magazines.IsLoaded &&                       // Is mag loaded
                lastShoot >= 60 &&                          // Fire rate is ready
                delayCounter <= 0 &&
                HasLoS)                                   // Has line of sight
            {
                if (Magazines.SelectedAmmo == -1)
                {
                    SoftHandle.RaiseSyncException($"Invalid ammo type on weapon! Subtype: {SorterWep.BlockDefinition.SubtypeId} | AmmoId: {Magazines.SelectedAmmo}");
                    return;
                }

                // Check if the weapon has a resource system and there are enough resources for at least one shot
                if (_resourceSystem != null && _resourceSystem.CanShoot())
                {
                    // Retrieve the AccuracyVarianceMultiplier for the selected ammo
                    float accuracyVarianceMultiplier = ProjectileDefinitionManager.GetDefinition(Magazines.SelectedAmmo).PhysicalProjectile.AccuracyVarianceMultiplier;
                    // Calculate the effective inaccuracy by applying the multiplier, default to 1 if multiplier is 0 to avoid change
                    float effectiveInaccuracy = Definition.Hardpoint.ShotInaccuracy * (accuracyVarianceMultiplier != 0 ? accuracyVarianceMultiplier : 1);

                    while (lastShoot >= 60 && Magazines.ShotsInMag > 0) // Allows for firerates higher than 60 rps
                    {
                        ProjectileDefinitionBase ammoDef = ProjectileDefinitionManager.GetDefinition(Magazines.SelectedAmmo);
                        for (int i = 0; i < Definition.Loading.BarrelsPerShot; i++)
                        {
                            nextBarrel++;
                            nextBarrel %= Definition.Assignments.Muzzles.Length;

                            MatrixD muzzleMatrix = CalcMuzzleMatrix(nextBarrel);
                            Vector3D muzzlePos = muzzleMatrix.Translation;

                            for (int j = 0; j < Definition.Loading.ProjectilesPerBarrel; j++)
                            {
                                if (MyAPIGateway.Session.IsServer)
                                {
                                    SorterWep.CubeGrid.Physics?.ApplyImpulse(muzzleMatrix.Backward * ammoDef.Ungrouped.Recoil, muzzleMatrix.Translation);
                                    // Use the effectiveInaccuracy instead of the original ShotInaccuracy
                                    // Don't sync hitscan projectiles!
                                    Projectile newProjectile = ProjectileManager.I.AddProjectile(Magazines.SelectedAmmo, muzzlePos, RandomCone(muzzleMatrix.Forward, effectiveInaccuracy), SorterWep, !ammoDef.PhysicalProjectile.IsHitscan);

                                    if (newProjectile == null) // Emergency failsafe
                                        return;

                                    if (newProjectile.Guidance != null) // Assign target for self-guided projectiles
                                    {
                                        if (this is SorterTurretLogic)
                                            newProjectile.Guidance.SetTarget(((SorterTurretLogic)this).TargetEntity);
                                        else
                                            newProjectile.Guidance.SetTarget(WeaponManagerAi.I.GetTargeting(SorterWep.CubeGrid)?.PrimaryGridTarget);
                                    }
                                }
                                else
                                {
                                    if (ammoDef.PhysicalProjectile.IsHitscan)
                                        DrawHitscanBeam(ammoDef);
                                }
                            }

                            lastShoot -= 60f;

                            // Not ideal (what if fire rate is insane?) but I don't care tbh
                            if (!string.IsNullOrEmpty(Definition.Audio.ShootSound))
                                MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(Definition.Audio.ShootSound, muzzlePos);
                            MuzzleFlash();

                            Magazines.UseShot(MuzzleMatrix.Translation);

                            if (lastShoot < 60)
                                break;
                        }
                    }

                    // Consume resources after shooting
                    _resourceSystem.ConsumeResources();
                }
            }
        }

        /// <summary>
        /// Fakes a hitscan beam, to lower network load.
        /// </summary>
        /// <param name="beam"></param>
        private void DrawHitscanBeam(ProjectileDefinitionBase beam)
        {
            List<IHitInfo> intersects = new List<IHitInfo>();
            Vector3D pos = MuzzleMatrix.Translation;
            Vector3D end = MuzzleMatrix.Translation + MuzzleMatrix.Forward * beam.PhysicalProjectile.MaxTrajectory;
            MyAPIGateway.Physics.CastRay(pos, end, intersects);

            if (intersects.Count > 0)
            {
                Vector3D hitPos = intersects[0].Position;
                GlobalEffects.AddLine(pos, hitPos, beam.Visual.TrailFadeTime, beam.Visual.TrailWidth, beam.Visual.TrailColor, beam.Visual.TrailTexture);

                MatrixD matrix = MatrixD.CreateWorld(hitPos, (Vector3D)intersects[0].Normal, Vector3D.CalculatePerpendicularVector(intersects[0].Normal));
                MyParticleEffect hitEffect;
                if (MyParticlesManager.TryCreateParticleEffect(beam.Visual.ImpactParticle, ref matrix, ref hitPos, uint.MaxValue, out hitEffect))
                {
                    //MyAPIGateway.Utilities.ShowNotification("Spawned particle at " + hitEffect.WorldMatrix.Translation);
                    //hitEffect.Velocity = av.Hit.HitVelocity;

                    if (hitEffect.Loop)
                        hitEffect.Stop();
                }
            }
            else
            {
                GlobalEffects.AddLine(pos, end, beam.Visual.TrailFadeTime, beam.Visual.TrailWidth, beam.Visual.TrailColor, beam.Visual.TrailTexture);
            }
        }

        public void MuzzleFlash(bool increment = false) // GROSS AND UGLY AND STUPID
        {
            if (Definition.Visuals.HasShootParticle && !HeartData.I.DegradedMode)
            {
                MatrixD localMuzzleMatrix = CalcMuzzleMatrix(nextBarrel, true);
                MatrixD muzzleMatrix = CalcMuzzleMatrix(nextBarrel);
                Vector3D muzzlePos = muzzleMatrix.Translation;

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

        public virtual MatrixD CalcMuzzleMatrix(int id, bool local = false)
        {
            if (Definition.Assignments.Muzzles.Length == 0 || !MuzzleDummies.ContainsKey(Definition.Assignments.Muzzles[id]))
                return SorterWep.WorldMatrix;

            MatrixD dummyMatrix = MuzzleDummies[Definition.Assignments.Muzzles[id]].Matrix; // Dummy's local matrix
            if (local)
                return dummyMatrix;

            MatrixD worldMatrix = SorterWep.WorldMatrix; // Block's world matrix

            // Combine the matrices by multiplying them to get the transformation of the dummy in world space

            return dummyMatrix * worldMatrix;

            // Now combinedMatrix.Translation is the muzzle position in world coordinates,
            // and combinedMatrix.Forward is the forward direction in world coordinates.
        }

        public void SetAmmo(int AmmoId)
        {
            Magazines.SelectedAmmo = AmmoId;
            Settings.AmmoLoadedState = Magazines.SelectedAmmo;

            Magazines.EmptyMagazines();
        }

        public void SetAmmoByIdx(int AmmoIdx)
        {
            if (AmmoIdx < 0 || AmmoIdx >= Definition.Loading.Ammos.Length)
                return;

            Magazines.AmmoIndex = AmmoIdx;
            Settings.AmmoLoadedState = Magazines.SelectedAmmo;

            Magazines.EmptyMagazines();
        }

        #region Terminal controls

        public bool Terminal_Heart_MouseShoot
        {
            get
            {
                return Settings.MouseShootState;
            }

            set
            {
                Settings.MouseShootState = value;
            }
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

        public int Terminal_Heart_AmmoComboBox
        {
            get
            {
                return Settings.AmmoLoadedState;
            }

            set
            {
                SetAmmoByIdx(value);

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
            if (forward)
                Magazines.AmmoIndex = (Magazines.AmmoIndex + 1) % Definition.Loading.Ammos.Length;
            else
                Magazines.AmmoIndex = (Magazines.AmmoIndex - 1 + Definition.Loading.Ammos.Length) % Definition.Loading.Ammos.Length;

            Settings.AmmoLoadedState = Magazines.AmmoIndex;
            Magazines.EmptyMagazines();

            Terminal_Heart_AmmoComboBox = Magazines.AmmoIndex;
        }

        public bool Terminal_Heart_ToggleHUDBarrelIndicator
        {
            get
            {
                return Settings.HudBarrelIndicatorState;
            }

            set
            {
                Settings.HudBarrelIndicatorState = value;
                HudBarrelIndicatorState.Value = value;
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

        internal virtual void LoadDefaultSettings()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            Terminal_Heart_Shoot = false;
            Terminal_Heart_AmmoComboBox = 0;
            Terminal_Heart_ToggleHUDBarrelIndicator = false;

        }

        internal virtual bool LoadSettings()
        {
            if (SorterWep.Storage == null)
            {
                LoadDefaultSettings();
                return false;
            }


            string rawData;
            if (!SorterWep.Storage.TryGetValue(HeartSettingsGUID, out rawData))
            {
                LoadDefaultSettings();
                return false;
            }

            try
            {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<Heart_Settings>(Convert.FromBase64String(rawData));

                if (loadedSettings != null)
                {
                    Settings.ShootState = loadedSettings.ShootState;
                    ShootState.Value = Settings.ShootState;

                    //insert ammo selection state here

                    Settings.AmmoLoadedState = loadedSettings.AmmoLoadedState;
                    AmmoLoadedState.Value = Settings.AmmoLoadedState;
                    Magazines.AmmoIndex = Array.IndexOf(Definition.Loading.Ammos, ProjectileDefinitionManager.GetDefinition(Settings.AmmoLoadedState).Name);

                    Settings.ControlTypeState = loadedSettings.ControlTypeState;
                    ControlTypeState.Value = Settings.ControlTypeState;

                    Settings.HudBarrelIndicatorState = loadedSettings.HudBarrelIndicatorState;
                    HudBarrelIndicatorState.Value = Settings.HudBarrelIndicatorState;

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
                //MyAPIGateway.Utilities.ShowNotification("AAAHH I'M SERIALIZING AAAHHHHH", 2000, "Red");
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
    }
}