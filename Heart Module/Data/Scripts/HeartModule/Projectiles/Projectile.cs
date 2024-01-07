using Heart_Module.Data.Scripts.HeartModule.Debug;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
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

        public long Firer = -1;
        public Vector3D Position = Vector3D.Zero;
        public Vector3D Direction = Vector3D.Up;
        public float Velocity = 0;
        public int RemainingImpacts = 0;

        public Action<Projectile> OnClose = (p) => { };
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

            if (!projectile.DefinitionId.HasValue || !ProjectileDefinitionManager.HasDefinition(projectile.DefinitionId.Value))
            {
                SoftHandle.RaiseSyncException("Unable to spawn projectile - invalid DefinitionId!");
                DefinitionId = -1;
                return;
            }

            Id = projectile.Id;
            DefinitionId = projectile.DefinitionId.Value;
            Definition = ProjectileDefinitionManager.GetDefinition(projectile.DefinitionId.Value);
            Firer = projectile.Firer.GetValueOrDefault(0);
            // TODO fill in from Definition

            SyncUpdate(projectile);
        }

        public Projectile(int DefinitionId, Vector3D Position, Vector3D Direction, IMyCubeBlock block) : this(DefinitionId, Position, Direction, block.EntityId, block.CubeGrid?.LinearVelocity ?? Vector3D.Zero)
        {
            
        }

        public Projectile(int DefinitionId, Vector3D Position, Vector3D Direction, long firer = 0, Vector3D InitialVelocity = new Vector3D())
        {
            if (!ProjectileDefinitionManager.HasDefinition(DefinitionId))
            {
                SoftHandle.RaiseSyncException("Unable to spawn projectile - invalid DefinitionId!");
                return;
            }

            this.DefinitionId = DefinitionId;
            Definition = ProjectileDefinitionManager.GetDefinition(DefinitionId);

            this.Position = Position;
            this.Direction = Direction;
            Velocity = Definition.PhysicalProjectile.Velocity;
            this.Firer = firer;
            this.InheritedVelocity = InitialVelocity;

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

        public void CheckHits(float delta)
        {
            List<IHitInfo> intersects = new List<IHitInfo>();
            Vector3D endCast = NextMoveStep;
            MyAPIGateway.Physics.CastRay(Position, endCast, intersects);

            double len = ((Direction * Velocity + InheritedVelocity) * delta).Length();

            foreach (var hitInfo in intersects)
            {
                if (QueuedDispose)
                    break;
                double dist = len * hitInfo.Fraction;
                ProjectileHit(hitInfo.HitEntity, hitInfo.Position);
            }
        }

        public void ProjectileHit(IMyEntity impact, Vector3D impactPosition)
        {
            if (impact.EntityId == Firer)
                return;

            if (impact is IMyCubeGrid)
                DamageHandler.QueueEvent(new DamageEvent(impact, DamageEvent.DamageEntType.Grid, this));
            else if (impact is IMyCharacter)
                DamageHandler.QueueEvent(new DamageEvent(impact, DamageEvent.DamageEntType.Character, this));

            DrawImpactParticle(impactPosition);

            RemainingImpacts -= 1;
            if (RemainingImpacts <= 0)
                QueueDispose();
        }

        public Vector3D NextMoveStep = Vector3D.Zero;

        public void SyncUpdate(SerializableProjectile projectile)
        {
            QueuedDispose = !projectile.IsActive;

            LastUpdate = projectile.Timestamp;
            float delta = (DateTime.Now.Ticks - LastUpdate) / (float)TimeSpan.TicksPerSecond;

            // The following values may be null to save network load
            if (projectile.Direction.HasValue)
                Direction = projectile.Direction.Value;
            if (projectile.Position.HasValue)
                Position = projectile.Position.Value;
            if (projectile.Velocity.HasValue)
                Velocity = projectile.Velocity.Value;
            if (projectile.InheritedVelocity.HasValue)
                InheritedVelocity = projectile.InheritedVelocity.Value;
            if (projectile.RemainingImpacts.HasValue)
                RemainingImpacts = projectile.RemainingImpacts.Value;

            TickUpdate(delta);
        }

        /// <summary>
        /// Returns the projectile as a network-ready projectile info class. 0 = max detail, 2+ = min detail
        /// </summary>
        /// <param name="DetailLevel"></param>
        /// <returns></returns>
        public SerializableProjectile AsSerializable(int DetailLevel = 1)
        {
            SerializableProjectile projectile = new SerializableProjectile()
            {
                IsActive = !QueuedDispose,
                Id = Id,
                Timestamp = DateTime.Now.Ticks,
            };

            switch (DetailLevel)
            {
                case 0:
                    projectile.DefinitionId = DefinitionId;
                    projectile.Position = Position;
                    projectile.Direction = Direction;
                    projectile.InheritedVelocity = InheritedVelocity;
                    projectile.Velocity = Velocity;
                    break;
                case 1:
                    projectile.Position = Position;
                    if (Definition.Guidance.Length > 0)
                        projectile.Direction = Direction;
                    if (Definition.PhysicalProjectile.Acceleration > 0)
                        projectile.Velocity = Velocity;
                    break;
            }

            return projectile;
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
