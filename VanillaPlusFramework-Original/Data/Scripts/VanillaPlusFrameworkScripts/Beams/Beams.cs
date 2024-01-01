using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VanillaPlusFramework.Utilities;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using Ingame = VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRageMath;
using VanillaPlusFramework.TemplateClasses;
using VRage.Utils;
using VRage.Game;
using VanillaPlusFramework.Missiles;
using VanillaPlusFramework.FX;
using Sandbox.Game;
using Sandbox.Game.Entities;
using VRage.Library.Utils;
using Sandbox.Game.Weapons;
using Sandbox.Definitions;

namespace VanillaPlusFramework.Beams
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation)]
    public class Beams : MySessionComponentBase
    {
        public static int MaxLayers = 32;

        static List<VPFMissile> m_DetectedMissiles = new List<VPFMissile>();

        public static MyRandom Random = new MyRandom(13000);
        public static void GenerateBeam(Vector3D StartPos, Vector3D Forwards, Vector3D? Velocity, ref BeamDefinition Definition, IMyEntity OwningEntity, int layers = 0)
        {
            if (layers > MaxLayers || OwningEntity == null)
                return;
            
            if (layers != 0)
            {
                Definition.BWT_Stats.BeamRenderOffset = 0;
            }

            Definition.MinReflectAngle = MathHelper.ToRadians(Definition.MinReflectAngle);
            Definition.MaxReflectAngle = MathHelper.ToRadians(Definition.MaxReflectAngle);

            Vector3D EndPos = StartPos + Forwards * Definition.MaxTrajectory;
            LineD RayLine = new LineD(StartPos, EndPos);

            Vector3D hitPos = EndPos;
            Vector3 normal = Forwards;

            List<IHitInfo> HitEntities = new List<IHitInfo>();
            MyAPIGateway.Physics.CastRay(StartPos, EndPos, HitEntities);

            MyEntity LastHitEntity = null;

            bool EndBeam = true;

            

            for (int i = 0; i < HitEntities.Count; i++)
            {
                var ent = HitEntities[i].HitEntity;

                if (ent?.Physics == null)
                {
                    continue;
                }

                LastHitEntity = (MyEntity)ent;

                if (ent is IMyCubeGrid)
                {
                    bool HitSelf = ((OwningEntity is IMyCubeBlock && (OwningEntity as IMyCubeBlock).CubeGrid.GetGridGroup(GridLinkTypeEnum.Logical) == (ent as IMyCubeGrid).GetGridGroup(GridLinkTypeEnum.Logical)) 
                        || (OwningEntity is IMyCubeGrid && (OwningEntity as IMyCubeGrid).GetGridGroup(GridLinkTypeEnum.Logical) == (ent as IMyCubeGrid).GetGridGroup(GridLinkTypeEnum.Logical))) && layers == 0;

                    double dist = Definition.MaxTrajectory;
                    IMySlimBlock block;

                    Vector3D? vec;

                    Vector3 reflectionForwards;

                    

                    if (ShouldReflect(HitEntities[i].Normal, Forwards, Definition.MinReflectAngle, Definition.MaxReflectAngle, Definition.MinReflectChance, Definition.MaxReflectChance, out reflectionForwards))
                    {
                        vec = (ent as IMyCubeGrid).GetLineIntersectionExactAll(ref RayLine, out dist, out block);
                        if (!vec.HasValue || block == null)
                        {
                            continue;
                        }

                        float DamageToDo = Definition.ReflectDamage < Definition.PenetrationDamage ? Definition.ReflectDamage : Definition.PenetrationDamage;
                        if (!HitSelf)
                            DoDamageToEntity(block, DamageToDo, DefinitionTools.FunctionOutput(dist, Definition.BWT_Stats.BWT_DamageFalloffPiecewisePolynomialFunction), HitEntities[i].Normal, vec.Value);

                        Definition.PenetrationDamage -= DamageToDo;

                        Definition.MaxTrajectory -= (float)dist;
                        hitPos = HitEntities[i].Position;
                        if (Definition.PenetrationDamage <= 0 && Definition.ExplosiveDamage <= 0 && Definition.SCI_Stats == null && Definition.EMP_Stats == null)
                        {
                            break;
                        }
                        else
                        {
                            EndBeam = false;
                            GenerateBeam(HitEntities[i].Position, reflectionForwards, Velocity, ref Definition, OwningEntity, layers + 1);
                            break;
                        }
                    }
                    else
                    {
                        normal = HitEntities[i].Normal;
                        int fallback = 127;
                        while (Definition.PenetrationDamage >= 0 && fallback > 0)
                        {
                            fallback--;
                            vec = (ent as IMyCubeGrid).GetLineIntersectionExactAll(ref RayLine, out dist, out block);

                            if (!vec.HasValue || block == null) { break; }

                            if (!HitSelf)
                                Definition.PenetrationDamage = DoDamageToEntity(block, Definition.PenetrationDamage, DefinitionTools.FunctionOutput(dist, Definition.BWT_Stats.BWT_DamageFalloffPiecewisePolynomialFunction), HitEntities[i].Normal, vec.Value);
                            else
                            {
                                break;
                            }

                            if (HitEntities.Count > i + 1)
                            {
                                if (dist > HitEntities[i+1].Fraction * Definition.MaxTrajectory)
                                {
                                    IHitInfo info = new OurHitInfo()
                                    {
                                        Position = vec.Value,
                                        HitEntity = HitEntities[i].HitEntity,
                                        Fraction = (float)dist / Definition.MaxTrajectory,
                                        Normal = HitEntities[i].Normal,
                                    };
                                    
                                    for (int j = i + 1; j < HitEntities.Count; j++)
                                    {
                                        if (HitEntities[j].Fraction > info.Fraction)
                                        {
                                            HitEntities.AddOrInsert(info, j);
                                            HitEntities.Remove(HitEntities[i]);
                                            i--;
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        if (Definition.PenetrationDamage <= 0 || HitSelf)
                        {
                            hitPos = StartPos + Forwards * dist;
                            break;
                        }
                    }
                }
                else if (ent is IMyMissile && Definition.PD_Stats != null)
                {
                    AttemptDamageMissile(Definition.PD_Stats.Value.PD_AntiMissileDamage, ent as IMyMissile);
                }
                else if (ent is IMyDestroyableObject)
                {
                    bool isPlayer = false;
                    if (ent is IMyCharacter)
                    {
                        isPlayer = true;
                    }

                    Vector3 reflectionForwards;
                    if (ShouldReflect(HitEntities[i].Normal, Forwards, Definition.MinReflectAngle, Definition.MaxReflectAngle, Definition.MinReflectChance, Definition.MaxReflectChance, out reflectionForwards))
                    {
                        float DamageToDo;

                        if (isPlayer)
                        {
                            DamageToDo = Definition.ReflectDamage < Definition.PlayerDamage ? Definition.PlayerDamage : Definition.PenetrationDamage;
                        }
                        else DamageToDo = Definition.ReflectDamage < Definition.PenetrationDamage ? Definition.ReflectDamage : Definition.PenetrationDamage;

                        DoDamageToEntity(ent as IMyDestroyableObject, DamageToDo, 
                            DefinitionTools.FunctionOutput(HitEntities[i].Fraction * RayLine.Length, Definition.BWT_Stats.BWT_DamageFalloffPiecewisePolynomialFunction), 
                            HitEntities[i].Normal, HitEntities[i].Position);

                        Definition.PenetrationDamage -= DamageToDo;

                        Definition.MaxTrajectory *=  1 - HitEntities[i].Fraction;
                        hitPos = HitEntities[i].Position;

                        if (Definition.PenetrationDamage <= 0 && Definition.ExplosiveDamage <= 0 && Definition.SCI_Stats == null && Definition.EMP_Stats == null)
                        {
                            break;
                        }
                        else
                        {
                            EndBeam = false;

                            GenerateBeam(HitEntities[i].Position, reflectionForwards, Velocity, ref Definition, OwningEntity, layers + 1);
                            break;
                        }
                    }
                    else
                    {
                        if (isPlayer)
                        {
                            Definition.PlayerDamage = DoDamageToEntity(ent as IMyDestroyableObject, Definition.PlayerDamage, 
                                DefinitionTools.FunctionOutput(HitEntities[i].Fraction * RayLine.Length, Definition.BWT_Stats.BWT_DamageFalloffPiecewisePolynomialFunction), 
                                HitEntities[i].Normal, HitEntities[i].Position);

                            if (Definition.PlayerDamage <= 0)
                            {
                                hitPos = HitEntities[i].Position;
                                break;
                            }
                        }
                        else
                        {
                            Definition.PenetrationDamage = DoDamageToEntity(ent as IMyDestroyableObject, Definition.PenetrationDamage,
                                DefinitionTools.FunctionOutput(HitEntities[i].Fraction * RayLine.Length, Definition.BWT_Stats.BWT_DamageFalloffPiecewisePolynomialFunction),
                                HitEntities[i].Normal, HitEntities[i].Position);

                            if (Definition.PenetrationDamage <= 0)
                            {
                                hitPos = HitEntities[i].Position;
                                break;
                            }
                        }
                    }

                }
                else
                {
                    Vector3 reflectionForwards;
                    if (ShouldReflect(HitEntities[i].Normal, Forwards, Definition.MinReflectAngle, Definition.MaxReflectAngle, Definition.MinReflectChance, Definition.MaxReflectChance, out reflectionForwards))
                    {
                        Definition.PenetrationDamage -= Definition.ReflectDamage;
                        Definition.MaxTrajectory *= 1 - HitEntities[i].Fraction;

                        if (Definition.PenetrationDamage <= 0 && Definition.ExplosiveDamage <= 0 && Definition.SCI_Stats == null && Definition.EMP_Stats == null)
                        {
                            break;
                        }
                        else
                        {
                            EndBeam = false;
                            GenerateBeam(HitEntities[i].Position, reflectionForwards, Velocity, ref Definition, OwningEntity, layers + 1);
                        }
                    }
                    normal = HitEntities[i].Normal;
                    hitPos = HitEntities[i].Position;
                    break;
                }
            }
            HitEntities.Clear();

            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                FXRenderer.ObjectsToRender.Add(new LineRenderer(StartPos + Forwards * Definition.BWT_Stats.BeamRenderOffset, hitPos, 
                    Definition.BWT_Stats.BWT_BeamColor, Definition.BWT_Stats.BWT_BeamThickness, Definition.BWT_Stats.BWT_TimeActive,
                    Definition.BWT_Stats.BWT_Fade, Velocity.HasValue ? Velocity.Value : Vector3D.Zero, "WeaponLaser", VRageRender.MyBillboard.BlendTypeEnum.Standard));
            }

            if (EndBeam)
            {
                if (Definition.PD_Stats != null && Definition.PD_Stats.Value.PD_AntiMissileDamage > 0 && Definition.PD_Stats.Value.PD_DetonationRadius > 0)
                {
                    CheckMissilesInSphere(new BoundingSphereD(hitPos, Definition.PD_Stats.Value.PD_DetonationRadius), Definition.PD_Stats.Value.PD_AntiMissileDamage);
                }
                if (Definition.ExplosiveDamage > 0 && Definition.ExplosiveRadius > 0)
                {
                    BoundingSphereD sphere = new BoundingSphereD(hitPos, Definition.ExplosiveRadius);

                    if (LastHitEntity == null)
                    {
                        List<IMyEntity> entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
                        entities.AddList(MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere));

                        for (int i = 0; i < entities.Count; i++)
                        {
                            if (entities[i] is MyVoxelBase)
                            {
                                entities.Remove(entities[i]);
                            }
                        }

                        if (entities.Count > 0)
                        {
                            LastHitEntity = (MyEntity)entities[0];
                        }
                    }

                    float interference;
                    var gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(sphere.Center, out interference);

                    if (gravity.LengthSquared() == 0)
                        MyAPIGateway.Physics.CalculateArtificialGravityAt(sphere.Center, interference);

                    Vector3D direction = gravity.LengthSquared() > 0 ? Vector3D.Normalize(-gravity) : (Vector3D)normal;

                    MyExplosionInfo explosionInfo = new MyExplosionInfo
                    {
                        PlayerDamage = Definition.ExplosiveDamage,
                        Damage = Definition.ExplosiveDamage,
                        ExplosionType = ((Definition.EndOfLifeEffect == null && Definition.EndOfLifeSound == null) && Definition.BWT_Stats.BWT_ShowExplosionFX) ? MyExplosionTypeEnum.MISSILE_EXPLOSION : MyExplosionTypeEnum.CUSTOM,
                        //ExplosionType = MyExplosionTypeEnum.CUSTOM,
                        ExplosionSphere = sphere,
                        StrengthImpulse = 0,
                        LifespanMiliseconds = 700,
                        HitEntity = LastHitEntity,
                        ParticleScale = Definition.ExplosiveRadius / 6f,
                        OwnerEntity = (MyEntity)OwningEntity,
                        CustomEffect = Definition.BWT_Stats.BWT_ShowExplosionFX ? Definition.EndOfLifeEffect : "NullParticle",
                        CustomSound = Definition.BWT_Stats.BWT_ShowExplosionFX ? Definition.EndOfLifeSound : new MySoundPair("NullSound"), // custom sound in sbc w/ no sound (doesn't work)
                        ShouldDetonateAmmo = true,
                        ExcludedEntity = OwningEntity is IMyCubeBlock ? (MyEntity)(OwningEntity as IMyCubeBlock).CubeGrid : (MyEntity)OwningEntity,
                        EffectHitAngle = MyObjectBuilder_MaterialPropertiesDefinition.EffectHitAngle.DeflectUp,
                        Direction = direction,
                        DirectionNormal = direction,
                        VoxelExplosionCenter = sphere.Center,
                        Velocity = Vector3.Zero,
                        
                        VoxelCutoutScale = 0.3f,
                        PlaySound = true,
                        ApplyForceAndDamage = true,
                        OriginEntity = OwningEntity.EntityId,
                        KeepAffectedBlocks = true,
                        IgnoreFriendlyFireSetting = false,
                        CheckIntersections = true,
                    };
                    //MyExplosionFlags.CREATE_PARTICLE_EFFECT
                    explosionInfo.ExplosionFlags = FrameworkUtilities.CastHax(explosionInfo.ExplosionFlags, Definition.ExplosionFlags); // probably fixed tmr by keen but idc

                    MyExplosions.AddExplosion(ref explosionInfo);

                    
                }

                if (Definition.EMP_Stats != null && MyAPIGateway.Multiplayer.IsServer)
                {
                    FrameworkUtilities.EMP(hitPos, Definition.EMP_Stats.Value.EMP_Radius, Definition.EMP_Stats.Value.EMP_TimeDisabled);
                }

                if (LastHitEntity != null)
                {
                    if (Definition.SCI_Stats != null && Definition.SCI_Stats.Count > 0)
                    {
                        FrameworkUtilities.SCI_Hit(LastHitEntity, Definition.SCI_Stats, hitPos);
                    }

                    if (Definition.JDI_Stats != null)
                    {
                        FrameworkUtilities.JDI_Hit(LastHitEntity, Definition.JDI_Stats.Value.JDI_PowerDrainInW, Definition.JDI_Stats.Value.JDI_DistributePower);
                    }
                }
            }
        }



        public struct OurHitInfo : IHitInfo
        {
            public Vector3D Position;

            public IMyEntity HitEntity;

            public Vector3 Normal;

            public float Fraction;

            Vector3D IHitInfo.Position => Position;

            IMyEntity IHitInfo.HitEntity => HitEntity;

            Vector3 IHitInfo.Normal => Normal;

            float IHitInfo.Fraction => Fraction;
        }

        private static float GetRicochetProbability(float impactAngle, double MinAngle, double MaxAngle, float MinPercent, float MaxPercent)
        {
            if (MinAngle == -1 && impactAngle < MaxAngle)
            {
                return 0f;
            }

            if (impactAngle < MinAngle)
            {
                return 0f;
            }

            if (impactAngle > MaxAngle)
            {
                return MaxPercent;
            }

            float num = (float)MaxAngle - (float)MinAngle;
            if (Math.Abs(num) < 1E-06f)
            {
                return MaxPercent;
            }

            float num2 = ((float)MaxPercent - MinPercent) / num;
            return MinPercent + num2 * (impactAngle - (float)MinAngle);
        }

        public static bool ShouldReflect(Vector3 Normal, Vector3 Forwards, double MinAngle, double MaxAngle, float MinPercent, float MaxPercent, out Vector3 reflectionForwards)
        {
            if (MaxPercent == 0)
            {
                reflectionForwards = Forwards; 
                return false;
            }

            float angle = MyMath.AngleBetween(Normal, -Forwards);

            float probability = GetRicochetProbability(angle, MinAngle, MaxAngle, MinPercent, MaxPercent);

            if (probability > 0)
            {
                double d = Random.NextDouble();

                if (d < probability)
                {
                    Matrix RotationMatrix = Matrix.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(Normal, (float)Math.PI));
                    reflectionForwards = Vector3.Transform(Forwards, RotationMatrix);
                    reflectionForwards = -reflectionForwards;
                    return true;
                }
            }
            reflectionForwards = Forwards;
            
            return false;
        }

        public static float DoDamageToEntity(IMyDestroyableObject ent, float Damage, double DamageMultiplier, Vector3 Normal, Vector3 Position, long AttackerId = 0)
        {
            float remainingDamage = Damage - ent.Integrity;

            ent.DoDamage(Damage * (float)DamageMultiplier / 100, MyStringHash.GetOrCompute("WeaponLaser"), true, new MyHitInfo
            {
                Normal = Normal,
                Position = Position,
                Velocity = Vector3.Zero,

            }, AttackerId, 0, true);

            return remainingDamage;
        }

        private static void CheckMissilesInSphere(BoundingSphere sphere, float Damage)
        {
            MissileLogic.MissileTree.OverlapAllBoundingSphere(ref sphere, m_DetectedMissiles, false);

            foreach (VPFMissile VPFMissileObject in m_DetectedMissiles)
            {
                if (VPFMissileObject.missile != null && !VPFMissileObject.missile.MarkedForClose && !VPFMissileObject.missile.Closed &&
                    Vector3.DistanceSquared(VPFMissileObject.missile.GetPosition(), sphere.Center) <= sphere.Radius * sphere.Radius * 2)
                {
                    if (!VPFMissileObject.IsDamaged)
                    {
                        VPFMissileObject.IsDamaged = true;
                        VPFMissileObject.missileHP -= Damage;
                        if (VPFMissileObject.missileHP <= 0)
                            VPFMissileObject.Close();
                    }
                }
            }
            m_DetectedMissiles.Clear();
        }

        private static void AttemptDamageMissile(float Damage, IMyMissile nearbyMissile)
        {
            VPFMissile VPFMissileObject;
            if (MissileLogic.Missiles.TryGetValue(nearbyMissile.EntityId, out VPFMissileObject))
            {
                if (VPFMissileObject.missile == nearbyMissile && !VPFMissileObject.IsDamaged)
                {
                    VPFMissileObject.IsDamaged = true;
                    VPFMissileObject.missileHP -= Damage;
                    if (VPFMissileObject.missileHP <= 0)
                    {
                        VPFMissileObject.DetonateSelf(0, true);
                        VPFMissileObject.Close();
                    }
                }
            }
        }
    }
}
