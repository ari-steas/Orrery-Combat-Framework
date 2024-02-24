using Heart_Module.Data.Scripts.HeartModule;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Network;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
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
            //HeartLog.Log("Sync called!");
            if (MyAPIGateway.Session.IsServer)
            {
                HeartData.I.Net.SendToEveryone(this);
                HeartLog.Log("Sent settings to all.\n" + ToString() + "\n---------------------------");
            }
            else
            {
                HeartData.I.Net.SendToServer(this);
                //HeartLog.Log("Sent settings to server");
            }
        }

        public static void RequestSync(long weaponEntityId)
        {
            if (MyAPIGateway.Session.IsServer)
                return;

            //HeartLog.Log("Requesting sync from server...");
            HeartData.I.Net.SendToServer(new Heart_Settings() { IsSyncRequest = true, WeaponEntityId = weaponEntityId });
        }

        [ProtoMember(1)]
        public bool ShootState;

        [ProtoMember(2)]
        public int AmmoLoadedId;

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
        public bool IsSyncRequest;

        // TODO: Use bitflags for booleans to save performance

        public override void Received(ulong SenderSteamId)
        {
            //HeartLog.Log("Recieve called: Sender: " + SenderSteamId + " | Self: " + HeartData.I.SteamId + "\n" + ToString());

            var weapon = WeaponManager.I.GetWeapon(WeaponEntityId);
            if (weapon == null)
            {
                //HeartLog.Log("Weapon doesn't exist! ThisId: " + WeaponEntityId);
                return;
            }

            if (IsSyncRequest)
            {
                weapon.Settings.Sync();
                return;
            }

            weapon.Settings = this;
            weapon.Magazines.SelectedAmmoId = AmmoLoadedId;
            if (MyAPIGateway.Session.IsServer)
                weapon.Settings.Sync();
        }

        public override string ToString()
        {
            return $"ShootState: {ShootState}\nAmmoLoadedId: {AmmoLoadedId} ({ProjectileDefinitionManager.GetDefinition(AmmoLoadedId).Name})\nAiRange: {AiRange}\nTargetGridsState: {TargetGridsState}\nTargetLargeGridsState: {TargetLargeGridsState}";
        }
    }
}
