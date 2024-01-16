using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VRageRender.MyBillboard;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using Heart_Module.Data.Scripts.HeartModule.Definitions;

namespace Heart_Module.Data.Scripts.HeartModule.FX
{
    public class SphereRenderer : IRenderObject
    {
        MyStringId? Material = null;

        MySimpleObjectRasterizer rasterizer;
        BlendTypeEnum BlendType;

        MatrixD Center;
        float StartRadius;
        float Radius;
        Vector4 StartColor;
        Vector4 Color;
        float StartThickness;
        float Thickness;
        int time;
        int startTime;
        int wireDivideRatio = 36;
        Vector3D velocity;
        public SphereRenderer(MatrixD Center, float Radius, Vector4 Color, float Thickness, int time, bool Fade, Vector3D velocity, string Material, int wireDivideRatio)
        {
            this.Center = Center;
            this.Radius = Radius;
            this.Color = Color;
            this.Thickness = Thickness;
            this.time = time;
            this.wireDivideRatio = wireDivideRatio;
            if (Material != "")
            {
                this.Material = MyStringId.GetOrCompute(Material);
            }

            if (Fade)
            {
                startTime = time;
                StartColor = Color;
                StartThickness = Thickness;
                StartRadius = Radius;
            }
            else startTime = 0;

            this.velocity = velocity;
        }

        public SphereRenderer(SphereDefinition def, MatrixD WorldMatrix, Vector3D velocity)
        {
            Center = WorldMatrix;
            Center.Translation = Vector3D.Transform(def.Pos1, WorldMatrix);

            Radius = def.Radius;
            Color = def.Color;
            Thickness = def.Thickness;
            time = def.TimeRendered;
            wireDivideRatio = def.wireDivideRatio;

            Material = def.Material;

            if (def.Fade)
            {
                startTime = time;
                StartColor = Color;
                StartThickness = Thickness;
            }
            else startTime = 0;

            this.velocity = velocity * def.VelocityInheritence;
        }
        public bool Update()
        {
            if (time <= 0)
                return true;

            Center.Translation = Center.Translation + velocity * (1 / 60f);

            if (startTime > 0)
            {
                Thickness = MathHelper.Lerp(StartThickness, 0f, (startTime - (float)time) / startTime);
                Color.W = MathHelper.Lerp(StartColor.W, 0f, (startTime - (float)time) / startTime);
            }

            Color c = Color;
            MySimpleObjectDraw.DrawTransparentSphere(ref Center, Radius, ref c, rasterizer, wireDivideRatio, Material, Material, Thickness, -1, null, BlendType);

            time--;
            return false;
        }
    }
}
