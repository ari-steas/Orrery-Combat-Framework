using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using VRage.Utils;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    internal class ProjectileDefinitionManager
    {
        // TODO replace with actual logic
        private static SerializableProjectileDefinition DefaultDefinition = new SerializableProjectileDefinition()
        {
            Name = "TestProjectile",
            Ungrouped = new Ungrouped()
            {
                ReloadPowerUsage = 0,
                Recoil = 0,
                Impulse = 0,
            },
            Damage = new Damage()
            {
                SlimBlockDamageMod = 1,
                FatBlockDamageMod = 1,
                BaseDamage = 1000,
                AreaDamage = 0,
                AreaRadius = 0,
                MaxImpacts = 1,
            },
            PhysicalProjectile = new PhysicalProjectile()
            {
                Velocity = 800,
                Acceleration = 0,
                Health = -1,
                MaxTrajectory = 1000,
                MaxLifetime = -1,
                IsHitscan = false,
            },
            Visual = new Visual()
            {
                //Model = "Models\\Weapons\\Projectile_Missile.mwm",
                TrailTexture = MyStringId.GetOrCompute("WeaponLaser"),
                TrailFadeTime = 0f,
                TrailLength = 4,
                TrailWidth = 0.25f,
                TrailColor = new VRageMath.Vector4(61, 24, 24, 200),
                //AttachedParticle = "Smoke_Missile",
                ImpactParticle = "MaterialHit_Metal",
                VisibleChance = 1f,
            },
            Audio = new Audio()
            {
                TravelSound = "",
                TravelVolume = 100,
                TravelMaxDistance = 1000,
                ImpactSound = "WepSmallWarheadExpl",
                SoundChance = 0.1f,
            },
            Guidance = new Guidance[0],
            LiveMethods = new LiveMethods()
            {
                DoOnShoot = false,
                DoOnImpact = false,
                DoUpdate1 = false,
            }
        };

        public static SerializableProjectileDefinition GetDefinition(int id)
        {
            return DefaultDefinition;
        }

        public static bool HasDefinition(int id)
        {
            return true;
        }
    }
}
