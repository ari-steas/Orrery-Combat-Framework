using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using ParallelTasks;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    public partial class ProjectileManager
    {
        public static ProjectileManager I;
        public const int MaxActiveProjectiles = 150000;

        private Dictionary<uint, Projectile> ActiveProjectiles = new Dictionary<uint, Projectile>();
        private HashSet<Projectile> ProjectilesWithHealth = new HashSet<Projectile>();
        public uint NextId { get; private set; } = 0;
        private List<uint> QueuedCloseProjectiles = new List<uint>();
        /// <summary>
        /// Delta for engine ticks; 60tps
        /// </summary>
        private const float deltaTick = 1 / 60f;
        /// <summary>
        /// Delta for frames; varies
        /// </summary>
        private Stopwatch clockTick = Stopwatch.StartNew();

        public int NumProjectiles => ActiveProjectiles.Count;

        private Task projectileTask;

        public ProjectileManager()
        {
            I?.UnloadData();
            I = this;
            DamageHandler.Load();

            projectileTask = MyAPIGateway.Parallel.Start(UpdateProjectilesParallel);

            //MyAPIGateway.Utilities.CreateNotification("PSim: " + Math.Round(pSim, 2), 1001 / 60);
        }

        public void UnloadData()
        {
            if (!projectileTask.IsComplete)
            {
                HeartData.I.Log.Log("Waiting for projectileTask to end...");
                projectileTask.Wait();
            }

            I = null;
            DamageHandler.Unload();
        }

        private bool AllowProjectileRemoval = true;
        HashSet<BoundingSphere> allValidEntities = new HashSet<BoundingSphere>();

        public void UpdateAfterSimulation()
        {
            float pSim = 0;
            for (int i = 0; i < projectileSim.Count; i++)
                pSim += projectileSim.ElementAt(i);
            pSim /= projectileSim.Count == 0 ? 1 : projectileSim.Count;

            while (projectileSim.Count > 30)
                projectileSim.Dequeue();

            HeartData.I.ProjectileSimSpeed = pSim;

            try
            {
                if (HeartData.I.IsSuspended) return;

                allValidEntities.Clear();
                MyAPIGateway.Entities.GetEntities(null, (ent) =>
                {
                    if (ent is IMyCubeGrid || ent is IMyCharacter)
                        allValidEntities.Add(ent.WorldVolume);
                    return false;
                }
                );

                // Tick projectiles
                foreach (var projectile in ActiveProjectiles.ToHashSet()) // This can be modified by ModApi calls during run
                {
                    if (projectile.Value == null || projectile.Value.QueuedDispose)
                        QueuedCloseProjectiles.Add(projectile.Key);
                    projectile.Value.AVTickUpdate(deltaTick);
                }

                // Queued removal of projectiles
                foreach (var projectileId in QueuedCloseProjectiles)
                {
                    Projectile projectile = ActiveProjectiles[projectileId];
                    if (projectile == null) // Emergency cull null projectiles.
                    {
                        ActiveProjectiles.Remove(projectileId);
                        continue;
                    }

                    //MyAPIGateway.Utilities.ShowMessage("Heart", $"Closing projectile {projectile.Id}. Age: {projectile.Age} ");
                    if (MyAPIGateway.Session.IsServer)
                        QueueSync(projectile, 2);

                    if (!MyAPIGateway.Utilities.IsDedicated)
                        projectile.CloseDrawing();

                    ActiveProjectiles.Remove(projectileId);
                    ProjectilesWithHealth.Remove(projectile);
                    projectile.OnClose.Invoke(projectile);
                }
                QueuedCloseProjectiles.Clear();

                // Sync stuff
                UpdateSync();

                DamageHandler.Update();

                clockTick.Restart();

                ticksReady++;

                if (projectileTask.IsComplete)
                    projectileTask = MyAPIGateway.Parallel.Start(UpdateProjectilesParallel);
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(ProjectileManager));
            }
        }

        Queue<float> projectileSim = new Queue<float>();
        Stopwatch watch = new Stopwatch();

        int ticksReady = 0;
        /// <summary>
        /// Updates parallel at MAX 60tps, but can run at under that without lagging the game.
        /// </summary>
        public void UpdateProjectilesParallel()
        {
            I.projectileSim.Enqueue(16.6666667f / watch.ElapsedMilliseconds);
            watch.Restart();
            Projectile[] projectiles;
            BoundingSphere[] spheres;

            if (ticksReady <= 0)
                return;

            float delta = ticksReady / 60f;

            try
            {
                projectiles = ActiveProjectiles.Values.ToArray();
                spheres = allValidEntities.ToArray();

                MyAPIGateway.Parallel.ForEach(projectiles, (p) => p.AsyncTickUpdate(delta, spheres));

                //foreach (var projectile in projectiles) // This can be modified by ModApi calls during run
                //{
                //    if (HeartData.I.IsSuspended)
                //        return;
                //
                //    //projectile.UpdateBoundingBoxCheck(spheres);
                //    projectile.AsyncTickUpdate(delta, spheres);
                //}
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(ProjectileManager));
            }

            ticksReady = 0;
        }

        public void UpdatingStopped()
        {
            clockTick.Stop();
        }

        public void UpdateProjectileSync(n_SerializableProjectile projectile)
        {
            if (MyAPIGateway.Session.IsServer)
                return;

            if (IsIdAvailable(projectile.Id) && projectile.IsActive && projectile.DefinitionId.HasValue)
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
                else if (projectile.IsActive)
                    HeartData.I.Net.SendToServer(new n_ProjectileRequest(projectile.Id));
            }
        }

        public Projectile AddProjectile(int projectileDefinitionId, Vector3D position, Vector3D direction, IMyConveyorSorter sorterWep)
        {
            if (ActiveProjectiles.Count > MaxActiveProjectiles) return null;

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
            if (ActiveProjectiles.Count > MaxActiveProjectiles) return null;

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
            if (projectile == null || projectile.DefinitionId == -1 || projectile.Definition == null) return null; // Ensure that invalid projectiles don't get added

            if (ActiveProjectiles.Count > MaxActiveProjectiles) return null;

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
            if (ActiveProjectiles.Count > MaxActiveProjectiles) return null;

            if (!HitscanList.ContainsKey(firer))
            {
                Projectile p = AddProjectile(new Projectile(projectileDefinitionId, position, direction, firer));
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
            {
                foreach (var projectil in ProjectilesWithHealth.ToArray())
                    if (projectil != null && Vector3D.DistanceSquared(pos, projectil.Position) < rangeSq)
                        projectiles.Add(projectil);
            }
            else
            {
                foreach (var projectile in ActiveProjectiles.Values.ToArray())
                    if (projectile != null && Vector3D.DistanceSquared(pos, projectile.Position) < rangeSq)
                        projectiles.Add(projectile);
            }
        }
    }
}
