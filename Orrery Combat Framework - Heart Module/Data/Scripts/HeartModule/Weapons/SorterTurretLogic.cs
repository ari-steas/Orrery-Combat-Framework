using Sandbox.Common.ObjectBuilders;
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
        public override MatrixD GetMuzzleMatrix()
        {
            Dictionary<string, IMyModelDummy> dummies = new Dictionary<string, IMyModelDummy>();
            MyEntitySubpart subpart = HeartData.I.SubpartManager.GetSubpart((MyEntity)SorterWep, "TestAz");

            HeartData.I.SubpartManager.RotateSubpart(subpart, MatrixD.CreateRotationY(0.1));

            ((IMyEntity)subpart).Model.GetDummies(dummies);

            MatrixD partMatrix = subpart.WorldMatrix;
            Matrix muzzleMatrix = dummies["muzzle01"].Matrix;

            //foreach (var part in HeartData.I.SubpartManager.GetAllSubparts((MyEntity)SorterWep))
            //    MyAPIGateway.Utilities.ShowMessage("HM", part);
            if (muzzleMatrix != null)
                return muzzleMatrix * partMatrix;
            return MatrixD.Identity;
        }
    }
}
