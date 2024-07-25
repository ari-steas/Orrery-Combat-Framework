using Heart_Module.Data.Scripts.HeartModule.Definitions.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Heart_Module.Data.Scripts.HeartModule.Weapons.Setup;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.Setup.Adding
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

            MyAPIGateway.TerminalControls.CustomControlGetter += CustomControlGetter;
        }

        private static void CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            SorterWeaponLogic logic = block?.GameLogic?.GetAs<SorterWeaponLogic>();
            if (logic == null)
                return;

            foreach (var control in controls)
            {
                if (control.Id == (IdPrefix + "HeartAmmoComboBox")) // Set ammos based on availability
                {
                    ((IMyTerminalControlCombobox)control).ComboBoxContent = (list) =>
                    {
                        for (int i = 0; i < logic.Definition.Loading.Ammos.Length; i++)
                            list.Add(new MyTerminalControlComboBoxItem() { Key = i, Value = MyStringId.GetOrCompute(logic.Definition.Loading.Ammos[i]) });
                    };
                    break;
                }
            }
        }

        static bool CustomVisibleCondition(IMyTerminalBlock b)
        {
            // only visible for the blocks having this gamelogic comp
            return b?.GameLogic?.GetAs<SorterWeaponLogic>() != null;
        }

        /// <summary>
        /// Return the ammo name of a given projectile.
        /// </summary>
        /// <param name="ammoKey"></param>
        /// <returns></returns>
        private static string GetAmmoTypeName(long ammoKey)
        {
            if (ProjectileDefinitionManager.HasDefinition((int)ammoKey))
                return ProjectileDefinitionManager.GetDefinition((int)ammoKey).Name;
            return "Unknown Ammo";
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
                   (b) => b.GameLogic.GetAs<SorterWeaponLogic>().ShootState,
                   (b, v) => b.GameLogic.GetAs<SorterWeaponLogic>().ShootState = v
                   );
            }
            {
                ControlsHelper.CreateToggle<SorterWeaponLogic>(
                   "HeartWeaponMouseShoot",
                   "Toogle Mouse Shoot",
                   "TargetGridsDesc",
                   (b) => b.GameLogic.GetAs<SorterWeaponLogic>().MouseShootState,
                   (b, v) => b.GameLogic.GetAs<SorterWeaponLogic>().MouseShootState = v
                   );
            }
            //{
            //    var ControlComboBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyConveyorSorter>(IdPrefix + "HeartControlComboBox");
            //    ControlComboBox.Title = MyStringId.GetOrCompute("Control Type");
            //    ControlComboBox.Tooltip = MyStringId.GetOrCompute("HeartControlComboBoxDesc");
            //    ControlComboBox.SupportsMultipleBlocks = true;
            //    ControlComboBox.Visible = CustomVisibleCondition;
            //
            //    // Link the combobox to the Terminal_Heart_ControlComboBox property
            //    ControlComboBox.Getter = (b) => b.GameLogic.GetAs<SorterWeaponLogic>().Terminal_ControlType_ComboBox;
            //    ControlComboBox.Setter = (b, key) => b.GameLogic.GetAs<SorterWeaponLogic>().Terminal_ControlType_ComboBox = key;
            //    ControlComboBox.ComboBoxContent = (list) =>
            //    {
            //        list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Value A") });
            //        list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Value B") });
            //        list.Add(new MyTerminalControlComboBoxItem() { Key = 2, Value = MyStringId.GetOrCompute("Value C") });
            //    };
            //
            //    MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(ControlComboBox);
            //}
            {
                var AmmoComboBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyConveyorSorter>(IdPrefix + "HeartAmmoComboBox");
                AmmoComboBox.Title = MyStringId.GetOrCompute("Ammo Type");
                AmmoComboBox.Tooltip = MyStringId.GetOrCompute("HeartAmmoComboBoxDesc");
                AmmoComboBox.SupportsMultipleBlocks = true;
                AmmoComboBox.Visible = CustomVisibleCondition;

                // Link the combobox to the Terminal_Heart_AmmoComboBox property
                AmmoComboBox.Getter = (b) =>
                {
                    var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
                    if (logic != null)
                    {
                        return logic.Magazines.SelectedAmmoIndex;
                    }
                    return -1; // Return a default value (e.g., -1) when the index is out of bounds
                };
                AmmoComboBox.Setter = (b, key) => b.GameLogic.GetAs<SorterWeaponLogic>().AmmoComboBox = (int)key;
                //AmmoComboBox.ComboBoxContent = HeartData.I.AmmoComboBoxSetter; // Set combo box based on what's open

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
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().AiRange,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().AiRange = v,
                    (b, sb) => sb.Append($"Current value: {Math.Round(b.GameLogic.GetAs<SorterTurretLogic>().AiRange)}")
                    )
                    .SetLimits(
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Definition.Targeting.MinTargetingRange,
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Definition.Targeting.MaxTargetingRange
                    );
            }
            {
                ControlsHelper.CreateButton<SorterTurretLogic>(
                    "ResetTargetButton",
                    "Reset Target",
                    "Resets the current target of the weapon",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            logic.ResetTarget();
                        }
                    }
                );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetUnique",
                    "Prefer Unique Targets",
                    "TargetUniqueDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().PreferUniqueTargetsState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().PreferUniqueTargetsState = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetGrids",
                    "Target Grids",
                    "TargetGridsDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().TargetGridsState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().TargetGridsState = v,
                    // Hide controls if not allowed to target
                    (b) => ((b.GameLogic?.GetAs<SorterTurretLogic>()?.Definition.Targeting.AllowedTargetTypes ?? 0) & TargetType_Enum.TargetGrids) == TargetType_Enum.TargetGrids
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetLargeGrids",
                    "Target Large Grids",
                    "TargetLargeGridsDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().TargetLargeGridsState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().TargetLargeGridsState = v,
                    (b) => ((b.GameLogic?.GetAs<SorterTurretLogic>()?.Definition.Targeting.AllowedTargetTypes ?? 0) & TargetType_Enum.TargetGrids) == TargetType_Enum.TargetGrids
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetSmallGrids",
                    "Target Small Grids",
                    "TargetSmallGridsDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().TargetSmallGridsState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().TargetSmallGridsState = v,
                    (b) => ((b.GameLogic?.GetAs<SorterTurretLogic>()?.Definition.Targeting.AllowedTargetTypes ?? 0) & TargetType_Enum.TargetGrids) == TargetType_Enum.TargetGrids
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetProjectiles",
                    "Target Projectiles",
                    "TargetProjectilesDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().TargetProjectilesState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().TargetProjectilesState = v,
                    (b) => ((b.GameLogic?.GetAs<SorterTurretLogic>()?.Definition.Targeting.AllowedTargetTypes ?? 0) & TargetType_Enum.TargetProjectiles) == TargetType_Enum.TargetProjectiles
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetCharacters",
                    "Target Characters",
                    "TargetCharactersDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().TargetCharactersState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().TargetCharactersState = v,
                    (b) => ((b.GameLogic?.GetAs<SorterTurretLogic>()?.Definition.Targeting.AllowedTargetTypes ?? 0) & TargetType_Enum.TargetCharacters) == TargetType_Enum.TargetCharacters
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetFriendlies",
                    "Target Friendlies",
                    "TargetFriendliesDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().TargetFriendliesState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().TargetFriendliesState = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetNeutrals",
                    "Target Neutrals",
                    "TargetNeutralsDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().TargetNeutralsState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().TargetNeutralsState = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetEnemies",
                    "Target Enemies",
                    "TargetEnemiesDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().TargetEnemiesState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().TargetEnemiesState = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetUnowned",
                    "Target Unowned",
                    "TargetUnownedDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().TargetUnownedState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().TargetUnownedState = v
                    );
            }
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyConveyorSorter>(IdPrefix + "HeartWeaponHUDDivider");
                c.Label = MyStringId.GetOrCompute("=== HUD ===");
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;
                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(c);
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartHUDBarrelIndicatorToggle",
                    "HUD Barrel Indicator",
                    "HUDBarrelIndicatorDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().HudBarrelIndicatorState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().HudBarrelIndicatorState = v
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
                            logic.ShootState = !logic.ShootState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.ShootState ? "Shoot ON" : "Shoot OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterWeaponLogic>(
                    "ToggleMouseShoot",
                    "Toggle Mouse Shoot",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Shoot" option and ensure sync
                            logic.MouseShootState = !logic.MouseShootState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.MouseShootState ? "Mouse ON" : "Mouse OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            //{
            //    ControlsHelper.CreateAction<SorterWeaponLogic>(
            //        "HeartControlType",
            //        "Control Type",
            //        (b) => b.GameLogic.GetAs<SorterWeaponLogic>().CycleControlType(true),
            //        (b, sb) => sb.Append($"{GetControlTypeName(b.GameLogic.GetAs<SorterWeaponLogic>().Terminal_ControlType_ComboBox)}"),
            //        @"Textures\GUI\Icons\Actions\MovingObjectToggle.dds"
            //        );
            //}
            {
                ControlsHelper.CreateAction<SorterWeaponLogic>(
                    "HeartCycleAmmoForward",
                    "Cycle Ammo",
                    (b) => b.GameLogic.GetAs<SorterWeaponLogic>().CycleAmmoType(true),
                    (b, sb) => sb.Append($"{GetAmmoTypeName(b.GameLogic.GetAs<SorterWeaponLogic>().Magazines.SelectedAmmoId)}"),
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
                            sb.Append($"{logic.AiRange} Range");
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
                            sb.Append($"{logic.AiRange} Range");
                    },
                    @"Textures\GUI\Icons\Actions\Decrease.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>("ResetTarget", "Reset Target",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            logic.ResetTarget();
                        }
                    },
                    (b, sb) =>
                    {
                        sb.Append("Reset Target");
                    },
                    @"Textures\GUI\Icons\Actions\Reset.dds");
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleUniqueTargets",
                    "Toggle Prefer Unique",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                            logic.PreferUniqueTargetsState = !logic.PreferUniqueTargetsState; // Toggling the value
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                            sb.Append(logic.PreferUniqueTargetsState ? "Grid ON" : "Grid OFF");
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
                            logic.TargetGridsState = !logic.TargetGridsState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.TargetGridsState ? "Grid ON" : "Grid OFF");
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
                            logic.TargetLargeGridsState = !logic.TargetLargeGridsState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.TargetLargeGridsState ? "LGrid ON" : "LGrid OFF");
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
                            logic.TargetSmallGridsState = !logic.TargetSmallGridsState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.TargetSmallGridsState ? "SGrid ON" : "SGrid OFF");
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
                            logic.TargetProjectilesState = !logic.TargetProjectilesState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.TargetProjectilesState ? "Proj. ON" : "Proj. OFF");
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
                            logic.TargetCharactersState = !logic.TargetCharactersState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.TargetCharactersState ? "Char. ON" : "Char. OFF");
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
                            logic.TargetFriendliesState = !logic.TargetFriendliesState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.TargetFriendliesState ? "Fr. ON" : "Fr. OFF");
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
                            logic.TargetNeutralsState = !logic.TargetNeutralsState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.TargetNeutralsState ? "Neu. ON" : "Neu. OFF");
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
                            logic.TargetEnemiesState = !logic.TargetEnemiesState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.TargetEnemiesState ? "Enem. ON" : "Enem. OFF");
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
                            logic.TargetUnownedState = !logic.TargetUnownedState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.TargetUnownedState ? "Unow. ON" : "Unow. OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }

            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleHUDBarrelIndicator",
                    "Toggle HUD Barrel Indicator",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Unowned" option and ensure sync
                            logic.HudBarrelIndicatorState = !logic.HudBarrelIndicatorState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.HudBarrelIndicatorState ? "Ind. ON" : "Ind. OFF");
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