using Sandbox.Game.GUI.DebugInputComponents;
using Sandbox.ModAPI;
using System.Security.Cryptography;
using VRage.Game.Components;
using VRage.Utils;

namespace OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, Priority = int.MaxValue)]
    internal class DefinitionSender : MySessionComponentBase
    {
        const int DefinitionMessageId = 8643;

        byte[] SerializedStorage = null;
        DefinitionContainer storedDef = null;
        bool AwaitingSend = true;

        public override void LoadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            HeartApi.LoadData();
            MyAPIGateway.Utilities.RegisterMessageHandler(DefinitionMessageId, InputHandler);
        }

        private void InputHandler(object o)
        {
            if (o is bool && (bool)o)
            {
                AwaitingSend = true;
            }
        }

        public override void UpdateBeforeSimulation()
        {
            if (AwaitingSend)
            {
                if (SerializedStorage == null)
                {
                    if (HeartApi.HasInited)
                    {
                        // Init
                        storedDef = HeartDefinitions.GetBaseDefinitions();
                        SerializedStorage = MyAPIGateway.Utilities.SerializeToBinary(storedDef);
                        MyLog.Default.WriteLineAndConsole($"OrreryDefinition [{ModContext.ModName}]: Packaged definitions & preparing to send.");
                    }
                }
                else
                {
                    MyAPIGateway.Utilities.SendModMessage(DefinitionMessageId, SerializedStorage);
                    foreach (var def in storedDef.AmmoDefs)
                        def.LiveMethods.RegisterMethods(def.Name);
                    AwaitingSend = false;
                    MyLog.Default.WriteLineAndConsole($"OrreryDefinition [{ModContext.ModName}]: Sent definitions & returning to sleep.");
                }
            }
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(DefinitionMessageId, InputHandler);
        }
    }
}
