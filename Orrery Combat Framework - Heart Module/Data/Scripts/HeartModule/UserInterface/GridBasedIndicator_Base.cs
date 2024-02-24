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
        public bool Visible = true;

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (MyAPIGateway.Utilities.IsDedicated || !HeartData.I.IsLoaded)
            {
                Visible = false;
                return;
            }

            Visible = MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.None;

            if (!Visible)
                return;

            IMyEntity controlledEntity = MyAPIGateway.Session.Player?.Controller?.ControlledEntity?.Entity?.GetTopMostParent();
            if (!(controlledEntity is IMyCubeGrid))
            {
                controlledGrid = null;
                return;
            }

            controlledGrid = (IMyCubeGrid)controlledEntity;

            foreach (var weaponLogic in WeaponManager.I.GridWeapons[controlledGrid])
            {
                // Check if the HUD Barrel Indicator is enabled for this weapon
                if (weaponLogic.HudBarrelIndicatorState)
                {
                    PerWeaponUpdate(weaponLogic);
                }
            }
        }

        public abstract void PerWeaponUpdate(SorterWeaponLogic weapon);
    }
}
