using Heart_Module.Data.Scripts.HeartModule.Network;
using ProtoBuf;
using Sandbox.ModAPI;
using System;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses
{
    [ProtoContract]
    internal class n_ProjectileRequest : PacketBase
    {
        [ProtoMember(21)] uint projectileId;

        public n_ProjectileRequest() { }
        public n_ProjectileRequest(uint projectileId)
        {
            this.projectileId = projectileId;
        }

        public override void Received(ulong SenderSteamId)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                Projectile p = ProjectileManager.I.GetProjectile(projectileId);
                if (p != null)
                ProjectileManager.I.QueueSync(p, 0, SenderSteamId);
            }
        }
    }
}
