using Heart_Module.Data.Scripts.HeartModule.Debug;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using VRage.Sync;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;
using VRage.Game.ModAPI.Network;
using VRage.ObjectBuilders;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "TestWeaponTurret")]
    public class SorterTurretLogic : SorterWeaponLogic
    {
        MatrixD MuzzleMatrix = MatrixD.Identity;
        public MySync<float, SyncDirection.FromServer> AzimuthSync;
        public MySync<float, SyncDirection.FromServer> ElevationSync;


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

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            UpdateTurretSubparts();
            MuzzleMatrix = CalcMuzzleMatrix();
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

                //foreach (var part in HeartData.I.SubpartManager.GetAllSubparts((MyEntity)SorterWep))
                //    MyAPIGateway.Utilities.ShowMessage("HM", part);

                if (muzzleMatrix != null)
                    return muzzleMatrix * partMatrix;
            }
            catch { }
            return MatrixD.Identity;
        }

        public void UpdateTurretSubparts()
        {
            Vector3D vecToTarget = TargetingHelper.InterceptionPoint(MuzzleMatrix.Translation, Vector3D.Zero, Vector3D.Zero, Vector3D.Zero, 1) ?? Vector3D.MaxValue;
            
            if (vecToTarget == Vector3D.MaxValue)
                return;
            if (!MyAPIGateway.Utilities.IsDedicated)
                DebugDraw.AddPoint(vecToTarget, Color.Red, 0);

            vecToTarget -= MuzzleMatrix.Translation;

            MyEntitySubpart azimuth = HeartData.I.SubpartManager.GetSubpart((MyEntity)SorterWep, "TestAz");
            MyEntitySubpart elevation = HeartData.I.SubpartManager.GetSubpart(azimuth, "TestEv");
            vecToTarget = Vector3D.Rotate(vecToTarget.Normalized(), MatrixD.Invert(SorterWep.WorldMatrix)); // Inverted because subparts are wonky. Pre-rotated.

            HeartData.I.SubpartManager.LocalRotateSubpartAbs(azimuth, GetAzimuthMatrix(vecToTarget));
            HeartData.I.SubpartManager.LocalRotateSubpartAbs(elevation, GetElevationMatrix(vecToTarget));
        }

        float Azimuth = 0;
        float Elevation = 0;

        private Matrix GetAzimuthMatrix(Vector3D targetDirection)
        {
            float desiredAzimuth = (float) Math.Atan2(targetDirection.X, targetDirection.Z);
            if (desiredAzimuth == float.NaN)
                desiredAzimuth = (float) Math.PI;

            desiredAzimuth = Clamp(desiredAzimuth - Azimuth, Definition.Hardpoint.AzimuthRate, -Definition.Hardpoint.AzimuthRate) + Azimuth;
            
            return GetAzimuthMatrix(desiredAzimuth);
        }

        private Matrix GetAzimuthMatrix(float desiredAzimuth)
        {
            Azimuth = Clamp(desiredAzimuth, Definition.Hardpoint.MaxAzimuth, Definition.Hardpoint.MinAzimuth);

            return Matrix.CreateFromYawPitchRoll(Azimuth, 0, 0);
        }

        private MatrixD GetElevationMatrix(Vector3D targetDirection)
        {
            float desiredElevation = (float)Math.Asin(-targetDirection.Y);
            if (desiredElevation == float.NaN)
                desiredElevation = (float)Math.PI;

            desiredElevation = Clamp(desiredElevation - Elevation, Definition.Hardpoint.ElevationRate, -Definition.Hardpoint.ElevationRate) + Elevation;

            return GetElevationMatrix(desiredElevation);
        }

        private Matrix GetElevationMatrix(float desiredElevation)
        {
            Elevation = -Clamp(-desiredElevation, Definition.Hardpoint.MaxElevation, Definition.Hardpoint.MinElevation);

            return Matrix.CreateFromYawPitchRoll(0, Elevation, 0);
        }

        private float Clamp(float value, float max, float min)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
