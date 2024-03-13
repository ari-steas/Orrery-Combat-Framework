using Heart_Module.Data.Scripts.HeartModule.Utility;
using Sandbox.Game;
using Sandbox.Game.Entities;
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
        MatrixD ProjectileMatrix = MatrixD.Identity;
        MyEntity3DSoundEmitter ProjectileSound;
        public bool IsVisible = true;
        public bool HasAudio = true;
        private float MaxBeamLength = 0; // Limits beam length when A Block:tm: is hit

        internal void InitEffects()
        {
            float f = (float)HeartData.I.Random.NextDouble();
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

        public void DrawUpdate()
        {
            if (!IsVisible || HeartData.I.DegradedMode)
                return;

            // deltaTick is the current offset between tick and draw, to account for variance between FPS and tickrate
            ProjectileMatrix = MatrixD.CreateWorld(Position, Direction, Vector3D.Cross(Direction, Vector3D.Up)); // TODO: Inherit up vector from firer. Also TODO: Make matrix a projectile var

            // Temporary debug draw
            //DebugDraw.AddPoint(visualPosition, Color.Green, 0.000001f);

            // Updated in TickUpdate as opposed to Draw because of lasers.
            if (Definition.Visual.HasTrail && !HeartData.I.IsPaused)
            {
                if (IsHitscan)
                    GlobalEffects.AddLine(Position, Position + Direction * MaxBeamLength, Definition.Visual.TrailFadeTime, Definition.Visual.TrailWidth, Definition.Visual.TrailColor, Definition.Visual.TrailTexture);
                else
                    GlobalEffects.AddLine(Position, Position + Direction * Definition.Visual.TrailLength, Definition.Visual.TrailFadeTime, Definition.Visual.TrailWidth, Definition.Visual.TrailColor, Definition.Visual.TrailTexture);
            }

            if (Definition.Visual.HasAttachedParticle && !HeartData.I.IsPaused)
            {
                if (ProjectileEffect == null)
                    MyParticlesManager.TryCreateParticleEffect(Definition.Visual.AttachedParticle, ref MatrixD.Identity, ref Vector3D.Zero, RenderId, out ProjectileEffect);
                if (RenderId == uint.MaxValue)
                    ProjectileEffect.WorldMatrix = ProjectileMatrix;
            }

            ProjectileEntity.WorldMatrix = ProjectileMatrix;

            if (HasAudio && Definition.Audio.HasTravelSound)
            {
                ProjectileSound.SetPosition(Position);
            }
        }

        private void UpdateAudio()
        {
            if (!HasAudio || !Definition.Audio.HasTravelSound) return;

            ProjectileSound.SetPosition(Position);
            ProjectileSound.SetVelocity(Direction * Velocity);
        }

        private void DrawImpactParticle(Vector3D ImpactPosition, Vector3D ImpactNormal) // TODO: Does not work in multiplayer
        {
            if (!IsVisible || Definition.Visual.ImpactParticle == "" || HeartData.I.DegradedMode)
                return;

            MatrixD matrix = MatrixD.CreateWorld(ImpactPosition, ImpactNormal, Vector3D.CalculatePerpendicularVector(ImpactNormal));
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

        private void CloseDrawing()
        {
            ProjectileEffect?.Close();
            ProjectileEntity?.Close();
            ProjectileSound?.StopSound(true);
            ProjectileSound?.Cleanup();
        }

        internal class LineFade
        {
            public Vector3D Start;
            public Vector3D End;
            public float FadeTime;

            public LineFade(Vector3D start, Vector3D end, float fadeTime)
            {
                Start = start;
                End = end;
                FadeTime = fadeTime;
            }
        }
    }
}
