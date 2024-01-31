using Heart_Module.Data.Scripts.HeartModule.Definitions;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.ModAPI;
using System.Collections.Generic;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    internal class ProjectileDefinitionManager
    {
        public static ProjectileDefinitionManager I;
        private List<ProjectileDefinitionBase> Definitions = new List<ProjectileDefinitionBase>();
        private Dictionary<string, int> DefinitionNamePairs = new Dictionary<string, int>();

        /// <summary>
        /// Changes the ID of a projectile definition. If the ID is already occupied, swaps the two definitions. DO NOT CALL ON SERVER!
        /// </summary>
        /// <param name="name"></param>
        /// <param name="newId"></param>
        public static void ReorderDefinitions(string name, int newId)
        {
            //if (!HasDefinition(name)) return;
            //int oldId = GetId(name);
            //if (oldId == newId) return;
            //ProjectileDefinitionBase bufferDefinition = GetDefinition(name);
            //while (!HasDefinition(newId))
            //    I.Definitions.Add(null);
            //I.Definitions[oldId] = GetDefinition(newId);
            //I.DefinitionNamePairs[I.Definitions[oldId].Name] = newId;
            //I.Definitions[newId] = bufferDefinition;
            //I.DefinitionNamePairs[name] = newId;
        }

        public static ProjectileDefinitionBase GetDefinition(int id)
        {
            foreach (var def in I.Definitions)
                HeartData.I.Log.Log("Def:  " + def.Name);
            foreach (var def in I.DefinitionNamePairs)
                HeartData.I.Log.Log("Pair: " + def.Key);
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
            return I.Definitions.Count > id && id > 0;
        }

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
            HeartData.I.Log.Log($"Registered projectile definition {definition.Name}.");
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

        public static int DefinitionCount()
        {
            return I.Definitions.Count;
        }
    }
}
