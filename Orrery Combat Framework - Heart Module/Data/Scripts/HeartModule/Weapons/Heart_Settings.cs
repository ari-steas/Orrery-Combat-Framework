using ProtoBuf;

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class Heart_Settings
    {
        [ProtoMember(1)]
        public bool ShootState;

        [ProtoMember(2)]
        public float AiRange;

    }
}
