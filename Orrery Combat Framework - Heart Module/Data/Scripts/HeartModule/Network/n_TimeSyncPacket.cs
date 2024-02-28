using ProtoBuf;
using Sandbox.ModAPI;
using System;

namespace Heart_Module.Data.Scripts.HeartModule.Network
{
    /// <summary>
    /// Packet used for syncing time betweeen client and server.
    /// </summary>
    [ProtoContract]
    internal class n_TimeSyncPacket : PacketBase
    {
        [ProtoMember(21)] public double OutgoingTimestamp;
        [ProtoMember(22)] public double IncomingTimestamp;

        public n_TimeSyncPacket() { }

        public override void Received(ulong SenderSteamId)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                HeartData.I.Net.SendToPlayer(new n_TimeSyncPacket()
                {
                    IncomingTimestamp = this.OutgoingTimestamp,
                    OutgoingTimestamp = DateTime.UtcNow.TimeOfDay.TotalMilliseconds
                }, SenderSteamId);
            }
            else
            {
                HeartData.I.Net.estimatedPing = DateTime.UtcNow.TimeOfDay.TotalMilliseconds - HeartData.I.Net.estimatedPing;
                HeartData.I.Net.ServerTimeOffset = OutgoingTimestamp - IncomingTimestamp - HeartData.I.Net.estimatedPing;
                //HeartLog.Log("Outgoing Timestamp: " + OutgoingTimestamp + "\nIncoming Timestamp: " + IncomingTimestamp);
                //HeartLog.Log("Total ping time (ms): " + HeartData.I.Net.estimatedPing);
            }
        }
    }
}
