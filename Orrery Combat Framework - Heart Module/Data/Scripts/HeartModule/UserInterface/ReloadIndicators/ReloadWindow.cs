using Heart_Module.Data.Scripts.HeartModule.Weapons;
using RichHudFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;


namespace Heart_Module.Data.Scripts.HeartModule.UserInterface.ReloadIndicators
{
    internal class ReloadWindow : WindowBase
    {
        Label debugInfo;
        Label debugInfo2;
        ListBox<uint> weaponStatus;

        public ReloadWindow(HudParentBase parent) : base(parent)
        {
            debugInfo = new Label(body)
            {
                ParentAlignment = ParentAlignments.Bottom | ParentAlignments.InnerV,
                DimAlignment = DimAlignments.Width,
            };

            debugInfo2 = new Label(body)
            {
                ParentAlignment = ParentAlignments.Bottom | ParentAlignments.InnerV,
                DimAlignment = DimAlignments.Width,
                Offset = new Vector2(0, debugInfo.Height),
            };

            weaponStatus = new ListBox<uint>(body)
            {
                ParentAlignment = ParentAlignments.Top | ParentAlignments.InnerV,
                DimAlignment = DimAlignments.Both,
                EnableScrolling = false,
                InputEnabled = false,
                Color = new Color(0, 0, 0, 0),
            };
            weaponStatus.hudChain.ScrollBar.Visible = false;

            // Window styling:
            BodyColor = new Color(41, 54, 62, 150);
            BorderColor = new Color(58, 68, 77);

            header.Format = new GlyphFormat(GlyphFormat.Blueish.Color, TextAlignment.Center, 1.08f);
            header.Height = 30f;

            HeaderText = "[OCF] HeartModule";
            Size = new Vector2(250f, 500f);
            Offset = new Vector2(835, 290); // Top-right corner; relative to 1920x1080
        }

        protected override void Layout()
        {
            base.Layout();

            MinimumSize = new Vector2(Math.Max(1, MinimumSize.X), MinimumSize.Y);
        }

        public void UpdateDebugText(int numProjectiles, int numWeapons, int networkLoad)
        {
            debugInfo.Text = $"Projectiles: {numProjectiles} | Weapons: {numWeapons}";
            debugInfo2.Text = $"Estimated Network Load: {Math.Round(networkLoad / 1000f, 1)}kb/s";
        }

        public void UpdateWeaponText(SorterWeaponLogic weapon)
        {
            if (!weapon.SorterWep.ShowInTerminal) // Hide weapons that aren't shown in terminal
                return;

            //MyAPIGateway.Utilities.ShowMessage("OCF", "Show weapon " + weapon.Id);
            var entry = GetEntry(weapon.Id);
            if (entry == null)
                entry = weaponStatus.Add("AWAIT INIT", weapon.Id);


            string targetStatus = "";
            if (weapon is SorterTurretLogic)
            {
                SorterTurretLogic turret = weapon as SorterTurretLogic;
                if (turret.AimPoint != Vector3D.MaxValue)
                    targetStatus = (turret.IsTargetAligned ? "" : "ALIGN") + (turret.IsTargetInRange ? "" : " RANGE") + (turret.HasLoS ? "" : " LOS");
                else
                    targetStatus = "NO TARGET";
            }

            string ammoStatus = $"{weapon.Magazines.ShotsInMag}/10"; // Placeholder value for max ammo
            if (weapon.Magazines.ShotsInMag == 0)
                ammoStatus = $"{Math.Round(weapon.Magazines.NextReloadTime, 1)}";

            entry.Element.Text = $"{weapon.Id}: [{ammoStatus}] {targetStatus}";
        }

        private ListBoxEntry<uint> GetEntry(uint id)
        {
            foreach (var value in weaponStatus.EntryList)
                if (value.AssocMember == id)
                    return value;
            return null;
        }

        public void UpdateWeaponText(List<SorterWeaponLogic> weapons)
        {
            foreach (var weaponTextId in weaponStatus.EntryList.ToArray()) // Check to see if any list items should be removed
            {
                bool shouldRemove = true;
                foreach (var weapon in weapons)
                {
                    if (weaponTextId.AssocMember == weapon.Id)
                    {
                        shouldRemove = !weapon.SorterWep.ShowInTerminal;
                        break;
                    }
                }
                if (shouldRemove)
                    weaponStatus.Remove(weaponTextId);
            }

            foreach (var weapon in weapons)
                UpdateWeaponText(weapon);
        }

        public void ClearWeaponText()
        {
            weaponStatus.ClearEntries();
        }
    }
}
