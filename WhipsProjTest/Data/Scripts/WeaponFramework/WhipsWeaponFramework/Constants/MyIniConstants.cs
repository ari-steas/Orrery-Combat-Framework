using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Whiplash.WeaponFramework
{
    public class MyIniConstants
    {
        /* 
        **          NO TOUCH!
        ** These keys should ALL be unique 
        */
        public const string INI_KEY_ART_GRAV = "Artificial gravity multiplier";
        public const string INI_KEY_NAT_GRAV = "Natural gravity multiplier";
        public const string INI_KEY_DRAW_TRAILS = "Draw projectile trails";
        public const string INI_KEY_TRAIL_DECAY = "Projectile trail decay ratio";
        public const string INI_KEY_SHOULD_EXP = "Explode on contact";
        public const string INI_KEY_EXP_RAD = "Contact Explosion radius (m)";
        public const string INI_KEY_EXP_DMG = "Contact Explosion damage";
        public const string INI_KEY_PEN = "Penetrate on contact";
        public const string INI_KEY_PEN_DMG = "Penetration damage pool";
        public const string INI_KEY_PEN_RANGE = "Penetration range";

        public const string INI_KEY_SHOULD_EXP_PEN = "Explode after penetration";
        public const string INI_KEY_EXP_PEN_RAD = "Penetration explosion radius (m)";
        public const string INI_KEY_EXP_PEN_DMG = "Penetration explosion damage";

        public const string INI_KEY_PWR_IDLE = "Idle power (MW)";
        public const string INI_KEY_PWR_RELOAD = "Reload power (MW)";
        public const string INI_KEY_MUZZLE_VEL = "Muzzle velocity (m/s)";
        public const string INI_KEY_MAX_RANGE = "Max range (m)";
        public const string INI_KEY_DEVIANCE = "Spread angle (°)";
        public const string INI_KEY_TRACER_SCALE = "Tracer scale";
        public const string INI_KEY_RECOIL = "Recoil impulse";
        public const string INI_KEY_IMPULSE = "Impact impulse";
        public const string INI_KEY_SHIELD_MULT = "Shield Damage Multiplier (for DarkStar's Shield Mod)";
        public const string INI_KEY_ROF = "Rate of fire (RPM)";
        public const string INI_KEY_TRACER_COLOR = "Tracer color (RGB where color channels range from 0 to 1)";
        public const string INI_KEY_PROXIMITY_DET = "Should proximity detonate";
        public const string INI_KEY_PROXIMITY_DET_RANGE = "Proximity detonation radius (m)";
        public const string INI_KEY_PROXIMITY_DET_ARM_RANGE = "Proximity detonation arming range (m)";
        public const string INI_KEY_CONFIG_VERSION_KEY = "Config version key";

        public const string INI_VALUE_DEFAULT_VERSION_KEY = "__default__";

        public const string INI_KEY_TURRET_PWR_MIN_RANGE = "Turret idle power at min range (MW)";
        public const string INI_KEY_TURRET_PWR_MAX_RANGE = "Turret idle power at max range (MW)";
    }
}
