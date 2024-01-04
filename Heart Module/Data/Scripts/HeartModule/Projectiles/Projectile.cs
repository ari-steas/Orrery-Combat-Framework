using Heart_Module.Data.Scripts.HeartModule.Debug;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
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

        public MyEntity Firer { get; private set; }
        public Vector3D Position;
        public Vector3D Direction;
        public float Velocity;
        public float Acceleration;
        public int RemainingHits;
        
        public Action<Projectile> Close = (p) => { };
        public long LastUpdate { get; private set; }

        public float DistanceTravelled { get; private set; } = 0;
        public float Age { get; private set; } = 0;
        public bool QueuedDispose { get; private set; } = false;

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

            // TODO fill in from Definition
            Acceleration = Definition.PhysicalProjectile.Acceleration;

            SyncUpdate(projectile);
        }

        public void TickUpdate(float delta)
        {
            if ((Definition.PhysicalProjectile.MaxTrajectory != -1 && Definition.PhysicalProjectile.MaxTrajectory < DistanceTravelled) || (Definition.PhysicalProjectile.MaxLifetime != -1 && Definition.PhysicalProjectile.MaxLifetime < Age))
            {
                QueueDispose();
                return;
            }
            CheckHits(delta);

            Velocity += Acceleration * delta;
            Position += (InheritedVelocity + Direction * Velocity) * delta;
            Age += delta;
            DistanceTravelled += Velocity * delta;
        }

        public void DrawUpdate(float delta)
        {
            DebugDraw.AddPoint(Position + (InheritedVelocity + Direction * (Velocity + Acceleration * delta)) * delta, Color.Green, 0.000001f);
        }
        
        public void CheckHits(float delta)
        {
            List<IHitInfo> intersects = new List<IHitInfo>();
            MyAPIGateway.Physics.CastRay(Position, Position + (Direction * Velocity + InheritedVelocity) * delta, intersects);

            double len = ((Direction * Velocity + InheritedVelocity) * delta).Length();

            foreach (var hitInfo in intersects)
            {
                double dist = len * hitInfo.Fraction;
                ProjectileHit(hitInfo.HitEntity, hitInfo.Fraction);
                MyAPIGateway.Utilities.ShowMessage("Heart", $"HitEnt type: {hitInfo.HitEntity.GetType().Name} @ {Math.Round(dist, 1)}m");
            }
        }

        public void ProjectileHit(IMyEntity impact, double distance)
        {

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
            InheritedVelocity = projectile.InheritedVelocity;
            TickUpdate(delta);
        }

        public SerializableProjectile AsSerializable()
        {
            return new SerializableProjectile()
            {
                Id = Id,
                DefinitionId = DefinitionId,
                Position = Position,
                Direction = Direction,
                InheritedVelocity = InheritedVelocity,
                Velocity = Velocity,
                Timestamp = DateTime.Now.Ticks,
            };
        }

        public void QueueDispose()
        {
            QueuedDispose = true;
        }

        public void SetId(uint id)
        {
            if (Id == 0)
                Id = id;
        }
    }
}
