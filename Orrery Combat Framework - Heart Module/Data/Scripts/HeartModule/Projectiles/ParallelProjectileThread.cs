using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using ParallelTasks;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    internal class ParallelProjectileThread
    {
        #region variables

        Task thisTask;
        /// <summary>
        /// Thread-safe buffer array for active projectiles; cannot be written to while the thread is active.
        /// </summary>
        Projectile[] ActiveProjectiles = new Projectile[0];
        /// <summary>
        /// Thread safe buffer list for projectiles to close.
        /// </summary>
        List<Projectile> ProjectilesToClose = new List<Projectile>();

        public float DeltaTick = 0;

        #endregion

        public ParallelProjectileThread()
        {
            thisTask = MyAPIGateway.Parallel.StartBackground(DoWork);
            HeartLog.Log("Started ParallelProjectileThread!");
        }

        #region methods

        public void Update()
        {
            DeltaTick += ProjectileManager.DeltaTick;

            MyAPIGateway.Utilities.ShowNotification("PPT Sim: " + Math.Round(1/60d/DeltaTick, 2), 1000/60);

            if (thisTask.IsComplete)
            {
                // Update thread-safe buffer lists
                ActiveProjectiles = ProjectileManager.I.ActiveProjectiles.Values.ToArray();
                ProjectileManager.I.QueuedCloseProjectiles.AddRange(ProjectilesToClose);
                ProjectilesToClose.Clear();
                thisTask = MyAPIGateway.Parallel.StartBackground(DoWork);
            }
        }

        public void Close()
        {
            HeartLog.Log("-------------------------------------------");
            HeartLog.Log("    Closing ParallelProjectileThread...");
            if (!thisTask.IsComplete)
                thisTask.Wait(true);
            HeartLog.Log("    Closed ParallelProjectileThread.");
            HeartLog.Log("-------------------------------------------");
        }

        void DoWork()
        {
            MyAPIGateway.Parallel.ForEach(ActiveProjectiles, UpdateSingleProjectile);
            DeltaTick = 0;
        }

        void UpdateSingleProjectile(Projectile projectile)
        {
            projectile.TickUpdate(DeltaTick);

            if (projectile.QueuedDispose)
                ProjectilesToClose.Add(projectile);
        }

        #endregion
    }
}
