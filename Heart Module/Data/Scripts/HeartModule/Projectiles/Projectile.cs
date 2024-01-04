using Heart_Module.Data.Scripts.HeartModule.Debug;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    public partial class Projectile
    {
        #region Definition Values
        public uint Id { get; private set; }
        public readonly SerializableProjectileDefinition Definition;
        public readonly int DefinitionId;
        Dictionary<string, object> Overrides = new Dictionary<string, object>();
        public Vector3D InheritedVelocity;
        #endregion

        public Vector3D Position;
        public Vector3D Direction;
        public float Velocity;
        public float Acceleration;
        
        public Action<Projectile> Close = (p) => { };
        public long LastUpdate { get; private set; }

        public float DistanceTravelled { get; private set; } = 0;
        public float Age { get; private set; } = 0;
        public bool QueuedClose { get; private set; } = false;

        public Projectile() { }

        public Projectile(SerializableProjectile projectile)
        { 
            if (!ProjectileManager.I.IsIdAvailable(projectile.Id))
            {
                SoftHandle.RaiseSyncException("Unable to spawn projectile - duplicate Id!");
                ProjectileManager.I.GetProjectile(projectile.Id)?.SyncUpdate(projectile);
                return;
            }

            if (!ProjectileDefinitionManager.HasDefinition(projectile.DefinitionId))
            {
                SoftHandle.RaiseSyncException("Unable to spawn projectile - invalid DefinitionId!");
                return;
            }

            Id = projectile.Id;
            DefinitionId = projectile.DefinitionId;
            Definition = ProjectileDefinitionManager.GetDefinition(projectile.DefinitionId);
            SyncUpdate(projectile);
        }

        public void TickUpdate(float delta)
        {
            if ((Definition.PhysicalProjectile.MaxTrajectory != -1 && Definition.PhysicalProjectile.MaxTrajectory < DistanceTravelled) || (Definition.PhysicalProjectile.MaxLifetime != -1 && Definition.PhysicalProjectile.MaxLifetime < Age))
            {
                QueueClose();
                return;
            }

            Velocity += Acceleration * delta;
            Position += (InheritedVelocity + Direction * Velocity) * delta;
            Age += delta;
            DistanceTravelled += Velocity * delta;
        }

        public void DrawUpdate(float delta)
        {
            DebugDraw.AddPoint(Position + (InheritedVelocity + Direction * (Velocity + Acceleration * delta)) * delta, Color.Green, 0.000001f);
        }
        
        public void SyncUpdate(SerializableProjectile projectile)
        {
            if (DefinitionId != projectile.DefinitionId)
            {
                SoftHandle.RaiseSyncException("DefinitionId Mismatch!");
                return;
            }

            LastUpdate = projectile.Timestamp;
            float delta = (DateTime.Now.Ticks - LastUpdate) / (float) TimeSpan.TicksPerSecond;

            Direction = projectile.Direction;
            Position = projectile.Position;
            Velocity = projectile.Velocity;
            Acceleration = projectile.Acceleration;
            InheritedVelocity = projectile.InheritedVelocity;
            TickUpdate(delta);
        }

        public void QueueClose()
        {
            QueuedClose = true;
        }

        public void SetId(uint id)
        {
            if (Id == 0)
                Id = id;
        }
    }
}
