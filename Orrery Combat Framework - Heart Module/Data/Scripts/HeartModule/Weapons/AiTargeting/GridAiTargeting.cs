using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting
{
    internal class GridAiTargeting
    {
        IMyCubeGrid Grid;
        List<SorterWeaponLogic> Weapons => WeaponManager.I.GridWeapons[Grid];
        public bool IsAiEnabled { get; set; }

        public GridAiTargeting(IMyCubeGrid grid)
        {
            Grid = grid;
            Grid.OnBlockAdded += Grid_OnBlockAdded;
        }

        private void Grid_OnBlockAdded(IMySlimBlock obj)
        {

        }

        public void EnableAi(bool enable)
        {
            IsAiEnabled = enable;
            MyAPIGateway.Utilities.ShowNotification("Activated Ai: " + enable);
        }

        public void UpdateTargeting()
        {
            ScanForTargets();
            // Other targeting logic here
        }

        private void ScanForTargets()
        {
            if (!IsAiEnabled)
                return;

            BoundingSphereD sphere = new BoundingSphereD(Grid.PositionComp.WorldAABB.Center, 1000.0);

            List<MyEntity> entities = new List<MyEntity>();
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, entities);

            foreach (MyEntity entity in entities)
            {
                MyCubeGrid targetGrid = entity as MyCubeGrid;
                if (targetGrid != null && entity.EntityId != Grid.EntityId && entity.Physics != null)
                {
                    // Apply your custom filters here
                    if (ShouldConsiderTarget(targetGrid))
                    {
                        double distance = Vector3D.Distance(Grid.PositionComp.WorldAABB.Center, targetGrid.PositionComp.WorldAABB.Center);
                        MyAPIGateway.Utilities.ShowNotification($"{Grid.DisplayName} is {distance:F0} meters from {targetGrid.DisplayName}", 1000 / 60, "White");

                        // Additional logic to handle the valid target
                    }
                }
            }
        }


        private bool ShouldConsiderTarget(MyCubeGrid targetGrid)
        {
            // Example filters
            bool isLargeGrid = targetGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large;
            bool isEnemy = GetRelationsToGrid(targetGrid) == MyRelationsBetweenPlayerAndBlock.Enemies;

            // Replace these with your actual settings/toggles
            bool targetLargeGrids = true; // Replace with your setting for targeting large grids
            bool targetEnemies = false;    // Replace with your setting for targeting enemies

            return (isLargeGrid && targetLargeGrids) && (!isEnemy || targetEnemies);
        }

        private MyRelationsBetweenPlayerAndBlock GetRelationsToGrid(MyCubeGrid grid)
        {
            // Implement your logic to determine the relationship to the grid
            // This could be friend, enemy, neutral, etc.
            return MyRelationsBetweenPlayerAndBlock.Neutral; // Placeholder return
        }

        public void UpdateAvailableTargets(List<IMyCubeGrid> allGrids, List<IMyCharacter> allCharacters, List<uint> allProjectiles)
        {

        }

        public void Close()
        {

        }
    }
}
