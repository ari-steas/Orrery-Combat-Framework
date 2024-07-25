using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.ProjectileNetworking;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Sandbox.Game.GUI.DebugInputComponents;
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
        public ProjectileNetwork Network = new ProjectileNetwork();

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

        private Dictionary<long, DateTime> lastLoggedTime = new Dictionary<long, DateTime>();

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

            try
            {
                MyAPIGateway.Parallel.ForEach(ActiveProjectiles.Values.ToArray(), UpdateSingleProjectile);

                foreach (var projectile in QueuedCloseProjectiles)
                {
                    ActiveProjectiles.Remove(projectile.Id);
                    ProjectilesWithHealth.Remove(projectile);
                    projectile.OnClose.Invoke(projectile);
                }
                QueuedCloseProjectiles.Clear();

                // Sync stuff
                Network.Update1();

                DamageHandler.Update();

                clockTick.Restart();
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(ProjectileManager));
            }
        }

        private void UpdateSingleProjectile(Projectile projectile)
        {
            projectile.TickUpdate(deltaTick);

            if (projectile.QueuedDispose)
                QueuedCloseProjectiles.Add(projectile);
        }

        public override void UpdatingStopped()
        {
            clockTick.Stop();
        }

        public override void Draw() // Called once per frame to avoid jitter
        {
            if (HeartData.I.IsSuspended || MyAPIGateway.Utilities.IsDedicated) // We don't want to needlessly use server CPU time
                return;

            foreach (var projectile in ActiveProjectiles.Values)
                projectile.DrawUpdate(); // Draw delta is always 1/60 because Keen:tm:
        }

        [Obsolete]
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

        public Projectile AddProjectile(int projectileDefinitionId, Vector3D position, Vector3D direction, IMyConveyorSorter sorterWep, bool shouldSync = true)
        {
            try
            {
                return AddProjectile(new Projectile(projectileDefinitionId, position, direction, sorterWep), shouldSync);
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException($"Invalid ammo definition ({projectileDefinitionId} of {ProjectileDefinitionManager.DefinitionCount()})", ex, typeof(ProjectileManager));
                return null;
            }
        }

        public Projectile AddProjectile(int projectileDefinitionId, Vector3D position, Vector3D direction, long firer, Vector3D initialVelocity, bool shouldSync = true)
        {
            try
            {
                return AddProjectile(new Projectile(projectileDefinitionId, position, direction, firer, initialVelocity), shouldSync);
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException($"Invalid ammo definition ({projectileDefinitionId} of {ProjectileDefinitionManager.DefinitionCount()})", ex, typeof(ProjectileManager));
                return null;
            }
        }

        internal Projectile AddProjectile(Projectile projectile, bool shouldSync = true)
        {
            if (projectile == null || projectile.DefinitionId == -1) return null;

            projectile.Position -= projectile.InheritedVelocity / 60f;

            NextId++;
            while (!IsIdAvailable(NextId))
                NextId++;
            projectile.SetId(NextId);

            // Rate-limited logging
            if (ShouldLog(projectile.Firer))
            {
                HeartLog.Log($"SpawnProjectile {projectile.Id} | [{projectile.Definition.Name}] {projectile.Definition.Networking.NetworkingMode}");
            }

            ActiveProjectiles.Add(projectile.Id, projectile);
            if (MyAPIGateway.Session.IsServer && shouldSync)
            {
                switch (projectile.Definition.Networking.NetworkingMode)
                {
                    case Networking.NetworkingModeEnum.FullSync:
                        Network.QueueSync_PP(projectile, 0);
                        break;
                    case Networking.NetworkingModeEnum.FireEvent:
                        Network.QueueSync_FireEvent(projectile);
                        break;
                }
            }
            if (!MyAPIGateway.Utilities.IsDedicated)
                projectile.InitEffects();
            if (projectile.Definition.PhysicalProjectile.Health > 0 && projectile.Definition.PhysicalProjectile.ProjectileSize > 0)
                ProjectilesWithHealth.Add(projectile);

            return projectile;
        }

        private bool ShouldLog(long firerId)
        {
            DateTime lastTime;
            if (!lastLoggedTime.TryGetValue(firerId, out lastTime))
            {
                lastTime = DateTime.MinValue;
            }

            DateTime now = DateTime.UtcNow;
            if ((now - lastTime).TotalSeconds >= 1) // Log at most once per second for each weapon
            {
                lastLoggedTime[firerId] = now;
                return true;
            }

            return false;
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
            {
                foreach (var projectile in ProjectilesWithHealth)
                    if (Vector3D.DistanceSquared(pos, projectile.Position) < rangeSq)
                        projectiles.Add(projectile);
            }
            else
            {
                foreach (var projectile in ActiveProjectiles.Values)
                    if (Vector3D.DistanceSquared(pos, projectile.Position) < rangeSq)
                        projectiles.Add(projectile);
            }
        }
    }
}
