using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.UserInterface
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal abstract class GridBasedIndicator_Base : MySessionComponentBase
    {
        public override void UpdateAfterSimulation()
        {
            IMyEntity controlledEntity = MyAPIGateway.Session.Player?.Controller?.ControlledEntity?.Entity?.GetTopMostParent(); // Get the currently controlled grid.
            if (!(controlledEntity is IMyCubeGrid))
                return;
            IMyCubeGrid controlledGrid = (IMyCubeGrid)controlledEntity; // TODO: Make work on subparts

            MyAPIGateway.Utilities.ShowNotification("Weapons: " + (WeaponManager.I.GridWeapons[controlledGrid]?.Count), 1000 / 60);

            foreach (var gridWeapon in WeaponManager.I.GridWeapons[controlledGrid])
                PerWeaponUpdate(gridWeapon);
        }

        public abstract void PerWeaponUpdate(SorterWeaponLogic weapon);
    }
}
