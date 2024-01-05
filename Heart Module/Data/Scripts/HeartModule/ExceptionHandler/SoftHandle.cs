using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Scripting;
using VRage.Utils;

namespace Heart_Module.Data.Scripts.HeartModule.ErrorHandler
{
    public class SoftHandle
    {
        public static void RaiseException(string message, Type callingType = null, ulong callerId = ulong.MaxValue)
        {
            MyAPIGateway.Utilities.ShowNotification(message);
            HeartLog.LogException(new Exception(message), callingType ?? typeof(SoftHandle), callerId != ulong.MaxValue ? $"Shared exception from {callerId}: " : "");
        }

        public static void RaiseException(Exception exception, Type callingType = null, ulong callerId = ulong.MaxValue)
        {
            MyAPIGateway.Utilities.ShowNotification(exception.Message);
            HeartLog.LogException(exception, callingType ?? typeof(SoftHandle), callerId != ulong.MaxValue ? $"Shared exception from {callerId}: " : "");
        }

        public static void RaiseSyncException(string message)
        {
            RaiseException("Client is out of sync!\n" + message);
        }
    }
}
