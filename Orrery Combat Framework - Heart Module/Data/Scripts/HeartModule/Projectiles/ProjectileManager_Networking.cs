using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    partial class ProjectileManager
    {
        public Dictionary<ulong, Queue<uint>> SyncStream = new Dictionary<ulong, Queue<uint>>();
        public void SyncProjectile(Projectile projectile, int DetailLevel = 1, ulong PlayerSteamId = 0)
        {
            if (PlayerSteamId == 0)
                HeartData.I.Net.SendToEveryone(projectile.AsSerializable(DetailLevel));
            else
                HeartData.I.Net.SendToPlayer(projectile.AsSerializable(DetailLevel), PlayerSteamId);
        }
    }
}
