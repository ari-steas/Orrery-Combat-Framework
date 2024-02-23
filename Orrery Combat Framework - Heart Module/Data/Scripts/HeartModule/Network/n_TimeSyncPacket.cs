using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public n_TimeSyncPacket() {}

        public override void Received(ulong SenderSteamId)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                HeartData.I.Net.SendToPlayer(new n_TimeSyncPacket()
                {
                    IncomingTimestamp = this.OutgoingTimestamp,
                    OutgoingTimestamp = DateTime.UtcNow.Date.TimeOfDay.TotalMilliseconds
                }, SenderSteamId);
            }
            else
            {
                HeartLog.Log("Outgoing Timestamp: " + OutgoingTimestamp + "\nIncoming Timestamp: " + IncomingTimestamp);
                HeartData.I.Net.estimatedPing = DateTime.UtcNow.Date.TimeOfDay.TotalMilliseconds - HeartData.I.Net.estimatedPing;
            }
        }
    }
}
