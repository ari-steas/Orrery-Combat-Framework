using Heart_Module.Data.Scripts.HeartModule.Debug;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "TestWeaponTurret")]
    public class SorterTurretLogic : SorterWeaponLogic
    {
        MatrixD MuzzleMatrix = MatrixD.Identity;

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
                MyEntitySubpart azSubpart = HeartData.I.SubpartManager.GetSubpart((MyEntity)SorterWep, "TestAz");
                MyEntitySubpart evSubpart = HeartData.I.SubpartManager.GetSubpart(azSubpart, "TestEv");

                ((IMyEntity)evSubpart).Model.GetDummies(dummies);

                MatrixD partMatrix = evSubpart.WorldMatrix;
                Matrix muzzleMatrix = dummies["muzzle01"].Matrix;

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
            //Vector3D vecToTarget = TargetingHelper.InterceptionPoint(MuzzleMatrix.Translation, Vector3D.Zero, (MyEntity)MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity, 0) ?? Vector3D.MaxValue;
            Vector3D vecToTarget = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.PositionComp.GetPosition();
            if (vecToTarget == Vector3D.MaxValue)
                return;
            if (!MyAPIGateway.Utilities.IsDedicated)
                DebugDraw.AddPoint(vecToTarget, Color.Red, 0);

            vecToTarget -= MuzzleMatrix.Translation;

            MyEntitySubpart azimuth = HeartData.I.SubpartManager.GetSubpart((MyEntity)SorterWep, "TestAz");
            MyEntitySubpart elevation = HeartData.I.SubpartManager.GetSubpart(azimuth, "TestEv");
            vecToTarget = vecToTarget.Normalized();

            // Inverted because SUBPARTS ARE FUCKED!
            HeartData.I.SubpartManager.LocalRotateSubpartAbs(azimuth, GetAzimuthMatrix(azimuth, vecToTarget));
            HeartData.I.SubpartManager.LocalRotateSubpartAbs(elevation, GetElevationMatrix(elevation, vecToTarget));
        }
        
        private Matrix GetAzimuthMatrix(MyEntitySubpart azimuth, Vector3D targetDirection)
        {
            double desiredAzimuth = Math.Atan2(targetDirection.X, targetDirection.Z);

            return Matrix.CreateFromYawPitchRoll((float) desiredAzimuth, 0, 0);
        }

        private MatrixD GetElevationMatrix(MyEntitySubpart elevation, Vector3D targetDirection)
        {
            double desiredElevation = Math.Asin(-targetDirection.Y);

            return Matrix.CreateFromYawPitchRoll(0, (float)desiredElevation, 0);
        }
    }
}
