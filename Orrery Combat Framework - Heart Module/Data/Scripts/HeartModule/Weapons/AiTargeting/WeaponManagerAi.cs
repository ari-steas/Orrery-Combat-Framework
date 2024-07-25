using Sandbox.ModAPI;
using System.Collections.Generic;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class WeaponManagerAi : MySessionComponentBase
    {
        public static WeaponManagerAi I;

        private Dictionary<IMyCubeGrid, GridAiTargeting> GridTargetingMap = new Dictionary<IMyCubeGrid, GridAiTargeting>();
        private Dictionary<IMyCubeGrid, List<SorterWeaponLogic>> GridWeapons => WeaponManager.I.GridWeapons;

        public GridAiTargeting GetTargeting(IMyCubeGrid grid)
        {
            if (GridTargetingMap.ContainsKey(grid))
                return GridTargetingMap[grid];
            return null;
        }

        public override void LoadData()
        {
            if (!MyAPIGateway.Session.IsServer)
            {
                SetUpdateOrder(MyUpdateOrder.NoUpdate);
                return;
            }

            HeartData.I.OnGridAdd += InitializeGridAI;
            HeartData.I.OnGridRemove += CloseGridAI;
            I = this;

            HeartLog.Log("WeaponManagerAi: LoadData completed");
        }

        protected override void UnloadData()
        {
            HeartData.I.OnGridAdd -= InitializeGridAI;
            HeartData.I.OnGridRemove -= CloseGridAI;
            I = null;

            HeartLog.Log("WeaponManagerAi: UnloadData completed");
        }

        public override void UpdateAfterSimulation()
        {
            UpdateAITargeting();
        }

        private void InitializeGridAI(IMyCubeGrid grid)
        {
            if (grid.Physics == null) return;

            var aiTargeting = new GridAiTargeting(grid);
            GridTargetingMap.Add(grid, aiTargeting);

            HeartLog.Log($"WeaponManagerAi: Grid AI initialized for grid '{grid.DisplayName}' [{(aiTargeting.Enabled ? "ENABLED" : "DISABLED")}]");

            // Debug all turrets on this grid
            List<SorterWeaponLogic> weapons;
            if (GridWeapons.TryGetValue(grid, out weapons))
            {
                foreach (var weapon in weapons)
                {
                    var turret = weapon as SorterTurretLogic;
                    if (turret != null)
                    {
                        turret.DebugAiInitialization();
                    }
                }
            }
        }

        private void CloseGridAI(IMyCubeGrid grid)
        {
            if (grid.Physics == null) return;

            GridAiTargeting aiTargeting;
            if (GridTargetingMap.TryGetValue(grid, out aiTargeting))
            {
                aiTargeting.Close();
                GridTargetingMap.Remove(grid);
                HeartLog.Log($"WeaponManagerAi: Grid AI closed for grid '{grid.DisplayName}'");
            }
            else
            {
                HeartLog.Log($"WeaponManagerAi: Attempted to close Grid AI on a non-tracked grid: '{grid.DisplayName}'");
            }
        }

        private void UpdateAITargeting()
        {
            foreach (var targetingKvp in GridTargetingMap)
            {
                targetingKvp.Value.UpdateTargeting();
            }
        }
    }
}