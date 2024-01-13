using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    partial class Projectile
    {
        MyEntity ProjectileEntity = new MyEntity();
        MyParticleEffect ProjectileEffect;
        uint RenderId = 0;
        Dictionary<MyTuple<Vector3D, Vector3D>, float> TrailFade = new Dictionary<MyTuple<Vector3D, Vector3D>, float>(); // Maybe try a Stack var?
        MatrixD ProjectileMatrix = MatrixD.Identity;
        MyEntity3DSoundEmitter ProjectileSound;
        public bool IsVisible = true;
        public bool HasAudio = true;
        private float MaxBeamLength = 0; // Limits beam length when A Block:tm: is hit

        internal void InitEffects()
        {
            float f = (float) HeartData.I.Random.NextDouble();
            IsVisible = f <= Definition.Visual.VisibleChance;
            HasAudio = f <= Definition.Audio.SoundChance;

            if (IsVisible && Definition.Visual.HasModel)
            {
                ProjectileEntity.Init(null, Definition.Visual.Model, null, null);
                ProjectileEntity.Render.CastShadows = false;
                ProjectileEntity.IsPreview = true;
                ProjectileEntity.Save = false;
                ProjectileEntity.SyncFlag = false;
                ProjectileEntity.NeedsWorldMatrix = false;
                ProjectileEntity.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
                MyEntities.Add(ProjectileEntity, true);
                ProjectileEntity.WorldMatrix = MatrixD.CreateWorld(Position, Direction, Vector3D.Cross(Direction, Vector3D.Up));
                RenderId = ProjectileEntity.Render.GetRenderObjectID();
            }
            else
                RenderId = uint.MaxValue;

            if (HasAudio && Definition.Audio.HasTravelSound)
            {
                ProjectileSound = new MyEntity3DSoundEmitter(null);
                ProjectileSound.SetPosition(Position);
                ProjectileSound.CanPlayLoopSounds = true;
                ProjectileSound.VolumeMultiplier = Definition.Audio.TravelVolume;
                ProjectileSound.CustomMaxDistance = Definition.Audio.TravelMaxDistance;
                ProjectileSound.PlaySound(Definition.Audio.TravelSoundPair, true);
            }
        }

        public void DrawUpdate(float deltaTick, float deltaDraw)
        {
            if (!IsVisible)
                return;

            // deltaTick is the current offset between tick and draw, to account for variance between FPS and tickrate
            Vector3D visualPosition = Position + (InheritedVelocity + Direction * (Velocity + Definition.PhysicalProjectile.Acceleration * deltaTick)) * deltaTick;
            ProjectileMatrix = MatrixD.CreateWorld(visualPosition, Direction, Vector3D.Cross(Direction, Vector3D.Up)); // TODO: Inherit up vector from firer. Also TODO: Make matrix a projectile var

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
            {
                if (IsHitscan)
                    TrailFade.Add(new MyTuple<Vector3D, Vector3D>(visualPosition, visualPosition + Direction * MaxBeamLength), Definition.Visual.TrailFadeTime);
                else
                    TrailFade.Add(new MyTuple<Vector3D, Vector3D>(visualPosition, visualPosition + Direction * Definition.Visual.TrailLength), Definition.Visual.TrailFadeTime);
            }
                
            UpdateTrailFade(deltaDraw);

            if (HasAudio && Definition.Audio.HasTravelSound)
            {
                ProjectileSound.SetPosition(Position);
            }
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

                // TODO: Make persistent billboard system. DrawLine is very expensive.
                BoundingSphereD sphere = new BoundingSphereD(positionTuple.Item1, IsHitscan ? MaxBeamLength : Definition.Visual.TrailLength);
                if (MyAPIGateway.Session.Camera?.IsInFrustum(ref sphere) ?? false)
                    MySimpleObjectDraw.DrawLine(positionTuple.Item1, positionTuple.Item2, Definition.Visual.TrailTexture, ref fadedColor, Definition.Visual.TrailWidth);
                
                if (!HeartData.I.IsPaused)
                    TrailFade[positionTuple] -= delta;
                if (TrailFade[positionTuple] <= 0)
                    TrailFade.Remove(positionTuple);
            }
        }

        private void UpdateAudio()
        {
            if (!HasAudio || !Definition.Audio.HasTravelSound) return;

            ProjectileSound.SetPosition(Position);
            ProjectileSound.SetVelocity(Direction * Velocity);
        }

        private void DrawImpactParticle(Vector3D ImpactPosition)
        {
            if (!IsVisible || Definition.Visual.ImpactParticle == "")
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

        private void PlayImpactAudio(Vector3D ImpactPosition)
        {
            if (!HasAudio || !Definition.Audio.HasImpactSound) return;
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(Definition.Audio.ImpactSound, ImpactPosition);
        }

        internal void CloseDrawing()
        {
            ProjectileEffect?.Close();
            ProjectileEntity?.Close();
            ProjectileSound?.StopSound(true);
            ProjectileSound?.Cleanup();
        }
    }
}
