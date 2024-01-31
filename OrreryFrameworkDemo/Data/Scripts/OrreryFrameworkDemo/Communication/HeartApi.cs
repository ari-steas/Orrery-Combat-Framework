using OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication.ProjectileBases;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication
{
    public class HeartApi
    {
        public static HeartApi I;

        #region API Loading
        private const long HeartApiChannel = 8644; // https://xkcd.com/221/
        private Dictionary<string, Delegate> methodMap;
        private Action OnLoad;
        private IMyModContext ModContext;

        public static void LoadData(IMyModContext modContext, Action OnLoad = null)
        {
            if (I != null && HasInited)
                return;
            I = new HeartApi
            {
                OnLoad = OnLoad,
                ModContext = modContext
            };
            MyAPIGateway.Utilities.RegisterMessageHandler(HeartApiChannel, I.RecieveApiMethods);
            MyAPIGateway.Utilities.SendModMessage(HeartApiChannel, true);
            MyLog.Default.WriteLineAndConsole("Orrery Combat Framework: HeartAPI awaiting methods.");
        }

        public static void UnloadData()
        {
            if (I == null)
                return;

            MyAPIGateway.Utilities.UnregisterMessageHandler(HeartApiChannel, I.RecieveApiMethods);
            I = null;
        }

        private void RecieveApiMethods(object data)
        {
            try
            {
                if (data == null)
                    return;

                if (!HasInited && data is Dictionary<string, Delegate>)
                {
                    methodMap = (Dictionary<string, Delegate>)data;

                    // Standard
                    SetApiMethod("LogWriteLine", ref logWriteLine);

                    // Projectile LiveMethods
                    SetApiMethod("AddOnProjectileSpawn", ref addOnProjectileSpawn);
                    SetApiMethod("AddOnProjectileImpact", ref addOnImpact);
                    SetApiMethod("AddOnEndOfLife", ref addOnEndOfLife);
                    //SetApiMethod("AddOnGuidanceStage", ref addOnGuidanceStage); // TODO: Cannot pass type Guidance or type ProjectileDefinitionBase!

                    // Projectile Generics
                    SetApiMethod("GetProjectileDefinitionId", ref getProjectileDefinitionId);
                    SetApiMethod("GetProjectileDefinition", ref getProjectileDefinition);
                    SetApiMethod("RegisterProjectileDefinition", ref registerProjectileDefinition);
                    SetApiMethod("UpdateProjectileDefinition", ref updateProjectileDefinition);

                    // Weapon Generics
                    SetApiMethod("BlockHasWeapon", ref blockHasWeapon);

                    HasInited = true;
                    LogWriteLine($"HeartAPI inited.");
                    OnLoad?.Invoke();
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Orrery Combat Framework: [{ModContext.ModName}] ERR: Failed to init HeartAPI! {ex}");
                logWriteLine?.Invoke($"ERR: Failed to init HeartAPI! {ex}");
            }

            methodMap = null;
        }

        private void SetApiMethod<T>(string name, ref T method) where T : class
        {
            if (!methodMap.ContainsKey(name))
                throw new Exception("Method Map does not contain method " + name);
            Delegate del = methodMap[name];
            if (del.GetType() != typeof(T))
                throw new Exception($"Method {name} type mismatch! [MapMethod: {del.GetType().Name} | ApiMethod: {typeof(T).Name}]");
            method = methodMap[name] as T;
        }

        #endregion

        public static bool HasInited = false;

        private Action<string, Action<uint, MyEntity>> addOnProjectileSpawn;
        /// <summary>
        /// Adds an action triggered on projectile spawn.
        /// </summary>
        /// <param name="projectileDefinition"></param>
        /// <param name="onSpawn"></param>
        public static void AddOnProjectileSpawn(string projectileDefinition, Action<uint, MyEntity> onSpawn) => I?.addOnProjectileSpawn?.Invoke(projectileDefinition, onSpawn);

        private Action<string, Action<uint, Vector3D, Vector3D, MyEntity>> addOnImpact;
        /// <summary>
        /// Adds an action triggered on projectile impact.
        /// </summary>
        /// <param name="projectileDefinition"></param>
        /// <param name="onImpact"></param>
        public static void AddOnImpact(string projectileDefinition, Action<uint, Vector3D, Vector3D, MyEntity> onImpact) => I?.addOnImpact?.Invoke(projectileDefinition, onImpact);

        private Action<string, Action<uint>> addOnEndOfLife;
        /// <summary>
        /// Adds an action triggered on projectile end-of-life.
        /// </summary>
        public static void AddOnEndOfLife(string projectileDefinition, Action<uint> onEndOfLife) => I?.addOnEndOfLife?.Invoke(projectileDefinition, onEndOfLife);

        //private Action<string, Action<uint, Guidance?>> addOnGuidanceStage;
        /// <summary>
        /// Adds an action triggered when a projectile's guidance stages.
        /// </summary>
        //public static void AddOnGuidanceStage(string projectileDefinition, Action<uint, Guidance?> onStage) => I?.addOnGuidanceStage?.Invoke(projectileDefinition, onStage);


        private Func<string, int> getProjectileDefinitionId;
        public static int GetProjectileDefinitionId(string projectileName) => I?.getProjectileDefinitionId?.Invoke(projectileName) ?? -1;

        private Func<int, byte[]> getProjectileDefinition;
        public static ProjectileDefinitionBase GetProjectileDefinition(int projectileDefId)
        {
            byte[] serialized = I?.getProjectileDefinition?.Invoke(projectileDefId);
            if (serialized == null)
                return null;
            return MyAPIGateway.Utilities.SerializeFromBinary<ProjectileDefinitionBase>(serialized);
        }

        private Func<byte[], int> registerProjectileDefinition;
        public static int RegisterProjectileDefinition(ProjectileDefinitionBase definition) => I?.registerProjectileDefinition?.Invoke(MyAPIGateway.Utilities.SerializeToBinary(definition)) ?? -1;

        private Func<int, byte[], bool> updateProjectileDefinition;
        public static bool UpdateProjectileDefinition(int definitionId, ProjectileDefinitionBase definition) => I?.updateProjectileDefinition?.Invoke(definitionId, MyAPIGateway.Utilities.SerializeToBinary(definition)) ?? false;

        private Func<MyEntity, bool> blockHasWeapon;
        public static bool BlockHasWeapon(MyEntity block) => I?.blockHasWeapon?.Invoke(block) ?? false;



        private Action<string> logWriteLine;
        /// <summary>
        /// Prints a line to the HeartModule log.
        /// </summary>
        /// <param name="text"></param>
        public static void LogWriteLine(string text) => I?.logWriteLine?.Invoke($"[{I.ModContext.ModName}] {text}");
    }
}
