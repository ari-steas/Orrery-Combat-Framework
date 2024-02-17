using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.ProjectileNetworking
{
    public class ProjectileNetworking
    {
        const int ProjectilesPerPacket = 50;
        const int TicksPerPacket = 4;

        private Dictionary<ulong, Queue<n_SerializableProjectileInfo>> SyncStream_PP = new Dictionary<ulong, Queue<n_SerializableProjectileInfo>>();
        private Dictionary<ulong, Queue<n_SerializableFireEvent>> SyncStream_FireEvent = new Dictionary<ulong, Queue<n_SerializableFireEvent>>();

        public void QueueSync_PP(Projectile projectile)
        {
            QueueSync_PP(null, projectile);
        }

        public void QueueSync_PP(IMyPlayer player, Projectile projectile)
        {

        }

        public void QueueSync_FireEvent(SorterWeaponLogic weapon, Projectile projectile)
        {
            QueueSync_FireEvent(null, weapon, projectile);
        }

        public void QueueSync_FireEvent(IMyPlayer player, SorterWeaponLogic weapon, Projectile projectile)
        {

        }


        int ticks = 0;
        public void Update1()
        {
            ticks++;
            if (ticks % TicksPerPacket != 0)
                return;
            
            // Iterate through SyncStreams based on projectilesperpacket
            // you will need a way to combine all the projectiles into one packet
            // this will not work without many edits sorry
        }

        public void Init()
        {
            // Called from ProjectileManager, this class will be a variable inside it (like a weapon's magazines)
        }

        public void Close()
        {
            // maybe???
        }
    }
}
