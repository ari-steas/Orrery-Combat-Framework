using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Heart_Module.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.UserInterface
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class TurretBarrelIndicator : GridBasedIndicator_Base
    {
        // TODO: Add global setting for indicator visibility
        const int MaxVisibleIndicators = 100;

        readonly MyStringId FixedMaterial = MyStringId.GetOrCompute("WhiteDot");
        readonly Vector4 FixedColor = new Vector4(1, 0.48f, 0, 0.5f);
        readonly MyStringId TurretMaterial = MyStringId.GetOrCompute("SquareFullColor");
        readonly Vector4 TurretColor = new Vector4(1 * 2, 0.48f * 2, 0, 1f);
        float viewDist = 10000;
        int numVisible = 0;

        public override void LoadData()
        {
            viewDist = MyAPIGateway.Multiplayer.MultiplayerActive ? MyAPIGateway.Session.SessionSettings.SyncDistance : MyAPIGateway.Session.SessionSettings.ViewDistance;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            numVisible = 0;
        }

        public override void PerWeaponUpdate(SorterWeaponLogic weapon)
        {
            if (numVisible > MaxVisibleIndicators)
                return;

            try // TODO: Fix error that occurs here
            {
                if (!weapon.SorterWep.IsWorking)
                    return;

                double dist = viewDist;
                MyStringId texture;
                Vector4 color;

                if (weapon is SorterTurretLogic)
                {
                    SorterTurretLogic turret = (SorterTurretLogic)weapon;
                    if (turret.AimPoint != Vector3D.MaxValue)
                        dist = Vector3D.Distance(turret.AimPoint, turret.MuzzleMatrix.Translation);
                    texture = TurretMaterial;
                    color = TurretColor;
                }
                else
                {
                    var targeting = WeaponManagerAi.I.GetTargeting(weapon.SorterWep.CubeGrid);
                    if (targeting != null && targeting.PrimaryGridTarget != null)
                        dist = Vector3D.Distance(targeting.PrimaryGridTarget.GetPosition(), weapon.MuzzleMatrix.Translation);
                    texture = FixedMaterial;
                    color = FixedColor;
                }

                Vector3D progradeCtr = weapon.MuzzleMatrix.Translation + (weapon.MuzzleMatrix.Forward * dist);
                float adjSymbolHeight = (float)dist / (40f / 70f * MyAPIGateway.Session.Camera.FieldOfViewAngle);
                var progradeTop = progradeCtr + MyAPIGateway.Session.Camera.WorldMatrix.Up * adjSymbolHeight;
                MySimpleObjectDraw.DrawLine(progradeTop, progradeTop - MyAPIGateway.Session.Camera.WorldMatrix.Up * adjSymbolHeight * 2, texture, ref color, adjSymbolHeight, MyBillboard.BlendTypeEnum.AdditiveTop); // Based on BDCarrillo's Flight Vector mod

                numVisible++;
            }
            catch { }
        }
    }
}
