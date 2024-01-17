using Heart_Module.Data.Scripts.HeartModule.Network;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses
{
    [ProtoContract]
    public class n_TurretFacing : PacketBase
    {
        // TODO: Add support for turrets
        [ProtoMember(21)] long TurretId;
        [ProtoMember(22)] float Azimuth;
        [ProtoMember(23)] float Elevation;

        public n_TurretFacing() { }
        public n_TurretFacing(SorterTurretLogic turret)
        {
            TurretId = turret.SorterWep.EntityId;
            Azimuth = (float) turret.DesiredAzimuth;
            Elevation = (float) turret.DesiredElevation;
        }

        public override void Received(ulong SenderSteamId)
        {
            if (!MyAPIGateway.Session.IsServer)
                (WeaponManager.I.GetWeapon(TurretId) as SorterTurretLogic)?.SetFacing(Azimuth, Elevation);
        }
    }

    [ProtoContract]
    public class n_TurretFacingArray : PacketBase
    {
        [ProtoMember(21)] byte[][] Facings = new byte[0][];

        public n_TurretFacingArray() { }
        public n_TurretFacingArray(List<n_TurretFacing> facings)
        {
            SerializeProjectiles(facings.ToArray());
        }

        public n_TurretFacingArray(n_TurretFacing[] facings)
        {
            SerializeProjectiles(facings);
        }

        private void SerializeProjectiles(n_TurretFacing[] facings)
        {
            Facings = new byte[facings.Length][];

            for (int i = 0; i < Facings.Length; i++)
                Facings[i] = MyAPIGateway.Utilities.SerializeToBinary(facings[i]);
        }

        private n_TurretFacing[] DeSerializeProjectiles()
        {
            n_TurretFacing[] deSerialized = new n_TurretFacing[Facings.Length];

            for (int i = 0; i < Facings.Length; i++)
                deSerialized[i] = MyAPIGateway.Utilities.SerializeFromBinary<n_TurretFacing>(Facings[i]);

            return deSerialized;
        }

        public override void Received(ulong SenderSteamId)
        {
            if (MyAPIGateway.Session.IsServer)
                return;
            foreach (var projectile in DeSerializeProjectiles())
                projectile?.Received(SenderSteamId);
        }
    }
}
