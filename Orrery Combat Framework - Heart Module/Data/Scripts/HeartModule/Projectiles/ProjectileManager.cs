using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game.Components;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public partial class ProjectileManager : MySessionComponentBase
    {
        public static ProjectileManager I = new ProjectileManager();

        private Dictionary<uint, Projectile> ActiveProjectiles = new Dictionary<uint, Projectile>();
        public uint NextId { get; private set; } = 0;
        private List<Projectile> QueuedCloseProjectiles = new List<Projectile>();
        /// <summary>
        /// Delta for engine ticks; 60tps
        /// </summary>
        private const float deltaTick = 1 / 60f;
        /// <summary>
        /// Delta for frames; varies
        /// </summary>
        private Stopwatch clockTick = Stopwatch.StartNew();

        public int NumProjectiles => ActiveProjectiles.Count;

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
                    QueueSync(projectile, 2);

                if (!MyAPIGateway.Utilities.IsDedicated)
                    projectile.CloseDrawing();

                ActiveProjectiles.Remove(projectile.Id);
                projectile.OnClose.Invoke(projectile);
            }
            QueuedCloseProjectiles.Clear();

            // Sync stuff
            UpdateSync();

            DamageHandler.Update();

            clockTick.Restart();
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

            foreach (var projectile in ActiveProjectiles.Values)
                projectile.DrawUpdate(deltaDrawTick, 1 / 60f); // Draw delta is always 1/60 because Keen:tm:
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
                else if (projectile.IsActive)
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
            if (MyAPIGateway.Session.IsServer)
                QueueSync(projectile, 0);
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

        public Projectile GetProjectile(uint id) => ActiveProjectiles.GetValueOrDefault(id, null);
        public bool IsIdAvailable(uint id) => !ActiveProjectiles.ContainsKey(id);

        /// <summary>
        /// Populates a list with all projectiles in a sphere.
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="projectiles"></param>
        /// <param name="onlyDamageable"></param>
        public void GetProjectilesInSphere(BoundingSphereD sphere, ref List<uint> projectiles, bool onlyDamageable = false)
        {
            projectiles.Clear();
            double rangeSq = sphere.Radius * sphere.Radius;
            Vector3D pos = sphere.Center;

            foreach (var projectile in ActiveProjectiles.Values)
                if (!onlyDamageable || projectile.Definition.PhysicalProjectile.Health != -1)
                    if (Vector3D.DistanceSquared(pos, projectile.Position) < rangeSq)
                        projectiles.Add(projectile.Id);
        }
    }
}
