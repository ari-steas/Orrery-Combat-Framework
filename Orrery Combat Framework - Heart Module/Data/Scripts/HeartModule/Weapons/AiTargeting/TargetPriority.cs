using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Weapons.Setup.Adding;
using VRage.Game;
using VRage.ModAPI;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting
{
    internal class TargetPriority
    {
        private const int EnemyPriority = 4;
        private const int NeutralPriority = 3;
        private const int UnownedPriority = 2;
        private const int FriendlyPriority = 1;

        public static int GetTargetPriority(object target, SorterTurretLogic turret)
        {
            int basePriority = GetBaseTargetPriority(target, turret);

            // Adjust priority based on distance
            double distance = Vector3D.Distance(turret.SorterWep.GetPosition(), GetTargetPosition(target));
            double distanceFactor = 1 - (distance / turret.AiRange); // Closer targets get higher priority

            // Combine base priority with distance factor
            return (int)(basePriority * 1000 + distanceFactor * 1000);
        }

        private static int GetBaseTargetPriority(object target, SorterTurretLogic turret)
        {
            MyRelationsBetweenPlayerAndBlock relation = GetRelationToTarget(target, turret);

            switch (relation)
            {
                case MyRelationsBetweenPlayerAndBlock.Enemies:
                    return turret.TargetEnemiesState ? EnemyPriority : 0;
                case MyRelationsBetweenPlayerAndBlock.Neutral:
                    return turret.TargetNeutralsState ? NeutralPriority : 0;
                case MyRelationsBetweenPlayerAndBlock.NoOwnership:
                    return turret.TargetUnownedState ? UnownedPriority : 0;
                case MyRelationsBetweenPlayerAndBlock.Friends:
                case MyRelationsBetweenPlayerAndBlock.FactionShare:
                case MyRelationsBetweenPlayerAndBlock.Owner:
                    return turret.TargetFriendliesState ? FriendlyPriority : 0;
                default:
                    return 0;
            }
        }

        private static MyRelationsBetweenPlayerAndBlock GetRelationToTarget(object target, SorterTurretLogic turret)
        {
            var grid = target as IMyCubeGrid;
            if (grid != null)
                return HeartUtils.GetRelationsBetweeenGrids(turret.SorterWep.CubeGrid, grid);
            else
            {
                var character = target as IMyCharacter;
                if (character != null)
                    return HeartUtils.GetRelationsBetweenGridAndPlayer(turret.SorterWep.CubeGrid, character.ControllerInfo?.ControllingIdentityId);
                else
                {
                    var projectile = target as Projectile;
                    if (projectile != null)
                    {
                        var entity = MyAPIGateway.Entities.GetEntityById(projectile.Firer);
                        var projectileGrid = entity as IMyCubeGrid;
                        if (projectileGrid != null)
                            return HeartUtils.GetRelationsBetweeenGrids(turret.SorterWep.CubeGrid, projectileGrid);
                        else
                        {
                            var projectileCharacter = entity as IMyCharacter;
                            if (projectileCharacter != null)
                                return HeartUtils.GetRelationsBetweenGridAndPlayer(turret.SorterWep.CubeGrid, projectileCharacter.ControllerInfo?.ControllingIdentityId);
                        }
                    }
                }
            }

            return MyRelationsBetweenPlayerAndBlock.Neutral;
        }

        private static Vector3D GetTargetPosition(object target)
        {
            var entity = target as IMyEntity;
            if (entity != null)
                return entity.GetPosition();
            else
            {
                var projectile = target as Projectile;
                if (projectile != null)
                    return projectile.Position;
            }

            return Vector3D.Zero;
        }

        public static List<object> GetPrioritizedTargets(List<object> targets, SorterTurretLogic turret)
        {
            targets.Sort((a, b) => GetTargetPriority(b, turret).CompareTo(GetTargetPriority(a, turret)));
            return targets;
        }

        public static bool ShouldConsiderTarget(object target, SorterTurretLogic turret)
        {
            var grid = target as IMyCubeGrid;
            if (grid != null)
                return turret.ShouldConsiderTarget(grid);
            else
            {
                var character = target as IMyCharacter;
                if (character != null)
                    return turret.ShouldConsiderTarget(character);
                else
                {
                    var projectile = target as Projectile;
                    if (projectile != null)
                        return turret.ShouldConsiderTarget(projectile);
                }
            }

            return false;
        }
    }
}