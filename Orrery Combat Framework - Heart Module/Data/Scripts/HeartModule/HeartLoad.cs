using Heart_Module.Data.Scripts.HeartModule.Definitions;
using Heart_Module.Data.Scripts.HeartModule.Definitions.ApiHandler;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using RichHudFramework.Client;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Hiding;

namespace Heart_Module.Data.Scripts.HeartModule
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, priority: int.MaxValue)]
    internal class HeartLoad : MySessionComponentBase
    {
        private static HeartLoad I;

        CriticalHandle handle;
        ApiSender apiSender;
        DefinitionReciever definitionReciever;
        CommandHandler commands;
        ProjectileManager projectileManager;
        int remainingDegradedModeTicks = 300;

        public override void LoadData()
        {
            I = this;
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

                WeaponDefinitionManager.I = new WeaponDefinitionManager();
                ProjectileDefinitionManager.I = new ProjectileDefinitionManager();
                HeartData.I.Log.Log($"Initialized DefinitionManagers");

                definitionReciever = new DefinitionReciever();
                definitionReciever.LoadData();
                HeartData.I.Log.Log($"Initialized DefinitionReciever");

                apiSender = new ApiSender();
                apiSender.LoadData();
                HeartData.I.Log.Log($"Initialized ApiSender");

                commands = new CommandHandler();
                commands.Init();
                HeartData.I.Log.Log($"Initialized CommandHandler");

                projectileManager = new ProjectileManager();
                HeartData.I.Log.Log($"Started ProjectileManager.");

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

                if (!HeartData.I.IsLoaded) // Definitions can load after blocks do :)
                {
                    MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
                    MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;

                    MyAPIGateway.Entities.GetEntities(null, ent =>
                    {
                        OnEntityAdd(ent);
                        return false;
                    });
                    HeartData.I.IsLoaded = true;

                    HideSorterControls.DoOnce();
                    SorterWeaponTerminalControls.DoOnce(ModContext);
                }

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

                projectileManager.UpdateAfterSimulation();
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex);
            }
        }

        public static void EnterDegradedMode(int ticks)
        {
            I.remainingDegradedModeTicks = ticks;
            HeartData.I.DegradedMode = true;
        }

        public override void UpdatingStopped()
        {
            HeartData.I.IsPaused = true;
        }

        protected override void UnloadData()
        {
            HeartData.I.Log.Log($"Start closing core...");
            HeartData.I.IsSuspended = true;

            commands.Close();

            handle.UnloadData();
            HeartData.I.Net.UnloadData();
            HeartData.I.Log.Log($"Unloaded HeartNetwork");

            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;

            projectileManager.UnloadData();
            HeartData.I.Log.Log($"Closed ProjectileManager");

            definitionReciever.UnloadData();
            WeaponDefinitionManager.I = null;
            ProjectileDefinitionManager.I = null;
            HeartData.I.Log.Log($"Closed DefinitionManagers");

            apiSender.UnloadData();

            HeartData.I.Log.Log($"Closing core, log finishes here.");
            HeartData.I.Log.Close();
            HeartData.I = null;

            I = null;
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

        public static void ResetDefinitions()
        {
            WeaponDefinitionManager.ClearDefinitions();

            ProjectileDefinitionManager.ClearDefinitions();

            // Re-request definitions
            I.definitionReciever.UnloadData();
            I.definitionReciever.LoadData();
        }
    }
}
