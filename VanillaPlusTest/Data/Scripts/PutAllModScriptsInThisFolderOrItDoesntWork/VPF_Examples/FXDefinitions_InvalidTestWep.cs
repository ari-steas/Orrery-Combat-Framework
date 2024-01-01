using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.EntityComponents;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using VanillaPlusFramework.TemplateClasses;
using static VRageRender.MyBillboard.BlendTypeEnum;
using VRageRender;
using static VRage.Game.MySimpleObjectRasterizer;

namespace Invalid.FXDefinitionTest /// Set namespace name to something else, preferably something no other mod uses. Can be the same as other definitions
{
    /// <summary>
    /// Recommend renaming the file.
    /// Recommend Visual Studio for editing this, all fields have descriptions that visual studio will display on hover over. Make a project solution to put all files in as well so the descriptions show up when hovered over.
    /// Contains all Vanilla+ Stats for all subtypes. Note: Vanilla Ammo.sbc is still used
    /// Note: All implented stats are here. There are some unimplimented ones not shown, do not use them or errors will be thrown.
    /// You can have multiple files, just change the class name ("VPFAmmoDefinitions" in front of public class) or the namespace
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class VPFFXDefinitions : MySessionComponentBase
    {
        List<VPFVisualEffectsDefinition> TrailDefinitions = new List<VPFVisualEffectsDefinition>() {
            /*
             * DON'T MODIFY ANYTHING ABOVE THIS LINE EXCEPT THE NAMESPACE & CLASS NAME
             */
            new VPFVisualEffectsDefinition
            {
                subtypeName = "", // name of the effect definition to be used in VPFAmmoDefinitions
                DrawnObjects = new List<SimpleObjectDefinition>
                {
                    new LineDefinition
                    {
                        Pos1 = new Vector3(0 ,0 ,0), // start point of the line
                        Pos2 = new Vector3(-5 ,0 ,0), // end point of the line

                        Thickness = 1f, // thickness of the line
                        Color = new Vector4(1f, 1f, 1f, 1f) * 10f, // color of the line. The Vector4 is RGBA and is then multiplied to get bloom.

                        BlendType = Standard, // blend type (how it is rendered) of the line
                        // Standard
                        // AdditiveBottom
                        // AdditiveTop
                        // LDR
                        // PostPP
                        // SDR
                        
                        Material = MyStringId.GetOrCompute("ProjectileTrailLine"), // texture name of the line

                        TimeRendered = 1, // time the line is rendered
                        VelocityInheritence = 1f, // how much of the missile's velocity is inherited for the line. 1 = 100%, -1 is 100% in the other direction.

                        Fade = true, // Should the line fade over time, lerping its color A and thickness towards zero

                        TicksPerSpawn = 1 // ticks per spawn  - 1 to spawn every tick, 2 to spawn every other, 60 to spawn every second, etc
                    },
                    new SphereDefinition
                    {
                        Pos1 = new Vector3(0, 0, 0), // center of the sphere
                        Radius = 1f, // radius of the sphere
                        Rasterizer = SolidAndWireframe, // how will the sphere will be rendered - solid, wireframe, or both
                        // Solid
                        // Wireframe
                        // SolidAndWireframe

                        wireDivideRatio = 12, // controls the resolution of the sphere, should be greater than 10-ish

                        Thickness = 1f, // thickness of the sphere. Unsure what it does.
                        Color = new Vector4(1f, 1f, 1f, 1f) * 10f, // color of the sphere. The Vector4 is RGBA and is then multiplied to get bloom.

                        BlendType = Standard, // blend type (how it is rendered) of the sphere
                        // Standard
                        // AdditiveBottom
                        // AdditiveTop
                        // LDR
                        // PostPP
                        // SDR
                        
                        Material = MyStringId.GetOrCompute("WeaponLaser"), // texture name of the sphere

                        TimeRendered = 1, // time the sphere is rendered
                        VelocityInheritence = 1f, // how much of the missile's velocity is inherited for the sphere. 1 = 100%, -1 is 100% in the other direction.

                        Fade = false, // Should the sphere fade over time, lerping its color A and thickness towards zero

                        TicksPerSpawn = 1 // ticks per spawn  - 1 to spawn every tick, 2 to spawn every other, 60 to spawn every second, etc
                    },
                    new TrailDefinition
                    {
                        Pos1 = new Vector3(0 ,0 ,0), // trail offset, line goes from this point relative to the missile's previous position to the point relative to the missile's current position

                        Thickness = 1f, // thickness of the line
                        Color = new Vector4(1f, 1f, 1f, 1f) * 10f, // color of the line. The Vector4 is RGBA and is then multiplied to get bloom.

                        BlendType = Standard, // blend type (how it is rendered) of the line
                        // Standard
                        // AdditiveBottom
                        // AdditiveTop
                        // LDR
                        // PostPP
                        // SDR
                        
                        Material = MyStringId.GetOrCompute("smoke_square"), // texture name of the line

                        TimeRendered = 1, // time the line is rendered
                        VelocityInheritence = 1f, // how much of the missile's velocity is inherited for the line. 1 = 100%, -1 is 100% in the other direction.

                        Fade = true, // Should the line fade over time, lerping its color A and thickness towards zero

                        TicksPerSpawn = 1 // ticks per spawn  - 1 to spawn every tick, 2 to spawn every other, 60 to spawn every second, etc
                    },
                }
            },


            /*
             * DON'T MODIFY ANYTHING BELOW THIS LINE
             */
        };

        public override void BeforeStart()
        {
            foreach (VPFVisualEffectsDefinition def in TrailDefinitions)
            {
                MyAPIUtilities.Static.SendModMessage(DefinitionTools.ModMessageID, DefinitionTools.DefinitionToMessage(def));
            }
        }
    }
}
