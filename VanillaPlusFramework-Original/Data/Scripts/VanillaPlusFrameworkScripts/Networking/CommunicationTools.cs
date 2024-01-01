using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.EntityComponents;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage.Game.Models;
using VRage.Render.Particles;
using System.Linq.Expressions;
using System.IO;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Weapons;
using VRage;
using VRage.Collections;
using VRage.Voxels;
using ProtoBuf;
using System.Collections.Concurrent;
using VRage.Serialization;
using VanillaPlusFramework.TemplateClasses;
using VanillaPlusFramework.Missiles;
using VRage.Game.Components.Interfaces;

namespace VanillaPlusFramework.Networking
{
    public static class CommunicationTools
    {
        public static readonly ushort MessageHandlerId = 7170;
        public static List<IMyPlayer> Players = new List<IMyPlayer>();

        public static void Load()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(MessageHandlerId, MessageRecieved);
        }

        public static void Unload()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(MessageHandlerId, MessageRecieved);

            Players = null;
        }

        public static void SendMessageTo(Packet packet, ushort channel, ulong RecipientId, bool reliable = true)
        {
            byte[] SerializedMessage = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageTo(channel, SerializedMessage, RecipientId, reliable);
        }

        public static void SendMessageToClients(Packet packet, ushort channel, bool reliable = true, params ulong[] ignoreList)
        {
            byte[] SerializedMessage = MyAPIGateway.Utilities.SerializeToBinary(packet);

            lock (Players)
            {
                MyAPIGateway.Players.GetPlayers(Players);

                foreach (IMyPlayer player in Players)
                {
                    if (!ignoreList.Contains(player.SteamUserId))
                        MyAPIGateway.Multiplayer.SendMessageTo(channel, SerializedMessage, player.SteamUserId, reliable);
                }
            }
        }

        public static void SendMessageToServer(Packet packet, ushort channel, bool reliable = true)
        {
            byte[] SerializedMessage = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageToServer(channel, SerializedMessage, reliable);

        }

        public static void MessageRecieved(ushort ChannelId, byte[] bytes, ulong SenderId, bool fromServer)
        {
            Packet packet = null;
            try
            {
                packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(bytes);
            }  
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole(e.ToString());
            }
            

            if (packet == null)
            {
                MyLog.Default.WriteLineAndConsole($"[VANILLA+ FRAMEWORK ERROR] Recieved message failed to deserialize. HandlerId: {MessageHandlerId}.Sent from: {SenderId}.");
                return;
            }

            OnMessageReceived.Invoke(ChannelId, packet, SenderId, fromServer);
        }

        public static event Action<ushort, Packet, ulong, bool> OnMessageReceived;
    }
}
