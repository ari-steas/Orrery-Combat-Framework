using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Game.Components;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ProjectileManager : MySessionComponentBase
    {
        public static ProjectileManager I = new ProjectileManager();
        const int MaxProjectilesSynced = 25; // TODO: Sync within range of client. This value should be ~100kB/s per player

        private Dictionary<uint, Projectile> ActiveProjectiles = new Dictionary<uint, Projectile>();
        private List<uint> ProjectileSyncStream = new List<uint>();
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

            if (j >= 1 && MyAPIGateway.Session.IsServer)
            {
                j = 0;
                try
                {
                    Projectile p = new Projectile(new SerializableProjectile()
                    {
                        IsActive = true,
                        Id = 0,
                        DefinitionId = 0,
                        Position = MyAPIGateway.Session.Player?.GetPosition() ?? Vector3D.Zero,
                        Direction = MyAPIGateway.Session.Player?.Controller.ControlledEntity.Entity.WorldMatrix.Forward ?? Vector3D.Forward,
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
            j++;

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
                projectile.Close.Invoke(projectile);
            }
            QueuedCloseProjectiles.Clear();

            // Sync stuff
            int numSyncs = 0;
            if (MyAPIGateway.Session.IsServer && MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                for (int i = 0; i < MaxProjectilesSynced && i < ProjectileSyncStream.Count; i++)
                {
                    uint id = ProjectileSyncStream[i];

                    if (ActiveProjectiles.ContainsKey(id))
                    {
                        SyncProjectile(ActiveProjectiles[id], 1);
                        numSyncs++;
                    }
                }

                if (ProjectileSyncStream.Count < MaxProjectilesSynced)
                {
                    ProjectileSyncStream.Clear();
                    ProjectileSyncStream = ActiveProjectiles.Keys.ToList();
                }
                else
                    ProjectileSyncStream.RemoveRange(0, MaxProjectilesSynced);
            }
            else
            {
                MyAPIGateway.Utilities.ShowNotification("Projectiles: " + ActiveProjectiles.Count, 1000 / 60);
            }

            DamageHandler.Update();

            clock.Restart();
        }

        public override void UpdatingStopped()
        {
            clock.Stop();
        }

        public override void Draw()
        {
            if (HeartData.I.IsSuspended) return;

            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            delta = clock.ElapsedTicks / (float)TimeSpan.TicksPerSecond;
            // Triggered every frame, avoids jitter in projectiles
            foreach (var projectile in ActiveProjectiles.Values)
            {
                projectile.DrawUpdate(delta);
            }
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
            projectile.Close += (p) => ActiveProjectiles.Remove(p.Id);
            ActiveProjectiles.Add(projectile.Id, projectile);
            SyncProjectile(projectile, 0);
        }

        public void SyncProjectile(Projectile projectile, int DetailLevel = 1) => HeartData.I.Net.SendToEveryone(projectile.AsSerializable(DetailLevel));

        public Projectile GetProjectile(uint id) => ActiveProjectiles.GetValueOrDefault(id, null);
        public bool IsIdAvailable(uint id) => !ActiveProjectiles.ContainsKey(id);
    }
}
