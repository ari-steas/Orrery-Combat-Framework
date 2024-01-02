using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using Rexxar;
using Rexxar.Communication;
using Whiplash.WeaponProjectiles;
using VRage.ModAPI;
using Sandbox.Game.Entities;
using VRageMath;
using VRage.Utils;
using VRage.Game.ModAPI.Ingame.Utilities;
using Whiplash.Utils;
using Whiplash.WeaponTracers;

namespace Whiplash.WeaponFramework
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation | MyUpdateOrder.BeforeSimulation, 1)]
    public class WeaponSession : MySessionComponentBase
    {
        #region Member Fields
        public static bool SessionInit { get; private set; } = false;
        public static bool IsServer;
        static List<WeaponProjectile> _realProjectiles = new List<WeaponProjectile>();
        static List<WeaponProjectileShadow> _shadowProjectiles = new List<WeaponProjectileShadow>();
        static List<WeaponTracer> _tracers = new List<WeaponTracer>();

        long _ticksSinceConfigLoad = 0;
        public static long ConfigRefreshTick = -1;
        public static long CurrentTick { get; private set; }
        readonly MyIni myIni = new MyIni();

        // Shield Api
        internal static WeaponSession Instance { get; private set; } // DS - Allow access from gamelogic
        public bool ShieldMod { get; set; }
        public bool ShieldApiLoaded { get; set; }
        public ShieldApi ShieldApi = new ShieldApi();
        #endregion

        #region Update and Init

        public override void BeforeStart()
        {
            base.BeforeStart();

            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                MyAPIGateway.Utilities.ShowMessage(FrameworkConstants.DEBUG_MSG_TAG, $"Registered {FrameworkWeaponAPI.FixedGunWeaponConfigs.Count} fixed guns and {FrameworkWeaponAPI.TurretWeaponConfigs.Count} turrets.");
            }

            foreach (var mod in MyAPIGateway.Session.Mods)
                if (mod.PublishedFileId == 1365616918) ShieldMod = true; //DS - detect shield is installed
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (!SessionInit)
            {
                SessionInit = true;
                
                Communication.Register();
            }

            ++CurrentTick;
            if (CurrentTick % 10 == 0)
                Settings.SyncSettings();

            ++_ticksSinceConfigLoad;
            if (!IsServer && _ticksSinceConfigLoad == 100)
            {
                FrameworkWeaponAPI.SendClientConfigSyncRequest();
            }
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (ShieldMod && !ShieldApiLoaded && ShieldApi.Load()) // DS - Init API.
                ShieldApiLoaded = true;

            SimulateProjectiles();
        }
        #endregion

        #region Projectile Methods
        private static void SimulateProjectiles()
        {
            // Simulate Real Projectiles only serverside
            if (IsServer)
            {
                for (int i = _realProjectiles.Count - 1; i >= 0; i--)
                {
                    var projectile = _realProjectiles[i];
                    projectile.Update();

                    if (projectile.Killed)
                        _realProjectiles.RemoveAt(i);
                }
            }

            // Simulate projectile shadows on clients
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                for (int i = _shadowProjectiles.Count - 1; i >= 0; --i)
                {
                    var shadow = _shadowProjectiles[i];
                    shadow.Update();

                    if (shadow.Remove)
                    {
                        _shadowProjectiles.RemoveAt(i);
                    }
                }
            }
        }

        public static void ShootProjectile(WeaponFireData fireData, WeaponConfig config)
        {
            var projectile = new WeaponProjectile(fireData, config);
            AddProjectile(projectile);

            WeaponFireSyncData fireSync = new WeaponFireSyncData()
            {
                Origin = (Vector3)fireData.Origin,
                Direction = (Vector3)projectile.DeviatedDirection,
                ShooterVelocity = (Vector3)fireData.ShooterVelocity,
                TracerColor = config.TracerColor,
                DrawTrails = config.ShouldDrawProjectileTrails,
                ProjectileTrailScale = config.TracerScale,
                TrailDecayRatio = config.ProjectileTrailFadeRatio,
                MuzzleVelocity = config.MuzzleVelocity,
                MaxRange = config.MaxRange,
                ArtGravityMult = config.ArtificialGravityMultiplier,
                NatGravityMult = config.NaturalGravityMultiplier,
                ShooterID = fireData.ShooterID,
                DrawImpactSprite = config.DrawImpactSprite,
                ShouldProximityDetonate = config.ShouldProximityDetonate,
                ProximityDetonationRange = config.ProximityDetonationRange,
                ProximityDetonationArmingRange = config.ProximityDetonationArmingRange,
            };
            WeaponSync.SendToClients(fireSync);
        }

        public static void CreateShadow(WeaponFireSyncData fireSyncData)
        {
            var shadow = new WeaponProjectileShadow(fireSyncData);
            _shadowProjectiles.Add(shadow);
        }

        public static void AddTracer(WeaponTracer tracerData)
        {
            _tracers.Add(tracerData);
        }

        public override void Draw()
        {
            base.Draw();

            // Update tracers
            for (int i = _tracers.Count - 1; i >= 0; --i)
            {
                var tracer = _tracers[i];
                tracer.Draw();

                if (tracer.Remove)
                    _tracers.RemoveAt(i);
            }
        }

        public static void AddProjectile(WeaponProjectile projectile)
        {
            _realProjectiles.Add(projectile);
        }
        #endregion

        #region Load and Unload Data

        public override void LoadData()
        {
            base.LoadData();
            IsServer = MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;
            Instance = this; // DS - assign Session instance.
            Logger.CreateDefault(FrameworkConstants.LOG_NAME, FrameworkConstants.DEBUG_MSG_TAG);
            WeaponProjectileShared.DamageLog = new Logger(FrameworkConstants.DAMAGE_LOG_NAME, FrameworkConstants.DEBUG_MSG_TAG);

            Logger.Default.WriteLine($"World storage path: {MyAPIGateway.Session.CurrentPath}\\Storage\\{ModContext.ModId}");
            Logger.Default.WriteLine($"Local storage path: {MyAPIGateway.Utilities.GamePaths.UserDataPath}\\Storage\\{ModContext.ModId}");

            FrameworkWeaponAPI.Register();
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            Logger.Default.Close();
            WeaponProjectileShared.DamageLog.Close();

            Communication.Unregister();
            FrameworkWeaponAPI.Unregister();
            if (ShieldApiLoaded) ShieldApi.Unload(); // DS - unload api
            Instance = null; // DS - null Instance method.
        }
        #endregion

        #region Config Save and Load
        public void LoadConfig(WeaponConfig config)
        {
            _ticksSinceConfigLoad = 0;
            string settings;
            bool worldConfigFound = MyAPIGateway.Utilities.FileExistsInWorldStorage(config.ConfigFileName, typeof(WeaponSession));
            if (worldConfigFound)
            {
                Logger.Default.WriteLine($"Found {config.ConfigFileName} in world storage. Loading config...");

                using (var Reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(config.ConfigFileName, typeof(WeaponSession)))
                {
                    settings = Reader.ReadToEnd();
                }
            }
            else
            {
                Logger.Default.WriteLine($"{config.ConfigFileName} not found in world storage. Looking in local storage instead...");
                if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(config.ConfigFileName, typeof(WeaponSession)))
                {
                    //MyAPIGateway.Utilities.ShowMessage(WeaponConstants.DEBUG_MSG_TAG, $"{config.ConfigFileName} not found. Writing defaults...");
                    Logger.Default.WriteLine($"{config.ConfigFileName} not found in local storage. Writing defaults...");
                    SaveConfig(config);
                    return;
                }

                using (var Reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(config.ConfigFileName, typeof(WeaponSession)))
                {
                    settings = Reader.ReadToEnd();
                }
            }

            myIni.Clear();
            bool parsed = myIni.TryParse(settings);

            if (!parsed)
            {
                Logger.Default.WriteLine("Config could not be parsed. Writing defaults...");
                SaveConfig(config, worldConfigFound: worldConfigFound);
                return;
            }

            // Check config version key
            string sectionTag = config.ConfigID;
            string storageVersionKey = myIni.Get(sectionTag, MyIniConstants.INI_KEY_CONFIG_VERSION_KEY).ToString(MyIniConstants.INI_VALUE_DEFAULT_VERSION_KEY);
            string modVersionKey = config.ConfigVersionKey?? MyIniConstants.INI_VALUE_DEFAULT_VERSION_KEY;

            if (modVersionKey != storageVersionKey)
            {
                Logger.Default.WriteLine($"Config version key '{storageVersionKey}' did not match mod version key '{modVersionKey}'...");
                Logger.Default.WriteLine($"Overwriting config with mod defaults...");
                SaveConfig(config, worldConfigFound: worldConfigFound);
                return;
            }

            config.TracerColor = MyIniHelper.GetVector3(sectionTag, MyIniConstants.INI_KEY_TRACER_COLOR, myIni, config.TracerColor);
            config.TracerScale =                 myIni.Get(sectionTag, MyIniConstants.INI_KEY_TRACER_SCALE).ToSingle(config.TracerScale);
            config.ArtificialGravityMultiplier = myIni.Get(sectionTag, MyIniConstants.INI_KEY_ART_GRAV).ToSingle(config.ArtificialGravityMultiplier);
            config.NaturalGravityMultiplier =    myIni.Get(sectionTag, MyIniConstants.INI_KEY_NAT_GRAV).ToSingle(config.NaturalGravityMultiplier);
            config.ShouldDrawProjectileTrails =  myIni.Get(sectionTag, MyIniConstants.INI_KEY_DRAW_TRAILS).ToBoolean(config.ShouldDrawProjectileTrails);
            config.ProjectileTrailFadeRatio =    myIni.Get(sectionTag, MyIniConstants.INI_KEY_TRAIL_DECAY).ToSingle(config.ProjectileTrailFadeRatio);
            config.PenetrationDamage =           myIni.Get(sectionTag, MyIniConstants.INI_KEY_PEN_DMG).ToSingle(config.PenetrationDamage);
            config.ContactExplosionRadius =      myIni.Get(sectionTag, MyIniConstants.INI_KEY_EXP_RAD).ToSingle(config.ContactExplosionRadius);
            config.ContactExplosionDamage =      myIni.Get(sectionTag, MyIniConstants.INI_KEY_EXP_DMG).ToSingle(config.ContactExplosionDamage);
            config.ExplodeOnContact =            myIni.Get(sectionTag, MyIniConstants.INI_KEY_SHOULD_EXP).ToBoolean(config.ExplodeOnContact);
            config.PenetrateOnContact =          myIni.Get(sectionTag, MyIniConstants.INI_KEY_PEN).ToBoolean(config.PenetrateOnContact);
            config.PenetrationRange =            myIni.Get(sectionTag, MyIniConstants.INI_KEY_PEN_RANGE).ToSingle(config.PenetrationRange);
            config.ExplodePostPenetration =      myIni.Get(sectionTag, MyIniConstants.INI_KEY_SHOULD_EXP_PEN).ToBoolean(config.ExplodePostPenetration);
            config.PenetrationExplosionRadius =  myIni.Get(sectionTag, MyIniConstants.INI_KEY_EXP_PEN_RAD).ToSingle(config.PenetrationExplosionRadius);
            config.PenetrationExplosionDamage =  myIni.Get(sectionTag, MyIniConstants.INI_KEY_EXP_PEN_DMG).ToSingle(config.PenetrationExplosionDamage);

            var turretConfig = config as TurretWeaponConfig;
            if (turretConfig != null)
            {
                turretConfig.IdlePowerDrawBase = myIni.Get(sectionTag, MyIniConstants.INI_KEY_TURRET_PWR_MIN_RANGE).ToSingle(turretConfig.IdlePowerDrawBase);
                turretConfig.IdlePowerDrawMax  = myIni.Get(sectionTag, MyIniConstants.INI_KEY_TURRET_PWR_MAX_RANGE).ToSingle(turretConfig.IdlePowerDrawMax);
            }
            else
            {
                config.IdlePowerDrawBase = myIni.Get(sectionTag, MyIniConstants.INI_KEY_PWR_IDLE).ToSingle(config.IdlePowerDrawBase);
            }

            config.ReloadPowerDraw =        myIni.Get(sectionTag, MyIniConstants.INI_KEY_PWR_RELOAD).ToSingle(config.ReloadPowerDraw);
            config.MuzzleVelocity =         myIni.Get(sectionTag, MyIniConstants.INI_KEY_MUZZLE_VEL).ToSingle(config.MuzzleVelocity);
            config.MaxRange =               myIni.Get(sectionTag, MyIniConstants.INI_KEY_MAX_RANGE).ToSingle(config.MaxRange);
            config.DeviationAngleDeg =      myIni.Get(sectionTag, MyIniConstants.INI_KEY_DEVIANCE).ToSingle(config.DeviationAngleDeg);
            config.RecoilImpulse =          myIni.Get(sectionTag, MyIniConstants.INI_KEY_RECOIL).ToSingle(config.RecoilImpulse);
            config.HitImpulse =             myIni.Get(sectionTag, MyIniConstants.INI_KEY_IMPULSE).ToSingle(config.HitImpulse);
            config.ShieldDamageMultiplier = myIni.Get(sectionTag, MyIniConstants.INI_KEY_SHIELD_MULT).ToSingle(config.ShieldDamageMultiplier);
            config.RateOfFireRPM =          myIni.Get(sectionTag, MyIniConstants.INI_KEY_ROF).ToSingle(config.RateOfFireRPM);
            config.ShouldProximityDetonate = myIni.Get(sectionTag, MyIniConstants.INI_KEY_PROXIMITY_DET).ToBoolean(config.ShouldProximityDetonate);
            config.ProximityDetonationRange = myIni.Get(sectionTag, MyIniConstants.INI_KEY_PROXIMITY_DET_RANGE).ToSingle(config.ProximityDetonationRange);
            config.ProximityDetonationArmingRange = myIni.Get(sectionTag, MyIniConstants.INI_KEY_PROXIMITY_DET_ARM_RANGE).ToSingle(config.ProximityDetonationArmingRange);

            Logger.Default.WriteLine("Config loaded from file!");

            // For debugging
            //string output = MyAPIGateway.Utilities.SerializeToXML(config);
            //Logger.Default.WriteLine($"{WeaponConstants.DEBUG_MSG_TAG}: Config xml\n{output}");

            SaveConfig(config, false, settings, worldConfigFound);

            if (config.MaxRange > MyAPIGateway.Session.SessionSettings.SyncDistance)
            { 
                //MyAPIGateway.Utilities.ShowMessage(FrameworkConstants.DEBUG_MSG_TAG,
                //        $"Sync distance is too low. Increasing from {MyAPIGateway.Session.SessionSettings.SyncDistance} to {config.MaxRange}");
                Logger.Default.WriteLine($"Sync distance is too low. Increasing from {MyAPIGateway.Session.SessionSettings.SyncDistance} to {config.MaxRange}", Logger.Severity.Warning);
                MyAPIGateway.Session.SessionSettings.SyncDistance = (int)config.MaxRange;
            }

            if (config.MaxRange > MyAPIGateway.Session.SessionSettings.ViewDistance)
            {
                //MyAPIGateway.Utilities.ShowMessage(FrameworkConstants.DEBUG_MSG_TAG,
                //        $"View distance is too low. Increasing from {MyAPIGateway.Session.SessionSettings.ViewDistance} to {config.MaxRange}");
                Logger.Default.WriteLine($"View distance is too low. Increasing from {MyAPIGateway.Session.SessionSettings.SyncDistance} to {config.MaxRange}", Logger.Severity.Warning);
                MyAPIGateway.Session.SessionSettings.ViewDistance = (int)config.MaxRange;
            }

        }

        public void SaveConfig(WeaponConfig config, bool verbose = true, string settings = "", bool worldConfigFound = false)
        {
            myIni.Clear();
            string sectionTag = config.ConfigID;
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_CONFIG_VERSION_KEY, config.ConfigVersionKey?? MyIniConstants.INI_VALUE_DEFAULT_VERSION_KEY);
            myIni.SetComment(sectionTag, MyIniConstants.INI_KEY_CONFIG_VERSION_KEY, " Do not manually edit the config version key");
            MyIniHelper.SetVector3(sectionTag, MyIniConstants.INI_KEY_TRACER_COLOR, ref config.TracerColor, myIni);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_TRACER_SCALE, config.TracerScale);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_ART_GRAV, config.ArtificialGravityMultiplier);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_NAT_GRAV, config.NaturalGravityMultiplier);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_DRAW_TRAILS, config.ShouldDrawProjectileTrails);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_TRAIL_DECAY, config.ProjectileTrailFadeRatio);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_SHOULD_EXP, config.ExplodeOnContact);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_EXP_RAD, config.ContactExplosionRadius);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_EXP_DMG, config.ContactExplosionDamage);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_PEN, config.PenetrateOnContact);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_PEN_DMG, config.PenetrationDamage);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_PEN_RANGE, config.PenetrationRange);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_SHOULD_EXP_PEN, config.ExplodePostPenetration);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_EXP_PEN_RAD, config.PenetrationExplosionRadius);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_EXP_PEN_DMG, config.PenetrationExplosionDamage);

            var turretConfig = config as TurretWeaponConfig;
            if (turretConfig != null)
            {
                myIni.Set(sectionTag, MyIniConstants.INI_KEY_TURRET_PWR_MIN_RANGE, turretConfig.IdlePowerDrawBase);
                myIni.Set(sectionTag, MyIniConstants.INI_KEY_TURRET_PWR_MAX_RANGE, turretConfig.IdlePowerDrawMax);
            }
            else
            {
                myIni.Set(sectionTag, MyIniConstants.INI_KEY_PWR_IDLE, config.IdlePowerDrawBase);
            }

            myIni.Set(sectionTag, MyIniConstants.INI_KEY_PWR_RELOAD, config.ReloadPowerDraw);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_MUZZLE_VEL, config.MuzzleVelocity);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_MAX_RANGE, config.MaxRange);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_DEVIANCE, config.DeviationAngleDeg);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_RECOIL, config.RecoilImpulse);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_IMPULSE, config.HitImpulse);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_SHIELD_MULT, config.ShieldDamageMultiplier);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_ROF, config.RateOfFireRPM);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_PROXIMITY_DET, config.ShouldProximityDetonate);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_PROXIMITY_DET_RANGE, config.ProximityDetonationRange);
            myIni.Set(sectionTag, MyIniConstants.INI_KEY_PROXIMITY_DET_ARM_RANGE, config.ProximityDetonationArmingRange);


            string finalOutput = myIni.ToString();

            if (finalOutput != settings)
            {
                if (worldConfigFound)
                {
                    using (var Writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(config.ConfigFileName, typeof(WeaponSession)))
                        Writer.Write(finalOutput);
                }
                else
                {
                    using (var Writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(config.ConfigFileName, typeof(WeaponSession)))
                        Writer.Write(finalOutput);
                }

                Logger.Default.WriteLine("Config updated and saved!");
            }
        }
        #endregion
    }
}
