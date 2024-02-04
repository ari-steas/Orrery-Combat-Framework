using Heart_Module.Data.Scripts.HeartModule.Network;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using ProtoBuf;
using Sandbox.ModAPI;
using System.ComponentModel;

namespace Heart_Module.Data.Scripts.HeartModule.Definitions
{
    [ProtoContract]
    public class n_ProjectileDefinitionIdSync : PacketBase
    {
        [ProtoMember(21)] public int Id { get; set; }
        [ProtoMember(22)] public string Name { get; set; }
        [ProtoMember(23), DefaultValue(null)] public byte[] Serialized = null;

        public n_ProjectileDefinitionIdSync() { }
        public n_ProjectileDefinitionIdSync(int id, string name, byte[] serialized = null)
        {
            Id = id;
            Name = name;
            Serialized = serialized;
        }

        public override void Received(ulong SenderSteamId)
        {
            if (MyAPIGateway.Session.IsServer)
                return;
            HeartData.I.Log.Log("Syncing projectile definition " + Name + " to " + Id);
            if (Serialized != null)
                ProjectileDefinitionManager.RegisterDefinition(MyAPIGateway.Utilities.SerializeFromBinary<ProjectileDefinitionBase>(Serialized));
            //ProjectileDefinitionManager.ReorderDefinitions(Name, Id);
        }
    }
}
