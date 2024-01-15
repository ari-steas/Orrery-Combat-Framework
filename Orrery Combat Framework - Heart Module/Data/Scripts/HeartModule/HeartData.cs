using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Network;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace Heart_Module.Data.Scripts.HeartModule
{
    internal class HeartData
    {
        public static HeartData I;
        public const ushort HeartNetworkId = (ushort)(65198749845 % ushort.MaxValue);

        public bool IsSuspended = false;
        public bool IsPaused = false;
        public HeartNetwork Net = new HeartNetwork();
        public HeartLog Log = new HeartLog();
        public int SyncRange = MyAPIGateway.Session.SessionSettings.SyncDistance;
        public int SyncRangeSq = MyAPIGateway.Session.SessionSettings.SyncDistance * MyAPIGateway.Session.SessionSettings.SyncDistance;
        public Random Random = new Random();
        public ulong SteamId = 0;
        public List<IMyPlayer> Players = new List<IMyPlayer>();
        public bool DegradedMode = false;
        public IMyPlayer GetPlayerFromSteamId(ulong id)
        {
            foreach (var player in Players)
                if (player.SteamUserId == id)
                    return player;
            return null;
        }
    }
}
