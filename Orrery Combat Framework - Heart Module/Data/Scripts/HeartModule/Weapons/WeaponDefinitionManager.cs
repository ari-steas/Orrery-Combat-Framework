using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    /// <summary>
    /// Collects and distributes all weapon definitions.
    /// </summary>
    internal class WeaponDefinitionManager
    {
        public static WeaponDefinitionManager I;

        // TODO: Unorganized list of definitions for single-block definitions
        private Dictionary<string, WeaponDefinitionBase> Definitions = new Dictionary<string, WeaponDefinitionBase>(); // TODO: Store serialized versions of definitions in case of modded functionality.
        private Dictionary<string, byte[]> SerializedDefinitions = new Dictionary<string, byte[]>();


        public static WeaponDefinitionBase GetDefinition(string subTypeId)
        {
            if (HasDefinition(subTypeId))
                return I.Definitions[subTypeId];
            return null;
        }

        public static byte[] GetSerializedDefinition(string subTypeId)
        {
            if (HasDefinition(subTypeId))
                return I.SerializedDefinitions[subTypeId];
            return null;
        }

        public static bool HasDefinition(string subTypeId)
        {
            return I.Definitions.ContainsKey(subTypeId);
        }

        public static bool UpdateDefinition(byte[] serializedDefinition)
        {
            var definition = MyAPIGateway.Utilities.SerializeFromBinary<WeaponDefinitionBase>(serializedDefinition);
            if (definition == null || !HasDefinition(definition.Assignments.BlockSubtype))
                return false;

            ApplyTrainingWheels(ref definition);

            I.Definitions[definition.Assignments.BlockSubtype] = definition;
            I.SerializedDefinitions[definition.Assignments.BlockSubtype] = serializedDefinition;
            return true;
        }

        public static bool UpdateDefinition(WeaponDefinitionBase definition)
        {
            if (!HasDefinition(definition.Assignments.BlockSubtype))
                return false;

            ApplyTrainingWheels(ref definition);

            I.Definitions[definition.Assignments.BlockSubtype] = definition;
            I.SerializedDefinitions[definition.Assignments.BlockSubtype] = MyAPIGateway.Utilities.SerializeToBinary(definition);
            return true;
        }

        public static bool RegisterModApiDefinition(byte[] serializedDefinition)
        {
            RegisterDefinition(serializedDefinition);
            return true; // TODO: Don't always return success
        }

        public static void RegisterDefinition(byte[] serializedDefinition)
        {
            if (serializedDefinition == null)
                return;

            var definition = MyAPIGateway.Utilities.SerializeFromBinary<WeaponDefinitionBase>(serializedDefinition);

            if (definition == null)
                return;

            if (I.Definitions.ContainsKey(definition.Assignments.BlockSubtype))
            {
                I.Definitions[definition.Assignments.BlockSubtype] = definition;
                I.SerializedDefinitions[definition.Assignments.BlockSubtype] = serializedDefinition;
                HeartLog.Log($"Duplicate weapon definition {definition.Assignments.BlockSubtype}! Overriding...");
            }
            else
            {
                I.Definitions.Add(definition.Assignments.BlockSubtype, definition);
                I.SerializedDefinitions.Add(definition.Assignments.BlockSubtype, serializedDefinition);
            }

            HeartData.I.OrreryBlockCategory.AddBlock(definition.Assignments.BlockSubtype);
            HeartLog.Log($"Registered weapon definition {definition.Assignments.BlockSubtype}.");

            if (HeartData.I.IsLoaded)
                WeaponManager.I.UpdateLogicOnExistingBlocks(definition);
        }

        public static void RegisterDefinition(WeaponDefinitionBase definition)
        {
            if (definition == null)
                return;

            ApplyTrainingWheels(ref definition);

            RegisterDefinition(MyAPIGateway.Utilities.SerializeToBinary(definition));
        }

        public static void RemoveDefinition(string subtype)
        {
            if (!HasDefinition(subtype))
                return;

            WeaponDefinitionBase definition = I.Definitions[subtype];
            WeaponManager.I.RemoveLogicOnExistingBlocks(definition);
            I.Definitions.Remove(subtype);
            I.SerializedDefinitions.Remove(subtype);

            HeartLog.Log("Removed weapon definition " + subtype);
        }

        public static int DefinitionCount()
        {
            return I.Definitions.Count;
        }

        public static string[] GetAllDefinitions()
        {
            return I.Definitions.Keys.ToArray();
        }

        public static void ClearDefinitions()
        {
            foreach (var id in GetAllDefinitions())
            {
                RemoveDefinition(id);
            }

            HeartLog.Log("Cleared all weapon definitions.");
        }

        /// <summary>
        /// Fixes weird definition values. TODO expand.
        /// </summary>
        /// <param name="input"></param>
        private static void ApplyTrainingWheels(ref WeaponDefinitionBase input)
        {
            if (input.Loading.RateOfFire > 500)
            {
                input.Loading.RateOfFire = 500;
                HeartLog.Log($"WeaponDefinitionManager.TrainingWheels: Definition {input.Assignments.BlockSubtype}'s firerate is over 500 rps!\nI've gone ahead and clamped it for you, but if you reeealllly want to break stuff, increase ProjectilesPerBarrel :)");
            }
        }
    }
}
