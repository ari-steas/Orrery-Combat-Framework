using OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication.ProjectileBases;
using OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication.WeaponBases;
using ProtoBuf;

namespace OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication
{
    partial class HeartDefinitions
    {
        internal DefinitionContainer Container = new DefinitionContainer();

        internal void LoadWeaponDefinitions(params WeaponDefinitionBase[] defs)
        {
            Container.WeaponDefs = defs;
        }
        internal void LoadAmmoDefinitions(params ProjectileDefinitionBase[] defs)
        {
            Container.AmmoDefs = defs;
        }

        /// <summary>
        /// Load all definitions for DefinitionSender
        /// </summary>
        /// <param name="baseDefs"></param>
        internal static DefinitionContainer GetBaseDefinitions()
        {
            return new HeartDefinitions().Container;
        }
    }

    [ProtoContract]
    internal class DefinitionContainer
    {
        [ProtoMember(1)]
        public WeaponDefinitionBase[] WeaponDefs { get; set; }
        [ProtoMember(2)]
        public ProjectileDefinitionBase[] AmmoDefs { get; set; }
    }
}
