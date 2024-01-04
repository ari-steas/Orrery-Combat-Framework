using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private Dictionary<uint, Projectile> ActiveProjectiles = new Dictionary<uint, Projectile>();
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
                    try
                    {
                        AddProjectile(new Projectile(new StandardClasses.SerializableProjectile()
                        {
                            Id = 0,
                            DefinitionId = 0,
                            Position = MyAPIGateway.Session.Player.GetPosition(),
                            Direction = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix.Forward,
                            Velocity = 10,
                            Acceleration = 5,
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

            foreach (var projectile in ActiveProjectiles.Values)
            {
                projectile.TickUpdate(delta);
                if (projectile.QueuedClose)
                    QueuedCloseProjectiles.Add(projectile);
            }

            foreach (var projectile in QueuedCloseProjectiles)
            {
                MyAPIGateway.Utilities.ShowMessage("Heart", $"Closing projectile {projectile.Id}. Age: {projectile.Age} ");
                projectile.Close.Invoke(projectile);
            }
            QueuedCloseProjectiles.Clear();

            clock.Restart();
        }

        public override void Draw()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            delta = clock.ElapsedTicks / (float)TimeSpan.TicksPerSecond;

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
