using Heart_Module.Data.Scripts.HeartModule.Debug;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
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

        public long Firer;
        public Vector3D Position;
        public Vector3D Direction;
        public float Velocity;
        public int RemainingImpacts;
        
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
            Firer = projectile.Firer;
            // TODO fill in from Definition

            SyncUpdate(projectile);
        }

        public Projectile(int DefinitionId)
        {
            if (!ProjectileDefinitionManager.HasDefinition(DefinitionId))
            {
                SoftHandle.RaiseSyncException("Unable to spawn projectile - invalid DefinitionId!");
                return;
            }

            this.DefinitionId = DefinitionId;
            Definition = ProjectileDefinitionManager.GetDefinition(DefinitionId);

            Velocity = Definition.PhysicalProjectile.Velocity;
            RemainingImpacts = Definition.Damage.MaxImpacts;
        }

        public void TickUpdate(float delta)
        {
            if ((Definition.PhysicalProjectile.MaxTrajectory != -1 && Definition.PhysicalProjectile.MaxTrajectory < DistanceTravelled) || (Definition.PhysicalProjectile.MaxLifetime != -1 && Definition.PhysicalProjectile.MaxLifetime < Age))
                QueueDispose();

            CheckHits(delta);

            Velocity += Definition.PhysicalProjectile.Acceleration * delta;
            Position += (InheritedVelocity + Direction * Velocity) * delta;
            Age += delta;
            DistanceTravelled += Velocity * delta;

            if (Velocity < 0)
            {
                Direction = -Direction;
                Velocity = -Velocity;
            }

            NextMoveStep = Position + (InheritedVelocity + Direction * (Velocity + Definition.PhysicalProjectile.Acceleration * delta)) * delta;
        }

        public void DrawUpdate(float delta)
        {
            DebugDraw.AddPoint(Position + (InheritedVelocity + Direction * (Velocity + Definition.PhysicalProjectile.Acceleration * delta)) * delta, Color.Green, 0.000001f);
        }
        
        public void CheckHits(float delta)
        {
            List<IHitInfo> intersects = new List<IHitInfo>();
            Vector3D endCast = NextMoveStep;
            MyAPIGateway.Physics.CastRay(Position, endCast, intersects);

            double len = ((Direction * Velocity + InheritedVelocity) * delta).Length();

            foreach (var hitInfo in intersects)
            {
                double dist = len * hitInfo.Fraction;
                ProjectileHit(hitInfo.HitEntity);
            }
        }

        public void ProjectileHit(IMyEntity impact)
        {
            if (impact.EntityId == Firer)
                return;

            if (impact is IMyCubeGrid)
                DamageHandler.QueueEvent(new DamageEvent(impact, DamageEvent.DamageEntType.Grid, this));
            else if (impact is IMyCharacter)
                DamageHandler.QueueEvent(new DamageEvent(impact, DamageEvent.DamageEntType.Character, this));

            RemainingImpacts -= 1;
            if (RemainingImpacts <= 0)
                QueueDispose();
        }

        public Vector3D NextMoveStep = Vector3D.Zero;

        public void SyncUpdate(SerializableProjectile projectile)
        {
            if (DefinitionId != projectile.DefinitionId)
            {
                SoftHandle.RaiseSyncException("DefinitionId Mismatch!");
                return;
            }

            QueuedDispose = !projectile.IsActive;

            LastUpdate = projectile.Timestamp;
            float delta = (DateTime.Now.Ticks - LastUpdate) / (float) TimeSpan.TicksPerSecond;

            Direction = projectile.Direction;
            Position = projectile.Position;
            Velocity = projectile.Velocity;
            InheritedVelocity = projectile.InheritedVelocity;
            RemainingImpacts = projectile.RemainingImpacts;
            TickUpdate(delta);
        }

        public SerializableProjectile AsSerializable()
        {
            return new SerializableProjectile()
            {
                IsActive = !QueuedDispose,
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
            if (MyAPIGateway.Session.IsServer)
                QueuedDispose = true;
        }

        public void SetId(uint id)
        {
            if (Id == 0)
                Id = id;
        }
    }
}
