using EmptyKeys.UserInterface.Generated.StoreBlockView_Bindings;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication.ProjectileBases;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;

namespace OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication
{
    partial class HeartDefinitions
    {
        ProjectileDefinitionBase ExampleAmmoProjectile => new ProjectileDefinitionBase()
        {
            Name = "ExampleAmmoProjectile",
            Ungrouped = new Ungrouped()
            {
                ReloadPowerUsage = 10,
                Recoil = 5000,
                Impulse = 5000,
                ShotsPerMagazine = 100,
                MagazineItemToConsume = "",
            },
            Damage = new Damage()
            {
                SlimBlockDamageMod = 1,
                FatBlockDamageMod = 1,
                BaseDamage = 5000,
                AreaDamage = 0,
                AreaRadius = 0,
                MaxImpacts = 1,
                DamageToProjectiles = 0.2f,
                DamageToProjectilesRadius = 0.2f,
            },
            PhysicalProjectile = new PhysicalProjectile()
            {
                Velocity = 800,
                VelocityVariance = 0,
                Acceleration = 0,
                Health = 1,
                MaxTrajectory = 4000,
                MaxLifetime = -1,
                IsHitscan = false,
                GravityInfluenceMultiplier = 0.01f,
            },
            Visual = new Visual()
            {
                //Model = "Models\\Weapons\\Projectile_Missile.mwm",
                TrailTexture = MyStringId.GetOrCompute("WeaponLaser"),
                TrailFadeTime = 0f,
                TrailLength = 8,
                TrailWidth = 0.5f,
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
            Guidance = new Guidance[]
            {
            },
            LiveMethods = new LiveMethods()
            {
               // OnImpact = (projectileInfo, hitPosition, hitNormal, hitEntity) =>
               // {
               //     if (hitEntity == null)
               //         return;
               //     HeartApi.SpawnProjectilesInCone(HeartApi.GetProjectileDefinitionId(ExampleAmmoMissile.Name), hitPosition - hitNormal * 50, hitNormal, 10, 0.1f);
               // }
            }
        };

        ProjectileDefinitionBase ExampleAmmoMissile => new ProjectileDefinitionBase()
        {
            Name = "ExampleAmmoMissile",
            Ungrouped = new Ungrouped()
            {
                ReloadPowerUsage = 0,
                Recoil = 0,
                Impulse = 5000,
                ShotsPerMagazine = 10,
                MagazineItemToConsume = "",
            },
            Damage = new Damage()
            {
                SlimBlockDamageMod = 1,
                FatBlockDamageMod = 1,
                BaseDamage = 5000,
                AreaDamage = 0,
                AreaRadius = 0,
                MaxImpacts = 1,
            },
            PhysicalProjectile = new PhysicalProjectile()
            {
                Velocity = 800,
                VelocityVariance = 0,
                Acceleration = 1,
                Health = 1,
                MaxTrajectory = 4000,
                MaxLifetime = -1,
                IsHitscan = false,
                ProjectileSize = 1,
            },
            Visual = new Visual()
            {
                Model = "Models\\Weapons\\Projectile_Missile.mwm",
                //TrailTexture = MyStringId.GetOrCompute("WeaponLaser"),
                //TrailFadeTime = 0f,
                //TrailLength = 8,
                //TrailWidth = 0.5f,
                //TrailColor = new VRageMath.Vector4(61, 24, 24, 200),
                AttachedParticle = "Smoke_Missile",
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
            Guidance = new Guidance[]
            {
                new Guidance()
                {
                    TriggerTime = 2,
                    ActiveDuration = -1,
                    UseAimPrediction = false,
                    MaxTurnRate = 1f,
                    IFF = IFF_Enum.TargetEnemies,
                    DoRaycast = false,
                    CastCone = 0.5f,
                    CastDistance = 1000,
                    Velocity = 50f,
                },
            },
            LiveMethods = new LiveMethods()
            {

                OnSpawn = (ProjectileId, Firer) => {
                    MyVisualScriptLogicProvider.SendChatMessage(ExampleAmmoMissile.Name + "WARNING: SOMEONE HAS FIRED A MISSILE WITHOUT A PID! LAUGH AT HIM!");
                },

                //OnSpawn = (ProjectileId, Firer) => {
                //    HeartApi.LogWriteLine("OnSpawn " + ProjectileId + " | " + HeartApi.BlockHasWeapon(Firer));
                //    },
                //OnImpact = (ProjectileId, HitPos, HitNormal, HitEntity) => HeartApi.LogWriteLine("OnImpact " + ProjectileId),
                //OnEndOfLife = (ProjectileId) => HeartApi.LogWriteLine("EndOfLife " + ProjectileId),
            }
        };

        ProjectileDefinitionBase ExampleAmmoMissilePID => new ProjectileDefinitionBase()
        {
            Name = "ExampleAmmoMissilePID",
            Ungrouped = new Ungrouped()
            {
                ReloadPowerUsage = 0,
                Recoil = 0,
                Impulse = 5000,
                ShotsPerMagazine = 10,
                MagazineItemToConsume = "",
            },
            Damage = new Damage()
            {
                SlimBlockDamageMod = 1,
                FatBlockDamageMod = 1,
                BaseDamage = 5000,
                AreaDamage = 0,
                AreaRadius = 0,
                MaxImpacts = 1,
            },
            PhysicalProjectile = new PhysicalProjectile()
            {
                Velocity = 800,
                VelocityVariance = 0,
                Acceleration = 1,
                Health = 1,
                MaxTrajectory = 4000,
                MaxLifetime = -1,
                IsHitscan = false,
                ProjectileSize = 1,
            },
            Visual = new Visual()
            {
                Model = "Models\\Weapons\\Projectile_Missile.mwm",
                TrailTexture = MyStringId.GetOrCompute("WeaponLaser"),
                //TrailFadeTime = 0f,
                //TrailLength = 8,
                //TrailWidth = 0.5f,
                TrailColor = new VRageMath.Vector4(61, 24, 24, 200),
                AttachedParticle = "Smoke_Missile",
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
            Guidance = new Guidance[]
            {
                new Guidance()
                {
                    TriggerTime = 0,
                    ActiveDuration = -1,
                    UseAimPrediction = false,
                    MaxTurnRate = 0f,
                    IFF = IFF_Enum.TargetEnemies,
                    DoRaycast = false,
                    CastCone = 0.5f,
                    CastDistance = 1000,
                    Velocity = 50f,
                    //MaxGs = 99f,

                },
                new Guidance()
                {
                    TriggerTime = 2f,
                    ActiveDuration = -1f,
                    UseAimPrediction = true,
                    MaxTurnRate = -1f,
                    IFF = IFF_Enum.TargetEnemies,
                    DoRaycast = false,
                    CastCone = 0.5f,
                    CastDistance = 1000,
                    Velocity = 50f,
                    Inaccuracy = 5f,
                    MaxGs = 99f,

                    PID = new Definition_PID()
                    {
                        kProportional = 0.2f,
                        kIntegral = 0.2f,
                        kDerivative = 0.2f,
                    }
                }
            },
            LiveMethods = new LiveMethods()
            {
                //OnSpawn = (ProjectileId, Firer) => {
                //    HeartApi.LogWriteLine("OnSpawn " + ProjectileId + " | " + HeartApi.BlockHasWeapon(Firer));
                //    },
                //OnImpact = (ProjectileId, HitPos, HitNormal, HitEntity) => HeartApi.LogWriteLine("OnImpact " + ProjectileId),
                //OnEndOfLife = (ProjectileId) => HeartApi.LogWriteLine("EndOfLife " + ProjectileId),
            }
        };

        ProjectileDefinitionBase ExampleAmmoBeam => new ProjectileDefinitionBase()
        {
            Name = "ExampleAmmoBeam",
            Ungrouped = new Ungrouped()
            {
                ReloadPowerUsage = 0,
                Recoil = 0,
                Impulse = 0,
                ShotsPerMagazine = 1,
                MagazineItemToConsume = "",
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
                VelocityVariance = 0,
                Acceleration = 0,
                Health = 1,
                MaxTrajectory = 4000,
                MaxLifetime = -1,
                IsHitscan = true,
            },
            Visual = new Visual()
            {
                //Model = "Models\\Weapons\\Projectile_Missile.mwm",
                TrailTexture = MyStringId.GetOrCompute("WeaponLaser"),
                TrailFadeTime = 0f,
                TrailLength = 8,
                TrailWidth = 0.5f,
                TrailColor = new VRageMath.Vector4(61, 24, 24, 200),
                //AttachedParticle = "Smoke_Missile",
                ImpactParticle = "",
                VisibleChance = 1f,
            },
            Audio = new Audio()
            {
                TravelSound = "",
                TravelVolume = 100,
                TravelMaxDistance = 1000,
                ImpactSound = "",
                SoundChance = 0.1f,
            },
            Guidance = new Guidance[]
            {
                //new Guidance()
                //{
                //    TriggerTime = 0,
                //    ActiveDuration = -1,
                //    UseAimPrediction = false,
                //    MaxTurnRate = -1.5f,
                //    IFF = 2,
                //    DoRaycast = false,
                //    CastCone = 0.5f,
                //    CastDistance = 1000,
                //    Velocity = 50f,
                //},
                //new Guidance()
                //{
                //    TriggerTime = 1f,
                //    ActiveDuration = -1f,
                //    UseAimPrediction = false,
                //    MaxTurnRate = 3.14f,
                //    IFF = 2,
                //    DoRaycast = false,
                //    CastCone = 0.5f,
                //    CastDistance = 1000,
                //    Velocity = -1f,
                //}
            },
            LiveMethods = new LiveMethods()
            {

            }
        };

        ProjectileDefinitionBase Hotloaded => new ProjectileDefinitionBase()
        {
            Name = "Hotloaded",
            Ungrouped = new Ungrouped()
            {
                ReloadPowerUsage = 10,
                Recoil = 5000,
                Impulse = 5000,
                ShotsPerMagazine = 100,
                MagazineItemToConsume = "",
            },
            Damage = new Damage()
            {
                SlimBlockDamageMod = 1,
                FatBlockDamageMod = 1,
                BaseDamage = 5000,
                AreaDamage = 0,
                AreaRadius = 0,
                MaxImpacts = 1,
                DamageToProjectiles = 0.2f,
                DamageToProjectilesRadius = 0.2f,
            },
            PhysicalProjectile = new PhysicalProjectile()
            {
                Velocity = 800,
                VelocityVariance = 400,
                Acceleration = 0,
                Health = 1,
                MaxTrajectory = 4000,
                MaxLifetime = -1,
                IsHitscan = false,
                AccuracyVarianceMultiplier = 10f,
            },
            Visual = new Visual()
            {
                //Model = "Models\\Weapons\\Projectile_Missile.mwm",
                TrailTexture = MyStringId.GetOrCompute("WeaponLaser"),
                TrailFadeTime = 0f,
                TrailLength = 8,
                TrailWidth = 0.5f,
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
            Guidance = new Guidance[]
            {
            },
            LiveMethods = new LiveMethods()
            {
            }
        };


        ProjectileDefinitionBase THEFOAMINATOR => new ProjectileDefinitionBase()
        {
            Name = "ExampleAmmoProjectile",
            Ungrouped = new Ungrouped()
            {
                ReloadPowerUsage = 10,
                Recoil = 5000,
                Impulse = 5000,
                ShotsPerMagazine = 100,
                MagazineItemToConsume = "",
            },
            Damage = new Damage()
            {
                SlimBlockDamageMod = 1,
                FatBlockDamageMod = 1,
                BaseDamage = 5000,
                AreaDamage = 0,
                AreaRadius = 0,
                MaxImpacts = 1,
                DamageToProjectiles = 0.2f,
                DamageToProjectilesRadius = 0.2f,
            },
            PhysicalProjectile = new PhysicalProjectile()
            {
                Velocity = 800,
                VelocityVariance = 400,
                Acceleration = 0,
                Health = 1,
                MaxTrajectory = 4000,
                MaxLifetime = -1,
                IsHitscan = false,
            },
            Visual = new Visual()
            {
                //Model = "Models\\Weapons\\Projectile_Missile.mwm",
                TrailTexture = MyStringId.GetOrCompute("WeaponLaser"),
                TrailFadeTime = 0f,
                TrailLength = 8,
                TrailWidth = 0.5f,
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
            Guidance = new Guidance[]
            {
            },
            LiveMethods = new LiveMethods()
            {
                OnImpact = (projectileInfo, hitPosition, hitNormal, hitEntity) =>
                {
                    if (hitEntity == null)
                        return;
                    HeartApi.SpawnProjectilesInCone(HeartApi.GetProjectileDefinitionId(ExampleAmmoMissile.Name), hitPosition - hitNormal * 50, hitNormal, 10, 0.1f);
                }
            }
        };


    }
}
