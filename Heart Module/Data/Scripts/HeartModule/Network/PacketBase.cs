using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using ProtoBuf;

namespace Heart_Module.Data.Scripts.HeartModule.Network
{
    [ProtoInclude(1, typeof(n_SerializableProjectile))]
    [ProtoInclude(2, typeof(ExceptionHandler.n_SerializableError))]
    [ProtoInclude(3, typeof(n_ProjectileRequest))]
    [ProtoContract(UseProtoMembersOnly = true)]
    public abstract partial class PacketBase
    {
        public abstract void Received(ulong SenderSteamId);
    }
}
