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
    public class TurretBarrelIndicator : MySessionComponentBase
    {
        readonly MyStringId FixedMaterial = MyStringId.GetOrCompute("WeaponLaser");
        readonly Vector4 FixedColor = new Vector4(0, 0, 1, 1f);
        readonly MyStringId TurretMaterial = MyStringId.GetOrCompute("WeaponLaser");
        readonly Vector4 TurretColor = new Vector4(1, 0, 0, 1f);

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            IMyEntity controlledEntity = MyAPIGateway.Session.Player?.Controller?.ControlledEntity?.Entity?.GetTopMostParent(); // Get the currently controlled grid.
            if (!(controlledEntity is IMyCubeGrid))
                return;
            IMyCubeGrid controlledGrid = (IMyCubeGrid) controlledEntity; // TODO: Make work on subparts

            MyAPIGateway.Utilities.ShowNotification("Weapons: " + (WeaponManager.I.GridWeapons[controlledGrid]?.Count), 1000/60);

            foreach (var gridWeapon in WeaponManager.I.GridWeapons[controlledGrid])
                UpdateIndicator(gridWeapon);
        }

        public void UpdateIndicator(SorterWeaponLogic weapon)
        {
            Vector3D progradeCtr = Session.Player.GetPosition() + (weapon.MuzzleMatrix.Forward * HeartData.I.SyncRange);
            MyStringId texture;
            Vector4 color;

            if (weapon is SorterTurretLogic)
            {
                //SorterTurretLogic turret = (SorterTurretLogic) weapon;
                texture = TurretMaterial;
                color = TurretColor;
            }
            else
            {
                texture = FixedMaterial;
                color = FixedColor;
            }

            //var progradeScreenCtr = Session.Camera.WorldToScreen(ref progradeCtr);
            //if (progradeScreenCtr.Z < 1)
            //{
            //    var edgeX = (float)(Session.Camera.ViewportSize.X * 0.5 + (progradeScreenCtr.X * Session.Camera.ViewportSize.X * 0.5));
            //    var edgeY = (float)(Session.Camera.ViewportSize.Y * 0.5 - (progradeScreenCtr.Y * Session.Camera.ViewportSize.Y * 0.5));
            //    var WorldCtr = Session.Camera.WorldLineFromScreen(new Vector2(edgeX, edgeY));
            //    var dirToCtr = Vector3D.Normalize(progradeCtr - Session.Camera.Position);
            //    WorldCtr.From += dirToCtr * 3 * 70 / MyAPIGateway.Session.Camera.FieldOfViewAngle;
            //    var tempadjSymbolHeight = 40 * 0.00275f;
            //    var tempTop = WorldCtr.From + MyAPIGateway.Session.Camera.WorldMatrix.Up * tempadjSymbolHeight;
            //    var tempBottom = WorldCtr.From - MyAPIGateway.Session.Camera.WorldMatrix.Up * tempadjSymbolHeight;
            //    var boldColor = color;//Chg
            //    boldColor.W = 0.75f;
            //    MySimpleObjectDraw.DrawLine(tempTop, tempBottom, texture, ref boldColor, tempadjSymbolHeight, MyBillboard.BlendTypeEnum.PostPP);
            //}

            var adjSymbolHeight = 40 / 70 * MyAPIGateway.Session.Camera.FieldOfViewAngle;
            var progradeTop = progradeCtr + MyAPIGateway.Session.Camera.WorldMatrix.Up * adjSymbolHeight;
            MySimpleObjectDraw.DrawLine(progradeTop, progradeTop - MyAPIGateway.Session.Camera.WorldMatrix.Up * adjSymbolHeight * 2, texture, ref color, adjSymbolHeight, MyBillboard.BlendTypeEnum.AdditiveTop);
        }
    }
}
