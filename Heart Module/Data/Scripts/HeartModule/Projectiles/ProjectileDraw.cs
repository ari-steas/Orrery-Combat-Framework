using Heart_Module.Data.Scripts.HeartModule.Debug;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRageMath;
using VRageRender;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    partial class Projectile
    {
        MyBillboard ProjectileBillboard;
        MyEntity ProjectileEntity;

        internal void InitDrawing()
        {
            ProjectileBillboard = new MyBillboard();
            ProjectileEntity = new MyEntity();
        }

        public void DrawUpdate(float delta)
        {
            // Temporary debug draw
            DebugDraw.AddPoint(Position + (InheritedVelocity + Direction * (Velocity + Definition.PhysicalProjectile.Acceleration * delta)) * delta, Color.Green, 0.000001f);
            
        }

        internal void CloseDrawing()
        {

        }
    }
}
