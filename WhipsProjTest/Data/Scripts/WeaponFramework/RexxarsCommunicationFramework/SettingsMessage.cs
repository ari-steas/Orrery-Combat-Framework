using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Serialization;

namespace Rexxar.Communication
{
    [ProtoContract]
    public class SettingsMessage : Message
    {
        [ProtoMember(1)]
        private ConcurrentDictionary<long, Settings.RailgunSettings> _settings;

        public SettingsMessage(ConcurrentDictionary<long, Settings.RailgunSettings> settings)
        {
            _settings = settings;
        }

        public SettingsMessage()
        {
            
        }

        public override void HandleServer()
        {
            if (_settings == null || _settings.Count == 0)
                return;

            Settings.ConsumeSync(_settings);
            this.SenderId = MyAPIGateway.Multiplayer.MyId;
            Communication.SendMessageToClients(this, ignore: this.SenderId);
            Settings.CommitSettings();
        }

        public override void HandleClient()
        {
            if (_settings == null || _settings.Count == 0)
                return;

            Settings.ConsumeSync(_settings);
            Settings.CommitSettings();
        }
    }
}
