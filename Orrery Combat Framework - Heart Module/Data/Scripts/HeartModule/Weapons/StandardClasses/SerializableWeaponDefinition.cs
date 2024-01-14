using ProtoBuf;
using System;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses
{
    /// <summary>
    /// Standard serializable weapon definition.
    /// </summary>
    [ProtoContract]
    public class SerializableWeaponDefinition
    {
        public SerializableWeaponDefinition() { }
    
        [ProtoMember(2)] public Targeting Targeting;
        [ProtoMember(3)] public Assignments Assignments;
        [ProtoMember(4)] public Hardpoint Hardpoint;
        [ProtoMember(5)] public Loading Loading;
        [ProtoMember(6)] public Audio Audio;
        [ProtoMember(7)] public Visuals Visuals;
        [ProtoMember(8)] public string[] Ammos; // TODO
    }

    [ProtoContract]
    public struct Targeting
    {
        /// <summary>
        /// The furthest target a turret can shoot.
        /// </summary>
        [ProtoMember(1)] public float MaxTargetingRange;
        /// <summary>
        /// The closest target a turret can shoot.
        /// </summary>
        [ProtoMember(2)] public float MinTargetingRange;
        /// <summary>
        /// Can the turret fire by itself? Tracks regardless.
        /// </summary>
        [ProtoMember(3)] public bool CanAutoShoot;
        [ProtoMember(4)] public IFF_Enum IFF; // TODO
        [ProtoMember(5)] public TargetType_Enum TargetTypes; // TODO
        /// <summary>
        /// Time until the turret is forced to find a new target
        /// </summary>
        [ProtoMember(6)] public float RetargetTime; // TODO
        [ProtoMember(7)] public float AimTolerance;

        public enum IFF_Enum
        {
            TargetSelf = 1,
            TargetEnemies = 2,
            TargetFriendlies = 4,
        }

        public enum TargetType_Enum
        {
            TargetGrids = 1,
            TargetProjectiles = 2,
            TargetCharacters = 4,
        }
    }

    [ProtoContract]
    public struct Assignments
    {
        [ProtoMember(1)] public string BlockSubtype;
        [ProtoMember(2)] public string MuzzleSubpart; // TODO allow recursion
        [ProtoMember(3)] public string ElevationSubpart;
        [ProtoMember(4)] public string AzimuthSubpart;
        [ProtoMember(5)] public float DurabilityModifier;
        [ProtoMember(6)] public string InventoryIconName;
        [ProtoMember(7)] public string[] Muzzles;

        public bool HasElevation => !ElevationSubpart?.Equals("") ?? false;
        public bool HasAzimuth => !AzimuthSubpart?.Equals("") ?? false;
        public bool HasMuzzleSubpart => !MuzzleSubpart?.Equals("") ?? false;
        public bool IsTurret => HasAzimuth && HasElevation;
    }

    [ProtoContract]
    public struct Hardpoint
    {
        // ALL VALUES IN RADIANS
        [ProtoMember(1)] public float AzimuthRate;
        [ProtoMember(2)] public float ElevationRate;
        [ProtoMember(3)] public float MaxAzimuth;
        [ProtoMember(4)] public float MinAzimuth;
        [ProtoMember(5)] public float MaxElevation;
        [ProtoMember(6)] public float MinElevation;
        [ProtoMember(7)] public float IdlePower;
        [ProtoMember(8)] public float ShotInaccuracy;
        [ProtoMember(9)] public bool LineOfSightCheck;
        [ProtoMember(10)] public bool ControlRotation;

        [ProtoMember(11)] public float HomeAzimuth;
        [ProtoMember(12)] public float HomeElevation;

        public bool CanRotateFull => MaxAzimuth >= -(float)Math.PI && MinAzimuth <= -(float)Math.PI;
        public bool CanElevateFull => MaxElevation >= -(float)Math.PI && MinElevation <= -(float)Math.PI;
    }

    [ProtoContract]
    public struct Loading
    {
        [ProtoMember(1)] public int RateOfFire; // Shots per second
        [ProtoMember(2)] public int BarrelsPerShot; // TODO
        [ProtoMember(3)] public int ProjectilesPerBarrel; // TODO
        [ProtoMember(4)] public float ReloadTime; // Seconds // TODO
        [ProtoMember(5)] public int MagsToLoad; // TODO
        [ProtoMember(6)] public float DelayUntilFire; // Seconds // TODO
        [ProtoMember(7)] public Resource[] Resources; // TODO

        [ProtoContract]
        public struct Resource // TODO
        {
            [ProtoMember(1)] public string ResourceType;
            [ProtoMember(2)] public float ResourceGeneration; // Per second
            [ProtoMember(3)] public float ResourceStorage;
            [ProtoMember(4)] public float ResourcePerShot;
            [ProtoMember(5)] public float MinResourceBeforeFire;
            // TODO: Action OnConsume
        }
    }

    [ProtoContract]
    public struct Audio
    {
        [ProtoMember(1)] public string PreShootSound; // TODO
        [ProtoMember(2)] public string ShootSound; // TODO
        [ProtoMember(3)] public string ReloadSound; // TODO
        [ProtoMember(4)] public string RotationSound; // TODO
    }

    [ProtoContract]
    public struct Visuals
    {
        [ProtoMember(1)] public string ShootParticle;
        [ProtoMember(2)] public bool ContinuousShootParticle; // TODO
        [ProtoMember(3)] public string ReloadParticle; // TODO

        public bool HasShootParticle => !ShootParticle?.Equals("") ?? false;
        public bool HasReloadParticle => !ReloadParticle?.Equals("") ?? false;
    }
}
