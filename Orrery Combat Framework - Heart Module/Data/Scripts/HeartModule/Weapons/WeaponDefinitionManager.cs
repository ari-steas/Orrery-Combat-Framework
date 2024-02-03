using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.Game.GUI.DebugInputComponents;
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

        public static bool UpdateDefinition(WeaponDefinitionBase definition)
        {
            if (!HasDefinition(definition.Assignments.BlockSubtype))
                return false;

            I.Definitions[definition.Assignments.BlockSubtype] = definition;
            return true;
        }

        public static bool RegisterModApiDefinition(WeaponDefinitionBase definition)
        {
            if (HasDefinition(definition.Assignments.BlockSubtype))
            {
                SoftHandle.RaiseException("Attempted to assign WeaponDefinition to existing ID!", callingType: typeof(WeaponDefinitionManager));
                return false;
            }
            RegisterDefinition(definition);
            return true;
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

            if (HeartData.I.IsLoaded)
                WeaponManager.I.UpdateLogicOnExistingBlocks(definition);
        }

        public static void RemoveDefinition(string subtype)
        {
            if (!HasDefinition(subtype))
                return;

            WeaponDefinitionBase definition = I.Definitions[subtype];
            WeaponManager.I.RemoveLogicOnExistingBlocks(definition);
            I.Definitions.Remove(subtype);

            HeartData.I.Log.Log("Removed weapon definition " + subtype);
        }

        public static int DefinitionCount()
        {
            return I.Definitions.Count;
        }

        public static string[] GetAllDefinitions()
        {
            return I.Definitions.Keys.ToArray();
        }
    }
}
