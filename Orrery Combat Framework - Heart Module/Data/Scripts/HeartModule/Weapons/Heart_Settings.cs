using ProtoBuf;

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class Heart_Settings
    {
        [ProtoMember(1)]
        public bool ShootState;

        [ProtoMember(2)]
        public long AmmoLoadedState;

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
    }
}
