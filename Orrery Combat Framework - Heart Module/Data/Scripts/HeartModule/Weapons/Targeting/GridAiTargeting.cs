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
        IMyCubeGrid grid;
        List<SorterWeaponLogic> Weapons => WeaponManager.I.GridWeapons[grid];

        public void Update()
        {

        }
    }
}
