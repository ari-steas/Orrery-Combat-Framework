using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VanillaPlusFramework.TemplateClasses;
using VRage.Game.Components;
using VRage.Utils;

namespace Heart_Module.Data.Scripts.HeartModule.FX
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class FXRenderer : MySessionComponentBase
    {
        public static List<VPFVisualEffectsDefinition> Definitions = new List<VPFVisualEffectsDefinition>();

        public static List<IRenderObject> ObjectsToRender = new List<IRenderObject>();

        public static void OnDefinitionRecieved(VPFVisualEffectsDefinition def)
        {
            if (def.subtypeName == "" || def.subtypeName == null)
            {
                MyLog.Default.WriteLineAndConsole($"Error. Specified subtype in {def} is null or empty.");
                return;
            }
            if (!Definitions.Contains(def))
                Definitions.Add(def);
            else return;

            MyLog.Default.WriteLineAndConsole($"Definition {def} loaded");
        }
        public override void UpdateAfterSimulation()
        {
            for (int i = 0; i < ObjectsToRender.Count; i++)
            {
                if (ObjectsToRender[i].Update())
                {
                    ObjectsToRender.RemoveAt(i);
                }
            }
        }
    }

    public interface IRenderObject
    {
        bool Update();
    }
}
