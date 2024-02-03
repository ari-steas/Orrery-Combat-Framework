using Heart_Module.Data.Scripts.HeartModule.Definitions;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.ModAPI;
using System.Collections.Generic;
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
        private Dictionary<string, int> DefinitionNamePairs = new Dictionary<string, int>();

        public static ProjectileDefinitionBase GetDefinition(int id)
        {
            if (HasDefinition(id))
                return I.Definitions[id];
            else
                return null;
        }

        public static ProjectileDefinitionBase GetDefinition(string name)
        {
            return GetDefinition(GetId(name));
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
            return I.Definitions.Count > id && id >= 0;
        }

        /// <summary>
        /// Use this when creating a definiton live.
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        public static int RegisterModApiDefinition(ProjectileDefinitionBase definition)
        {
            if (HasDefinition(definition.Name))
                throw new System.Exception("Attempted to assign ProjectileDefinition to existing ID!");
            return RegisterDefinition(definition, true);
        }

        /// <summary>
        /// Registers a projectile definition.
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="syncToClients"></param>
        /// <returns></returns>
        public static int RegisterDefinition(ProjectileDefinitionBase definition, bool syncToClients = false)
        {
            if (I.DefinitionNamePairs.ContainsKey(definition.Name))
            {
                HeartData.I.Log.Log($"Duplicate ammo definition {definition.Name}! Skipping...");
                return -1;
            }

            I.Definitions.Add(definition);
            I.DefinitionNamePairs.Add(definition.Name, I.Definitions.Count - 1);
            if (MyAPIGateway.Session.IsServer)
                HeartData.I.Net.SendToEveryone(new n_ProjectileDefinitionIdSync(
                    I.Definitions.Count - 1,
                    definition.Name,
                    syncToClients ? MyAPIGateway.Utilities.SerializeToBinary(definition) : null
                    ));
            HeartData.I.Log.Log($"Registered projectile definition {definition.Name} for ID {I.Definitions.Count - 1}.");
            return I.Definitions.Count - 1;
        }

        public static bool ReplaceDefinition(int definitionId, ProjectileDefinitionBase definition, bool syncToClients = false)
        {
            if (!HasDefinition(definitionId))
                return false;
            I.Definitions[definitionId] = definition;
            if (MyAPIGateway.Session.IsServer && syncToClients)
                HeartData.I.Net.SendToEveryone(new n_ProjectileDefinitionIdSync(
                    definitionId,
                    definition.Name,
                    MyAPIGateway.Utilities.SerializeToBinary(definition)
                    ));
            return true;
        }

        public static void RemoveDefinition(int definitionId)
        {
            if (!HasDefinition(definitionId))
                return;

            var definition = I.Definitions[definitionId];
            I.DefinitionNamePairs.Remove(definition.Name);
            I.Definitions.RemoveAt(definitionId);

            HeartData.I.Log.Log($"Removed ammo definition " + definitionId);
        }

        public static int DefinitionCount()
        {
            return I.Definitions.Count;
        }
    }
}
