using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting
{
    internal class GridAiTargeting
    {
        IMyCubeGrid Grid;
        List<SorterWeaponLogic> Weapons => WeaponManager.I.GridWeapons[Grid];
        public bool IsAiEnabled { get; set; }

        public GridAiTargeting(IMyCubeGrid grid)
        {
            Grid = grid;
            Grid.OnBlockAdded += Grid_OnBlockAdded;
        }

        private void Grid_OnBlockAdded(IMySlimBlock obj)
        {
            
        }

        public void EnableAi(bool enable)
        {
            IsAiEnabled = enable;
            MyAPIGateway.Utilities.ShowNotification("Activated Ai" + enable);
        }

        public void UpdateTargeting()
        {

        }

        public void UpdateAvailableTargets(List<IMyCubeGrid> allGrids, List<IMyCharacter> allCharacters, List<uint> allProjectiles)
        {

        }

        public void Close()
        {

        }
    }
}
