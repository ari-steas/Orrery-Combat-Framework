using ProtoBuf;

namespace Heart_Module.Data.Scripts.HeartModule.Definitions.StandardClasses
{
    [ProtoContract]
    internal class DefinitionContainer
    {
        [ProtoMember(1)]
        public byte[][] WeaponDefs { get; set; }
        [ProtoMember(2)]
        public byte[][] AmmoDefs { get; set; }
    }
}
