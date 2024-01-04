using Digi.Examples.NetworkProtobuf;
using Digi.NetworkLib;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Messaging;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class ProjectileManager : MySessionComponentBase
    {
        public static ProjectileManager I = new ProjectileManager();
        const int MaxProjectilesSynced = 500;

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
        }

        protected override void UnloadData()
        {
            I = null;
            MyAPIGateway.Utilities.MessageEnteredSender -= TempChatCommandHandler;
        }

        private void TempChatCommandHandler(ulong sender, string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("!"))
                return;

            string[] split = messageText.Split(' ');
            switch (split[0].ToLower())
            {
                case "!hhelp":
                    MyAPIGateway.Utilities.ShowMessage("HeartModule", "Commands:\n!hHelp - Prints all commands\n!f - Spawns a projectile on your face");
                    sendToOthers = false;
                    break;
                case "!f":
                    if (!MyAPIGateway.Session.IsServer)
                        return;
                    try
                    {
                        AddProjectile(new Projectile(new StandardClasses.SerializableProjectile()
                        {
                            Id = 0,
                            DefinitionId = 0,
                            Position = MyAPIGateway.Session.Player.GetPosition(),
                            Direction = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix.Forward,
                            Velocity = 10,
                            Timestamp = DateTime.Now.Ticks,
                            InheritedVelocity = Vector3D.Zero
                        }));
                    }
                    catch (Exception ex)
                    {
                        MyLog.Default.WriteLineAndConsole(ex.ToString());
                    }
                    sendToOthers = false;
                    break;
            }
        }

        public override void UpdateBeforeSimulation()
        {
            delta = clock.ElapsedTicks / (float) TimeSpan.TicksPerSecond;

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
                MyAPIGateway.Utilities.ShowMessage("Heart", $"Closing projectile {projectile.Id}. Age: {projectile.Age} ");
                projectile.Close.Invoke(projectile);
            }
            QueuedCloseProjectiles.Clear();

            // Sync stuff
            if (MyAPIGateway.Session.IsServer && MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                for (int i = 0; i < MaxProjectilesSynced && i < ProjectileSyncStream.Count; i++)
                {
                    uint id = ProjectileSyncStream[i];

                    if (ActiveProjectiles.ContainsKey(id))
                        HeartNetwork_Session.Net.SendToEveryone(ActiveProjectiles[id].AsSerializable());
                }

                if (ProjectileSyncStream.Count < MaxProjectilesSynced)
                    ProjectileSyncStream.Clear();
                else
                    ProjectileSyncStream.RemoveRange(0, MaxProjectilesSynced);
            }

            clock.Restart();
        }

        public override void Draw()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;
            
            delta = clock.ElapsedTicks / (float)TimeSpan.TicksPerSecond;
            // Triggered every frame, avoids jitter in projectiles
            foreach (var projectile in ActiveProjectiles.Values)
            {
                projectile.DrawUpdate(delta);
            }
        }

        public void AddProjectile(Projectile projectile)
        {
            NextId++;
            while (!IsIdAvailable(NextId))
                NextId++;
            projectile.SetId(NextId);
            projectile.Close += (p) => ActiveProjectiles.Remove(p.Id);
            ActiveProjectiles.Add(projectile.Id, projectile);
        }

        public Projectile GetProjectile(uint id) => ActiveProjectiles.GetValueOrDefault(id, null);
        public bool IsIdAvailable(uint id) => !ActiveProjectiles.ContainsKey(id);
    }
}
