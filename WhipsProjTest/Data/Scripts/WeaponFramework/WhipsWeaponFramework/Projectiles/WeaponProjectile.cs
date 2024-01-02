using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using Sandbox.Game.Weapons;
using VRage.Game.ModAPI;
using VRageMath;
using Sandbox.Game;
using VRage.Game.Entity;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI.Interfaces;
using Sandbox.Definitions;
using VRage;
using Whiplash.WeaponFramework;
using VRage.Collections;
using VRage.Voxels;
using Whiplash.Utils;

namespace Whiplash.WeaponProjectiles
{
    public class WeaponProjectile
    {
        public bool Killed { get; private set; } = false;

        const float Tick = 1f / 60f;
        public const int RaycastDelayTicks = 6;

        Vector3D _origin;
        Vector3D _lastCheckedPosition;
        Vector3D _lastPosition;
        Vector3D _position;
        Vector3D _velocity;
        Vector3D _lastVelocity;
        Vector3D _direction;
        Vector3D _hitPosition;
        readonly float _explosionDamage;
        readonly float _shieldDamageMult;
        readonly float _explosionRadius;
        readonly float _projectileSpeed;
        readonly float _maxTrajectory;
        readonly float _minimumArmDistance = 0f;
        readonly float _penetrationRange;
        readonly float _penetrationDamage;
        readonly float _deviationAngle;
        int _checkIntersectionIndex = 5;
        bool _positionChecked = false;
        readonly bool _shouldExplode;
        readonly bool _shouldPenetrate;
        readonly bool _drawTrail;
        readonly bool _drawTracer;
        bool _targetHit = false;
        bool _penetratedObjectsSorted = false;
        bool _penetratedObjectsDamaged = false;
        bool _shouldProximityDetonate = false;
        readonly float _proximityDetonationRange;
        readonly float _proximityDetonationArmingRangeSq;
        readonly long _gunEntityID;
        Vector4 _trailColor;
        readonly MyStringId _material = MyStringId.GetOrCompute("WeaponLaser");
        readonly MyStringId _bulletMaterial = MyStringId.GetOrCompute("ProjectileTrailLine");
        readonly Vector3 _tracerColor;
        readonly float _tracerScale;
        readonly List<PenetratedEntityContainer> _objectsToPenetrate = new List<PenetratedEntityContainer>();
        readonly List<MyLineSegmentOverlapResult<MyEntity>> _overlappingEntities = new List<MyLineSegmentOverlapResult<MyEntity>>();
        readonly List<Vector3I> _hitPositions = new List<Vector3I>();
        readonly List<MyLineSegmentOverlapResult<MyVoxelBase>> _voxelOverlap = new List<MyLineSegmentOverlapResult<MyVoxelBase>>();
        readonly List<MyEntity> _entitiesAlreadyImpulsed = new List<MyEntity>();
        Vector3D? _cachedSurfacePoint = null;
        bool _hitMaxTrajectory = false;
        WeaponConfig _config;
        readonly List<Vector3I> _voxelTestPoints = new List<Vector3I>();
        float _hitImpulse;
        public Vector3D DeviatedDirection { get; private set; }

        public struct PenetratedEntityContainer
        {
            public IMyDestroyableObject PenetratedEntity;
            public Vector3D WorldPosition;
            public MyEntity BaseEntity;
        }

        public WeaponProjectile(WeaponFireData fireData, WeaponConfig config)
        {
            _config = config;

            // Weapon data
            _tracerColor = config.TracerColor;
            _trailColor = new Vector4(_tracerColor, 1f);
            _tracerScale = _config.TracerScale;
            _maxTrajectory = _config.MaxRange;
            _projectileSpeed = _config.MuzzleVelocity;
            _deviationAngle = MathHelper.ToRadians(_config.DeviationAngleDeg);
            _gunEntityID = fireData.ShooterID;
            _drawTrail = _config.ShouldDrawProjectileTrails;
            _explosionDamage = _config.ContactExplosionDamage;
            _explosionRadius = _config.ContactExplosionRadius;
            _penetrationDamage = _config.PenetrationDamage;
            _penetrationRange = _config.PenetrationRange;
            _shouldExplode = _config.ExplodeOnContact;
            _shouldPenetrate = _config.PenetrateOnContact;
            _shieldDamageMult = _config.ShieldDamageMultiplier;
            _hitImpulse = _config.HitImpulse;
            _shouldProximityDetonate = _config.ShouldProximityDetonate;
            _proximityDetonationRange = _config.ProximityDetonationRange;
            _proximityDetonationArmingRangeSq = _config.ProximityDetonationArmingRange * _config.ProximityDetonationArmingRange;

            // Fire data
            var temp = fireData.Direction;
            _direction = Vector3D.IsUnit(ref temp) ? temp : Vector3D.Normalize(temp);
            _direction = GetDeviatedVector(_direction, _deviationAngle);
            DeviatedDirection = _direction;
            _origin = fireData.Origin;
            _lastPosition = _origin;
            _lastCheckedPosition = _origin;
            _velocity = fireData.ShooterVelocity + _direction * _projectileSpeed;
            _lastVelocity = _velocity;

            double initialStepTime = RaycastDelayTicks * Tick;
            Vector3D acceleration = GetGravity(_origin, _config.NaturalGravityMultiplier, _config.ArtificialGravityMultiplier);
            _position = _origin + _velocity * initialStepTime + 0.5 * acceleration * initialStepTime * initialStepTime; // Step forward in time
            _velocity += acceleration * initialStepTime;
        }

        public static Vector3D GetGravity(Vector3D point, double naturalGravityMult, double artificialGravityMult)
        {
            float naturalGravityInterference = 0f;
            Vector3D naturalGravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(point, out naturalGravityInterference);
            Vector3D artificialGravity = MyAPIGateway.Physics.CalculateArtificialGravityAt(point, naturalGravityInterference);
            return naturalGravity * naturalGravityMult + artificialGravity * artificialGravityMult;
        }

        public static Vector3D GetDeviatedVector(Vector3D direction, float deviationAngle)
        {
            float elevationAngle = MyUtils.GetRandomFloat(-deviationAngle, deviationAngle);
            float rotationAngle = MyUtils.GetRandomFloat(0f, MathHelper.TwoPi);
            Vector3 normal = -new Vector3(MyMath.FastSin(elevationAngle) * MyMath.FastCos(rotationAngle), MyMath.FastSin(elevationAngle) * MyMath.FastSin(rotationAngle), MyMath.FastCos(elevationAngle));
            var mat = MatrixD.CreateFromDir(direction);
            return Vector3D.TransformNormal(normal, mat);
        }

        public void Update()
        {
            if (_targetHit)
            {
                Kill();
                return;
            }

            // Calc acceleration
            Vector3D acceleration = GetGravity(_position, _config.NaturalGravityMultiplier, _config.ArtificialGravityMultiplier);

            // Update position
            _lastPosition = _position;
            _position += _velocity * Tick + 0.5 * acceleration * Tick * Tick;
            var toOrigin = _position - _origin;

            // Update velocity
            _velocity += acceleration * Tick;

            // Update direction if velocity has changed
            if (!_velocity.Equals(_lastVelocity, 1e-3))
                _direction = Vector3D.Normalize(_velocity);

            _lastVelocity = _velocity;

            if (_hitMaxTrajectory)
            {
                _targetHit = true;
                _hitPosition = _position;
                Kill();
                if (_shouldExplode)
                    CreateExplosion(_position, _direction, _explosionRadius, _explosionDamage);
                return;
            }

            if (toOrigin.LengthSquared() > _maxTrajectory * _maxTrajectory)
            {
                WeaponProjectileShared.DamageLog.WriteLine("------------------------------------------------", writeToGameLog: false);
                WeaponProjectileShared.DamageLog.WriteLine("Max range hit", writeToGameLog: false);
                _hitMaxTrajectory = true;
                _positionChecked = false;
            }

            _checkIntersectionIndex = ++_checkIntersectionIndex % RaycastDelayTicks;
            if (_checkIntersectionIndex != 0 && _positionChecked)
            {
                return;
            }

            var to = _position;
            var from = _lastCheckedPosition;
            _positionChecked = true;
            _lastCheckedPosition = _position;

            IHitInfo hitInfo;
            bool hit  = MyAPIGateway.Physics.CastRay(from, to, out hitInfo, 15);

            if (hit)
            {
                _hitPosition = hitInfo.Position + -0.5 * _direction;
            }

            // DS - Shield hit intersection and damage
            if (WeaponSession.Instance.ShieldApiLoaded)
            {
                var checkLine = new LineD(from, to);
                var shieldInfo = WeaponSession.Instance.ShieldApi.ClosestShieldInLine(checkLine, true);
                if (hit && shieldInfo.Item1.HasValue && hitInfo.Fraction * checkLine.Length > shieldInfo.Item1.Value || shieldInfo.Item1.HasValue)
                {
                    _hitPosition = from + (checkLine.Direction * shieldInfo.Item1.Value) + -0.5 * _direction;

                    float damage = _shieldDamageMult * (_explosionDamage + _penetrationDamage);
                    float currentCharge = WeaponSession.Instance.ShieldApi.GetCharge(shieldInfo.Item2);
                    float hpToCharge = WeaponSession.Instance.ShieldApi.HpToChargeRatio(shieldInfo.Item2);
                    float newCharge = currentCharge - (damage / hpToCharge);

                    // Deal damage
                    WeaponSession.Instance.ShieldApi.SetCharge(shieldInfo.Item2, newCharge);

                    // Draw impact
                    WeaponSession.Instance.ShieldApi.PointAttackShield(
                            shieldInfo.Item2,
                            _hitPosition,
                            _gunEntityID,
                            0f,
                            false,
                            true,
                            false);

                    _targetHit = true;
                    Kill();
                    return;
                }
            }

            // Check for proximity detonation
            if (_shouldProximityDetonate && !hit && !_targetHit && toOrigin.LengthSquared() > _proximityDetonationArmingRangeSq)
            {
                Vector3D hitPos;
                if (WeaponProjectileShared.ProximityDetonate(_gunEntityID, _proximityDetonationRange, _proximityDetonationArmingRangeSq, _origin, from, to, out hitPos))
                {
                    WeaponProjectileShared.DamageLog.WriteLine("------------------------------------------------", writeToGameLog: false);
                    WeaponProjectileShared.DamageLog.WriteLine("Proximity detonation", writeToGameLog: false);
                    _hitPosition = hitPos;

                    if (_shouldExplode)
                        CreateExplosion(_hitPosition, _direction, _explosionRadius, _explosionDamage);

                    _targetHit = true;
                    Kill();
                    return;
                }
            }

            // Check for grid/player intersections
            if (hit)
            {
                WeaponProjectileShared.DamageLog.WriteLine("------------------------------------------------", writeToGameLog: false);
                WeaponProjectileShared.DamageLog.WriteLine("Raycast hit", writeToGameLog: false);
                if (WeaponProjectileShared.ShouldRegisterHit(hitInfo.HitEntity, _gunEntityID))
                {
                    if ((_hitPosition - _origin).LengthSquared() > _minimumArmDistance * _minimumArmDistance) //only explode if beyond arm distance
                    {
                        if (_shouldExplode)
                            CreateExplosion(_hitPosition, _direction, _explosionRadius, _explosionDamage, hitInfo.HitEntity);

                        if (_shouldPenetrate)
                            GetObjectsToPenetrate(_hitPosition, _hitPosition + _direction * _penetrationRange);

                        _targetHit = true;
                        Kill();
                    }
                    else
                    {
                        _targetHit = true;
                        _hitPosition = _position;
                        Kill();
                    }
                    return;
                }
                else
                {
                    WeaponProjectileShared.DamageLog.WriteLine("  Raycast hit ignored", writeToGameLog: false);
                }
            }
            // implied else

            // Check for voxel intersections
            var line = new LineD(from, to);
            MyGamePruningStructure.GetVoxelMapsOverlappingRay(ref line, _voxelOverlap);
            foreach (var result in _voxelOverlap)
            {
                MyPlanet planet = result.Element as MyPlanet;
                MyVoxelMap voxelMap = result.Element as MyVoxelMap;

                IMyEntity hitEntity = null;
                Vector3D? hitPos = null;
                if (planet != null)
                {
                    planet.GetIntersectionWithLine(ref line, out hitPos);
                    hitEntity = planet;
                }
                if (voxelMap != null)
                {
                    voxelMap.GetIntersectionWithLine(ref line, out hitPos);
                    hitEntity = voxelMap;
                }

                if (hitPos.HasValue)
                {
                    WeaponProjectileShared.DamageLog.WriteLine("------------------------------------------------", writeToGameLog: false);
                    WeaponProjectileShared.DamageLog.WriteLine("Hit voxel", writeToGameLog: false);
                    _hitPosition = hitPos.Value;
                    if (_shouldExplode)
                        CreateExplosion(_hitPosition, _direction, _explosionRadius, _explosionDamage, hitEntity);
                    _targetHit = true;
                    Kill();
                    return;
                }
            }
        }

        void CreateExplosion(Vector3D position, Vector3D direction, float radius, float damage, IMyEntity hitEntity = null, float scale = 1f)
        {
            var m_explosionFullSphere = new BoundingSphere((Vector3)position, radius);

            MyExplosionInfo info = new MyExplosionInfo()
            {
                PlayerDamage = damage,
                Damage = damage,
                ExplosionType = MyExplosionTypeEnum.WARHEAD_EXPLOSION_02,
                ExplosionSphere = m_explosionFullSphere,
                LifespanMiliseconds = MyExplosionsConstants.EXPLOSION_LIFESPAN,
                ParticleScale = _config.DrawImpactSprite ? 0f : scale,
                Direction = (Vector3)direction,
                VoxelExplosionCenter = m_explosionFullSphere.Center,
                ExplosionFlags = MyExplosionFlags.AFFECT_VOXELS | MyExplosionFlags.CREATE_PARTICLE_EFFECT | MyExplosionFlags.CREATE_DECALS | MyExplosionFlags.APPLY_FORCE_AND_DAMAGE | MyExplosionFlags.APPLY_DEFORMATION,
                VoxelCutoutScale = 0.25f,
                PlaySound = true,
                ApplyForceAndDamage = true,
                KeepAffectedBlocks = true,
                HitEntity = hitEntity == null ? null : (MyEntity)hitEntity,
                ObjectsRemoveDelayInMiliseconds = 40
            };

            MyExplosions.AddExplosion(ref info);
        }

        #region Penetration
        void GetObjectsToPenetrate(Vector3D start, Vector3D end)
        {
            WeaponProjectileShared.DamageLog.WriteLine("Getting railgun penetrated objects", writeToGameLog: false);

            _objectsToPenetrate.Clear();
            var testRay = new LineD(start, end);
            MyGamePruningStructure.GetAllEntitiesInRay(ref testRay, _overlappingEntities);

            foreach (var hit in _overlappingEntities)
            {
                var destroyable = hit.Element as IMyDestroyableObject;
                if (destroyable != null)
                {
                    WeaponProjectileShared.DamageLog.WriteLine("  Destroyable object found", writeToGameLog: false);

                    var penetratedEntity = new PenetratedEntityContainer()
                    {
                        PenetratedEntity = destroyable,
                        WorldPosition = hit.Element.PositionComp.GetPosition(),
                        BaseEntity = hit.Element,
                    };

                    _objectsToPenetrate.Add(penetratedEntity);
                    continue;
                }

                var grid = hit.Element as IMyCubeGrid;
                if (grid != null)
                {
                    WeaponProjectileShared.DamageLog.WriteLine("  Cube grid found", writeToGameLog: false);
                    IMySlimBlock slimBlock;

                    grid.RayCastCells(start, end, _hitPositions);

                    if (_hitPositions.Count == 0)
                    {
                        WeaponProjectileShared.DamageLog.WriteLine("    No slim block found in intersection", writeToGameLog: false);
                        continue;
                    }

                    WeaponProjectileShared.DamageLog.WriteLine($"    {_hitPositions.Count} slim blocks in intersection", writeToGameLog: false);

                    foreach (var position in _hitPositions)
                    {
                        slimBlock = grid.GetCubeBlock(position);
                        if (slimBlock == null)
                            continue;

                        var penetratedEntity = new PenetratedEntityContainer()
                        {
                            PenetratedEntity = slimBlock,
                            WorldPosition = Vector3D.Transform(position * grid.GridSize, grid.WorldMatrix),
                            BaseEntity = hit.Element,
                        };
                        _objectsToPenetrate.Add(penetratedEntity);
                    }
                    continue;
                }
            }
        }

        void SortObjectsToPenetrate(Vector3D start)
        {
            WeaponProjectileShared.DamageLog.WriteLine("Sorting railgun penetrated objects", writeToGameLog: false);
            // Sort objects to penetrate by distance, closest first
            _objectsToPenetrate.Sort((x, y) => Vector3D.DistanceSquared(start, x.WorldPosition).CompareTo(Vector3D.DistanceSquared(start, y.WorldPosition)));
        }

        void DamageObjectsToPenetrate(float damage)
        {
            WeaponProjectileShared.DamageLog.WriteLine("Railgun penetration", writeToGameLog: false);
            WeaponProjectileShared.DamageLog.WriteLine($"  Railgun initial pooled damage: {damage}", writeToGameLog: false);

            for (int i = 0; i < _objectsToPenetrate.Count; ++i)
            {
                var item = _objectsToPenetrate[i];

                // Post penetration explosion
                if (damage <= 0)
                {
                    WeaponProjectileShared.DamageLog.WriteLine("  Pooled damage expended", writeToGameLog: false);
                    if (_config.ExplodePostPenetration)
                    {
                        Vector3D explosionPosition = i > 0 ? _objectsToPenetrate[i - 1].WorldPosition : _hitPosition;
                        IMySlimBlock slim = item.PenetratedEntity as IMySlimBlock;
                        IMyEntity hitEnt = null;
                        if (slim != null)
                        {
                            hitEnt = slim.CubeGrid;
                        }
                        CreateExplosion(explosionPosition, _direction, _config.PenetrationExplosionRadius, _config.PenetrationExplosionDamage, hitEnt);
                    }
                    break;
                }

                // Hit impulse
                if (!_entitiesAlreadyImpulsed.Contains(item.BaseEntity))
                {
                    _entitiesAlreadyImpulsed.Add(item.BaseEntity);

                    if (item.BaseEntity.Physics != null)
                        item.BaseEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, (Vector3)_direction * _hitImpulse, item.WorldPosition, null);
                }

                var destroyableObject = item.PenetratedEntity;
                var slimBlock = destroyableObject as IMySlimBlock;
                if (slimBlock != null)
                {
                    WeaponProjectileShared.DamageLog.WriteLine("  Slim block found", writeToGameLog: false);

                    var blockIntegrity = slimBlock.Integrity;
                    var cube = slimBlock.FatBlock;
                    WeaponProjectileShared.DamageLog.WriteLine($"    Pooled damage before: {damage}", writeToGameLog: false);
                    WeaponProjectileShared.DamageLog.WriteLine($"    Block integrity before: {blockIntegrity}", writeToGameLog: false);

                    var invDamageMultiplier = 1f;
                    var cubeDef = slimBlock.BlockDefinition as MyCubeBlockDefinition;
                    if (cubeDef != null)
                    {
                        WeaponProjectileShared.DamageLog.WriteLine($"    Block damage mult: {cubeDef.GeneralDamageMultiplier}", writeToGameLog: false);
                        invDamageMultiplier = 1f / cubeDef.GeneralDamageMultiplier;
                    }

                    try
                    {
                        if (damage > blockIntegrity)
                        {
                            damage -= blockIntegrity;
                            slimBlock.DoDamage(blockIntegrity * invDamageMultiplier, MyStringHash.GetOrCompute("Railgun"), false, default(MyHitInfo), _gunEntityID); //because some blocks have a stupid damage intake modifier
                        }
                        else
                        {
                            slimBlock.DoDamage(damage * invDamageMultiplier, MyStringHash.GetOrCompute("Railgun"), false, default(MyHitInfo), _gunEntityID);
                            damage = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        WeaponProjectileShared.DamageLog.WriteLine($"{ex}", writeToGameLog: true);
                    }

                    WeaponProjectileShared.DamageLog.WriteLine($"    Pooled damage after: {damage}", writeToGameLog: false);
                    WeaponProjectileShared.DamageLog.WriteLine($"    Block integrity after: {slimBlock.Integrity}", writeToGameLog: false);

                    continue;
                }

                var character = destroyableObject as IMyCharacter;
                if (character != null)
                {
                    WeaponProjectileShared.DamageLog.WriteLine("  Character found", writeToGameLog: false);

                    WeaponProjectileShared.DamageLog.WriteLine($"    Pooled damage before: {damage}", writeToGameLog: false);
                    WeaponProjectileShared.DamageLog.WriteLine($"    Character health before: {character.Integrity}", writeToGameLog: false);

                    if (damage > character.Integrity)
                    {
                        damage -= character.Integrity;
                        character.DoDamage(character.Integrity, MyStringHash.GetOrCompute("Railgun"), false, default(MyHitInfo), _gunEntityID);
                    }
                    else
                    {
                        character.DoDamage(damage, MyStringHash.GetOrCompute("Railgun"), false, default(MyHitInfo), _gunEntityID);
                        damage = 0;
                    }

                    WeaponProjectileShared.DamageLog.WriteLine($"    Pooled damage after: {damage}", writeToGameLog: false);
                    WeaponProjectileShared.DamageLog.WriteLine($"    Character health after: {character.Integrity}", writeToGameLog: false);

                    continue;
                }
                // Implied else

                WeaponProjectileShared.DamageLog.WriteLine("  Destroyable entity found", writeToGameLog: false);

                WeaponProjectileShared.DamageLog.WriteLine($"    Pooled damage before: {damage}", writeToGameLog: false);
                WeaponProjectileShared.DamageLog.WriteLine($"    Object integrity before: {destroyableObject.Integrity}", writeToGameLog: false);
                var cachedIntegrity = destroyableObject.Integrity;
                destroyableObject.DoDamage(damage, MyStringHash.GetOrCompute("Railgun"), false, default(MyHitInfo), _gunEntityID);
                damage -= cachedIntegrity;
                WeaponProjectileShared.DamageLog.WriteLine($"    Pooled damage after: {damage}", writeToGameLog: false);
                WeaponProjectileShared.DamageLog.WriteLine($"    Object integrity after: {destroyableObject.Integrity}", writeToGameLog: false); ;

            }
        }
        #endregion

        void Kill()
        {
            if (_shouldPenetrate && _targetHit)
            {
                if (!_penetratedObjectsSorted)
                {
                    SortObjectsToPenetrate(_hitPosition);
                    _penetratedObjectsSorted = true;
                    return;
                }

                if (!_penetratedObjectsDamaged)
                {
                    DamageObjectsToPenetrate(_penetrationDamage);
                    _penetratedObjectsDamaged = true;
                }
            }

            Killed = true;
        }
    }
}
