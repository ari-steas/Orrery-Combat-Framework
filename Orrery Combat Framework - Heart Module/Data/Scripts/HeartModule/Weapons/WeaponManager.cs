using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class WeaponManager : MySessionComponentBase
    {
        public static WeaponManager I;

        internal Dictionary<uint, SorterWeaponLogic> ActiveWeapons = new Dictionary<uint, SorterWeaponLogic>();
        private uint NextId = 0;
        public Dictionary<IMyCubeGrid, List<SorterWeaponLogic>> GridWeapons = new Dictionary<IMyCubeGrid, List<SorterWeaponLogic>>(); // EntityId based because IMyCubeGrid keys break garbage collection

        /// <summary>
        /// Delta for engine ticks; 60tps
        /// </summary>
        private const float deltaTick = 1 / 60f;

        public override void LoadData()
        {
            I = this;
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;
        }

        /// <summary>
        /// Check if new grids contain valid weapons.
        /// </summary>
        /// <param name="entity"></param>
        private void OnEntityAdd(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid))
                return;
            IMyCubeGrid grid = (IMyCubeGrid)entity;

            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            grid.GetBlocks(null, b =>
            {
                if (b.FatBlock is IMyConveyorSorter)
                    blocks.Add(b);
                return false;
            });
            GridWeapons.Add(grid, new List<SorterWeaponLogic>());
            foreach (var block in blocks)
                OnBlockAdd(block);
            grid.OnBlockAdded += OnBlockAdd;
        }

        /// <summary>
        /// Removes grids from the GridWeapons list
        /// </summary>
        /// <param name="entity"></param>
        private void OnEntityRemove(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid))
                return;
            IMyCubeGrid grid = (IMyCubeGrid)entity;
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
            SerializableWeaponDefinition def = WeaponDefinitionManager.GetDefinition(sorter.BlockDefinition.SubtypeName);
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
        }

        protected override void UnloadData()
        {
            I = null;
            ActiveWeapons.Clear();
            GridWeapons.Clear();
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;
        }

        public override void UpdateBeforeSimulation()
        {
            if (HeartData.I.IsSuspended) return;
        }

        public SorterWeaponLogic GetWeapon(uint id) => ActiveWeapons.GetValueOrDefault(id, null);
        public bool IsIdAvailable(uint id) => !ActiveWeapons.ContainsKey(id);
    }
}
