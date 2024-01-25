using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting
{
    internal class GridAiTargeting
    {
        IMyCubeGrid Grid;
        List<SorterWeaponLogic> Weapons => WeaponManager.I.GridWeapons[Grid];
        Vector3D gridPosition => Grid.PositionComp.WorldAABB.Center;

        SortedList<IMyCubeGrid, int> TargetedGrids = new SortedList<IMyCubeGrid, int>();
        SortedList<IMyCharacter, int> TargetedCharacters = new SortedList<IMyCharacter, int>();

        // Priority target list that gets checked first 
        SortedList<MyEntity, int> PriorityTargets = new SortedList<MyEntity, int>();
        SortedList<uint, int> TargetedProjectiles = new SortedList<uint, int>();

        private GenericKeenTargeting keenTargeting = new GenericKeenTargeting();

        /// <summary>
        /// The main focused target 
        /// </summary>
        public IMyCubeGrid PrimaryGridTarget { get; private set; }

        public bool Enabled = false;
        float MaxTargetingRange = 1000;
        bool DoesTargetGrids = true;
        bool DoesTargetCharacters = true;
        bool DoesTargetProjectiles = true;

        public GridAiTargeting(IMyCubeGrid grid)
        {
            Grid = grid;
            Grid.OnBlockAdded += Grid_OnBlockAdded;

            GridComparer = Comparer<IMyCubeGrid>.Create((x, y) =>
            {
                return (int)(Vector3D.DistanceSquared(gridPosition, x.GetPosition()) - Vector3D.DistanceSquared(gridPosition, y.GetPosition()));
            });
            CharacterComparer = Comparer<IMyCharacter>.Create((x, y) =>
            {
                return (int)(Vector3D.DistanceSquared(gridPosition, x.GetPosition()) - Vector3D.DistanceSquared(gridPosition, y.GetPosition()));
            });
            ProjectileComparer = Comparer<uint>.Create((x, y) =>
            {
                return (int)(Vector3D.DistanceSquared(gridPosition, ProjectileManager.I.GetProjectile(x).Position) - Vector3D.DistanceSquared(gridPosition, ProjectileManager.I.GetProjectile(y).Position));
            });

            SetTargetingFlags();
        }

        private void Grid_OnBlockAdded(IMySlimBlock obj)
        {
            // Unused for now
        }

        public void UpdateTargeting()
        {
            try
            {
                if (!Enabled) return;

                SetTargetingFlags();
                ScanForTargets();

                MyEntity manualTarget = null;
                if (keenTargeting != null)
                {
                    manualTarget = keenTargeting.GetTarget(Grid);
                    if (manualTarget is IMyCubeGrid)
                        PrimaryGridTarget = (IMyCubeGrid)manualTarget;
                    else
                        PrimaryGridTarget = null;
                }

                //MyAPIGateway.Utilities.ShowNotification("Grids: " + ValidGrids.Count, 1000/60);
                //MyAPIGateway.Utilities.ShowNotification("Characters: " + ValidCharacters.Count, 1000/60);
                //MyAPIGateway.Utilities.ShowNotification("Projectiles: " + ValidProjectiles.Count, 1000/60);


                foreach (var weapon in Weapons)
                {
                    if (!(weapon is SorterTurretLogic))
                        continue;

                    SorterTurretLogic turret = weapon as SorterTurretLogic;
                    turret.TargetProjectile = null;
                    turret.TargetEntity = null;
                    bool turretHasTarget = false;

                    // First, check for manually locked target using GenericKeenTargeting
                    if (keenTargeting != null)
                    {
                        // Check if manually locked target is within range or null
                        bool isManuallyLockedTargetInRange = manualTarget == null || Vector3D.DistanceSquared(manualTarget.PositionComp.WorldAABB.Center, Grid.PositionComp.WorldAABB.Center) <= MaxTargetingRange * MaxTargetingRange;

                        // Set the manually locked target as the primary target regardless of range
                        if (manualTarget != null && isManuallyLockedTargetInRange)
                        {
                            turret.TargetEntity = manualTarget;
                            turretHasTarget = true;
                        }
                    }

                    // Check priority targets first if no manually locked target
                    if (!turretHasTarget && PriorityTargets.Count > 0)
                    {
                        // Assign first priority target 
                        MyEntity priorityTarget = PriorityTargets.First().Key;
                        if (turret.ShouldConsiderTarget((IMyCubeGrid)priorityTarget))
                        {
                            if (priorityTarget is IMyCharacter)
                                turret.TargetEntity = (IMyCharacter)priorityTarget;
                            else if (priorityTarget is IMyCubeGrid)
                                turret.TargetEntity = (IMyCubeGrid)priorityTarget;

                            turretHasTarget = true;

                            PriorityTargets[priorityTarget]++;
                        }
                    }

                    // Rest of targeting logic...

                    if (turret.TargetProjectilesState)
                    {
                        if (turret.PreferUniqueTargets) // Try to balance targeting
                        {
                            List<Projectile> targetable = new List<Projectile>();
                            foreach (var target in TargetedProjectiles)
                            {
                                Projectile proj = ProjectileManager.I.GetProjectile(target.Key);
                                if (turret.ShouldConsiderTarget(proj))
                                    targetable.Add(proj);
                            }

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
                            foreach (var projectile in TargetedProjectiles.Keys) // Tell turrets to focus on the closest valid target
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
                            foreach (var target in TargetedCharacters.Keys)
                            {
                                if (turret.ShouldConsiderTarget(target))
                                    targetable.Add(target);
                            }

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
                            foreach (var character in TargetedCharacters.Keys)
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
                            foreach (var target in TargetedGrids.Keys)
                            {
                                if (turret.ShouldConsiderTarget(target))
                                    targetable.Add(target);
                            };

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
                            foreach (var grid in TargetedGrids.Keys)
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
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(GridAiTargeting));
            }
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
                    var turret = (SorterTurretLogic)weapon;
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
                    allGrids.Add((IMyCubeGrid)entity);
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

            Dictionary<IMyCubeGrid, int> gridBuffer = new Dictionary<IMyCubeGrid, int>();
            Dictionary<IMyCharacter, int> charBuffer = new Dictionary<IMyCharacter, int>();
            Dictionary<uint, int> projBuffer = new Dictionary<uint, int>();

            if (DoesTargetGrids) // Limit valid grids to those in range
                foreach (var grid in allGrids)
                    if (!distanceCheck || Vector3D.DistanceSquared(gridPosition, grid.GetPosition()) < maxRangeSq)
                        gridBuffer.Add(grid, 0);

            if (DoesTargetCharacters) // Limit valid characters to those in range
                foreach (var character in allCharacters)
                    if (!distanceCheck || Vector3D.DistanceSquared(gridPosition, character.GetPosition()) < maxRangeSq)
                        charBuffer.Add(character, 0);

            if (DoesTargetProjectiles) // Limit valid projectiles to those in range
                foreach (var projectile in allProjectiles)
                    if (!distanceCheck || Vector3D.DistanceSquared(gridPosition, ProjectileManager.I.GetProjectile(projectile).Position) < maxRangeSq)
                        projBuffer.Add(projectile, 0);

            TargetedGrids = new SortedList<IMyCubeGrid, int>(gridBuffer, GridComparer);
            TargetedCharacters = new SortedList<IMyCharacter, int>(charBuffer, CharacterComparer);
            TargetedProjectiles = new SortedList<uint, int>(projBuffer, ProjectileComparer);
        }

        public void Close()
        {
            TargetedGrids.Clear();
            TargetedCharacters.Clear();
            TargetedProjectiles.Clear();
        }

        private Comparer<IMyCubeGrid> GridComparer;
        private Comparer<IMyCharacter> CharacterComparer;
        private Comparer<uint> ProjectileComparer;
    }
}
