using System;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using Sandbox.Game.Weapons;
using VRage.Game.ModAPI;
using VRageMath;
using VRage.Game.Entity;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI.Interfaces;
using Sandbox.Definitions;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using Rexxar;
using Whiplash.Utils;
using System.Collections.Generic;

namespace Whiplash.WeaponFramework
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeGatlingTurret), false)]
    public class WeaponBlockGatlingTurretBase : WeaponBlockTurretBase
    {

    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), false)]
    public class WeaponBlockMissileTurretBase : WeaponBlockTurretBase
    {

    }

    abstract public class WeaponBlockTurretBase : WeaponBlockBase
    {
        protected static bool _terminalControlsInit = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            IsTurret = true;
            base.Init(objectBuilder);
        }

        public override void CreateTerminalControlsInvoke()
        {
            try
            {
                CreateCustomTerminalControls<IMyLargeGatlingTurret>(ref _terminalControlsInit, _cube.BlockDefinition.SubtypeId);
            }
            catch (Exception e)
            {

                MyAPIGateway.Utilities.ShowNotification("Exception in weapon turret terminal control init", 10000, MyFontEnum.Red);
                Logger.Default.WriteLine(e.ToString(), Logger.Severity.Error);
            }
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallGatlingGun), false)]
    public class WeaponBlockGatlingFixed : WeaponBlockFixedBase
    {

    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncher), false)]
    public class WeaponBlockMissileFixed : WeaponBlockFixedBase
    {

    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncherReload), false)]
    public class WeaponBlockMissileFixedReloadable : WeaponBlockFixedBase
    {

    }

    abstract public class WeaponBlockFixedBase : WeaponBlockBase
    {
        static bool _terminalControlsInit = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            IsTurret = false;
            base.Init(objectBuilder);
        }

        public override void CreateTerminalControlsInvoke()
        {
            try
            {
                CreateCustomTerminalControls<IMySmallMissileLauncher>(ref _terminalControlsInit, _cube.BlockDefinition.SubtypeId);
            }
            catch (Exception e)
            {

                MyAPIGateway.Utilities.ShowNotification("Exception in fixed railgun terminal control init", 10000, MyFontEnum.Red);
                Logger.Default.WriteLine(e.ToString(), Logger.Severity.Error);
            }
        }
    }

    public struct WeaponFireData
    {
        public Vector3D Origin;
        public Vector3D Direction;
        public Vector3D ShooterVelocity;
        public long ShooterID;
    }

    public abstract class WeaponBlockBase : MyGameLogicComponent
    {
        #region Member Fields
        public bool IsTurret { get; protected set; }
        public bool SettingsDirty;
        public bool ShouldShoot
        {
            get
            {
                return IsWorking
                    && !_isReloading
                    && (_userControllableGun.IsShooting || (_shootProperty != null && _shootProperty.GetValue(_userControllableGun)));
            }
        }

        public bool IsWorking
        {
            get
            {
                return _userControllableGun != null
                    && _userControllableGun.IsFunctional
                    && _userControllableGun.Enabled
                    && _sink != null
                    && _sink.IsPoweredByType(_electricityResId);
            }
        }

        protected IMyCubeBlock _cube;
        IMyUserControllableGun _userControllableGun;
        IMyGunObject<MyGunBase> _gun;
        IMyFunctionalBlock _block;
        IMyLargeTurretBase _turret;
        ITerminalProperty<bool> _shootProperty;
        float _turretMaxRange;
        float _shootInterval;
        float _reloadTicks;
        float _currentReloadTicks = 0;
        long _configTick = 0;
        bool _isReloading = false;
        bool _firstUpdate = true;
        bool _init = false;
        float _idlePowerDrawBase;
        float _idlePowerDrawMax = 1;
        float _reloadPowerDraw;
        private static readonly MyDefinitionId _electricityResId = MyResourceDistributorComponent.ElectricityId;
        MyResourceSinkComponent _sink;
        MySoundPair _shootSound;
        MyEntity3DSoundEmitter soundEmitter;
        MyDefinitionId _definitionId;
        MyParticleEffect _muzzleFlash;
        protected WeaponConfig _config;
        protected TurretWeaponConfig _turretConfig = null;
        double _secondsSinceMuzzleFlashSpawned = 0;
        bool _muzzleFlashSpawned = false;

        #endregion

        #region Terminal Action/Property Methods
        public abstract void CreateTerminalControlsInvoke();

        public void CreateCustomTerminalControls<T>(ref bool controlsInit, string subtypeName) where T : class, IMyTerminalBlock
        {
            if (controlsInit)
                return;

            controlsInit = true;

            IMyTerminalControlOnOffSwitch rechargeControl = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, T>("RechargeRailgun");
            rechargeControl.Title = MyStringId.GetOrCompute("Recharge Railgun");
            rechargeControl.Enabled = x => x.BlockDefinition.SubtypeId.Equals(subtypeName);
            rechargeControl.Visible = x => x.BlockDefinition.SubtypeId.Equals(subtypeName);
            rechargeControl.SupportsMultipleBlocks = true;
            rechargeControl.OnText = MyStringId.GetOrCompute("On");
            rechargeControl.OffText = MyStringId.GetOrCompute("Off");
            rechargeControl.Setter = (x, v) => SetRecharging(x, v);
            rechargeControl.Getter = x => GetRecharging(x);
            MyAPIGateway.TerminalControls.AddControl<T>(rechargeControl);

            //Recharge toggle action
            IMyTerminalAction rechargeOnOff = MyAPIGateway.TerminalControls.CreateAction<T>("Recharge_OnOff");
            rechargeOnOff.Action = (x) =>
            {
                var recharge = GetRecharging(x);
                SetRecharging(x, !recharge);
            };
            rechargeOnOff.ValidForGroups = true;
            rechargeOnOff.Writer = (x, s) => GetWriter(x, s);
            rechargeOnOff.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            rechargeOnOff.Enabled = x => x.BlockDefinition.SubtypeId.Equals(subtypeName);
            rechargeOnOff.Name = new StringBuilder("Recharge On/Off");
            MyAPIGateway.TerminalControls.AddAction<T>(rechargeOnOff);

            //Recharge on action
            IMyTerminalAction rechargeOn = MyAPIGateway.TerminalControls.CreateAction<T>("Recharge_On");
            rechargeOn.Action = (x) => SetRecharging(x, true);
            rechargeOn.ValidForGroups = true;
            rechargeOn.Writer = (x, s) => GetWriter(x, s);
            rechargeOn.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            rechargeOn.Enabled = x => x.BlockDefinition.SubtypeId.Equals(subtypeName);
            rechargeOn.Name = new StringBuilder("Recharge On");
            MyAPIGateway.TerminalControls.AddAction<T>(rechargeOn);

            //Recharge off action
            IMyTerminalAction rechargeOff = MyAPIGateway.TerminalControls.CreateAction<T>("Recharge_Off");
            rechargeOff.Action = (x) => SetRecharging(x, false);
            rechargeOff.ValidForGroups = true;
            rechargeOff.Writer = (x, s) => GetWriter(x, s);
            rechargeOff.Icon = @"Textures\GUI\Icons\Actions\SwitchOff.dds";
            rechargeOff.Enabled = x => x.BlockDefinition.SubtypeId.Equals(subtypeName);
            rechargeOff.Name = new StringBuilder("Recharge Off");
            MyAPIGateway.TerminalControls.AddAction<T>(rechargeOff);
        }

        public void GetWriter(IMyTerminalBlock x, StringBuilder s)
        {
            s.Clear();
            var y = x.GameLogic.GetAs<WeaponBlockBase>();
            var set = Settings.GetSettings(x);

            if (y != null)
            {
                if (set.Recharging)
                    s.Append("On");
                else
                    s.Append("Off");
            }
        }

        public void SetRecharging(IMyTerminalBlock b, bool v)
        {
            var s = Settings.GetSettings(b);
            s.Recharging = v;
            Settings.SetSettings(b, s);
            SetDirty(b);
        }

        public bool GetRecharging(IMyTerminalBlock b)
        {
            return Settings.GetSettings(b).Recharging;
        }

        public void SetDirty(IMyTerminalBlock b)
        {
            var g = b.GameLogic.GetAs<WeaponBlockBase>();
            if (g != null)
                g.SettingsDirty = true;
        }
        #endregion

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            try
            {
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                // this.m_missileAmmoDefinition = weaponProperties.GetCurrentAmmoDefinitionAs<MyMissileAmmoDefinition>();
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Exception in init", 10000, MyFontEnum.Red);
                Logger.Default.WriteLine(e.ToString(), Logger.Severity.Error);
            }
        }

        public void GetBulletOriginAndDirection(ref Vector3D origin, ref Vector3D direction)
        {
            MatrixD muzzleMatrix = _gun.GunBase.GetMuzzleWorldMatrix();
            direction = muzzleMatrix.Forward;
            Vector3D offset = direction * _config.BulletSpawnForwardOffsetMeters;
            origin = muzzleMatrix.Translation + offset;
        }

        bool TryGetWeaponConfig()
        {
            bool hasConfig;
            if (!IsTurret)
            {
                hasConfig = FrameworkWeaponAPI.FixedGunWeaponConfigs.TryGetValue(_block.BlockDefinition.SubtypeName, out _config);
                _turretConfig = null;
            }
            else
            {
                hasConfig = FrameworkWeaponAPI.TurretWeaponConfigs.TryGetValue(_block.BlockDefinition.SubtypeName, out _turretConfig);
                if (hasConfig)
                    _config = (WeaponConfig)_turretConfig;
            }
            _configTick = WeaponSession.CurrentTick;
            return hasConfig;
        }

        enum InitStage { Start, GetConfig, CreateTerminalControls, GetShootProperty, SetAmmoProps, SetPowerDraw, End }
        void Initialize()
        {
            if (_init)
                return;

            InitStage initStage = InitStage.Start;
            try
            {
                _cube = (IMyCubeBlock)Entity;
                _block = (IMyFunctionalBlock)Entity;
                _userControllableGun = (IMyUserControllableGun)Entity;
                _gun = Entity as IMyGunObject<MyGunBase>;

                initStage = InitStage.GetConfig;
                if (!TryGetWeaponConfig())
                {
                    NeedsUpdate = MyEntityUpdateEnum.NONE; // Disable updating
                    return;
                }
                Logger.Default.WriteLine($"Config found for type: {_block.BlockDefinition.TypeIdStringAttribute}/{_block.BlockDefinition.SubtypeName}", writeToGameLog: false);

                initStage = InitStage.CreateTerminalControls;
                CreateTerminalControlsInvoke();
                //MyAPIGateway.Utilities.ShowNotification($"Config found for type: {block.BlockDefinition.SubtypeName}", font: "Green");

                initStage = InitStage.GetShootProperty;
                GetShootProperty();

                initStage = InitStage.SetAmmoProps;
                SetAmmoProperties();

                initStage = InitStage.SetPowerDraw;
                SetPowerDraw();

                GetTurretMaxRange();
                SetPowerSink();

                GetTurretPowerDrawConstants(_idlePowerDrawBase, _idlePowerDrawMax, _turretMaxRange); // Update power draw constants

                initStage = InitStage.End;
                ComputeRateOfFireParameters();
                SetShootSound();
                _init = true;
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification($"RAILGUN FRAMEWORK: Weapon '{_block.BlockDefinition.TypeIdString}/{_block.BlockDefinition.SubtypeName}' failed to init", 10000, MyFontEnum.Red);
                Logger.Default.WriteLine($"Weapon '{_block.BlockDefinition.TypeIdString}/{_block.BlockDefinition.SubtypeName}' failed to init at stage {initStage}{Environment.NewLine}{e}", Logger.Severity.Error);
            }
        }

        void GetShootProperty()
        {
            if (_shootProperty == null)
            {
                ITerminalProperty genericShoot = null;
                _block.GetProperties(null, (p) => {
                    if (p.Id == "Shoot")
                    {
                        genericShoot = p;
                    }
                    return false;
                });

                if (genericShoot != null)
                {
                    _shootProperty = genericShoot.Cast<bool>();
                }
                else
                {
                    MyAPIGateway.Utilities.ShowNotification($"Shoot property not found for '{_block.BlockDefinition.TypeIdString}/{_block.BlockDefinition.SubtypeName}'", 960, MyFontEnum.Red);
                    Logger.Default.WriteLine($"Shoot property not found for '{_block.BlockDefinition.TypeIdString}/{_block.BlockDefinition.SubtypeName}'", Logger.Severity.Warning);
                }
            }
        }

        void SetShootSound()
        {
            _shootSound = new MySoundPair(_config.FireSoundName);
        }

        void SetPowerDraw()
        {
            _idlePowerDrawBase = _config.IdlePowerDrawBase; //MW
            if (_turretConfig != null)
            {
                _idlePowerDrawMax = _turretConfig.IdlePowerDrawMax;
            }
            else
            {
                _idlePowerDrawMax = _config.IdlePowerDrawBase;
            }
            _reloadPowerDraw = _config.ReloadPowerDraw; //MW
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            InitStage stage = InitStage.CreateTerminalControls;
            try
            {
                // Dear Keen, I hate you.
                Initialize();
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification($"RAILGUN FRAMEWORK: Weapon '{_block.BlockDefinition.TypeIdString}/{_block.BlockDefinition.SubtypeName}' failed at update once", 10000, MyFontEnum.Red);
                Logger.Default.WriteLine($"Weapon '{_block.BlockDefinition.TypeIdString}/{_block.BlockDefinition.SubtypeName}' failed at update once in stage {stage}{Environment.NewLine}{e}", Logger.Severity.Error);
            }
        }

        public override void UpdateBeforeSimulation()
        {
            if (!_init)
            {
                base.UpdateBeforeSimulation();
                return;
            }

            if (_configTick < WeaponSession.ConfigRefreshTick)
            {
                if (TryGetWeaponConfig())
                {
                    SetAmmoProperties();
                    SetPowerDraw();
                    GetTurretMaxRange();
                    SetPowerSink();
                    GetTurretPowerDrawConstants(_idlePowerDrawBase, _idlePowerDrawMax, _turretMaxRange);
                    ComputeRateOfFireParameters();
                    SetShootSound();
                }

                Logger.Default.WriteLine($"Refreshed config for entityId {Entity.EntityId} (subtype: {_block.BlockDefinition.TypeIdString}/{_block.BlockDefinition.SubtypeName})", writeToGameLog: false);
            }

            if (_init && MyAPIGateway.Multiplayer.IsServer && _gun.GunBase.HasEnoughAmmunition())
                _gun.GunBase.CurrentAmmo = 1;

            base.UpdateBeforeSimulation();

            try
            {
                //MyAPIGateway.Utilities.ShowNotification($"phys:{_cube?.CubeGrid?.Physics != null} | enab:{_userControllableGun.Enabled} | func:{_userControllableGun.IsFunctional} | work:{_userControllableGun.IsWorking} | powered:{_sink.IsPoweredByType(_electricityResId)} | shoot:{_userControllableGun.IsShooting} | tog: {TerminalPropertyExtensions.GetValue<bool>(_userControllableGun, "Shoot")}", 16);

                if (_cube?.CubeGrid?.Physics == null) //ignore ghost grids
                    return;

                if (ShouldShoot && !_isReloading && !_firstUpdate) //Shoot
                {

                    _isReloading = true;
                    soundEmitter = new MyEntity3DSoundEmitter((MyEntity)Entity, true);
                    soundEmitter.CustomVolume = _config.FireSoundVolumeMultiplier;
                    soundEmitter.PlaySingleSound(_shootSound, true);

                    //MyAPIGateway.Utilities.ShowNotification($"curr ammo:{gun.GunBase.CurrentAmmo}");

                    Vector3D direction = Vector3D.Zero;
                    Vector3D origin = Vector3D.Zero;
                    GetBulletOriginAndDirection(ref origin, ref direction);

                    // Fire weapon
                    if (MyAPIGateway.Multiplayer.IsServer)
                    {
                        _gun.GunBase.CurrentAmmo = 0;
                        _gun.GunBase.ConsumeAmmo();

                        var velocity = _block.CubeGrid.Physics.LinearVelocity;

                        var fireData = new WeaponFireData()
                        {
                            ShooterVelocity = velocity,
                            Origin = origin,
                            Direction = direction,
                            ShooterID = Entity.EntityId,
                        };

                        WeaponSession.ShootProjectile(fireData, _config);

                        _currentReloadTicks = 0;

                        //Apply recoil force
                        var centerOfMass = _block.CubeGrid.Physics.CenterOfMassWorld;
                        var forceVector = -direction * _config.RecoilImpulse;

                        _block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, (Vector3)forceVector, _block.GetPosition(), null);
                    }

                    if (!MyAPIGateway.Utilities.IsDedicated && _config.DrawMuzzleFlash)
                    {
                        MatrixD matrix = MatrixD.CreateFromDir(-direction); //Negative because muzzle flashes are fucking backwards
                        matrix.Translation = origin;
                        bool foundParticle = MyParticlesManager.TryCreateParticleEffect(_config.MuzzleFlashSpriteName, ref matrix, ref origin, uint.MaxValue, out _muzzleFlash);
                        if (foundParticle)
                        {
                            _muzzleFlash.UserScale = _config.MuzzleFlashScale;
                            _muzzleFlash.Play();
                            _muzzleFlashSpawned = true;
                            _secondsSinceMuzzleFlashSpawned = 0;
                        }
                    }
                }

                if (!MyAPIGateway.Utilities.IsDedicated && _muzzleFlashSpawned)
                {
                    if (_secondsSinceMuzzleFlashSpawned >= _config.MuzzleFlashDuration)
                    {
                        if (_muzzleFlash != null)
                            _muzzleFlash.Stop();
                        _muzzleFlashSpawned = false;
                    }
                    else
                        _secondsSinceMuzzleFlashSpawned += (1.0 / 60.0);
                }

                _firstUpdate = false;
                ShowReloadMessage();
                _sink.Update();
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification($"Exception in update for '{_block.BlockDefinition.TypeIdString}/{_block.BlockDefinition.SubtypeName}'", 960, MyFontEnum.Red);
                Logger.Default.WriteLine($"Exception in update for '{_block.BlockDefinition.TypeIdString}/{_block.BlockDefinition.SubtypeName}'{Environment.NewLine}{e}", Logger.Severity.Error);
            }
        }

        void SetAmmoProperties()
        {
            //-----------------------------------------------------------------
            //Thanks digi <3
            var slim = _block.SlimBlock; //.CubeGrid.GetCubeBlock(block.Position);
            var definition = slim.BlockDefinition;
            var weapon = (MyWeaponBlockDefinition)definition;
            _definitionId = weapon.WeaponDefinitionId;
            var wepDef = MyDefinitionManager.Static.GetWeaponDefinition(_definitionId);

            for (int i = 0; i < wepDef.AmmoMagazinesId.Length; i++)
            {
                var mag = MyDefinitionManager.Static.GetAmmoMagazineDefinition(wepDef.AmmoMagazinesId[i]);
                var ammo = MyDefinitionManager.Static.GetAmmoDefinition(mag.AmmoDefinitionId);

                ammo.MaxTrajectory = _config.MaxRange;
                ammo.DesiredSpeed = _config.MuzzleVelocity;

                var projectileAmmo = ammo as MyProjectileAmmoDefinition;
                projectileAmmo.ProjectileTrailProbability = 0f; //disable default tracers
                projectileAmmo.ProjectileMassDamage = 0f;
                projectileAmmo.ProjectileHealthDamage = 0f;
                projectileAmmo.BackkickForce = 0f;
                projectileAmmo.ProjectileHitImpulse = 0f;
                projectileAmmo.ProjectileCount = 0; // Makes sure no default KEEN projectile is spawned!
            }

            for (int i = 0; i < wepDef.WeaponAmmoDatas.Length; ++i)
            {
                var ammoData = wepDef.WeaponAmmoDatas[i];

                if (ammoData == null)
                    continue;

                ammoData.ShotsInBurst = -1;
                ammoData.ShootIntervalInMiliseconds = 17;
                ammoData.ShootSound = new MySoundPair("");
            }

            //-------------------------------------------
        }

        void ComputeRateOfFireParameters()
        {
            //Compute reload ticks
            _shootInterval = 60f / _config.RateOfFireRPM;  // gun.GunBase.ShootIntervalInMiliseconds; //wepDef.ReloadTime;
            _reloadTicks = (_shootInterval * 60f); // + 1;
        }

        void GetTurretMaxRange()
        {
            //init turret power draw function constants
            if (Entity is IMyLargeTurretBase)
            {
                _turret = Entity as IMyLargeTurretBase;
                var def = _cube.SlimBlock.BlockDefinition as MyLargeTurretBaseDefinition;
                _turretMaxRange = def.MaxRangeMeters;
                var ob = (MyObjectBuilder_TurretBase)_cube.GetObjectBuilderCubeBlock();
                ob.Range = _turretMaxRange;
                GetTurretPowerDrawConstants(_idlePowerDrawBase, _idlePowerDrawMax, _turretMaxRange);
            }
        }

        void SetPowerSink()
        {
            _sink = Entity.Components.Get<MyResourceSinkComponent>();

            MyResourceSinkInfo resourceInfo = new MyResourceSinkInfo()
            {
                ResourceTypeId = _electricityResId,
                MaxRequiredInput = IsTurret ? _idlePowerDrawMax : _idlePowerDrawBase,
                RequiredInputFunc = () => GetPowerInput()
            };

            _sink.RemoveType(ref resourceInfo.ResourceTypeId);
            _sink.Init(MyStringHash.GetOrCompute("Thrust"), resourceInfo);
            _sink.AddType(ref resourceInfo);   
        }

        float GetPowerInput()
        {
            var s = Settings.GetSettings(Entity);

            if (!_block.Enabled && (!s.Recharging || !_isReloading))
                return 0f;

            if (!_block.IsFunctional)
                return 0f;

            var requiredInput = IsTurret ? CalculateTurretPowerDraw(_turret.Range) : _idlePowerDrawBase;
            if (!_isReloading || (MyAPIGateway.Session.SurvivalMode && _isReloading && !_gun.GunBase.HasEnoughAmmunition()))
            {
                _sink.SetMaxRequiredInputByType(_electricityResId, requiredInput);
                return requiredInput;
            }

            if (!s.Recharging)
            {
                _sink.SetMaxRequiredInputByType(_electricityResId, requiredInput);
                return requiredInput;
            }

            var suppliedRatio = _sink.SuppliedRatioByType(_electricityResId);
            if (suppliedRatio == 1)
                _currentReloadTicks += 1;
            else
                _currentReloadTicks += 1 * suppliedRatio * 0.5f; // nerfed recharge rate if overloaded
            
            if (_currentReloadTicks >= _reloadTicks)
            {
                _isReloading = false;
                _currentReloadTicks = 0;
                _sink.SetMaxRequiredInputByType(_electricityResId, requiredInput);
                return requiredInput;
            }

            var scaledReloadPowerDraw = _reloadPowerDraw;
            requiredInput = Math.Max(requiredInput, scaledReloadPowerDraw);
            _sink.SetMaxRequiredInputByType(_electricityResId, requiredInput);
            return requiredInput;
        }

        void ShowReloadMessage()
        {
            var s = Settings.GetSettings(Entity);

            if (_isReloading && s.Recharging)
            {
                if (MyAPIGateway.Utilities.IsDedicated)
                    return;

                IMyTerminalBlock controlledBlock = MyAPIGateway.Session.Player?.Controller?.ControlledEntity?.Entity as IMyTerminalBlock;
                bool sameShip = controlledBlock != null && controlledBlock.IsSameConstructAs(this._block);

                if (_config.ShowReloadMessage)
                {
                    if (!sameShip // Not same grid
                            || !_isReloading // Not reloading
                            || (_isReloading && MyAPIGateway.Session.SurvivalMode && !_gun.GunBase.HasEnoughAmmunition())) // IS reloading with no ammo in survival
                        return;

                    MyAPIGateway.Utilities.ShowNotification($"{_config.ReloadMessage} ({100 * _currentReloadTicks / _reloadTicks:n0}%)", 16);
                }
            }
        }

        float _m = 0;
        float _b = 0;
        void GetTurretPowerDrawConstants(float start, float end, float maxRange)
        {
            _b = start;
            if (maxRange == 0)
                _m = 0; // Avoids divide by zero
            else
                _m = (end - start) / (maxRange * maxRange * maxRange);
        }

        float CalculateTurretPowerDraw(float currentRange)
        {
            return _m * currentRange * currentRange * currentRange + _b;
        }
    }
}
