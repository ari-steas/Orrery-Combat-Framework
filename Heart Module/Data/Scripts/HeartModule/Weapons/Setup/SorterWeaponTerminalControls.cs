using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Text;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons
{
    // Example of adding terminal controls and actions to a specific gyro subtype.
    // It can be generalized to the entire type by simply not using the visible-filtering methods.


    /*
     * Important notes about controls/actions:
     * 
     * 1. They are global per block type! Not per block instance, not per block type+subtype.
     * Which is why they need to be added once per world and isolated to avoid accidental use of instanced things from a gamelogic.
     * 
     * 2. Should only be retrieved/edited/added after the block type fully spawned because of game bugs.
     * Simplest way is with a gamelogic component via first update (not Init(), that's too early).
     * 
     * 3. They're only UI! They do not save nor sync anything, they only read and call things locally.
     * That means you have to roll your own implementation of saving and synchronizing the data.
     * 
     * Also keep in mind that these can be called by mods and PBs, which also includes being called dedicated-server-side.
     * Make sure your backend code does all the checks, including ensuring limits for sliders and such.
     */

    public static class SorterWeaponTerminalControls
    {
        const string IdPrefix = "ModularHeartMod_"; // highly recommended to tag your properties/actions like this to avoid colliding with other mods'

        static bool Done = false;

        // just to clarify, don't store your states/values here, those should be per block and not static.

        public static void DoOnce(IMyModContext context) // called by GyroLogic.cs
        {
            if (Done)
                return;
            Done = true;

            // these are all the options and they're not all required so use only what you need.
            CreateControls();
            CreateActions(context);
            CreateProperties();
        }

        static bool CustomVisibleCondition(IMyTerminalBlock b)
        {
            // only visible for the blocks having this gamelogic comp
            return b?.GameLogic?.GetAs<SorterWeaponLogic>() != null;
        }

        static void CreateControls()
        {
            // all the control types:
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyConveyorSorter>(""); // separators don't store the id
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(c);
            }
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyConveyorSorter>(IdPrefix + "HeartWeaponOptions");
                c.Label = MyStringId.GetOrCompute("HeartWeaponOptions");
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(c);
            }
            {
                var ShootToggle = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyConveyorSorter>(IdPrefix + "Shoot");
                ShootToggle.Title = MyStringId.GetOrCompute("Toogle Shoot");
                ShootToggle.Tooltip = MyStringId.GetOrCompute("This does some stuff!");
                ShootToggle.SupportsMultipleBlocks = true; // wether this control should be visible when multiple blocks are selected (as long as they all have this control).

                // callbacks to determine if the control should be visible or not-grayed-out(Enabled) depending on whatever custom condition you want, given a block instance.
                // optional, they both default to true.
                ShootToggle.Visible = CustomVisibleCondition;
                //c.Enabled = CustomVisibleCondition;

                ShootToggle.OnText = MySpaceTexts.SwitchText_On;
                ShootToggle.OffText = MySpaceTexts.SwitchText_Off;
                //c.OffText = MyStringId.GetOrCompute("Off");

                // setters and getters should both be assigned on all controls that have them, to avoid errors in mods or PB scripts getting exceptions from them.
                ShootToggle.Getter = (b) => b.GameLogic.GetAs<SorterWeaponLogic>().ShootState.Value;  // Getting the value
                ShootToggle.Setter = (b, v) => b.GameLogic.GetAs<SorterWeaponLogic>().ShootState.Value = v; // Setting the value


                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(ShootToggle);
            }
        }

        static void CreateActions(IMyModContext context)
        {
            var ShootToggleAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "ToggleShoot");

            ShootToggleAction.Name = new StringBuilder("Toggle Shoot");

            // If the action is visible for grouped blocks (as long as they all have this action).
            ShootToggleAction.ValidForGroups = true;

            // The icon shown in the list and top-right of the block icon in toolbar.
            ShootToggleAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";

            // Called when the toolbar slot is triggered
            ShootToggleAction.Action = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
                if (logic != null && logic.ShootState != null)
                {
                    // Toggle the shoot state and ensure sync
                    logic.ShootState.Value = !logic.ShootState.Value;  // Toggling the value
                    MyAPIGateway.Utilities.ShowNotification($"Shoot Action toggled to: {(logic.ShootState.Value ? "ON" : "OFF")}", 2000, "White");
                }
            };

            // Define what the action's tooltip/status text should say
            ShootToggleAction.Writer = (b, sb) =>
            {
                var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
                if (logic != null && logic.ShootState != null)
                {
                    sb.Append(logic.ShootState.Value ? "Shooting" : "Not Shooting");
                }
            };

            ShootToggleAction.Enabled = CustomVisibleCondition;

            MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(ShootToggleAction);
        }


        static void CreateProperties()
        {
            // Terminal controls automatically generate properties like these, but you can also add new ones manually without the GUI counterpart.
            // The main use case is for PB to be able to read them.
            // The type given is only limited by access, can only do SE or .NET types, nothing custom (except methods because the wrapper Func/Action is .NET).
            // For APIs, one can send a IReadOnlyDictionary<string, Delegate> for a list of callbacks. Just be sure to use a ImmutableDictionary to avoid getting your API hijacked.
            {
                var p = MyAPIGateway.TerminalControls.CreateProperty<Vector3, IMyConveyorSorter>(IdPrefix + "SampleProp");
                // SupportsMultipleBlocks, Enabled and Visible don't have a use for this, and Title/Tooltip don't exist.

                p.Getter = (b) =>
                {
                    float interferrence;
                    Vector3 gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(b.GetPosition(), out interferrence);
                    return gravity;
                };

                p.Setter = (b, v) =>
                {
                };

                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(p);


                // a mod or PB can use it like:
                //Vector3 vec = gyro.GetValue<Vector3>("YourMod_SampleProp");
                // just careful with sending mutable reference types, there's no serialization inbetween so the mod/PB can mutate your reference.
            }
        }
    }
}