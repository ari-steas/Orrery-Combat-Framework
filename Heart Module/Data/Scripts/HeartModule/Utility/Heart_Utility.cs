using Digi.Examples.NetworkProtobuf;
using Digi.NetworkLib;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;

namespace YourName.ModName.Data.Scripts.HeartModule.Utility
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Heart_Utility : MySessionComponentBase
    {
        public static Heart_Utility Instance;

        public bool ControlsCreated = false;
        public Network Network; // declare here without initializing
        public List<MyEntity> Entities = new List<MyEntity>();
        public PacketBlockSettings CachedPacketSettings;

        public override void LoadData()
        {
            Instance = this;
            Network = new Network(58969, null, false); // Initialize the Network object here.
            CachedPacketSettings = new PacketBlockSettings();

            // If there are any network registrations or initializations, they should go here too.
        }

        protected override void UnloadData()
        {
            Instance = null;

            if (Network != null)
            {
                Network.Dispose();
                Network = null;
            }
        }
    }
}
