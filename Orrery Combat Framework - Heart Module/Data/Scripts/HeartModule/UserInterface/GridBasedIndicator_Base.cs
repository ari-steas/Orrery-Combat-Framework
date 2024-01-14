using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.UserInterface
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal abstract class GridBasedIndicator_Base : MySessionComponentBase
    {
        internal IMyCubeGrid controlledGrid;

        public override void UpdateAfterSimulation()
        {
            IMyEntity controlledEntity = MyAPIGateway.Session.Player?.Controller?.ControlledEntity?.Entity?.GetTopMostParent(); // Get the currently controlled grid.
            if (!(controlledEntity is IMyCubeGrid))
            {
                controlledGrid = null;
                return;
            }

            controlledGrid = (IMyCubeGrid)controlledEntity; // TODO: Make work on subparts

            foreach (var gridWeapon in WeaponManager.I.GridWeapons[controlledGrid])
                PerWeaponUpdate(gridWeapon);
        }

        public abstract void PerWeaponUpdate(SorterWeaponLogic weapon);
    }
}
