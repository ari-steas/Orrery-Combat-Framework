using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Whiplash.WeaponFramework
{
    class FrameworkConstants
    {
        // Tag for debugging
        public const string DEBUG_MSG_TAG = "Railgun Weapon Framework";
        public const string LOG_NAME = "RailgunFramework.log";
        public const string DAMAGE_LOG_NAME = "RailgunDamage.log";

        // Network traffic IDs
        public const ushort NETID_FIRE_SYNC = 1507;
        public const ushort NETID_RECHARGE_SYNC = 3896; 
    }
}