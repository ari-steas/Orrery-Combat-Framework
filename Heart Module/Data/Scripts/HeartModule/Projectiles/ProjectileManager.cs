using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Game.Components;
using VRage.Game.ModAPI;
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
        private float delta = 0;
        private Stopwatch clock = Stopwatch.StartNew();

        public override void LoadData()
        {
            I = this;
            MyAPIGateway.Utilities.MessageEnteredSender += TempChatCommandHandler;
            DamageHandler.Load();
        }

        protected override void UnloadData()
        {
            I = null;
            MyAPIGateway.Utilities.MessageEnteredSender -= TempChatCommandHandler;
            DamageHandler.Unload();
        }

        private void TempChatCommandHandler(ulong sender, string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("!"))
                return;

            string[] split = messageText.Split(' ');
            switch (split[0].ToLower())
            {
                case "!hhelp":
                    MyAPIGateway.Utilities.ShowMessage("HeartModule", "Commands:\n!hHelp - Prints all commands\n(press L) - Spawns a projectile on your face");
                    sendToOthers = false;
                    break;
            }
        }

        int j = 0;
        public override void UpdateAfterSimulation()
        {
            if (HeartData.I.IsSuspended) return;

            // spawn projectiles at world origin for debugging. Don't actually do this to spawn projectiles, please.
            if (j >= 25 && MyAPIGateway.Session.IsServer)
            {
                j = 0;
                try
                {
                    Random r = new Random();
                    Vector3D randVec = new Vector3D(r.NextDouble(), r.NextDouble(), r.NextDouble()).Normalized();
                    Projectile p = new Projectile(new SerializableProjectile()
                    {
                        IsActive = true,
                        Id = 0,
                        DefinitionId = 0,
                        Position = MyAPIGateway.Session.Player?.GetPosition() ?? Vector3D.Zero, // CHECK OUT HOW HARD I CAN PISS
                        Direction = MyAPIGateway.Session.Player?.Controller.ControlledEntity.Entity.WorldMatrix.Forward.Rotate(randVec, r.NextDouble() * 0.0873 - 0.04365) ?? Vector3D.Forward,
                        Velocity = 100,
                        Timestamp = DateTime.Now.Ticks,
                        InheritedVelocity = Vector3D.Zero,
                        Firer = MyAPIGateway.Session.Player?.Controller.ControlledEntity.Entity.EntityId ?? -1,
                    });
                    AddProjectile(p);
                    //MyLog.Default.WriteLineToConsole($"Projectiles: {ActiveProjectiles.Count}");
                }
                catch (Exception ex)
                {
                    SoftHandle.RaiseException(ex, typeof(ProjectileManager));
                }
            }
            //j++;

            // Delta time for tickrate-independent projectile movement
            delta = clock.ElapsedTicks / (float)TimeSpan.TicksPerSecond;

            // Tick projectiles
            foreach (var projectile in ActiveProjectiles.Values)
            {
                projectile.TickUpdate(delta);
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
                    if (!ProjectileSyncStream.ContainsKey(player.SteamUserId))
                        ProjectileSyncStream.Add(player.SteamUserId, new List<uint>());
                
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
                        ProjectileSyncStream.Remove(syncedPlayerSteamId);
                }
            }
            else
            {
                MyAPIGateway.Utilities.ShowNotification("Projectiles: " + ActiveProjectiles.Count, 1000 / 60);
            }

            DamageHandler.Update();

            clock.Restart();
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
            clock.Stop();
        }

        public override void Draw()
        {
            if (HeartData.I.IsSuspended) return;

            if (MyAPIGateway.Utilities.IsDedicated) // We don't want to needlessly use server CPU time
                return;

            delta = clock.ElapsedTicks / (float)TimeSpan.TicksPerSecond;
            // Triggered every frame, avoids jitter in projectiles
            foreach (var projectile in ActiveProjectiles.Values)
                projectile.DrawUpdate(delta);
        }

        public void UpdateProjectile(SerializableProjectile projectile)
        {
            if (MyAPIGateway.Session.IsServer)
                return;

            if (IsIdAvailable(projectile.Id) && projectile.IsActive && projectile.DefinitionId.HasValue)
                AddProjectile(new Projectile(projectile));
            else
                GetProjectile(projectile.Id)?.SyncUpdate(projectile);
        }

        public void AddProjectile(Projectile projectile)
        {
            if (projectile == null || projectile.DefinitionId == -1) return; // Ensure that invalid projectiles don't get added

            NextId++;
            while (!IsIdAvailable(NextId))
                NextId++;
            projectile.SetId(NextId);
            ActiveProjectiles.Add(projectile.Id, projectile);
            SyncProjectile(projectile, 0);
            if (!MyAPIGateway.Utilities.IsDedicated)
                projectile.InitDrawing();
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
