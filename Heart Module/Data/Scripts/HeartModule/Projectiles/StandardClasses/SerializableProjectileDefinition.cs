using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses
{
    /// <summary>
    /// Standard serializable projectile definition.
    /// </summary>
    [ProtoContract]
    internal class SerializableProjectileDefinition
    {
        public SerializableProjectileDefinition() { }

        [ProtoMember(1)] public string Name;
        [ProtoMember(2)] public Ungrouped Ungrouped;
        [ProtoMember(3)] public Damage Damage;
        [ProtoMember(4)] public PhysicalProjectile PhysicalProjectile;
        [ProtoMember(5)] public Visual Visual;
        [ProtoMember(5)] public Audio Audio;
        [ProtoMember(6)] 
    }

    [ProtoContract]
    internal struct Ungrouped
    {
        /// <summary>
        /// Power draw during reload, in MW
        /// </summary>
        [ProtoMember(1)] public float ReloadPowerUsage;
        /// <summary>
        /// Length of projectile, in Meters. For beams, range.
        /// </summary>
        [ProtoMember(2)] public float Length;
        /// <summary>
        /// Recoil of projectile, in Newtons
        /// </summary>
        [ProtoMember(3)] public int Recoil;
        /// <summary>
        /// Impulse of projectile, in Newtons
        /// </summary>
        [ProtoMember(4)] public int Impulse;
    }

    [ProtoContract]
    internal struct Damage
    {
        [ProtoMember(1)] public float SlimBlockDamageMod;
        [ProtoMember(2)] public float FatBlockDamageMod;
        [ProtoMember(3)] public float BaseDamage;
        [ProtoMember(4)] public float AreaDamage;
        [ProtoMember(5)] public int MaxImpacts;
    }

    /// <summary>
    /// Projectile information for non-hitscan projectiles.
    /// </summary>
    [ProtoContract]
    internal struct PhysicalProjectile
    {
        [ProtoMember(1)] float Speed;
        [ProtoMember(2)] float Acceleration;
        [ProtoMember(3)] float Health;
        [ProtoMember(4)] float MaxTrajectory;
        [ProtoMember(5)] float MaxLifetime;
    }

    [ProtoContract]
    internal struct Visual
    {
        [ProtoMember(1)] string Model;
        [ProtoMember(2)] string TrailTexture;
        [ProtoMember(3)] float TrailFadeTime;
        [ProtoMember(4)] string AttachedParticle;
        [ProtoMember(5)] string ImpactParticle;
        [ProtoMember(6)] float VisibleChance;
    }

    [ProtoContract]
    internal struct Audio
    {
        [ProtoMember(1)] string TravelSound;
        [ProtoMember(2)] string ImpactSound;
        [ProtoMember(3)] float ImpactSoundChance;
    }

    [ProtoContract]
    internal struct Guidance
    {

    }
}
