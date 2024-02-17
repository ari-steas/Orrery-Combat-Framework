using Heart_Module.Data.Scripts.HeartModule.Network;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.ProjectileNetworking
{
    // TODO: Make these implement packetbase stuff

    [ProtoContract]
    internal class n_SerializableProjectileInfo : PacketBase
    {
        public n_SerializableProjectileInfo() { }

        public n_SerializableProjectileInfo(uint uniqueProjectileId, Vector3 positionRelativeToPlayer, Vector3 direction, int definitionId, int msFromMidnight, long? firerEntityId = null, long? targetEntityId = null, uint? projectileAge = null)
        {
            UniqueProjectileId = uniqueProjectileId;
            playerRelativeX = positionRelativeToPlayer.X;
            playerRelativeY = positionRelativeToPlayer.Y;
            playerRelativeZ = positionRelativeToPlayer.Z;
            directionX = direction.X;
            directionY = direction.Y;
            directionZ = direction.Z;
            DefinitionId = definitionId;
            MillisecondsFromMidnight = msFromMidnight;
            FirerEntityId = firerEntityId;
            TargetEntityId = targetEntityId;
            ProjectileAge = projectileAge;
        }

        [ProtoMember(21)] public uint UniqueProjectileId;

        [ProtoMember(22)] private float playerRelativeX;
        [ProtoMember(23)] private float playerRelativeY;
        [ProtoMember(24)] private float playerRelativeZ;
        public Vector3 PlayerRelativePosition => new Vector3(playerRelativeX, playerRelativeY, playerRelativeZ); // just using a Vector3 adds 2 extra bytes (!!!)

        [ProtoMember(25)] private float directionX;
        [ProtoMember(26)] private float directionY;
        [ProtoMember(27)] private float directionZ;
        public Vector3 Direction => new Vector3(directionX, directionY, directionZ); // just using a Vector3 adds 2 extra bytes (!!!)

        [ProtoMember(28)] public int DefinitionId;
        [ProtoMember(29)] public int MillisecondsFromMidnight;
        [ProtoMember(30)] public long? FirerEntityId;

        [ProtoMember(31)] public long? TargetEntityId;
        [ProtoMember(32)] public uint? ProjectileAge;
    }

    [ProtoContract]
    internal class n_SerializableFireEvent : PacketBase
    {
        public n_SerializableFireEvent() { }

        public n_SerializableFireEvent(int firerWeaponId, uint uniqueProjectileId, Vector3 direction, int millisecondsFromMidnight)
        {
            FirerWeaponId = firerWeaponId;
            UniqueProjectileId = uniqueProjectileId;
            directionX = direction.X;
            directionY = direction.Y;
            directionZ = direction.Z;
            MillisecondsFromMidnight = millisecondsFromMidnight;
        }

        [ProtoMember(21)] int FirerWeaponId;
        [ProtoMember(22)] uint UniqueProjectileId;

        [ProtoMember(23)] float directionX;
        [ProtoMember(24)] float directionY;
        [ProtoMember(25)] float directionZ;
        public Vector3 Direction => new Vector3(directionX, directionY, directionZ); // just using a Vector3 adds 2 extra bytes (!!!)


        [ProtoMember(26)] int MillisecondsFromMidnight;
    }
}
