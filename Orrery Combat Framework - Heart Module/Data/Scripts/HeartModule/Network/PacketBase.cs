using Heart_Module.Data.Scripts.HeartModule.Definitions;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.ProjectileNetworking;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using ProtoBuf;

namespace Heart_Module.Data.Scripts.HeartModule.Network
{
    [ProtoInclude(1, typeof(n_SerializableProjectile))]
    [ProtoInclude(2, typeof(ExceptionHandler.n_SerializableError))]
    [ProtoInclude(3, typeof(n_ProjectileRequest))]
    [ProtoInclude(4, typeof(n_ProjectileArray))]
    [ProtoInclude(5, typeof(n_TurretFacing))]
    [ProtoInclude(6, typeof(n_TurretFacingArray))]
    [ProtoInclude(7, typeof(n_ProjectileDefinitionIdSync))]
    [ProtoInclude(8, typeof(n_SerializableProjectileInfos))]
    [ProtoInclude(9, typeof(n_SerializableFireEvents))]
    [ProtoInclude(10, typeof(n_TimeSyncPacket))]
    [ProtoContract(UseProtoMembersOnly = true)]
    public abstract partial class PacketBase
    {
        public abstract void Received(ulong SenderSteamId);
    }
}
