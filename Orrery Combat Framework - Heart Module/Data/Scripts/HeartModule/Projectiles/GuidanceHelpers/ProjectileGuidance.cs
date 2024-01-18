using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Scripting;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.GuidanceHelpers
{
    public class ProjectileGuidance
    {
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

            StepDirecion(-projectile.Position, currentStage.TurnRate, currentStage.TurnRateSpeedRatio, delta);

            time += delta;
        }

        internal void NextStage(float delta)
        {
            stages.RemoveFirst();
            RunGuidance(delta); // Avoid a tick of delay
        }

        internal void StepDirecion(Vector3D targetDir, float maxAngle, float rateSpeedRatio, float delta)
        {
            double currentAngle = Vector3D.Angle(projectile.Direction, targetDir);
            double ratio = HeartUtils.LimitRotationSpeed(currentAngle, 0, maxAngle)/currentAngle; // TODO fix math

            Vector3D newDirection = Vector3D.Lerp(projectile.Direction, targetDir, ratio * delta).Normalized();

            projectile.Direction = newDirection;

            if (rateSpeedRatio != 0)
            {
                projectile.Velocity *= (float)(ratio * rateSpeedRatio);
            }
        }
    }
}
