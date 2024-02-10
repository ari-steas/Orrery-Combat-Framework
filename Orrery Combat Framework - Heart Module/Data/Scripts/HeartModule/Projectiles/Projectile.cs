using Heart_Module.Data.Scripts.HeartModule.Debug;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.GuidanceHelpers;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    public partial class Projectile // TODO: Make physical, beams, and guided projectiles inheritors, and make a projectile struct class
    {
        #region Definition Values
        public uint Id { get; private set; }
        public readonly ProjectileDefinitionBase Definition;
        public readonly int DefinitionId;
        Dictionary<string, object> Overrides = new Dictionary<string, object>();
        public Vector3D InheritedVelocity;
        #endregion

        public ProjectileGuidance Guidance;
        public bool IsHitscan { get; private set; } = false;
        public long Firer = -1;
        public Vector3D Position = Vector3D.Zero;
        public Vector3D Direction = Vector3D.Up;
        public float Velocity = 0;
        public int RemainingImpacts = 0;

        public Action<Projectile> OnClose = (p) =>
        {
            p.Definition.LiveMethods.OnEndOfLife?.Invoke(p.Id);
        };
        public long LastUpdate { get; private set; }

        public float DistanceTravelled { get; private set; } = 0;
        public float Age { get; private set; } = 0;
        public bool QueuedDispose { get; private set; } = false;

        private float _health = 0;
        public float Health
        {
            get
            {
                return _health;
            }
            set
            {
                _health = value;
                if (_health <= 0)
                    QueueDispose();
            }
        }

        public Projectile() { }

        public Projectile(n_SerializableProjectile projectile)
        {
            if (!ProjectileManager.I.IsIdAvailable(projectile.Id))
            {
                SoftHandle.RaiseSyncException("Unable to spawn projectile - duplicate Id!");
                //ProjectileManager.I.GetProjectile(projectile.Id)?.UpdateFromSerializable(projectile);
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
            IsHitscan = Definition.PhysicalProjectile.IsHitscan;
            Health = Definition.PhysicalProjectile.Health;
            if (!IsHitscan)
                Velocity = Definition.PhysicalProjectile.Velocity;
            else
                Definition.PhysicalProjectile.MaxLifetime = 1 / 60f;

            if (Definition.Guidance.Length > 0)
                Guidance = new ProjectileGuidance(this);

            Definition.LiveMethods.OnSpawn?.Invoke(Id, (MyEntity)MyAPIGateway.Entities.GetEntityById(Firer));
            UpdateFromSerializable(projectile);
        }

        /// <summary>
        /// Spawn a projectile with a grid as a reference.
        /// </summary>
        /// <param name="DefinitionId"></param>
        /// <param name="Position"></param>
        /// <param name="Direction"></param>
        /// <param name="block"></param>
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
            this.Firer = firer;

            IsHitscan = Definition.PhysicalProjectile.IsHitscan;

            // Apply velocity variance
            if (!IsHitscan)
            {
                // Randomly adjust velocity within the variance range
                float variance = (float)(new Random().NextDouble() * 2 - 1) * Definition.PhysicalProjectile.VelocityVariance;
                Velocity = Definition.PhysicalProjectile.Velocity + variance;
                this.InheritedVelocity = InitialVelocity;
            }
            else
            {
                Definition.PhysicalProjectile.MaxLifetime = 1 / 60f;
            }

            RemainingImpacts = Definition.Damage.MaxImpacts;
            Health = Definition.PhysicalProjectile.Health;

            if (Definition.Guidance.Length > 0)
                Guidance = new ProjectileGuidance(this);

            Definition.LiveMethods.OnSpawn?.Invoke(Id, (MyEntity)MyAPIGateway.Entities.GetEntityById(Firer));
        }

        public void AVTickUpdate(float delta)
        {
            if (QueuedDispose)
                return;

            if (MyAPIGateway.Session.IsServer)
                UpdateAudio();
            else
                DrawUpdate();
        }

        public void AsyncTickUpdate(float delta, BoundingSphere[] spheres)
        {
            if (QueuedDispose || HeartData.I.IsSuspended)
                return;

            if ((Definition.PhysicalProjectile.MaxTrajectory != -1 && Definition.PhysicalProjectile.MaxTrajectory < DistanceTravelled) || (Definition.PhysicalProjectile.MaxLifetime != -1 && Definition.PhysicalProjectile.MaxLifetime < Age))
                QueueDispose();

            if (Guidance == null && Definition.Guidance.Length > 0)
                Guidance = new ProjectileGuidance(this);

            UpdateBoundingBoxCheck(spheres);

            Age += delta;
            if (!IsHitscan)
            {
                Guidance?.RunGuidance(delta);

                CheckHits();
                Velocity += Definition.PhysicalProjectile.Acceleration * delta;
                Position += (InheritedVelocity + Direction * Velocity) * delta;
                DistanceTravelled += Velocity * delta;

                if (Velocity < 0)
                {
                    Direction = -Direction;
                    Velocity = -Velocity;
                }

                NextMoveStep = Position + (InheritedVelocity + Direction * (Velocity + Definition.PhysicalProjectile.Acceleration * delta)) * delta;
            }
            else // Beams are really special, and need their own handling.
            {
                if (!MyAPIGateway.Session.IsServer)
                    RemainingImpacts = Definition.Damage.MaxImpacts;
                NextMoveStep = Position + Direction * Definition.PhysicalProjectile.MaxTrajectory;

                if (RemainingImpacts > 0)
                {
                    MaxBeamLength = CheckHits(); // Set visual beam length
                    if (MaxBeamLength == -1)
                        MaxBeamLength = Definition.PhysicalProjectile.MaxTrajectory;
                }
            }
        }

        public Vector3D NextMoveStep = Vector3D.Zero;

        public void UpdateFromSerializable(n_SerializableProjectile projectile)
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
            if (projectile.Firer.HasValue)
                Firer = projectile.Firer.Value;
            AsyncTickUpdate(delta, new BoundingSphere[0]);
        }

        public void UpdateHitscan(Vector3D newPosition, Vector3D newDirection)
        {
            Age = 0;
            Position = newPosition;
            Direction = newDirection;
            RemainingImpacts = Definition.Damage.MaxImpacts;
        }

        /// <summary>
        /// Returns the projectile as a network-ready projectile info class. 0 = max detail, 2+ = min detail
        /// </summary>
        /// <param name="DetailLevel"></param>
        /// <returns></returns>
        public n_SerializableProjectile AsSerializable(int DetailLevel = 1)
        {
            n_SerializableProjectile projectile = new n_SerializableProjectile()
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
                    projectile.Firer = Firer;
                    //projectile.Velocity = Velocity;
                    break;
                case 1:
                    projectile.Position = Position;
                    if (IsHitscan || Definition.Guidance.Length > 0)
                        projectile.Direction = Direction;
                    if (!IsHitscan && Definition.PhysicalProjectile.Acceleration > 0)
                        projectile.Velocity = Velocity;
                    break;
            }

            return projectile;
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
