using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Heart_Module.Data.Scripts.HeartModule.Weapons.Setup.Adding;
using Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting;
using System;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class WeaponManager : MySessionComponentBase
    {
        public static WeaponManager I;

        internal Dictionary<uint, SorterWeaponLogic> ActiveWeapons = new Dictionary<uint, SorterWeaponLogic>();
        private uint NextId = 0;
        public Dictionary<IMyCubeGrid, List<SorterWeaponLogic>> GridWeapons = new Dictionary<IMyCubeGrid, List<SorterWeaponLogic>>(); // EntityId based because IMyCubeGrid keys break garbage collection
        public bool DidFirstInit = false;

        /// <summary>
        /// Delta for engine ticks; 60tps
        /// </summary>
        private const float deltaTick = 1 / 60f;

        public override void LoadData()
        {
            I = this;
            HeartData.I.OnGridAdd += OnGridAdd;
            HeartData.I.OnGridRemove += OnGridRemove;
        }

        /// <summary>
        /// Check for blocks already in the world without weapon logic.
        /// </summary>
        /// <param name="definition"></param>
        public void UpdateLogicOnExistingBlocks(WeaponDefinitionBase definition)
        {
            foreach (var grid in GridWeapons.Keys)
            {
                foreach (var block in grid.GetFatBlocks<IMyConveyorSorter>())
                {
                    if (block.BlockDefinition.SubtypeName != definition.Assignments.BlockSubtype || block.GameLogic?.GetAs<SorterWeaponLogic>() != null)
                        continue;

                    AddWeapon(block as IMyConveyorSorter);

                    // Notify the grid AI that a new weapon has been added
                    var gridAiTargeting = WeaponManagerAi.I.GetOrCreateGridAiTargeting(grid);
                    gridAiTargeting.EnableGridAiIfNeeded();
                }
            }
        }

        /// <summary>
        /// Removes weapon logic on weapons of a given type
        /// </summary>
        /// <param name="definition"></param>
        public void RemoveLogicOnExistingBlocks(WeaponDefinitionBase definition)
        {
            foreach (var grid in GridWeapons)
            {
                foreach (var weapon in grid.Value.ToArray())
                {
                    if (weapon.Definition == definition)
                    {
                        ActiveWeapons.Remove(weapon.Id);
                        List<SorterWeaponLogic> values;
                        GridWeapons.TryGetValue(weapon.SorterWep.CubeGrid, out values);
                        values?.Remove(weapon);

                        weapon.MarkForClose();
                    }
                }
            }
        }

        /// <summary>
        /// Check if new grids contain valid weapons.
        /// </summary>
        /// <param name="entity"></param>
        private void OnGridAdd(IMyCubeGrid grid)
        {
            try
            {
                HeartLog.Log($"OnGridAdd: Starting for grid {grid?.EntityId}");

                if (grid == null || grid.Physics == null)
                {
                    HeartLog.Log($"OnGridAdd: Grid is null or has no physics. Skipping.");
                    return;
                }

                HeartLog.Log($"OnGridAdd: Processing grid {grid.EntityId}");
                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                grid.GetBlocks(null, b =>
                {
                    if (b.FatBlock is IMyConveyorSorter)
                        blocks.Add(b);
                    return false;
                });

                HeartLog.Log($"OnGridAdd: Found {blocks.Count} potential weapon blocks");

                if (!GridWeapons.ContainsKey(grid))
                    GridWeapons.Add(grid, new List<SorterWeaponLogic>());

                foreach (var block in blocks)
                    OnBlockAdd(block);

                grid.OnBlockAdded += OnBlockAdd;

                HeartLog.Log($"OnGridAdd: Finished processing grid {grid.EntityId}");
            }
            catch (Exception ex)
            {
                HeartLog.Log($"OnGridAdd: Exception occurred: {ex.Message}");
                HeartLog.Log($"OnGridAdd: Stack trace: {ex.StackTrace}");
                SoftHandle.RaiseException(ex, typeof(WeaponManager));
            }
        }

        /// <summary>
        /// Removes grids from the GridWeapons list
        /// </summary>
        /// <param name="entity"></param>
        private void OnGridRemove(IMyCubeGrid grid)
        {
            HeartLog.Log($"WeaponManager: OnGridRemove called for grid {grid.EntityId}");

            List<SorterWeaponLogic> weapons;
            if (GridWeapons.TryGetValue(grid, out weapons))
            {
                HeartLog.Log($"WeaponManager: Removing {weapons.Count} weapons for grid {grid.EntityId}");

                foreach (var weapon in weapons)
                {
                    HeartLog.Log($"WeaponManager: Removing weapon {weapon.Id} from ActiveWeapons");
                    ActiveWeapons.Remove(weapon.Id);
                    weapon.Close();
                }

                GridWeapons.Remove(grid);
            }
            else
            {
                HeartLog.Log($"WeaponManager: No weapons found for grid {grid.EntityId}");
            }
        }

        /// <summary>
        /// Check if new blocks contain a valid weapon, and attach logic if it does.
        /// </summary>
        /// <param name="slim"></param>
        private void OnBlockAdd(IMySlimBlock slim)
        {
            IMyConveyorSorter sorter = slim.FatBlock as IMyConveyorSorter;
            if (sorter == null || !WeaponDefinitionManager.HasDefinition(slim.BlockDefinition.Id.SubtypeName))
                return;

            AddWeapon(sorter);
        }

        private void AddWeapon(IMyConveyorSorter sorter)
        {
            try
            {
                HeartLog.Log($"AddWeapon: Starting to add weapon for sorter {sorter?.EntityId}");

                if (sorter == null)
                {
                    HeartLog.Log("AddWeapon: sorter is null");
                    return;
                }

                if (sorter.CubeGrid == null)
                {
                    HeartLog.Log($"AddWeapon: CubeGrid is null for sorter {sorter.EntityId}");
                    return;
                }

                if (sorter.CubeGrid.Physics == null)
                {
                    HeartLog.Log($"AddWeapon: Grid Physics is null for sorter {sorter.EntityId}");
                    return;
                }

                HeartLog.Log($"AddWeapon: Getting definition for {sorter.BlockDefinition.SubtypeName}");
                WeaponDefinitionBase def = WeaponDefinitionManager.GetDefinition(sorter.BlockDefinition.SubtypeName);
                if (def == null)
                {
                    HeartLog.Log($"AddWeapon: No definition found for {sorter.BlockDefinition.SubtypeName}");
                    return;
                }

                HeartLog.Log($"AddWeapon: Creating logic for {(def.Assignments.IsTurret ? "turret" : "fixed weapon")}");
                SorterWeaponLogic logic;
                while (!IsIdAvailable(NextId))
                    NextId++;

                if (def.Assignments.IsTurret)
                    logic = new SorterTurretLogic(sorter, def, NextId);
                else
                    logic = new SorterWeaponLogic(sorter, def, NextId);

                if (logic == null)
                {
                    HeartLog.Log($"AddWeapon: Failed to create logic for sorter {sorter.EntityId}");
                    return;
                }

                HeartLog.Log($"AddWeapon: Adding weapon to ActiveWeapons with ID {NextId}");
                ActiveWeapons.Add(NextId, logic);

                HeartLog.Log($"AddWeapon: Adding weapon to GridWeapons for grid {sorter.CubeGrid.EntityId}");
                if (!GridWeapons.ContainsKey(sorter.CubeGrid))
                {
                    GridWeapons[sorter.CubeGrid] = new List<SorterWeaponLogic>();
                }
                GridWeapons[sorter.CubeGrid].Add(logic);

                HeartLog.Log($"AddWeapon: Setting up OnMarkForClose");
                sorter.OnMarkForClose += (a) =>
                {
                    HeartLog.Log($"OnMarkForClose: Removing weapon {NextId} from ActiveWeapons and GridWeapons");
                    ActiveWeapons.Remove(NextId);
                    List<SorterWeaponLogic> values;
                    if (GridWeapons.TryGetValue(sorter.CubeGrid, out values))
                    {
                        values?.Remove(logic);
                    }
                };

                HeartLog.Log($"AddWeapon: Requesting sync for weapon {sorter.EntityId}");
                Heart_Settings.RequestSync(sorter.EntityId);

                if (logic.Id == uint.MaxValue)
                {
                    HeartLog.Log($"AddWeapon: Failed to initialize weapon! Subtype: {sorter.BlockDefinition.SubtypeId}");
                    logic.Close();
                    CriticalHandle.ThrowCriticalException(new System.Exception($"Failed to initialize weapon! Subtype: {sorter.BlockDefinition.SubtypeId}"), typeof(WeaponManager));
                }

                HeartLog.Log($"AddWeapon: Getting or creating GridAiTargeting for grid {sorter.CubeGrid.EntityId}");
                var gridAiTargeting = WeaponManagerAi.I.GetOrCreateGridAiTargeting(sorter.CubeGrid);
                gridAiTargeting.EnableGridAiIfNeeded();

                HeartLog.Log($"AddWeapon: Finished adding weapon for sorter {sorter.EntityId}");
            }
            catch (Exception ex)
            {
                HeartLog.Log($"AddWeapon: Exception occurred: {ex.Message}");
                HeartLog.Log($"AddWeapon: Stack trace: {ex.StackTrace}");
                SoftHandle.RaiseException(ex, typeof(WeaponManager));
            }
        }

        protected override void UnloadData()
        {
            I = null;
            ActiveWeapons.Clear();
            GridWeapons.Clear();
            HeartData.I.OnGridAdd -= OnGridAdd;
            HeartData.I.OnGridRemove -= OnGridRemove;
        }

        int update25Ct = 0;
        public override void UpdateAfterSimulation()
        {
            if (HeartData.I.IsSuspended) return;

            if (!MyAPIGateway.Utilities.IsDedicated)
                HandleMouseShoot();

            update25Ct++;

            foreach (var weapon in ActiveWeapons.Values) // I cannot be asked to tease apart how to seperate updating on weapons
            {
                (weapon as SorterTurretLogic)?.UpdateTurretSubparts(deltaTick);
            }

            if (update25Ct >= 25)
            {
                Update25();
                update25Ct = 0;
            }
        }

        public void HandleMouseShoot()
        {
            IMyEntity controlledEntity = MyAPIGateway.Session.Player?.Controller?.ControlledEntity?.Entity?.GetTopMostParent(); // Get the currently controlled grid.
            IMyCubeGrid grid = controlledEntity as IMyCubeGrid;
            if (MyAPIGateway.Gui.IsCursorVisible || grid == null || !GridWeapons.ContainsKey(grid))
                return;

            bool isMousePressed = MyAPIGateway.Input.IsMousePressed(VRage.Input.MyMouseButtonsEnum.Left);

            foreach (var weapon in GridWeapons[grid])
                if (weapon.MouseShootState && weapon.ShootState != isMousePressed)
                    weapon.ShootState = isMousePressed;
        }

        public void Update25()
        {
            if (!MyAPIGateway.Session.IsServer || !MyAPIGateway.Multiplayer.MultiplayerActive)
                return;

            // HeartLog.Log("WeaponManager: Starting Update25");

            Dictionary<Vector3D, n_TurretFacing> facings = new Dictionary<Vector3D, n_TurretFacing>();

            try
            {
                foreach (var weaponKvp in ActiveWeapons)
                {
                    var weapon = weaponKvp.Value;
                    if (!(weapon is SorterTurretLogic))
                        continue;

                    SorterTurretLogic turret = weapon as SorterTurretLogic;

                    if (turret.SorterWep == null || turret.SorterWep.MarkedForClose)
                    {
                        HeartLog.Log($"WeaponManager: Skipping turret {weaponKvp.Key} because it's null or marked for close");
                        continue;
                    }

                    Vector3D position = turret.SorterWep.GetPosition();

                    if (facings.ContainsKey(position))
                    {
                        HeartLog.Log($"WeaponManager: Warning - Duplicate position detected for turret {weaponKvp.Key} at {position}");
                        continue;
                    }

                    facings.Add(position, new n_TurretFacing(turret));
                }

                //HeartLog.Log($"WeaponManager: Processed {facings.Count} turrets");

                foreach (var player in HeartData.I.Players)
                {
                    Vector3D playerPos = player.GetPosition();
                    List<n_TurretFacing> facingsForPlayer = new List<n_TurretFacing>();

                    foreach (var facing in facings)
                    {
                        if (Vector3D.DistanceSquared(facing.Key, playerPos) <= HeartData.I.SyncRangeSq)
                            facingsForPlayer.Add(facing.Value);
                    }

                    if (facingsForPlayer.Count == 0)
                        continue;

                    //HeartLog.Log($"WeaponManager: Sending {facingsForPlayer.Count} facings to player {player.SteamUserId}");
                    HeartData.I.Net.SendToPlayer(new n_TurretFacingArray(facingsForPlayer), player.SteamUserId);
                }
            }
            catch (Exception ex)
            {
                HeartLog.Log($"WeaponManager: Exception in Update25 - {ex.Message}");
                HeartLog.Log($"WeaponManager: Stack Trace - {ex.StackTrace}");
            }

            //HeartLog.Log("WeaponManager: Finished Update25");
        }

        public SorterWeaponLogic GetWeapon(uint id) => ActiveWeapons.GetValueOrDefault(id, null);
        /// <summary>
        /// By EntityId
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public SorterWeaponLogic GetWeapon(long id) => (MyAPIGateway.Entities.GetEntityById(id) as IMyCubeBlock)?.GameLogic as SorterWeaponLogic;
        public bool IsIdAvailable(uint id) => !ActiveWeapons.ContainsKey(id);
    }
}
