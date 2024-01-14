using RichHudFramework.UI;
using System;
using VRageMath;


namespace Heart_Module.Data.Scripts.HeartModule.UserInterface.ReloadIndicators
{
    internal class ReloadWindow : WindowBase
    {
        Label debugInfo;
        ListBox<uint> weaponStatus;

        public ReloadWindow(HudParentBase parent) : base(parent)
        {
            debugInfo = new Label(body)
            {
                ParentAlignment = ParentAlignments.Bottom | ParentAlignments.InnerV,
                DimAlignment = DimAlignments.Width
            };

            weaponStatus = new ListBox<uint>()
            {
                ParentAlignment = ParentAlignments.Top | ParentAlignments.InnerV,
                DimAlignment = DimAlignments.Width,
            };

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

        public void UpdateDebugText(int numProjectiles, int numWeapons)
        {
            debugInfo.Text = $"Projectiles: {numProjectiles} | Weapons: {numWeapons}";
        }

        public void UpdateWeaponText(uint weaponId)
        {

        }

        public void ClearWeaponText()
        {
            weaponStatus.ClearEntries();
        }
    }
}
