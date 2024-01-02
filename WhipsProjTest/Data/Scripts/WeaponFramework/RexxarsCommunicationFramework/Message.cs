using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using ProtoBuf;

namespace Rexxar.Communication
{
    [ProtoInclude(1354, typeof(SettingsMessage))]
    [ProtoContract]
    public abstract class Message
    {
        [ProtoMember(1)]
        public ulong SenderId;

        public Message()
        {
            SenderId = MyAPIGateway.Multiplayer.MyId;
        }

        public abstract void HandleServer();
        public abstract void HandleClient();
    }
}
