using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using RichHudFramework.Client;
using RichHudFramework.UI.Client;
using VRage.Game;
using VRage.Game.Components;
using Heart_Module.Data.Scripts.HeartModule.Weapons.Setup.Adding;

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
            try // TODO: Fix error that throws here on DS
            {
                int numWeapons = 0;
                if (controlledGrid != null)
                {
                    numWeapons = WeaponManager.I.GridWeapons[controlledGrid].Count;
                    Window.UpdateWeaponText(WeaponManager.I.GridWeapons[controlledGrid]);
                }
                else
                    Window.ClearWeaponText();

                Window.UpdateDebugText(ProjectileManager.I.NumProjectiles, numWeapons, HeartData.I.Net.TotalNetworkLoad);
            }
            catch { }
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
            //Window?.UpdateWeaponText(weapon);
        }
    }

}
