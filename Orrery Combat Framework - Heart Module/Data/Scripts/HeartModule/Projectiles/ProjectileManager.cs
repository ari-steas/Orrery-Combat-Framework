using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
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
    public partial class ProjectileManager : MySessionComponentBase
    {
        public static ProjectileManager I = new ProjectileManager();

        private Dictionary<uint, Projectile> ActiveProjectiles = new Dictionary<uint, Projectile>();
        private HashSet<Projectile> ProjectilesWithHealth = new HashSet<Projectile>();
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
            foreach (var projectile in ActiveProjectiles.Values.ToArray()) // This can be modified by ModApi calls during run
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
                if (ProjectilesWithHealth.Contains(projectile))
                    ProjectilesWithHealth.Remove(projectile);
                projectile.OnClose.Invoke(projectile);
                if (projectile.Health < 0)
                    MyAPIGateway.Utilities.ShowNotification(projectile.Id + "");
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
                projectile.DrawUpdate(); // Draw delta is always 1/60 because Keen:tm:
        }

        public void UpdateProjectileSync(n_SerializableProjectile projectile)
        {
            if (MyAPIGateway.Session.IsServer)
                return;

            if (IsIdAvailable(projectile.Id) && projectile.DefinitionId.HasValue)
            {
                if (projectile.Firer != null)
                {
                    WeaponManager.I.GetWeapon(projectile.Firer.Value)?.MuzzleFlash(true);
                }
                AddProjectile(new Projectile(projectile));
            }
            else
            {
                Projectile p = GetProjectile(projectile.Id);
                if (p != null)
                    p.UpdateFromSerializable(projectile);
                else if (projectile.IsActive ?? false)
                    HeartData.I.Net.SendToServer(new n_ProjectileRequest(projectile.Id));
            }
        }

        public Projectile AddProjectile(int projectileDefinitionId, Vector3D position, Vector3D direction, IMyConveyorSorter sorterWep)
        {
            try
            {
                if (ProjectileDefinitionManager.GetDefinition(projectileDefinitionId)?.PhysicalProjectile.IsHitscan ?? false)
                    return AddHitscanProjectile(projectileDefinitionId, position, direction, sorterWep.EntityId);
                else
                    return AddProjectile(new Projectile(projectileDefinitionId, position, direction, sorterWep));
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException($"Invalid ammo definition ({projectileDefinitionId} of {ProjectileDefinitionManager.DefinitionCount()})", ex, typeof(ProjectileManager));
                return null;
            }
        }

        public Projectile AddProjectile(int projectileDefinitionId, Vector3D position, Vector3D direction, long firer, Vector3D initialVelocity)
        {
            try
            {
                if (ProjectileDefinitionManager.GetDefinition(projectileDefinitionId)?.PhysicalProjectile.IsHitscan ?? false)
                    return AddHitscanProjectile(projectileDefinitionId, position, direction, firer);
                else
                    return AddProjectile(new Projectile(projectileDefinitionId, position, direction, firer, initialVelocity));
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException($"Invalid ammo definition ({projectileDefinitionId} of {ProjectileDefinitionManager.DefinitionCount()})", ex, typeof(ProjectileManager));
                return null;
            }
        }

        private Projectile AddProjectile(Projectile projectile)
        {
            if (projectile == null || projectile.DefinitionId == -1) return null; // Ensure that invalid projectiles don't get added

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
            if (projectile.Definition.PhysicalProjectile.Health > 0 && projectile.Definition.PhysicalProjectile.ProjectileSize > 0)
                ProjectilesWithHealth.Add(projectile);
            return projectile;
        }

        Dictionary<long, uint> HitscanList = new Dictionary<long, uint>();
        private Projectile AddHitscanProjectile(int projectileDefinitionId, Vector3D position, Vector3D direction, long firer)
        {
            if (!HitscanList.ContainsKey(firer))
            {
                Projectile p = new Projectile(projectileDefinitionId, position, direction, firer);
                AddProjectile(p);
                p.OnClose += (projectile) => HitscanList.Remove(firer);
                HitscanList.Add(firer, p.Id);
            }
            Projectile outProjectile = GetProjectile(HitscanList[firer]);
            outProjectile?.UpdateHitscan(position, direction);
            return outProjectile;
        }

        public Projectile GetProjectile(uint id) => ActiveProjectiles.GetValueOrDefault(id, null);
        public bool IsIdAvailable(uint id) => !ActiveProjectiles.ContainsKey(id);

        /// <summary>
        /// Populates a list with all projectiles in a sphere.
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="projectiles"></param>
        /// <param name="onlyDamageable"></param>
        public void GetProjectilesInSphere(BoundingSphereD sphere, ref List<Projectile> projectiles, bool onlyDamageable = false)
        {
            projectiles.Clear();
            double rangeSq = sphere.Radius * sphere.Radius;
            Vector3D pos = sphere.Center;

            if (onlyDamageable)
                foreach (var projectil in ProjectilesWithHealth)
                    if (Vector3D.DistanceSquared(pos, projectil.Position) < rangeSq)
                        projectiles.Add(projectil);
            else
                foreach (var projectile in ActiveProjectiles.Values)
                    if (Vector3D.DistanceSquared(pos, projectile.Position) < rangeSq)
                        projectiles.Add(projectile);
        }
    }
}
