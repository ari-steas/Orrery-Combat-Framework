using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
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
        }

        public void UpdateTargeting()
        {
            if (!Enabled) return;

            SetTargetingFlags();
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
        }

        public void UpdateAvailableTargets(List<IMyCubeGrid> allGrids, List<IMyCharacter> allCharacters, List<uint> allProjectiles)
        {
            float maxRangeSq = MaxTargetingRange * MaxTargetingRange;
            Vector3D gridPosition = Grid.GetPosition();

            if (DoesTargetGrids) // Limit valid grids to those in range
            {
                ValidGrids.Clear();
                foreach (var grid in allGrids)
                    if (Vector3D.DistanceSquared(gridPosition, grid.GetPosition()) < maxRangeSq)
                        ValidGrids.Add(grid);
            }

            if (DoesTargetCharacters) // Limit valid characters to those in range
            {
                ValidCharacters.Clear();
                foreach (var character in allCharacters)
                    if (Vector3D.DistanceSquared(gridPosition, character.GetPosition()) < maxRangeSq)
                        ValidCharacters.Add(character);
            }

            if (DoesTargetProjectiles) // Limit valid projectiles to those in range
            {
                ValidProjectiles.Clear();
                foreach (var projectile in allProjectiles)
                    if (Vector3D.DistanceSquared(gridPosition, ProjectileManager.I.GetProjectile(projectile).Position) < maxRangeSq)
                        ValidProjectiles.Add(projectile);
            }
        }

        public void Close()
        {
            ValidGrids.Clear();
            ValidCharacters.Clear();
            ValidProjectiles.Clear();
        }
    }
}
