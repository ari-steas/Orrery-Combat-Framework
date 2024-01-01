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
using System.Net.Sockets;


namespace VanillaPlusFramework.Networking
{
    [Serializable]
    public enum PacketType
    {
        Definitions = 1,
        Missiles = 2,
        Command = 4
    }

    [ProtoInclude(1001, typeof(SyncMissileTarget))]
    [ProtoInclude(1002, typeof(ChatCommand))]
    [ProtoInclude(1003, typeof(Request))]
    [ProtoInclude(1004, typeof(SendDefinition))]
    [ProtoContract]
    public abstract class Packet
    {
        [ProtoMember(999)]
        public PacketType DataType;

        public override string ToString()
        {
            return base.ToString() + $" {DataType}";
        }

        public Packet() { }
    }

    [ProtoContract]
    public class SendDefinition : Packet
    {
        [ProtoMember(1)]
        public VPFAmmoDefinition Data;

        public SendDefinition(VPFAmmoDefinition data, PacketType type)
        {
            Data = data;
            DataType = type;
        }

        public SendDefinition() { }
    }

    [ProtoContract]
    public class SyncMissileTarget : Packet
    {

        [ProtoMember(1)]
        public long[] missileIDs;

        [ProtoMember(2)]
        public long[] targetIDs;

        public SyncMissileTarget(long[] missileIDs, long[] targetIDs)
        {
            this.missileIDs = missileIDs;
            this.targetIDs = targetIDs;
            DataType = PacketType.Missiles;
        }

        public SyncMissileTarget() { }
    }


    [ProtoContract]
    public class Request : Packet
    {
        public Request(PacketType type)
        {
            DataType = type;
        }

        public Request() { }
    }

    [ProtoContract]

    public class ChatCommand : Packet
    {
        [ProtoMember(1)]
        public ulong SenderId;
        [ProtoMember(2)]
        public string[] message;

        public ChatCommand(ulong senderId, string[] message)
        {
            SenderId = senderId;
            this.message = message;
            DataType = PacketType.Command;
        }

        public ChatCommand() { }
    }
}
