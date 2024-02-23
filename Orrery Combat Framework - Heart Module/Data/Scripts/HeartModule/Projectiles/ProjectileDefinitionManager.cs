using Heart_Module.Data.Scripts.HeartModule.Definitions;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game;
using VRage.Utils;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    /// <summary>
    /// Collects and distributes all projectile definitions.
    /// </summary>
    internal class ProjectileDefinitionManager
    {
        public static ProjectileDefinitionManager I;
        private List<ProjectileDefinitionBase> Definitions = new List<ProjectileDefinitionBase>(); // TODO: Store serialized versions of definitions in case of modded functionality
        private List<byte[]> SerializedDefinitions = new List<byte[]>();
        private Dictionary<string, int> DefinitionNamePairs = new Dictionary<string, int>();

        public static ProjectileDefinitionBase GetDefinition(int id)
        {
            if (HasDefinition(id))
                return I.Definitions[id];
            else
                return null;
        }

        public static byte[] GetSerializedDefinition(int id)
        {
            if (HasDefinition(id))
                return I.SerializedDefinitions[id];
            else
                return null;
        }

        public static ProjectileDefinitionBase GetDefinition(string name)
        {
            return GetDefinition(GetId(name));
        }

        public static byte[] GetSerializedDefinition(string name)
        {
            return GetSerializedDefinition(GetId(name));
        }

        public static int GetId(string definitionName)
        {
            if (HasDefinition(definitionName))
                return I.DefinitionNamePairs[definitionName];
            return -1;
        }

        public static int GetId(ProjectileDefinitionBase definition)
        {
            return I.Definitions.IndexOf(definition);
        }

        public static bool HasDefinition(string name)
        {
            if (name == null)
                return false;
            return I.DefinitionNamePairs.ContainsKey(name);
        }

        public static bool HasDefinition(int id)
        {
            return I.Definitions.Count > id && id >= 0 && I.Definitions[id] != null;
        }

        /// <summary>
        /// Use this when creating a definiton live.
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        public static int RegisterModApiDefinition(byte[] serializedDefinition)
        {
            return RegisterDefinition(serializedDefinition);
        }

        /// <summary>
        /// Registers a projectile definition.
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="syncToClients"></param>
        /// <returns></returns>
        public static int RegisterDefinition(ProjectileDefinitionBase definition)
        {
            if (I.DefinitionNamePairs.ContainsKey(definition.Name))
            {
                HeartLog.Log($"Duplicate ammo definition {definition.Name}! Skipping...");
                return -1;
            }

            var serializedDefinition = MyAPIGateway.Utilities.SerializeToBinary(definition);

            I.Definitions.Add(definition);
            I.SerializedDefinitions.Add(serializedDefinition);
            I.DefinitionNamePairs.Add(definition.Name, I.Definitions.Count - 1);
            if (MyAPIGateway.Session.IsServer)
                HeartData.I.Net.SendToEveryone(new n_ProjectileDefinitionIdSync(
                    I.Definitions.Count - 1,
                    definition.Name,
                    serializedDefinition
                    ));
            HeartLog.Log($"Registered class projectile definition {definition.Name} for ID {I.Definitions.Count - 1}.");
            return I.Definitions.Count - 1;
        }

        /// <summary>
        /// Registers a projectile definition.
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="syncToClients"></param>
        /// <returns></returns>
        public static int RegisterDefinition(byte[] serializedDefinition)
        {
            var definition = MyAPIGateway.Utilities.SerializeFromBinary<ProjectileDefinitionBase>(serializedDefinition);
            if (I.DefinitionNamePairs.ContainsKey(definition.Name))
            {
                HeartLog.Log($"Duplicate ammo definition {definition.Name}! Skipping...");
                return -1;
            }

            I.Definitions.Add(definition);
            I.SerializedDefinitions.Add(serializedDefinition);
            I.DefinitionNamePairs.Add(definition.Name, I.Definitions.Count - 1);
            if (MyAPIGateway.Session.IsServer)
                HeartData.I.Net.SendToEveryone(new n_ProjectileDefinitionIdSync(
                    I.Definitions.Count - 1,
                    definition.Name,
                    serializedDefinition
                    ));
            HeartLog.Log($"Registered binary projectile definition {definition.Name} for ID {I.Definitions.Count - 1}.");
            return I.Definitions.Count - 1;
        }

        public static bool ReplaceDefinition(int definitionId, byte[] serializedDefinition, bool syncToClients = false)
        {
            if (!HasDefinition(definitionId))
                return false;
            var definition = MyAPIGateway.Utilities.SerializeFromBinary<ProjectileDefinitionBase>(serializedDefinition);

            I.Definitions[definitionId] = definition;
            I.SerializedDefinitions[definitionId] = serializedDefinition;
            if (MyAPIGateway.Session.IsServer && syncToClients)
                HeartData.I.Net.SendToEveryone(new n_ProjectileDefinitionIdSync(
                    definitionId,
                    definition.Name,
                    serializedDefinition
                    ));

            HeartLog.Log($"Updated binary projectile definition {definition.Name} for ID {definitionId}");
            return true;
        }

        public static bool ReplaceDefinition(int definitionId, ProjectileDefinitionBase definition, bool syncToClients = false)
        {
            if (!HasDefinition(definitionId))
                return false;
            var serializedDefinition = MyAPIGateway.Utilities.SerializeToBinary(definition);

            I.Definitions[definitionId] = definition;
            I.SerializedDefinitions[definitionId] = serializedDefinition;
            if (MyAPIGateway.Session.IsServer && syncToClients)
                HeartData.I.Net.SendToEveryone(new n_ProjectileDefinitionIdSync(
                    definitionId,
                    definition.Name,
                    serializedDefinition
                    ));

            HeartLog.Log($"Updated class projectile definition {definition.Name} for ID {definitionId}");
            return true;
        }

        /// <summary>
        /// TODO: Does not properly remove ammos!
        /// </summary>
        /// <param name="definitionId"></param>
        public static void RemoveDefinition(int definitionId)
        {
            if (!HasDefinition(definitionId))
                return;

            var definition = I.Definitions[definitionId];
            I.DefinitionNamePairs.Remove(definition.Name);
            I.Definitions[definitionId] = null;
            I.SerializedDefinitions[definitionId] = null;

            HeartLog.Log($"Removed ammo definition " + definitionId);
        }

        public static void ClearDefinitions()
        {
            I.Definitions.Clear();
            I.DefinitionNamePairs.Clear();
            I.SerializedDefinitions.Clear();
            HeartLog.Log($"Cleared all ammo definitions.");
        }

        public static int DefinitionCount()
        {
            return I.Definitions.Count;
        }
    }
}
