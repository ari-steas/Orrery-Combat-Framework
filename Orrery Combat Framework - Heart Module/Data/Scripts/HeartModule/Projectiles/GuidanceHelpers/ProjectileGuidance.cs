using BulletXNA.BulletCollision;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Voxels;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.GuidanceHelpers
{
    public class ProjectileGuidance
    {
        IMyEntity targetEntity;

        Projectile projectile;
        SerializableProjectileDefinition Definition;
        LinkedList<Guidance> stages;
        float time = 0;

        public ProjectileGuidance(Projectile projectile)
        {
            this.projectile = projectile;
            Definition = projectile.Definition;
            stages = new LinkedList<Guidance>(Definition.Guidance);
        }

        public void SetTarget(IMyEntity target)
        {
            targetEntity = target;
            MyAPIGateway.Utilities.ShowNotification("SET TARGET " + (target == null), 1000/60);
        }

        public void RunGuidance(float delta)
        {
            if (stages.Count == 0)
                return;

            Guidance currentStage = stages.First.Value;

            if (currentStage.ActiveDuration == -1)
            {
                if ((stages.First.Next?.Value.TriggerTime ?? float.MaxValue) <= time) // Move to next stage
                {
                    NextStage(delta);
                    return;
                }
            }
            else if (currentStage.TriggerTime + currentStage.ActiveDuration > time)
            {
                NextStage(delta);
                return;
            }

            if (currentStage.DoRaycast)
                CheckRaycast(currentStage);

            if (targetEntity != null) // If target is null, just move forward lol lmao
            {
                Vector3D leadPos = targetEntity.PositionComp.WorldAABB.Center;

                if (currentStage.UseAimPrediction)
                    leadPos = TargetingHelper.InterceptionPoint(projectile.Position, projectile.InheritedVelocity, targetEntity.PositionComp.WorldAABB.Center, targetEntity.Physics.LinearVelocity, projectile.Velocity) ?? leadPos;

                StepDirecion((leadPos - projectile.Position).Normalized(), currentStage.TurnRate, delta);
            }

            time += delta;
        }

        internal void NextStage(float delta)
        {
            stages.RemoveFirst();
            RunGuidance(delta); // Avoid a tick of delay
        }

        internal void StepDirecion(Vector3D targetDir, float turnRate, float delta)
        {
            double AngleDifference = Vector3D.Angle(projectile.Direction, targetDir);

            Vector3 RotAxis = Vector3.Cross(projectile.Direction, targetDir);
            RotAxis.Normalize();

            Matrix RotationMatrix = Matrix.CreateFromAxisAngle(RotAxis, (float) HeartUtils.ClampAbs(AngleDifference, turnRate * delta));
            projectile.Direction = Vector3.Transform(projectile.Direction, RotationMatrix).Normalized();
        }

        internal void CheckRaycast(Guidance currentstage)
        {
            if (targetEntity == null)
            {
                PreformRaycast(currentstage);
                return;
            }
            double angle = Vector3D.Angle(projectile.Direction, targetEntity.WorldAABB.Center);

            if (angle > currentstage.CastCone)
                PreformRaycast(currentstage);
        }

        /// <summary>
        /// Scans for valid targets within the missile's cone
        /// </summary>
        /// <param name="currentstage"></param>
        internal void PreformRaycast(Guidance currentstage)
        {
            MatrixD frustrumMatrix = MatrixD.CreatePerspectiveFieldOfView(currentstage.CastCone, 1, 50, currentstage.CastDistance);
            frustrumMatrix = MatrixD.Invert(MatrixD.CreateWorld(projectile.Position, projectile.Direction, Vector3D.CalculatePerpendicularVector(projectile.Direction))) * frustrumMatrix;
            BoundingFrustumD frustrum = new BoundingFrustumD(frustrumMatrix);
            BoundingSphereD sphere = new BoundingSphereD(projectile.Position, currentstage.CastDistance);

            foreach (var entity in MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere))
            {
                if (frustrum.Intersects(entity.WorldAABB))
                {
                    //MyAPIGateway.Utilities.ShowNotification("Hit " + entity.DisplayName, 1000 / 60);
                    targetEntity = entity;
                    break;
                }
            }
        }
    }
}
