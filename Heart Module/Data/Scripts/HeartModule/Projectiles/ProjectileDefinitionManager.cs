using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;

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
                SlimBlockDamageMod = 1,
                FatBlockDamageMod = 1,
                BaseDamage = 100,
                AreaDamage = 0,
                MaxImpacts = 1,
            },
            PhysicalProjectile = new PhysicalProjectile()
            {
                Speed = 100,
                Acceleration = 0,
                Health = -1,
                MaxTrajectory = 800,
                MaxLifetime = -1,
            },
            Visual = new Visual()
            {
                Model = "",
                TrailTexture = "",
                TrailFadeTime = 0,
                AttachedParticle = "",
                ImpactParticle = "",
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
