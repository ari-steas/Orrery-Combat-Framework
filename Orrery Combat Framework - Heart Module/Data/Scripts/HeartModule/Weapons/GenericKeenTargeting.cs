using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Game;
using Sandbox.Game;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    public class GenericKeenTargeting
    {
        public MyEntity GetTarget(IMyCubeGrid grid, bool targetGrids, bool targetLargeGrids, bool targetSmallGrids,
                                  bool targetFriendlies, bool targetNeutrals, bool targetEnemies, bool targetUnowned)
        {
            if (grid == null)
            {
                MyAPIGateway.Utilities.ShowNotification("No grid found", 1000 / 60, VRage.Game.MyFontEnum.Red);
                return null;
            }

            var myCubeGrid = grid as MyCubeGrid;
            if (myCubeGrid != null)
            {
                MyShipController activeController = null;

                foreach (var block in myCubeGrid.GetFatBlocks<MyShipController>())
                {
                    if (block.NeedsPerFrameUpdate)
                    {
                        activeController = block;
                        break;
                    }
                }

                if (activeController != null && activeController.Pilot != null)
                {
                    var targetLockingComponent = activeController.Pilot.Components.Get<MyTargetLockingComponent>();
                    if (targetLockingComponent != null && targetLockingComponent.IsTargetLocked)
                    {
                        var targetEntity = targetLockingComponent.TargetEntity;
                        if (targetEntity != null && targetGrids)
                        {
                            bool isLargeGrid = targetEntity is IMyCubeGrid && ((IMyCubeGrid)targetEntity).GridSizeEnum == VRage.Game.MyCubeSize.Large;
                            bool isSmallGrid = targetEntity is IMyCubeGrid && ((IMyCubeGrid)targetEntity).GridSizeEnum == VRage.Game.MyCubeSize.Small;

                            if ((isLargeGrid && targetLargeGrids) || (isSmallGrid && targetSmallGrids))
                            {
                                // Filter the target based on faction relationship
                                var filteredTarget = FilterTargetBasedOnFactionRelation(targetEntity, targetFriendlies, targetNeutrals, targetEnemies, targetUnowned);

                                if (filteredTarget != null)
                                {
                                    MyAPIGateway.Utilities.ShowNotification("Target selected: " + filteredTarget.DisplayName, 1000 / 60, VRage.Game.MyFontEnum.Blue);
                                }
                                else
                                {
                                    MyAPIGateway.Utilities.ShowNotification("Target filtered out based on faction relationship", 1000 / 60, VRage.Game.MyFontEnum.Red);
                                }

                                return filteredTarget;
                            }
                        }
                    }
                }
            }

            MyAPIGateway.Utilities.ShowNotification("No valid target found", 1000 / 60, VRage.Game.MyFontEnum.Red);
            return null;
        }

        private MyEntity FilterTargetBasedOnFactionRelation(MyEntity targetEntity, bool targetFriendlies, bool targetNeutrals, bool targetEnemies, bool targetUnowned)
        {
            IMyCubeGrid grid = targetEntity as IMyCubeGrid;
            if (grid != null)
            {
                MyRelationsBetweenPlayerAndBlock relation = GetRelationsToGrid(grid);
                bool isFriendly = relation == MyRelationsBetweenPlayerAndBlock.Friends || relation == MyRelationsBetweenPlayerAndBlock.FactionShare;
                bool isNeutral = relation == MyRelationsBetweenPlayerAndBlock.NoOwnership || relation == MyRelationsBetweenPlayerAndBlock.Neutral;
                bool isEnemy = relation == MyRelationsBetweenPlayerAndBlock.Enemies;
                bool isUnowned = grid.BigOwners.Count == 0;

                if ((isFriendly && targetFriendlies) ||
                    (isNeutral && targetNeutrals) ||
                    (isEnemy && targetEnemies) ||
                    (isUnowned && targetUnowned))
                {
                    return targetEntity;
                }
            }

            return null;
        }

        private MyRelationsBetweenPlayerAndBlock GetRelationsToGrid(IMyCubeGrid grid)
        {
            if (grid.BigOwners.Count == 0)
                return MyRelationsBetweenPlayerAndBlock.NoOwnership; // Unowned grid

            long playerId = MyAPIGateway.Session.Player?.IdentityId ?? 0;
            long gridOwner = grid.BigOwners[0];

            IMyFaction ownerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(gridOwner);
            if (ownerFaction == null)
                return MyRelationsBetweenPlayerAndBlock.NoOwnership; // No faction, treat as unowned

            // Get the relationship status using the faction tag
            int relationInt = MyVisualScriptLogicProvider.GetRelationBetweenPlayerAndFaction(playerId, ownerFaction.Tag);
            return (MyRelationsBetweenPlayerAndBlock)relationInt; // Explicitly cast the int to MyRelationsBetweenPlayerAndBlock
        }
    }
}
