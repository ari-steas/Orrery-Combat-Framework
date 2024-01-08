using Heart_Module.Data.Scripts.HeartModule.Debug;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.Models;
using VRage.ModAPI;
using VRageMath;
using VRageRender;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    partial class Projectile
    {
        MyEntity ProjectileEntity = new MyEntity();
        MyParticleEffect ProjectileEffect;
        uint RenderId = 0;
        Dictionary<MyTuple<Vector3D, Vector3D>, float> TrailFade = new Dictionary<MyTuple<Vector3D, Vector3D>, float>();
        MatrixD ProjectileMatrix = MatrixD.Identity;

        internal void InitEffects()
        {
            if (Definition.Visual.HasModel)
            {
                ProjectileEntity.Init(null, Definition.Visual.Model, null, null);
                ProjectileEntity.Render.CastShadows = false;
                ProjectileEntity.IsPreview = true;
                ProjectileEntity.Save = false;
                ProjectileEntity.SyncFlag = false;
                ProjectileEntity.NeedsWorldMatrix = false;
                ProjectileEntity.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
                MyEntities.Add(ProjectileEntity, true);
                RenderId = ProjectileEntity.Render.GetRenderObjectID();
            }
            else
                RenderId = uint.MaxValue;
        }

        public void DrawUpdate(float deltaTick, float deltaDraw)
        {
            // deltaTick is the current offset between tick and draw, to account for variance between FPS and tickrate
            Vector3D visualPosition = Position + (InheritedVelocity + Direction * (Velocity + Definition.PhysicalProjectile.Acceleration * deltaTick)) * deltaTick;
            ProjectileMatrix = MatrixD.CreateWorld(visualPosition, Direction, Vector3D.Cross(Direction, Vector3D.Up));

            // Temporary debug draw
            //DebugDraw.AddPoint(visualPosition, Color.Green, 0.000001f);

            if (Definition.Visual.HasAttachedParticle && !HeartData.I.IsPaused)
            {
                if (ProjectileEffect == null)
                    MyParticlesManager.TryCreateParticleEffect(Definition.Visual.AttachedParticle, ref MatrixD.Identity, ref Vector3D.Zero, RenderId, out ProjectileEffect);
                if (RenderId == uint.MaxValue)
                    ProjectileEffect.WorldMatrix = ProjectileMatrix;
            }

            ProjectileEntity.WorldMatrix = ProjectileMatrix;

            if (Definition.Visual.HasTrail && !HeartData.I.IsPaused)
                TrailFade.Add(new MyTuple<Vector3D, Vector3D>(visualPosition, visualPosition + Direction * Definition.Visual.TrailLength), Definition.Visual.TrailFadeTime);
            UpdateTrailFade(deltaDraw);
        }

        /// <summary>
        /// Updates trail fade for this projectile.
        /// </summary>
        private void UpdateTrailFade(float delta)
        {
            foreach (var positionTuple in TrailFade.Keys.ToList())
            {
                float lifetimePct = TrailFade[positionTuple] / Definition.Visual.TrailFadeTime;
                Vector4 fadedColor = Definition.Visual.TrailColor * (Definition.Visual.TrailFadeTime == 0 ? 1 : lifetimePct);
                MySimpleObjectDraw.DrawLine(positionTuple.Item1, positionTuple.Item2, Definition.Visual.TrailTexture, ref fadedColor, Definition.Visual.TrailWidth);

                if (!HeartData.I.IsPaused)
                    TrailFade[positionTuple] -= delta;
                if (TrailFade[positionTuple] <= 0)
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
                //MyAPIGateway.Utilities.ShowNotification("Spawned particle at " + hitEffect.WorldMatrix.Translation);
                //hitEffect.Velocity = av.Hit.HitVelocity;

                if (hitEffect.Loop)
                    hitEffect.Stop();
            }
        }

        internal void CloseDrawing()
        {
            ProjectileEffect?.Close();
            ProjectileEntity.Close();
        }
    }
}
