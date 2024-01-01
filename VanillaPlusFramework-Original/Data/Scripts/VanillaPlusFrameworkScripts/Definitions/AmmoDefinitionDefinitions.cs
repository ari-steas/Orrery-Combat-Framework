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
using VRage.Game.ModAPI.Interfaces;
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
using VRage.Game.Models;
using VRage.Render.Particles;
using System.Linq.Expressions;
using System.IO;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Weapons;
using VRage;
using VRage.Collections;
using VRage.Voxels;
using ProtoBuf;
using SpaceEngineers.Game.ModAPI;
using System.Diagnostics.Contracts;
using VRageRender;
using static VRageRender.MyBillboard.BlendTypeEnum;
using static VanillaPlusFramework.TemplateClasses.IdType;
using static VanillaPlusFramework.TemplateClasses.DamageType;
using static VanillaPlusFramework.TemplateClasses.GuidanceType;


// all the data structures for the ammo definitions, you can ignore this file it just ensures compilation successful.

/******************************************************************************************************************************************************
 *                                                                                                                                                    *
 *                                                            DO NOT MODIFY THIS FILE                                                                 *
 *                                                                                                                                                    *
 ******************************************************************************************************************************************************/


namespace VanillaPlusFramework.TemplateClasses
{
    /// <summary>
    /// Base class for all definitions so deserialization can occur without errors.
    /// </summary>
    [ProtoContract]
    [ProtoInclude(1000, typeof(VPFAmmoDefinition))]
    public partial class VPFDefinition
    { }
    /// <summary>
    /// Specifies what type the companion string points to.
    /// <para>
    /// Availible Types:
    /// <code>
    /// SubtypeId
    /// TypeId
    /// </code>
    /// </para>
    /// </summary>
    [Serializable]
    public enum IdType
    {
        SubtypeId,
        TypeId
    }

    /// <summary>
    /// Speficies what type of damage the companion float points to. Percent deals a percentage of total hp, damage deals that amount of damage.
    /// <para>
    /// Availible Types:
    /// <code>
    /// Percent
    /// Damage
    /// </code>
    /// </para>
    /// </summary>
    [Serializable]
    public enum DamageType
    {
        Percent,
        Damage
    }

    /// <summary>
    /// Speficies how a guided missile can gain a target
    /// <para>
    /// Availible Types:
    /// <code>
    /// None
    /// LockOn
    /// TurretTarget
    /// DesignatedPosition
    /// OneTimeRaycast
    /// </code>
    /// </para>
    /// </summary>
    [Serializable]
    [Flags]
    public enum GuidanceType
    {
        None = 0,
        LockOn = 1,
        TurretTarget = 2,
        DesignatedPosition = 4,
        OneTimeRaycast = 8,
    }

    ///<summary>
    /// Protobuf doesn't allow directly nested arrays or multidmensional ones, so this is necessary to serialize.
    ///</summary>
    [ProtoContract]
    public struct DoubleArray
    {
        [ProtoMember(1)]
        public double[] array;
        public DoubleArray(double[] array)
        {
            this.array = array;
        }

        public override string ToString()
        {
            string vals = "";
            foreach (double d in array)
            {
                vals += $"{d}, ";
            }
            return "{ " + vals + " },";
        }
    }

    /// <summary>
    /// Struct containing variables controlling EMP behavior
    /// </summary>
    [ProtoContract]
    public struct EMP_Logic
    {
        /// <summary>
        /// Radius the EMP effect will affect. This will turn off all blocks within this radius.
        /// <para>
        /// Unit: Meters
        /// </para>
        /// <para>
        /// Should never be zero or negative.
        /// </para>
        /// </summary>
        [ProtoMember(1)]
        public float EMP_Radius;
        /// <summary>
        /// How long anything disabled by the round will be forced turned off for.
        /// Anything EMP'd already will have their disabled time increased by half of this.
        /// <para>
        /// Unit: Ticks
        /// </para>
        /// <para>
        /// Should never be zero or negative.
        /// </para>
        /// </summary>
        [ProtoMember(2)]
        public int EMP_TimeDisabled;
        public EMP_Logic(float EMP_Radius, int EMP_TimeDisabled)
        {
            this.EMP_Radius = EMP_Radius;
            this.EMP_TimeDisabled = EMP_TimeDisabled;
        }

        public override string ToString()
        {
            return $" EMP_Radius: {EMP_Radius} EMP_TimeDisabled: {EMP_TimeDisabled}";
        }
    }

    /// <summary>
    /// Struct containing variables controlling behavior relating to guided missiles.
    /// </summary>
    [ProtoContract]
    public struct GuidanceLock_Logic
    {
        /// <summary>
        /// <para>
        /// Storage for the 2D array controlling the missile guidance function.
        /// </para>
        /// <para>
        /// Each horizontal row describes one function, with the start point at the leftmost value, and endpoint at the next row's start.
        /// Vertical columns describe the coefficients, with the leftmost one being the time index, and each one after that being part of the function itself, with the x^n increasing the further rightward.
        /// </para>
        /// <example>
        /// Example:
        /// <code>
        /// {
        ///     {0, 1, 5},
        ///     {5, 6, 0},
        /// }
        /// </code>
        /// Gives a function of 1+5x from time 0 to 5,
        /// and a function of 6+0x from time 5 to positive infinity.
        /// </example>
        /// <para>
        /// The units this function is in is seconds for input, and degrees per second for output, so if the function at a point returns 1, the missile will home in at 1 degree per second.
        /// </para>
        /// <para>
        /// Set to <c>null</c> to disable any guided behavior. Time index should never be negative.
        /// </para>
        /// </summary>
        [ProtoMember(1)]
        public List<DoubleArray> GL_HomingPiecewisePolynomialFunction;

        /// <summary>
        /// Percent chance that any guided missile will retarget this missile if this missile is within a certain radius of the guided missile's target when checked.
        /// This check will happen around 6 times per second.
        /// <para>
        /// Unit: Percent
        /// </para>
        /// <para>
        /// Set to 0 to disable any decoy-like behavior. Should never be negative.
        /// </para>
        /// </summary>
        [ProtoMember(2)]
        public double GL_DecoyPercentChanceToCauseRetarget;

        /// <summary>
        /// Radius that controls if a check to see if the guided missile will retarget will happen. 
        /// This check will happen around 6 times per second. The guided missile's target must be within this radius of the missile for the check to happen.
        /// <para>
        /// Unit: Meters
        /// </para>
        /// <para>
        /// Set to 0 to disable any decoy-like behavior. Should never be negative.
        /// </para>
        /// </summary>
        [ProtoMember(3)]
        public float GL_DecoyRetargetRadius;

        /// <summary>
        /// Controls how the missile will aquire a target. Topmost in this list takes priority.
        /// <para>
        /// None - don't use if you intend for the missile to guide. Use on flares
        /// </para>
        /// <para>
        /// LockOn - Missile will target whatever the player in the grid is locked on to
        /// </para>
        /// <para>
        /// TurretTarget - Missile will target whatever the turret that fired it is currently targeting
        /// </para>
        /// <para>
        /// DesignatedPosition - Missile will use turret designators specified in TurretDefinitions to aquire targets based off of what it is targeting / pointing at
        /// </para>
        /// <para>
        /// OneTimeRaycast - Missile will perform a one time raycast on spawn direclty forward for its maximum range to attempt to aquire a target
        /// </para>
        /// </summary>
        [ProtoMember(4)]
        public GuidanceType GL_AllowedGuidanceTypes;


        /// <summary>
        /// Set to true if missile has no guidance
        /// </summary>
        [ProtoIgnore]
        public bool NoGuidance
        {
            get
            {
                return GL_AllowedGuidanceTypes == None;
            }
            set
            {
                if (value) GL_AllowedGuidanceTypes = None;
            }
        }

        /// <summary>
        /// Set to true if missile uses lock on guidance
        /// </summary>
        [ProtoIgnore]
        public bool UseLockOn
        {
            get
            {
                return GL_AllowedGuidanceTypes.HasFlag(LockOn);
            }
            set
            {
                if (value) GL_AllowedGuidanceTypes |= LockOn;

                else GL_AllowedGuidanceTypes &= ~LockOn;
            }
        }

        /// <summary>
        /// Set to true if missile uses turret targeting
        /// </summary>
        [ProtoIgnore]
        public bool UseTurretTarget
        {
            get
            {
                return GL_AllowedGuidanceTypes.HasFlag(TurretTarget);
            }
            set
            {
                if (value) GL_AllowedGuidanceTypes |= TurretTarget;

                else GL_AllowedGuidanceTypes &= ~TurretTarget;
            }
        }

        /// <summary>
        /// Set to true if missile uses designated position
        /// </summary>
        [ProtoIgnore]
        public bool UseDesignatedPosition
        {
            get
            {
                return GL_AllowedGuidanceTypes.HasFlag(DesignatedPosition);
            }
            set
            {
                if (value) GL_AllowedGuidanceTypes |= DesignatedPosition;

                else GL_AllowedGuidanceTypes &= ~DesignatedPosition;
            }
        }

        /// <summary>
        /// Set to true if missile uses one time raycast
        /// </summary>
        public bool UseOneTimeRaycast
        {
            get
            {
                return GL_AllowedGuidanceTypes.HasFlag(OneTimeRaycast);
            }
            set
            {
                if (value) GL_AllowedGuidanceTypes |= OneTimeRaycast;

                else GL_AllowedGuidanceTypes &= ~OneTimeRaycast;
            }
        }
        public GuidanceLock_Logic(double[,] GL_HomingPiecewisePolynomialFunction, double GL_DecoyPercentChanceToCauseRetarget, float GL_DecoyRetargetRadius, GuidanceType GL_AllowedGuidanceTypes)
        {
            this.GL_HomingPiecewisePolynomialFunction = DefinitionTools.ConvertToDoubleArrayList(GL_HomingPiecewisePolynomialFunction);
            this.GL_DecoyPercentChanceToCauseRetarget = GL_DecoyPercentChanceToCauseRetarget;
            this.GL_DecoyRetargetRadius = GL_DecoyRetargetRadius;
            this.GL_AllowedGuidanceTypes = GL_AllowedGuidanceTypes;
        }

        public GuidanceLock_Logic(List<DoubleArray> GL_HomingPiecewisePolynomialFunction, double GL_DecoyPercentChanceToCauseRetarget, float GL_DecoyRetargetRadius, GuidanceType GL_AllowedGuidanceTypes)
        {
            this.GL_HomingPiecewisePolynomialFunction = GL_HomingPiecewisePolynomialFunction;
            this.GL_DecoyPercentChanceToCauseRetarget = GL_DecoyPercentChanceToCauseRetarget;
            this.GL_DecoyRetargetRadius = GL_DecoyRetargetRadius;
            this.GL_AllowedGuidanceTypes = GL_AllowedGuidanceTypes;
        }

        public override string ToString()
        {
            string func = "";

            if (GL_HomingPiecewisePolynomialFunction != null)
            {
                foreach (DoubleArray arr in GL_HomingPiecewisePolynomialFunction)
                {
                    func += "{ " +$"{arr}" +" },\n";
                }
            }

            return $"Homing Function: {func} GL_DecoyPercentChanceToCauseRetarget: {GL_DecoyPercentChanceToCauseRetarget} GL_DecoyRetargetRadius: {GL_DecoyRetargetRadius}";
        }
    }

    /// <summary>
    /// Struct containing variables controlling proximity detonation behavior.
    /// </summary>
    [ProtoContract]
    public struct ProximityDetonation_Logic
    {
        /// <summary>
        /// Damage dealt to any missiles it proximity detonates against. Serves as PD. Missiles will default to 1 HP, and can be specified in the VPFAmmoDefinition
        /// <para>
        /// Units: Damage (unitless)
        /// </para>
        /// <para>
        /// Set to 0 to disable proximity detonation against missiles.
        /// </para>
        /// </summary>
        [ProtoMember(1)]
        public float PD_AntiMissileDamage;

        /// <summary>
        /// Radius at which the missile will detect any hostile grids, players, or missiles if set. If a hostile is detected, the missile detonates.
        /// Note: This does not control the explosion's radius, that is handled in Ammo.sbc.
        /// <para>
        /// Units: Meters
        /// </para>
        /// <para>
        /// Should never be zero or negative.
        /// </para>
        /// </summary>
        [ProtoMember(2)]
        public float PD_DetonationRadius;
        public ProximityDetonation_Logic(float PD_AntiMissileDamage, float PD_DetonationRadius)
        {
            this.PD_AntiMissileDamage = PD_AntiMissileDamage;
            this.PD_DetonationRadius = PD_DetonationRadius;
        }

        public override string ToString()
        {
            return $" PD_AntiMissileDamage: {PD_AntiMissileDamage} PD_DetonationRadius: {PD_DetonationRadius}";
        }
    }


    /// <summary>
    /// Struct containing variables controlling jump drive power drain on hit.
    /// </summary>
    [ProtoContract]
    public struct JumpDriveInhibition_Logic
    {
        /// <summary>
        /// How much power will be removed from any hit grid's jump drives, if any, if possible.
        /// <para>
        /// Units: Watt Hours
        /// </para>
        /// <para>
        /// Should never be zero or negative.
        /// </para>
        /// </summary>
        [ProtoMember(1)]
        public float JDI_PowerDrainInW;

        /// <summary>
        /// Should the power drain be distributed evenly across EACH jump drive (true), or remove that amount from EVERY jump drive (false).
        /// </summary>
        [ProtoMember(2)]
        public bool JDI_DistributePower;

        public JumpDriveInhibition_Logic(float JDI_PowerDrainInW, bool JDI_DistributePower)
        {
            this.JDI_PowerDrainInW = JDI_PowerDrainInW;
            this.JDI_DistributePower = JDI_DistributePower;
        }

        public override string ToString()
        {
            return $" JDI_PowerDrainInW: {JDI_PowerDrainInW} JDI_DistributePower: {JDI_DistributePower}";
        }
    }


    /// <summary>
    /// Struct containing variables controlling any beam weaponry.
    /// </summary>
    [ProtoContract]
    public struct BeamWeaponType_Logic
    {
        /// <summary>
        /// Controls how long will the beam be rendered. Note: Only one tick of damage will still happen.
        /// <para>
        /// Units: Ticks
        /// </para>
        /// <para>
        /// Should never be zero or negative.
        /// </para>
        /// </summary>
        [ProtoMember(1)]
        public int BWT_TimeActive;
        /// <summary>
        /// <para>
        /// Storage for the 2D array controlling damage falloff for the beam as a piecewise polynomial function.
        /// </para>
        /// <para>
        /// Each horizontal row describes one function, with the start point at the leftmost value, and endpoint at the next row's start.
        /// Vertical columns describe the coefficients, with the leftmost one being the time index, and each one after that being part of the function itself, with the x^n increasing the further rightward.
        /// </para>
        /// <example>
        /// Example:
        /// <code>
        /// {
        ///     {0, 1, 5},
        ///     {5, 6, 0},
        /// }
        /// </code>
        /// Gives a function of 1+5x from time 0 to 5,
        /// and a function of 6+0x from time 5 to positive infinity.
        /// </example>
        /// <para>
        /// The units this function is in is seconds for input, and percent for output, with 100 being 100% and 0 being 0%. If this evaluates to a negative number it will be set to 0. Values over 100% are possible.
        /// </para>
        /// </summary>
        [ProtoMember(2)]
        public List<DoubleArray> BWT_DamageFalloffPiecewisePolynomialFunction;
        /// <summary>
        /// Controls how thick will the beam be when rendered.
        /// <para>
        /// Units: Keen doesn't tell me, so best guess is pixels
        /// </para>
        /// <para>
        /// Should never be zero or negative.
        /// </para>
        /// </summary>
        [ProtoMember(3)]
        public float BWT_BeamThickness;
        /// <summary>
        /// Controls the color.
        /// <para>
        /// Units: RGBA format, in Vector4.
        /// </para>
        /// <para>
        /// No part should be negative.
        /// </para>
        /// </summary>
        [ProtoMember(4)]
        public Vector4D BWT_BeamColor;
        /// <summary>
        /// Controls if the missile's explosion FX defined in the sbc will be rendered (true) or not (false).
        /// <para>
        /// Units: Bpp;
        /// </para>
        /// </summary>
        [ProtoMember(5)]
        public bool BWT_ShowExplosionFX;
        /// <summary>
        /// Bool to fade the beam to black over time. (Lerps alpha from its intended value to zero from start of time active to end).
        /// <para>
        /// Units: Bool
        /// </para>
        /// </summary>
        [ProtoMember(6)]
        public bool BWT_Fade;
        /// <summary>
        /// Distance to offset the start of the beam forwards/backwards. Positive is forwards, negative is backwards.
        /// <para>
        /// Units: Meters
        /// </para>
        /// </summary>
        [ProtoMember(7)]
        public float BeamRenderOffset;
        public BeamWeaponType_Logic(int BWT_TimeActive, double[,] BWT_DamageFalloffPiecewisePolynomialFunction, float BWT_BeamThickness, Vector4 BWT_BeamColor, bool BWT_ShowExplosionFX, bool BWT_Fade, float BeamRenderOffset)
        {
            this.BWT_TimeActive = BWT_TimeActive;
            this.BWT_DamageFalloffPiecewisePolynomialFunction = DefinitionTools.ConvertToDoubleArrayList(BWT_DamageFalloffPiecewisePolynomialFunction);
            this.BWT_BeamThickness = BWT_BeamThickness;
            this.BWT_BeamColor = BWT_BeamColor;
            this.BWT_ShowExplosionFX = BWT_ShowExplosionFX;
            this.BWT_Fade = BWT_Fade;
            this.BeamRenderOffset = BeamRenderOffset;
        }

        public BeamWeaponType_Logic(int BWT_TimeActive, List<DoubleArray> BWT_DamageFalloffPiecewisePolynomialFunction, float BWT_BeamThickness, Vector4 BWT_BeamColor, bool BWT_ShowExplosionFX, bool BWT_Fade, float BeamRenderOffset)
        {
            this.BWT_TimeActive = BWT_TimeActive;
            this.BWT_DamageFalloffPiecewisePolynomialFunction = BWT_DamageFalloffPiecewisePolynomialFunction;
            this.BWT_BeamThickness = BWT_BeamThickness;
            this.BWT_BeamColor = BWT_BeamColor;
            this.BWT_ShowExplosionFX = BWT_ShowExplosionFX;
            this.BWT_Fade = BWT_Fade;
            this.BeamRenderOffset = BeamRenderOffset;
        }

        public override string ToString()
        {
            string func = "";

            if (BWT_DamageFalloffPiecewisePolynomialFunction != null)
            {
                foreach (DoubleArray arr in BWT_DamageFalloffPiecewisePolynomialFunction)
                {
                    func += "{ " + $"{arr}" + " },\n";
                }
            }

            return $" BWT_TimeActive: {BWT_TimeActive} BWT_DamageFalloffPiecewisePolynomialFunction: {func} BWT_BeamThickness: {BWT_BeamThickness}" +
                $" BWT_BeamColor: {BWT_BeamColor} BWT_ShowExplosionFX: {BWT_ShowExplosionFX} BWT_Fade: {BWT_Fade}";
        }
    }

    /// <summary>
    /// <para>
    /// Struct containing variables controlling special behavior.
    /// Special behavior affects all blocks on the hit grid regardless of distance hit.
    /// </para>
    /// Useful for things like damaging and disabling remote controls for a time, or antennas, etc.
    /// </summary>
    [ProtoContract]
    public struct SpecialComponentryInteraction_Logic
    {
        /// <summary>
        /// String for the Id of the block as defined in Cubeblocks.sbc. Can be type or subtype Id, which is configured in <c>IsSubtype</c>.
        /// <para>
        /// Should never be the empty string.
        /// </para>
        /// </summary>
        [ProtoMember(1)]
        public string SCI_BlockId;
        /// <summary>
        /// Controls whether the <c>BlockIdName</c> is a <c>SubtypeId</c> (true) or <c>TypeId</c> (false)
        /// </summary>
        [ProtoMember(2)]
        public IdType SCI_IdType;
        /// <summary>
        /// Damage dealt to the every block with the specified type or subtype on the hit grid. Set to zero to disable damaging these blocks.
        /// <para>
        /// Units: Damage (Unitless) or Percent (Defined in the IsPercent bool)
        /// </para>
        /// <para>
        /// Should never be negative.
        /// </para>
        /// </summary>
        [ProtoMember(3)]
        public float SCI_DamageDealt;
        /// <summary>
        /// Determines if the unit of <c>SCI_DamageDealt</c> is percent (true) or actual damage (false).
        /// </summary>
        [ProtoMember(4)]
        public DamageType SCI_DamageType;
        /// <summary>
        /// Applies an EMP effect to the affected blocks for this variable's amount of time. Set to zero to disable.
        /// <para>
        /// Units: Ticks
        /// </para>
        /// <para>
        /// Should never be negative.
        /// </para>
        /// </summary>
        [ProtoMember(5)]
        public float SCI_DisableTime;

        /// <summary>
        /// Radius of effect on the grid. Set to 0 to disable. Defaults to 0.
        /// <para>
        /// Units: meters
        /// </para>
        /// <para>
        /// Should never be negative.
        /// </para>
        /// </summary>
        [ProtoMember(6)]
        public float SCI_Radius;

        public SpecialComponentryInteraction_Logic(string SCI_BlockId, IdType SCI_IdType, float SCI_DamageDealt, DamageType SCI_DamageType, float SCI_DisableTime, float SCI_Radius)
        {
            this.SCI_BlockId = SCI_BlockId;
            this.SCI_IdType = SCI_IdType;
            this.SCI_DamageDealt = SCI_DamageDealt;
            this.SCI_DamageType = SCI_DamageType;
            this.SCI_DisableTime = SCI_DisableTime;
            this.SCI_Radius = SCI_Radius;
        }

        public override string ToString()
        {
            return $" SCI_BlockIdName: {SCI_BlockId} SCI_IdType: {SCI_IdType} SCI_DamageDealt: {SCI_DamageDealt} SCI_IsDamagePercent: {SCI_DamageType}" +
                $" SCI_DisableTime: {SCI_DisableTime}";
        }
    }
    [ProtoContract]
    public class VPFAmmoDefinition : VPFDefinition
    {
        /// <summary>
        /// Name of the Subtype of the ammo defined in Ammo.sbc paired with the following stats.
        /// <para>
        /// Should never be the empty string or null.
        /// </para>
        /// </summary>
        [ProtoMember(1)]
        public string subtypeName = null;

        /// <summary>
        /// Name of the Subtype of the FX subtype defined in VPF FX Definitions. Set to null to disable.
        /// </summary>
        [ProtoMember(2)]
        public string FXsubtypeName = null;

        /// <summary>
        /// Actual healthpool of the missile. This is the value 
        /// <para>
        /// Units: Health (Unitless)
        /// </para>
        /// <para>
        /// Should never be zero or negative. (Unless you want to cause the missile to die on firing)
        /// </para>
        /// </summary>
        [ProtoMember(3)]
        public float VPF_MissileHitpoints;

        /// <summary>
        /// Struct for all of the stats relating to the missile causing an EMP effect.
        /// <para>
        /// Incompatible with: None
        /// </para>
        /// <para>
        /// SET TO NULL IF YOU DO NOT WANT TO USE THIS LOGIC. HOW TO DO SO IS LISTED BELOW
        /// <code>
        /// EMP_Stats = null,
        /// </code>
        /// </para>
        /// </summary>
        [ProtoMember(4)]
        public EMP_Logic? EMP_Stats = null;
        /// <summary>
        /// Struct for all of the stats relating to guided missiles and decoys against them.
        /// <para>
        /// <para>
        /// Incompatible with: Beam Weapon Type. Beam will take priority
        /// </para>
        /// SET TO NULL IF YOU DO NOT WANT TO USE THIS LOGIC. HOW TO DO SO IS LISTED BELOW
        /// <code>
        /// GL_Stats = null,
        /// </code>
        /// </para>
        /// </summary>
        [ProtoMember(5)]
        public GuidanceLock_Logic? GL_Stats = null;

        /// <summary>
        /// <para>Struct for all of the stats relating to proximity detonation of the missile.
        /// <br>Incompatible with: Beam Weapon Type. Beam will take priority</br>
        /// <br>Partially Incompatible with: Jump Drive Inhibition, Special Componentry Interaction (both require a direct hit, which proximity detonation usually never allows)</br>
        /// </para>
        /// <para>
        /// SET TO NULL IF YOU DO NOT WANT TO USE THIS LOGIC. HOW TO DO SO IS LISTED BELOW
        /// <code>
        /// PD_Stats = null,
        /// </code>
        /// </para>
        /// </summary>
        [ProtoMember(6)]
        public ProximityDetonation_Logic? PD_Stats = null;
        /// <summary>
        /// Struct for all of the stats relating to jump drive inhibition by removing jump drive charge.
        /// <para>
        /// <para>
        /// Incompatible with: None
        /// Partially Incompatible with: Proximity Detonation (This logic requires a direct hit, which proximity detonation usually never allows)
        /// </para>
        /// SET TO NULL IF YOU DO NOT WANT TO USE THIS LOGIC. HOW TO DO SO IS LISTED BELOW
        /// <code>
        /// JDI_Stats = null,
        /// </code>
        /// </para>
        /// </summary>
        [ProtoMember(7)]
        public JumpDriveInhibition_Logic? JDI_Stats = null;
        /// <summary>
        /// Struct for all of the stats relating to turning the missile into a hitreg beam weapon.
        /// <para>
        /// <para>
        /// Incompatible with: Proximity detonation, Guided Missiles. Beam will take priority
        /// </para>
        /// SET TO NULL IF YOU DO NOT WANT TO USE THIS LOGIC. HOW TO DO SO IS LISTED BELOW
        /// <code>
        /// BWT_Stats = null,
        /// </code>
        /// </para>
        /// </summary>
        [ProtoMember(8)]
        public BeamWeaponType_Logic? BWT_Stats = null;
        /// <summary>
        /// <para>Struct for all of the stats relating to special componentry interaction logic.
        /// <br>Essentually, for each logic in the list, any blocks with the matching TypeId or SubtypeId (depends whats specified), will be affected by its following parameters.</br>
        /// <br>Parameters are defined in more detail  by themselves.</br></para>
        /// <example>
        /// Say something like this is written
        /// <code>
        /// SCI_BlockIdName = Thrust,
        /// SCI_IdType = TypeId,
        /// SCI_DamageDealt = 100,
        /// SCI_DamageType = Percent,
        /// SCI_DisableTime = 1
        /// </code>
        /// This will apply, to every thruster on the hit grid, 1 second of forced disable like EMP, and deal 100% of the block's health as damage.
        /// </example>
        /// <para>
        /// <para>
        /// Incompatible with: None
        /// Partially Incompatible with: Proximity Detonation (This logic requires a direct hit, which proximity detonation usually never allows)
        /// </para>
        /// SET TO NULL IF YOU DO NOT WANT TO USE THIS LOGIC. HOW TO DO SO IS LISTED BELOW
        /// <code>
        /// SCI_Stats = null,
        /// </code>
        /// </para>
        /// </summary>
        [ProtoMember(9)]
        public List<SpecialComponentryInteraction_Logic> SCI_Stats = null;

        /// <summary>
        /// <para>if a missile contains both penetration and explosive damage, setting this to true will implement a fix to make the missile damage properly.
        /// <br> - It will no longer remove the first deformable block it hits.</br>
        /// <br>Defaults to <c>false</c></br></para>
        /// <para>
        /// Values:
        ///  - true (impliments the fix)
        ///  - false (does nothing)
        /// Units: bool
        /// </para>
        /// </summary>
        [ProtoMember(10)]
        public bool NeedsAPHEFix = false;

        /// <summary>
        /// Removes all damage that the missile deals until the missile has lived for atleast this long. Set to zero to disable.
        /// <para>
        /// Units: Seconds
        /// </para>
        /// <para>
        /// Should never be less than zero.
        /// </para>
        /// </summary>
        [ProtoMember(11)]
        public float TimeToArm = 0;


        public VPFAmmoDefinition()
        {
            subtypeName = "";
            FXsubtypeName = "";
            VPF_MissileHitpoints = -1;

            EMP_Stats = null;
            GL_Stats = null;
            PD_Stats = null;
            JDI_Stats = null;
            BWT_Stats = null;
            SCI_Stats = null;

            NeedsAPHEFix = false;
        }

        public VPFAmmoDefinition(string subtypeName, float VPF_MissileHitpoints, EMP_Logic? EMP_Stats = null, GuidanceLock_Logic? GL_Stats = null, ProximityDetonation_Logic? PD_Stats = null, JumpDriveInhibition_Logic? JDI_Stats = null, BeamWeaponType_Logic? BWT_Stats = null, List<SpecialComponentryInteraction_Logic> SCI_Stats = null)
        {
            this.subtypeName = subtypeName;
            this.VPF_MissileHitpoints = VPF_MissileHitpoints;

            this.EMP_Stats = EMP_Stats;
            this.GL_Stats = GL_Stats;
            this.PD_Stats = PD_Stats;
            this.JDI_Stats = JDI_Stats;
            this.BWT_Stats = BWT_Stats;
            this.SCI_Stats = SCI_Stats;
        }

        public override string ToString()
        {
            string str = "";

            if (SCI_Stats != null)
            {
                foreach (SpecialComponentryInteraction_Logic logic in SCI_Stats)
                {
                    str += logic.ToString() + "\n";
                }
            }


            return $" Ammo Subtype: {subtypeName}" +
                FXsubtypeName != null ? $"\n FXSubtypeName: {FXsubtypeName}" : "" +
                $"\nHitpoints: {VPF_MissileHitpoints}" +
                EMP_Stats != null ? $"\nEMP_Stats: {EMP_Stats}" : "" +
                GL_Stats != null ? $"\nGL_Stats: {GL_Stats}" : "" +
                PD_Stats != null ? $"\nPD_Stats: {PD_Stats}" : "" +
                JDI_Stats != null ? $"\nJDI_Stats: {JDI_Stats}" : "" +
                BWT_Stats != null ? $"\nBWT_Stats: {BWT_Stats}" : "" +
                SCI_Stats != null ? $"\nSCI_Stats: " + str : "";
        }

        public override bool Equals(object obj)
        {
            return obj is VPFAmmoDefinition ? ((VPFAmmoDefinition)obj).subtypeName == subtypeName : false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
