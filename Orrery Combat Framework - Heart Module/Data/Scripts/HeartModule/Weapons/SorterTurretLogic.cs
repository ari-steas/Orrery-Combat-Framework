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
using System.Security.Policy;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "TestWeaponTurret")]
    public partial class SorterTurretLogic : SorterWeaponLogic
    {
        internal float Azimuth = (float)Math.PI;
        internal float Elevation = 0;
        internal double DesiredAzimuth = Math.PI;
        internal double DesiredElevation = 0;

        /// <summary>
        /// Delta for engine ticks; 60tps
        /// </summary>
        private const float deltaTick = 1 / 60f;

        public bool IsTargetAligned { get; private set; } = false;
        public bool IsTargetInRange { get; private set; } = false;

        public Vector3D AimPoint { get; private set; } = Vector3D.MaxValue; // TODO fix, should be in targeting CS

        public IMyEntity TargetEntity = null;
        public Projectile TargetProjectile = null;


        public SorterTurretLogic(IMyConveyorSorter sorterWeapon, SerializableWeaponDefinition definition, uint id) : base(sorterWeapon, definition, id) { }

        public override void UpdateAfterSimulation()
        {
            if (!SorterWep.IsWorking) // Don't turn if the turret is disabled
                return;
            if (MyAPIGateway.Session.IsServer)
                UpdateTargeting();

            base.UpdateAfterSimulation();
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

        public void UpdateAzimuthElevation(Vector3D aimpoint)
        {
            if (aimpoint == Vector3D.MaxValue)
            {
                DesiredAzimuth = Math.PI - Definition.Hardpoint.HomeAzimuth;
                DesiredElevation = -Definition.Hardpoint.HomeElevation;
                return; // Exit if interception point does not exist
            }

            Vector3D vecToTarget = aimpoint - MuzzleMatrix.Translation;
            //DebugDraw.AddLine(MuzzleMatrix.Translation, MuzzleMatrix.Translation + MuzzleMatrix.Forward * vecToTarget.Length(), Color.Blue, 0); // Muzzle line

            vecToTarget = Vector3D.Rotate(vecToTarget.Normalized(), -MatrixD.Invert(SorterWep.WorldMatrix)); // Inverted because subparts are wonky. Pre-rotated. //Inverted again for compat with wc's bass ackwards model stitching

            DesiredAzimuth = GetNewAzimuthAngle(vecToTarget);
            DesiredElevation = GetNewElevationAngle(vecToTarget);
        }

        public void UpdateTurretSubparts(float delta)
        {
            if (!Definition.Hardpoint.ControlRotation)
                return;

            if (Azimuth == DesiredAzimuth || Elevation == DesiredElevation) // Don't move if you're already there
                return;

            MyEntitySubpart azimuth = SubpartManager.GetSubpart((MyEntity)SorterWep, Definition.Assignments.AzimuthSubpart);
            MyEntitySubpart elevation = SubpartManager.GetSubpart(azimuth, Definition.Assignments.ElevationSubpart);
            
            SubpartManager.LocalRotateSubpartAbs(azimuth, GetAzimuthMatrix(DesiredAzimuth, delta));
            SubpartManager.LocalRotateSubpartAbs(elevation, GetElevationMatrix(DesiredElevation, delta));
        }

        private double GetNewAzimuthAngle(Vector3D targetDirection)
        {
            double desiredAzimuth = Math.Atan2(targetDirection.X, targetDirection.Z);
            if (desiredAzimuth == double.NaN)
                desiredAzimuth = Math.PI;
            return desiredAzimuth;
        }

        private Matrix GetAzimuthMatrix(double desiredAzimuth, float delta)
        {
            desiredAzimuth = HeartUtils.LimitRotationSpeed(Azimuth, desiredAzimuth, Definition.Hardpoint.AzimuthRate * delta);
            float oldAzi = Azimuth;
            if (!Definition.Hardpoint.CanRotateFull)
                Azimuth = (float)HeartUtils.Clamp(desiredAzimuth, Definition.Hardpoint.MinAzimuth, Definition.Hardpoint.MaxAzimuth); // Basic angle clamp
            else
                Azimuth = (float)HeartUtils.NormalizeAngle(desiredAzimuth); // Adjust rotation to (-180, 180), but don't have any limits
            //MyAPIGateway.Utilities.ShowNotification("AZ: " + Math.Round(MathHelper.ToDegrees(Azimuth)), 1000/60);
            return Matrix.CreateFromYawPitchRoll(Azimuth, 0, 0);
        }

        private double GetNewElevationAngle(Vector3D targetDirection)
        {
            double desiredElevation = Math.Asin(-targetDirection.Y);
            if (desiredElevation == double.NaN)
                desiredElevation = Math.PI;
            return desiredElevation;
        }

        private Matrix GetElevationMatrix(double desiredElevation, float delta)
        {
            desiredElevation = HeartUtils.LimitRotationSpeed(Elevation, desiredElevation, Definition.Hardpoint.ElevationRate * delta);
            float oldEle = Elevation;
            if (!Definition.Hardpoint.CanElevateFull)
                Elevation = (float)-HeartUtils.Clamp(-desiredElevation, Definition.Hardpoint.MinElevation, Definition.Hardpoint.MaxElevation);
            else
                Elevation = (float)HeartUtils.NormalizeAngle(desiredElevation);
            return Matrix.CreateFromYawPitchRoll(0, Elevation, 0);
        }

        public void SetFacing(float azimuth, float elevation)
        {
            DesiredAzimuth = azimuth;
            DesiredElevation = elevation;
        }

        /// <summary>
        /// Returns the angle needed to reach a target.
        /// </summary>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        private Vector2D GetAngleToTarget(Vector3D targetPosition)
        {
            Vector3D vecToTarget = targetPosition - MuzzleMatrix.Translation;

            vecToTarget = Vector3D.Rotate(vecToTarget.Normalized(), MatrixD.Invert(SorterWep.WorldMatrix));

            double desiredAzimuth = Math.Atan2(vecToTarget.X, vecToTarget.Z);
            if (desiredAzimuth == double.NaN)
                desiredAzimuth = Math.PI;

            double desiredElevation = Math.Asin(-vecToTarget.Y);
            if (desiredElevation == double.NaN)
                desiredElevation = Math.PI;

            return new Vector2D(desiredAzimuth, desiredElevation);
        }

        /// <summary>
        /// Determines if a target position is within the turret's aiming bounds.
        /// </summary>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        private bool CanAimAtTarget(Vector3D targetPosition)
        {
            if (Vector3D.DistanceSquared(MuzzleMatrix.Translation, targetPosition) > AiRange * AiRange) // Range check
                return false;

            Vector2D neededAngle = GetAngleToTarget(targetPosition);
            neededAngle.X = HeartUtils.NormalizeAngle(neededAngle.X - Math.PI);
            neededAngle.Y = HeartUtils.NormalizeAngle(-neededAngle.Y, Math.PI/2);

            bool canAimAzimuth = Definition.Hardpoint.CanRotateFull;

            if (!canAimAzimuth && !(neededAngle.X < Definition.Hardpoint.MaxAzimuth && neededAngle.X > Definition.Hardpoint.MinAzimuth))
                return false; // Check azimuth constrainst

            bool canAimElevation = Definition.Hardpoint.CanElevateFull;

            if (!canAimElevation && !(neededAngle.Y < Definition.Hardpoint.MaxElevation && neededAngle.Y > Definition.Hardpoint.MinElevation))
                return false; // Check elevation constraints

            return true;
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
            // Defaults
            Terminal_Heart_Range_Slider = Definition.Targeting.MaxTargetingRange;
            Terminal_Heart_TargetGrids = true;
            Terminal_Heart_TargetProjectiles = true;
            Terminal_Heart_TargetCharacters = true;
            Terminal_Heart_TargetLargeGrids = true;
            Terminal_Heart_TargetSmallGrids = true;
            Terminal_Heart_TargetEnemies = true;
            Terminal_Heart_TargetNeutrals = true;
            Terminal_Heart_TargetFriendlies = false;
            Terminal_Heart_TargetUnowned = false;
            Terminal_Heart_PreferUniqueTargets = false;

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

                    Settings.PreferUniqueTargetState = loadedSettings.PreferUniqueTargetState;
                    PreferUniqueTargets.Value = Settings.PreferUniqueTargetState;

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
        public MySync<bool, SyncDirection.BothWays> PreferUniqueTargets;
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

        public bool Terminal_Heart_PreferUniqueTargets // TODO
        {
            get
            {
                return Settings.PreferUniqueTargetState;
            }

            set
            {
                Settings.PreferUniqueTargetState = value;
                PreferUniqueTargets.Value = value;
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
