using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRage.Library;
using VRage.Utils;
using VRageMath;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.ProjectileNetworking
{
    public class ProjectileNetwork
    {
        const int ProjectilesPerPacket = 50;
        const int TicksPerPacket = 4;

        private Dictionary<ulong, PriorityQueue<Projectile, ushort>> SyncStream_PP = new Dictionary<ulong, PriorityQueue<Projectile, ushort>>();
        private Dictionary<ulong, PriorityQueue<Projectile, ushort>> SyncStream_FireEvent = new Dictionary<ulong, PriorityQueue<Projectile, ushort>>();

        public void QueueSync_PP(Projectile projectile, int detailLevel = 0)
        {
            foreach (var player in HeartData.I.Players)
                QueueSync_PP(player, projectile, detailLevel);
        }

        public void QueueSync_PP(IMyPlayer player, Projectile projectile, int detailLevel = 0) // TODO implement detailLevel
        {
            if (!SyncStream_PP.ContainsKey(player.SteamUserId)) // Avoid throwing an error if the player hasn't been added yet
                return;

            SyncStream_PP[player.SteamUserId].Enqueue(projectile);
        }

        /// <summary>
        /// Enqueues a FireEvent for all players.
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="projectile"></param>
        public void QueueSync_FireEvent(Projectile projectile)
        {
            foreach (var player in HeartData.I.Players)
                QueueSync_FireEvent(player, projectile);
        }
        
        /// <summary>
        /// Enqueues a FireEvent.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="weapon"></param>
        /// <param name="projectile"></param>
        public void QueueSync_FireEvent(IMyPlayer player, Projectile projectile)
        {
            if (!SyncStream_FireEvent.ContainsKey(player.SteamUserId)) // Avoid throwing an error if the player hasn't been added yet
                return;

            SyncStream_FireEvent[player.SteamUserId].Enqueue(projectile);
        }

        /// <summary>
        /// Recieve a projectileInfos packet and generate its projectiles.
        /// </summary>
        /// <param name="projectileInfos"></param>
        internal void Recieve_PP(n_SerializableProjectileInfos projectileInfos)
        {
            if (MyAPIGateway.Session.IsServer || MyAPIGateway.Session.Player?.Character == null)
                return;

            if (projectileInfos == null)
            {
                SoftHandle.RaiseException("Null ProjectileInfos!", null, typeof(ProjectileNetwork));
                return;
            }

            if (projectileInfos.UniqueProjectileId == null)
                return; // Zero projectiles to sync

            for (int i = 0; i < projectileInfos.UniqueProjectileId.Length; i++)
            {
                if (ProjectileManager.I.IsIdAvailable(projectileInfos.UniqueProjectileId[i]) && projectileInfos.DefinitionId != null)
                {
                    if (projectileInfos.FirerEntityId != null)
                    {
                        WeaponManager.I.GetWeapon(projectileInfos.FirerEntityId[i])?.MuzzleFlash(true);
                    }
                    ProjectileManager.I.AddProjectile(projectileInfos.ToProjectile(i));
                }
                else
                {
                    Projectile p = ProjectileManager.I.GetProjectile(projectileInfos.UniqueProjectileId[i]);
                    if (p != null)
                    {
                        p.Position = projectileInfos.PlayerRelativePosition(i) + MyAPIGateway.Session.Player.Character.GetPosition();
                        p.Direction = projectileInfos.Direction(i);
                        p.LastUpdate = DateTime.UtcNow.Date.AddMilliseconds(projectileInfos.MillisecondsFromMidnight - HeartData.I.Net.ServerTimeOffset).Ticks;

                        if (projectileInfos.ProjectileAge != null)
                            p.Age = projectileInfos.ProjectileAge[i];
                        if (projectileInfos.TargetEntityId != null)
                        {
                            if (projectileInfos.TargetEntityId[i] == null)
                                p.Guidance.SetTarget(null);
                            else
                                p.Guidance.SetTarget(MyAPIGateway.Entities.GetEntityById(projectileInfos.TargetEntityId[i]));
                        }
                    }
                    else
                        HeartData.I.Net.SendToServer(new n_ProjectileRequest(projectileInfos.UniqueProjectileId[i]));
                }
            }
        }

        internal void Recieve_FireEvent(n_SerializableFireEvents fireEvents)
        {
            if (MyAPIGateway.Session.IsServer)
                return;

            if (fireEvents == null)
            {
                SoftHandle.RaiseException("Null ProjectileInfos!", null, typeof(ProjectileNetwork));
                return;
            }

            if (fireEvents.UniqueProjectileId == null)
                return; // Zero projectiles to sync

            for (int i = 0; i < fireEvents.UniqueProjectileId.Length; i++)
            {
                if (ProjectileManager.I.IsIdAvailable(fireEvents.UniqueProjectileId[i]))
                {
                    if (fireEvents.FirerEntityId != null)
                    {
                        WeaponManager.I.GetWeapon(fireEvents.FirerEntityId[i])?.MuzzleFlash(true);
                    }
                    Projectile p = ProjectileManager.I.AddProjectile(fireEvents.ToProjectile(i));
                }
                // No need for an update case because this is only for new projectiles
            }
        }


        int AwaitedTicks = 0;
        public void Update1()
        {
            AwaitedTicks++;
            if (AwaitedTicks % TicksPerPacket != 0 || !(MyAPIGateway.Session.IsServer && MyAPIGateway.Multiplayer.MultiplayerActive))
                return;

            // Iterate through SyncStreams based on projectilesperpacket
            // you will need a way to combine all the projectiles into one packet
            // this will not work without many edits sorry

            foreach (var player in HeartData.I.Players) // Ensure that all players are being synced
            {
                if (!SyncStream_PP.ContainsKey(player.SteamUserId))
                {
                    SyncStream_PP.Add(player.SteamUserId, new PriorityQueue<Projectile, ushort>(PriorityQueueSelector));
                    SyncStream_FireEvent.Add(player.SteamUserId, new PriorityQueue<Projectile, ushort>(PriorityQueueSelector));
                    MyLog.Default.WriteLineAndConsole($"Heart Module: Registered player {player.SteamUserId}");
                }
            }

            foreach (ulong syncedPlayerSteamId in SyncStream_PP.Keys.ToList())
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
                    SyncStream_PP.Remove(syncedPlayerSteamId);
                    SyncStream_FireEvent.Remove(syncedPlayerSteamId);
                    MyLog.Default.WriteLineAndConsole($"Heart Module: Deregistered player {syncedPlayerSteamId}");
                }
            }
        }

        private void SyncPlayerProjectiles(IMyPlayer player)
        {
            // TODO: Distance based syncing.
            if (player.Character != null) // TODO: Proper handling for spectator
            {
                if (!SyncStream_PP.ContainsKey(player.SteamUserId)) // Avoid breaking if the player somehow hasn't been added
                {
                    SoftHandle.RaiseSyncException("Player " + player.DisplayName + " is missing projectile sync queue!");
                    return;
                }

                // Full Projectile Packets
                List<Projectile> PPProjectiles = new List<Projectile>();
                for (int i = 0; SyncStream_PP[player.SteamUserId].Count > 0 && i < ProjectilesPerPacket; i++) // Add up to (n) projectiles to the queue
                    PPProjectiles.Add(SyncStream_PP[player.SteamUserId].Dequeue());

                if (PPProjectiles.Count > 0)
                {
                    n_SerializableProjectileInfos ppInfos = new n_SerializableProjectileInfos(PPProjectiles, player.Character);
                    HeartData.I.Net.SendToPlayer(ppInfos, player.SteamUserId);
                }
            }

            // FireEvent packets (these are smaller but less precise)
            List<Projectile> FEProjectiles = new List<Projectile>();
            for (int i = 0; SyncStream_FireEvent[player.SteamUserId].Count > 0 && i < ProjectilesPerPacket; i++) // Add up to (n) projectiles to the queue
                FEProjectiles.Add(SyncStream_FireEvent[player.SteamUserId].Dequeue());

            if (FEProjectiles.Count > 0)
            {
                n_SerializableFireEvents feInfos = new n_SerializableFireEvents(FEProjectiles);
                HeartData.I.Net.SendToPlayer(feInfos, player.SteamUserId);
            }

            // TODO: FireEvents
        }

        public void Init()
        {
            // Called from ProjectileManager, this class will be a variable inside it (like a weapon's magazines)
        }

        public void Close()
        {
            // maybe???
        }

        private static Func<Projectile, ushort> PriorityQueueSelector = (projectile) => projectile.Definition.Networking.NetworkPriority;
    }
}
