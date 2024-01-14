using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    partial class ProjectileManager
    {
        const int MaxProjectilesSynced = 75; // This value should result in ~100kB/s up per player.

        public Dictionary<ulong, LinkedList<n_SerializableProjectile>> SyncStream = new Dictionary<ulong, LinkedList<n_SerializableProjectile>>();

        public void QueueSync(Projectile projectile, int DetailLevel = 1)
        {
            // Sync to everyone if player is undefined
            foreach (var player in HeartData.I.Players) // Ensure that all players are being synced
                QueueSync(projectile, player, DetailLevel);
        }

        public void QueueSync(Projectile projectile, IMyPlayer Player, int DetailLevel = 1)
        {
            if (!SyncStream.ContainsKey(Player.SteamUserId)) // Avoid throwing an error if the player hasn't been added yet
                return;

            if (DetailLevel == 2 && SyncStream[Player.SteamUserId].Count > MaxProjectilesSynced) // Don't sync projectile closing if network load is too high
                return;

            if (projectile.Position != null && Vector3D.DistanceSquared(projectile.Position, Player.GetPosition()) > HeartData.I.SyncRangeSq) // Don't sync if the player is out of sync range
                return;

            n_SerializableProjectile sP = projectile.AsSerializable(DetailLevel);
            if (DetailLevel == 0)
                SyncStream[Player.SteamUserId].AddFirst(sP); // Queue new projectiles first
            else
                SyncStream[Player.SteamUserId].AddLast(sP); // Queue other projectile updates last
        }

        public void UpdateSync()
        {
            if (MyAPIGateway.Session.IsServer && MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                foreach (var player in HeartData.I.Players) // Ensure that all players are being synced
                {
                    if (!SyncStream.ContainsKey(player.SteamUserId))
                    {
                        SyncStream.Add(player.SteamUserId, new LinkedList<n_SerializableProjectile>());
                        MyLog.Default.WriteLineAndConsole($"Heart Module: Added player {player.SteamUserId}");
                    }
                }

                foreach (ulong syncedPlayerSteamId in SyncStream.Keys.ToList())
                {
                    bool remove = true;
                    foreach (var player in HeartData.I.Players)
                    {
                        if (syncedPlayerSteamId == player.SteamUserId)
                        {
                            SyncPlayerProjectiles(player); // Sync individual players to lower network load
                            remove = false;
                        }
                    }
                    if (remove) // Remove disconnected players from sync list
                    {
                        SyncStream.Remove(syncedPlayerSteamId);
                        MyLog.Default.WriteLineAndConsole($"Heart Module: Removed player {syncedPlayerSteamId}");
                    }
                }
            }
        }

        private void SyncPlayerProjectiles(IMyPlayer player)
        {
            if (!SyncStream.ContainsKey(player.SteamUserId)) // Avoid breaking if the player somehow hasn't been added
            {
                SoftHandle.RaiseSyncException("Player " + player.DisplayName + " is missing projectile sync queue!");
                return;
            }

            LinkedList<n_SerializableProjectile> queue = SyncStream[player.SteamUserId];

            SyncExistingProjectiles(player, queue);

            List<n_SerializableProjectile> toSync = new List<n_SerializableProjectile>();
            for (int i = 0; i < MaxProjectilesSynced && queue.Count > 0; i++)
            {
                n_SerializableProjectile projectile = queue.First();
                queue.RemoveFirst();
                toSync.Add(projectile);
            }
            if (toSync.Count > 0)
                HeartData.I.Net.SendToPlayer(new n_ProjectileArray(toSync), player.SteamUserId);

            if (queue.Count > MaxProjectilesSynced * 2) // Emergency queue freeing
                queue.Clear();
        }

        private void SyncExistingProjectiles(IMyPlayer player, LinkedList<n_SerializableProjectile> queue)
        {
            int numSyncs = 0;
            foreach (var projectile in ActiveProjectiles.Values)
            {
                if (numSyncs > (MaxProjectilesSynced - queue.Count)) // Only sync if there's free network load.
                    break;
                if (Vector3D.DistanceSquared(projectile.Position, player.GetPosition()) < HeartData.I.SyncRangeSq)
                {
                    QueueSync(projectile);
                    numSyncs++;
                }
            }
        }
    }
}
