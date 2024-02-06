using OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication.WeaponBases;
using System;

namespace OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication
{
    partial class HeartDefinitions
    {
        WeaponDefinitionBase ExampleFixedBeamWeapon => new WeaponDefinitionBase()
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
                BlockSubtype = "SC_AR_LocusGleam",
                MuzzleSubpart = "",
                ElevationSubpart = "",
                AzimuthSubpart = "",
                DurabilityModifier = 1,
                InventoryIconName = "",
                Muzzles = new string[]
                {
                    "muzzle_beam_01",
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
                ShotInaccuracy = 0f,
                LineOfSightCheck = true,
                ControlRotation = true,
            },
            Loading = new Loading()
            {
                Ammos = new string[]
                {
                    ExampleAmmoBeam.Name,
                },

                RateOfFire = 60,
                RateOfFireVariance = 0f,
                BarrelsPerShot = 1,
                ProjectilesPerBarrel = 1,
                ReloadTime = 0,
                DelayUntilFire = 0,
                MagazinesToLoad = 1,

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
