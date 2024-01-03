using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;
using VanillaPlusFramework.TemplateClasses;
using static VanillaPlusFramework.TemplateClasses.GuidanceType;
using static VanillaPlusFramework.TemplateClasses.IdType;
using static VanillaPlusFramework.TemplateClasses.DamageType;

namespace Invalid.TestAmmoType /// Set namespace name to something else, preferably something no other mod uses. Can be the same as other definitions
{
    /// <summary>
    /// Recommend renaming the file.
    /// Recommend Visual Studio for editing this, all fields have descriptions that visual studio will display on hover over. Make a project solution to put all files in as well so the descriptions show up when hovered over.
    /// Contains all Vanilla+ Stats for all subtypes. Note: Vanilla Ammo.sbc is still used
    /// Note: All implented stats are here. There are some unimplimented ones not shown, do not use them or errors will be thrown.
    /// You can have multiple files, just change the class name ("VPFAmmoDefinitions" in front of public class) or the namespace
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class VPFAmmoDefinitions : MySessionComponentBase
    {
        public static List<VPFAmmoDefinition> AmmoDefinitions = new List<VPFAmmoDefinition>() {
            /*
             * DON'T MODIFY ANYTHING ABOVE THIS LINE EXCEPT THE NAMESPACE & CLASS NAME
             */
            new VPFAmmoDefinition // example flak round
            { // if you are NOT using a logic type, SET IT TO NULL OR THE SCRIPT MAY NOT FUNCTION AS INTENDED (or deleting it, that works too)
            subtypeName = "Example_Flak", //Ammo.sbc subtype of the missile (or projectile if a beam) you want logic for
            FXsubtypeName = "Example_LineTracer", // name of the effect definition to be used for this missile subtype defined in VPFFXDefinitions. If a matching one is not found none will be used.

            VPF_MissileHitpoints = 1, // missile health, used by a few logics like prox det for anti missile; larger = harder to shoot down. Note: shootdown via direct impact from projectiles ignores this.

            PD_Stats = new ProximityDetonation_Logic { // replace this with null if you do not want the missile to have proximity detonation (everything from 'new' until the '},') before the next object or delete the entire section including the `PD_Stats = `
                /* Beam weapon logic with this changes max range to the range of the target, somewhat incompatible with Jump Drive Inhibition and Special Componentry Interaction as they require a direct collision */

                PD_AntiMissileDamage = 1, // damage missile will do to other missiles, set to 0 to disable proximity detonation vs other missiles
                PD_DetonationRadius = 25 // radius missile will check for hostiles, when one is detected missile will explode. this is NOT the explosion radius, that is defined in Ammo.sbc still
            },
            },

            new VPFAmmoDefinition // example guided missile used in VLS
            {
                subtypeName = "InvalidMissile", //Ammo.sbc subtype of the missile (or projectile if a beam) you want logic for
                FXsubtypeName = "Example_CoolRailgunEffect", // name of the effect definition to be used for this missile subtype defined in VPFFXDefinitions. If a matching one is not found none will be used.

                VPF_MissileHitpoints = 1,

                TimeToArm = 3f, // 3 seconds until the missile can hit anything and actually deal damage.

                GL_Stats = new GuidanceLock_Logic {// replace this with null if you do not want the missile to have guidance (everything from 'new' until the '},') before the next object or delete the entire section including the `GL_Stats = `
                    GL_HomingPiecewisePolynomialFunction = DefinitionTools.ConvertToDoubleArrayList(new double[,] { // remove entire variable assignment to remove guidance (use on flares and such)
                        /* Incompatible with beam weapons */
                        
                        /* GL_HomingPiecewisePolynomialFunction
                      * Piecewise polynomial function for homing, input is time in seconds, output is degrees per second the missile will home
                      * For each row:[0] = Start time, [1] = const, [2] = coefficient for x, [3] = coefficient for x^2, [N+1] coefficient for x^N
                      * ex. {0, 1, 0.1, 0.2} yields a function of 0.2x^2+0.1x+1 starting at time = 0
                      * each function will be used until the next function starts
                      * ex. Say {1, 0, 12, 5} is the next row after the previous example
                      * this one will start at time = 1, and yield a function of 5x^2+12x, and when it starts the function will completely replace the old one */
                        {0, -5, 117.5, -37.5}, // until 3 seconds, guide at a rate of -37.5t^2+117.5t-5 where t is time in seconds
                        {3, 2, 0, 0}, // after 3 seconds, guide at a constant 2 deg/s
                        {20, 0, 0, 0 } // after 20 seconds, no more guidance so the missile doesn't run in circles
                    }),
                    GL_DecoyPercentChanceToCauseRetarget = 0, // chance in percent to cause a retarget if within the decoy's retarget radius of a missile's target. 100+ will have it always retarget.
                                                              // Once a missile is targeted onto a decoy it will not attempt to retarget any other entities
                                                              // note: the missile this is set to IS the decoy 
                    GL_DecoyRetargetRadius = 0, // retarget radius of above

                    NoGuidance = false, // set to true if missile has no guidance. Should not be true if other guidance types are true
                    UseLockOn = true, // set to true if missile uses lock on guidance
                    UseOneTimeRaycast = false, // set to true if missile uses one time raycast (raycasts out to max range on missile spawn, if it gets a valid target then it assigns it as such)
                    UseTurretTarget = false, // set to true if missile uses turret targeting (if missile is from an AI turret the missile's target will be on the turret's targeted grid or will be its target if not a grid)
                },
            },

            new VPFAmmoDefinition // example distraction flare setup
            {
                subtypeName = "Example_Flare", //Ammo.sbc subtype of the missile (or projectile if a beam) you want logic for
                FXsubtypeName = "Example_Flare", // name of the effect definition to be used for this missile subtype defined in VPFFXDefinitions. If a matching one is not found none will be used.

                VPF_MissileHitpoints = 1,

                GL_Stats = new GuidanceLock_Logic {// replace this with null if you do not want the missile to have guidance (everything from 'new' until the '},') before the next object or delete the entire section including the `GL_Stats = `
                    GL_HomingPiecewisePolynomialFunction = null,
                    GL_DecoyPercentChanceToCauseRetarget = 50, // chance in percent to cause a retarget if within the decoy's retarget radius of a missile's target. 100+ will have it always retarget.
                                                               // Once a missile is targeted onto a decoy it will not attempt to retarget any other entities
                                                               // note: the missile this is set to IS the decoy 
                    GL_DecoyRetargetRadius = 50, // retarget radius of above
                    // if a missile target is within 50m, every ~10 ticks it has a 50% chance to retarget to the flare
                    NoGuidance = false, // set to true if missile has no guidance. Should not be true if other guidance types are true
                    UseLockOn = true, // set to true if missile uses lock on guidance
                    UseOneTimeRaycast = true, // set to true if missile uses one time raycast (raycasts out to max range on missile spawn, if it gets a valid target then it assigns it as such)
                    UseTurretTarget = true, // set to true if missile uses turret targeting (if missile is from an AI turret the missile's target will be on the turret's targeted grid or will be its target if not a grid)
                },
            },

            // note: I'm combining logics here, but all logics are independent of eachother and I'm only combining them for some basic weapon ideas for how they likely will be used.

            new VPFAmmoDefinition // example EMP railgun
            {
                subtypeName = "Example_EMPRailgun", //Ammo.sbc subtype of the missile (or projectile if a beam) you want logic for
                FXsubtypeName = "Example_CoolRailgunEffect", // name of the effect definition to be used for this missile subtype defined in VPFFXDefinitions. If a matching one is not found none will be used.

                VPF_MissileHitpoints = 5, // PD resistant

                
                EMP_Stats = new EMP_Logic { // replace this object constructor with null if you do not want the missile to have EMP logic (everything from new until the '),') before the next object or delete the entire section including the `EMP_Stats = `
                    EMP_Radius = 25, // radius missile will EMP all blocks within
                    EMP_TimeDisabled = 300 //time block will be disabled for in ticks, halved for already disabled blocks

                    // all blocks within a 25m or 10 block radius are disabled for 300 ticks or 6 seconds
                },
            },

            new VPFAmmoDefinition // example thrust disable beam weapon
            {
                subtypeName = "CEASE", //Ammo.sbc subtype of the missile (or projectile if a beam) you want logic for
                
                // beams do not use FXsubtypeName, can be removed

                VPF_MissileHitpoints = 1, // doesn't matter, beam is untargetable

                SCI_Stats = new List<SpecialComponentryInteraction_Logic>() { // replace this list with null if you do not want the missile to have any interaction with specific blocks when the missile directly hits a grid (everything until the closing curly bracket) or delete the entire section including the `SCI_Stats = `

                /* somewhat incompatible with proximity detonation like jump drive inhibition, missile requires a direct collision to trigger */

                new SpecialComponentryInteraction_Logic
                {
                    SCI_BlockId = "Thrust", // block subtype or type id (in cubeblocks.sbc) you want to have the missile interact with specially
                    SCI_IdType = TypeId, // determines if the above string points to a TypeId or SubtypeId
                    SCI_DamageDealt = 0, // damage it will do to the block, regardless of the distance between the missile and the block as long as it hits the same grid
                    SCI_DamageType = Damage, // speficies if the damage done will be actual damage, or a percentage of its total health
                    SCI_DisableTime = 600,  // time it will disable (turn the block off), similar to EMP but affects any block of the above subtype ID regardless of distance on the same grid
                    SCI_Radius = 5 // Radius to apply the effect in. Set to 0 to disable.

                    // disable all thrusters on the hit grid within 5m of the hit point (blocks with TypeId of 'Thrust') for 600 ticks or 10 seconds. Yes this can friendly fire :)
                },
                // to add more specific blocks copy everything in the 'new SpecialComponentryInteraction_Logic { [stats] },', if you only want one interaction delete one of them currently in the template
            },


                BWT_Stats = new BeamWeaponType_Logic() { // replace this with null if you do not want the missile be a beam (everything from 'new' until the '},') before the next object, or delete the entire section including the `BWT_Stats = `
                 /* beams can be defined using projectiles (gatlings). Doing so has some drawbacks
                  *  - Proximity Detonation is not compatible with them as opposed to missile types, though JDI, SCI, and EMP are
                  *  - Projectile Beam headshot damage is unused
                  *  - due to there being no richochet tags for projecties, <ProjectileTrailColor x="x" y="y" z="z" /> is used instead.
                  *     - x: missileMaxMissileRichochetProbability
                  *     - y: MaxMissileRichochetAngle
                  *     - z: MissileRichochetDamage
                  *  - There has one upside - <ProjectileCount>num</ProjectileCount> can be used to create multiple beams per tick.
                  */
                 /* BWT_DamageFalloffPiecewisePolynomialFunction
                  * Piecewise polynomial function for damage falloff, input is distance in meters, output is damage multiplier in percent
                  * For each row:[0] = Start time, [1] = const, [2] = coefficient for x, [3] = coefficient for x^2, [N+1] coefficient for x^N
                  * ex. {0, 1, 0.1, 0.2} yields a function of 0.2x^2+0.1x+1 starting at time = 0
                  * each function will be used until the next function starts
                  * ex. Say {1, 0, 12, 5} is the next row after the previous example
                  * this one will start at time = 1, and yield a function of 5x^2+12x, and when it starts the function will completely replace the old one */
                BWT_DamageFalloffPiecewisePolynomialFunction = DefinitionTools.ConvertToDoubleArrayList(new double[,]
                {
                    { 0, 100, 0 }, // deal 100% of the damage no matter what
                }),
                BWT_BeamColor = new Vector4(0.1f, 0.2f, 1f, 0.001f) * 50, // blue beam
                BWT_BeamThickness = 0.1f, // thickness of the beam
                BWT_ShowExplosionFX = false, // whether or not to use the particle effect defined in Ammo.sbc or none.
                BWT_Fade = true, // whether or not to fade the beam to black over time
                BWT_TimeActive = 100, // time the beam will be visible in ticks
                                      // fade the beam to nothing over 100 ticks after firing

                BeamRenderOffset = 0f // forwards offset of the start of the beam. Negative to go backwards.
                                      // use BeamRenderOffset to put the beam on the actual barrel
                },
            },

            new VPFAmmoDefinition // example jump drive inhibitor beam weapon
            {
                subtypeName = "Example_JDDT_Charge",

                // beams do not use FXsubtypeName, can be removed

                VPF_MissileHitpoints = 1, // doesn't matter, beam is untargetable

                BWT_Stats = new BeamWeaponType_Logic() { // replace this with null if you do not want the missile be a beam (everything from 'new' until the '},') before the next object, or delete the entire section including the `BWT_Stats = `
                 /* beams can be defined using projectiles (gatlings). Doing so has some drawbacks
                  *  - Proximity Detonation is not compatible with them as opposed to missile types, though JDI, SCI, and EMP are
                  *  - Projectile Beam headshot damage is unused
                  *  - due to there being no richochet tags for projecties, <ProjectileTrailColor x="x" y="y" z="z" /> is used instead.
                  *     - x: MaxMissileRichochetProbability
                  *     - y: MaxMissileRichochetAngle
                  *     - z: MissileRichochetDamage
                  *  - There has one upside - <ProjectileCount>num</ProjectileCount> can be used to create multiple beams per tick.
                  */
                 /* BWT_DamageFalloffPiecewisePolynomialFunction
                  * Piecewise polynomial function for damage falloff, input is distance in meters, output is damage multiplier in percent
                  * For each row:[0] = Start time, [1] = const, [2] = coefficient for x, [3] = coefficient for x^2, [N+1] coefficient for x^N
                  * ex. {0, 1, 0.1, 0.2} yields a function of 0.2x^2+0.1x+1 starting at time = 0
                  * each function will be used until the next function starts
                  * ex. Say {1, 0, 12, 5} is the next row after the previous example
                  * this one will start at time = 1, and yield a function of 5x^2+12x, and when it starts the function will completely replace the old one */
                BWT_DamageFalloffPiecewisePolynomialFunction = DefinitionTools.ConvertToDoubleArrayList(new double[,]
                {
                    { 0, 100, 0 }, // deal 100% of the damage no matter what
                }),
                BWT_BeamColor = new Vector4(0.4f, 1f, 0.7f, 0.0001f) * 100, // green beam
                BWT_BeamThickness = 1f, // thick beam
                BWT_ShowExplosionFX = false, // whether or not to use the particle effect defined in Ammo.sbc or none.
                BWT_Fade = true, // whether or not to fade the beam to black over time
                BWT_TimeActive = 100, // time the beam will be visible in ticks
                                      // fade the beam to nothing over 100 ticks after firing

                BeamRenderOffset = 0f // forwards offset of the start of the beam. Negative to go backwards.
                                      // use BeamRenderOffset to put the beam on the actual barrel
                },

                JDI_Stats = new JumpDriveInhibition_Logic
                {
                    JDI_DistributePower = false,
                    JDI_PowerDrainInW = 150000,

                    // remove 15000Wh (150kWh or 0.15MWh), about 5% of a jump drive's total charge) from every jump drive
                }
            },


            // copy everything from 'new VPFAmmoDefinition' to '} },' and paste it after the '} },' to add another ammo definition to this list

            /*
             * DON'T MODIFY ANYTHING BELOW THIS LINE
             */
        };



        public override void BeforeStart()
        {
            foreach (VPFAmmoDefinition def in AmmoDefinitions)
            {
                MyAPIUtilities.Static.SendModMessage(DefinitionTools.ModMessageID, DefinitionTools.DefinitionToMessage(def));
            }
        }
    }
}
