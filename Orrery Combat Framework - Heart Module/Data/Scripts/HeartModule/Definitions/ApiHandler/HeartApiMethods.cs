using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRageMath;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;
using static VRage.Game.MyObjectBuilder_BehaviorTreeActionNode;

namespace Heart_Module.Data.Scripts.HeartModule.Definitions.ApiHandler
{
    internal class HeartApiMethods
    {
        internal readonly Dictionary<string, Delegate> ModApiMethods;

        internal HeartApiMethods()
        {
            ModApiMethods = new Dictionary<string, Delegate>()
            {
                // Projectile LiveMethods
                ["AddOnProjectileSpawn"] = new Action<string, Action<uint, MyEntity>>(AddOnSpawn),
                ["AddOnProjectileImpact"] = new Action<string, Action<uint, Vector3D, Vector3D, MyEntity>>(AddOnImpact),
                ["AddOnEndOfLife"] = new Action<string, Action<uint>>(AddOnEndOfLife),
                //["AddOnGuidanceStage"] = new Action<string, Action<uint, Guidance?>>(AddOnGuidanceStage), // TODO: Cannot pass type Guidance or type ProjectileDefinitionBase!

                // Projectile Generics
                ["GetProjectileDefinitionId"] = new Func<string, int>(GetProjectileDefinitionId),
                ["GetProjectileDefinition"] = new Func<int, byte[]>(GetProjectileDefinition), // TODO: Allow projectiles/weapons to have independent definitions
                ["RegisterProjectileDefinition"] = new Func<byte[], int>(RegisterProjectileDefinition),
                ["UpdateProjectileDefinition"] = new Func<int, byte[], bool>(UpdateProjectileDefinition),

                // Weapon Generics
                ["BlockHasWeapon"] = new Func<MyEntity, bool>(HasWeapon),

                // Standard
                ["LogWriteLine"] = new Action<string>(HeartData.I.Log.Log),
            };
        }

        #region Projectile Methods
        public void AddOnSpawn(string definitionName,  Action<uint, MyEntity> onSpawn)
        {
            if (onSpawn == null)
                return;

            try
            {
                ProjectileDefinitionManager.GetDefinition(definitionName).LiveMethods.OnSpawn += onSpawn;
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException("Failed to call AddOnSpawn to projectile definition " + definitionName, ex, typeof(HeartApiMethods));
            }
        }
        public void AddOnImpact(string definitionName, Action<uint, Vector3D, Vector3D, MyEntity> onImpact)
        {
            if (onImpact == null)
                return;

            try
            {
                ProjectileDefinitionManager.GetDefinition(definitionName).LiveMethods.OnImpact += onImpact;
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException("Failed to call AddOnImpact to projectile definition " + definitionName, ex, typeof(HeartApiMethods));
            }
        }
        public void AddOnEndOfLife(string definitionName, Action<uint> onEol)
        {
            if (onEol == null)
                return;

            try
            {
                ProjectileDefinitionManager.GetDefinition(definitionName).LiveMethods.OnEndOfLife += onEol;
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException("Failed to call AddOnEndOfLife to projectile definition " + definitionName, ex, typeof(HeartApiMethods));
            }
        }
        //public void AddOnGuidanceStage(string definitionName, Action<uint, byte[]> onStage)
        //{
        //    if (onStage == null)
        //        return;
        //
        //    try
        //    {
        //        ProjectileDefinitionManager.GetDefinition(definitionName).LiveMethods.OnGuidanceStage += onStage;
        //    }
        //    catch
        //    {
        //        SoftHandle.RaiseException("Failed to call AddOnGuidanceStage to projectile definition " + definitionName, typeof(HeartApiMethods));
        //    }
        //}
        public int GetProjectileDefinitionId(string definitionName)
        {
            return ProjectileDefinitionManager.GetId(definitionName);
        }
        public byte[] GetProjectileDefinition(int definitionId)
        {
            ProjectileDefinitionBase def = ProjectileDefinitionManager.GetDefinition(definitionId);
            if (def == null)
                return null;
            return MyAPIGateway.Utilities.SerializeToBinary(def);
        }
        public int RegisterProjectileDefinition(byte[] serialized)
        {
            if (serialized == null)
                return -1;
            ProjectileDefinitionBase def = MyAPIGateway.Utilities.SerializeFromBinary<ProjectileDefinitionBase>(serialized);
            if (def == null)
                return -1;
            return ProjectileDefinitionManager.RegisterDefinition(def, true);
        }
        public bool UpdateProjectileDefinition(int definitionId, byte[] serialized)
        {
            if (serialized == null)
                return false;
            ProjectileDefinitionBase def = MyAPIGateway.Utilities.SerializeFromBinary<ProjectileDefinitionBase>(serialized);
            if (def == null)
                return false;
            return ProjectileDefinitionManager.ReplaceDefinition(definitionId, def, true);
        }
        #endregion

        #region Weapon Methods
        public bool HasWeapon(MyEntity block)
        {
            return block is IMyConveyorSorter && ((IMyConveyorSorter) block).GameLogic is SorterWeaponLogic;
        }
        #endregion
    }
}
