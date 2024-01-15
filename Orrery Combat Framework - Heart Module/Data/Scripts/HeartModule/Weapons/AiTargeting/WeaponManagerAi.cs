using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class WeaponManagerAi : MySessionComponentBase
    {
        public WeaponManagerAi I;
        private Dictionary<IMyCubeGrid, GridAiTargeting> AiPreparedGrids = new Dictionary<IMyCubeGrid, GridAiTargeting>();
        private List<IMyCubeGrid> AiActivatedGrids = new List<IMyCubeGrid>();
        Dictionary<IMyCubeGrid, List<SorterWeaponLogic>> GridWeapons => WeaponManager.I.GridWeapons;

        public override void LoadData()
        {
            I = this;
            // Ensure this runs only on the server to avoid unnecessary calculations on clients
            if (!MyAPIGateway.Session.IsServer)
            {
                SetUpdateOrder(MyUpdateOrder.NoUpdate);
                return;
            }

            // Subscribe to grid addition and removal events
            HeartData.I.OnGridAdd += InitializeGridAI;
            HeartData.I.OnGridRemove += CloseGridAI;
        }

        protected override void UnloadData()
        {
            I = null;
            HeartData.I.OnGridAdd -= InitializeGridAI;
            HeartData.I.OnGridRemove -= CloseGridAI;
        }

        public override void UpdateAfterSimulation()
        {
            // AI update logic here, potentially throttled for performance
            UpdateAITargeting();
        }

        private void InitializeGridAI(IMyCubeGrid grid)
        {
            if (grid.Physics != null)
            {
                var aiTargeting = new GridAiTargeting(grid);
                bool hasConveyorSorter = CheckGridForConveyorSorter(grid);
                aiTargeting.EnableAi(hasConveyorSorter);

                string status = hasConveyorSorter ? "enabled" : "disabled";
                MyAPIGateway.Utilities.ShowNotification($"Grid AI {status} initialized for grid '{grid.DisplayName}'", 1000, "White");

                AiPreparedGrids.Add(grid, aiTargeting);

                if (hasConveyorSorter)
                {
                    AiActivatedGrids.Add(grid);
                }
            }
        }

        private bool CheckGridForConveyorSorter(IMyCubeGrid grid)
        {
            bool hasConveyorSorter = false;

            // Check the main grid for ConveyorSorter
            hasConveyorSorter |= HasConveyorSorterBlock(grid);

            // Check all subgrids (connected grids) for ConveyorSorter
            var connectedGrids = new List<IMyCubeGrid>(); // Create your own collection
            MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, connectedGrids);
            foreach (var subGrid in connectedGrids)
            {
                hasConveyorSorter |= HasConveyorSorterBlock(subGrid);
                if (hasConveyorSorter) break; // If found in any grid, no need to check further
            }
            return hasConveyorSorter;
        }

        private bool HasConveyorSorterBlock(IMyCubeGrid grid)
        {
            var blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks, b => b.FatBlock != null && b.FatBlock is IMyConveyorSorter);
            return blocks.Count > 0;
        }

        private void CloseGridAI(IMyCubeGrid grid)
        {
            if (grid.Physics != null)
            {
                if (AiPreparedGrids.ContainsKey(grid))
                {
                    AiPreparedGrids[grid].Close();
                    AiPreparedGrids.Remove(grid);
                    AiActivatedGrids.Remove(grid);
                    MyAPIGateway.Utilities.ShowNotification($"Grid AI closed for grid '{grid.DisplayName}'", 1000, "White");
                }
                else
                {
                    MyAPIGateway.Utilities.ShowNotification($"Attempted to close Grid AI on a non-tracked grid: '{grid.DisplayName}'", 1000, "Red");
                }
            }
        }

        private void UpdateAITargeting()
        {
            foreach (var grid in AiActivatedGrids)
            {
                var gridAi = AiPreparedGrids[grid];
                gridAi.UpdateTargeting(); // Method to be implemented in GridAiTargeting class
            }
        }
    }
}
