using Heart_Module.Data.Scripts.HeartModule.Debug;
using Heart_Module.Data.Scripts.HeartModule.Definitions.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.GuidanceHelpers
{
    public class ProjectileGuidance
    {
        IMyEntity targetEntity;

        Projectile projectile;
        ProjectileDefinitionBase Definition;
        LinkedList<Guidance> stages;
        float time = 0;
        Vector3D randomOffset = Vector3D.Zero;

        public ProjectileGuidance(Projectile projectile)
        {
            this.projectile = projectile;
            Definition = projectile.Definition;
            stages = new LinkedList<Guidance>(Definition.Guidance);

            // Set projectile velocity
            if ((stages.First?.Value.Velocity ?? -1) != -1)
                projectile.Velocity = stages.First.Value.Velocity;
            else
                projectile.Velocity = projectile.Definition.PhysicalProjectile.Velocity;
        }

        public void SetTarget(IMyEntity target)
        {
            targetEntity = target;
        }

        public void RunGuidance(float delta)
        {
            time += delta;
            if (stages.Count == 0 || stages.First.Value.TriggerTime > time) // Don't run logic if no stage is active
                return;

            Guidance currentStage = stages.First.Value;

            if (currentStage.ActiveDuration == -1) // Keep guiding until next stage
            {
                if ((stages.First.Next?.Value.TriggerTime ?? float.MaxValue) <= time) // Move to next stage
                {
                    NextStage(delta);
                    return;
                }
            }
            else if (currentStage.TriggerTime + currentStage.ActiveDuration < time) // Go to next stage if projectile has ran out of time
            {
                NextStage(delta);
                return;
            }

            if (currentStage.DoRaycast)
                CheckRaycast(currentStage);

            if (targetEntity != null && !targetEntity.Closed) // If target is null, just move forward lol lmao
            {
                Vector3D leadPos = targetEntity.PositionComp.WorldAABB.Center;
                if (currentStage.UseAimPrediction)
                    leadPos = TargetingHelper.InterceptionPoint(projectile.Position, projectile.InheritedVelocity, targetEntity.PositionComp.WorldAABB.Center, targetEntity.Physics.LinearVelocity, projectile.Velocity) ?? leadPos;
                leadPos += randomOffset;

                //DebugDraw.AddPoint(leadPos, Color.Wheat, 0);
                StepDirecion((leadPos - projectile.Position).Normalized(), currentStage.TurnRate, delta);
            }
        }

        internal void NextStage(float delta)
        {
            stages.RemoveFirst();

            if ((stages.First?.Value.Velocity ?? -1) != -1)
                projectile.Velocity = stages.First.Value.Velocity;
            else
                projectile.Velocity = projectile.Definition.PhysicalProjectile.Velocity;

            if (stages.First == null)
                return;

            randomOffset = Vector3D.Zero;
            if (stages.First.Value.Inaccuracy != 0)
            {
                Vector3D.CreateFromAzimuthAndElevation(HeartData.I.Random.NextDouble() * 2 * Math.PI, HeartData.I.Random.NextDouble() * 2 * Math.PI, out randomOffset);
                randomOffset *= stages.First.Value.Inaccuracy * HeartData.I.Random.NextDouble();
            }

            RunGuidance(delta); // Avoid a tick of delay
        }

        internal void StepDirecion(Vector3D targetDir, float turnRate, float delta)
        {
            double AngleDifference = Vector3D.Angle(projectile.Direction, targetDir);

            Vector3 RotAxis = Vector3.Cross(projectile.Direction, targetDir);
            RotAxis.Normalize();

            Matrix RotationMatrix = Matrix.CreateFromAxisAngle(RotAxis, (float)HeartUtils.ClampAbs(AngleDifference, turnRate * delta));
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
                if (!IsTargetAllowed(entity, currentstage))
                    continue;

                if (frustrum.Intersects(entity.WorldAABB))
                {
                    //MyAPIGateway.Utilities.ShowNotification("Hit " + entity.DisplayName, 1000 / 60);
                    targetEntity = entity;
                    break;
                }
            }
        }

        internal bool IsTargetAllowed(IMyEntity target, Guidance currentStage)
        {
            if (projectile.Firer == 0) return true;
            IMyEntity firer = MyAPIGateway.Entities.GetEntityById(projectile.Firer);
            if (firer == null || !(firer is IMyCubeBlock))
                return true;

            MyRelationsBetweenPlayerAndBlock relations;

            if (target is IMyCubeGrid)
                relations = HeartUtils.GetRelationsBetweeenGrids(((IMyCubeBlock)firer).CubeGrid, (IMyCubeGrid)target);
            else if (target is IMyPlayer)
                relations = HeartUtils.GetRelationsBetweenGridAndPlayer(((IMyCubeBlock)firer).CubeGrid, ((IMyPlayer)target).IdentityId);
            else
                return true;

            if ((relations == MyRelationsBetweenPlayerAndBlock.NoOwnership || relations == MyRelationsBetweenPlayerAndBlock.Neutral) &&
                (currentStage.IFF & IFF_Enum.TargetNeutrals) == IFF_Enum.TargetNeutrals)
                return true;
            if ((relations == MyRelationsBetweenPlayerAndBlock.Owner) &&
                (currentStage.IFF & IFF_Enum.TargetSelf) == IFF_Enum.TargetSelf)
                return true;
            if ((relations == MyRelationsBetweenPlayerAndBlock.Friends) &&
                (currentStage.IFF & IFF_Enum.TargetFriendlies) == IFF_Enum.TargetFriendlies)
                return true;
            if ((relations == MyRelationsBetweenPlayerAndBlock.Enemies) &&
                (currentStage.IFF & IFF_Enum.TargetEnemies) == IFF_Enum.TargetEnemies)
                return true;

            return false;
        }
    }
}
