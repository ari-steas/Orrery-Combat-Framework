using ProtoBuf;
using System.Collections.Generic;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses
{
    /// <summary>
    /// Used for syncing between server and clients, and in the API.
    /// </summary>
    [ProtoContract]
    public class SerializableProjectile
    {
        [ProtoMember(1)] public uint Id;
        [ProtoMember(2)] public int DefinitionId;
        [ProtoMember(3)] public Vector3D Position;
        [ProtoMember(4)] public Vector3D Direction;
        [ProtoMember(9)] public Vector3D InheritedVelocity;
        [ProtoMember(5)] public float Velocity;
        [ProtoMember(6)] public float Acceleration;
        [ProtoMember(7)] public Dictionary<string, byte[]> OverridenValues;
        [ProtoMember(8)] public long Timestamp;
    }
}
