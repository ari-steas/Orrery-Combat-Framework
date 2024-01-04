using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;

namespace Heart_Module.Data.Scripts.HeartModule.ErrorHandler
{
    public class SoftHandle
    {
        public static void RaiseException(string message)
        {
            MyAPIGateway.Utilities.ShowNotification(message);
            MyLog.Default.WriteLineAndConsole(message);
        }

        public static void RaiseSyncException(string message)
        {
            RaiseException("Client is out of sync!\n"+message);
        }
    }
}
