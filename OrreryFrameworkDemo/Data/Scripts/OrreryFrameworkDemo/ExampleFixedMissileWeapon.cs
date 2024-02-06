using OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication.WeaponBases;
using System;

namespace OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication
{
    partial class HeartDefinitions
    {
        WeaponDefinitionBase ExampleFixedMissileWeapon => new WeaponDefinitionBase()
        {
            Targeting = new Targeting()
            {
                MinTargetingRange = 0,
                MaxTargetingRange = 1000,
                CanAutoShoot = true,
                RetargetTime = -1,
                AimTolerance = 0.0175f,
            },
            Assignments = new Assignments()
            {
                BlockSubtype = "OCF_ExampleMissileLauncher",
                MuzzleSubpart = "",
                ElevationSubpart = "",
                AzimuthSubpart = "",
                DurabilityModifier = 1,
                InventoryIconName = "",
                Muzzles = new string[]
                {
                    "Muzzle_10",
                    "Muzzle_01",
                    "Muzzle_02",
                    "Muzzle_03",
                    "Muzzle_04",
                    "Muzzle_05",
                    "Muzzle_06",
                    "Muzzle_07",
                    "Muzzle_08",
                    "Muzzle_09",
                },
            },
            Hardpoint = new Hardpoint()
            {
                AzimuthRate = 0.01f,
                ElevationRate = 0.01f,
                MaxAzimuth = (float)Math.PI,
                MinAzimuth = (float)-Math.PI,
                MaxElevation = (float)Math.PI / 4,
                MinElevation = (float)-Math.PI / 4,
                IdlePower = 0,
                ShotInaccuracy = 0.0175f,
                LineOfSightCheck = true,
                ControlRotation = true,
            },
            Loading = new Loading()
            {
                Ammos = new string[]
                {
                    ExampleAmmoMissile.Name,
                },

                RateOfFire = 20,
                RateOfFireVariance = 0f,
                BarrelsPerShot = 1,
                ProjectilesPerBarrel = 1,
                ReloadTime = 6,
                DelayUntilFire = 0,
                MagazinesToLoad = 2,

                MaxReloads = -1,
            },
            Audio = new Audio()
            {
                PreShootSound = "",
                ShootSound = "ArcWepShipOnyxPlasmaHelios_Fire",
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
