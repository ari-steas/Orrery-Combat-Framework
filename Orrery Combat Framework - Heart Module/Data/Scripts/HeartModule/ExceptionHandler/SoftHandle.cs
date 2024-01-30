using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Sandbox.ModAPI;
using System;

namespace Heart_Module.Data.Scripts.HeartModule.ErrorHandler
{
    public class SoftHandle
    {
        public static void RaiseException(string message, Exception ex = null, Type callingType = null, ulong callerId = ulong.MaxValue)
        {
            MyAPIGateway.Utilities.ShowNotification("Minor Exception: " + message);
            Exception soft = new Exception(message, ex);
            HeartData.I.Log.LogException(soft, callingType ?? typeof(SoftHandle), callerId != ulong.MaxValue ? $"Shared exception from {callerId}: " : "");
            if (MyAPIGateway.Session.IsServer)
                HeartData.I.Net.SendToEveryone(new n_SerializableError(soft, false));
        }

        public static void RaiseException(Exception exception, Type callingType = null, ulong callerId = ulong.MaxValue)
        {
            if (exception == null)
                return;

            MyAPIGateway.Utilities.ShowNotification("Minor Exception: " + exception.Message);
            HeartData.I.Log.LogException(exception, callingType ?? typeof(SoftHandle), callerId != ulong.MaxValue ? $"Shared exception from {callerId}: " : "");
        }

        public static void RaiseException(n_SerializableError exception, Type callingType = null, ulong callerId = ulong.MaxValue)
        {
            if (exception == null)
                return;

            MyAPIGateway.Utilities.ShowNotification("Minor Exception: " + exception.ExceptionMessage);
            HeartData.I.Log.LogException(exception, callingType ?? typeof(SoftHandle), callerId != ulong.MaxValue ? $"Shared exception from {callerId}: " : "");
        }

        public static void RaiseSyncException(string message)
        {
            RaiseException("Client is out of sync!\n" + message);
        }
    }
}
