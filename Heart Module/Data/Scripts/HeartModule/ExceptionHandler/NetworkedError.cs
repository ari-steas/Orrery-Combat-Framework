using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Network;
using ProtoBuf;
using System;
using VRage.Utils;

namespace Heart_Module.Data.Scripts.HeartModule.ExceptionHandler
{
    public class NetworkedError : PacketBase
    {
        [ProtoMember(21)] Exception Exception;
        [ProtoMember(22)] bool IsCritical;

        public NetworkedError() { }
        public NetworkedError(Exception e, bool IsCritical)
        {
            Exception = e;
            this.IsCritical = IsCritical;
        }

        public override void Received(ulong SenderSteamId)
        {
            if (IsCritical)
                CriticalHandle.ThrowCriticalException(Exception, typeof(NetworkedError), SenderSteamId);
            else
                SoftHandle.RaiseException(Exception, callerId: SenderSteamId);
        }
    }
}
