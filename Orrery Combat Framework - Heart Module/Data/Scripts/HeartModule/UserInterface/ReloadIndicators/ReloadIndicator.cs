using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using RichHudFramework.Client;
using RichHudFramework.UI.Client;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.UserInterface.ReloadIndicators
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class ReloadIndicator : GridBasedIndicator_Base
    {
        bool HasInitedHud = false;
        ReloadWindow Window;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
        }

        public override void Draw()
        {
            if (!RichHudClient.Registered)
                return;
            if (!HasInitedHud)
                InitHud();

            int numWeapons = 0;
            if (controlledGrid != null && WeaponManager.I.GridWeapons.ContainsKey(controlledGrid))
                numWeapons = WeaponManager.I.GridWeapons[controlledGrid].Count;
            Window.UpdateDebugText(ProjectileManager.I.NumProjectiles, numWeapons);
        }

        void InitHud()
        {
            Window = new ReloadWindow(HudMain.HighDpiRoot)
            {
                Visible = true,
            };

            HasInitedHud = true;
        }

        public override void PerWeaponUpdate(SorterWeaponLogic weapon)
        {

        }
    }
}
