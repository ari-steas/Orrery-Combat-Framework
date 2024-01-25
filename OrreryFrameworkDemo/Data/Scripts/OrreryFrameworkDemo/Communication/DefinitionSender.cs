using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Utils;

namespace OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, Priority = int.MaxValue)]
    internal class DefinitionSender : MySessionComponentBase
    {
        const int DefinitionMessageId = 8643;

        byte[] SerializedStorage = new byte[0];
        DefinitionContainer storedDef = null;

        public override void LoadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            MyAPIGateway.Utilities.RegisterMessageHandler(DefinitionMessageId, InputHandler);

            // Init
            storedDef = HeartDefinitions.GetBaseDefinitions();
            SerializedStorage = MyAPIGateway.Utilities.SerializeToBinary(storedDef);

            MyLog.Default.WriteLineAndConsole($"OrreryDefinition [{ModContext.ModName}]: Packaged definitions & preparing to send.");
            InputHandler(true); // In case this is delayed, send to heart mod.
        }

        private void InputHandler(object o)
        {
            if (o is bool && (bool)o)
            {
                MyAPIGateway.Utilities.SendModMessage(DefinitionMessageId, SerializedStorage);
                MyLog.Default.WriteLineAndConsole($"OrreryDefinition [{ModContext.ModName}]: Sent definitions & returning to sleep.");
            }
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(DefinitionMessageId, InputHandler);
        }
    }
}
