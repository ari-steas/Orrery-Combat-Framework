using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using System;
using VRage.Game.Entity;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Utility
{
    public static class TargetingHelper
    {
        public static Vector3D? InterceptionPoint(Vector3D startPos, Vector3D startVel, MyEntity target, int projectileDef)
        {
            SerializableProjectileDefinition def = ProjectileDefinitionManager.GetDefinition(projectileDef);
            if (def == null || target?.Physics == null)
                return null;
            if (def.PhysicalProjectile.IsHitscan)
                return target.PositionComp.GetPosition() - target.Physics.LinearVelocity/60; // Because this doesn't run during simulation, offset velocity
            return InterceptionPoint(startPos + startVel/60, startVel, target.PositionComp.GetPosition() - target.Physics.LinearVelocity / 60, target.Physics.LinearVelocity, def.PhysicalProjectile.Velocity);
        }

        public static Vector3D? InterceptionPoint(Vector3D startPos, Vector3D startVel, Vector3D targetPos, Vector3D targetVel, float projectileSpeed)
        {
            Vector3D relativeVelocity = targetVel - startVel;

            try
            {
                double t = TimeOfInterception(startPos, targetPos, relativeVelocity, projectileSpeed);
                
                // Calculate interception point
                Vector3D interceptionPoint = targetPos + relativeVelocity * t;

                return interceptionPoint;
            }
            catch
            {
                return null;
            }
        }

        public static double TimeOfInterception(Vector3 startPos, Vector3 targetPos, Vector3 relativeVelocity, float projectileSpeed)
        {
            var deltaPos = targetPos  - startPos;

            return 0;
        }

        //public static double TimeOfInterception(Vector3 startPos, Vector3 targetPos, Vector3 relativeVelocity, float projectileSpeed)
        //{
        //    // Calculate quadratic equation coefficients
        //    double a = relativeVelocity.LengthSquared() - projectileSpeed * projectileSpeed;
        //    double b = 2 * relativeVelocity.Dot(targetPos - startPos);
        //    double c = (targetPos - startPos).Dot(targetPos - startPos);
        //
        //    // Solve quadratic equation for time
        //    double discriminant = b * b - 4 * a * c;
        //
        //    if (discriminant < 0)
        //    {
        //        // No real solutions, interception not possible
        //        throw new InvalidOperationException("Interception not possible.");
        //    }
        //
        //    double t1 = (-b + Math.Sqrt(discriminant)) / (2 * a);
        //    double t2 = (-b - Math.Sqrt(discriminant)) / (2 * a);
        //
        //    // Return the positive real solution, if any
        //    if (t1 > 0 && t2 > 0)
        //        return Math.Min(t1, t2);
        //
        //    if (t1 > 0)
        //        return t1;
        //    else if (t2 > 0)
        //        return t2;
        //    else
        //        // No positive real solutions, interception not possible
        //        throw new InvalidOperationException("Interception not possible.");
        //}
    }
}
