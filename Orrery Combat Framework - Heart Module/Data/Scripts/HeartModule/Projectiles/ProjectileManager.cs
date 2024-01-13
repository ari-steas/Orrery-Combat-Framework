using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ProjectileManager : MySessionComponentBase
    {
        public static ProjectileManager I = new ProjectileManager();
        const int MaxProjectilesSynced = 25; // This value should result in ~100kB/s per player.

        private Dictionary<uint, Projectile> ActiveProjectiles = new Dictionary<uint, Projectile>();
        private Dictionary<ulong, List<uint>> ProjectileSyncStream = new Dictionary<ulong, List<uint>>();
        public uint NextId { get; private set; } = 0;
        private List<Projectile> QueuedCloseProjectiles = new List<Projectile>();
        /// <summary>
        /// Delta for engine ticks; 60tps
        /// </summary>
        private const float deltaTick = 1/60f;
        /// <summary>
        /// Delta for frames; varies
        /// </summary>
        private float deltaDraw = 0;
        private Stopwatch clockTick = Stopwatch.StartNew();
        private Stopwatch clockDraw = Stopwatch.StartNew();

        public override void LoadData()
        {
            I = this;
            DamageHandler.Load();
        }

        protected override void UnloadData()
        {
            I = null;
            DamageHandler.Unload();
        }

        public override void UpdateAfterSimulation()
        {
            if (HeartData.I.IsSuspended) return;

            // Tick projectiles
            foreach (var projectile in ActiveProjectiles.Values)
            {
                projectile.TickUpdate(deltaTick);
                if (projectile.QueuedDispose)
                    QueuedCloseProjectiles.Add(projectile);
            }

            // Queued removal of projectiles
            foreach (var projectile in QueuedCloseProjectiles)
            {
                //MyAPIGateway.Utilities.ShowMessage("Heart", $"Closing projectile {projectile.Id}. Age: {projectile.Age} ");
                if (MyAPIGateway.Session.IsServer)
                    SyncProjectile(projectile, 2);

                if (!MyAPIGateway.Utilities.IsDedicated)
                    projectile.CloseDrawing();

                ActiveProjectiles.Remove(projectile.Id);
                projectile.OnClose.Invoke(projectile);
            }
            QueuedCloseProjectiles.Clear();

            // Sync stuff
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
            else
            {
                MyAPIGateway.Utilities.ShowNotification("Projectiles: " + ActiveProjectiles.Count, 1000 / 60);
            }

            DamageHandler.Update();

            clockTick.Restart();
        }

        private void SyncPlayerProjectiles(IMyPlayer player)
        { // NOTE - BEAMS SHOULD NOT BE SYNCED ASIDE FROM ON SHOOT.
            int numSyncs = 0;

            for (int i = 0; i < MaxProjectilesSynced && i < ProjectileSyncStream[player.SteamUserId].Count; i++)
            {
                uint id = ProjectileSyncStream[player.SteamUserId][i];

                if (ActiveProjectiles.ContainsKey(id))
                {
                    SyncProjectile(ActiveProjectiles[id], 1, player.SteamUserId);
                    numSyncs++;
                }
            }

            if (ProjectileSyncStream[player.SteamUserId].Count < MaxProjectilesSynced)
            {
                // Limits projectile syncing to within sync range. 
                // Syncing is based off of character position for now, camera position may be wise in the future
                ProjectileSyncStream[player.SteamUserId].Clear();
                foreach (var projectile in ActiveProjectiles.Values)
                    if (Vector3D.DistanceSquared(projectile.Position, player.GetPosition()) < HeartData.I.SyncRangeSq)
                        ProjectileSyncStream[player.SteamUserId].Add(projectile.Id);
            }
            else
                ProjectileSyncStream[player.SteamUserId].RemoveRange(0, MaxProjectilesSynced);
        }

        public override void UpdatingStopped()
        {
            clockTick.Stop();
        }

        public override void Draw() // Called once per frame to avoid jitter
        {
            if (HeartData.I.IsSuspended || MyAPIGateway.Utilities.IsDedicated) // We don't want to needlessly use server CPU time
                return;

            float deltaDrawTick = (float)clockTick.ElapsedTicks / TimeSpan.TicksPerSecond; // deltaDrawTick is the current offset between tick and draw, to account for variance between FPS and tickrate
            deltaDraw = (float)clockDraw.ElapsedTicks / TimeSpan.TicksPerSecond; // deltaDraw is a standard delta value based on FPS

            foreach (var projectile in ActiveProjectiles.Values)
                projectile.DrawUpdate(deltaDrawTick, deltaDraw);

            clockDraw.Restart();
        }

        public void UpdateProjectile(n_SerializableProjectile projectile)
        {
            if (MyAPIGateway.Session.IsServer)
                return;

            if (IsIdAvailable(projectile.Id) && projectile.IsActive && projectile.DefinitionId.HasValue)
                AddProjectile(new Projectile(projectile));
            else
            {
                Projectile p = GetProjectile(projectile.Id);
                if (p != null)
                    p.UpdateFromSerializable(projectile);
                else
                    HeartData.I.Net.SendToServer(new n_ProjectileRequest(projectile.Id));
            }
        }

        public void AddProjectile(int projectileDefinitionId, Vector3D position, Vector3D direction, IMyConveyorSorter sorterWep)
        {
            if (ProjectileDefinitionManager.GetDefinition(projectileDefinitionId)?.PhysicalProjectile.IsHitscan ?? false)
                AddHitscanProjectile(projectileDefinitionId, position, direction, sorterWep);
            else
                AddProjectile(new Projectile(projectileDefinitionId, position, direction, sorterWep));
        }

        private void AddProjectile(Projectile projectile)
        {
            if (projectile == null || projectile.DefinitionId == -1) return; // Ensure that invalid projectiles don't get added

            projectile.Position -= projectile.InheritedVelocity / 60f; // Because this doesn't run during simulation

            NextId++;
            while (!IsIdAvailable(NextId))
                NextId++;
            projectile.SetId(NextId);
            ActiveProjectiles.Add(projectile.Id, projectile);
            SyncProjectile(projectile, 0);
            if (!MyAPIGateway.Utilities.IsDedicated)
                projectile.InitEffects();
        }

        Dictionary<long, uint> HitscanList = new Dictionary<long, uint>();
        private void AddHitscanProjectile(int projectileDefinitionId, Vector3D position, Vector3D direction, IMyConveyorSorter sorterWep)
        {
            if (!HitscanList.ContainsKey(sorterWep.EntityId))
            {
                Projectile p = new Projectile(projectileDefinitionId, position, direction, sorterWep);
                AddProjectile(p);
                p.OnClose += (projectile) => HitscanList.Remove(sorterWep.EntityId);
                HitscanList.Add(sorterWep.EntityId, p.Id);
            }

            GetProjectile(HitscanList[sorterWep.EntityId])?.UpdateHitscan(position, direction);
        }

        public void SyncProjectile(Projectile projectile, int DetailLevel = 1, ulong PlayerSteamId = 0)
        {
            if (PlayerSteamId == 0)
                HeartData.I.Net.SendToEveryone(projectile.AsSerializable(DetailLevel));
            else
                HeartData.I.Net.SendToPlayer(projectile.AsSerializable(DetailLevel), PlayerSteamId);
        }

        public Projectile GetProjectile(uint id) => ActiveProjectiles.GetValueOrDefault(id, null);
        public bool IsIdAvailable(uint id) => !ActiveProjectiles.ContainsKey(id);
    }
}
