using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using RichHudFramework.Client;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Heart_Module.Data.Scripts.HeartModule
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, priority: int.MinValue)]
    internal class HeartLoad : MySessionComponentBase
    {
        CriticalHandle handle;
        int remainingDegradedModeTicks = 300;

        public override void LoadData()
        {
            HeartData.I = new HeartData();
            HeartData.I.Log.Log($"Start loading core...");

            handle = new CriticalHandle();
            handle.LoadData();

            try
            {
                HeartData.I.Net.LoadData();

                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    RichHudClient.Init("HeartModule", () => { }, () => { });
                    HeartData.I.Log.Log($"Loaded RichHudClient");
                }
                else
                    HeartData.I.Log.Log($"Skipped loading RichHudClient");

                MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
                MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;

                HeartData.I.IsSuspended = false;
                HeartData.I.Log.Log($"Finished loading core.");
            }
            catch (Exception ex)
            {
                CriticalHandle.ThrowCriticalException(ex, typeof(HeartLoad));
            }
        }

        public override void UpdateAfterSimulation()
        {
            // This has the power to shut down the server. Afaik the only way to do this is throwing an exception. Yeah.
            handle.Update();

            try
            {
                if (HeartData.I.IsSuspended)
                    return;
                HeartData.I.IsPaused = false;

                if (!MyAPIGateway.Utilities.IsDedicated && HeartData.I.SteamId == 0)
                    HeartData.I.SteamId = MyAPIGateway.Session?.Player?.SteamUserId ?? 0;

                HeartData.I.Net.Update(); // Update network stats

                if (MyAPIGateway.Session.IsServer) // Get players
                {
                    HeartData.I.Players.Clear(); // KEEN DOESN'T. CLEAR. THE LIST. AUTOMATICALLY. AUGH. -aristeas
                    MyAPIGateway.Multiplayer.Players.GetPlayers(HeartData.I.Players);
                }

                if (MyAPIGateway.Physics.SimulationRatio < 0.7 && !HeartData.I.IsPaused) // Set degraded mode
                {
                    if (!HeartData.I.DegradedMode)
                    {
                        if (remainingDegradedModeTicks >= 60) // Wait 300 ticks before engaging degraded mode
                        {
                            HeartData.I.DegradedMode = true;
                            if (MyAPIGateway.Session.IsServer)
                                MyAPIGateway.Utilities.SendMessage("[OCF] Entering degraded mode!");
                            MyAPIGateway.Utilities.ShowMessage("[OCF]", "Entering client degraded mode!");
                            remainingDegradedModeTicks = 600;
                        }
                        else
                            remainingDegradedModeTicks++;
                    }
                }
                else if (MyAPIGateway.Physics.SimulationRatio > 0.87)
                {
                    if (remainingDegradedModeTicks <= 0 && HeartData.I.DegradedMode)
                    {
                        HeartData.I.DegradedMode = false;
                        if (MyAPIGateway.Session.IsServer)
                            MyAPIGateway.Utilities.SendMessage("[OCF] Exiting degraded mode.");
                        MyAPIGateway.Utilities.ShowMessage("[OCF]", "Exiting client degraded mode.");
                        remainingDegradedModeTicks = 0;
                    }
                    else if (remainingDegradedModeTicks > 0)
                        remainingDegradedModeTicks--;
                }
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex);
            }
        }

        public override void UpdatingStopped()
        {
            HeartData.I.IsPaused = true;
        }

        protected override void UnloadData()
        {
            handle.UnloadData();
            HeartData.I.Net.UnloadData();

            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;

            HeartData.I.Log.Log($"Closing core, log finishes here.");
            HeartData.I.Log.Close();
            HeartData.I = null;
        }

        private void OnEntityAdd(IMyEntity entity)
        {
            if (entity is IMyCubeGrid)
                HeartData.I?.OnGridAdd?.Invoke(entity as IMyCubeGrid);
        }

        private void OnEntityRemove(IMyEntity entity)
        {
            if (entity is IMyCubeGrid)
                HeartData.I?.OnGridRemove?.Invoke(entity as IMyCubeGrid);
        }
    }
}
