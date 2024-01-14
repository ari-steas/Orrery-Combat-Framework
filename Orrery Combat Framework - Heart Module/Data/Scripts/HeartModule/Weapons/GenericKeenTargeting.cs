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
                                // Pass the player parameter when calling the filtering method
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

        private MyEntity FilterTargetBasedOnFactionRelation(MyEntity targetEntity, bool targetFriendlies, bool targetNeutrals, bool targetEnemies, bool targetUnowned, IMyPlayer player = null)
        {
            IMyCubeGrid grid = targetEntity as IMyCubeGrid;
            if (grid != null)
            {
                MyRelationsBetweenPlayerAndBlock relation = GetRelationsToGrid(grid, player);
                bool isFriendly = relation == MyRelationsBetweenPlayerAndBlock.Friends;
                bool isNeutral = relation == MyRelationsBetweenPlayerAndBlock.Neutral;
                bool isEnemy = relation == MyRelationsBetweenPlayerAndBlock.Enemies;

                // Check if the grid has no owners
                bool isUnowned = grid.BigOwners == null || (relation == MyRelationsBetweenPlayerAndBlock.NoOwnership || grid.BigOwners.Count == 0);

                // Display the faction relationship as a debug message
                MyAPIGateway.Utilities.ShowNotification($"Faction Relation: {relation}", 1000 / 60, VRage.Game.MyFontEnum.White);

                // Debug playerId and gridOwner values
                long playerId = player?.IdentityId ?? 0;
                long gridOwner = grid.BigOwners != null && grid.BigOwners.Count > 0 ? grid.BigOwners[0] : 0;
                MyAPIGateway.Utilities.ShowNotification($"Player ID: {playerId}", 1000 / 60, VRage.Game.MyFontEnum.White);
                MyAPIGateway.Utilities.ShowNotification($"Grid Owner ID: {gridOwner}", 1000 / 60, VRage.Game.MyFontEnum.White);

                // Check TryGetPlayerFaction
                IMyFaction playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
                IMyFaction ownerFaction = gridOwner != 0 ? MyAPIGateway.Session.Factions.TryGetPlayerFaction(gridOwner) : null;
                MyAPIGateway.Utilities.ShowNotification($"Player Faction: {playerFaction?.Tag}", 1000 / 60, VRage.Game.MyFontEnum.White);
                MyAPIGateway.Utilities.ShowNotification($"Owner Faction: {ownerFaction?.Tag}", 1000 / 60, VRage.Game.MyFontEnum.White);

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

        private MyRelationsBetweenPlayerAndBlock GetRelationsToGrid(IMyCubeGrid grid, IMyPlayer player)
        {
            if (grid.BigOwners == null || grid.BigOwners.Count == 0)
                return MyRelationsBetweenPlayerAndBlock.NoOwnership; // Unowned grid

            long playerId = player?.IdentityId ?? 0;
            long gridOwner = grid.BigOwners[0];

            if (playerId == gridOwner)
                return MyRelationsBetweenPlayerAndBlock.Owner; // Treat grids owned by the player as "friendly"

            IMyFaction playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
            IMyFaction ownerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(gridOwner);

            if (playerFaction != null && ownerFaction != null)
            {
                // Use the GetRelationBetweenFactions method to get the relationship between factions
                MyRelationsBetweenFactions relation = MyAPIGateway.Session.Factions.GetRelationBetweenFactions(playerFaction.FactionId, ownerFaction.FactionId);

                switch (relation)
                {
                    case MyRelationsBetweenFactions.Friends:
                        return MyRelationsBetweenPlayerAndBlock.Friends;
                    case MyRelationsBetweenFactions.Neutral:
                        return MyRelationsBetweenPlayerAndBlock.Neutral;
                    case MyRelationsBetweenFactions.Enemies:
                        return MyRelationsBetweenPlayerAndBlock.Enemies;
                    default:
                        return MyRelationsBetweenPlayerAndBlock.Neutral; // Default to neutral if not allies or enemies
                }
            }
            else if (ownerFaction == null)
            {
                return MyRelationsBetweenPlayerAndBlock.Neutral; // Treat as neutral if the owner has no faction
            }

            // If none of the above conditions are met, treat as neutral
            return MyRelationsBetweenPlayerAndBlock.Neutral;
        }
    }
}
