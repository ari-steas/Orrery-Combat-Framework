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
        private Dictionary<IMyCubeGrid, GridAiTargeting> GridAITargeting = new Dictionary<IMyCubeGrid, GridAiTargeting>();
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
            // Check if the grid has valid physics before initializing AI
            if (grid.Physics != null)
            {
                GridAITargeting.Add(grid, new GridAiTargeting(grid));
                //debug shownotification
                MyAPIGateway.Utilities.ShowNotification("Grid AI Initialized on " + grid, 1000, "White");
            }
        }

        private void CloseGridAI(IMyCubeGrid grid)
        {
            // Check if the GridAITargeting dictionary contains the grid before trying to access it
            if (GridAITargeting.ContainsKey(grid))
            {
                GridAITargeting[grid].Close();
                GridAITargeting.Remove(grid);
                MyAPIGateway.Utilities.ShowNotification("Grid AI closed on " + grid, 1000, "White");
            }
            else
            {
                // Handle the case where the grid is not in the dictionary, if necessary
                MyAPIGateway.Utilities.ShowNotification("Attempted to close Grid AI on a non-tracked grid: " + grid, 1000, "Red");
            }
        }


        private void UpdateAITargeting()
        {
            foreach (var gridAi in GridAITargeting)
            {
                // Execute AI targeting logic only for grids with SorterWeaponLogic
                if (GridWeapons.ContainsKey(gridAi.Key))
                {
                    gridAi.Value.UpdateTargeting(); // Method to be implemented in GridAiTargeting class
                }
            }
        }

        // The GridAiTargeting class should handle the AI logic for each grid
        // Including targeting range and target selection logic
    }
}
