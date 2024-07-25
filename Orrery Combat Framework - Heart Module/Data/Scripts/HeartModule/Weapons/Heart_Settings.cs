using Heart_Module.Data.Scripts.HeartModule;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Network;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using ProtoBuf;
using Sandbox.ModAPI;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class Heart_Settings : PacketBase // this will ABSOLUTELY bite me in the ass later.
    {
        public void Sync(Vector3D turretPosition)
        {
            HeartLog.Log("Sync called!");
            if (MyAPIGateway.Session.IsServer)
            {
                HeartData.I.Net.SendToEveryoneInSync(this, turretPosition);
                HeartLog.Log("Sent settings to all.\n" + ToString() + "\n---------------------------");
            }
            else
            {
                HeartData.I.Net.SendToServer(this);
                HeartLog.Log("Sent settings to server");
            }
        }

        public static void RequestSync(long weaponEntityId)
        {
            if (MyAPIGateway.Session.IsServer)
                return;
            HeartLog.Log($"RequestSync: Requesting sync for weapon {weaponEntityId}");
            HeartData.I.Net.SendToServer(new Heart_Settings()
            {
                IsSyncRequest = true,
                WeaponEntityId = weaponEntityId
            });
        }

        public override void Received(ulong SenderSteamId)
        {
            HeartLog.Log($"Heart_Settings Received: Sender: {SenderSteamId} | Self: {HeartData.I.SteamId}\n{ToString()}");
            var weapon = WeaponManager.I.GetWeapon(WeaponEntityId);
            if (weapon == null)
            {
                HeartLog.Log($"Weapon doesn't exist! ThisId: {WeaponEntityId}");
                return;
            }

            if (IsSyncRequest)
            {
                HeartLog.Log($"Processing sync request for weapon {WeaponEntityId}");
                weapon.Settings.Sync(weapon.SorterWep.GetPosition());
                return;
            }

            HeartLog.Log($"Updating settings for weapon {WeaponEntityId}");
            weapon.Settings = this;
            weapon.Magazines.SelectedAmmoIndex = AmmoLoadedIdx;
            HeartLog.Log($"UPDATED Id: {weapon.Magazines.SelectedAmmoId} | Idx: {weapon.Magazines.SelectedAmmoIndex}");
            HeartLog.Log($"SHOULD BE Idx: {AmmoLoadedIdx}");

            if (MyAPIGateway.Session.IsServer)
            {
                HeartLog.Log($"Server is syncing settings for weapon {WeaponEntityId}");
                weapon.Settings.Sync(weapon.SorterWep.GetPosition());
            }
        }

        [ProtoMember(1)]
        internal short ShootStateContainer;

        [ProtoMember(2)]
        public int AmmoLoadedIdx;

        [ProtoMember(3)]
        public float AiRange;

        [ProtoMember(4)]
        internal int TargetStateContainer;

        [ProtoMember(5)]
        public long ControlTypeState;

        [ProtoMember(6)]
        public long WeaponEntityId;

        [ProtoMember(7)]
        internal short ResetTargetStateContainer;

        #region ShootStates

        public bool ShootState
        {
            get
            {
                return ExpandValue(ShootStateContainer, ShootStates.Shoot);
            }
            set
            {
                CompressValue(ref ShootStateContainer, ShootStates.Shoot, value);
            }
        }

        public bool MouseShootState
        {
            get
            {
                return ExpandValue(ShootStateContainer, ShootStates.MouseShoot);
            }
            set
            {
                CompressValue(ref ShootStateContainer, ShootStates.MouseShoot, value);
            }
        }

        public bool HudBarrelIndicatorState
        {
            get
            {
                return ExpandValue(ShootStateContainer, ShootStates.HudBarrelIndicator);
            }
            set
            {
                CompressValue(ref ShootStateContainer, ShootStates.HudBarrelIndicator, value);
            }
        }

        public bool IsSyncRequest
        {
            get
            {
                return ExpandValue(ShootStateContainer, ShootStates.IsSyncRequest);
            }
            set
            {
                CompressValue(ref ShootStateContainer, ShootStates.IsSyncRequest, value);
            }
        }

        #endregion

        #region TargetingStates

        public bool ResetTargetState
        {
            get
            {
                return ExpandValue(ResetTargetStateContainer, ShootStates.ResetTarget);
            }
            set
            {
                CompressValue(ref ResetTargetStateContainer, ShootStates.ResetTarget, value);
            }
        }

        public bool TargetGridsState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetGrids);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetGrids, value);
            }
        }

        public bool TargetSmallGridsState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetSmallGrids);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetSmallGrids, value);
            }
        }

        public bool TargetLargeGridsState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetLargeGrids);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetLargeGrids, value);
            }
        }

        public bool TargetCharactersState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetCharacters);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetCharacters, value);
            }
        }

        public bool TargetProjectilesState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetProjectiles);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetProjectiles, value);
            }
        }

        public bool TargetEnemiesState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetEnemies);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetEnemies, value);
            }
        }

        public bool TargetFriendliesState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetFriendlies);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetFriendlies, value);
            }
        }

        public bool TargetNeutralsState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetNeutrals);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetNeutrals, value);
            }
        }

        public bool TargetUnownedState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetUnowned);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetUnowned, value);
            }
        }

        public bool PreferUniqueTargetState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.PreferUniqueTarget);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.PreferUniqueTarget, value);
            }
        }

        #endregion

        public override string ToString()
        {
            return $"ShootState: {ShootState}\nAmmoLoadedIdx: {AmmoLoadedIdx}";
        }

        private bool ExpandValue(int bitwise, int enumValue)
        {
            return (bitwise & enumValue) == enumValue;
        }

        private void CompressValue(ref int bitwise, int enumValue, bool state)
        {
            if (state)
                bitwise |= enumValue;
            else
                bitwise &= ~enumValue; // AND with negated enumValue
        }

        private bool ExpandValue(short bitwise, int enumValue)
        {
            return (bitwise & enumValue) == enumValue;
        }

        private void CompressValue(ref short bitwise, int enumValue, bool state)
        {
            if (state)
                bitwise |= (short)enumValue;
            else
                bitwise &= (short)~enumValue; // AND with negated enumValue
        }

        private static class TargetingSettingStates
        {
            public const int TargetGrids = 2;
            public const int TargetLargeGrids = 4;
            public const int TargetSmallGrids = 8;
            public const int TargetProjectiles = 16;
            public const int TargetCharacters = 32;
            public const int TargetFriendlies = 64;
            public const int TargetNeutrals = 128;
            public const int TargetEnemies = 256;
            public const int TargetUnowned = 512;
            public const int PreferUniqueTarget = 1024;
        }

        private static class ShootStates
        {
            public const int Shoot = 1;
            public const int MouseShoot = 2;
            public const int HudBarrelIndicator = 4;
            public const int IsSyncRequest = 8;
            public const int ResetTarget = 16;
        }
    }
}
