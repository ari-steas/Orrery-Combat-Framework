using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.Targeting
{
    internal class GridAiTargeting
    {
        IMyCubeGrid Grid;
        List<SorterWeaponLogic> Weapons => WeaponManager.I.GridWeapons[Grid];

        public GridAiTargeting(IMyCubeGrid grid)
        {
            Grid = grid;
            Grid.OnBlockAdded += Grid_OnBlockAdded;
        }

        private void Grid_OnBlockAdded(IMySlimBlock obj)
        {
            
        }

        public void Update()
        {

        }

        public void Close()
        {

        }
    }
}
