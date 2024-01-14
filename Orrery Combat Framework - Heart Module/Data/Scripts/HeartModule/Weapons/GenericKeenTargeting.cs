using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.ModAPI;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    public class GenericKeenTargeting
    {
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
                MyShipController activeController = GetActiveController(myCubeGrid);
                if (activeController != null)
                {
                    MyEntity targetEntity = GetLockedTarget(activeController);
                    if (targetEntity != null && targetGrids)
                    {
                        return FilterTargetBasedOnGridSize(targetEntity, targetLargeGrids, targetSmallGrids);
                    }
                }
            }

            return null;
        }

        private MyShipController GetActiveController(MyCubeGrid myCubeGrid)
        {
            foreach (var block in myCubeGrid.GetFatBlocks<MyShipController>())
            {
                if (block.NeedsPerFrameUpdate)
                {
                    return block;
                }
            }
            return null;
        }

        private MyEntity GetLockedTarget(MyShipController controller)
        {
            var targetLockingComponent = controller.Pilot.Components.Get<MyTargetLockingComponent>();
            if (targetLockingComponent != null && targetLockingComponent.IsTargetLocked)
            {
                return targetLockingComponent.TargetEntity;
            }
            return null;
        }

        private MyEntity FilterTargetBasedOnGridSize(MyEntity targetEntity, bool targetLargeGrids, bool targetSmallGrids)
        {
            bool isLargeGrid = targetEntity is IMyCubeGrid && ((IMyCubeGrid)targetEntity).GridSizeEnum == VRage.Game.MyCubeSize.Large;
            bool isSmallGrid = targetEntity is IMyCubeGrid && ((IMyCubeGrid)targetEntity).GridSizeEnum == VRage.Game.MyCubeSize.Small;

            if ((isLargeGrid && targetLargeGrids) || (isSmallGrid && targetSmallGrids) || !(targetEntity is IMyCubeGrid))
            {
                return targetEntity;
            }
            return null;
        }
    }
}
