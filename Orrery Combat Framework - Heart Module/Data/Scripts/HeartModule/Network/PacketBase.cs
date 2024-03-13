using Heart_Module.Data.Scripts.HeartModule.Definitions;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.ProjectileNetworking;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using ProtoBuf;
using System;
using YourName.ModName.Data.Scripts.HeartModule.Weapons;

namespace Heart_Module.Data.Scripts.HeartModule.Network
{
    [ProtoInclude(1, typeof(n_SerializableProjectile))]
    [ProtoInclude(2, typeof(n_SerializableError))]
    [ProtoInclude(3, typeof(n_ProjectileRequest))]
    [ProtoInclude(4, typeof(n_ProjectileArray))]
    [ProtoInclude(5, typeof(n_TurretFacing))]
    [ProtoInclude(6, typeof(n_TurretFacingArray))]
    [ProtoInclude(7, typeof(n_ProjectileDefinitionIdSync))]
    [ProtoInclude(8, typeof(n_SerializableProjectileInfos))]
    [ProtoInclude(9, typeof(n_SerializableFireEvents))]
    [ProtoInclude(10, typeof(n_TimeSyncPacket))]
    [ProtoInclude(11, typeof(Heart_Settings))]
    [ProtoInclude(12, typeof(n_TargetingUpdate))]
    [ProtoInclude(13, typeof(n_MagazineUpdate))]
    [ProtoContract(UseProtoMembersOnly = true)]
    public abstract partial class PacketBase
    {
        /// <summary>
        /// Called whenever your packet is recieved.
        /// </summary>
        /// <param name="SenderSteamId"></param>
        public abstract void Received(ulong SenderSteamId);

        public static Type[] Types = new Type[]
        {
            typeof(PacketBase),
            typeof(n_SerializableProjectile),
            typeof(n_SerializableError),
            typeof(n_ProjectileRequest),
            typeof(n_ProjectileArray),
            typeof(n_TurretFacing),
            typeof(n_TurretFacingArray),
            typeof(n_ProjectileDefinitionIdSync),
            typeof(n_SerializableProjectileInfos),
            typeof(n_SerializableFireEvents),
            typeof(n_TimeSyncPacket),
            typeof(Heart_Settings),
            typeof(n_TargetingUpdate),
            typeof(n_MagazineUpdate),
        };
    }
}
