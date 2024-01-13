using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRage.ModAPI;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    public class GenericKeenTargeting
    {
        private MyEntity lastKnownTarget = null;

        public MyEntity GetTarget(IMyCubeGrid grid, bool targetGrids, bool targetLargeGrids, bool targetSmallGrids)
        {
            if (grid == null)
            {
                MyAPIGateway.Utilities.ShowNotification("No grid found", 1000 / 60, VRage.Game.MyFontEnum.Red);
                return null;
            }

            var myCubeGrid = grid as MyCubeGrid;
            if (myCubeGrid != null)
            {
                MyShipController activeController = null;

                foreach (var block in myCubeGrid.GetFatBlocks<MyShipController>())
                {
                    if (block.NeedsPerFrameUpdate)
                    {
                        activeController = block;
                        break;
                    }
                }

                if (activeController != null && activeController.Pilot != null)
                {
                    var targetLockingComponent = activeController.Pilot.Components.Get<MyTargetLockingComponent>();
                    if (targetLockingComponent != null && targetLockingComponent.IsTargetLocked)
                    {
                        var targetEntity = targetLockingComponent.TargetEntity;
                        if (targetEntity != null && targetGrids) // Target grids must be enabled
                        {
                            bool isLargeGrid = targetEntity is IMyCubeGrid && ((IMyCubeGrid)targetEntity).GridSizeEnum == VRage.Game.MyCubeSize.Large;
                            bool isSmallGrid = targetEntity is IMyCubeGrid && ((IMyCubeGrid)targetEntity).GridSizeEnum == VRage.Game.MyCubeSize.Small;

                            if ((isLargeGrid && targetLargeGrids) || (isSmallGrid && targetSmallGrids) || !(targetEntity is IMyCubeGrid))
                            {
                                lastKnownTarget = targetEntity;
                                return targetEntity;
                            }
                        }
                    }
                }
            }

            return lastKnownTarget;
        }
    }
}
