using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    public partial class Projectile
    {
        private bool shouldCheckEntities = false;

        /// <summary>
        /// Perform a cheap intersection check to see if it's worth doing a raycast.
        /// </summary>
        public void UpdateBoundingBoxCheck(BoundingSphere[] entitiesToCheck)
        {
            shouldCheckEntities = false;
            Ray travelLine = new Ray(Position, Direction);
            double checkDistSq = (NextMoveStep - Position).LengthSquared();

            foreach (var entity in entitiesToCheck)
            {
                double? dist = entity.Intersects(travelLine); // This seems to be the cheapest form of line checking
                if (!dist.HasValue)
                    continue;
                if (dist * dist > checkDistSq)
                    continue;

                shouldCheckEntities = true;
                break;
            }
        }

        public float CheckHits()
        {
            if (NextMoveStep == Vector3D.Zero)
                return -1;

            double len = IsHitscan ? Definition.PhysicalProjectile.MaxTrajectory : Vector3D.Distance(Position, NextMoveStep);
            double dist = -1;

            if (RemainingImpacts > 0 && Definition.Damage.DamageToProjectiles > 0)
            {
                List<Projectile> hittableProjectiles = new List<Projectile>();
                ProjectileManager.I.GetProjectilesInSphere(new BoundingSphereD(Position, len), ref hittableProjectiles, true);

                float damageToProjectilesInAoE = 0;
                List<Projectile> projectilesInAoE = new List<Projectile>();
                if (Definition.Damage.DamageToProjectilesRadius > 0)
                    ProjectileManager.I.GetProjectilesInSphere(new BoundingSphereD(Position, Definition.Damage.DamageToProjectilesRadius), ref projectilesInAoE, true);

                RayD ray = new RayD(Position, Direction);

                foreach (var projectile in hittableProjectiles.ToArray())
                {
                    if (RemainingImpacts <= 0 || projectile == this || projectile.Firer == Firer)
                        continue;

                    BoundingSphereD sphere = new BoundingSphereD(projectile.Position, projectile.Definition.PhysicalProjectile.ProjectileSize);
                    double? intersectDist = ray.Intersects(sphere);
                    if (intersectDist != null)
                    {
                        dist = intersectDist.Value;
                        projectile.Health -= Definition.Damage.DamageToProjectiles;

                        damageToProjectilesInAoE += Definition.Damage.DamageToProjectiles;

                        Vector3D hitPos = Position + Direction * dist;

                        if (MyAPIGateway.Session.IsServer)
                            PlayImpactAudio(hitPos); // Audio is global
                        if (!MyAPIGateway.Utilities.IsDedicated)
                            DrawImpactParticle(hitPos, Direction); // Visuals are clientside

                        Definition.LiveMethods.OnImpact?.Invoke(Id, hitPos, Direction, null);

                        RemainingImpacts--;
                    }
                }

                if (damageToProjectilesInAoE > 0)
                    foreach (var projectile in projectilesInAoE)
                        if (projectile != this)
                            projectile.Health -= damageToProjectilesInAoE;
            }

            if (shouldCheckEntities && RemainingImpacts > 0)
            {
                List<IHitInfo> intersects = new List<IHitInfo>();
                MyAPIGateway.Physics.CastRay(Position, NextMoveStep, intersects);

                foreach (var hitInfo in intersects)
                {
                    if (RemainingImpacts <= 0)
                        break;

                    if (hitInfo.HitEntity.EntityId == Firer)
                        continue; // Skip firer

                    dist = hitInfo.Fraction * len;

                    if (hitInfo.HitEntity is IMyCubeGrid)
                        DamageHandler.QueueEvent(new DamageEvent(hitInfo.HitEntity, DamageEvent.DamageEntType.Grid, this, hitInfo.Position, hitInfo.Normal));
                    else if (hitInfo.HitEntity is IMyCharacter)
                        DamageHandler.QueueEvent(new DamageEvent(hitInfo.HitEntity, DamageEvent.DamageEntType.Character, this, hitInfo.Position, hitInfo.Normal));

                    if (MyAPIGateway.Session.IsServer)
                        PlayImpactAudio(hitInfo.Position); // Audio is global
                    if (!MyAPIGateway.Utilities.IsDedicated)
                        DrawImpactParticle(hitInfo.Position, hitInfo.Normal); // Visuals are clientside

                    Definition.LiveMethods.OnImpact?.Invoke(Id, hitInfo.Position, Direction, (MyEntity)hitInfo.HitEntity);

                    RemainingImpacts--;
                }
            }

            if (RemainingImpacts <= 0)
                if (!IsHitscan)
                    QueueDispose();

            return (float)dist;
        }
    }
}
