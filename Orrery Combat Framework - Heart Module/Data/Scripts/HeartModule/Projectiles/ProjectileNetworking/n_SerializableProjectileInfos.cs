using Heart_Module.Data.Scripts.HeartModule.Network;
using ProtoBuf;
using Sandbox.ModAPI;
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

        public n_SerializableProjectileInfos(uint[] uniqueProjectileId, Vector3[] positionRelativeToPlayer, Vector3[] direction, int[] definitionId, int[] msFromMidnight, long[] firerEntityId = null, long?[] targetEntityId = null, uint[] projectileAge = null)
        {
            UniqueProjectileId = uniqueProjectileId;

            playerRelativeX = new float[positionRelativeToPlayer.Length];
            playerRelativeY = new float[positionRelativeToPlayer.Length];
            playerRelativeZ = new float[positionRelativeToPlayer.Length];
            for (int i = 0; i < positionRelativeToPlayer.Length; i++)
            {
                playerRelativeX[i] = positionRelativeToPlayer[i].X;
                playerRelativeY[i] = positionRelativeToPlayer[i].Y;
                playerRelativeZ[i] = positionRelativeToPlayer[i].Z;
            }

            directionX = new float[direction.Length];
            directionY = new float[direction.Length];
            directionZ = new float[direction.Length];
            for (int i = 0; i < direction.Length; i++)
            {
                directionX[i] = direction[i].X;
                directionY[i] = direction[i].Y;
                directionZ[i] = direction[i].Z;
            }

            DefinitionId = definitionId;
            MillisecondsFromMidnight = msFromMidnight;
            FirerEntityId = firerEntityId;
            TargetEntityId = targetEntityId;
            ProjectileAge = projectileAge;
        }

        public n_SerializableProjectileInfos(List<Projectile> projectiles, IMyCharacter character, ProjectileDetailLevel detailLevel = ProjectileDetailLevel.Full)
        {
            UniqueProjectileId = new uint[projectiles.Count];
            playerRelativeX = new float[projectiles.Count];
            playerRelativeY = new float[projectiles.Count];
            playerRelativeZ = new float[projectiles.Count];
            directionX = new float[projectiles.Count];
            directionY = new float[projectiles.Count];
            directionZ = new float[projectiles.Count];
            MillisecondsFromMidnight = new int[projectiles.Count];

            if (detailLevel != ProjectileDetailLevel.Minimal)
            {
                DefinitionId = new int[projectiles.Count];
                FirerEntityId = new long[projectiles.Count];
                if (detailLevel != ProjectileDetailLevel.NoGuidance)
                {
                    TargetEntityId = new long?[projectiles.Count];
                    ProjectileAge = new uint[projectiles.Count];
                }
            }
            

            Vector3D characterPos = character.GetPosition();

            for (int i = 0; i < projectiles.Count; i++)
            {
                UniqueProjectileId[i] = projectiles[i].Id;
                playerRelativeX[i] = (float)(projectiles[i].Position.X - characterPos.X);
                playerRelativeY[i] = (float)(projectiles[i].Position.Y - characterPos.Y);
                playerRelativeZ[i] = (float)(projectiles[i].Position.Z - characterPos.Z);
                directionX[i] = (float)projectiles[i].Direction.X;
                directionY[i] = (float)projectiles[i].Direction.Y;
                directionZ[i] = (float)projectiles[i].Direction.Z;

                MillisecondsFromMidnight[i] = (int)DateTime.Now.TimeOfDay.TotalMilliseconds;

                if (detailLevel != ProjectileDetailLevel.Minimal)
                {
                    DefinitionId[i] = projectiles[i].DefinitionId;
                    FirerEntityId[i] = projectiles[i].Firer;
                    if (detailLevel != ProjectileDetailLevel.NoGuidance)
                    {
                        TargetEntityId[i] = projectiles[i].Guidance?.GetTarget()?.EntityId;
                        ProjectileAge[i] = (uint)(projectiles[i].Age * 60);
                    }
                }
            }  
        }


        [ProtoMember(21)] public uint[] UniqueProjectileId;

        [ProtoMember(22)] private float[] playerRelativeX;
        [ProtoMember(23)] private float[] playerRelativeY;
        [ProtoMember(24)] private float[] playerRelativeZ;

        public Vector3[] PlayerRelativePosition() // just using a Vector3 adds 2 extra bytes (!!!)
        {
            Vector3[] array = new Vector3[playerRelativeX.Length];

            for (int i = 0; i < array.Length; i++)
                array[i] = new Vector3(playerRelativeX[i], playerRelativeY[i], playerRelativeZ[i]);

            return array;
        }

        public Vector3 PlayerRelativePosition(int index)
        {
            return new Vector3(playerRelativeX[index], playerRelativeY[index], playerRelativeZ[index]);
        }

        [ProtoMember(25)] private float[] directionX;
        [ProtoMember(26)] private float[] directionY;
        [ProtoMember(27)] private float[] directionZ;

        public Vector3[] Direction() // just using a Vector3 adds 2 extra bytes (!!!)
        {
            Vector3[] array = new Vector3[directionX.Length];

            for (int i = 0; i < array.Length; i++)
                array[i] = new Vector3(directionX[i], directionY[i], directionZ[i]);

            return array;
        }

        public Vector3 Direction(int index)
        {
            return new Vector3(directionX[index], directionY[index], directionZ[index]);
        }

        [ProtoMember(28)] public int[] DefinitionId;
        [ProtoMember(29)] public int[] MillisecondsFromMidnight;
        [ProtoMember(30)] public long[] FirerEntityId;

        [ProtoMember(31)] public long?[] TargetEntityId;
        [ProtoMember(32)] public uint[] ProjectileAge;

        public override void Received(ulong SenderSteamId)
        {
            ProjectileManager.I.Network.Recieve_PP(this);
        }

        public enum ProjectileDetailLevel
        {
            Full = 0,
            NoGuidance = 1,
            Minimal = 2,
        }

        /// <summary>
        /// This is dangerous to call!
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Projectile ToProjectile(int index)
        {
            if (DefinitionId == null)
                return null;

            Projectile p = new Projectile(
                DefinitionId[index],
                PlayerRelativePosition(index) + MyAPIGateway.Session.Player.Character.GetPosition(),
                Direction(index),
                FirerEntityId[index],
                ((IMyCubeBlock)MyAPIGateway.Entities.GetEntityById(FirerEntityId[index]))?.CubeGrid.LinearVelocity ?? Vector3D.Zero
                )
            {
                LastUpdate = DateTime.Now.Date.AddMilliseconds(MillisecondsFromMidnight[index]).Ticks
            };

            if (ProjectileAge != null)
                p.Age = ProjectileAge[index];
            if (TargetEntityId != null)
            {
                if (TargetEntityId[index] == null)
                    p.Guidance.SetTarget(null);
                else
                    p.Guidance.SetTarget(MyAPIGateway.Entities.GetEntityById(TargetEntityId[index]));
            }
                

            float delta = (DateTime.Now.Ticks - p.LastUpdate) / (float)TimeSpan.TicksPerSecond;
            p.TickUpdate(delta);

            return p;
        }
    }

    [ProtoContract]
    internal class n_SerializableFireEvents : PacketBase
    {
        public n_SerializableFireEvents() { }

        public n_SerializableFireEvents(long[] firerWeaponId, uint[] uniqueProjectileId, Vector3[] direction, int[] millisecondsFromMidnight)
        {
            FirerWeaponId = firerWeaponId;
            UniqueProjectileId = uniqueProjectileId;

            directionX = new float[direction.Length];
            directionY = new float[direction.Length];
            directionZ = new float[direction.Length];
            for (int i = 0; i < direction.Length; i++)
            {
                directionX[i] = direction[i].X;
                directionY[i] = direction[i].Y;
                directionZ[i] = direction[i].Z;
            }

            MillisecondsFromMidnight = millisecondsFromMidnight;
        }

        [ProtoMember(21)] public long[] FirerWeaponId;
        [ProtoMember(22)] public uint[] UniqueProjectileId;

        [ProtoMember(23)] private float[] directionX;
        [ProtoMember(24)] private float[] directionY;
        [ProtoMember(25)] private float[] directionZ;

        public Vector3[] Direction() // just using a Vector3 adds 2 extra bytes (!!!)
        {
            Vector3[] array = new Vector3[directionX.Length];

            for (int i = 0; i < array.Length; i++)
                array[i] = new Vector3(directionX[i], directionY[i], directionZ[i]);

            return array;
        }

        [ProtoMember(26)] public int[] MillisecondsFromMidnight;

        public override void Received(ulong SenderSteamId)
        {
            ProjectileManager.I.Network.Recieve_FireEvent(this);
        }
    }
}
