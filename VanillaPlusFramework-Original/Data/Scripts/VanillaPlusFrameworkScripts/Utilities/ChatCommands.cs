using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VanillaPlusFramework.Networking;

namespace VanillaPlusFramework.Utilities
{

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class VPFChatCommands : MySessionComponentBase
    {
        private static Dictionary<string, Action<ulong, string[]>> ChatCommands = new Dictionary<string, Action<ulong, string[]>>();

        public static void Debug(object s)
        {
            ShowMessage(s.ToString(), 0, false);
        }

        public void OnChatMessageRecieved(ulong sender, string messageText, ref bool sendToOthers)
        {
            string[] messageTextSplit = messageText.Split(' ');

            Action<ulong, string[]> Command;
            
            if (ChatCommands.TryGetValue(messageTextSplit[0], out Command))
            {
                Command.Invoke(sender, messageTextSplit);

                return;
                if (!MyAPIGateway.Multiplayer.IsServer)
                    CommunicationTools.SendMessageToServer(new ChatCommand(sender, messageTextSplit), CommunicationTools.MessageHandlerId);

                sendToOthers = false;
            }
        }

        public static void AddChatCommand(string CommandText, Action<ulong, string[]> Command)
        {
            if (!ChatCommands.ContainsKey(CommandText))
                ChatCommands.Add(CommandText, Command);
            else
            {
                MyLog.Default.Warning("Chat command already exists.");
            }
        }

        private void OnNetworkMessageRecieved(ushort ChannelId, Packet Packet, ulong SenderId, bool FromServer)
        {
            return;
            if (Packet.DataType == PacketType.Command)
            {
                ChatCommand CommandPacket = Packet as ChatCommand;

                Action<ulong, string[]> Command;

                if (ChatCommands.TryGetValue(CommandPacket.message[0], out Command))
                {
                    Command.Invoke(CommandPacket.SenderId, CommandPacket.message);
                }

                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    CommunicationTools.SendMessageToClients(Packet, CommunicationTools.MessageHandlerId, true, SenderId);
                }
            }
        }

        public void ChatCommand_GetAllCommands(ulong SenderId, string[] message)
        {
            foreach (KeyValuePair<string, Action<ulong, string[]>> Command in ChatCommands)
            {
                ShowMessage(Command.Key.ToString(), SenderId, true);
            }
        }

        public override void BeforeStart()
        {
            MyAPIUtilities.Static.MessageEnteredSender += OnChatMessageRecieved;
            CommunicationTools.OnMessageReceived += OnNetworkMessageRecieved;
            AddChatCommand("/ShowAllCommands", ChatCommand_GetAllCommands);
        }

        protected override void UnloadData()
        {
            MyAPIUtilities.Static.MessageEnteredSender -= OnChatMessageRecieved;
            CommunicationTools.OnMessageReceived -= OnNetworkMessageRecieved;
            ChatCommands.Clear();
        }

        public static void ShowMessage(string message, ulong SenderId, bool Local)
        {
            if (!Local || SenderId == MyAPIGateway.Multiplayer.MyId)
                MyAPIGateway.Utilities.ShowMessage("Vanilla+ Framework API", message);
        }


        public static bool IsOwner(ulong PlayerId)
        {
            return MyAPIGateway.Session.GetUserPromoteLevel(PlayerId) >= MyPromoteLevel.Owner;
        }
        public static bool IsAdmin(ulong PlayerId)
        {
            return MyAPIGateway.Session.GetUserPromoteLevel(PlayerId) >= MyPromoteLevel.Admin;
        }

        public static bool IsSpaceMaster(ulong PlayerId)
        {
            return MyAPIGateway.Session.GetUserPromoteLevel(PlayerId) >= MyPromoteLevel.SpaceMaster;
        }

        public static bool IsModerator(ulong PlayerId)
        {
            return MyAPIGateway.Session.GetUserPromoteLevel(PlayerId) >= MyPromoteLevel.Moderator;
        }

        public static bool IsOnlyPlaer(ulong PlayerId)
        {
            return MyAPIGateway.Session.GetUserPromoteLevel(PlayerId) == MyPromoteLevel.None;
        }
    }
}
