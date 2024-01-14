using Heart_Module.Data.Scripts.HeartModule.Debug;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using VRage.Sync;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;
using VRage.Game.ModAPI.Network;
using VRage.ObjectBuilders;
using System.Diagnostics;
using Sandbox.Game.Entities;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using VRage.Game;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "TestWeaponTurret")]
    public partial class SorterTurretLogic : SorterWeaponLogic
    {
        public MySync<float, SyncDirection.FromServer> Azimuth;
        public MySync<float, SyncDirection.FromServer> Elevation;

        /// <summary>
        /// Delta for engine ticks; 60tps
        /// </summary>
        private const float deltaTick = 1/60f;

        public bool IsTargetAligned { get; private set; } = false;
        public bool IsTargetInRange { get; private set; } = false;

        public Vector3D AimPoint { get; private set; } = Vector3D.MaxValue; // TODO fix, should be in targeting CS
        private GenericKeenTargeting targeting = new GenericKeenTargeting();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            Azimuth.ValueChanged += OnAzimuthChanged;
            Elevation.ValueChanged += OnElevationChanged;
            Azimuth.Value = (float)Math.PI; // defaults
            Elevation.Value = 0;
        }


        private void OnAzimuthChanged(MySync<float, SyncDirection.FromServer> obj)
        {
            // Handle the change in azimuth
            //Azimuth = obj.Value;
            // Additional logic to apply azimuth changes, if needed
        }

        private void OnElevationChanged(MySync<float, SyncDirection.FromServer> obj)
        {
            // Handle the change in elevation
            //Elevation = obj.Value;
            // Additional logic to apply elevation changes, if needed
        }

        public SorterTurretLogic(IMyConveyorSorter sorterWeapon, SerializableWeaponDefinition definition, uint id) : base(sorterWeapon, definition, id) { }

        public override void UpdateAfterSimulation()
        {
            if (!SorterWep.IsWorking) // Don't turn if the turret is disabled
                return;

            UpdateTargeting();

            base.UpdateAfterSimulation();
        }

        private MyEntity currentTarget = null;

        public void UpdateTargeting()
        {
            MuzzleMatrix = CalcMuzzleMatrix(0); // Set stored MuzzleMatrix

            // Retrieve the target based on the targeting settings
            MyEntity potentialTarget = targeting.GetTarget(
                SorterWep?.CubeGrid,
                Terminal_Heart_TargetGrids,
                Terminal_Heart_TargetLargeGrids,
                Terminal_Heart_TargetSmallGrids,
                Terminal_Heart_TargetFriendlies,
                Terminal_Heart_TargetNeutrals,
                Terminal_Heart_TargetEnemies,
                Terminal_Heart_TargetUnowned
            );

            // Debug Info: Display whether a potential target is found
            if (potentialTarget != null)
            {
                MyAPIGateway.Utilities.ShowNotification("Potential Target Found", 1000 / 60, VRage.Game.MyFontEnum.Green);
            }
            else
            {
                MyAPIGateway.Utilities.ShowNotification("No Potential Target", 1000 / 60, VRage.Game.MyFontEnum.Red);
            }

            // Check if the potential target is different from the current target
            if (currentTarget != potentialTarget)
            {
                // Assign the new potential target
                currentTarget = potentialTarget;

                // Debug Info: Display the current target's name
                string targetName = currentTarget != null ? currentTarget.DisplayName : "None";
                MyAPIGateway.Utilities.ShowNotification($"Current Target: {targetName}", 2000, VRage.Game.MyFontEnum.Blue);

                // If the potential target is null, reset targeting state
                if (currentTarget == null)
                {
                    ResetTargetingState();
                }
            }

            // Proceed with targeting if a valid target is found
            if (currentTarget != null)
            {
                AimPoint = TargetingHelper.InterceptionPoint(
                    MuzzleMatrix.Translation,
                    SorterWep.CubeGrid.LinearVelocity,
                    currentTarget, 0) ?? Vector3D.MaxValue;

                UpdateTurretSubparts(deltaTick, AimPoint); // Rotate the turret
            }
            else
            {
                // If no target is found, ensure the turret is not aligned or in range
                IsTargetAligned = false;
                IsTargetInRange = false;
                AimPoint = Vector3D.MaxValue;

                UpdateTurretSubparts(deltaTick, Vector3D.MaxValue);
            }
        }

        private void ResetTargetingState()
        {
            currentTarget = null;
            IsTargetAligned = false;
            IsTargetInRange = false;
            AutoShoot = false; // Disable automatic shooting
        }

        /// <summary>
        /// Sets state of target alignment and target range
        /// </summary>
        /// <param name="target"></param>
        /// <param name="aimPoint"></param>
        private void UpdateTargetState(MyEntity target, Vector3D aimPoint)
        {
            double angle = Vector3D.Angle(MuzzleMatrix.Forward, (aimPoint - MuzzleMatrix.Translation).Normalized());
            IsTargetAligned = angle < Definition.Targeting.AimTolerance;

            double range = Vector3D.Distance(MuzzleMatrix.Translation, aimPoint);
            IsTargetInRange = range < Definition.Targeting.MaxTargetingRange && range > Definition.Targeting.MinTargetingRange;
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
            desiredAzimuth = LimitRotationSpeed(Azimuth, desiredAzimuth, Definition.Hardpoint.AzimuthRate * delta);

            if (!Definition.Hardpoint.CanRotateFull)
                Azimuth.Value = (float) Clamp(desiredAzimuth, Definition.Hardpoint.MinAzimuth, Definition.Hardpoint.MaxAzimuth); // Basic angle clamp
            else
                Azimuth.Value = (float) NormalizeAngle(desiredAzimuth); // Adjust rotation to (-180, 180), but don't have any limits

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
            desiredElevation = LimitRotationSpeed(Elevation, desiredElevation, Definition.Hardpoint.ElevationRate * delta);
            if (!Definition.Hardpoint.CanElevateFull)
                Elevation.Value = (float) -Clamp(-desiredElevation, Definition.Hardpoint.MinElevation, Definition.Hardpoint.MaxElevation);
            else
                Elevation.Value = (float) NormalizeAngle(desiredElevation);
            return Matrix.CreateFromYawPitchRoll(0, Elevation, 0);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static double ClampAbs(double value, double absMax) => Clamp(value, -absMax, absMax);

        public static double LimitRotationSpeed(double currentAngle, double targetAngle, double maxRotationSpeed)
        {
            // https://yal.cc/angular-rotations-explained/
            // It should NOT HAVE BEEN THAT HARD
            // I (aristeas) AM REALLY STUPID

            var diff = NormalizeAngle(targetAngle - currentAngle);

            // clamp rotations by speed:
            if (diff < -maxRotationSpeed) return currentAngle - maxRotationSpeed;
            if (diff > maxRotationSpeed) return currentAngle + maxRotationSpeed;
            // if difference within speed, rotation's done:
            return targetAngle;
        }

        private static double NormalizeAngle(double angleRads)
        {
            if (angleRads > Math.PI)
                return (angleRads % Math.PI) - Math.PI;
            if (angleRads < -Math.PI)
                return (angleRads % Math.PI) + Math.PI;
            return angleRads;
        }
    }
}
