using Heart_Module.Data.Scripts.HeartModule.Definitions;
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
