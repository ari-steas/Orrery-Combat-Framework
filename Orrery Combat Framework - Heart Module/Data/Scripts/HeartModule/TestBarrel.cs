using Heart_Module.Data.Scripts.HeartModule.Debug;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "BarrelBlock")]
    public class TestBarrel : MyGameLogicComponent
    {
        IMyCubeBlock block;
        SubpartManager SubpartManager = new SubpartManager();
        SorterTurretLogic turret = null;
        double offset = 0;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            block = (IMyCubeBlock)Entity;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (block?.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            if (!HeartData.I.DidFirstInit)
                return;

            if (turret?.SorterWep == null || turret.SorterWep.MarkedForClose || turret.SorterWep.CubeGrid != block.CubeGrid)
            {
                TryGetNewTurret();
                return;
            }

            MyEntitySubpart subpart = SubpartManager.GetSubpart(Entity, "barrel");

            MatrixD muzzleMatrix = turret.MuzzleMatrix;
            muzzleMatrix.Translation = turret.SorterWep.GetPosition() + turret.MuzzleMatrix.Forward * offset;
            MatrixD parentMatrix = subpart.Parent.PositionComp.WorldMatrixRef;
            
            Matrix m = muzzleMatrix * MatrixD.Invert(parentMatrix);
            subpart.PositionComp.SetLocalMatrix(ref m);
        }
        
        private void TryGetNewTurret()
        {
            SubpartManager.GetSubpart(Entity, "barrel").PositionComp.SetLocalMatrix(ref Matrix.Identity);

            if (WeaponManager.I.GridWeapons[block.CubeGrid].Count == 0)
                return;
            foreach (var wep in WeaponManager.I.GridWeapons[block.CubeGrid])
            {
                if (wep is SorterTurretLogic)
                {
                    turret = (SorterTurretLogic) wep;
                    offset = ((Vector3D) (block.Position - wep.SorterWep.Position)).Max() * 2.5f;
                    MyAPIGateway.Utilities.ShowNotification(offset.ToString());
                    break;
                }
            }
        }
    }
}
