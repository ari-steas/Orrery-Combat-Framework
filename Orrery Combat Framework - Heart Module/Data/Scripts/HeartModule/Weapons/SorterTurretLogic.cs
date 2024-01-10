using Heart_Module.Data.Scripts.HeartModule.Debug;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            Dictionary<string, IMyModelDummy> dummies = new Dictionary<string, IMyModelDummy>();
            MyEntitySubpart subpart = HeartData.I.SubpartManager.GetSubpart((MyEntity)SorterWep, "TestAz");

            ((IMyEntity)subpart).Model.GetDummies(dummies);

            MatrixD partMatrix = subpart.WorldMatrix;
            Matrix muzzleMatrix = dummies["muzzle01"].Matrix;

            //foreach (var part in HeartData.I.SubpartManager.GetAllSubparts((MyEntity)SorterWep))
            //    MyAPIGateway.Utilities.ShowMessage("HM", part);

            if (muzzleMatrix != null)
                return muzzleMatrix * partMatrix;

            return MatrixD.Identity;
        }

        public void UpdateTurretSubparts()
        {
            Vector3D vecToTarget = TargetingHelper.InterceptionPoint(MuzzleMatrix.Translation, Vector3D.Zero, (MyEntity)MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity, 0) ?? Vector3D.MaxValue;
            if (vecToTarget == Vector3D.MaxValue)
                return;
            if (!MyAPIGateway.Utilities.IsDedicated)
                DebugDraw.AddPoint(vecToTarget, Color.Red, 0);

            vecToTarget -= MuzzleMatrix.Translation;

            MyEntitySubpart azimuth = HeartData.I.SubpartManager.GetSubpart((MyEntity)SorterWep, "TestAz");
            
            // Inverted because SUBPARTS ARE FUCKED!
            HeartData.I.SubpartManager.LocalRotateSubpartAbs(azimuth, MatrixD.CreateWorld(Vector3D.Zero, -vecToTarget.Normalized(), Vector3D.Up));
        }
    }
}
