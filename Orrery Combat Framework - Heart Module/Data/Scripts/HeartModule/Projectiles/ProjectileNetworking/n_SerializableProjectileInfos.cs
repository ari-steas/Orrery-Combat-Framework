using Heart_Module.Data.Scripts.HeartModule.Network;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.ProjectileNetworking
{
    // TODO: Make these implement packetbase stuff

    [ProtoContract]
    internal class n_SerializableProjectileInfos : PacketBase
    {
        public n_SerializableProjectileInfos() { }

        public n_SerializableProjectileInfos(uint uniqueProjectileId, Vector3 positionRelativeToPlayer, Vector3 direction, int definitionId, int msFromMidnight, long? firerEntityId = null, long? targetEntityId = null, uint? projectileAge = null)
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

        public n_SerializableProjectileInfos(Projectile projectile, IMyCharacter character, ProjectileDetailLevel detailLevel = ProjectileDetailLevel.Full)
        {
            UniqueProjectileId = projectile.Id;
            playerRelativeX = (float) (projectile.Position.X - character.GetPosition().X);
            playerRelativeY = (float) (projectile.Position.Y - character.GetPosition().Y);
            playerRelativeZ = (float) (projectile.Position.Z - character.GetPosition().Z);
            directionX = (float) projectile.Direction.X;
            directionY = (float) projectile.Direction.Y;
            directionZ = (float) projectile.Direction.Z;
            MillisecondsFromMidnight = (int)DateTime.Now.TimeOfDay.TotalMilliseconds;

            if (detailLevel != ProjectileDetailLevel.Minimal)
            {
                DefinitionId = projectile.DefinitionId;
                FirerEntityId = projectile.Firer;
                if (detailLevel != ProjectileDetailLevel.NoGuidance)
                {
                    TargetEntityId = projectile.Guidance?.GetTarget()?.EntityId;
                    ProjectileAge = (uint)(projectile.Age * 60);
                }
            }        
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

        [ProtoMember(28)] public int? DefinitionId;
        [ProtoMember(29)] public int MillisecondsFromMidnight;
        [ProtoMember(30)] public long? FirerEntityId;

        [ProtoMember(31)] public long? TargetEntityId;
        [ProtoMember(32)] public uint? ProjectileAge;

        public override void Received(ulong SenderSteamId)
        {
            
        }

        public enum ProjectileDetailLevel
        {
            Full = 0,
            NoGuidance = 1,
            Minimal = 2,
        }
    }

    [ProtoContract]
    internal class n_SerializableFireEvents : PacketBase
    {
        public n_SerializableFireEvents() { }

        public n_SerializableFireEvents(long firerWeaponId, uint uniqueProjectileId, Vector3 direction, int millisecondsFromMidnight)
        {
            FirerWeaponId = firerWeaponId;
            UniqueProjectileId = uniqueProjectileId;
            directionX = direction.X;
            directionY = direction.Y;
            directionZ = direction.Z;
            MillisecondsFromMidnight = millisecondsFromMidnight;
        }

        [ProtoMember(21)] public long FirerWeaponId;
        [ProtoMember(22)] public uint UniqueProjectileId;

        [ProtoMember(23)] private float directionX;
        [ProtoMember(24)] private float directionY;
        [ProtoMember(25)] private float directionZ;
        public Vector3 Direction => new Vector3(directionX, directionY, directionZ); // just using a Vector3 adds 2 extra bytes (!!!)


        [ProtoMember(26)] public int MillisecondsFromMidnight;

        public override void Received(ulong SenderSteamId)
        {

        }
    }
}
