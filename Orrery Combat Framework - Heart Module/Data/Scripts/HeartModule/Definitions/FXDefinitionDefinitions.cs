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
using VRage.Game.ModAPI.Interfaces;
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
using VRage.Game.Models;
using VRage.Render.Particles;
using System.Linq.Expressions;
using System.IO;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Weapons;
using VRage;
using VRage.Collections;
using VRage.Voxels;
using ProtoBuf;
using SpaceEngineers.Game.ModAPI;
using System.Diagnostics.Contracts;
using VRageRender;
using static VRageRender.MyBillboard.BlendTypeEnum;
using static VRageRender.MyBillboard;


// all the data structures for the FX definitions, you can ignore this file it just ensures compilation successful.

/******************************************************************************************************************************************************
 *                                                                                                                                                    *
 *                                                            DO NOT MODIFY THIS FILE                                                                 *
 *                                                                                                                                                    *
 ******************************************************************************************************************************************************/


namespace VanillaPlusFramework.TemplateClasses
{
    [ProtoInclude(1002, typeof(VPFVisualEffectsDefinition))]
    public partial class VPFDefinition
    { }

    /// <summary>
    /// Base class for Line and Sphere definitions
    /// </summary>
    [ProtoContract]
    [ProtoInclude(2000, typeof(LineDefinition))]
    [ProtoInclude(2001, typeof(SphereDefinition))]
    [ProtoInclude(2002, typeof(TrailDefinition))]
    public class SimpleObjectDefinition
    {
        /// <summary>
        /// Origin for the drawing relative to the missile.
        /// -Z is the forward axis, +Y is the up axis.
        /// <para>
        /// Units: Vector3 (meters)
        /// </para>
        /// </summary>
        [ProtoMember(1)]
        public Vector3 Pos1;
        /// <summary>
        /// Color of the drawing.
        /// <para>
        /// Units: RGBA (X is R, Y is G, Z is B, W is A)
        /// </para>
        /// <para>
        /// A should always be greater than 0 so it is visible, and no value should ever be negative.
        /// </para>
        /// </summary>
        [ProtoMember(2)]
        public Vector4 Color;
        /// <summary>
        /// Thickness of the drawing.
        /// <para>
        /// Units: Unitless (eyeball required)
        /// </para>
        /// <para>
        /// Should always be greater than 0.
        /// </para>
        /// </summary>
        [ProtoMember(3)]
        public float Thickness;
        /// <summary>
        /// Multiplier to the velocity of the drawing, inherited from the missile.
        /// <para>
        /// Units: Multiplier (Percent/100)
        /// </para>
        /// </summary>
        [ProtoMember(4)]
        public float VelocityInheritence;
        /// <summary>
        /// Amount of ticks each render will be drawn. Be careful, one is spawned every tick.
        /// <para>
        /// Units: Ticks
        /// </para>
        /// <para>
        /// Should always be greater than 0.
        /// </para>
        /// </summary>
        [ProtoMember(5)]
        public int TimeRendered;

        /// <summary>
        /// Texture of the drawing.
        /// <para>
        /// Units: string (Use the MyStringId.GetOrCompute(string)) in the template
        /// </para>
        /// <para>
        /// Should always be a texture name.
        /// </para>
        /// </summary>
        [ProtoMember(6)]
        public MyStringId Material;
        /// <summary>
        /// Render type of the drawing.
        /// <para>
        /// Availible Types:
        /// <code>
        /// Standard
        /// AdditiveBottom
        /// AdditiveTop
        /// LDR
        /// PostPP
        /// SDR
        /// </code>
        /// </para>
        /// </summary>
        [ProtoMember(7)]
        public BlendTypeEnum BlendType;
        /// <summary>
        /// Should the drawing fade away over time by lerping its color alpha and thickness down to zero.
        /// <para>
        /// Units: bool
        /// </para>
        /// </summary>
        [ProtoMember(8)]
        public bool Fade;


        /// <summary>
        /// Ticks per spawn of the effect
        /// 1 spawns every tick.
        /// 2 spawns every other tick.
        /// 60 spawns every second.
        /// etc.
        /// <para>
        /// Units: Ticks
        /// </para>
        /// <para>
        /// Should always be greater than 0.
        /// </para>
        /// </summary>
        [ProtoMember(9)]
        public int TicksPerSpawn = 1;
    }

    /// <summary>
    /// Definition for drawing lines.
    /// </summary>
    [ProtoContract]
    public class LineDefinition : SimpleObjectDefinition
    {
        /// <summary>
        /// End point for the drawing relative to the missile.
        /// -Z is the forward axis, +Y is the up axis.
        /// <para>
        /// Units: Vector3 (meters)
        /// </para>
        /// </summary>
        [ProtoMember(1)]
        public Vector3 Pos2;
    }

    /// <summary>
    /// Definition for drawing spheres.
    /// </summary>
    [ProtoContract]
    public class SphereDefinition : SimpleObjectDefinition
    {
        /// <summary>
        /// Radius of the circle drawn
        /// <para>
        /// Units: Meters
        /// </para>
        /// <para>
        /// Should always be greater than 0.
        /// </para>
        /// </summary>
        [ProtoMember(1)]
        public float Radius;
        /// <summary>
        /// How high resolution the sphere should be.
        /// <para>
        /// Units: Divider
        /// </para>
        /// <para>
        /// Should always be greater than ~10, go higher than 36 with caution.
        /// </para>
        /// </summary>
        [ProtoMember(2)]
        public int wireDivideRatio;
        /// <summary>
        /// Rasterizer type of the drawing.
        /// <para>
        /// Availible Types:
        /// <code>
        /// Solid
        /// Wireframe
        /// SolidAndWireframe
        /// </code>
        /// </para>
        /// </summary>
        [ProtoMember(3)]
        public MySimpleObjectRasterizer Rasterizer;
    }

    /// <summary>
    /// Definition for drawing lines with the offset Pos1 from the missile's last known position to the offset Pos1 from the missile's current position.
    /// </summary>
    public class TrailDefinition : SimpleObjectDefinition { }

    /// <summary>
    /// only works with missile subtypeids
    /// </summary>
    [ProtoContract]
    public class VPFVisualEffectsDefinition : VPFDefinition
    {
        [ProtoMember(1)]
        public string subtypeName = null;

        [ProtoMember(2)]
        public List<SimpleObjectDefinition> DrawnObjects = null;

        public override string ToString()
        {
            return $"Subtype Name: {subtypeName}. Contains {DrawnObjects.Count} drawn objects.";
        }
    }
}
