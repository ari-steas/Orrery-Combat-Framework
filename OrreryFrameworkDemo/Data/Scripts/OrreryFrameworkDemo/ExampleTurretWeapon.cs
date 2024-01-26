using OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication.WeaponBases;
using System;

namespace OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication
{
    partial class HeartDefinitions
    {
        WeaponDefinitionBase ExampleTurretWeapon => new WeaponDefinitionBase()
        {
            Targeting = new Targeting()
            {
                MaxTargetingRange = 1000,
                MinTargetingRange = 0,
                CanAutoShoot = true,
                RetargetTime = -1,
                AimTolerance = 0.0175f,
                IFF = Targeting.IFF_Enum.TargetEnemies,
                TargetTypes = Targeting.TargetType_Enum.TargetGrids | Targeting.TargetType_Enum.TargetProjectiles | Targeting.TargetType_Enum.TargetCharacters
            },
            Assignments = new Assignments()
            {
                BlockSubtype = "TestWeaponTurret",
                MuzzleSubpart = "TestEv",
                ElevationSubpart = "TestEv",
                AzimuthSubpart = "TestAz",
                DurabilityModifier = 1,
                InventoryIconName = "",
                Muzzles = new string[]
                {
                    "muzzle01",
                    "muzzle02",
                },
            },
            Hardpoint = new Hardpoint()
            {
                AzimuthRate = 0.5f,
                ElevationRate = 0.5f,
                MaxAzimuth = (float)Math.PI,
                MinAzimuth = (float)-Math.PI,
                MaxElevation = (float)Math.PI,
                MinElevation = -0.1745f,
                HomeAzimuth = 0,
                HomeElevation = (float)Math.PI / 4,
                IdlePower = 10,
                ShotInaccuracy = 0.0025f,
                LineOfSightCheck = true,
                ControlRotation = true,
            },
            Loading = new Loading()
            {
                Ammos = new string[]
                {
                    ExampleAmmoProjectile.Name,
                },

                RateOfFire = 10,
                BarrelsPerShot = 1,
                ProjectilesPerBarrel = 1,
                ReloadTime = 2,
                DelayUntilFire = 0,

                MaxReloads = -1,
            },
            Audio = new Audio()
            {
                PreShootSound = "",
                ShootSound = "",
                ReloadSound = "",
                RotationSound = "",
            },
            Visuals = new Visuals()
            {
                ShootParticle = "Muzzle_Flash_Autocannon",
                ContinuousShootParticle = false,
                ReloadParticle = "",
            },
        };
    }
}
