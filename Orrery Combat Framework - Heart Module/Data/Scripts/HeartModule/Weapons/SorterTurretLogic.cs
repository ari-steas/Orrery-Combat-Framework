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

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "TestWeaponTurret")]
    public partial class SorterTurretLogic : SorterWeaponLogic
    {
        MatrixD MuzzleMatrix = MatrixD.Identity;
        public MySync<float, SyncDirection.FromServer> AzimuthSync;
        public MySync<float, SyncDirection.FromServer> ElevationSync;

        /// <summary>
        /// Delta for engine ticks; 60tps
        /// </summary>
        private const float deltaTick = 1/60f;
        private MyEntity lastKnownTarget = null;

        public bool IsTargetAligned { get; private set; } = false;
        public bool IsTargetInRange { get; private set; } = false;
        public Vector3D AimPoint { get; private set; } = Vector3D.Zero;
        private GenericKeenTargeting targeting = new GenericKeenTargeting();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            AzimuthSync.ValueChanged += OnAzimuthChanged;
            ElevationSync.ValueChanged += OnElevationChanged;
        }


        private void OnAzimuthChanged(MySync<float, SyncDirection.FromServer> obj)
        {
            // Handle the change in azimuth
            Azimuth = obj.Value;
            // Additional logic to apply azimuth changes, if needed
        }

        private void OnElevationChanged(MySync<float, SyncDirection.FromServer> obj)
        {
            // Handle the change in elevation
            Elevation = obj.Value;
            // Additional logic to apply elevation changes, if needed
        }

        public SorterTurretLogic(IMyConveyorSorter sorterWeapon, SerializableWeaponDefinition definition) : base(sorterWeapon, definition) { }

        public override void UpdateAfterSimulation()
        {
            UpdateTargeting();

            MyEntity target = targeting.GetTarget(SorterWep?.CubeGrid); // Using the new targeting class

            base.UpdateAfterSimulation(); // TryShoot is contained in here
        }

        public void UpdateTargeting()
        {
            MuzzleMatrix = CalcMuzzleMatrix(); // Set stored MuzzleMatrix

            MyEntity target = targeting.GetTarget(SorterWep?.CubeGrid);

            AimPoint = TargetingHelper.InterceptionPoint(
                MuzzleMatrix.Translation,
                SorterWep.CubeGrid.LinearVelocity,
                target, 0) ?? Vector3D.MaxValue;

            UpdateTurretSubparts(deltaTick, target, AimPoint); // Rotate the turret

            // Update IsTargetAligned
            if (lastKnownTarget ==  null)
                IsTargetAligned = false;
            else
            {
                double angle = Vector3D.Angle(MuzzleMatrix.Forward, (AimPoint - MuzzleMatrix.Translation).Normalized());
                IsTargetAligned = angle < Definition.Targeting.AimTolerance;
                MyAPIGateway.Utilities.ShowNotification($"Angle: {Math.Round(MathHelper.ToDegrees(angle))} [{IsTargetAligned}]", 1000 / 60);
            }

            // Update IsTargetInRange
            if (lastKnownTarget == null)
                IsTargetInRange = false;
            else
            {
                double range = Vector3D.Distance(MuzzleMatrix.Translation, AimPoint); // Use aimpoint because that will be the actual intercept position
                IsTargetInRange = range < Definition.Targeting.MaxTargetingRange && range > Definition.Targeting.MinTargetingRange;
                MyAPIGateway.Utilities.ShowNotification($"Range: {Math.Round(range)}m [{IsTargetInRange}]", 1000 / 60);
            }
        }

        public override void TryShoot()
        {
            AutoShoot = IsTargetAligned && IsTargetInRange;
            base.TryShoot();
        }

        public override MatrixD CalcMuzzleMatrix()
        {
            try
            {
                Dictionary<string, IMyModelDummy> dummies = new Dictionary<string, IMyModelDummy>();
                MyEntitySubpart azSubpart = HeartData.I.SubpartManager.GetSubpart((MyEntity)SorterWep, Definition.Assignments.AzimuthSubpart);
                MyEntitySubpart evSubpart = HeartData.I.SubpartManager.GetSubpart(azSubpart, Definition.Assignments.ElevationSubpart);

                ((IMyEntity)evSubpart).Model.GetDummies(dummies);

                MatrixD partMatrix = evSubpart.WorldMatrix;
                Matrix muzzleMatrix = dummies[Definition.Assignments.Muzzles[0]].Matrix;

                if (muzzleMatrix != null)
                    return muzzleMatrix * partMatrix;
            }
            catch { }
            return MatrixD.Identity;
        }

        public void UpdateTurretSubparts(float delta, MyEntity target, Vector3D aimpoint)
        {
            if (target == null)
                return; // Exit if there is no target

            // Calculate the vector to the target

            if (aimpoint == Vector3D.MaxValue)
                return; // Exit if the interception point cannot be calculated

            Vector3D vecToTarget = aimpoint - MuzzleMatrix.Translation;
            DebugDraw.AddLine(MuzzleMatrix.Translation, MuzzleMatrix.Translation + MuzzleMatrix.Forward * vecToTarget.Length(), Color.Blue, 0);
            DebugDraw.AddPoint(target.PositionComp.GetPosition(), Color.Blue, 0);

            MyEntitySubpart azimuth = HeartData.I.SubpartManager.GetSubpart((MyEntity)SorterWep, Definition.Assignments.AzimuthSubpart);
            MyEntitySubpart elevation = HeartData.I.SubpartManager.GetSubpart(azimuth, Definition.Assignments.ElevationSubpart);

            vecToTarget = Vector3D.Rotate(vecToTarget.Normalized(), MatrixD.Invert(SorterWep.WorldMatrix)); // Inverted because subparts are wonky. Pre-rotated.
            HeartData.I.SubpartManager.LocalRotateSubpartAbs(azimuth, GetAzimuthMatrix(vecToTarget, delta));
            HeartData.I.SubpartManager.LocalRotateSubpartAbs(elevation, GetElevationMatrix(vecToTarget, delta));
        }


        float Azimuth = 0;
        float Elevation = 0;

        private Matrix GetAzimuthMatrix(Vector3D targetDirection, float delta)
        {
            double desiredAzimuth = Math.Atan2(targetDirection.X, targetDirection.Z); // The problem is that rotation jumps from 0 to Pi. This is difficult to limit.
            if (desiredAzimuth == double.NaN)
                desiredAzimuth = Math.PI;

            //desiredAzimuth = ModularClamp(Azimuth - desiredAzimuth, -Definition.Hardpoint.AzimuthRate * delta, Definition.Hardpoint.AzimuthRate * delta) + Azimuth;

            return GetAzimuthMatrix(desiredAzimuth);
        }

        private Matrix GetAzimuthMatrix(double desiredAzimuth)
        {
            if (!Definition.Hardpoint.CanRotateFull)
                Azimuth = (float) Clamp(desiredAzimuth, Definition.Hardpoint.MaxAzimuth, Definition.Hardpoint.MinAzimuth); // Basic angle clamp
            else
                Azimuth = (float) NormalizeAngle(desiredAzimuth); // Adjust rotation to (-180, 180), but don't have any limits

            //MyAPIGateway.Utilities.ShowNotification("AZ: " + Math.Round(MathHelper.ToDegrees(Azimuth)), 1000/60);
            return Matrix.CreateFromYawPitchRoll(Azimuth, 0, 0);
        }

        private MatrixD GetElevationMatrix(Vector3D targetDirection, float delta)
        {
            float desiredElevation = (float)Math.Asin(-targetDirection.Y);
            if (desiredElevation == float.NaN)
                desiredElevation = (float)Math.PI;

            //desiredElevation = Clamp(desiredElevation - Elevation, Definition.Hardpoint.ElevationRate * delta, -Definition.Hardpoint.ElevationRate * delta) + Elevation;
            //desiredElevation = (float) ModularClamp(Elevation - desiredElevation, -Definition.Hardpoint.ElevationRate * delta, Definition.Hardpoint.ElevationRate * delta) + Elevation;

            return GetElevationMatrix(desiredElevation);
        }

        private Matrix GetElevationMatrix(float desiredElevation)
        {
            if (!Definition.Hardpoint.CanElevateFull)
                Elevation = (float) -Clamp(-desiredElevation, Definition.Hardpoint.MaxElevation, Definition.Hardpoint.MinElevation);
            else
                Elevation = (float) NormalizeAngle(desiredElevation);
            return Matrix.CreateFromYawPitchRoll(0, Elevation, 0);
        }

        private static double Clamp(double value, double max, double min)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static double ClampAbs(double value, double absMax) => Clamp(value, absMax, -absMax);

        private static double ModularClamp(double val, double min, double max, double rangemin = -Math.PI, double rangemax = Math.PI) // https://forum.unity.com/threads/clamping-angle-between-two-values.853771/
        {
            var modulus = Math.Abs(rangemax - rangemin);
            if ((val %= modulus) < 0f) val += modulus;
            return Clamp(val + Math.Min(rangemin, rangemax), max, min);
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
