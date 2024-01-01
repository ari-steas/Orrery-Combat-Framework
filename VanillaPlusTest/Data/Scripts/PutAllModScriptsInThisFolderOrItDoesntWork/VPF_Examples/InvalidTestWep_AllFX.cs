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

namespace Invalid.AllFXTest /// Set namespace name to something else, preferably something no other mod uses. Can be the same as other definitions
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
            new VPFVisualEffectsDefinition // lines are lines, this one for example is a tracer
            {
                subtypeName = "Example_LineTracer", // name of the effect definition to be used in VPFAmmoDefinitions
                DrawnObjects = new List<SimpleObjectDefinition>
                {
                    new LineDefinition
                    {
                        Pos1 = new Vector3(0 ,0 , 4), // start point of the line
                        Pos2 = new Vector3(0 ,0 , -4), // end point of the line
                        //line from 4 meters in front to 4 meters behind the actual missile

                        Thickness = 0.1f, // thickness of the line
                        Color = new Vector4(1f, 0.7f, 0.4f, 1f) * 10f, // color of the line. The Vector4 is RGBA and is then multiplied to get bloom.
                        // yellow-orange

                        BlendType = Standard, // blend type (how it is rendered) of the line, using standard so it renders normally
                        // Standard
                        // AdditiveBottom
                        // AdditiveTop
                        // LDR
                        // PostPP
                        // SDR
                        
                        Material = MyStringId.GetOrCompute("ProjectileTrailLine"), // use the texture ProjectileTrailLine, the one used for vanilla projectile

                        TimeRendered = 1, // time the line is rendered
                        // FX definitions with a time render of 1 can look like actual parts of the missile, and has some memory savings
                        VelocityInheritence = 1f, // how much of the missile's velocity is inherited for the line. 1 = 100%, -1 is 100% in the other direction.
                        // velocity doesn't matter when time rendered is 1
                        Fade = true, // Should the line fade over time, lerping its color A and thickness towards zero
                        // fade doesn't matter when time rendered is 1
                        TicksPerSpawn = 1 // ticks per spawn  - 1 to spawn every tick, 2 to spawn every other, 60 to spawn every second, etc
                        // spawn every tick so the tracer doesn't blink
                    },
                }
            },

            new VPFVisualEffectsDefinition
            {
                subtypeName = "Example_Sphere", // name of the effect definition to be used in VPFAmmoDefinitions
                DrawnObjects = new List<SimpleObjectDefinition>
                {
                    new SphereDefinition
                    {
                        Pos1 = new Vector3(0, 0, 0), // center of the sphere
                        Radius = 1f, // radius of the sphere
                        Rasterizer = SolidAndWireframe, // how will the sphere will be rendered - solid, wireframe, or both
                        // Solid
                        // Wireframe
                        // SolidAndWireframe

                        // render the projectile as a 1m sphere for a projectile

                        wireDivideRatio = 12, // controls the resolution of the sphere, should be greater than 10-ish
                        // smi low-poly but nobody is going to notice when the missile is going 400m/s

                        Thickness = 1f, // thickness of the sphere. Unsure what it does.
                        Color = new Vector4(1f, 1f, 1f, 1f) * 10f, // color of the sphere. The Vector4 is RGBA and is then multiplied to get bloom.

                        // solid white except that the cyan texture overrides it so its actually cyan. Alpha does matter so it gets bloom though

                        BlendType = Standard, // blend type (how it is rendered) of the sphere
                        // Standard
                        // AdditiveBottom
                        // AdditiveTop
                        // LDR
                        // PostPP
                        // SDR
                        
                        Material = MyStringId.GetOrCompute("Cyan"), // there is in fact a keen texture file called Cyan.dds in Content\Textures\Debug, and it is just a square of solid cyan. For spheres, anything other than a repeating pattern looks really bad.

                        // render the sphere each tick for one tick where the missile is, so fade and velocity doesn't matter

                        TimeRendered = 1, // time the sphere is rendered
                        VelocityInheritence = 1f, // how much of the missile's velocity is inherited for the sphere. 1 = 100%, -1 is 100% in the other direction.

                        Fade = false, // Should the sphere fade over time, lerping its color A and thickness towards zero

                        TicksPerSpawn = 1 // ticks per spawn  - 1 to spawn every tick, 2 to spawn every other, 60 to spawn every second, etc
                    },
                }
            },

            new VPFVisualEffectsDefinition
            {
                subtypeName = "Example_Trail", // name of the effect definition to be used in VPFAmmoDefinitions
                DrawnObjects = new List<SimpleObjectDefinition>
                {
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

            new VPFVisualEffectsDefinition
            {
                subtypeName = "Example_Flare",
                DrawnObjects = new List<SimpleObjectDefinition>
                {
                    new LineDefinition
                    {
                        Pos1 = new Vector3(0, 0, 1),
                        Pos2 = new Vector3(0, 0, -1),
                        // really fat line so it doesn't look like one
                        Thickness = 1f,
                        Color = new Vector4(1f, 0.7f, 0.5f, 0.003f) * 100f,
                        // yellowish-white color with insane bloom
                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("ProjectileTrailLine"), // personal looks

                        TimeRendered = 1, // view as part of the missile

                        // note: several variables are missing, as they are not needed so they can just be their default values
                    },
                }
            },


            new VPFVisualEffectsDefinition
            {
                subtypeName = "Example_CoolRailgunEffect",
                DrawnObjects = new List<SimpleObjectDefinition>
                {
                    new LineDefinition // the actual "projectile part"
                    {
                        Pos1 = new Vector3(0, 0, -10),
                        Pos2 = new Vector3(0, 0, 11),

                        Thickness = 3f,
                        Color = new Vector4(0.7f, 0.9f, 1f, 0.01f) * 25f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("ProjectileTrailLine"),

                        TimeRendered = 1,
                    },

                    new LineDefinition // a secondary line to the main "projectile part"
                    {
                        Pos1 = new Vector3(0, 0, -10),
                        Pos2 = new Vector3(0, 0, 15),

                        Thickness = 2.8f,
                        Color = new Vector4(0.7f, 0.8f, 1f, 0.01f) * 22.5f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("ProjectileTrailLine"),

                        TimeRendered = 1,
                    },

                    new TrailDefinition // falloff trail of the railgun because it looks cool
                    {
                        Pos1 = new Vector3(0, 0, 0), // no offset so it looks like it comes from the projectile

                        Thickness = 2.5f,
                        Color = new Vector4(0.4f, 0.6f, 1f, 0.01f) * 25f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 50, // 50 ticks of render while fading, no velocity inheritance so it stays in place
                        VelocityInheritence = 0f,
                        Fade = true,
                    },
                }
            },

            new VPFVisualEffectsDefinition
            {
                subtypeName = "Example_MissileTrail",
                DrawnObjects = new List<SimpleObjectDefinition>
                {
                    new LineDefinition // standard projectile tracer
                    {
                        Pos1 = new Vector3(0, 0, 3),
                        Pos2 = new Vector3(0, 0, -3),

                        Thickness = 0.6f,
                        Color = new Vector4(1f, 0.6f, 0.4f, 0.03f) * 14f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("ProjectileTrailLine"),

                        TimeRendered = 1,
                        VelocityInheritence = 1f,
                    },

                    new TrailDefinition
                    {
                        Pos1 = new Vector3(0, 0, 3), // appear at the offset 3m behind so it looks like the trail is coming from the missile's line

                        Thickness = 0.8f,
                        Color = new Vector4(0.5f, 0.5f, 0.5f, 0.3f) * 1f, // transparent gray

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("Smoke_square"),
                        // smoke coming from the back of the missile, no this doesn't actually look that good but its reasonable
                        // 200 tick render w/ fade so the smoke fades semi-slowly
                        TimeRendered = 200,
                        VelocityInheritence = -0.1f, // render as if moving back slightly 
                        Fade = true,
                    },
                }
            },

            new VPFVisualEffectsDefinition // unfortunately, when you have a lot of things, it gets out of hand but should be fairly readable. I'll let you put this on a projectile and see what it does
                                           // this is technically copied from a private server's superweapon, so maybe not use without some changes?
            {
                subtypeName = "Example_FuckoffNukeTrail",
                DrawnObjects = new List<SimpleObjectDefinition>
                {
                    // innermost
                    new TrailDefinition
                    {
                        Pos1 = new Vector3(3.53553390593, 3.53553390593, -35), // 5*sin(45 degrees)

                        Thickness = 4f,
                        Color = new Vector4(0.6f, 0.8f, 1f, 0.01f) * 4f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 280,
                        VelocityInheritence = 0f,
                        Fade = true,
                    },

                    new TrailDefinition
                    {
                        Pos1 = new Vector3(3.53553390593, -3.53553390593, -35),

                        Thickness = 4f,
                        Color = new Vector4(0.6f, 0.8f, 1f, 0.01f) * 4f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 280,
                        VelocityInheritence = 0f,
                        Fade = true,
                    },

                    new TrailDefinition
                    {
                        Pos1 = new Vector3(-3.53553390593, 3.53553390593, -35),

                        Thickness = 4f,
                        Color = new Vector4(0.6f, 0.8f, 1f, 0.01f) * 4f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 280,
                        VelocityInheritence = 0f,
                        Fade = true,
                    },

                    new TrailDefinition
                    {
                        Pos1 = new Vector3(-3.53553390593, -3.53553390593, -35),

                        Thickness = 4f,
                        Color = new Vector4(0.6f, 0.8f, 1f, 0.01f) * 4f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 280,
                        VelocityInheritence = 0f,
                        Fade = true,
                    },

                    // layer 1
                    new TrailDefinition
                    {
                        Pos1 = new Vector3(0, 12.5, -6),

                        Thickness = 3f,
                        Color = new Vector4(0.6f, 0.8f, 1f, 0.01f) * 12f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 200,
                        VelocityInheritence = 0f,
                        Fade = true,
                    },

                    new TrailDefinition
                    {
                        Pos1 = new Vector3(0, -12.5, -6),

                        Thickness = 3f,
                        Color = new Vector4(0.6f, 0.8f, 1f, 0.01f) * 12f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 200,
                        VelocityInheritence = 0f,
                        Fade = true,
                    },

                    new TrailDefinition
                    {
                        Pos1 = new Vector3(12.5, 0, -6),

                        Thickness = 3f,
                        Color = new Vector4(0.6f, 0.8f, 1f, 0.01f) * 12f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 200,
                        VelocityInheritence = 0f,
                        Fade = true,
                    },

                    new TrailDefinition
                    {
                        Pos1 = new Vector3(-12.5, 0, -6),

                        Thickness = 3f,
                        Color = new Vector4(0.6f, 0.8f, 1f, 0.01f) * 12f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 200,
                        VelocityInheritence = 0f,
                        Fade = true,
                    },
                    // layer 2

                    new TrailDefinition
                    {
                        Pos1 = new Vector3(17.6776695, 17.6776695, 30), // 25*sin(45 deg)

                        Thickness = 3f,
                        Color = new Vector4(0.4f, 0.6f, 1f, 0.01f) * 10f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 150,
                        VelocityInheritence = 0f,
                        Fade = true,
                    },

                    new TrailDefinition
                    {
                        Pos1 = new Vector3(17.6776695, -17.6776695, 30),

                        Thickness = 3f,
                        Color = new Vector4(0.4f, 0.6f, 1f, 0.01f) * 10f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 150,
                        VelocityInheritence = 0f,
                        Fade = true,
                    },

                    new TrailDefinition
                    {
                        Pos1 = new Vector3(-17.6776695, 17.6776695, 30),

                        Thickness = 3f,
                        Color = new Vector4(0.4f, 0.6f, 1f, 0.01f) * 10f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 150,
                        VelocityInheritence = 0f,
                        Fade = true,
                    },

                    new TrailDefinition
                    {
                        Pos1 = new Vector3(-17.6776695, -17.6776695, 30),

                        Thickness = 3f,
                        Color = new Vector4(0.4f, 0.6f, 1f, 0.01f) * 10f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 150,
                        VelocityInheritence = 0f,
                        Fade = true,
                    },

                    // outer layer

                    new TrailDefinition
                    {
                        Pos1 = new Vector3(0, 37.5, 100),

                        Thickness = 3f,
                        Color = new Vector4(0.3f, 0.4f, 1f, 0.01f) * 6f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 100,
                        VelocityInheritence = 0f,
                        Fade = true,
                    },

                    new TrailDefinition
                    {
                        Pos1 = new Vector3(0, -37.5, 100),

                        Thickness = 2f,
                        Color = new Vector4(0.3f, 0.4f, 1f, 0.01f) * 6f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 100,
                        VelocityInheritence = 0f,
                        Fade = true,
                    },

                    new TrailDefinition
                    {
                        Pos1 = new Vector3(37.5, 0, 100),

                        Thickness = 2f,
                        Color = new Vector4(0.3f, 0.4f, 1f, 0.01f) * 6f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 100,
                        VelocityInheritence = 0f,
                        Fade = true,
                    },

                    new TrailDefinition
                    {
                        Pos1 = new Vector3(-37.5, 0, 100),

                        Thickness = 2f,
                        Color = new Vector4(0.3f, 0.4f, 1f, 0.01f) * 6f,

                        BlendType = Standard,
                        Material = MyStringId.GetOrCompute("WeaponLaser"),

                        TimeRendered = 100,
                        VelocityInheritence = 0f,
                        Fade = true,
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
