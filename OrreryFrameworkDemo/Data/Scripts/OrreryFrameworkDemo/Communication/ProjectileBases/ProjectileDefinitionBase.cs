using ProtoBuf;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;

namespace OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication.ProjectileBases
{
    /// <summary>
    /// Standard serializable projectile definition.
    /// </summary>
    [ProtoContract]
    public class ProjectileDefinitionBase
    {
        public ProjectileDefinitionBase() { }

        [ProtoMember(99)] public int InternalId = 0; // TODO: Deterministic weapon IDs to avoid syncing )))
        [ProtoMember(1)] public string Name = "";
        [ProtoMember(2)] public Ungrouped Ungrouped;
        [ProtoMember(3)] public Damage Damage;
        [ProtoMember(4)] public PhysicalProjectile PhysicalProjectile;
        [ProtoMember(5)] public Visual Visual;
        [ProtoMember(6)] public Audio Audio;
        [ProtoMember(7)] public Guidance[] Guidance = new Guidance[0];
        [ProtoMember(8)] public LiveMethods LiveMethods = new LiveMethods();
    }

    [ProtoContract]
    public struct Ungrouped
    {
        /// <summary>
        /// Power draw during reload, in MW
        /// </summary>
        [ProtoMember(1)] public float ReloadPowerUsage; // TODO
        /// <summary>
        /// Recoil of projectile, in Newtons
        /// </summary>
        [ProtoMember(2)] public int Recoil;
        /// <summary>
        /// Impulse of projectile, in Newtons
        /// </summary>
        [ProtoMember(3)] public int Impulse;
        /// <summary>
        /// Number of shots in single reload.
        /// </summary>
        [ProtoMember(4)] public int ShotsPerMagazine;
    }

    [ProtoContract]
    public struct Damage
    {
        [ProtoMember(1)] public float SlimBlockDamageMod;
        [ProtoMember(2)] public float FatBlockDamageMod;
        [ProtoMember(3)] public float BaseDamage;
        [ProtoMember(4)] public float AreaDamage;
        [ProtoMember(5)] public int MaxImpacts;
        [ProtoMember(6)] public float AreaRadius;
    }

    /// <summary>
    /// Projectile information for non-hitscan projectiles.
    /// </summary>
    [ProtoContract]
    public struct PhysicalProjectile
    {
        [ProtoMember(1)] public float Velocity;
        [ProtoMember(2)] public float Acceleration;
        [ProtoMember(3)] public float Health; // TODO // -1 for un-targetable
        /// <summary>
        /// Max range of projectile, relative to first firing. For hitscans, max hitscan length.
        /// </summary>
        [ProtoMember(4)] public float MaxTrajectory;
        [ProtoMember(5)] public float MaxLifetime;
        /// <summary>
        /// Disables velocity updates, and changes several behaviors. Call (Projectile).UpdateBeam() to recycle and lower performance impact.
        /// </summary>
        [ProtoMember(6)] public bool IsHitscan;
    }

    [ProtoContract]
    public struct Visual
    {
        [ProtoMember(1)] public string Model;
        [ProtoMember(2)] public MyStringId TrailTexture;
        [ProtoMember(7)] public float TrailLength;
        [ProtoMember(9)] public float TrailWidth;
        [ProtoMember(8)] public Vector4 TrailColor;
        [ProtoMember(3)] public float TrailFadeTime;
        [ProtoMember(4)] public string AttachedParticle;
        [ProtoMember(5)] public string ImpactParticle;
        [ProtoMember(6)] public float VisibleChance;
        public bool HasModel => !Model?.Equals("") ?? false;
        public bool HasTrail => TrailTexture != null && TrailLength > 0 && TrailWidth > 0 && TrailColor != null && TrailColor != Vector4.Zero;
        public bool HasAttachedParticle => !AttachedParticle?.Equals("") ?? false;
        public bool HasImpactParticle => !ImpactParticle?.Equals("") ?? false;
    }

    [ProtoContract]
    public struct Audio
    {
        [ProtoMember(1)] public string TravelSound;
        [ProtoMember(2)] public float TravelMaxDistance;
        [ProtoMember(3)] public float TravelVolume;
        [ProtoMember(4)] public string ImpactSound;
        [ProtoMember(5)] public float SoundChance;

        public bool HasTravelSound => !TravelSound?.Equals("") ?? false && SoundChance > 0 && TravelMaxDistance > 0 && TravelVolume > 0;
        public bool HasImpactSound => !ImpactSound?.Equals("") ?? false && SoundChance > 0;
        public MySoundPair TravelSoundPair => new MySoundPair(TravelSound);
        public MySoundPair ImpactSoundPair => new MySoundPair(ImpactSound);
    }

    [ProtoContract]
    public struct Guidance
    {
        [ProtoMember(1)] public float TriggerTime;
        [ProtoMember(2)] public float ActiveDuration; // Ignore if -1 or greater than next
        [ProtoMember(3)] public bool UseAimPrediction;
        [ProtoMember(4)] public float TurnRate;
        [ProtoMember(6)] public IFF_Enum IFF; // 1 is TargetSelf, 2 is TargetEnemies, 4 is TargetFriendlies // TODO
        [ProtoMember(7)] public bool DoRaycast;
        [ProtoMember(8)] public float CastCone;
        [ProtoMember(9)] public float CastDistance;
        [ProtoMember(10)] public float Velocity;
        /// <summary>
        /// Random offset from target, in meters.
        /// </summary>
        [ProtoMember(11)] public float Inaccuracy;
    }

    [ProtoContract]
    public class LiveMethods
    {
        [ProtoMember(1)] public bool DoOnShoot; // TODO
        [ProtoMember(2)] public bool DoOnImpact; // TODO
        [ProtoMember(3)] public bool DoUpdate1; // TODO

        // TODO move to definition, and seperate
        Dictionary<string, Delegate> liveMethods = new Dictionary<string, Delegate>()
        {
            ["OnShoot"] = new Action<uint, MyEntity>(BaseOnShoot),
            ["OnImpact"] = new Action<uint, MyEntity, MyEntity, bool>(BaseOnImpact),
            ["Update1"] = new Action<uint, MyEntity>(BaseUpdate1),
        };

        private static void BaseOnShoot(uint ProjectileId, MyEntity Shooter) { }
        private static void BaseOnImpact(uint ProjectileId, MyEntity Shooter, MyEntity ImpactEntity, bool EndOfLife) { }
        private static void BaseUpdate1(uint ProjectileId, MyEntity Shooter) { }
    }
}