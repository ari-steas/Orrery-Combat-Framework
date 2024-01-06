using Heart_Module.Data.Scripts.HeartModule.Debug;
using Sandbox.ModAPI;
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

        internal void InitDrawing()
        {
            ProjectileBillboard = new MyBillboard();
            ProjectileEntity = new MyEntity();
            RenderId = ProjectileEntity.Render.GetRenderObjectID();
        }

        public void DrawUpdate(float delta)
        {
            Vector3D visualPosition = Position + (InheritedVelocity + Direction * (Velocity + Definition.PhysicalProjectile.Acceleration * delta)) * delta;
            // Temporary debug draw
            //DebugDraw.AddPoint(visualPosition, Color.Green, 0.000001f);

            if (Definition.Visual.AttachedParticle != "")
            {
                MatrixD matrix = MatrixD.CreateWorld(visualPosition, Direction, Vector3D.Cross(Direction, Vector3D.Up));

                if (ProjectileEffect == null)
                {
                    MyParticlesManager.TryCreateParticleEffect(Definition.Visual.AttachedParticle, ref matrix, ref visualPosition, RenderId, out ProjectileEffect);
                }
                else
                {
                    ProjectileEffect.WorldMatrix = matrix;
                }
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
