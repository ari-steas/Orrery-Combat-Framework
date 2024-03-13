using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using VRage.Game.ModAPI;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Network
{
    public class HeartNetwork
    {
        public int NetworkLoadTicks = 240;
        public int TotalNetworkLoad { get; private set; } = 0;
        public Dictionary<Type, int> TypeNetworkLoad = new Dictionary<Type, int>();

        private int networkLoadUpdate = 0;

        public double ServerTimeOffset { get; internal set; } = 0;
        internal double estimatedPing = 0;

        public void LoadData()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(HeartData.HeartNetworkId, ReceivedPacket);

            foreach (var type in PacketBase.Types)
            {
                TypeNetworkLoad.Add(type, 0);
            }

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
            if (networkLoadUpdate <= 0)
            {
                networkLoadUpdate = NetworkLoadTicks;
                TotalNetworkLoad = 0;
                foreach (var networkLoadArray in TypeNetworkLoad.Keys.ToArray())
                {
                    TotalNetworkLoad += TypeNetworkLoad[networkLoadArray];
                    TypeNetworkLoad[networkLoadArray] = 0;
                }

                TotalNetworkLoad /= (NetworkLoadTicks / 60); // Average per-second
            }

            if (tickCounter % 307 == 0)
                UpdateTimeOffset();
            tickCounter++;
        }

        void ReceivedPacket(ushort channelId, byte[] serialized, ulong senderSteamId, bool isSenderServer)
        {
            try
            {
                PacketBase packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(serialized);
                TypeNetworkLoad[packet.GetType()] += serialized.Length;
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





        public KeyValuePair<Type, int> HighestNetworkLoad()
        {
            Type highest = null;

            foreach (var networkLoadArray in TypeNetworkLoad)
            {
                if (highest == null || networkLoadArray.Value > TypeNetworkLoad[highest])
                {
                    highest = networkLoadArray.Key;
                }
            }

            return new KeyValuePair<Type, int>(highest, TypeNetworkLoad[highest]);
        }

        public void SendToPlayer(PacketBase packet, ulong playerSteamId, byte[] serialized = null)
        {
            RelayToClient(packet, playerSteamId, HeartData.I.SteamId, serialized);
        }

        public void SendToEveryone(PacketBase packet, byte[] serialized = null)
        {
            RelayToClients(packet, HeartData.I.SteamId, serialized);
        }

        public void SendToEveryoneInSync(PacketBase packet, Vector3D position, byte[] serialized = null)
        {
            List<ulong> toSend = new List<ulong>();
            foreach (var player in HeartData.I.Players)
                if (Vector3D.DistanceSquared(player.GetPosition(), position) <= HeartData.I.SyncRangeSq)
                    toSend.Add(player.SteamUserId);

            if (toSend.Count == 0)
                return;

            if (serialized == null)
                serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

            foreach (var clientSteamId in toSend)
                RelayToClient(packet, clientSteamId, HeartData.I.SteamId, serialized);
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
