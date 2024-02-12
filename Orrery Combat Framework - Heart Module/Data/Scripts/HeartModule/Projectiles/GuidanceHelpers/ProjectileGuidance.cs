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

            if (targetEntity != null && !targetEntity.Closed) // If target is null, just move forward
            {
                Vector3D leadPos = targetEntity.PositionComp.WorldAABB.Center;
                if (currentStage.UseAimPrediction)
                    leadPos = TargetingHelper.InterceptionPoint(projectile.Position, projectile.InheritedVelocity, targetEntity.PositionComp.WorldAABB.Center, targetEntity.Physics.LinearVelocity, projectile.Velocity) ?? leadPos;
                leadPos += randomOffset;

                // Assuming MaxGs is part of the currentStage object
                float maxGs = currentStage.MaxGs; // You need to have MaxGs defined in your Guidance structure

                // Adjust the call to StepDirection to include the maxGs parameter
                StepDirection((leadPos - projectile.Position).Normalized(), currentStage.TurnRate, delta, maxGs);
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
            //projectile.Definition.LiveMethods.OnGuidanceStage?.Invoke(projectile.Id, stages.First?.Value);
            RunGuidance(delta); // Avoid a tick of delay
        }

        internal void StepDirection(Vector3D targetDir, float turnRate, float delta, float maxGs)
        {
            double angleDifference = Vector3D.Angle(projectile.Direction, targetDir);

            // Calculate the rotational axis
            Vector3D rotAxis = Vector3D.Cross(projectile.Direction, targetDir);
            rotAxis.Normalize();

            // Calculate the maximum allowable angle change based on turn rate
            float maxAngleChangeByTurnRate = turnRate * delta;

            // Calculate the maximum allowable angle change based on MaxGs
            float maxAngleChangeByMaxGs = CalculateMaxAngleChange(projectile.Velocity, maxGs, delta);

            // Apply the most restrictive limit
            float actualAngleChange = Math.Min((float)angleDifference, Math.Min(maxAngleChangeByTurnRate, maxAngleChangeByMaxGs));

            // Ensure the angle change does not exceed the physical capabilities of the projectile
            actualAngleChange = Math.Min(actualAngleChange, (float)angleDifference);

            // Apply the calculated rotation
            if (angleDifference > 0) // Avoid division by zero
            {
                MatrixD rotationMatrix = MatrixD.CreateFromAxisAngle(rotAxis, actualAngleChange / angleDifference * (float)angleDifference);
                projectile.Direction = Vector3D.Transform(projectile.Direction, rotationMatrix).Normalized();
            }
        }

        // Helper method to calculate the maximum angle change allowed by MaxGs
        private float CalculateMaxAngleChange(float velocity, float maxGs, float delta)
        {
            // Assuming velocity is in meters per second and delta is in seconds,
            // calculate the radius of the circular path for the given velocity and G-force
            float gForceAcceleration = maxGs * 9.81f; // Earth gravity in m/s^2
            float radiusOfTurn = (velocity * velocity) / gForceAcceleration;

            // The maximum distance the projectile can travel in one tick, given its velocity
            float distance = velocity * delta;

            // The maximum angle change, in radians, without exceeding the MaxGs
            float maxAngleChange = distance / radiusOfTurn;

            return maxAngleChange;
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
