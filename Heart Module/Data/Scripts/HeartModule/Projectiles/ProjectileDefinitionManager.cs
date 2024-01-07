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
                Length = 1,
                Recoil = 0,
                Impulse = 0,
            },
            Damage = new Damage()
            {
                SlimBlockDamageMod = 25,
                FatBlockDamageMod = 1,
                BaseDamage = 100,
                AreaDamage = 100,
                AreaRadius = 15,
                MaxImpacts = 1,
            },
            PhysicalProjectile = new PhysicalProjectile()
            {
                Velocity = 1000,
                Acceleration = 0,
                Health = -1,
                MaxTrajectory = 800,
                MaxLifetime = -1,
            },
            Visual = new Visual()
            {
                Model = "Models\\Weapons\\Projectile_Missile.mwm",
                TrailTexture = MyStringId.GetOrCompute("WeaponLaser"),
                TrailFadeTime = 0,
                TrailLength = 1,
                TrailWidth = 0.1f,
                //TrailColor = new VRageMath.Vector4(61, 24, 24, 200),
                AttachedParticle = "Smoke_Missile",
                ImpactParticle = "Explosion_LargeCaliberShell_Backup",
                VisibleChance = 1,
            },
            Audio = new Audio()
            {
                TravelSound = "",
                ImpactSound = "",
                ImpactSoundChance = 1,
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
