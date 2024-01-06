using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Network;

namespace Heart_Module.Data.Scripts.HeartModule
{
    internal class HeartData
    {
        public static HeartData I;
        public const ushort HeartNetworkId = (ushort)(65198749845 % ushort.MaxValue);

        public bool IsSuspended = false;
        public HeartNetwork Net = new HeartNetwork();
        public HeartLog Log = new HeartLog();
    }
}
