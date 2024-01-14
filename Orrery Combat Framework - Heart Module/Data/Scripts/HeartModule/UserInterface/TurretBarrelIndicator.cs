using Heart_Module.Data.Scripts.HeartModule.Debug;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Render.Scene;
using VRage.Utils;
using VRageMath;
using VRageRender;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.UserInterface
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class TurretBarrelIndicator : GridBasedIndicator_Base
    {
        // TODO: Add global setting for indicator visibility

        readonly MyStringId FixedMaterial = MyStringId.GetOrCompute("WhiteDot");
        readonly Vector4 FixedColor = new Vector4(1, 0.48f, 0, 0.5f);
        readonly MyStringId TurretMaterial = MyStringId.GetOrCompute("SquareFullColor");
        readonly Vector4 TurretColor = new Vector4(1 * 2, 0.48f * 2, 0, 1f);
        float viewDist = 10000;

        public override void LoadData()
        {
            viewDist = MyAPIGateway.Multiplayer.MultiplayerActive ? MyAPIGateway.Session.SessionSettings.SyncDistance : MyAPIGateway.Session.SessionSettings.ViewDistance;
        }

        public override void PerWeaponUpdate(SorterWeaponLogic weapon)
        {
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
                    texture = FixedMaterial;
                    color = FixedColor;
                }

                Vector3D progradeCtr = weapon.MuzzleMatrix.Translation + (weapon.MuzzleMatrix.Forward * dist);
                float adjSymbolHeight = (float)dist / (40f / 70f * MyAPIGateway.Session.Camera.FieldOfViewAngle);
                var progradeTop = progradeCtr + MyAPIGateway.Session.Camera.WorldMatrix.Up * adjSymbolHeight;
                MySimpleObjectDraw.DrawLine(progradeTop, progradeTop - MyAPIGateway.Session.Camera.WorldMatrix.Up * adjSymbolHeight * 2, texture, ref color, adjSymbolHeight, MyBillboard.BlendTypeEnum.AdditiveTop); // Based on BDCarrillo's Flight Vector mod
            }
            catch { }
        }
    }
}
