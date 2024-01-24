using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using System;
using VRage.ModAPI;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Utility
{
    public static class TargetingHelper
    {
        /// <summary>
        /// Calculate lead position for a projectile.
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="startVel"></param>
        /// <param name="target"></param>
        /// <param name="projectileDef"></param>
        /// <returns></returns>
        public static Vector3D? InterceptionPoint(Vector3D startPos, Vector3D startVel, Projectile target, int projectileDef)
        {
            SerializableProjectileDefinition def = ProjectileDefinitionManager.GetDefinition(projectileDef);
            if (def == null || target == null)
                return null;
            if (def.PhysicalProjectile.IsHitscan)
                return target.Position - (target.InheritedVelocity + target.Direction * target.Velocity) / 60f; // Because this doesn't run during simulation, offset velocity

            return InterceptionPoint(startPos, startVel, target.Position, target.InheritedVelocity + target.Direction * target.Velocity, def.PhysicalProjectile.Velocity);
        }

        /// <summary>
        /// Calculate lead position for an entity.
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="startVel"></param>
        /// <param name="target"></param>
        /// <param name="projectileDef"></param>
        /// <returns></returns>
        public static Vector3D? InterceptionPoint(Vector3D startPos, Vector3D startVel, IMyEntity target, int projectileDef)
        {
            SerializableProjectileDefinition def = ProjectileDefinitionManager.GetDefinition(projectileDef);
            if (def == null || target?.Physics == null)
                return null;
            if (def.PhysicalProjectile.IsHitscan)
                return target.WorldAABB.Center - target.Physics.LinearVelocity / 60f; // Because this doesn't run during simulation, offset velocity

            return InterceptionPoint(startPos, startVel, target.WorldAABB.Center, target.Physics.LinearVelocity, def.PhysicalProjectile.Velocity);
        }

        public static Vector3D? InterceptionPoint(Vector3D startPos, Vector3D startVel, Vector3D targetPos, Vector3D targetVel, float projectileSpeed)
        {
            Vector3D relativeVelocity = targetVel - startVel;

            try
            {
                double t = TimeOfInterception(targetPos - startPos, relativeVelocity, projectileSpeed);
                if (t == -1)
                    return null;

                // Calculate interception point
                Vector3D interceptionPoint = targetPos + relativeVelocity * t;

                return interceptionPoint;
            }
            catch
            {
                return null;
            }
        }

        public static double TimeOfInterception(Vector3 relativePosition, Vector3 relativeVelocity, float projectileSpeed) // Adapted from Bunny83 on the Unity forums https://discussions.unity.com/t/how-to-calculate-the-point-of-intercept-in-3d-space/22540
        {
            double velocitySquared = relativeVelocity.LengthSquared();
            if (velocitySquared < double.Epsilon)
                return 0;

            double a = velocitySquared - projectileSpeed * projectileSpeed;
            if (Math.Abs(a) < double.Epsilon)
            {
                double t = -relativePosition.LengthSquared() / (2 * Vector3D.Dot(relativeVelocity, relativePosition));
                return t > 0 ? t : -1;
            }

            double b = 2 * Vector3D.Dot(relativeVelocity, relativePosition);
            double c = relativePosition.LengthSquared();
            double determinant = b * b - 4 * a * c;

            if (determinant > 0) // Two solutions
            {
                double t1 = (-b + Math.Sqrt(determinant)) / (2 * a);
                double t2 = (-b - Math.Sqrt(determinant)) / (2 * a);
                if (t1 > 0)
                {
                    if (t2 > 0)
                        return t1 < t2 ? t1 : t2;
                    return t1;
                }
                return t2 > 0 ? t2 : -1;
            }
            else if (determinant < 0) // No solutions
                return -1;

            double solution = -b / (2 * a); // One solution
            return solution > 0 ? solution : -1;
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
