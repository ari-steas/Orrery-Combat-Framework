using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Hiding
{
    // In this example we're hiding the "Detect asteroids" terminal control and terminal action. Also bonus, enforcing it to stay false.
    //  All this only on a specific sensor block to show doing it properly without breaking other mods trying to do the same.
    //
    // This is also compatible with multiple mods doing the same thing on the same type, but for different subtypes.
    //   For example another mod could have the same on largegrid sensor to hide a different control, or even the same control, it would work properly.


    // For important notes about terminal controls see: https://github.com/THDigi/SE-ModScript-Examples/blob/master/Data/Scripts/Examples/TerminalControls/Adding/GyroTerminalControls.cs#L21-L35
    public static class HideSorterControls
    {
        static bool Done = false;

        public static void DoOnce() // called by SensorLogic.cs
        {
            if (Done)
                return;
            //MyAPIGateway.Utilities.ShowNotification("DoOnce called");
            Done = true;

            EditControls();
            EditActions();
        }

        static bool AppendedCondition(IMyTerminalBlock block)
        {
            // if block has this gamelogic component then return false to hide the control/action.
            return block?.GameLogic?.GetAs<ConveyorSorterLogic>() == null;
        }

        static void EditControls()
        {
            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyConveyorSorter>(out controls);

            foreach (IMyTerminalControl c in controls)
            {
                switch (c.Id)
                {
                    case "DrainAll":
                    case "blacklistWhitelist":
                    case "CurrentList":
                    case "removeFromSelectionButton":
                    case "candidatesList":
                    case "addToSelectionButton":
                        {
                            // appends a custom condition after the original condition with an AND.
                            MyAPIGateway.Utilities.ShowNotification("Removing terminal actions!!");
                            // pick which way you want it to work:
                            //c.Enabled = TerminalChainedDelegate.Create(c.Enabled, AppendedCondition); // grays out
                            c.Visible = TerminalChainedDelegate.Create(c.Visible, AppendedCondition); // hides
                            break;
                        }
                }
            }
        }

        static void EditActions()
        {
            List<IMyTerminalAction> actions;
            MyAPIGateway.TerminalControls.GetActions<IMyConveyorSorter>(out actions);

            foreach (IMyTerminalAction a in actions)
            {
                switch (a.Id)
                {
                    case "DrainAll":
                    case "DrainAll_On":
                    case "DrainAll_Off":
                        {
                            // appends a custom condition after the original condition with an AND.

                            a.Enabled = TerminalChainedDelegate.Create(a.Enabled, AppendedCondition);
                            // action.Enabled hides it, there is no grayed-out for actions.

                            break;
                        }
                }
            }
        }
    }
}