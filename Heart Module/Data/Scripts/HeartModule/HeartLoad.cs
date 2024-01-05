using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;

namespace Heart_Module.Data.Scripts.HeartModule
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class HeartLoad : MySessionComponentBase
    {
        CriticalHandle handle;
        

        public override void LoadData()
        {
            HeartData.I.Log.Log($"Start loading core...");

            HeartData.I = new HeartData();
            handle = new CriticalHandle();
            handle.LoadData();
            HeartData.I.Net.LoadData();

            HeartData.I.Log.Log($"Finished loading core.");
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (HeartData.I.IsClosing)
                    return;

                handle.Update();
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex);
            }
        }

        protected override void UnloadData()
        {
            handle.UnloadData();
            HeartData.I.Net.UnloadData();
            HeartData.I.Log.Log($"Closing core, log finishes here.");
            HeartData.I = null;
        }
    }
}
