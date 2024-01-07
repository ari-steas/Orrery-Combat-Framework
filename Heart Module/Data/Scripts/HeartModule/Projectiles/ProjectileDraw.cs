using Heart_Module.Data.Scripts.HeartModule.Debug;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;
using VRageRender;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    partial class Projectile
    {
        MyBillboard ProjectileBillboard;
        MyEntity ProjectileEntity;
        MyParticleEffect ProjectileEffect;
        uint RenderId = 0;
        Dictionary<MyTuple<Vector3D, Vector3D>, long> TrailFade = new Dictionary<MyTuple<Vector3D, Vector3D>, long>();

        internal void InitDrawing()
        {
            ProjectileBillboard = new MyBillboard();
            ProjectileEntity = new MyEntity();
            RenderId = ProjectileEntity.Render.GetRenderObjectID();
        }

        public void DrawUpdate(float delta)
        {
            Vector3D visualPosition = Position + (InheritedVelocity + Direction * (Velocity + Definition.PhysicalProjectile.Acceleration * delta)) * delta;
            MatrixD matrix = MatrixD.CreateWorld(visualPosition, Direction, Vector3D.Cross(Direction, Vector3D.Up));

            // Temporary debug draw
            //DebugDraw.AddPoint(visualPosition, Color.Green, 0.000001f);

            if (Definition.Visual.AttachedParticle != "" && !HeartData.I.IsPaused)
            {
                if (ProjectileEffect == null)
                    MyParticlesManager.TryCreateParticleEffect(Definition.Visual.AttachedParticle, ref matrix, ref visualPosition, RenderId, out ProjectileEffect);
                else
                    ProjectileEffect.WorldMatrix = matrix;
            }

            if (Definition.Visual.TrailTexture != null && !HeartData.I.IsPaused)
                TrailFade.Add(new MyTuple<Vector3D, Vector3D>(visualPosition, visualPosition + Direction * Definition.Visual.TrailLength), DateTime.Now.Ticks + (long)(TimeSpan.TicksPerSecond * Definition.Visual.TrailFadeTime));
            UpdateTrailFade();
        }

        /// <summary>
        /// Updates trail fade for this projectile.
        /// </summary>
        private void UpdateTrailFade()
        {
            foreach (var positionTuple in TrailFade.Keys.ToList())
            {
                float percentage = (TrailFade[positionTuple] - DateTime.Now.Ticks) / (Definition.Visual.TrailFadeTime * TimeSpan.TicksPerSecond);
                Vector4 fadedColor = Definition.Visual.TrailColor * percentage;
                MySimpleObjectDraw.DrawLine(positionTuple.Item1, positionTuple.Item2, Definition.Visual.TrailTexture, ref fadedColor, Definition.Visual.TrailWidth);
                if (TrailFade[positionTuple] <= DateTime.Now.Ticks)
                    TrailFade.Remove(positionTuple);
            }
        }

        private void DrawImpactParticle(Vector3D ImpactPosition)
        {
            if (Definition.Visual.ImpactParticle == "")
                return;

            MatrixD matrix = MatrixD.CreateTranslation(ImpactPosition);
            MyParticleEffect hitEffect;
            if (MyParticlesManager.TryCreateParticleEffect(Definition.Visual.ImpactParticle, ref matrix, ref ImpactPosition, uint.MaxValue, out hitEffect))
            {
                MyAPIGateway.Utilities.ShowNotification("Spawned particle at " + hitEffect.WorldMatrix.Translation);
                //hitEffect.UserScale = av.AmmoDef.AmmoGraphics.Particles.Hit.Extras.Scale;
                //hitEffect.Velocity = av.Hit.HitVelocity;

                if (hitEffect.Loop)
                    hitEffect.Stop();
            }
        }

        internal void CloseDrawing()
        {
            ProjectileEffect?.Close();
        }
    }
}
