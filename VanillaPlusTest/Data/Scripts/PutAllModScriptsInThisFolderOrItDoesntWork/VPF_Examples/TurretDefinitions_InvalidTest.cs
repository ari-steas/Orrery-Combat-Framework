using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.EntityComponents;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using VanillaPlusFramework.TemplateClasses;
using static VanillaPlusFramework.TemplateClasses.TargetFlags;
using static VanillaPlusFramework.TemplateClasses.FuelType;

namespace Template.VPFDefinitions /// Set namespace name to something else, preferably something no other mod uses. Can be the same as other definitions
{
    /// <summary>
    /// Recommend renaming the file.
    /// Recommend Visual Studio for editing this, all fields have descriptions that visual studio will display on hover over. Make a project solution to put all files in as well so the descriptions show up when hovered over.
    /// Contains all Vanilla+ Stats for all subtypes. Note: Vanilla Ammo.sbc is still used
    /// Note: All implented stats are here. There are some unimplimented ones not shown, do not use them or errors will be thrown.
    /// You can have multiple files, just change the class name ("VPFAmmoDefinitions" in front of public class) or the namespace
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class VPFTurretDefinitions : MySessionComponentBase
    {
        List<VPFTurretDefinition> TurretDefinitions = new List<VPFTurretDefinition>() {
            /*
             * DON'T MODIFY ANYTHING ABOVE THIS LINE EXCEPT THE NAMESPACE & CLASS NAME
             */
            new VPFTurretDefinition
            {
                subtypeName = "name of subtype in Cubeblocks.sbc",

                TAI_Stats = new TurretAI_Logic
                {
                    // specifies maximum range independent of target lock, set to -1 to disable
                    TAI_ForceMaximumRange = -1,
                    // specifies minimum AI target range, set to 0 to disable
                    TAI_MinimumRange = 0,

                    // controls what targeting types will forcefully be disabled
                    DisableNoTargeting = true, // this should not be true if any of the below are true
                    DisableMeteorTargeting = true,
                    DisableMissileTargeting = true,
                    DisableSmallShipTargeting = true,
                    DisableLargeShipTargeting = true,
                    DisableCharacterTargeting = true,
                    DisableStationTargeting = true,
                    DisableFriendlyTargeting = true,
                    DisableNeutralTargeting = true,
                    DisableHostileTargeting = true,
                    DisableAIControl = true, // sets AI target range to 0
                    DisableLocking = true, // force disable the 'Enable Target Locking'
                    DisableManualControl = true, // makes people who try to manually control this experience a mild headache
                     
                    // controls what targeting types will forcefully be enabled
                    EnableNoTargeting = true, // this should not be true if any of the below are true
                    EnableMeteorTargeting = true,
                    EnableMissileTargeting = true,
                    EnableSmallShipTargeting = true,
                    EnableLargeShipTargeting = true,
                    EnableCharacterTargeting = true,
                    EnableStationTargeting = true,
                    EnableFriendlyTargeting = true,
                    EnableNeutralTargeting = true,
                    EnableHostileTargeting = true,

                    TAI_ShootWhenTargetAquired = false, // makes the turret shoot when it has a target. Useful for PD.
                    // Note: On missile types the missiles will point towards the target no matter the barrel angle - keen bug.
                },

                AG_Stats = new AmmoGeneration_Logic
                {
                    // name of ammo magazine to generate
                    AG_AmmoDefinitionName = "Name of the turret's ammo magazine in AmmoMagazines.sbc",
                    
                    // cost of each batch of ammo, in megawatt hours for power or liters for the gases
                    AG_AmmoCost = 1 / 3600f, // 1 / 3600 for 1 MW input power given the 1 second of generation time. 0.0002777777778f is the decimal way of writing this and is valid as well.
                    
                    // what resource is required. Valid types: POWER, HYDROGEN, OXYGEN
                    AG_FuelType = POWER,
                    
                    // how long it takes to generate each batch of ammo in seconds. Required input will be AG_AmmoCost / AG_GenerationTime
                    AG_GenerationTime = 1,

                    // how many ammo magazines is generated each batch
                    AG_NumberGenerated = 1,
                }
            },

            /*
             * DON'T MODIFY ANYTHING BELOW THIS LINE
             */
        };

        public override void BeforeStart()
        {
            foreach (VPFTurretDefinition def in TurretDefinitions)
            {
                MyAPIUtilities.Static.SendModMessage(DefinitionTools.ModMessageID, DefinitionTools.DefinitionToMessage(def));
            }
        }
    }
}
