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
            }
        };

        private static SerializableWeaponDefinition TurretDefinition = new SerializableWeaponDefinition()
        {
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
            }
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
