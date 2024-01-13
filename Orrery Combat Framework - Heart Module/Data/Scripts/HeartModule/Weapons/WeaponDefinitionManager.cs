using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using VRage.Utils;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    internal class WeaponDefinitionManager
    {
        private static SerializableWeaponDefinition DefaultDefinition = new SerializableWeaponDefinition()
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
                BlockSubtype = "TestWeapon",
                MuzzleSubpart = "",
                ElevationSubpart = "",
                AzimuthSubpart = "",
                DurabilityModifier = 1,
                InventoryIconName = "",
                Muzzles = new string[]
                {
                    "muzzle01",
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
                ShotInaccuracy = 0,
                LineOfSightCheck = true,
                ControlRotation = true,
            },
            Loading = new Loading()
            {
                RateOfFire = 30,
                BarrelsPerShot = 1,
                ProjectilesPerBarrel = 1,
                ReloadTime = 0,
                MagsToLoad = 1,
                DelayUntilFire = 0,
            },
        };

        private static SerializableWeaponDefinition TurretDefinition = new SerializableWeaponDefinition()
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
                BlockSubtype = "TestWeaponTurret",
                MuzzleSubpart = "TestEv",
                ElevationSubpart = "TestEv",
                AzimuthSubpart = "TestAz",
                DurabilityModifier = 1,
                InventoryIconName = "",
                Muzzles = new string[]
                {
                    "muzzle01",
                },
            },
            Hardpoint = new Hardpoint()
            {
                AzimuthRate = 0.5f,
                ElevationRate = 0.5f,
                MaxAzimuth = (float)Math.PI,
                MinAzimuth = (float)-Math.PI,
                MaxElevation = (float)Math.PI,
                MinElevation = (float)-Math.PI,
                IdlePower = 0,
                ShotInaccuracy = 0,//0.0075f,
                LineOfSightCheck = true,
                ControlRotation = true,
            },
            Loading = new Loading()
            {
                RateOfFire = 60,
                BarrelsPerShot = 1,
                ProjectilesPerBarrel = 1,
                ReloadTime = 0,
                MagsToLoad = 1,
                DelayUntilFire = 0,
            },
            Visuals = new Visuals()
            {
                ShootParticle = "",//"BlockDestroyedExplosion_Small",
                ContinuousShootParticle = false,
                ReloadParticle = "",
    },
        };

        // this is after the definitions because FUCKING STATICS ARE THE WORK OF THE DEVIL
        private static Dictionary<string, SerializableWeaponDefinition> Definitions = new Dictionary<string, SerializableWeaponDefinition>()
        {
            ["TestWeapon"] = DefaultDefinition,
            ["TestWeaponTurret"] = TurretDefinition,
        };

        public static SerializableWeaponDefinition GetDefinition(string subTypeId)
        {
            MyLog.Default.WriteLine(subTypeId + " | " + HasDefinition(subTypeId) + " | " + (Definitions[subTypeId] == null));
            if (HasDefinition(subTypeId))
                return Definitions[subTypeId];
            return null;
        }

        public static bool HasDefinition(string subTypeId)
        {
            return Definitions.ContainsKey(subTypeId);
        }
    }
}
