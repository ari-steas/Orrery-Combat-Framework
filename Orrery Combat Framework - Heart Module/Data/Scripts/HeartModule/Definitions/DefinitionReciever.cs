using Heart_Module.Data.Scripts.HeartModule.Definitions.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;

namespace Heart_Module.Data.Scripts.HeartModule.Definitions
{
    public class DefinitionReciever
    {
        const int DefinitionMessageId = 8643; // https://xkcd.com/221/

        public void LoadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            MyAPIGateway.Utilities.RegisterMessageHandler(DefinitionMessageId, RecieveDefinitions);
            MyAPIGateway.Utilities.SendModMessage(DefinitionMessageId, true); // Notify client mods that this is ready
        }

        private void RecieveDefinitions(object o)
        {
            if (!(o is byte[]))
                return;

            byte[] message = o as byte[];
            if (message == null)
                return;

            try
            {
                DefinitionContainer definitionContainer = MyAPIGateway.Utilities.SerializeFromBinary<DefinitionContainer>(message);
                if (definitionContainer == null)
                    return;

                if (definitionContainer.WeaponDefs == null || definitionContainer.AmmoDefs == null)
                {
                    SoftHandle.RaiseException($"Error in recieved definition! WeaponDefsIsNull: {definitionContainer.WeaponDefs == null} AmmoDefsIsNull: {definitionContainer.AmmoDefs == null}", callingType: typeof(DefinitionReciever));
                    return;
                }

                foreach (var wepDef in definitionContainer.WeaponDefs)
                    WeaponDefinitionManager.RegisterDefinition(wepDef);
                foreach (var projDef in definitionContainer.AmmoDefs)
                    ProjectileDefinitionManager.RegisterDefinition(projDef);

                MyAPIGateway.Utilities.ShowMessage("[OCF]", $"Loaded {definitionContainer.WeaponDefs.Length + definitionContainer.AmmoDefs.Length} definitions.");
            }
            catch (Exception e)
            {
                SoftHandle.RaiseException(e, typeof(DefinitionReciever));
            }
        }

        public void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(DefinitionMessageId, RecieveDefinitions);
        }
    }
}
