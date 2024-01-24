using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Heart_Module.Data.Scripts.HeartModule.Weapons.Setup;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding
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

        private static string GetAmmoTypeName(long ammoKey)
        {
            // Implementation
            switch (ammoKey)
            {
                case 0: return "Value A";
                case 1: return "Value B";
                case 2: return "Value C";
                default: return "Unknown Ammo";
            }
        }

        private static string GetControlTypeName(long controltypeKey)
        {
            // Implementation
            switch (controltypeKey)
            {
                case 0: return "Value A";
                case 1: return "Value B";
                case 2: return "Value C";
                default: return "Unknown Control";
            }
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
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyConveyorSorter>(IdPrefix + "HeartWeaponOptionsDivider");
                c.Label = MyStringId.GetOrCompute("=== HeartWeaponOptions ===");
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;
                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(c);
            }
            {
                ControlsHelper.CreateToggle<SorterWeaponLogic>(
                   "HeartWeaponShoot",
                   "Toogle Shoot",
                   "TargetGridsDesc",
                   (b) => b.GameLogic.GetAs<SorterWeaponLogic>().Terminal_Heart_Shoot,
                   (b, v) => b.GameLogic.GetAs<SorterWeaponLogic>().Terminal_Heart_Shoot = v
                   );
            }
            {
                var ControlComboBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyConveyorSorter>(IdPrefix + "HeartControlComboBox");
                ControlComboBox.Title = MyStringId.GetOrCompute("Control Type");
                ControlComboBox.Tooltip = MyStringId.GetOrCompute("HeartControlComboBoxDesc");
                ControlComboBox.SupportsMultipleBlocks = true;
                ControlComboBox.Visible = CustomVisibleCondition;

                // Link the combobox to the Terminal_Heart_ControlComboBox property
                ControlComboBox.Getter = (b) => b.GameLogic.GetAs<SorterWeaponLogic>().Terminal_ControlType_ComboBox;
                ControlComboBox.Setter = (b, key) => b.GameLogic.GetAs<SorterWeaponLogic>().Terminal_ControlType_ComboBox = key;
                ControlComboBox.ComboBoxContent = (list) =>
                {
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Value A") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Value B") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 2, Value = MyStringId.GetOrCompute("Value C") });
                };

                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(ControlComboBox);
            }
            {
                var AmmoComboBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyConveyorSorter>(IdPrefix + "HeartAmmoComboBox");
                AmmoComboBox.Title = MyStringId.GetOrCompute("Ammo Type");
                AmmoComboBox.Tooltip = MyStringId.GetOrCompute("HeartAmmoComboBoxDesc");
                AmmoComboBox.SupportsMultipleBlocks = true;
                AmmoComboBox.Visible = CustomVisibleCondition;

                // Link the combobox to the Terminal_Heart_AmmoComboBox property
                AmmoComboBox.Getter = (b) => b.GameLogic.GetAs<SorterWeaponLogic>().Terminal_Heart_AmmoComboBox;
                AmmoComboBox.Setter = (b, key) => b.GameLogic.GetAs<SorterWeaponLogic>().Terminal_Heart_AmmoComboBox = key;
                AmmoComboBox.ComboBoxContent = (list) =>
                {
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Value A") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Value B") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 2, Value = MyStringId.GetOrCompute("Value C") });
                };

                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(AmmoComboBox);
            }
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyConveyorSorter>(IdPrefix + "HeartWeaponTargetingOptionsDivider");
                c.Label = MyStringId.GetOrCompute("=== HeartWeaponTargetingOptions === ");
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;
                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(c);
            }
            {
                ControlsHelper.CreateSlider<SorterTurretLogic>(
                    "HeartAIRange",
                    "AI Range",
                    "HeartSliderDesc",
                    0,
                    10000,
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_Range_Slider,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_Range_Slider = v,
                    (b, sb) => sb.Append($"Current value: {Math.Round(b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_Range_Slider)}")
                    )
                    .SetLimits(
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Definition.Targeting.MinTargetingRange,
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Definition.Targeting.MaxTargetingRange
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetUnique",
                    "Prefer Unique Targets",
                    "TargetUniqueDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_PreferUniqueTargets,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_PreferUniqueTargets = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetGrids",
                    "Target Grids",
                    "TargetGridsDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetGrids,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetGrids = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetLargeGrids",
                    "Target Large Grids",
                    "TargetLargeGridsDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetLargeGrids,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetLargeGrids = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetSmallGrids",
                    "Target Small Grids",
                    "TargetSmallGridsDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetSmallGrids,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetSmallGrids = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetProjectiles",
                    "Target Projectiles",
                    "TargetProjectilesDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetProjectiles,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetProjectiles = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetCharacters",
                    "Target Characters",
                    "TargetCharactersDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetCharacters,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetCharacters = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetFriendlies",
                    "Target Friendlies",
                    "TargetFriendliesDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetFriendlies,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetFriendlies = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetNeutrals",
                    "Target Neutrals",
                    "TargetNeutralsDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetNeutrals,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetNeutrals = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetEnemies",
                    "Target Enemies",
                    "TargetEnemiesDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetEnemies,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetEnemies = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetUnowned",
                    "Target Unowned",
                    "TargetUnownedDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetUnowned,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_TargetUnowned = v
                    );
            }
        }

        static void CreateActions(IMyModContext context)
        {
            {
                ControlsHelper.CreateAction<SorterWeaponLogic>(
                    "ToggleShoot",
                    "Toggle Shoot",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Shoot" option and ensure sync
                            logic.Terminal_Heart_Shoot = !logic.Terminal_Heart_Shoot; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Terminal_Heart_Shoot ? "Shoot ON" : "Shoot OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterWeaponLogic>(
                    "HeartControlType",
                    "Control Type",
                    (b) => b.GameLogic.GetAs<SorterWeaponLogic>().CycleControlType(true),
                    (b, sb) => sb.Append($"{GetControlTypeName(b.GameLogic.GetAs<SorterWeaponLogic>().Terminal_ControlType_ComboBox)}"),
                    @"Textures\GUI\Icons\Actions\MovingObjectToggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterWeaponLogic>(
                    "HeartCycleAmmoForward",
                    "Cycle Ammo",
                    (b) => b.GameLogic.GetAs<SorterWeaponLogic>().CycleAmmoType(true),
                    (b, sb) => sb.Append($"{GetAmmoTypeName(b.GameLogic.GetAs<SorterWeaponLogic>().Terminal_Heart_AmmoComboBox)}"),
                    @"Textures\GUI\Icons\Actions\MissileToggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "IncreaseAIRange",
                    "Increase AI Range",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            logic.IncreaseAIRange(); // Custom method to increase AI range
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append($"{logic.Terminal_Heart_Range_Slider} Range");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Increase.dds"
                    );

                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "DecreaseAIRange",
                    "Decrease AI Range",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                            logic.DecreaseAIRange(); // Custom method to decrease AI range
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                            sb.Append($"{logic.Terminal_Heart_Range_Slider} Range");
                    },
                    @"Textures\GUI\Icons\Actions\Decrease.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleUniqueTargets",
                    "Toggle Prefer Unique",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                            logic.Terminal_Heart_PreferUniqueTargets = !logic.Terminal_Heart_PreferUniqueTargets; // Toggling the value
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                            sb.Append(logic.Terminal_Heart_PreferUniqueTargets ? "Grid ON" : "Grid OFF");
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetGrids",
                    "Toggle Target Grids",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Grids" option and ensure sync
                            logic.Terminal_Heart_TargetGrids = !logic.Terminal_Heart_TargetGrids; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Terminal_Heart_TargetGrids ? "Grid ON" : "Grid OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetLargeGrids",
                    "Toggle Target Large Grids",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Large Grids" option and ensure sync
                            logic.Terminal_Heart_TargetLargeGrids = !logic.Terminal_Heart_TargetLargeGrids; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Terminal_Heart_TargetLargeGrids ? "LGrid ON" : "LGrid OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetSmallGrids",
                    "Toggle Target Small Grids",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Small Grids" option and ensure sync
                            logic.Terminal_Heart_TargetSmallGrids = !logic.Terminal_Heart_TargetSmallGrids; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Terminal_Heart_TargetSmallGrids ? "SGrid ON" : "SGrid OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetProjectiles",
                    "Toggle Target Projectiles",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the targeting of projectiles and ensure sync
                            logic.Terminal_Heart_TargetProjectiles = !logic.Terminal_Heart_TargetProjectiles; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Terminal_Heart_TargetProjectiles ? "Proj. ON" : "Proj. OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetCharacters",
                    "Toggle Target Characters",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Characters" option and ensure sync
                            logic.Terminal_Heart_TargetCharacters = !logic.Terminal_Heart_TargetCharacters; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Terminal_Heart_TargetCharacters ? "Char. ON" : "Char. OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetFriendlies",
                    "Toggle Target Friendlies",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Friendlies" option and ensure sync
                            logic.Terminal_Heart_TargetFriendlies = !logic.Terminal_Heart_TargetFriendlies; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Terminal_Heart_TargetFriendlies ? "Fr. ON" : "Fr. OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetNeutrals",
                    "Toggle Target Neutrals",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Neutrals" option and ensure sync
                            logic.Terminal_Heart_TargetNeutrals = !logic.Terminal_Heart_TargetNeutrals; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Terminal_Heart_TargetNeutrals ? "Neu. ON" : "Neu. OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetEnemies",
                    "Toggle Target Enemies",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Enemies" option and ensure sync
                            logic.Terminal_Heart_TargetEnemies = !logic.Terminal_Heart_TargetEnemies; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Terminal_Heart_TargetEnemies ? "Enem. ON" : "Enem. OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetUnowned",
                    "Toggle Target Unowned",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Unowned" option and ensure sync
                            logic.Terminal_Heart_TargetUnowned = !logic.Terminal_Heart_TargetUnowned; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Terminal_Heart_TargetUnowned ? "Unow. ON" : "Unow. OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
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