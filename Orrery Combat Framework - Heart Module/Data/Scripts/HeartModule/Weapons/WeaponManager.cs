using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class WeaponManager : MySessionComponentBase
    {
        public static WeaponManager I;

        private Dictionary<uint, SorterWeaponLogic> ActiveWeapons = new Dictionary<uint, SorterWeaponLogic>();
        private uint NextId = 0;

        /// <summary>
        /// Delta for engine ticks; 60tps
        /// </summary>
        private float deltaTick = 0;
        private Stopwatch clockTick = Stopwatch.StartNew();

        public override void LoadData()
        {
            I = this;
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
        }

        /// <summary>
        /// Check if new grids contain valid weapons.
        /// </summary>
        /// <param name="entity"></param>
        private void OnEntityAdd(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid))
                return;
            IMyCubeGrid grid = (IMyCubeGrid) entity;

            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            grid.GetBlocks(null, b =>
            {
                if (b.FatBlock is IMyConveyorSorter)
                    blocks.Add(b);
                return false;
            });
            foreach (var block in blocks)
                OnBlockAdd(block);
            grid.OnBlockAdded += OnBlockAdd;
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
            if (def.Assignments.IsTurret)
                logic = new SorterTurretLogic(sorter, def);
            else
                logic = new SorterWeaponLogic(sorter, def);

            NextId++;
            while (!IsIdAvailable(NextId))
                NextId++;
            ActiveWeapons.Add(NextId, logic);
        }

        protected override void UnloadData()
        {
            I = null;
        }

        public override void UpdateBeforeSimulation()
        {
            if (HeartData.I.IsSuspended) return;

            // Delta time for tickrate-independent weapon movement
            deltaTick = clockTick.ElapsedTicks / (float)TimeSpan.TicksPerSecond;
            clockTick.Restart();
        }

        public SorterWeaponLogic GetWeapon(uint id) => ActiveWeapons.GetValueOrDefault(id, null);
        public bool IsIdAvailable(uint id) => !ActiveWeapons.ContainsKey(id);
    }
}
