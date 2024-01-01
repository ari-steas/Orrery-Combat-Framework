using Sandbox.Game.Entities;
using System.Collections.Generic;
using VanillaPlusFramework.TemplateClasses;

namespace VanillaPlusFramework.Beams
{
    public struct BeamDefinition
    {
        public float MaxTrajectory;

        public float MinReflectChance;
        public float  MaxReflectChance;
        public double MinReflectAngle;
        public double MaxReflectAngle;
        public float ReflectDamage;

        public float PenetrationDamage;
        public float PlayerDamage;
        public float ExplosiveDamage;
        public float ExplosiveRadius;

        public int ExplosionFlags;
        public string EndOfLifeEffect;
        public MySoundPair EndOfLifeSound;

        public BeamWeaponType_Logic BWT_Stats;
        public ProximityDetonation_Logic? PD_Stats;
        public EMP_Logic? EMP_Stats;
        public JumpDriveInhibition_Logic? JDI_Stats;
        public List<SpecialComponentryInteraction_Logic> SCI_Stats;
    }
}
