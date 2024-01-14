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
        const int MaxProjectilesSynced = 25; // This value should result in ~100kB/s per player.

        public Dictionary<ulong, Queue<n_SerializableProjectile>> SyncStream = new Dictionary<ulong, Queue<n_SerializableProjectile>>();

        public void QueueSync(Projectile projectile, int DetailLevel = 1, ulong PlayerSteamId = 0)
        {
            n_SerializableProjectile sP = projectile.AsSerializable(DetailLevel);
            if (PlayerSteamId == 0)
                foreach (var queue in SyncStream.Values)
                    queue.Enqueue(sP);
            else if (SyncStream.ContainsKey(PlayerSteamId))
                SyncStream[PlayerSteamId].Enqueue(sP);

            //if (PlayerSteamId == 0)
            //    HeartData.I.Net.SendToEveryone(projectile.AsSerializable(DetailLevel));
            //else
            //    HeartData.I.Net.SendToPlayer(projectile.AsSerializable(DetailLevel), PlayerSteamId);
        }

        public void UpdateSync()
        {
            if (MyAPIGateway.Session.IsServer && MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Multiplayer.Players.GetPlayers(players);

                foreach (var player in players) // Ensure that all players are being synced
                {
                    if (!ProjectileSyncStream.ContainsKey(player.SteamUserId))
                    {
                        ProjectileSyncStream.Add(player.SteamUserId, new List<uint>());
                        MyLog.Default.WriteLineAndConsole($"Heart Module: Added player {player.SteamUserId}");
                    }
                }

                foreach (ulong syncedPlayerSteamId in ProjectileSyncStream.Keys.ToList())
                {
                    bool remove = true;
                    foreach (var player in players)
                    {
                        if (syncedPlayerSteamId == player.SteamUserId)
                        {
                            SyncPlayerProjectiles(player); // Sync individual players to lower network load
                            remove = false;
                        }
                    }
                    if (remove) // Remove disconnected players from sync list
                    {
                        ProjectileSyncStream.Remove(syncedPlayerSteamId);
                        MyLog.Default.WriteLineAndConsole($"Heart Module: Removed player {syncedPlayerSteamId}");
                    }
                }
            }
        }

        private void SyncPlayerProjectiles(IMyPlayer player)
        {
            int numSyncs = 0;

            for (int i = 0; i < MaxProjectilesSynced && i < ProjectileSyncStream[player.SteamUserId].Count; i++) // Queue updating of projectiles
            {
                uint id = ProjectileSyncStream[player.SteamUserId][i];

                if (ActiveProjectiles.ContainsKey(id))
                {
                    QueueSync(ActiveProjectiles[id], 1, player.SteamUserId);
                    numSyncs++;
                }
            }

            Queue<n_SerializableProjectile> queue = SyncStream[player.SteamUserId];
            List<n_SerializableProjectile> toSync = new List<n_SerializableProjectile>();
            for (int i = 0; i < MaxProjectilesSynced && queue.Count > 0; i++)
            {
                n_SerializableProjectile projectile = queue.Dequeue();
                if (projectile.Position == null || Vector3D.DistanceSquared(projectile.Position.Value, player.GetPosition()) < HeartData.I.SyncRangeSq)
                    toSync.Add(projectile);
            }
            HeartData.I.Net.SendToPlayer(new n_ProjectileArray(toSync), player.SteamUserId);

            //if (ProjectileSyncStream[player.SteamUserId].Count < MaxProjectilesSynced)
            //{
            //    // Limits projectile syncing to within sync range. 
            //    // Syncing is based off of character position for now, camera position may be wise in the future
            //    ProjectileSyncStream[player.SteamUserId].Clear();
            //    foreach (var projectile in ActiveProjectiles.Values)
            //        if (Vector3D.DistanceSquared(projectile.Position, player.GetPosition()) < HeartData.I.SyncRangeSq)
            //            ProjectileSyncStream[player.SteamUserId].Add(projectile.Id);
            //}
            //else
            //    ProjectileSyncStream[player.SteamUserId].RemoveRange(0, MaxProjectilesSynced);
        }
    }
}
