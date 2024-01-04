using Digi.NetworkLib;
using ProtoBuf;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses
{
    /// <summary>
    /// Used for syncing between server and clients, and in the API.
    /// </summary>
    [ProtoContract]
    public class SerializableProjectile : PacketBase
    {
        [ProtoMember(1)] public uint Id;
        [ProtoMember(2)] public int DefinitionId;
        [ProtoMember(3)] public Vector3D Position;
        [ProtoMember(4)] public Vector3D Direction;
        [ProtoMember(5)] public Vector3D InheritedVelocity;
        [ProtoMember(6)] public float Velocity;
        [ProtoMember(7)] public Dictionary<string, byte[]> OverridenValues;
        [ProtoMember(8)] public long Timestamp;

        public override void Received(ref PacketInfo packetInfo, ulong senderSteamId)
        {
            if (MyAPIGateway.Session.IsServer)
                return;

            ProjectileManager.I.GetProjectile(Id)?.SyncUpdate(this);
            MyLog.Default.WriteLineAndConsole("Recieved projectile!");
        }
    }
}
