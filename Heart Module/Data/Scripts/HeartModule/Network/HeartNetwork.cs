using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Heart_Module.Data.Scripts.HeartModule.Network
{
    public class HeartNetwork
    {
        public void LoadData()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(HeartData.HeartNetworkId, ReceivedPacket);
        }

        public void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(HeartData.HeartNetworkId, ReceivedPacket);
        }

        void ReceivedPacket(ushort channelId, byte[] serialized, ulong senderSteamId, bool isSenderServer)
        {
            try
            {
                PacketBase packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(serialized);
                HandlePacket(packet, senderSteamId);
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(HeartNetwork));
            }
        }

        public void SendToEveryone(PacketBase packet, byte[] serialized = null)
        {
            RelayToClients(packet, 0, serialized);
        }

        List<IMyPlayer> TempPlayers = new List<IMyPlayer>();
        void RelayToClients(PacketBase packet, ulong senderSteamId = 0, byte[] serialized = null)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            TempPlayers.Clear();
            MyAPIGateway.Players.GetPlayers(TempPlayers);

            foreach (IMyPlayer p in TempPlayers)
            {
                // skip sending to self (server player) or back to sender
                if (p.SteamUserId == MyAPIGateway.Multiplayer.ServerId || p.SteamUserId == senderSteamId)
                    continue;

                if (serialized == null) // only serialize if necessary, and only once.
                    serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

                MyAPIGateway.Multiplayer.SendMessageTo(HeartData.HeartNetworkId, serialized, p.SteamUserId);
            }

            TempPlayers.Clear();
        }

        void HandlePacket(PacketBase packet, ulong senderSteamId)
        {
            packet.Received(senderSteamId);
        }
    }
}
