using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Heart_Module.Data.Scripts.HeartModule.Weapons.Setup;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Text;
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
            //CreateActions(context);
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
                ControlsHelper.CreateSlider(
                    "HeartAIRange",
                    "AI Range",
                    "HeartSliderDesc",
                    0,
                    10000,
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_Range_Slider,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_Range_Slider = v,
                    (b, sb) => sb.AppendFormat("Current value: {0}", b.GameLogic.GetAs<SorterTurretLogic>().Terminal_Heart_Range_Slider)
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

        //static void CreateActions(IMyModContext context)
        //{
        //    {
        //        var ShootAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "ToggleShoot");
        //        ShootAction.Name = new StringBuilder("Toggle Shoot");
        //        ShootAction.ValidForGroups = true;
        //        ShootAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
        //        ShootAction.Action = (b) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                // Toggle the "Shoot" option and ensure sync
        //                logic.Terminal_Heart_Shoot = !logic.Terminal_Heart_Shoot; // Toggling the value
        //                MyAPIGateway.Utilities.ShowNotification($"Shoot toggled to: {(logic.Terminal_Heart_Shoot ? "ON" : "OFF")}", 2000, "White");
        //            }
        //        };
        //        ShootAction.Writer = (b, sb) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                sb.Append(logic.Terminal_Heart_Shoot ? "Shoot ON" : "Shoot OFF");
        //            }
        //        };
        //
        //        ShootAction.Enabled = CustomVisibleCondition;
        //        MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(ShootAction);
        //    }
        //    {
        //        var cycleControlForwardAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "HeartControlType");
        //        cycleControlForwardAction.Name = new StringBuilder("Control Type");
        //        cycleControlForwardAction.Action = (b) => b.GameLogic.GetAs<SorterWeaponLogic>().CycleControlType(true);
        //        cycleControlForwardAction.Writer = (b, sb) => sb.Append($"{GetControlTypeName(b.GameLogic.GetAs<SorterWeaponLogic>().Terminal_ControlType_ComboBox)}");
        //        cycleControlForwardAction.Icon = @"Textures\GUI\Icons\Actions\MovingObjectToggle.dds";
        //        cycleControlForwardAction.Enabled = CustomVisibleCondition;
        //        MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(cycleControlForwardAction);
        //        MyAPIGateway.Utilities.ShowNotification("Control Type Cycled");
        //    }
        //    {
        //        var cycleAmmoForwardAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "HeartCycleAmmoForward");
        //        cycleAmmoForwardAction.Name = new StringBuilder("Cycle Ammo");
        //        cycleAmmoForwardAction.Action = (b) => b.GameLogic.GetAs<SorterWeaponLogic>().CycleAmmoType(true);
        //        cycleAmmoForwardAction.Writer = (b, sb) => sb.Append($"{GetAmmoTypeName(b.GameLogic.GetAs<SorterWeaponLogic>().Terminal_Heart_AmmoComboBox)}");
        //        cycleAmmoForwardAction.Icon = @"Textures\GUI\Icons\Actions\MissileToggle.dds";
        //        cycleAmmoForwardAction.Enabled = CustomVisibleCondition;
        //        MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(cycleAmmoForwardAction);
        //        MyAPIGateway.Utilities.ShowNotification("Ammo Cycled");
        //    }
        //    {
        //        // Action to Increase AI Range
        //        var IncreaseAIRangeAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "IncreaseAIRange");
        //        IncreaseAIRangeAction.Name = new StringBuilder("Increase AI Range");
        //        IncreaseAIRangeAction.ValidForGroups = true;
        //        IncreaseAIRangeAction.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
        //        IncreaseAIRangeAction.Action = (b) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                logic.IncreaseAIRange(); // Custom method to increase AI range
        //            }
        //        };
        //        IncreaseAIRangeAction.Writer = (b, sb) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                sb.Append($"{logic.Terminal_Heart_Range_Slider} Range");
        //            }
        //        };
        //        IncreaseAIRangeAction.Enabled = CustomVisibleCondition;
        //        MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(IncreaseAIRangeAction);
        //
        //        // Action to Decrease AI Range
        //        var DecreaseAIRangeAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "DecreaseAIRange");
        //        DecreaseAIRangeAction.Name = new StringBuilder("Decrease AI Range");
        //        DecreaseAIRangeAction.ValidForGroups = true;
        //        DecreaseAIRangeAction.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
        //        DecreaseAIRangeAction.Action = (b) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                logic.DecreaseAIRange(); // Custom method to decrease AI range
        //            }
        //        };
        //        DecreaseAIRangeAction.Writer = (b, sb) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                sb.Append($"{logic.Terminal_Heart_Range_Slider} Range");
        //            }
        //        };
        //        DecreaseAIRangeAction.Enabled = CustomVisibleCondition;
        //        MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(DecreaseAIRangeAction);
        //    }
        //    {
        //        var TargetGridsAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "ToggleTargetGrids");
        //        TargetGridsAction.Name = new StringBuilder("Toggle Target Grids");
        //        TargetGridsAction.ValidForGroups = true;
        //        TargetGridsAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
        //        TargetGridsAction.Action = (b) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                // Toggle the "Target Grids" option and ensure sync
        //                logic.Terminal_Heart_TargetGrids = !logic.Terminal_Heart_TargetGrids; // Toggling the value
        //                MyAPIGateway.Utilities.ShowNotification($"Target Grids toggled to: {(logic.Terminal_Heart_TargetGrids ? "ON" : "OFF")}", 2000, "White");
        //            }
        //        };
        //        TargetGridsAction.Writer = (b, sb) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                sb.Append(logic.Terminal_Heart_TargetGrids ? "Grid ON" : "Grid OFF");
        //            }
        //        };
        //
        //        TargetGridsAction.Enabled = CustomVisibleCondition;
        //        MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(TargetGridsAction);
        //    }
        //    {
        //        var TargetLargeGridsAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "ToggleTargetLargeGrids");
        //        TargetLargeGridsAction.Name = new StringBuilder("Toggle Target Large Grids");
        //        TargetLargeGridsAction.ValidForGroups = true;
        //        TargetLargeGridsAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
        //        TargetLargeGridsAction.Action = (b) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                // Toggle the "Target Large Grids" option and ensure sync
        //                logic.Terminal_Heart_TargetLargeGrids = !logic.Terminal_Heart_TargetLargeGrids; // Toggling the value
        //                MyAPIGateway.Utilities.ShowNotification($"Target Large Grids toggled to: {(logic.Terminal_Heart_TargetLargeGrids ? "ON" : "OFF")}", 2000, "White");
        //            }
        //        };
        //        TargetLargeGridsAction.Writer = (b, sb) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                sb.Append(logic.Terminal_Heart_TargetLargeGrids ? "LGrid ON" : "LGrid OFF");
        //            }
        //        };
        //
        //        TargetLargeGridsAction.Enabled = CustomVisibleCondition;
        //        MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(TargetLargeGridsAction);
        //    }
        //    {
        //        var TargetSmallGridsAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "ToggleTargetSmallGrids");
        //        TargetSmallGridsAction.Name = new StringBuilder("Toggle Target Small Grids");
        //        TargetSmallGridsAction.ValidForGroups = true;
        //        TargetSmallGridsAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
        //        TargetSmallGridsAction.Action = (b) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                // Toggle the "Target Small Grids" option and ensure sync
        //                logic.Terminal_Heart_TargetSmallGrids = !logic.Terminal_Heart_TargetSmallGrids; // Toggling the value
        //                MyAPIGateway.Utilities.ShowNotification($"Target Small Grids toggled to: {(logic.Terminal_Heart_TargetSmallGrids ? "ON" : "OFF")}", 2000, "White");
        //            }
        //        };
        //        TargetSmallGridsAction.Writer = (b, sb) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                sb.Append(logic.Terminal_Heart_TargetSmallGrids ? "SGrid ON" : "SGrid OFF");
        //            }
        //        };
        //
        //        TargetSmallGridsAction.Enabled = CustomVisibleCondition;
        //        MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(TargetSmallGridsAction);
        //    }
        //    {
        //        var TargetProjectilesAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "ToggleTargetProjectiles");
        //        TargetProjectilesAction.Name = new StringBuilder("Toggle Target Projectiles");
        //        TargetProjectilesAction.ValidForGroups = true;
        //        TargetProjectilesAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
        //        TargetProjectilesAction.Action = (b) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                // Toggle the targeting of projectiles and ensure sync
        //                logic.Terminal_Heart_TargetProjectiles = !logic.Terminal_Heart_TargetProjectiles; // Toggling the value
        //                MyAPIGateway.Utilities.ShowNotification($"Target Projectiles toggled to: {(logic.Terminal_Heart_TargetProjectiles ? "ON" : "OFF")}", 2000, "White");
        //            }
        //        };
        //        TargetProjectilesAction.Writer = (b, sb) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                sb.Append(logic.Terminal_Heart_TargetProjectiles ? "Proj. ON" : "Proj. OFF");
        //            }
        //        };
        //
        //        TargetProjectilesAction.Enabled = CustomVisibleCondition;
        //        MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(TargetProjectilesAction);
        //    }
        //    {
        //        var TargetCharactersAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "ToggleTargetCharacters");
        //        TargetCharactersAction.Name = new StringBuilder("Toggle Target Characters");
        //        TargetCharactersAction.ValidForGroups = true;
        //        TargetCharactersAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
        //        TargetCharactersAction.Action = (b) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                // Toggle the "Target Characters" option and ensure sync
        //                logic.Terminal_Heart_TargetCharacters = !logic.Terminal_Heart_TargetCharacters; // Toggling the value
        //                MyAPIGateway.Utilities.ShowNotification($"Target Characters toggled to: {(logic.Terminal_Heart_TargetCharacters ? "ON" : "OFF")}", 2000, "White");
        //            }
        //        };
        //        TargetCharactersAction.Writer = (b, sb) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                sb.Append(logic.Terminal_Heart_TargetCharacters ? "Char. ON" : "Char. OFF");
        //            }
        //        };
        //
        //        TargetCharactersAction.Enabled = CustomVisibleCondition;
        //        MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(TargetCharactersAction);
        //    }
        //    {
        //        var TargetFriendliesAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "ToggleTargetFriendlies");
        //        TargetFriendliesAction.Name = new StringBuilder("Toggle Target Friendlies");
        //        TargetFriendliesAction.ValidForGroups = true;
        //        TargetFriendliesAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
        //        TargetFriendliesAction.Action = (b) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                // Toggle the "Target Friendlies" option and ensure sync
        //                logic.Terminal_Heart_TargetFriendlies = !logic.Terminal_Heart_TargetFriendlies; // Toggling the value
        //                MyAPIGateway.Utilities.ShowNotification($"Target Friendlies toggled to: {(logic.Terminal_Heart_TargetFriendlies ? "ON" : "OFF")}", 2000, "White");
        //            }
        //        };
        //        TargetFriendliesAction.Writer = (b, sb) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                sb.Append(logic.Terminal_Heart_TargetFriendlies ? "Fr. ON" : "Fr. OFF");
        //            }
        //        };
        //
        //        TargetFriendliesAction.Enabled = CustomVisibleCondition;
        //        MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(TargetFriendliesAction);
        //    }
        //    {
        //        var TargetNeutralsAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "ToggleTargetNeutrals");
        //        TargetNeutralsAction.Name = new StringBuilder("Toggle Target Neutrals");
        //        TargetNeutralsAction.ValidForGroups = true;
        //        TargetNeutralsAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
        //        TargetNeutralsAction.Action = (b) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                // Toggle the "Target Neutrals" option and ensure sync
        //                logic.Terminal_Heart_TargetNeutrals = !logic.Terminal_Heart_TargetNeutrals; // Toggling the value
        //                MyAPIGateway.Utilities.ShowNotification($"Target Neutrals toggled to: {(logic.Terminal_Heart_TargetNeutrals ? "ON" : "OFF")}", 2000, "White");
        //            }
        //        };
        //        TargetNeutralsAction.Writer = (b, sb) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                sb.Append(logic.Terminal_Heart_TargetNeutrals ? "Neu. ON" : "Neu. OFF");
        //            }
        //        };
        //
        //        TargetNeutralsAction.Enabled = CustomVisibleCondition;
        //        MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(TargetNeutralsAction);
        //    }
        //    {
        //        var TargetEnemiesAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "ToggleTargetEnemies");
        //        TargetEnemiesAction.Name = new StringBuilder("Toggle Target Enemies");
        //        TargetEnemiesAction.ValidForGroups = true;
        //        TargetEnemiesAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
        //        TargetEnemiesAction.Action = (b) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                // Toggle the "Target Enemies" option and ensure sync
        //                logic.Terminal_Heart_TargetEnemies = !logic.Terminal_Heart_TargetEnemies; // Toggling the value
        //                MyAPIGateway.Utilities.ShowNotification($"Target Enemies toggled to: {(logic.Terminal_Heart_TargetEnemies ? "ON" : "OFF")}", 2000, "White");
        //            }
        //        };
        //        TargetEnemiesAction.Writer = (b, sb) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                sb.Append(logic.Terminal_Heart_TargetEnemies ? "Enem. ON" : "Enem. OFF");
        //            }
        //        };
        //
        //        TargetEnemiesAction.Enabled = CustomVisibleCondition;
        //        MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(TargetEnemiesAction);
        //    }
        //    {
        //        var TargetUnownedAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "ToggleTargetUnowned");
        //        TargetUnownedAction.Name = new StringBuilder("Toggle Target Unowned");
        //        TargetUnownedAction.ValidForGroups = true;
        //        TargetUnownedAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
        //        TargetUnownedAction.Action = (b) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                // Toggle the "Target Unowned" option and ensure sync
        //                logic.Terminal_Heart_TargetUnowned = !logic.Terminal_Heart_TargetUnowned; // Toggling the value
        //                MyAPIGateway.Utilities.ShowNotification($"Target Unowned toggled to: {(logic.Terminal_Heart_TargetUnowned ? "ON" : "OFF")}", 2000, "White");
        //            }
        //        };
        //        TargetUnownedAction.Writer = (b, sb) =>
        //        {
        //            var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
        //            if (logic != null)
        //            {
        //                sb.Append(logic.Terminal_Heart_TargetUnowned ? "Unow. ON" : "Unow. OFF");
        //            }
        //        };
        //
        //        TargetUnownedAction.Enabled = CustomVisibleCondition;
        //        MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(TargetUnownedAction);
        //    }
        //}


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