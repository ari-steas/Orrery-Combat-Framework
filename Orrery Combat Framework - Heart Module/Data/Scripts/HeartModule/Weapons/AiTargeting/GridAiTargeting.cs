using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
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

            BoundingSphereD sphere = new BoundingSphereD(Grid.PositionComp.WorldAABB.Center, 1000.0); // Set your desired scan range in meters.

            List<MyEntity> entities = new List<MyEntity>();
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, entities);

            foreach (MyEntity entity in entities)
            {
                if (entity is MyCubeGrid && entity.EntityId != Grid.EntityId && entity.Physics != null)
                {
                    double distance = Vector3D.Distance(Grid.PositionComp.WorldAABB.Center, entity.PositionComp.WorldAABB.Center);
                    MyAPIGateway.Utilities.ShowNotification($"{Grid.DisplayName} is {distance:F0} meters from {entity.DisplayName}", 1000 / 60, "White");
                }
            }
        }

        public void UpdateAvailableTargets(List<IMyCubeGrid> allGrids, List<IMyCharacter> allCharacters, List<uint> allProjectiles)
        {

        }

        public void Close()
        {

        }
    }
}
