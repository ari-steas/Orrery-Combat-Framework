using Heart_Module.Data.Scripts.HeartModule.Definitions.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.Network;
using VRage.Sync;
using VRageMath;
using YourName.ModName.Data.Scripts.HeartModule.Weapons;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "TestWeaponTurret")]
    public partial class SorterTurretLogic : SorterWeaponLogic, IMyEventProxy
    {
        internal float Azimuth = 0; // lol and lmao
        internal float Elevation = 0;
        internal double DesiredAzimuth = 0;
        internal double DesiredElevation = 0;
        MyEntity3DSoundEmitter TurretRotationSound;

        /// <summary>
        /// Delta for engine ticks; 60tps
        /// </summary>
        private const float deltaTick = 1 / 60f;

        public bool IsTargetAligned { get; private set; } = false;
        public bool IsTargetInRange { get; private set; } = false;

        public Vector3D AimPoint { get; private set; } = Vector3D.MaxValue; // TODO fix, should be in targeting CS

        public SorterTurretLogic(IMyConveyorSorter sorterWeapon, WeaponDefinitionBase definition, uint id) : base(sorterWeapon, definition, id)
        {
            TurretRotationSound = new MyEntity3DSoundEmitter(null);
        }

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
            if (Definition.Assignments.Muzzles.Length == 0 || !MuzzleDummies.ContainsKey(Definition.Assignments.Muzzles[id]))
                return SorterWep.WorldMatrix;

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
                DesiredAzimuth = Definition.Hardpoint.HomeAzimuth;
                DesiredElevation = -Definition.Hardpoint.HomeElevation;
                return; // Exit if interception point does not exist
            }

            Vector3D vecToTarget = aimpoint - MuzzleMatrix.Translation;
            //DebugDraw.AddLine(MuzzleMatrix.Translation, MuzzleMatrix.Translation + MuzzleMatrix.Forward * vecToTarget.Length(), Color.Blue, 0); // Muzzle line

            vecToTarget = Vector3D.Rotate(vecToTarget.Normalized(), -MatrixD.Invert(SorterWep.WorldMatrix)); // Inverted because subparts are wonky. Pre-rotated. //Inverted again for compat with wc's bass ackwards model stitching

            DesiredAzimuth = GetNewAzimuthAngle(vecToTarget);
            DesiredElevation = GetNewElevationAngle(vecToTarget);
        }

        const float tolerance = 0.1f; // Adjust this value as needed

        public void UpdateTurretSubparts(float delta)
        {
            if (!Definition.Hardpoint.ControlRotation)
                return;
            if (Azimuth == DesiredAzimuth && Elevation == DesiredElevation) // Don't move if you're already there
                return;

            // Play sound if the turret is rotating
            if (Math.Abs(Azimuth - DesiredAzimuth) > tolerance || Math.Abs(Elevation - DesiredElevation) > tolerance)      //so it doesnt keep playing on tiny adjustments
            {
                if (!TurretRotationSound.IsPlaying)
                {
                    TurretRotationSound.PlaySound(Definition.Audio.RotationSoundPair, true);
                }
            }
            else if (TurretRotationSound.IsPlaying)
            {
                TurretRotationSound.StopSound(false);
            }


            MyEntitySubpart azimuth = SubpartManager.GetSubpart(SorterWep, Definition.Assignments.AzimuthSubpart);
            if (azimuth == null)
            {
                SoftHandle.RaiseException($"Azimuth subpart null on \"{SorterWep?.CustomName}\"");
                return;
            }
            MyEntitySubpart elevation = SubpartManager.GetSubpart(azimuth, Definition.Assignments.ElevationSubpart);
            if (elevation == null)
            {
                SoftHandle.RaiseException($"Elevation subpart null on \"{SorterWep?.CustomName}\"");
                return;
            }

            SubpartManager.LocalRotateSubpartAbs(azimuth, GetAzimuthMatrix(delta));
            SubpartManager.LocalRotateSubpartAbs(elevation, GetElevationMatrix(delta));
        }

        private double GetNewAzimuthAngle(Vector3D targetDirection)
        {
            double desiredAzimuth = Math.Atan2(targetDirection.X, targetDirection.Z);
            if (desiredAzimuth == double.NaN)
                desiredAzimuth = Math.PI;
            return desiredAzimuth;
        }

        private Matrix GetAzimuthMatrix(float delta)
        {
            var _limitedAzimuth = HeartUtils.LimitRotationSpeed(Azimuth, DesiredAzimuth, Definition.Hardpoint.AzimuthRate * delta);

            if (!Definition.Hardpoint.CanRotateFull)
                Azimuth = (float)HeartUtils.Clamp(_limitedAzimuth, Definition.Hardpoint.MinAzimuth, Definition.Hardpoint.MaxAzimuth); // Basic angle clamp
            else
                Azimuth = (float)HeartUtils.NormalizeAngle(_limitedAzimuth); // Adjust rotation to (-180, 180), but don't have any limits
            return Matrix.CreateFromYawPitchRoll(Azimuth, 0, 0);
        }

        private double GetNewElevationAngle(Vector3D targetDirection)
        {
            double desiredElevation = Math.Asin(-targetDirection.Y);
            if (desiredElevation == double.NaN)
                desiredElevation = Math.PI;
            return desiredElevation;
        }

        private Matrix GetElevationMatrix(float delta)
        {
            var _limitedElevation = HeartUtils.LimitRotationSpeed(Elevation, DesiredElevation, Definition.Hardpoint.ElevationRate * delta);

            if (!Definition.Hardpoint.CanElevateFull)
                Elevation = (float)HeartUtils.Clamp(_limitedElevation, Definition.Hardpoint.MinElevation, Definition.Hardpoint.MaxElevation);
            else
                Elevation = (float)HeartUtils.NormalizeAngle(_limitedElevation);
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
            neededAngle.Y = -HeartUtils.NormalizeAngle(neededAngle.Y, Math.PI / 2);

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
            AiRange = Math.Min(AiRange + 100, 1000);
        }

        public void DecreaseAIRange()
        {
            // Decrease AI Range within limits
            AiRange = Math.Max(AiRange - 100, 0);
        }

        internal override void LoadDefaultSettings()
        {
            base.LoadDefaultSettings();

            // Defaults
            if (MyAPIGateway.Session.IsServer) // Defaults get set whenever a client joins, which is bad.
            {
                AiRange = Definition.Targeting.MaxTargetingRange;

                // These default to true, and are disabled elsewhere if not allowed.
                TargetProjectilesState = true;
                TargetCharactersState = true;
                TargetGridsState = true;
                TargetLargeGridsState = true;
                TargetSmallGridsState = true;

                TargetEnemiesState = (Definition.Targeting.DefaultIFF & IFF_Enum.TargetEnemies) == IFF_Enum.TargetEnemies;
                TargetFriendliesState = (Definition.Targeting.DefaultIFF & IFF_Enum.TargetFriendlies) == IFF_Enum.TargetFriendlies;
                TargetNeutralsState = (Definition.Targeting.DefaultIFF & IFF_Enum.TargetNeutrals) == IFF_Enum.TargetNeutrals;
                TargetUnownedState = false;
                PreferUniqueTargetsState = (Definition.Targeting.DefaultIFF & IFF_Enum.TargetUnique) == IFF_Enum.TargetUnique;
            }
        }

        internal override bool LoadSettings()
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

            bool baseRet = base.LoadSettings();

            try
            {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<Heart_Settings>(Convert.FromBase64String(rawData));
                if (loadedSettings != null)
                {
                    Settings.AiRange = loadedSettings.AiRange;
                    Settings.PreferUniqueTargetState = loadedSettings.PreferUniqueTargetState;
                    Settings.TargetGridsState = loadedSettings.TargetGridsState;
                    Settings.TargetProjectilesState = loadedSettings.TargetProjectilesState;
                    Settings.TargetCharactersState = loadedSettings.TargetCharactersState;
                    Settings.TargetLargeGridsState = loadedSettings.TargetLargeGridsState;
                    Settings.TargetSmallGridsState = loadedSettings.TargetSmallGridsState;
                    Settings.TargetFriendliesState = loadedSettings.TargetFriendliesState;
                    Settings.TargetNeutralsState = loadedSettings.TargetNeutralsState;
                    Settings.TargetEnemiesState = loadedSettings.TargetEnemiesState;
                    Settings.TargetUnownedState = loadedSettings.TargetUnownedState;
                    return baseRet;
                }
            }
            catch
            {

            }

            // In case Target(n) is turned off after a weapon is placed
            TargetProjectilesState &= (Definition.Targeting.AllowedTargetTypes & TargetType_Enum.TargetProjectiles) == TargetType_Enum.TargetProjectiles;
            TargetCharactersState &= (Definition.Targeting.AllowedTargetTypes & TargetType_Enum.TargetCharacters) == TargetType_Enum.TargetCharacters;
            TargetGridsState &= (Definition.Targeting.AllowedTargetTypes & TargetType_Enum.TargetGrids) == TargetType_Enum.TargetGrids;
            TargetLargeGridsState &= (Definition.Targeting.AllowedTargetTypes & TargetType_Enum.TargetGrids) == TargetType_Enum.TargetGrids;
            TargetSmallGridsState &= (Definition.Targeting.AllowedTargetTypes & TargetType_Enum.TargetGrids) == TargetType_Enum.TargetGrids;

            return false;
        }

        public float AiRange
        {
            get
            {
                return Settings.AiRange;
            }

            set
            {
                Settings.AiRange = value;
                Settings.Sync();
            }
        }

        public bool PreferUniqueTargetsState
        {
            get
            {
                return Settings.PreferUniqueTargetState;
            }

            set
            {
                Settings.PreferUniqueTargetState = value;
                Settings.Sync();
            }
        }

        public bool TargetGridsState
        {
            get
            {
                return Settings.TargetGridsState;
            }

            set
            {
                Settings.TargetGridsState = value;
                Settings.Sync();

            }
        }

        public bool TargetProjectilesState
        {
            get
            {
                return Settings.TargetProjectilesState;
            }

            set
            {
                Settings.TargetProjectilesState = value;
                Settings.Sync();
            }
        }

        public bool TargetCharactersState
        {
            get
            {
                return Settings.TargetCharactersState;
            }

            set
            {
                Settings.TargetCharactersState = value;
                Settings.Sync();
            }
        }

        public bool TargetLargeGridsState
        {
            get
            {
                return Settings.TargetLargeGridsState;
            }

            set
            {
                Settings.TargetLargeGridsState = value;
                Settings.Sync();
            }
        }

        public bool TargetSmallGridsState
        {
            get
            {
                return Settings.TargetSmallGridsState;
            }

            set
            {
                Settings.TargetSmallGridsState = value;
                Settings.Sync();
            }
        }

        public bool TargetFriendliesState
        {
            get
            {
                return Settings.TargetFriendliesState;
            }

            set
            {
                Settings.TargetFriendliesState = value;
                Settings.Sync();
            }
        }

        public bool TargetNeutralsState
        {
            get
            {
                return Settings.TargetNeutralsState;
            }

            set
            {
                Settings.TargetNeutralsState = value;
                Settings.Sync();
            }
        }

        public bool TargetEnemiesState
        {
            get
            {
                return Settings.TargetEnemiesState;
            }

            set
            {
                Settings.TargetEnemiesState = value;
                Settings.Sync();
            }
        }

        public bool TargetUnownedState
        {
            get
            {
                return Settings.TargetUnownedState;
            }

            set
            {
                Settings.TargetUnownedState = value;
                Settings.Sync();
            }
        }

        #endregion
    }
}
