using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Network;
using ProtoBuf;
using System;

namespace Heart_Module.Data.Scripts.HeartModule.ExceptionHandler
{
    [ProtoContract]
    public class n_SerializableError : PacketBase
    {
        [ProtoMember(21)] public string ExceptionMessage;
        [ProtoMember(22)] public string ExceptionStackTrace;
        [ProtoMember(23)] public bool IsCritical;

        public n_SerializableError() { }
        public n_SerializableError(Exception e, bool IsCritical)
        {
            ExceptionMessage = e.Message;
            ExceptionStackTrace = e.StackTrace;
            this.IsCritical = IsCritical;
        }

        public override void Received(ulong SenderSteamId)
        {
            if (IsCritical)
                CriticalHandle.ThrowCriticalException(this, typeof(n_SerializableError), SenderSteamId);
            else
                SoftHandle.RaiseException(this, callerId: SenderSteamId);
        }
    }
}
