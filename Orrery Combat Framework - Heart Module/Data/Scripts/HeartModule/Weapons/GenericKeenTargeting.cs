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

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    public class GenericKeenTargeting
    {
        private MyEntity lastKnownTarget = null;

        public MyEntity GetTarget(IMyCubeGrid grid)
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

                // Iterate over all ship controllers on the grid
                foreach (var block in myCubeGrid.GetFatBlocks<MyShipController>())
                {
                    if (block.NeedsPerFrameUpdate)   //this is the most reliable way to get the main cockpit without calling the main cockpit apparently
                    {
                        activeController = block;
                        break; // Break the loop once the active controller is found
                    }
                }

                if (activeController != null && activeController.Pilot != null)
                {
                    var targetLockingComponent = activeController.Pilot.Components.Get<MyTargetLockingComponent>();
                    if (targetLockingComponent != null && targetLockingComponent.IsTargetLocked)
                    {
                        var targetEntity = targetLockingComponent.TargetEntity;
                        if (targetEntity != null)
                        {
                            lastKnownTarget = targetEntity;
                            MyAPIGateway.Utilities.ShowNotification($"Target locked: {targetEntity.DisplayName}", 1000 / 60, VRage.Game.MyFontEnum.Green);
                            return targetEntity;
                        }
                    }
                }
            }

            return lastKnownTarget;
        }
    }
}
