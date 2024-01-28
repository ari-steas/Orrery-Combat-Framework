using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using System.Collections.Generic;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    internal class WeaponDefinitionManager
    {
        public static WeaponDefinitionManager I;

        private Dictionary<string, WeaponDefinitionBase> Definitions = new Dictionary<string, WeaponDefinitionBase>();

        public static WeaponDefinitionBase GetDefinition(string subTypeId)
        {
            //MyLog.Default.WriteLine(subTypeId + " | " + HasDefinition(subTypeId) + " | " + (I.Definitions[subTypeId] == null));
            if (HasDefinition(subTypeId))
                return I.Definitions[subTypeId];
            return null;
        }

        public static bool HasDefinition(string subTypeId)
        {
            return I.Definitions.ContainsKey(subTypeId);
        }

        public static void RegisterDefinition(WeaponDefinitionBase definition)
        {
            if (definition == null)
                return;

            if (I.Definitions.ContainsKey(definition.Assignments.BlockSubtype))
            {
                I.Definitions[definition.Assignments.BlockSubtype] = definition;
                HeartData.I.Log.Log($"Duplicate weapon definition {definition.Assignments.BlockSubtype}! Overriding...");
            }
            else
                I.Definitions.Add(definition.Assignments.BlockSubtype, definition);

            HeartData.I.OrreryBlockCategory.AddBlock(definition.Assignments.BlockSubtype);
            HeartData.I.Log.Log($"Registered weapon definition {definition.Assignments.BlockSubtype}.");
        }

        public static int DefinitionCount()
        {
            return I.Definitions.Count;
        }
    }
}
