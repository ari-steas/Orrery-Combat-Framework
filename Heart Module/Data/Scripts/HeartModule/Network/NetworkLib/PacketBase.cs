using Digi.Examples.NetworkProtobuf;
using ProtoBuf;
using Sandbox.ModAPI;
using YourName.ModName.Data.Scripts.HeartModule.Utility;

namespace Digi.NetworkLib
{
    [ProtoInclude(2, typeof(PacketBlockSettings))]
    [ProtoContract(UseProtoMembersOnly = true)]
    public abstract partial class PacketBase
    {
        /// <summary>
        /// Automatically assigned to original sender's SteamId, validated when it reaches server.
        /// </summary>
        [ProtoMember(1)]
        public ulong OriginalSenderSteamId;

        //protected Network Network => Heart_Utility.Network.Instance;


        public PacketBase()
        {
            if(MyAPIGateway.Multiplayer == null)
                Network.CrashAfterLoad($"Cannot instantiate packets in fields ({GetType().Name}), too early! Do it in one of the methods where MyAPIGateway.Multiplayer is not null.");
            else
                OriginalSenderSteamId = MyAPIGateway.Multiplayer.MyId;
        }

        /// <summary>
        /// Called when this packet is received on this machine.<br />
        /// <paramref name="packetInfo"/> can be modified serverside to setup automatic relay.
        /// </summary>
        public abstract void Received(ref PacketInfo packetInfo, ulong senderSteamId);
    }
}