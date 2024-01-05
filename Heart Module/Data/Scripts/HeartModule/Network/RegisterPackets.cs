using Digi.Examples.NetworkProtobuf;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using ProtoBuf;

namespace Digi.NetworkLib
{
    [ProtoInclude(10, typeof(PacketBlockSettings))]
    [ProtoInclude(11, typeof(SerializableProjectile))]
    //[ProtoInclude(11, typeof(SomeOtherPacketClass))]
    //[ProtoInclude(12, typeof(Etc...))]
    public abstract partial class PacketBase
    {
    }
}