using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using ProtoBuf;

namespace Heart_Module.Data.Scripts.HeartModule.Network
{
    [ProtoInclude(1, typeof(SerializableProjectile))]
    [ProtoInclude(2, typeof(ExceptionHandler.NetworkedError))]
    [ProtoContract(UseProtoMembersOnly = true)]
    public abstract partial class PacketBase
    {
        public abstract void Received(ulong SenderSteamId);
    }
}
