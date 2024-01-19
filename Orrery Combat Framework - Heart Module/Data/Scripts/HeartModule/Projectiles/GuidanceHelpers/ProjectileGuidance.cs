using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using System.Collections.Generic;
using VRage.ModAPI;
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

            if (targetEntity != null) // If target is null, just 
            {
                Vector3D leadPos = targetEntity.PositionComp.WorldAABB.Center;

                if (currentStage.UseAimPrediction)
                    leadPos = TargetingHelper.InterceptionPoint(projectile.Position, projectile.InheritedVelocity, targetEntity.PositionComp.WorldAABB.Center, targetEntity.Physics.LinearVelocity, projectile.Velocity) ?? leadPos;

                StepDirecion((leadPos - projectile.Position).Normalized(), currentStage.TurnRate, currentStage.TurnRateSpeedRatio, delta);
            }

            time += delta;
        }

        internal void NextStage(float delta)
        {
            stages.RemoveFirst();
            RunGuidance(delta); // Avoid a tick of delay
        }

        internal void StepDirecion(Vector3D targetDir, float turnRate, float rateSpeedRatio, float delta)
        {
            double AngleDifference = Vector3D.Angle(projectile.Direction, targetDir);

            Vector3 RotAxis = Vector3.Cross(projectile.Direction, targetDir);
            RotAxis.Normalize();

            Matrix RotationMatrix = Matrix.CreateFromAxisAngle(RotAxis, (float) HeartUtils.ClampAbs(AngleDifference, turnRate * delta));
            projectile.Direction = Vector3.Transform(projectile.Direction, RotationMatrix).Normalized();
        }
    }
}
