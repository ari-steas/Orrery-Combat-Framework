using Heart_Module.Data.Scripts.HeartModule.Debug;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
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
        List<IMyCubeGrid> ValidGrids = new List<IMyCubeGrid>();
        List<IMyCharacter> ValidCharacters = new List<IMyCharacter>();
        List<uint> ValidProjectiles = new List<uint>();

        Dictionary<IMyCubeGrid, int> TargetedGrids = new Dictionary<IMyCubeGrid, int>(); 
        Dictionary<IMyCharacter, int> TargetedCharacters = new Dictionary<IMyCharacter, int>(); 
        Dictionary<uint, int> TargetedProjectiles = new Dictionary<uint, int>(); 

        /// <summary>
        /// The main focused target 
        /// </summary>
        IMyCubeGrid PrimaryGridTarget;

        public bool Enabled = false;
        float MaxTargetingRange = 1000;
        bool DoesTargetGrids = true;
        bool DoesTargetCharacters = true;
        bool DoesTargetProjectiles = true;

        public GridAiTargeting(IMyCubeGrid grid)
        {
            Grid = grid;
            Grid.OnBlockAdded += Grid_OnBlockAdded;

            SetTargetingFlags();
        }

        private void Grid_OnBlockAdded(IMySlimBlock obj)
        {
            // Unused for now
        }

        public void UpdateTargeting()
        {
            if (!Enabled) return;

            SetTargetingFlags();
            ScanForTargets();
            //MyAPIGateway.Utilities.ShowNotification("Grids: " + ValidGrids.Count, 1000/60);
            //MyAPIGateway.Utilities.ShowNotification("Characters: " + ValidCharacters.Count, 1000/60);
            //MyAPIGateway.Utilities.ShowNotification("Projectiles: " + ValidProjectiles.Count, 1000/60);

            IMyCubeGrid closestGrid = GetClosestGrid();
            if (closestGrid != null)
                DebugDraw.AddLine(Grid.PositionComp.WorldAABB.Center, closestGrid.PositionComp.WorldAABB.Center, Color.Pink, 0);
            IMyCharacter closestChar = GetClosestCharacter();
            if (closestChar != null)
                DebugDraw.AddLine(Grid.PositionComp.WorldAABB.Center, closestChar.PositionComp.WorldAABB.Center, Color.Orange, 0);
            Projectile closestProj = GetClosestProjectile();
            if (closestProj != null)
                DebugDraw.AddLine(Grid.PositionComp.WorldAABB.Center, closestProj.Position, Color.Blue, 0);

            TargetedGrids.Clear();
            TargetedProjectiles.Clear();
            TargetedCharacters.Clear();

            foreach (var weapon in Weapons) // In desperate need of a refactoring
            {
                if (weapon is SorterTurretLogic)
                {
                    SorterTurretLogic turret = weapon as SorterTurretLogic;
                    turret.TargetProjectile = null;
                    turret.TargetEntity = null;
                    bool turretHasTarget = false;

                    if (turret.TargetProjectilesState)
                    {
                        if (turret.PreferUniqueTargets) // Try to balance targeting
                        {
                            List<Projectile> targetable = new List<Projectile>();
                            ValidProjectiles.ForEach(p =>
                            {
                                Projectile proj = ProjectileManager.I.GetProjectile(p);
                                if (turret.ShouldConsiderTarget(proj))
                                    targetable.Add(proj);
                            });

                            if (targetable.Count == 0) // If zero targetable, go to next weapon
                                continue;

                            Projectile minTargeted = targetable[0];
                            int minCount = int.MaxValue;
                            targetable.ForEach(p =>
                            {
                                if (TargetedProjectiles[p.Id] < minCount)
                                {
                                    minTargeted = p;
                                    minCount = TargetedProjectiles[p.Id];
                                }
                            });

                            turret.TargetProjectile = minTargeted;
                            turretHasTarget = true;

                            TargetedProjectiles[minTargeted.Id]++; // Keep track of the number of turrets shooting a target
                        }
                        else
                        {
                            foreach (var projectile in ValidProjectiles) // Tell turrets to focus on the closest valid target
                            {
                                if (turret.ShouldConsiderTarget(ProjectileManager.I.GetProjectile(projectile)))
                                {
                                    turret.TargetProjectile = ProjectileManager.I.GetProjectile(projectile);
                                    turretHasTarget = true;

                                    TargetedProjectiles[projectile]++; // Keep track of the number of turrets shooting a target

                                    break;
                                }
                            }
                        }
                    }
                    if (!turretHasTarget && turret.TargetCharactersState)
                    {
                        if (turret.PreferUniqueTargets) // Try to balance targeting
                        {
                            List<IMyCharacter> targetable = new List<IMyCharacter>();
                            ValidCharacters.ForEach(p =>
                            {
                                if (turret.ShouldConsiderTarget(p))
                                    targetable.Add(p);
                            });

                            if (targetable.Count == 0) // If zero targetable, go to next weapon
                                continue;

                            IMyCharacter minTargeted = targetable[0];
                            int minCount = int.MaxValue;
                            targetable.ForEach(p =>
                            {
                                if (TargetedCharacters[p] < minCount)
                                {
                                    minTargeted = p;
                                    minCount = TargetedCharacters[p];
                                }
                            });

                            turret.TargetEntity = minTargeted;
                            turretHasTarget = true;

                            TargetedCharacters[minTargeted]++; // Keep track of the number of turrets shooting a target
                        }
                        else
                        {
                            foreach (var character in ValidCharacters)
                            {
                                if (turret.ShouldConsiderTarget(character))
                                {
                                    turret.TargetEntity = character;
                                    turretHasTarget = true;

                                    TargetedCharacters[character]++; // Keep track of the number of turrets shooting a target

                                    break;
                                }
                            }
                        }
                    }
                    if (!turretHasTarget && turret.TargetGridsState)
                    {
                        if (turret.PreferUniqueTargets) // Try to balance targeting
                        {
                            List<IMyCubeGrid> targetable = new List<IMyCubeGrid>();
                            ValidGrids.ForEach(p =>
                            {
                                if (turret.ShouldConsiderTarget(p))
                                    targetable.Add(p);
                            });

                            if (targetable.Count == 0) // If zero targetable, go to next weapon
                                continue;

                            IMyCubeGrid minTargeted = targetable[0];
                            int minCount = int.MaxValue;
                            targetable.ForEach(p =>
                            {
                                if (TargetedGrids[p] < minCount)
                                {
                                    minTargeted = p;
                                    minCount = TargetedGrids[p];
                                }
                            });

                            turret.TargetEntity = minTargeted;
                            turretHasTarget = true;

                            TargetedGrids[minTargeted]++; // Keep track of the number of turrets shooting a target
                        }
                        else
                        {
                            foreach (var grid in ValidGrids)
                            {
                                if (turret.ShouldConsiderTarget(grid))
                                {
                                    turret.TargetEntity = grid;
                                    turretHasTarget = true;

                                    TargetedGrids[grid]++; // Keep track of the number of turrets shooting a target

                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private IMyCubeGrid GetClosestGrid()
        {
            if (ValidGrids.Count == 0) return null;
            
            return ValidGrids[0];
        }
        private IMyCharacter GetClosestCharacter()
        {
            if (ValidCharacters.Count == 0) return null;

            return ValidCharacters[0];
        }

        private Projectile GetClosestProjectile()
        {
            if (ValidProjectiles.Count == 0) return null;

            return ProjectileManager.I.GetProjectile(ValidProjectiles[0]);
        }

        /// <summary>
        /// Scan all turrets for flags
        /// </summary>
        private void SetTargetingFlags()
        {
            Enabled = Weapons.Count > 0; // Disable if it has no weapons
            if (!Enabled)
                return;

            DoesTargetGrids = false;
            DoesTargetCharacters = false;
            DoesTargetProjectiles = false;
            MaxTargetingRange = 0;
            foreach (var weapon in Weapons)
            {
                if (weapon is SorterTurretLogic) // Only set targeting flags with turrets
                {
                    var turret = (SorterTurretLogic) weapon;
                    DoesTargetGrids |= turret.Settings.TargetGridsState;
                    DoesTargetCharacters |= turret.Settings.TargetCharactersState;
                    DoesTargetProjectiles |= turret.Settings.TargetProjectilesState;
                }

                float maxTrajectory = ProjectileDefinitionManager.GetDefinition(weapon.CurrentAmmo).PhysicalProjectile.MaxTrajectory;
                if (maxTrajectory > MaxTargetingRange)
                    MaxTargetingRange = maxTrajectory;
            }

            MaxTargetingRange *= 1.1f; // Increase range by a little bit to make targeting less painful

            if (Enabled) // Disable if MaxRange = 0.
                Enabled = MaxTargetingRange > 0;

            // Other targeting logic here
        }

        private void ScanForTargets()
        {
            if (!Enabled)
                return;

            BoundingSphereD sphere = new BoundingSphereD(Grid.PositionComp.WorldAABB.Center, MaxTargetingRange);

            List<MyEntity> entities = new List<MyEntity>();
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, entities);

            List<IMyCubeGrid> allGrids = new List<IMyCubeGrid>();
            List<IMyCharacter> allCharacters = new List<IMyCharacter>();

            foreach (var entity in entities)
            {
                if (entity == Grid || entity.Physics == null)
                    continue;
                if (entity is IMyCubeGrid)
                {
                    //IMyCubeGrid topmost = (IMyCubeGrid)((IMyCubeGrid)entity).GetTopMostParent(); // Ignore subgrids, and instead target parents.
                    //if (!allGrids.Contains(topmost)) // Note - GetTopMostParent() consistently picks the first subgrid to spawn.
                    //    allGrids.Add(topmost);
                    allGrids.Add((IMyCubeGrid) entity);
                }
                else if (entity is IMyCharacter)
                    allCharacters.Add(entity as IMyCharacter);
            }

            List<uint> allProjectiles = new List<uint>();
            ProjectileManager.I.GetProjectilesInSphere(sphere, ref allProjectiles, true);

            UpdateAvailableTargets(allGrids, allCharacters, allProjectiles, false);
        }

        public void UpdateAvailableTargets(List<IMyCubeGrid> allGrids, List<IMyCharacter> allCharacters, List<uint> allProjectiles, bool distanceCheck = true)
        {
            float maxRangeSq = MaxTargetingRange * MaxTargetingRange;
            Vector3D gridPosition = Grid.PositionComp.WorldAABB.Center;
            ValidGrids.Clear();
            ValidCharacters.Clear();
            ValidProjectiles.Clear();

            TargetedGrids.Clear();
            TargetedCharacters.Clear();
            TargetedProjectiles.Clear();

            if (DoesTargetGrids) // Limit valid grids to those in range
            {
                foreach (var grid in allGrids)
                {
                    if (!distanceCheck || Vector3D.DistanceSquared(gridPosition, grid.GetPosition()) < maxRangeSq)
                    {
                        ValidGrids.Add(grid);
                        TargetedGrids.Add(grid, 0);
                    }
                }
            }

            if (DoesTargetCharacters) // Limit valid characters to those in range
            {
                foreach (var character in allCharacters)
                {
                    if (!distanceCheck || Vector3D.DistanceSquared(gridPosition, character.GetPosition()) < maxRangeSq)
                    {
                        ValidCharacters.Add(character);
                        TargetedCharacters.Add(character, 0);
                    }
                }
            }

            if (DoesTargetProjectiles) // Limit valid projectiles to those in range
            {
                foreach (var projectile in allProjectiles)
                {
                    if (!distanceCheck || Vector3D.DistanceSquared(gridPosition, ProjectileManager.I.GetProjectile(projectile).Position) < maxRangeSq)
                    {
                        ValidProjectiles.Add(projectile);
                        TargetedProjectiles.Add(projectile, 0);
                    }
                }
            }

            ValidGrids.Sort((x, y) => {
                return (int)(Vector3D.DistanceSquared(gridPosition, x.GetPosition()) - Vector3D.DistanceSquared(gridPosition, y.GetPosition()));
            });

            ValidCharacters.Sort((x, y) => {
                return (int)(Vector3D.DistanceSquared(gridPosition, x.GetPosition()) - Vector3D.DistanceSquared(gridPosition, y.GetPosition()));
            });

            ValidProjectiles.Sort((x, y) => {
                return (int)(Vector3D.DistanceSquared(gridPosition, ProjectileManager.I.GetProjectile(x).Position) - Vector3D.DistanceSquared(gridPosition, ProjectileManager.I.GetProjectile(y).Position));
            });
        }

        public void Close()
        {
            ValidGrids.Clear();
            ValidCharacters.Clear();
            ValidProjectiles.Clear();
        }
    }
}
