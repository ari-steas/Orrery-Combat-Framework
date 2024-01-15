using Heart_Module.Data.Scripts.HeartModule.Utility;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting;
using Sandbox.ModAPI;
using System;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRageMath;
using YourName.ModName.Data.Scripts.HeartModule.Weapons;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;
using VRage.Game;
using VRage.Game.ModAPI;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "TestWeaponTurret")]
    public partial class SorterTurretLogic : SorterWeaponLogic
    {
        internal float Azimuth;
        internal float Elevation;

        /// <summary>
        /// Delta for engine ticks; 60tps
        /// </summary>
        private const float deltaTick = 1 / 60f;

        public bool IsTargetAligned { get; private set; } = false;
        public bool IsTargetInRange { get; private set; } = false;

        public Vector3D AimPoint { get; private set; } = Vector3D.MaxValue; // TODO fix, should be in targeting CS
        //private GenericKeenTargeting targeting = new GenericKeenTargeting();

        public IMyEntity TargetEntity = null;
        public Projectile TargetProjectile = null;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            Azimuth = (float)Math.PI; // defaults
            Elevation = 0;
        }

        public SorterTurretLogic(IMyConveyorSorter sorterWeapon, SerializableWeaponDefinition definition, uint id) : base(sorterWeapon, definition, id) { }

        public override void UpdateAfterSimulation()
        {
            if (!SorterWep.IsWorking) // Don't turn if the turret is disabled
                return;

            UpdateTargeting();

            base.UpdateAfterSimulation();
        }

        public void UpdateTargeting()
        {
            MuzzleMatrix = CalcMuzzleMatrix(0); // Set stored MuzzleMatrix

            if (TargetProjectile != null)
            {
                AimPoint = TargetingHelper.InterceptionPoint(
                        MuzzleMatrix.Translation,
                        SorterWep.CubeGrid.LinearVelocity,
                        TargetProjectile, 0) ?? Vector3D.MaxValue;
                UpdateTargetState(AimPoint);
            }
            else if (TargetEntity != null)
            {
                AimPoint = TargetingHelper.InterceptionPoint(
                        MuzzleMatrix.Translation,
                        SorterWep.CubeGrid.LinearVelocity,
                        TargetEntity, 0) ?? Vector3D.MaxValue;
                UpdateTargetState(AimPoint);
            }
            else
                ResetTargetingState();

            UpdateTurretSubparts(deltaTick, AimPoint);
        }

        private void ResetTargetingState()
        {
            //currentTarget = null;
            IsTargetAligned = false;
            IsTargetInRange = false;
            AutoShoot = false; // Disable automatic shooting
            AimPoint = Vector3D.MaxValue;
        }

        /// <summary>
        /// Sets state of target alignment and target range
        /// </summary>
        /// <param name="target"></param>
        /// <param name="aimPoint"></param>
        private void UpdateTargetState(Vector3D aimPoint)
        {
            double angle = Vector3D.Angle(MuzzleMatrix.Forward, (aimPoint - MuzzleMatrix.Translation).Normalized());
            IsTargetAligned = angle < Definition.Targeting.AimTolerance;

            double range = Vector3D.Distance(MuzzleMatrix.Translation, aimPoint);
            IsTargetInRange = range < Definition.Targeting.MaxTargetingRange && range > Definition.Targeting.MinTargetingRange;
        }

        public bool ShouldConsiderTarget(IMyCubeGrid targetGrid)
        {
            if (!TargetGridsState || targetGrid == null)
                return false;

            switch (targetGrid.GridSizeEnum) // Filter large/small grid
            {
                case MyCubeSize.Large:
                    if (!TargetLargeGridsState)
                        return false;
                    break;
                case MyCubeSize.Small:
                    if (!TargetSmallGridsState)
                        return false;
                    break;
            }

            if (!ShouldConsiderTarget(HeartUtils.GetRelationsBetweeenGrids(SorterWep.CubeGrid, targetGrid)))
                return false;

            Vector3D? intercept = TargetingHelper.InterceptionPoint(MuzzleMatrix.Translation, SorterWep.CubeGrid.LinearVelocity, targetGrid, CurrentAmmo); // Check if it can even hit
            return intercept != null; // All possible negatives have been filtered out
        }

        public bool ShouldConsiderTarget(IMyCharacter targetCharacter)
        {
            if (!TargetCharactersState || targetCharacter == null)
                return false;

            if (!ShouldConsiderTarget(HeartUtils.GetRelationsBetweenGridAndPlayer(SorterWep.CubeGrid, targetCharacter.ControllerInfo?.ControllingIdentityId)))
                return false;

            Vector3D? intercept = TargetingHelper.InterceptionPoint(MuzzleMatrix.Translation, SorterWep.CubeGrid.LinearVelocity, targetCharacter, CurrentAmmo); // Check if it can even hit
            return intercept != null; // All possible negatives have been filtered out
        }

        public bool ShouldConsiderTarget(Projectile targetProjectile)
        {
            if (!TargetProjectilesState || targetProjectile == null)
                return false;

            MyRelationsBetweenPlayerAndBlock relations = MyRelationsBetweenPlayerAndBlock.NoOwnership;

            IMyEntity entity = MyAPIGateway.Entities.GetEntityById(targetProjectile.Firer);
            if (entity is IMyCharacter)
                relations = HeartUtils.GetRelationsBetweenGridAndPlayer(SorterWep.CubeGrid, ((IMyCharacter)entity).ControllerInfo?.ControllingIdentityId);
            else if (entity is IMyCubeBlock)
                relations = HeartUtils.GetRelationsBetweeenGrids(SorterWep.CubeGrid, ((IMyCubeBlock)entity).CubeGrid);

            MyAPIGateway.Utilities.ShowNotification("" + relations, 1000/60);

            if (!ShouldConsiderTarget(relations))
                return false;

            Vector3D? intercept = TargetingHelper.InterceptionPoint(MuzzleMatrix.Translation, SorterWep.CubeGrid.LinearVelocity, targetProjectile, CurrentAmmo); // Check if it can even hit
            return intercept != null; // All possible negatives have been filtered out
        }

        public bool ShouldConsiderTarget(MyRelationsBetweenPlayerAndBlock relations)
        {
            switch (relations) // Filter target relations
            {
                case MyRelationsBetweenPlayerAndBlock.NoOwnership:
                    if (!TargetUnownedState)
                        return false;
                    break;
                case MyRelationsBetweenPlayerAndBlock.Owner:
                case MyRelationsBetweenPlayerAndBlock.Friends:
                case MyRelationsBetweenPlayerAndBlock.FactionShare:
                    if (!TargetFriendliesState)
                        return false;
                    break;
                case MyRelationsBetweenPlayerAndBlock.Enemies:
                    if (!TargetEnemiesState)
                        return false;
                    break;
                case MyRelationsBetweenPlayerAndBlock.Neutral:
                    if (!TargetNeutralsState)
                        return false;
                    break;
            }
            return true;
        }

        public override void TryShoot()
        {
            AutoShoot = Definition.Targeting.CanAutoShoot && IsTargetAligned && IsTargetInRange;
            base.TryShoot();
        }

        public override MatrixD CalcMuzzleMatrix(int id, bool local = false)
        {
            try
            {
                MyEntitySubpart azSubpart = SubpartManager.GetSubpart((MyEntity)SorterWep, Definition.Assignments.AzimuthSubpart);
                MyEntitySubpart evSubpart = SubpartManager.GetSubpart(azSubpart, Definition.Assignments.ElevationSubpart);

                MatrixD partMatrix = evSubpart.WorldMatrix;
                Matrix muzzleMatrix = MuzzleDummies[Definition.Assignments.Muzzles[id]].Matrix;

                if (local)
                {
                    return muzzleMatrix * evSubpart.PositionComp.LocalMatrixRef * azSubpart.PositionComp.LocalMatrixRef;
                }

                if (muzzleMatrix != null)
                    return muzzleMatrix * partMatrix;
            }
            catch { }
            return MatrixD.Identity;
        }

        public void UpdateTurretSubparts(float delta, Vector3D aimpoint)
        {
            if (!Definition.Hardpoint.ControlRotation)
                return;

            MyEntitySubpart azimuth = SubpartManager.GetSubpart((MyEntity)SorterWep, Definition.Assignments.AzimuthSubpart);
            MyEntitySubpart elevation = SubpartManager.GetSubpart(azimuth, Definition.Assignments.ElevationSubpart);

            if (aimpoint == Vector3D.MaxValue)
            {
                SubpartManager.LocalRotateSubpartAbs(azimuth, GetAzimuthMatrix(Math.PI - Definition.Hardpoint.HomeAzimuth, deltaTick));
                SubpartManager.LocalRotateSubpartAbs(elevation, GetElevationMatrix(-Definition.Hardpoint.HomeElevation, deltaTick));
                return; // Exit if interception point does not exist
            }

            Vector3D vecToTarget = aimpoint - MuzzleMatrix.Translation;
            //DebugDraw.AddLine(MuzzleMatrix.Translation, MuzzleMatrix.Translation + MuzzleMatrix.Forward * vecToTarget.Length(), Color.Blue, 0); // Muzzle line

            vecToTarget = Vector3D.Rotate(vecToTarget.Normalized(), MatrixD.Invert(SorterWep.WorldMatrix)); // Inverted because subparts are wonky. Pre-rotated.
            SubpartManager.LocalRotateSubpartAbs(azimuth, GetAzimuthMatrix(vecToTarget, delta));
            SubpartManager.LocalRotateSubpartAbs(elevation, GetElevationMatrix(vecToTarget, delta));
        }

        //float Azimuth = (float) Math.PI;
        //float Elevation = 0;

        private Matrix GetAzimuthMatrix(Vector3D targetDirection, float delta)
        {
            double desiredAzimuth = Math.Atan2(targetDirection.X, targetDirection.Z); // The problem is that rotation jumps from 0 to Pi. This is difficult to limit.
            if (desiredAzimuth == double.NaN)
                desiredAzimuth = Math.PI;
            return GetAzimuthMatrix(desiredAzimuth, delta);
        }

        private Matrix GetAzimuthMatrix(double desiredAzimuth, float delta)
        {
            desiredAzimuth = HeartUtils.LimitRotationSpeed(Azimuth, desiredAzimuth, Definition.Hardpoint.AzimuthRate * delta);

            if (!Definition.Hardpoint.CanRotateFull)
                Azimuth = (float)HeartUtils.Clamp(desiredAzimuth, Definition.Hardpoint.MinAzimuth, Definition.Hardpoint.MaxAzimuth); // Basic angle clamp
            else
                Azimuth = (float)HeartUtils.NormalizeAngle(desiredAzimuth); // Adjust rotation to (-180, 180), but don't have any limits

            //MyAPIGateway.Utilities.ShowNotification("AZ: " + Math.Round(MathHelper.ToDegrees(Azimuth)), 1000/60);
            return Matrix.CreateFromYawPitchRoll(Azimuth, 0, 0);
        }

        private MatrixD GetElevationMatrix(Vector3D targetDirection, float delta)
        {
            double desiredElevation = Math.Asin(-targetDirection.Y);
            if (desiredElevation == double.NaN)
                desiredElevation = Math.PI;
            return GetElevationMatrix(desiredElevation, delta);
        }

        private Matrix GetElevationMatrix(double desiredElevation, float delta)
        {
            desiredElevation = HeartUtils.LimitRotationSpeed(Elevation, desiredElevation, Definition.Hardpoint.ElevationRate * delta);
            if (!Definition.Hardpoint.CanElevateFull)
                Elevation = (float)-HeartUtils.Clamp(-desiredElevation, Definition.Hardpoint.MinElevation, Definition.Hardpoint.MaxElevation);
            else
                Elevation = (float)HeartUtils.NormalizeAngle(desiredElevation);
            return Matrix.CreateFromYawPitchRoll(0, Elevation, 0);
        }

        #region Terminal Controls

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

        internal override bool LoadSettings()
        {
            if (SorterWep.Storage == null)
                return false;

            string rawData;
            if (!SorterWep.Storage.TryGetValue(HeartSettingsGUID, out rawData))
                return false;

            bool baseRet = base.LoadSettings();

            try
            {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<Heart_Settings>(Convert.FromBase64String(rawData));
                if (loadedSettings != null)
                {
                    // Set the AI Range from loaded settings
                    Settings.AiRange = loadedSettings.AiRange;
                    AiRange.Value = Settings.AiRange;

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

                    return baseRet;
                }
            }
            catch
            {

            }

            return false;
        }

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
    }
}
