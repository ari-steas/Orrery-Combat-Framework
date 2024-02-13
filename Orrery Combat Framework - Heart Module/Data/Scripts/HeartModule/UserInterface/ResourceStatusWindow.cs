using RichHudFramework.Client;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using VRage.Game.Components;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.UserInterface
{

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class ResourceIndicator : MySessionComponentBase
    {
        private bool _hasInitedHud = false;
        private ResourceStatusWindow _window;

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (!RichHudClient.Registered)
                return;

            if (!_hasInitedHud)
                InitHud();

            try
            {
                // Here you would fetch the current resource count and update the window.
                // For demonstration purposes, let's assume a method GetResourceCount() returns an int.
                int resourceCount = GetResourceCount(); // You need to implement this method.
                _window.UpdateResourceInfo($"Resources: {resourceCount}");
            }
            catch { /* Handle any exceptions appropriately */ }
        }

        private void InitHud()
        {
            _window = new ResourceStatusWindow(HudMain.HighDpiRoot)
            {
                Visible = true,
            };

            _hasInitedHud = true;
        }

        private int GetResourceCount()
        {
            // Implement logic to retrieve the current resource count
            return 0; // Placeholder return
        }
    }

    internal class ResourceStatusWindow : WindowBase
    {
        private Label _resourceInfo;

        public ResourceStatusWindow(HudParentBase parent) : base(parent)
        {
            _resourceInfo = new Label(body)
            {
                ParentAlignment = ParentAlignments.Top | ParentAlignments.InnerH | ParentAlignments.InnerV,
                Text = "Resource Status: Initializing...",
                AutoResize = false,
                DimAlignment = DimAlignments.Both,
            };

            // Window styling
            BodyColor = new Color(41, 54, 62, 150);
            BorderColor = new Color(58, 68, 77);

            header.Format = new GlyphFormat(Vector4.One, TextAlignment.Center);
            header.Height = 30f;

            HeaderText = "Resource Status";
            Size = new Vector2(250f, 150f);
            // Adjust Offset here to align it with the ReloadWindow but a bit to the left
            Offset = new Vector2(580, 464); 
        }

        public void UpdateResourceInfo(string infoText)
        {
            _resourceInfo.Text = infoText;
        }
    }

}
