using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using Whiplash.WeaponFramework;

namespace Rexxar.Communication
{
    public static class Communication
    {
        private static List<IMyPlayer> _playerCache = new List<IMyPlayer>();

        public static void Register()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(FrameworkConstants.NETID_RECHARGE_SYNC, MessageHandler);
        }

        public static void Unregister()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(FrameworkConstants.NETID_RECHARGE_SYNC, MessageHandler);
            _playerCache = null;
        }

        private static void MessageHandler(byte[] bytes)
        {
            Message m = MyAPIGateway.Utilities.SerializeFromBinary<Message>(bytes);

            if (WeaponSession.IsServer)
                m.HandleServer();
            else
                m.HandleClient();
        }

        public static void SendMessageTo(ulong steamId, Message message, bool reliable = true)
        {
            var d = MyAPIGateway.Utilities.SerializeToBinary(message);
            if (!reliable && d.Length >= 1000)
                throw new Exception($"Attempting to send unreliable message beyond message size limits! Message type: {message.GetType()} Content: {string.Join(" ", d)}");
            MyAPIGateway.Multiplayer.SendMessageTo(FrameworkConstants.NETID_RECHARGE_SYNC, d, steamId, reliable);
        }

        public static void SendMessageToServer(Message message, bool reliable = true)
        {
            var d = MyAPIGateway.Utilities.SerializeToBinary(message);
            if (!reliable && d.Length >= 1000)
                throw new Exception($"Attempting to send unreliable message beyond message size limits! Message type: {message.GetType()} Content: {string.Join(" ", d)}");
            MyAPIGateway.Multiplayer.SendMessageToServer(FrameworkConstants.NETID_RECHARGE_SYNC, d, reliable);
        }

        public static void SendMessageToClients(Message message, bool reliable = true, params ulong[] ignore)
        {
            var d = MyAPIGateway.Utilities.SerializeToBinary(message);
            if (!reliable && d.Length >= 1000)
                throw new Exception($"Attempting to send unreliable message beyond message size limits! Message type: {message.GetType()} Content: {string.Join(" ", d)}");

            lock (_playerCache)
            {
                MyAPIGateway.Players.GetPlayers(_playerCache);
                foreach (var player in _playerCache)
                {
                    var steamId = player.SteamUserId;
                    if (ignore?.Contains(steamId) == true)
                        continue;
                    MyAPIGateway.Multiplayer.SendMessageTo(FrameworkConstants.NETID_RECHARGE_SYNC, d, steamId, reliable);
                }
                _playerCache.Clear();
            }
        }
    }

    public static class Extensions
    {
        public static bool Contains<T>(this IEnumerable<T> input, T query)
        {
            return Enumerable.Contains(input, query);
        }
    }
}
