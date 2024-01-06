using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using System;
using VRage.Game.Components;

namespace Heart_Module.Data.Scripts.HeartModule
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class HeartLoad : MySessionComponentBase
    {
        CriticalHandle handle;


        public override void LoadData()
        {
            HeartData.I = new HeartData();
            HeartData.I.Log.Log($"Start loading core...");

            handle = new CriticalHandle();
            handle.LoadData();
            HeartData.I.Net.LoadData();

            HeartData.I.IsSuspended = false;
            HeartData.I.Log.Log($"Finished loading core.");
        }

        public override void UpdateAfterSimulation()
        {
            // This has the power to shut down the server. Afaik the only way to do this is throwing an exception. Yeah.
            handle.Update();

            try
            {
                if (HeartData.I.IsSuspended)
                    return;
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
