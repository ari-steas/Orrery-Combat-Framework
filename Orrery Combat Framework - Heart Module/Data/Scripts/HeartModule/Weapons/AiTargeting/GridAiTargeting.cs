using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using Heart_Module.Data.Scripts.HeartModule.Weapons.Setup.Adding;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting
{
    internal class GridAiTargeting
    {
        IMyCubeGrid Grid;
        List<SorterWeaponLogic> Weapons
        {
            get
            {
                List<SorterWeaponLogic> weapons;
                if (WeaponManager.I.GridWeapons.TryGetValue(Grid, out weapons))
                {
                    return weapons;
                }
                else
                {
                    HeartLog.Log($"Weapons list not found for grid '{Grid.DisplayName}'");
                    return new List<SorterWeaponLogic>();
                }
            }
        }
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
            HeartLog.Log($"Initializing GridAiTargeting for grid '{grid.DisplayName}'");
            Grid = grid;
            Grid.OnBlockAdded += Grid_OnBlockAdded;
            Grid.OnBlockRemoved += Grid_OnBlockRemoved; // Subscribe to the block removed event

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
            HeartLog.Log($"GridAiTargeting initialized for grid '{grid.DisplayName}' with targeting enabled: {Enabled}");
        }

        private void Grid_OnBlockAdded(IMySlimBlock obj)
        {
            if (obj.FatBlock is SorterWeaponLogic)
            {
                EnableGridAiIfNeeded();
            }
        }

        private void Grid_OnBlockRemoved(IMySlimBlock obj)
        {
            if (obj.FatBlock is SorterWeaponLogic)
            {
                DisableGridAiIfNeeded();
            }
        }

        public void EnableGridAiIfNeeded()
        {
            if (!Enabled && Weapons.Count > 0)
            {
                Enabled = true;
                SetTargetingFlags();
                HeartLog.Log($"GridAiTargeting enabled for grid '{Grid.DisplayName}'");
            }
        }

        public void DisableGridAiIfNeeded()
        {
            if (Enabled && Weapons.Count == 0)
            {
                Enabled = false;
                HeartLog.Log($"GridAiTargeting disabled for grid '{Grid.DisplayName}' due to no weapons");
            }
        }

        public void SetPrimaryTarget(IMyCubeGrid entity)
        {
            PrimaryGridTarget = entity;
        }

        private DateTime lastLogTime = DateTime.MinValue;

        public void UpdateTargeting()
        {
            if (Grid.Physics == null)
            {
                return;
            }

            try
            {
                if (!Enabled) return;

                SetTargetingFlags();
                ScanForTargets();

                MyEntity manualTarget = null;
                bool isTargetLocked = keenTargeting != null && keenTargeting.IsTargetLocked(Grid);

                if (isTargetLocked)
                {
                    manualTarget = keenTargeting.GetTarget(Grid);
                    if (manualTarget is IMyCubeGrid)
                        PrimaryGridTarget = (IMyCubeGrid)manualTarget;
                }

                foreach (var weapon in Weapons)
                {
                    if (!(weapon is SorterTurretLogic) || !weapon.SorterWep.IsWorking)
                        continue;

                    SorterTurretLogic turret = weapon as SorterTurretLogic;
                    bool turretHasTarget = false;
                    bool targetChanged = false;

                    // Prioritize manual target if it is locked and valid
                    if (isTargetLocked)
                    {
                        bool isManuallyLockedTargetInRange = manualTarget == null || Vector3D.DistanceSquared(manualTarget.PositionComp.WorldAABB.Center, Grid.PositionComp.WorldAABB.Center) <= MaxTargetingRange * MaxTargetingRange;
                        if (manualTarget != null && isManuallyLockedTargetInRange)
                        {
                            if ((manualTarget is IMyCubeGrid && turret.ShouldConsiderTarget(manualTarget as IMyCubeGrid)) ||
                                (manualTarget is IMyCharacter && turret.ShouldConsiderTarget(manualTarget as IMyCharacter)))
                            {
                                if (turret.TargetEntity != manualTarget)
                                {
                                    turret.SetTarget(manualTarget);
                                    turretHasTarget = true;
                                    targetChanged = true;
                                }
                                continue; // Skip AI targeting if manual target is set
                            }
                        }
                    }

                    // Check priority targets
                    if (!turretHasTarget && PriorityTargets.Count > 0)
                    {
                        MyEntity priorityTarget = PriorityTargets.First().Key;
                        if (turret.ShouldConsiderTarget(priorityTarget as IMyCubeGrid))
                        {
                            if (turret.TargetEntity != priorityTarget)
                            {
                                turret.SetTarget(priorityTarget);
                                turretHasTarget = true;
                                targetChanged = true;
                                PriorityTargets[priorityTarget]++;
                            }
                        }
                    }

                    // Automatic targeting logic
                    if (!turretHasTarget)
                    {
                        if (turret.TargetProjectilesState)
                        {
                            if (turret.PreferUniqueTargetsState)
                            {
                                List<Projectile> targetable = new List<Projectile>();
                                foreach (var target in TargetedProjectiles)
                                {
                                    Projectile proj = ProjectileManager.I.GetProjectile(target.Key);
                                    if (turret.ShouldConsiderTarget(proj))
                                        targetable.Add(proj);
                                }

                                if (targetable.Count == 0)
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

                                if (turret.TargetEntity != minTargeted)
                                {
                                    turret.SetTarget(minTargeted);
                                    turretHasTarget = true;
                                    targetChanged = true;
                                    TargetedProjectiles[minTargeted.Id]++;
                                }
                            }
                            else
                            {
                                foreach (var projectile in TargetedProjectiles.Keys)
                                {
                                    var proj = ProjectileManager.I.GetProjectile(projectile);
                                    if (turret.ShouldConsiderTarget(proj))
                                    {
                                        if (turret.TargetEntity != proj)
                                        {
                                            turret.SetTarget(proj);
                                            turretHasTarget = true;
                                            targetChanged = true;
                                            TargetedProjectiles[projectile]++;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        if (!turretHasTarget && turret.TargetCharactersState)
                        {
                            if (turret.PreferUniqueTargetsState)
                            {
                                List<IMyCharacter> targetable = new List<IMyCharacter>();
                                foreach (var target in TargetedCharacters.Keys)
                                {
                                    if (turret.ShouldConsiderTarget(target))
                                        targetable.Add(target);
                                }

                                if (targetable.Count == 0)
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

                                if (turret.TargetEntity != minTargeted)
                                {
                                    turret.SetTarget(minTargeted);
                                    turretHasTarget = true;
                                    targetChanged = true;
                                    TargetedCharacters[minTargeted]++;
                                }
                            }
                            else
                            {
                                foreach (var character in TargetedCharacters.Keys)
                                {
                                    if (turret.ShouldConsiderTarget(character))
                                    {
                                        if (turret.TargetEntity != character)
                                        {
                                            turret.SetTarget(character);
                                            turretHasTarget = true;
                                            targetChanged = true;
                                            TargetedCharacters[character]++;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        if (!turretHasTarget && turret.TargetGridsState)
                        {
                            if (turret.PreferUniqueTargetsState)
                            {
                                List<IMyCubeGrid> targetable = new List<IMyCubeGrid>();
                                foreach (var target in TargetedGrids.Keys)
                                {
                                    if (turret.ShouldConsiderTarget(target))
                                        targetable.Add(target);
                                }

                                if (targetable.Count == 0)
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

                                if (turret.TargetEntity != minTargeted)
                                {
                                    turret.SetTarget(minTargeted);
                                    turretHasTarget = true;
                                    targetChanged = true;
                                    TargetedGrids[minTargeted]++;
                                }
                            }
                            else
                            {
                                foreach (var grid in TargetedGrids.Keys)
                                {
                                    if (turret.ShouldConsiderTarget(grid))
                                    {
                                        if (turret.TargetEntity != grid)
                                        {
                                            turret.SetTarget(grid);
                                            turretHasTarget = true;
                                            targetChanged = true;
                                            TargetedGrids[grid]++;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Reset target if no valid target is found
                    if (!turretHasTarget && !turret.HasValidTarget())
                    {
                        turret.ResetTargetingState();
                    }

                    if (targetChanged)
                    {
                        var now = DateTime.Now;
                        if ((now - lastLogTime).TotalSeconds > 10)
                        {
                            HeartLog.Log($"Target updated for turret '{turret}' on grid '{Grid.DisplayName}'");
                            lastLogTime = now;
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
            try
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

                    float maxTrajectory = ProjectileDefinitionManager.GetDefinition(weapon.Magazines.SelectedAmmoId)?.PhysicalProjectile.MaxTrajectory ?? 0;
                    if (maxTrajectory > MaxTargetingRange)
                        MaxTargetingRange = maxTrajectory;
                }

                MaxTargetingRange *= 1.1f; // Increase range by a little bit to make targeting less painful

                if (Enabled) // Disable if MaxRange = 0.
                    Enabled = MaxTargetingRange > 0;

                // Other targeting logic here
            }
            catch (Exception ex)
            {
                HeartLog.LogException(ex, typeof(GridAiTargeting), "Error in SetTargetingFlags: ");
                Enabled = false;
            }
        }

        private void ScanForTargets()
        {
            if (!Enabled)
                return;

            if (Grid.Physics == null)
            {
                return;
            }

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
                    allGrids.Add((IMyCubeGrid)entity);
                }
                else if (entity is IMyCharacter)
                    allCharacters.Add(entity as IMyCharacter);
            }

            List<Projectile> allProjectiles = new List<Projectile>();
            ProjectileManager.I.GetProjectilesInSphere(sphere, ref allProjectiles, true);

            UpdateAvailableTargets(allGrids, allCharacters, allProjectiles, false);
        }

        public void UpdateAvailableTargets(List<IMyCubeGrid> allGrids, List<IMyCharacter> allCharacters, List<Projectile> allProjectiles, bool distanceCheck = true)
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
                    if (!distanceCheck || Vector3D.DistanceSquared(gridPosition, projectile.Position) < maxRangeSq)
                        projBuffer.Add(projectile.Id, 0);

            TargetedGrids = new SortedList<IMyCubeGrid, int>(gridBuffer, GridComparer);
            TargetedCharacters = new SortedList<IMyCharacter, int>(charBuffer, CharacterComparer);
            TargetedProjectiles = new SortedList<uint, int>(projBuffer, ProjectileComparer);
        }

        public void Close()
        {
            HeartLog.Log($"Closing GridAiTargeting for grid '{Grid.DisplayName}'");
            Grid.OnBlockAdded -= Grid_OnBlockAdded;
            Grid.OnBlockRemoved -= Grid_OnBlockRemoved;
            TargetedGrids.Clear();
            TargetedCharacters.Clear();
            TargetedProjectiles.Clear();
        }

        private Comparer<IMyCubeGrid> GridComparer;
        private Comparer<IMyCharacter> CharacterComparer;
        private Comparer<uint> ProjectileComparer;
    }
}
