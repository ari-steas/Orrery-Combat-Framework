using Heart_Module.Data.Scripts.HeartModule.Network;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses
{
    [ProtoContract]
    public class n_ProjectileArray : PacketBase
    {
        [ProtoMember(21)] n_SerializableProjectile[] Projectiles;

        public n_ProjectileArray() { }
        public n_ProjectileArray(List<n_SerializableProjectile> projectiles)
        {
            Projectiles = projectiles.ToArray();
        }

        public n_ProjectileArray(n_SerializableProjectile[] projectiles)
        {
            Projectiles = projectiles;
        }

        public override void Received(ulong SenderSteamId)
        {
            if (MyAPIGateway.Session.IsServer)
                return;
            foreach (var projectile in Projectiles)
                projectile.Received(SenderSteamId);
        }
    }
}
