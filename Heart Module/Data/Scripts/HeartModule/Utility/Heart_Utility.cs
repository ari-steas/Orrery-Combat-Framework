using Digi.Examples.NetworkProtobuf;
using Digi.NetworkLib;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;
//using YourName.ModName.Data.Scripts.OneFuckingFolderDeeper.StructuralIntegrity.Sync;

namespace YourName.ModName.Data.Scripts.HeartModule.Utility
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Heart_Utility : MySessionComponentBase
    {
        public static Heart_Utility Instance;

        public bool ControlsCreated = false;
        public Network Network  = new Network(58969, null, false);
        public List<MyEntity> Entities = new List<MyEntity>();
        public PacketBlockSettings CachedPacketSettings;


        public override void LoadData()
        {
            Instance = this;

          //  Networking.Register();

            CachedPacketSettings = new PacketBlockSettings();
        }

        protected override void UnloadData()
        {
            Instance = null;

            Network.Dispose();
            Network = null;
        }
    }
}
