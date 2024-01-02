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
using Whiplash.WeaponFramework;
using VRage.Collections;
using VRage.Voxels;
using Whiplash.WeaponTracers;
using Whiplash.Utils;

namespace Whiplash.WeaponProjectiles
{
    public class WeaponProjectileShadow
    {
        public bool DrawingImpactSprite { get; private set; } = false;
        public bool Remove { get; private set; } = false;
        bool _drawImpactSprite = false;

        readonly bool _drawTrail;

        readonly float _proximityDetonationRange;
        readonly float _proximityDetonationArmingRangeSq;
        readonly bool _shouldProximityDetonate;
        readonly Vector3 _tracerColor;
        readonly float _tracerScale;
        readonly Vector3D _origin;
        Vector3D _lastCheckedPosition;
        Vector3D _position;
        Vector3D _lastPosition;
        Vector3D _velocity;
        Vector3D _initProjectileVelocity;
        Vector3D _initShooterVelocity;
        Vector3D _lastVelocity;
        Vector3D _direction;
        Vector3D _trailFrom;
        Vector3D _trailTo;
        Vector4 _trailColor;
        readonly float _trailDecayRatio;
        bool _hasDrawnTracer = false;
        bool _targetHit = false;
        bool _hitMaxTrajectory = false;
        bool _positionChecked = false;
        double _naturalGravityMult;
        double _artificialGravityMult;
        double _maxTrajectory;
        const double Tick = 1.0 / 60.0;
        readonly List<MyLineSegmentOverlapResult<MyVoxelBase>> _voxelOverlap = new List<MyLineSegmentOverlapResult<MyVoxelBase>>();
        WeaponTracer _currentTracer;
        int _checkIntersectionIndex = 5;
        long _shooterID;

        double _secondsSinceImpactSpriteSpawn = 0;
        double _impactSpriteDuration = 0;
        MyParticleEffect _impactSprite;

        public WeaponProjectileShadow(WeaponFireSyncData fireSyncData)
        {
            _tracerColor = fireSyncData.TracerColor;
            _trailColor = new Vector4(_tracerColor, 1f);
            _tracerScale = fireSyncData.ProjectileTrailScale;
            _velocity = fireSyncData.ShooterVelocity + fireSyncData.Direction * fireSyncData.MuzzleVelocity;
            _lastVelocity = _velocity;
            _initShooterVelocity = fireSyncData.ShooterVelocity;
            _initProjectileVelocity = fireSyncData.Direction * fireSyncData.MuzzleVelocity;
            _direction = Vector3.Normalize(_velocity);
            _drawTrail = fireSyncData.DrawTrails;
            _trailDecayRatio = fireSyncData.TrailDecayRatio;
            _naturalGravityMult = fireSyncData.NatGravityMult;
            _artificialGravityMult = fireSyncData.ArtGravityMult;
            _maxTrajectory = fireSyncData.MaxRange;
            _shooterID = fireSyncData.ShooterID;
            _drawImpactSprite = fireSyncData.DrawImpactSprite;
            _shouldProximityDetonate = fireSyncData.ShouldProximityDetonate;
            _proximityDetonationRange = fireSyncData.ProximityDetonationRange;
            _proximityDetonationArmingRangeSq = fireSyncData.ProximityDetonationArmingRange * fireSyncData.ProximityDetonationArmingRange;

            _origin = fireSyncData.Origin;

            _lastCheckedPosition = _origin;
            _trailFrom = _origin;
            _trailTo = _origin;
            _lastPosition = _origin;

            double initialStepTime = WeaponProjectile.RaycastDelayTicks * Tick;
            Vector3D acceleration = WeaponProjectile.GetGravity(_origin, _naturalGravityMult, _artificialGravityMult);
            _position = _origin +  _velocity * initialStepTime + 0.5 * acceleration * initialStepTime * initialStepTime; // Step forward in time
            _velocity += acceleration * initialStepTime;
        }

        public void Update()
        {
            if (DrawingImpactSprite)
            {
                _secondsSinceImpactSpriteSpawn += Tick;
                if (_secondsSinceImpactSpriteSpawn >= _impactSpriteDuration)
                {
                    if (_impactSprite != null)
                        _impactSprite.Stop();
                    Remove = true;
                }
                return;
            }

            SimulateShadow();
            if (_targetHit)
            {
                DrawLastTracer();
                if (_drawImpactSprite)
                {
                    DrawImpactSprite();
                    DrawingImpactSprite = true;
                }
                else
                {
                    Remove = true;
                }
            }
            WeaponSession.AddTracer(_currentTracer);
        }

        public void DrawLastTracer()
        {
            _currentTracer.To = _position;
        }


        public void SimulateShadow()
        {
            // Calc acceleration
            Vector3D acceleration = WeaponProjectile.GetGravity(_position, _naturalGravityMult, _artificialGravityMult);

            // Update position
            _lastPosition = _position;
            _position += (_velocity * Tick + 0.5 * acceleration * Tick * Tick);
            var toOrigin = _position - _origin;

            // update velocity
            _velocity += acceleration * Tick;

            // Update direction if velocity has changed
            if (!_velocity.Equals(_lastVelocity, 1e-3))
                _direction = Vector3D.Normalize(_velocity);

            _lastVelocity = _velocity;

            _trailFrom = _trailTo;
            var stepBackDist = _velocity * Tick * WeaponProjectile.RaycastDelayTicks;
            _trailTo = _position - stepBackDist;

            // Get important bullet parameters
            float lengthMultiplier = 40f * _tracerScale;
            lengthMultiplier *= 0.6f;

            bool shouldDraw = false;
            if (lengthMultiplier > 0f && !_targetHit && Vector3D.DistanceSquared(_trailTo, _origin) > lengthMultiplier * lengthMultiplier) // && !Killed)
                shouldDraw = true;

            _currentTracer = new WeaponTracer(
                _velocity - _initShooterVelocity,
                _initShooterVelocity,
                _trailFrom,
                _trailTo,
                _tracerColor,
                _tracerScale,
                _trailDecayRatio,
                shouldDraw,
                _drawTrail);

            if (_hitMaxTrajectory)
            {
                _targetHit = true;
                return;
            }

            if (toOrigin.LengthSquared() > _maxTrajectory * _maxTrajectory)
            {
                WeaponProjectileShared.DamageLog.WriteLine("Shadow: Max range hit");
                _hitMaxTrajectory = true;
                _positionChecked = false;
            }

            _checkIntersectionIndex = ++_checkIntersectionIndex % WeaponProjectile.RaycastDelayTicks;
            if (_checkIntersectionIndex != 0 && _positionChecked)
            {
                return;
            }

            var to = _position;
            var from = _lastCheckedPosition;
            _positionChecked = true;
            _lastCheckedPosition = _position;

            IHitInfo hitInfo;
            bool hit = MyAPIGateway.Physics.CastRay(from, to, out hitInfo, 15);

            // DS - Shield hit intersection and damage
            if (WeaponSession.Instance.ShieldApiLoaded)
            {
                var checkLine = new LineD(from, to);
                var shieldInfo = WeaponSession.Instance.ShieldApi.ClosestShieldInLine(checkLine, true);
                if (hit && shieldInfo.Item1.HasValue && hitInfo.Fraction * checkLine.Length > shieldInfo.Item1.Value || shieldInfo.Item1.HasValue)
                {
                    _position = from + (checkLine.Direction * shieldInfo.Item1.Value);
                    _targetHit = true;
                    return;
                }
            }

            // Check for proximity detonation
            if (_shouldProximityDetonate && !hit && !_targetHit && toOrigin.LengthSquared() > _proximityDetonationArmingRangeSq)
            {
                Vector3D hitPos;
                if (WeaponProjectileShared.ProximityDetonate(_shooterID, _proximityDetonationRange, _proximityDetonationArmingRangeSq, _origin, from, to, out hitPos))
                {
                    WeaponProjectileShared.DamageLog.WriteLine("Shadow: Proximity detonation");
                    _position = hitPos;
                    _targetHit = true;
                    return;
                }
            }

            if (hit)
            {
                WeaponProjectileShared.DamageLog.WriteLine("Shadow: Raycast hit");
                if (WeaponProjectileShared.ShouldRegisterHit(hitInfo.HitEntity, _shooterID))
                {
                    _position = hitInfo.Position;
                    _targetHit = true;
                    return;
                }
                else
                {
                    WeaponProjectileShared.DamageLog.WriteLine("  Shadow: Raycast hit ignored", writeToGameLog: false);
                }
            }
            // implied else
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
                    WeaponProjectileShared.DamageLog.WriteLine("Shadow: Voxel hit");
                    _position = hitPos.Value;
                    _targetHit = true;
                    return;
                }
            }
        }

        void DrawImpactSprite()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
            {
                return;
            }

            IMyEntity gunEnt;
            MyAPIGateway.Entities.TryGetEntityById(_shooterID, out gunEnt);
            if (gunEnt == null)
            {
                return;
            }
            IMyCubeBlock gunCube = gunEnt as IMyCubeBlock;
            if (gunCube == null)
            {
                return;
            }

            WeaponConfig config;
            bool fixedConfigExists = FrameworkWeaponAPI.FixedGunWeaponConfigs.TryGetValue(gunCube.BlockDefinition.SubtypeName, out config);
            if (!fixedConfigExists)
            {
                TurretWeaponConfig turretConfig;
                bool turretConfigExists = FrameworkWeaponAPI.TurretWeaponConfigs.TryGetValue(gunCube.BlockDefinition.SubtypeName, out turretConfig);
                if (!turretConfigExists)
                {
                    return;
                }
                config = turretConfig;
            }

            if (!MyAPIGateway.Utilities.IsDedicated)
            {

                MatrixD matrix = MatrixD.CreateFromDir(-_direction); //Negative because muzzle flashes are fucking backwards
                matrix.Translation = _position;
                bool foundParticle = MyParticlesManager.TryCreateParticleEffect(config.ImpactSpriteName, ref matrix, ref _position, uint.MaxValue, out _impactSprite);
                if (foundParticle)
                {
                    _impactSprite.UserScale = config.ImpactSpriteScale;
                    _impactSprite.Play();
                    _secondsSinceImpactSpriteSpawn = 0;
                    _impactSpriteDuration = config.ImpactSpriteDuration;
                }
            }
        }
    }
}
