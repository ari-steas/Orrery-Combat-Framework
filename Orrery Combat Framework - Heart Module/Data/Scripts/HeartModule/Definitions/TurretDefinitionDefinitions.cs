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
using static VanillaPlusFramework.TemplateClasses.TargetFlags;

// all the data structures for the turret definitions, you can ignore this file it just ensures compilation successful.

/******************************************************************************************************************************************************
 *                                                                                                                                                    *
 *                                                            DO NOT MODIFY THIS FILE                                                                 *
 *                                                                                                                                                    *
 ******************************************************************************************************************************************************/


namespace VanillaPlusFramework.TemplateClasses
{
    [ProtoInclude(1001, typeof(VPFTurretDefinition))]
    public partial class VPFDefinition
    { }
    /// <summary>
    /// <para>
    /// Availible Types:
    /// <code>
    /// POWER
    /// HYDROGEN
    /// OXYGEN
    /// </code>
    /// </para>
    /// </summary>
    [Serializable]
    public enum FuelType
    {
        POWER,
        HYDROGEN,
        OXYGEN
    }

    /// <summary>
    /// <para>
    /// Flags - Multiple can be set by separating each with '|'. Availible Types:
    /// <code>
    /// MeteorTargeting
    /// MissileTargeting
    /// SmallShipTargeting
    /// LargeShipTargeting
    /// CharacterTargeting
    /// StationTargeting
    /// FriendlyTargeting
    /// NeutralTargeting
    /// HostileTargeting
    /// AIControl - force disable only
    /// ManualControl - force disable only
    /// Locking
    /// </code>
    /// </para>
    /// </summary>
    [Serializable]
    [Flags]
    public enum TargetFlags
    {
        None = 0,
        MeteorTargeting = 1,
        MissileTargeting = 2,
        SmallShipTargeting = 4,
        LargeShipTargeting = 8,
        CharacterTargeting = 16,
        StationTargeting = 32,
        FriendlyTargeting = 64,
        NeutralTargeting = 128,
        HostileTargeting = 256,
        Locking = 512,
        AIControl = 1024,
        ManualControl = 2048,
        ToggleShooting = 4096
    }
    /// <summary>
    /// Struct containing all the stats for the turret auto generating ammo based off of power or a vanilla gas.
    /// </summary>
    [ProtoContract]
    public struct AmmoGeneration_Logic
    {
        /// <summary>
        /// Specifies what the ammo generator will use to generate the ammo from.
        /// <para>
        /// Availible Values:
        /// <code>
        /// Any string
        /// </code>
        /// </para>
        /// </summary>
        [ProtoMember(1)]
        public string AG_AmmoDefinitionName;
        /// <summary>
        /// Specifies what resource the ammo generator will use to generate the ammo from.
        /// <para>
        /// Availible Values:
        /// <code>
        /// POWER
        /// HYDROGEN
        /// OXYGEN
        /// </code>
        /// </para>
        /// </summary>
        [ProtoMember(2)]
        public FuelType AG_FuelType;
        /// <summary>
        /// Specifies how much of the specified resource will be used to generate one ammo magazine batch.
        /// <para>
        /// Units: Megawatt Hours OR Liters, depending on <c>AG_FuelType</c>
        /// </para>
        /// <para>
        /// Should never be negative or zero.
        /// </para>
        /// </summary>
        [ProtoMember(3)]
        public float AG_AmmoCost;
        /// <summary>
        /// Specifies how much time it takes to generate one ammo magazine batch.
        /// <para>
        /// Units: Seconds
        /// </para>
        /// <para>
        /// Should never be negative or zero.
        /// </para>
        /// </summary>
        [ProtoMember(4)]
        public float AG_GenerationTime;

        /// <summary>
        /// Specifies how many ammo magazines will be made per batch.
        /// <para>
        /// Units: Number
        /// </para>
        /// <para>
        /// Should never be negative or zero.
        /// </para>
        /// </summary>
        [ProtoMember(5)]
        public int AG_NumberGenerated;

        public AmmoGeneration_Logic(string AG_AmmoDefinitionName, FuelType AG_FuelType, float AG_AmmoCost, float AG_GenerationTime, int AG_NumberGenerated)
        {
            this.AG_AmmoDefinitionName = AG_AmmoDefinitionName;
            this.AG_FuelType = AG_FuelType;
            this.AG_AmmoCost = AG_AmmoCost;
            this.AG_GenerationTime = AG_GenerationTime;
            this.AG_NumberGenerated = AG_NumberGenerated;
        }

        public override string ToString()
        {
            return $" AG_FuelType: {AG_FuelType} AG_AmmoCost: {AG_AmmoCost} AG_GenerationTime: {AG_GenerationTime} AG_NumberGenerated:";
        }
    }
    /// <summary>
    /// Struct containing all the variables for modifying turret AI.
    /// </summary>
    [ProtoContract]
    public struct TurretAI_Logic
    {
        /// <summary>
        /// Specifies the delay between searching for targets if the turret is not actively targeting. Vanilla turrets use Update100(), which would be 100 here.
        /// <para>
        /// Units: Ticks
        /// </para>
        /// <para>
        /// Should never be zero.
        /// </para>
        /// </summary>
        [ProtoMember(1)]
        public int TAI_ResponseTime;

        /// <summary>
        /// Specifies any targeting parameters that will be forcefully disabled. Conflicts with <c>TAI_ForceEnableTargetingFlags</c> when one flag is set for both.
        /// <para>
        /// Flags - Multiple can be set by separating each with '|'. Availible Types:
        /// <code>
        /// None
        /// MeteorTargeting
        /// MissileTargeting
        /// SmallShipTargeting
        /// LargeShipTargeting
        /// CharacterTargeting
        /// StationTargeting
        /// FriendlyTargeting
        /// NeutralTargeting
        /// HostileTargeting
        /// AIControl
        /// ManualControl
        /// Locking
        /// </code>
        /// </para>
        /// </summary>
        [ProtoMember(3)]
        public TargetFlags TAI_DisableTargetingFlags;

        /// <summary>
        /// Set to true to force disable no targeting options
        /// </summary>
        [ProtoIgnore]
        public bool DisableNoTargeting
        {
            get
            {
                return TAI_DisableTargetingFlags.HasFlag(None);
            }
            set
            {
                if (value) TAI_DisableTargetingFlags |= None;

                else TAI_DisableTargetingFlags &= ~None;
            }
        }

        /// <summary>
        /// Set to true to force disable targeting meteors
        /// </summary>
        [ProtoIgnore]
        public bool DisableMeteorTargeting
        {
            get
            {
                return TAI_DisableTargetingFlags.HasFlag(MeteorTargeting);
            }
            set
            {
                if (value) TAI_DisableTargetingFlags |= MeteorTargeting;

                else TAI_DisableTargetingFlags &= ~MeteorTargeting;
            }
        }

        /// <summary>
        /// Set to true to force disable targeting missiles (now called rockets)
        /// </summary>
        [ProtoIgnore]
        public bool DisableMissileTargeting
        {
            get
            {
                return TAI_DisableTargetingFlags.HasFlag(MissileTargeting);
            }
            set
            {
                if (value) TAI_DisableTargetingFlags |= MissileTargeting;

                else TAI_DisableTargetingFlags &= ~MissileTargeting;
            }
        }

        /// <summary>
        /// Set to true to force disable targeting small ships
        /// </summary>
        [ProtoIgnore]
        public bool DisableSmallShipTargeting
        {
            get
            {
                return TAI_DisableTargetingFlags.HasFlag(SmallShipTargeting);
            }
            set
            {
                if (value) TAI_DisableTargetingFlags |= SmallShipTargeting;

                else TAI_DisableTargetingFlags &= ~SmallShipTargeting;
            }
        }

        /// <summary>
        /// Set to true to force disable targeting large ships
        /// </summary>
        [ProtoIgnore]
        public bool DisableLargeShipTargeting
        {
            get
            {
                return TAI_DisableTargetingFlags.HasFlag(LargeShipTargeting);
            }
            set
            {
                if (value) TAI_DisableTargetingFlags |= LargeShipTargeting;

                else TAI_DisableTargetingFlags &= ~LargeShipTargeting;
            }
        }

        /// <summary>
        /// Set to true to force disable targeting characters
        /// </summary>
        [ProtoIgnore]
        public bool DisableCharacterTargeting
        {
            get
            {
                return TAI_DisableTargetingFlags.HasFlag(CharacterTargeting);
            }
            set
            {
                if (value) TAI_DisableTargetingFlags |= CharacterTargeting;

                else TAI_DisableTargetingFlags &= ~CharacterTargeting;
            }
        }

        /// <summary>
        /// Set to true to force disable targeting stations
        /// </summary>
        [ProtoIgnore]
        public bool DisableStationTargeting
        {
            get
            {
                return TAI_DisableTargetingFlags.HasFlag(StationTargeting);
            }
            set
            {
                if (value) TAI_DisableTargetingFlags |= StationTargeting;

                else TAI_DisableTargetingFlags &= ~StationTargeting;
            }
        }

        /// <summary>
        /// Set to true to force disable targeting friendlies
        /// </summary>
        [ProtoIgnore]
        public bool DisableFriendlyTargeting
        {
            get
            {
                return TAI_DisableTargetingFlags.HasFlag(FriendlyTargeting);
            }
            set
            {
                if (value) TAI_DisableTargetingFlags |= FriendlyTargeting;

                else TAI_DisableTargetingFlags &= ~FriendlyTargeting;
            }
        }

        /// <summary>
        /// Set to true to force disable targeting neutrals
        /// </summary>
        [ProtoIgnore]
        public bool DisableNeutralTargeting
        {
            get
            {
                return TAI_DisableTargetingFlags.HasFlag(NeutralTargeting);
            }
            set
            {
                if (value) TAI_DisableTargetingFlags |= NeutralTargeting;

                else TAI_DisableTargetingFlags &= ~NeutralTargeting;
            }
        }

        /// <summary>
        /// Set to true to force disable targeting hostiles
        /// </summary>
        [ProtoIgnore]
        public bool DisableHostileTargeting
        {
            get
            {
                return TAI_DisableTargetingFlags.HasFlag(HostileTargeting);
            }
            set
            {
                if (value) TAI_DisableTargetingFlags |= HostileTargeting;

                else TAI_DisableTargetingFlags &= ~HostileTargeting;
            }
        }

        /// <summary>
        /// Set to true to force disable AI control
        /// </summary>
        [ProtoIgnore]
        public bool DisableAIControl
        {
            get
            {
                return TAI_DisableTargetingFlags.HasFlag(AIControl);
            }
            set
            {
                if (value) TAI_DisableTargetingFlags |= AIControl;

                else TAI_DisableTargetingFlags &= ~AIControl;
            }
        }

        /// <summary>
        /// Set to true to force disable manual control
        /// </summary>
        [ProtoIgnore]
        public bool DisableManualControl
        {
            get
            {
                return TAI_DisableTargetingFlags.HasFlag(ManualControl);
            }
            set
            {
                if (value) TAI_DisableTargetingFlags |= ManualControl;

                else TAI_DisableTargetingFlags &= ~ManualControl;
            }
        }

        /// <summary>
        /// Set to true to force disable turrets being able to use lock on
        /// </summary>
        [ProtoIgnore]
        public bool DisableLocking
        {
            get
            {
                return TAI_DisableTargetingFlags.HasFlag(Locking);
            }
            set
            {
                if (value) TAI_DisableTargetingFlags |= Locking;

                else TAI_DisableTargetingFlags &= ~Locking;
            }
        }
        /// <summary>
        /// Specifies any targeting parameters that will be forcefully enabled. Conflicts with <c>TAI_DisableTargetingFlags</c> when one flag is set for both.
        /// <para>
        /// Flags - Multiple can be set by separating each with '|'. Availible Types:
        /// <code>
        /// None
        /// MeteorTargeting
        /// MissileTargeting
        /// SmallShipTargeting
        /// LargeShipTargeting
        /// CharacterTargeting
        /// StationTargeting
        /// FriendlyTargeting
        /// NeutralTargeting
        /// HostileTargeting
        /// Locking
        /// </code>
        /// </para>
        /// </summary>
        [ProtoMember(4)]
        public TargetFlags TAI_ForceEnableTargetingFlags;


        /// <summary>
        /// Set to true to force enable no targeting options
        /// </summary>
        [ProtoIgnore]
        public bool EnableNoTargeting
        {
            get
            {
                return TAI_ForceEnableTargetingFlags.HasFlag(None);
            }
            set
            {
                if (value) TAI_ForceEnableTargetingFlags |= None;

                else TAI_ForceEnableTargetingFlags &= ~None;
            }
        }

        /// <summary>
        /// Set to true to force enable targeting meteors
        /// </summary>
        [ProtoIgnore]
        public bool EnableMeteorTargeting
        {
            get
            {
                return TAI_ForceEnableTargetingFlags.HasFlag(MeteorTargeting);
            }
            set
            {
                if (value) TAI_ForceEnableTargetingFlags |= MeteorTargeting;

                else TAI_ForceEnableTargetingFlags &= ~MeteorTargeting;
            }
        }

        /// <summary>
        /// Set to true to force enable targeting missiles (now called rockets)
        /// </summary>
        [ProtoIgnore]
        public bool EnableMissileTargeting
        {
            get
            {
                return TAI_ForceEnableTargetingFlags.HasFlag(MissileTargeting);
            }
            set
            {
                if (value) TAI_ForceEnableTargetingFlags |= MissileTargeting;

                else TAI_ForceEnableTargetingFlags &= ~MissileTargeting;
            }
        }

        /// <summary>
        /// Set to true to force enable targeting small ships
        /// </summary>
        [ProtoIgnore]
        public bool EnableSmallShipTargeting
        {
            get
            {
                return TAI_ForceEnableTargetingFlags.HasFlag(SmallShipTargeting);
            }
            set
            {
                if (value) TAI_ForceEnableTargetingFlags |= SmallShipTargeting;

                else TAI_ForceEnableTargetingFlags &= ~SmallShipTargeting;
            }
        }

        /// <summary>
        /// Set to true to force enable targeting large ships
        /// </summary>
        [ProtoIgnore]
        public bool EnableLargeShipTargeting
        {
            get
            {
                return TAI_ForceEnableTargetingFlags.HasFlag(LargeShipTargeting);
            }
            set
            {
                if (value) TAI_ForceEnableTargetingFlags |= LargeShipTargeting;

                else TAI_ForceEnableTargetingFlags &= ~LargeShipTargeting;
            }
        }

        /// <summary>
        /// Set to true to force enable targeting characters
        /// </summary>
        [ProtoIgnore]
        public bool EnableCharacterTargeting
        {
            get
            {
                return TAI_ForceEnableTargetingFlags.HasFlag(CharacterTargeting);
            }
            set
            {
                if (value) TAI_ForceEnableTargetingFlags |= CharacterTargeting;

                else TAI_ForceEnableTargetingFlags &= ~CharacterTargeting;
            }
        }

        /// <summary>
        /// Set to true to force enable targeting stations
        /// </summary>
        [ProtoIgnore]
        public bool EnableStationTargeting
        {
            get
            {
                return TAI_ForceEnableTargetingFlags.HasFlag(StationTargeting);
            }
            set
            {
                if (value) TAI_ForceEnableTargetingFlags |= StationTargeting;

                else TAI_ForceEnableTargetingFlags &= ~StationTargeting;
            }
        }

        /// <summary>
        /// Set to true to force enable targeting friendlies
        /// </summary>
        [ProtoIgnore]
        public bool EnableFriendlyTargeting
        {
            get
            {
                return TAI_ForceEnableTargetingFlags.HasFlag(FriendlyTargeting);
            }
            set
            {
                if (value) TAI_ForceEnableTargetingFlags |= FriendlyTargeting;

                else TAI_ForceEnableTargetingFlags &= ~FriendlyTargeting;
            }
        }

        /// <summary>
        /// Set to true to force enable targeting neutrals
        /// </summary>
        [ProtoIgnore]
        public bool EnableNeutralTargeting
        {
            get
            {
                return TAI_ForceEnableTargetingFlags.HasFlag(NeutralTargeting);
            }
            set
            {
                if (value) TAI_ForceEnableTargetingFlags |= NeutralTargeting;

                else TAI_ForceEnableTargetingFlags &= ~NeutralTargeting;
            }
        }

        /// <summary>
        /// Set to true to force enable targeting hostiles
        /// </summary>
        [ProtoIgnore]
        public bool EnableHostileTargeting
        {
            get
            {
                return TAI_ForceEnableTargetingFlags.HasFlag(HostileTargeting);
            }
            set
            {
                if (value) TAI_ForceEnableTargetingFlags |= HostileTargeting;

                else TAI_ForceEnableTargetingFlags &= ~HostileTargeting;
            }
        }

        /// <summary>
        /// Specifies the maximum range of the turret. Above this range the turret AI will not fire.
        /// <para>
        /// Units: Meters
        /// </para>
        /// <para>
        /// Should never be greater than maximum range. -1 to disable.
        /// </para>
        /// </summary>
        [ProtoMember(5)]
        public float TAI_ForceMaximumRange;

        /// <summary>
        /// Specifies the minimum range of the turret. Below this range the turret AI will not fire.
        /// <para>
        /// Units: Meters
        /// </para>
        /// <para>
        /// Should never be greater than maximum range. 0 to disable.
        /// </para>
        /// </summary>
        [ProtoMember(6)]
        public float TAI_MinimumRange;

        /// <summary>
        /// Set to true to force the turret to fire when it has a target. Useful for PD or projectile beam turrets.
        /// Note: For missile turrets, the missiles will point directly towards the target regardless of where the barrel is facing.
        /// </summary>
        [ProtoMember(7)]
        public bool TAI_ShootWhenTargetAquired;

        public TurretAI_Logic(int TAI_ResponseTime, TargetFlags TAI_DisableTargetingFlags, TargetFlags TAI_ForceEnableTargetingFlags, float TAI_ForceMaximumRange, float TAI_MinimumRange, bool TAI_ShootWhenTargetAquired)
        {
            this.TAI_ResponseTime = TAI_ResponseTime;
            this.TAI_DisableTargetingFlags = TAI_DisableTargetingFlags;
            this.TAI_ForceEnableTargetingFlags = TAI_ForceEnableTargetingFlags;
            this.TAI_ForceMaximumRange = TAI_ForceMaximumRange;
            this.TAI_MinimumRange = TAI_MinimumRange;
            this.TAI_ShootWhenTargetAquired = TAI_ShootWhenTargetAquired;
        }

        public override string ToString()
        {
            return 
                $"TAI_ResponseTime: {TAI_ResponseTime} \n" + 
                $"TAI_DisableTargetingFlags: {TAI_DisableTargetingFlags} \n" +
                $"TAI_ForceEnableTargetingFlags: {TAI_ForceEnableTargetingFlags} \n" +
                $"TAI_ForceMaximumRange: {TAI_ForceMaximumRange} \n" +
                $"TAI_MinimumRange: {TAI_MinimumRange}\n" +
                $"TAI_TAI_ShootWhenTargetAquired: {TAI_ShootWhenTargetAquired}\n";
        }
    }

    [ProtoContract]
    public class VPFTurretDefinition : VPFDefinition
    {
        /// <summary>
        /// Subtype of the turret you want to pair the stats to in <c>Cubeblocks.sbc</c>
        /// <para>
        /// Should never be the empty string or null.
        /// </para>
        /// </summary>
        [ProtoMember(1)]
        public string subtypeName = null;

        /// <summary>
        /// Struct for all the stats relating to modifying the turret's AI.
        /// <para>
        /// <para>
        /// Incompatible with: None
        /// </para>
        /// SET TO NULL IF YOU DO NOT WANT TO USE THIS LOGIC. HOW TO DO SO IS LISTED BELOW
        /// <code>
        /// TAI_Stats = null,
        /// </code>
        /// </para>
        /// </summary>
        [ProtoMember(2)]
        public TurretAI_Logic? TAI_Stats = null;
        /// <summary>
        /// Struct for all the stats relating to the turret self generating ammo.
        /// <para>
        /// <para>
        /// Incompatible with: None
        /// </para>
        /// SET TO NULL IF YOU DO NOT WANT TO USE THIS LOGIC. HOW TO DO SO IS LISTED BELOW
        /// <code>
        /// AG_Stats = null,
        /// </code>
        /// </para>
        /// </summary>
        [ProtoMember(3)]
        public AmmoGeneration_Logic? AG_Stats = null;

        public VPFTurretDefinition() { }

        public VPFTurretDefinition(string subtypeName, TurretAI_Logic? TAI_Stats, AmmoGeneration_Logic? AG_Stats)
        {
            this.subtypeName = subtypeName;
            this.TAI_Stats = TAI_Stats;
            this.AG_Stats = AG_Stats;
        }

        public override string ToString()
        {
            return $" SubtypeName: {subtypeName}" +
                $"\nTAI_Stats: {TAI_Stats}" +
                $"\nAG_Stats: {AG_Stats}";
        }

        public override bool Equals(object obj)
        {
            return obj is VPFTurretDefinition ? ((VPFTurretDefinition)obj).subtypeName == subtypeName : false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
