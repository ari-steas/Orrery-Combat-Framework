using Heart_Module.Data.Scripts.HeartModule.Network;
using ProtoBuf;
using System;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses
{
    /// <summary>
    /// Triggered whenever a weapon's magazine is emptied by the server.
    /// </summary>
    [ProtoContract]
    internal class n_MagazineUpdate : PacketBase
    {
        [ProtoMember(1)] internal long WeaponEntityId;
        [ProtoMember(2)] internal int MillisecondsFromMidnight;
        [ProtoMember(3)] internal int MagazinesLoaded;
        [ProtoMember(4)] internal short NextMuzzleIdx;

        public override void Received(ulong SenderSteamId)
        {
            var weapon = WeaponManager.I.GetWeapon(WeaponEntityId);
            HeartLog.Log($"Trigger update {WeaponEntityId}. " + (weapon == null));
            var magazine = weapon?.Magazines;
            if (magazine == null)
                return;

            float timeDelta = (float)((DateTime.UtcNow.TimeOfDay.TotalMilliseconds - MillisecondsFromMidnight) / 1000);

            magazine.EmptyMagazines();
            magazine.MagazinesLoaded = MagazinesLoaded;
            magazine.UpdateReload(timeDelta);

            weapon.NextMuzzleIdx = NextMuzzleIdx;

            HeartLog.Log($"Magazine updated for weapon {WeaponEntityId}! Delta: " + timeDelta);
        }
    }
}
