using Heart_Module.Data.Scripts.HeartModule.Network;
using ProtoBuf;
using Sandbox.ModAPI;
using System.Collections.Generic;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses
{
    [ProtoContract]
    public class n_ProjectileArray : PacketBase
    {
        [ProtoMember(21)] byte[][] Projectiles = new byte[0][];

        public n_ProjectileArray() { }
        public n_ProjectileArray(List<n_SerializableProjectile> projectiles)
        {
            SerializeProjectiles(projectiles.ToArray());
        }

        public n_ProjectileArray(n_SerializableProjectile[] projectiles)
        {
            SerializeProjectiles(projectiles);
        }

        private void SerializeProjectiles(n_SerializableProjectile[] projectiles)
        {
            Projectiles = new byte[projectiles.Length][];

            for (int i = 0; i < Projectiles.Length; i++)
                Projectiles[i] = MyAPIGateway.Utilities.SerializeToBinary(projectiles[i]);
        }

        private n_SerializableProjectile[] DeSerializeProjectiles()
        {
            n_SerializableProjectile[] deSerialized = new n_SerializableProjectile[Projectiles.Length];

            for (int i = 0; i < Projectiles.Length; i++)
                deSerialized[i] = MyAPIGateway.Utilities.SerializeFromBinary<n_SerializableProjectile>(Projectiles[i]);

            return deSerialized;
        }

        public override void Received(ulong SenderSteamId)
        {
            if (MyAPIGateway.Session.IsServer)
                return;
            foreach (var projectile in DeSerializeProjectiles())
                projectile?.Received(SenderSteamId);
        }
    }
}
