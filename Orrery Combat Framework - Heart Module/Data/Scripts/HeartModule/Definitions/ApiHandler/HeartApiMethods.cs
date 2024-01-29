using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;

namespace Heart_Module.Data.Scripts.HeartModule.Definitions.ApiHandler
{
    internal class HeartApiMethods
    {
        internal readonly Dictionary<string, Delegate> ModApiMethods;

        internal HeartApiMethods()
        {
            ModApiMethods = new Dictionary<string, Delegate>()
            {
                ["AddOnProjectileSpawn"] = new Action<string, Action<uint, MyEntity>>(AddOnSpawn),
                ["LogWriteLine"] = new Action<string>(HeartData.I.Log.Log),
            };
        }

        public void AddOnSpawn(string definitionName,  Action<uint, MyEntity> onSpawn)
        {
            if (onSpawn == null)
                return;

            try
            {
                ProjectileDefinitionManager.GetDefinition(definitionName).LiveMethods.OnSpawn += onSpawn;
            }
            catch
            {
                SoftHandle.RaiseException("Failed to call AddOnSpawn to projectile definition " + definitionName, typeof(HeartApiMethods));
            }
        }
    }
}
