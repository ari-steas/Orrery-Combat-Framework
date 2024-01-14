using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.UserInterface
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class ReloadIndicator : GridBasedIndicator_Base
    {
        public override void LoadData()
        {
            
        }

        public override void UpdateAfterSimulation()
        {

        }

        public override void PerWeaponUpdate(SorterWeaponLogic weapon)
        {
            
        }
    }
}
