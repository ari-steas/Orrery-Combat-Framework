using Heart_Module.Data.Scripts.HeartModule.Definitions.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;

namespace Heart_Module.Data.Scripts.HeartModule.Definitions
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, Priority = int.MinValue)]
    public class DefinitionReciever : MySessionComponentBase
    {
        const int DefinitionMessageId = 8643;

        public override void LoadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            MyAPIGateway.Utilities.RegisterMessageHandler(DefinitionMessageId, RecieveDefinitions);
        }

        private void RecieveDefinitions(object o)
        {
            byte[] message = o as byte[];
            if (message == null)
                return;

            try
            {
                DefinitionContainer definitionContainer = MyAPIGateway.Utilities.SerializeFromBinary<DefinitionContainer>(message);
                if (definitionContainer == null)
                    return;

                MyAPIGateway.Utilities.ShowMessage("Orrery", $"Loaded {definitionContainer.WeaponDefs.Length + definitionContainer.AmmoDefs.Length} definitions.");
            }
            catch (Exception e)
            {
                SoftHandle.RaiseException(e, typeof(DefinitionReciever));
            }
        }
    }
}
