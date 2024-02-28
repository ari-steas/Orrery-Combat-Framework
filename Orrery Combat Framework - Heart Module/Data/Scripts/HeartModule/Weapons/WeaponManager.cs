using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using YourName.ModName.Data.Scripts.HeartModule.Weapons;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

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

                    AddWeapon(block);
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
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            grid.GetBlocks(null, b =>
            {
                if (b.FatBlock is IMyConveyorSorter)
                    blocks.Add(b);
                return false;
            });
            if (!GridWeapons.ContainsKey(grid))
                GridWeapons.Add(grid, new List<SorterWeaponLogic>());
            foreach (var block in blocks)
                OnBlockAdd(block);
            grid.OnBlockAdded += OnBlockAdd;
        }

        /// <summary>
        /// Removes grids from the GridWeapons list
        /// </summary>
        /// <param name="entity"></param>
        private void OnGridRemove(IMyCubeGrid grid)
        {
            GridWeapons.Remove(grid);
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
            WeaponDefinitionBase def = WeaponDefinitionManager.GetDefinition(sorter.BlockDefinition.SubtypeName);
            SorterWeaponLogic logic;

            while (!IsIdAvailable(NextId))
                NextId++;

            if (def.Assignments.IsTurret)
                logic = new SorterTurretLogic(sorter, def, NextId);
            else
                logic = new SorterWeaponLogic(sorter, def, NextId);

            ActiveWeapons.Add(NextId, logic);
            GridWeapons[sorter.CubeGrid].Add(logic); // Add to grid list

            sorter.OnMarkForClose += (a) =>
            {
                ActiveWeapons.Remove(NextId);
                List<SorterWeaponLogic> values;
                GridWeapons.TryGetValue(sorter.CubeGrid, out values);
                values?.Remove(logic);
            };

            Heart_Settings.RequestSync(sorter.EntityId);


            if (logic.Id == uint.MaxValue)
            {
                logic.Close();
                CriticalHandle.ThrowCriticalException(new System.Exception($"Failed to initialize weapon! Subtype: {sorter.BlockDefinition.SubtypeId}"), typeof(WeaponManager));
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
            if (grid == null || !GridWeapons.ContainsKey(grid))
                return;

            bool isMousePressed = MyAPIGateway.Input.IsMousePressed(VRage.Input.MyMouseButtonsEnum.Left);

            foreach (var weapon in GridWeapons[grid])
                if (weapon.MouseShootState && weapon.ShootState != isMousePressed)
                    weapon.ShootState = isMousePressed;
        }

        public void Update25()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            Dictionary<Vector3D, n_TurretFacing> facings = new Dictionary<Vector3D, n_TurretFacing>(); // TODO: Limit the max number of syncs by network load, and also by player distance
            foreach (var weapon in ActiveWeapons.Values)
            {
                if (!(weapon is SorterTurretLogic))
                    continue;

                SorterTurretLogic turret = weapon as SorterTurretLogic;
                facings.Add(turret.SorterWep.GetPosition(), new n_TurretFacing(turret));
            }

            // Construct a BIG FUCKOFF ARRAY for every player !!! WOO !!!
            // for NETWORK PERFORMANCE
            // !!!
            foreach (var player in HeartData.I.Players)
            {
                Vector3D playerPos = player.GetPosition();
                List<n_TurretFacing> facingsForPlayer = new List<n_TurretFacing>();

                foreach (var facing in facings)
                    if (Vector3D.DistanceSquared(facing.Key, playerPos) <= HeartData.I.SyncRangeSq)
                        facingsForPlayer.Add(facing.Value);

                if (facingsForPlayer.Count == 0)
                    continue;

                HeartData.I.Net.SendToPlayer(new n_TurretFacingArray(facingsForPlayer), player.SteamUserId);
            }
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
