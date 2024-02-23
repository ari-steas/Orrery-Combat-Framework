using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace Heart_Module.Data.Scripts.HeartModule.Network
{
    public class HeartNetwork
    {
        public int NetworkLoadTicks = 240;
        public int NetworkLoad { get; private set; } = 0; // TODO: Per-packet type network load

        private List<int> networkLoadArray = new List<int>();
        private int networkLoadUpdate = 0;

        public double ServerTimeOffset { get; internal set; } = 0;
        internal double estimatedPing = 0;

        public void LoadData()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(HeartData.HeartNetworkId, ReceivedPacket);

            UpdateTimeOffset();
        }

        private void UpdateTimeOffset()
        {
            estimatedPing = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;
            if (!MyAPIGateway.Session.IsServer)
                SendToServer(new n_TimeSyncPacket() { OutgoingTimestamp = estimatedPing });
        }

        public void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(HeartData.HeartNetworkId, ReceivedPacket);
        }

        int tickCounter = 0;
        public void Update()
        {
            networkLoadUpdate--;
            if (networkLoadUpdate <= 0 && networkLoadArray.Count > 0) // Update NetworkLoad average once per second
            {
                networkLoadUpdate = NetworkLoadTicks;
                NetworkLoad = 0;
                foreach (int i in networkLoadArray)
                    NetworkLoad += i;
                NetworkLoad /= (NetworkLoadTicks / 60); // Average per-second
                networkLoadArray.Clear();
            }

            if (tickCounter % 307 == 0)
                UpdateTimeOffset();
            tickCounter++;
        }

        void ReceivedPacket(ushort channelId, byte[] serialized, ulong senderSteamId, bool isSenderServer)
        {
            networkLoadArray.Add(serialized.Length);
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

        void HandlePacket(PacketBase packet, ulong senderSteamId)
        {
            packet.Received(senderSteamId);
        }

        public void SendToPlayer(PacketBase packet, ulong playerSteamId, byte[] serialized = null)
        {
            RelayToClient(packet, playerSteamId, HeartData.I.SteamId, serialized);
        }

        public void SendToEveryone(PacketBase packet, byte[] serialized = null)
        {
            RelayToClients(packet, HeartData.I.SteamId, serialized);
        }

        public void SendToServer(PacketBase packet, byte[] serialized = null)
        {
            RelayToServer(packet, HeartData.I.SteamId, serialized);
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

        void RelayToClient(PacketBase packet, ulong playerSteamId, ulong senderSteamId, byte[] serialized = null)
        {
            if (playerSteamId == MyAPIGateway.Multiplayer.ServerId || playerSteamId == senderSteamId)
                return;

            if (serialized == null) // only serialize if necessary, and only once.
                serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageTo(HeartData.HeartNetworkId, serialized, playerSteamId);
        }

        void RelayToServer(PacketBase packet, ulong senderSteamId = 0, byte[] serialized = null)
        {
            if (senderSteamId == MyAPIGateway.Multiplayer.ServerId)
                return;

            if (serialized == null) // only serialize if necessary, and only once.
                serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageToServer(HeartData.HeartNetworkId, serialized);
        }
    }
}
