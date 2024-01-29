using Sandbox.ModAPI;
using System.Collections.Generic;
using System;
using VRage.Game.Components;
using VRage.Utils;

namespace Heart_Module.Data.Scripts.HeartModule.Definitions.ApiHandler
{
    internal class ApiSender
    {
        const long HeartApiChannel = 8644; // https://xkcd.com/221/

        Dictionary<string, Delegate> methods = new HeartApiMethods().ModApiMethods;

        public void LoadData()
        {
            MyAPIGateway.Utilities.SendModMessage(HeartApiChannel, methods); // Update mods that loaded before this one
            MyAPIGateway.Utilities.RegisterMessageHandler(HeartApiChannel, RecieveApiMethods);
            MyLog.Default.WriteLineAndConsole("Orrery Combat Framework: HeartAPISender ready.");
        }

        public void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(HeartApiChannel, RecieveApiMethods);
        }

        /// <summary>
        /// Listens for an API request.
        /// </summary>
        /// <param name="data"></param>
        public void RecieveApiMethods(object data)
        {
            if (data == null)
                return;

            if (data is bool && (bool) data)
            {
                MyAPIGateway.Utilities.SendModMessage(HeartApiChannel, methods);
                MyLog.Default.WriteLineAndConsole("Orrery Combat Framework: HeartAPISender send methods.");
            }
        }
    }
}
