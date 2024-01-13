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
                var mainCockpit = myCubeGrid.MainCockpit as IMyCockpit;
                if (mainCockpit != null && mainCockpit.Pilot != null)
                {
                    var targetLockingComponent = mainCockpit.Pilot.Components.Get<MyTargetLockingComponent>();
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
