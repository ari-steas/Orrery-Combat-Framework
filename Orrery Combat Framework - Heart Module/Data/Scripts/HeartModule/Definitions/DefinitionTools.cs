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
using VanillaPlusFramework.TemplateClasses;


// tools for making & serializing definitions, you can ignore this file it just ensures compilation successful.

/******************************************************************************************************************************************************
 *                                                                                                                                                    *
 *                                                            DO NOT MODIFY THIS FILE                                                                 *
 *                                                                                                                                                    *
 ******************************************************************************************************************************************************/


namespace Heart_Module.Data.Scripts.HeartModule.Definitions
{
    public static class DefinitionTools
    {
        public const long ModMessageID = 2915780227;

        /// <summary>
        /// Converts an easier to look at 2D array into something that can be used without causing errors.
        /// </summary>
        /// <param name="array"></param>
        /// <returns>2D array in a list of structs containing one array each. Gets around ProtoBuf's no multidimensional or nested array limit.</returns>
        public static List<DoubleArray> ConvertToDoubleArrayList(double[,] array)
        {
            List<DoubleArray> returnval = new List<DoubleArray>();
            for (int i = 0; i < array.GetLength(0); i++)
            {
                double[] functionpt = new double[array.GetLength(1)];

                for (int j = 0; j < array.GetLength(1); j++)
                {
                    functionpt[j] = array[i, j];
                }

                returnval.Add(new DoubleArray(functionpt));
            }
            return returnval;
        }

        /// <summary>
        /// Gets the desired output for a given input from a piecewise polynomial function defined in the given List of DoubleArrays.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="function"></param>
        /// <returns>Output of the function at the input x</returns>
        public static double FunctionOutput(double x, List<DoubleArray> function)
        {
            double val = 0;

            for (int i = 0; i < function.Count; i++)
            {
                double NextFunctionValue;

                if (i + 1 >= function.Count)
                {
                    NextFunctionValue = double.MaxValue;
                }
                else
                {
                    NextFunctionValue = function[i + 1].array[0];
                }

                if (function[i].array[0] <= x && x < NextFunctionValue)
                {
                    for (int j = 1; j < function[i].array.Length; j++)
                    {
                        val = val + function[i].array[j] * Math.Pow(x, j - 1);
                    }
                    break;
                }
            }

            return val;
        }
        /// <summary>
        /// Converts the framework definition into a byte array to send to the API.
        /// </summary>
        /// <param name="def"></param>
        /// <returns>array of bytes to send</returns>
        public static byte[] DefinitionToMessage(VPFDefinition def)
        {
            return MyAPIGateway.Utilities.SerializeToBinary(def);
        }
    }
}
