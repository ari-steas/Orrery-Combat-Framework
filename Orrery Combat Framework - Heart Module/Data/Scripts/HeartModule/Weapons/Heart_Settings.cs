using Heart_Module.Data.Scripts.HeartModule;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Network;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using ProtoBuf;
using Sandbox.ModAPI;

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class Heart_Settings : PacketBase // this will ABSOLUTELY bite me in the ass later.
    {
        public void Sync()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                HeartData.I.Net.SendToEveryone(this);
                HeartLog.Log("Sent settings to all");
            }
            //else
            //{
            //    HeartData.I.Net.SendToServer(this);
            //    HeartLog.Log("Sent settings to server");
            //}
        }

        public void RequestSync()
        {
            if (!MyAPIGateway.Session.IsServer)
            {
                HeartData.I.Net.SendToServer(new Heart_Settings() { IsRequest = true, WeaponEntityId = this.WeaponEntityId });
                HeartLog.Log("Requested settings from server...");
            }
        }

        [ProtoMember(1)]
        public bool ShootState;

        [ProtoMember(2)]
        public int AmmoLoadedState;

        [ProtoMember(3)]
        public float AiRange;

        [ProtoMember(4)]
        public bool TargetGridsState;

        [ProtoMember(5)]
        public bool TargetLargeGridsState;

        [ProtoMember(6)]
        public bool TargetSmallGridsState;

        [ProtoMember(7)]
        public bool TargetProjectilesState;

        [ProtoMember(8)]
        public bool TargetCharactersState;

        [ProtoMember(9)]
        public bool TargetFriendliesState;

        [ProtoMember(10)]
        public bool TargetNeutralsState;

        [ProtoMember(11)]
        public bool TargetEnemiesState;

        [ProtoMember(12)]
        public bool TargetUnownedState;

        [ProtoMember(13)]
        public long ControlTypeState;

        [ProtoMember(14)]
        public bool PreferUniqueTargetState;

        [ProtoMember(15)]
        public bool MouseShootState;

        [ProtoMember(16)]
        public bool HudBarrelIndicatorState;

        [ProtoMember(17)]
        public long WeaponEntityId;

        [ProtoMember(18)]
        public bool IsRequest = false;

        public override void Received(ulong SenderSteamId)
        {
            HeartLog.Log("Sender: " + SenderSteamId + " | Self: " + HeartData.I.SteamId);

            var weapon = WeaponManager.I.GetWeapon(WeaponEntityId);
            if (weapon == null)
                return;

            if (MyAPIGateway.Session.IsServer && IsRequest)
            {
                weapon.Settings.Sync();
                return;
            }

            weapon.Settings = this;
            if (MyAPIGateway.Session.IsServer)
                weapon.Settings.Sync();
        }
    }
}
