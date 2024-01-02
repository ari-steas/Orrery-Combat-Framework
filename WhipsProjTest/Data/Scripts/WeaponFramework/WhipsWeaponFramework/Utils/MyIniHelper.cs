using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace Whiplash.Utils
{
    public class MyIniHelper
    {
        /// <summary>
        /// Adds a Vector3D to a MyIni object
        /// </summary>
        public static void SetVector3(string sectionName, string vectorName, ref Vector3 vector, MyIni ini)
        {
            ini.Set(sectionName, vectorName, vector.ToString());
        }

        /// <summary>
        /// Parses a MyIni object for a Vector3D
        /// </summary>
        public static Vector3 GetVector3(string sectionName, string vectorName, MyIni ini, Vector3? defaultVector = null)
        {
            // Vector3 doesnt have a freaking TryParse method...
            var vector = Vector3D.Zero;
            var vectorString = ini.Get(sectionName, vectorName).ToString();
            vectorString = vectorString.Replace("{", "");
            vectorString = vectorString.Replace("}", "");
            if (Vector3D.TryParse(vectorString, out vector))
            {
                return (Vector3)vector;
            }

            if (defaultVector.HasValue)
            {
                return defaultVector.Value;
            }
            return default(Vector3);
        }
    }
}
