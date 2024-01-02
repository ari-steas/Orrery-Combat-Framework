using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;
using Whiplash.Utils;

namespace Whiplash.WeaponFramework
{
    public class FrameworkWeaponAPI
    {
        const long FIXED_GUN_REGESTRATION_NETID = 1411;
        const long TURRET_REGESTRATION_NETID = 1412;
        const long REGESTRATION_REQUEST_NETID = 1413;

        const ushort FIXED_GUN_CONFIG_SYNC_NETID = 50211;
        const ushort TURRET_CONFIG_SYNC_NETID = 58847;
        const ushort CLIENT_CONFIG_SYNC_REQUEST_NETID = 19402;
        const ushort SERVER_CONFIG_SYNC_REQUEST_FINISHED_NETID = 62508;

        public static ConcurrentDictionary<string, WeaponConfig> FixedGunWeaponConfigs = new ConcurrentDictionary<string, WeaponConfig>();
        public static ConcurrentDictionary<string, TurretWeaponConfig> TurretWeaponConfigs = new ConcurrentDictionary<string, TurretWeaponConfig>();

        public static void Register()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(FIXED_GUN_REGESTRATION_NETID, HandleFixedGunRegistration);
            MyAPIGateway.Utilities.RegisterMessageHandler(TURRET_REGESTRATION_NETID, HandleTurretRegistration);
            Logger.Default.WriteLine("Sending registration request");
            MyAPIGateway.Utilities.SendModMessage(REGESTRATION_REQUEST_NETID, null);
            if (!WeaponSession.IsServer)
            {
                MyAPIGateway.Multiplayer.RegisterMessageHandler(FIXED_GUN_CONFIG_SYNC_NETID, HandleFixedGunConfigSyncMessage);
                MyAPIGateway.Multiplayer.RegisterMessageHandler(TURRET_CONFIG_SYNC_NETID, HandleTurretConfigSyncMessage);
                MyAPIGateway.Multiplayer.RegisterMessageHandler(SERVER_CONFIG_SYNC_REQUEST_FINISHED_NETID, OnClientConfigSyncFinished);
            }
            else
            {
                MyAPIGateway.Multiplayer.RegisterMessageHandler(CLIENT_CONFIG_SYNC_REQUEST_NETID, ServerSendConfigSyncResponse);
            }
        }

        public static void Unregister()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(FIXED_GUN_REGESTRATION_NETID, HandleFixedGunRegistration);
            MyAPIGateway.Utilities.UnregisterMessageHandler(TURRET_REGESTRATION_NETID, HandleTurretRegistration);

            if (!WeaponSession.IsServer)
            {
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(FIXED_GUN_CONFIG_SYNC_NETID, HandleFixedGunConfigSyncMessage);
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(TURRET_CONFIG_SYNC_NETID, HandleTurretConfigSyncMessage);
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(SERVER_CONFIG_SYNC_REQUEST_FINISHED_NETID, OnClientConfigSyncFinished);
            }
            else
            {
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(CLIENT_CONFIG_SYNC_REQUEST_NETID, ServerSendConfigSyncResponse);
            }
        }

        #region Client Config Sync
        public static void SendClientConfigSyncRequest()
        {
            ulong clientId = MyAPIGateway.Multiplayer.MyId;
            var s = MyAPIGateway.Utilities.SerializeToBinary(clientId);
            Logger.Default.WriteLine($"Sending client config sync request ({clientId})");
            MyAPIGateway.Multiplayer.SendMessageToServer(CLIENT_CONFIG_SYNC_REQUEST_NETID, s);
        }

        static void ServerSendConfigSyncResponse(byte[] b)
        {
            try
            {
                ulong clientId = MyAPIGateway.Utilities.SerializeFromBinary<ulong>(b);
                Logger.Default.WriteLine($"Server sending configs to client {clientId}");

                if (clientId == MyAPIGateway.Multiplayer.MyId)
                {
                    Logger.Default.WriteLine($"Server and client have same id!", Logger.Severity.Warning);
                }

                foreach (var kvp in FixedGunWeaponConfigs)
                {
                    var s = MyAPIGateway.Utilities.SerializeToBinary(kvp.Value);
                    MyAPIGateway.Multiplayer.SendMessageTo(FIXED_GUN_CONFIG_SYNC_NETID, s, clientId);
                }

                foreach (var kvp in TurretWeaponConfigs)
                {
                    var s = MyAPIGateway.Utilities.SerializeToBinary(kvp.Value);
                    MyAPIGateway.Multiplayer.SendMessageTo(TURRET_CONFIG_SYNC_NETID, s, clientId);
                }

                MyAPIGateway.Multiplayer.SendMessageTo(SERVER_CONFIG_SYNC_REQUEST_FINISHED_NETID, new byte[0], clientId);
            }
            catch (Exception e)
            {
                Logger.Default.WriteLine($"Failed to send config sync response to clients!", Logger.Severity.Error);
                Logger.Default.WriteLine(e.ToString(), Logger.Severity.Error);
            }
        }

        static void OnClientConfigSyncFinished(byte[] b)
        {
            Logger.Default.WriteLine($"Config sync request finished!");
            WeaponSession.ConfigRefreshTick = WeaponSession.CurrentTick;

            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                MyAPIGateway.Utilities.ShowMessage(FrameworkConstants.DEBUG_MSG_TAG, $"Synced configs with server.");
            }
        }

        static void HandleFixedGunConfigSyncMessage(byte[] b)
        {
            try
            {
                if (b == null) return;
                WeaponConfig config = MyAPIGateway.Utilities.SerializeFromBinary<WeaponConfig>(b);
                FixedGunWeaponConfigs[config.BlockSubtype] = config;
                Logger.Default.WriteLine($"Received config update for fixed gun: {config.BlockSubtype}");
            }
            catch (Exception e)
            {
                Logger.Default.WriteLine($"Failed to deserialize fixed gun config sync!", Logger.Severity.Error);
                Logger.Default.WriteLine(e.ToString(), Logger.Severity.Error);
            }
        }

        static void HandleTurretConfigSyncMessage(byte[] b)
        {
            try
            {
                if (b == null) return;
                TurretWeaponConfig config = MyAPIGateway.Utilities.SerializeFromBinary<TurretWeaponConfig>(b);
                TurretWeaponConfigs[config.BlockSubtype] = config;
                Logger.Default.WriteLine($"Received config update for turret: {config.BlockSubtype}");
            }
            catch (Exception e)
            {
                Logger.Default.WriteLine($"Failed to deserialize turret config sync!", Logger.Severity.Error);
                Logger.Default.WriteLine(e.ToString(), Logger.Severity.Error);
            }
        }
        #endregion

        #region Registration
        public static void HandleFixedGunRegistration(object o)
        {
            try
            {
                var binaryMsg = o as byte[];
                if (binaryMsg == null) return;
                WeaponConfig config = MyAPIGateway.Utilities.SerializeFromBinary<WeaponConfig>(binaryMsg);
                bool isValid = ValidateConfig(config, true);
                if (isValid)
                {
                    FixedGunWeaponConfigs[config.BlockSubtype] = config;
                    WeaponSession.Instance.LoadConfig(config);
                }
            }
            catch (Exception e)
            {
                Logger.Default.WriteLine($"Failed to register fixed gun!", Logger.Severity.Error);
                Logger.Default.WriteLine(e.ToString(), Logger.Severity.Error);
            }
        }

        public static void HandleTurretRegistration(object o)
        {
            try
            {
                var binaryMsg = o as byte[];
                if (binaryMsg == null) return;

                TurretWeaponConfig config = MyAPIGateway.Utilities.SerializeFromBinary<TurretWeaponConfig>(binaryMsg);
                bool isValid = ValidateConfig(config, false);
                if (isValid)
                {
                    TurretWeaponConfigs[config.BlockSubtype] = config;
                    WeaponSession.Instance.LoadConfig(config);
                }
            }
            catch (Exception e)
            {
                Logger.Default.WriteLine($"Failed to register fixed gun!", Logger.Severity.Error);
                Logger.Default.WriteLine(e.ToString(), Logger.Severity.Error);
            }
        }

        static bool ValidateConfig(WeaponConfig config, bool isFixed)
        {
            string subtype = config.BlockSubtype;
            string id = config.ConfigID;
            if (string.IsNullOrWhiteSpace(subtype))
            {
                Logger.Default.WriteLine("Registration message with no sybtype. Ignoring...", Logger.Severity.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                Logger.Default.WriteLine($"Registration message from '{subtype}' with no ConfigID. Ignoring...", Logger.Severity.Warning);
                return false;
            }

            bool duped = false;
            if (isFixed)
            {
                duped = FixedGunWeaponConfigs.ContainsKey(config.BlockSubtype);
            }
            else
            {
                duped = TurretWeaponConfigs.ContainsKey(config.BlockSubtype);
            }

            if (duped)
            {
                Logger.Default.WriteLine($"Duplicate weapon BlockSubtype: '{config.BlockSubtype}'", Logger.Severity.Warning);
            }

            Logger.Default.WriteLine($"Registered weapon '{id}' for subtype '{subtype}'");
            return true;
        }
        #endregion
    }
}
