using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;

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

                    SetApiMethod("AddOnProjectileSpawn", ref addOnProjectileSpawn);
                    SetApiMethod("LogWriteLine", ref logWriteLine);

                    HasInited = true;
                    LogWriteLine($"[{ModContext.ModName}] HeartAPI inited.");
                    OnLoad?.Invoke();
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Orrery Combat Framework: [{ModContext.ModName}] ERR: Failed to init HeartAPI! {ex}");
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

        private Action<string> logWriteLine;
        /// <summary>
        /// Prints a line to the HeartModule log.
        /// </summary>
        /// <param name="text"></param>
        public static void LogWriteLine(string text) => I?.logWriteLine?.Invoke($"[{I.ModContext.ModName}] {text}");
    }
}
