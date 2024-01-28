using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using ProtoBuf;

namespace Heart_Module.Data.Scripts.HeartModule.Definitions.StandardClasses
{
    [ProtoContract]
    internal class DefinitionContainer
    {
        [ProtoMember(1)]
        public WeaponDefinitionBase[] WeaponDefs { get; set; }
        [ProtoMember(2)]
        public ProjectileDefinitionBase[] AmmoDefs { get; set; }
    }
}
