using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using Whiplash.Utils;

namespace Whiplash.WeaponProjectiles
{
    static class WeaponProjectileShared
    {
        public static Logger DamageLog;

        static List<MyEntity> _entitiesInProximityRange = new List<MyEntity>();

        public static bool ShouldRegisterHit(IMyEntity hitEntity, long shooterID)
        {
            var grid = hitEntity as IMyCubeGrid;

            if (grid != null)
            {
                if (grid.Physics == null) // Ignore projections - thx Toed for finding this lol
                {
                    DamageLog.WriteLine("  Ignoring grid with no physics", writeToGameLog: false);
                    return false;
                }

                var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
                if (gts.GetBlockWithId(shooterID) != null)
                {
                    DamageLog.WriteLine("  Ignoring own grid hit", writeToGameLog: false);
                    return false;
                }
            }

            return true;
        }

        public static bool ProximityDetonate(long shooterID, double proximityRadius, double minimumRangeSq, Vector3D origin, Vector3D from, Vector3D to, out Vector3D closestPoint)
        {
            closestPoint = default(Vector3D);

            Vector3D dir = to - from;
            Vector3D avg = 0.5 * (to + from);
            double dist = dir.Normalize();
            double inflatedRadius = proximityRadius + dist * 0.5;

            BoundingSphereD _inflatedProximitySphere = new BoundingSphereD(avg, inflatedRadius);
            _entitiesInProximityRange.Clear();
            MyGamePruningStructure.GetAllEntitiesInSphere(ref _inflatedProximitySphere, _entitiesInProximityRange);

            if (_entitiesInProximityRange.Count == 0)
            {
                return false;
            }

            MyEntity closest = null;
            double distSq = double.PositiveInfinity;
            foreach (var e in _entitiesInProximityRange)
            {
                var grid = e as IMyCubeGrid;
                if (grid == null)
                {
                    continue;
                }

                Vector3D pos = e.PositionComp.WorldVolume.Center;

                if (Vector3D.DistanceSquared(pos, origin) < minimumRangeSq)
                {
                    continue;
                }

                if (!ShouldRegisterHit(e, shooterID))
                {
                    continue;
                }

                if (!WithinCapsule(pos, from, to, dir, proximityRadius, e.PositionComp.WorldVolume.Radius))
                {
                    continue;
                }

                var thisDistSq = Vector3D.DistanceSquared(pos, from);
                if (thisDistSq < distSq)
                {
                    closest = e;
                    distSq = thisDistSq;
                }
            }

            if (closest == null)
                return false;

            double r1 = closest.PositionComp.WorldVolume.Radius;
            double r2 = proximityRadius;
            double r = r1 + r2;
            Vector3D toClosest = closest.PositionComp.WorldVolume.Center - from;
            Vector3D normalVec = VectorMath.Rejection(toClosest, dir);
            Vector3D parallelVec = toClosest - normalVec;
            double d = Math.Sqrt(r * r - normalVec.LengthSquared());
            double dMaxSq = parallelVec.LengthSquared();
            if (d * d > dMaxSq)
            {
                d = Math.Sqrt(dMaxSq);
            }

            closestPoint = from + parallelVec - dir * d * 0.5;
            return true;
        }

        static bool WithinCapsule(Vector3D position, Vector3D capsuleFrom, Vector3D capsuleTo, Vector3D capsuleAxis, double capsuleRadius, double itemRadius)
        {
            double radSq = (capsuleRadius + itemRadius) * (capsuleRadius + itemRadius);
            double axisLen = Vector3D.Dot(capsuleAxis, capsuleTo - capsuleFrom);
            if (Vector3D.DistanceSquared(position, capsuleFrom) < radSq)
            {
                return true;
            }
            /*
            if (Vector3D.DistanceSquared(position, capsuleTo) < radSq)
            {
                return true;
            }
            */
            Vector3D rejection = VectorMath.Rejection(position - capsuleFrom, capsuleAxis);
            if (rejection.LengthSquared() < radSq 
                && Vector3D.Dot(position - capsuleFrom, capsuleAxis) >= 0
                && Vector3D.Dot(position - capsuleTo, capsuleAxis) < 0)
            {
                return true;
            }
            return false;
        }
    }
}
