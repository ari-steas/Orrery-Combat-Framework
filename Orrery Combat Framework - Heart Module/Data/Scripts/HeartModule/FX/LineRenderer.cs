using Heart_Module.Data.Scripts.HeartModule.Definitions;
using Sandbox.ModAPI;
using VanillaPlusFramework.TemplateClasses;
//using VanillaPlusFramework.TemplateClasses;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace Heart_Module.Data.Scripts.HeartModule.FX
{
    public class LineRenderer : IRenderObject
    {
        MyStringId Material;

        BlendTypeEnum BlendType;

        Vector3D Origin;
        Vector3D End;
        Vector4 StartColor;
        Vector4 Color;
        float Thickness;
        float StartThickness;
        int time;
        int startTime;
        Vector3D velocity;
        public LineRenderer(Vector3D Origin, Vector3D End, Vector4 Color, float Thickness, int time, bool Fade, Vector3D velocity, string Material, BlendTypeEnum blendType)
        {
            this.Origin = Origin;
            this.Color = Color;
            this.Thickness = Thickness;
            this.time = time;
            this.Material = MyStringId.GetOrCompute(Material);

            if (Fade)
            {
                startTime = time;
                StartColor = Color;
                StartThickness = Thickness;
            }
            else startTime = 0;

            this.End = End;
            this.velocity = velocity;
            BlendType = blendType;
        }

        public LineRenderer(LineDefinition def, MatrixD WorldMatrix, Vector3D velocity)
        {

            Origin = Vector3D.Transform(def.Pos1, WorldMatrix);
            Color = def.Color;
            Thickness = def.Thickness;
            time = def.TimeRendered;
            Material = def.Material;

            End = Vector3D.Transform(def.Pos2, WorldMatrix);
            this.velocity = velocity * def.VelocityInheritence;
            BlendType = def.BlendType;


            if (def.Fade)
            {
                startTime = time;
                StartColor = Color;
                StartThickness = Thickness;
            }
            else startTime = 0;
        }

        public LineRenderer(TrailDefinition def, MatrixD WorldMatrix, Vector3D velocity, MatrixD LastWorldMatrix)
        {

            Origin = Vector3D.Transform(def.Pos1, LastWorldMatrix);
            Color = def.Color;
            Thickness = def.Thickness;
            time = def.TimeRendered;
            Material = def.Material;

            End = Vector3D.Transform(def.Pos1, WorldMatrix);
            this.velocity = velocity * def.VelocityInheritence;
            BlendType = def.BlendType;


            if (def.Fade)
            {
                startTime = time;
                StartColor = Color;
                StartThickness = Thickness;
            }
            else startTime = 0;
        }

        public bool Update()
        {
            if (time <= 0)
                return true;

            Origin += velocity * (1 / 60f);
            End += velocity * (1 / 60f);

            if (startTime > 0)
            {
                Color.W = MathHelper.Lerp(StartColor.W, 0f, (startTime - (float)time) / startTime);
                Thickness = MathHelper.Lerp(StartThickness, 0f, (startTime - (float)time) / startTime);
            }
            MySimpleObjectDraw.DrawLine(Origin, End, Material, ref Color, Thickness, BlendType);
            time--;
            return false;
        }
    }
}
