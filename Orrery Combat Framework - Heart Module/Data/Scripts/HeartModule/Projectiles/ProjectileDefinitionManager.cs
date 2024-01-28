using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using System.Collections.Generic;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    internal class ProjectileDefinitionManager
    {
        public static ProjectileDefinitionManager I;
        private List<ProjectileDefinitionBase> Definitions = new List<ProjectileDefinitionBase>();
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
            return I.Definitions.Count > id;
        }

        public static void RegisterDefinition(ProjectileDefinitionBase definition)
        {
            if (I.DefinitionNamePairs.ContainsKey(definition.Name))
            {
                HeartData.I.Log.Log($"Duplicate ammo definition {definition.Name}! Skipping...");
                return;
            }

            I.Definitions.Add(definition);
            I.DefinitionNamePairs.Add(definition.Name, I.Definitions.Count - 1);
            HeartData.I.Log.Log($"Registered projectile definition {definition.Name}.");
        }

        public static int DefinitionCount()
        {
            return I.Definitions.Count;
        }
    }
}
