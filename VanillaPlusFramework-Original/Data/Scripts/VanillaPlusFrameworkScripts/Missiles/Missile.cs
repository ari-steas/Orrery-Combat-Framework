using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.EntityComponents;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage.Game.Models;
using VRage.Render.Particles;
using System.Linq.Expressions;
using System.IO;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Weapons;
using VRage;
using VRage.Collections;
using VRage.Voxels;
using ProtoBuf;
using SpaceEngineers.Game.Entities.Blocks;
using VanillaPlusFramework.TemplateClasses;
using VanillaPlusFramework.Utilities;
using VanillaPlusFramework.Beams;
using VanillaPlusFramework.FX;
using Sandbox.Game.GameSystems;
using System.ComponentModel;
using Sandbox.Game.Entities.Cube;

namespace VanillaPlusFramework.Missiles
{
    [ProtoContract]
    public class VPFMissile
    {
        #region Variables and Constructor
        public IMyMissile missile;
        public float missileHP;

        public int ProxyId = -1;

        MyMissileAmmoDefinition missileAmmoDefinition;

        /// <summary>
        /// time since spawn in seconds
        /// </summary>
        private float time = 0;
        private const float ticktime = 1f / 60f;

        /// <summary>
        /// time since spawn in ticks
        /// </summary>
        private int ticks;

        List<VPFMissile> detectedMissiles = new List<VPFMissile>();

        private Vector3D CurrentPosition;
        private MatrixD LastWorldMatrix;
        private Vector3 MissileVelocity;
        private Vector3 MissileForwardDirection;

        public static float MaximumRetargetRadius = 0;

        public EMP_Logic? EMP_Stats;
        public GuidanceLock_Logic? GL_Stats;
        public ProximityDetonation_Logic? PD_Stats;
        public JumpDriveInhibition_Logic? JDI_Stats;
        public BeamWeaponType_Logic? BWT_Stats;
        public List<SpecialComponentryInteraction_Logic> SCI_Stats;

        public float TimeToArm = 0;

        public VPFVisualEffectsDefinition CustomVisualEffects = null;

        public IMyEntity GL_Target = null;
        private Vector3? GL_GPSTargetPos = null;
        private Vector3 GL_PrevLeadPos = Vector3.Zero;
        private Vector3 GL_Direction = Vector3.Zero;
        private Vector3 GL_PrevTargetPos;

        public bool collided = false;
        private bool IsClosed = false;
        public bool IsDamaged = false;
        public bool SyncTarget = false;
        public bool IsDisarmed = false;

        public bool NeedsAPHEFix = false;
        private float APHE_ExplosionDamage;
        private float APHE_ExplosionRadius;

        public MyEntity LastHitEntity;
        public VPFMissile() { }

        public VPFMissile(IMyMissile missile, VPFAmmoDefinition AmmoDefinition, VPFVisualEffectsDefinition effects)
        {
            // Missile Constructor
            this.missile = missile;
            missileHP = AmmoDefinition.VPF_MissileHitpoints;

            missileAmmoDefinition = (MyMissileAmmoDefinition)missile.AmmoDefinition;

            CurrentPosition = missile.GetPosition();
            MissileVelocity = missile.LinearVelocity;
            MissileForwardDirection = missile.WorldMatrix.Forward;

            EMP_Stats = AmmoDefinition.EMP_Stats;
            GL_Stats = AmmoDefinition.GL_Stats;
            PD_Stats = AmmoDefinition.PD_Stats;
            JDI_Stats = AmmoDefinition.JDI_Stats;
            BWT_Stats = AmmoDefinition.BWT_Stats;
            SCI_Stats = AmmoDefinition.SCI_Stats;
            CustomVisualEffects = effects;

            TimeToArm = AmmoDefinition.TimeToArm;

            if (BWT_Stats != null)
            {
                GL_Stats = null;

                IMyLargeTurretBase launcher = MyAPIGateway.Entities.GetEntityById(missile.LauncherId) as IMyLargeTurretBase;

                float distance;

                if (PD_Stats != null && launcher != null && launcher.Target != null)
                {
                    distance = Math.Min(missileAmmoDefinition.MaxTrajectory * ((MyWeaponDefinition)missile.WeaponDefinition).RangeMultiplier, Vector3.Distance(CurrentPosition, launcher.Target.PositionComp.GetPosition()));
                }
                else
                {
                    distance = missileAmmoDefinition.MaxTrajectory * ((MyWeaponDefinition)missile.WeaponDefinition).RangeMultiplier;
                }

                BeamDefinition def = new BeamDefinition
                {
                    MaxTrajectory = distance,

                    MaxReflectChance = missileAmmoDefinition.MissileMaxRicochetProbability,
                    MinReflectChance = missileAmmoDefinition.MissileMinRicochetProbability,
                    MaxReflectAngle = missileAmmoDefinition.MissileMaxRicochetAngle,
                    MinReflectAngle = missileAmmoDefinition.MissileMinRicochetAngle,
                    ReflectDamage = missileAmmoDefinition.MissileRicochetDamage,

                    PlayerDamage = -1,
                    PenetrationDamage = missile.HealthPool * ((MyWeaponDefinition)missile.WeaponDefinition).DamageMultiplier,
                    ExplosiveDamage = missileAmmoDefinition.MissileExplosionDamage * ((MyWeaponDefinition)missile.WeaponDefinition).DamageMultiplier,
                    ExplosiveRadius = missileAmmoDefinition.MissileExplosionRadius,

                    ExplosionFlags = missileAmmoDefinition.ExplosionFlags.HasValue ? (int)missileAmmoDefinition.ExplosionFlags : 1006,
                    EndOfLifeEffect = missileAmmoDefinition.EndOfLifeEffect,
                    EndOfLifeSound = missileAmmoDefinition.EndOfLifeSound,

                    BWT_Stats = BWT_Stats.Value,
                    PD_Stats = PD_Stats,
                    EMP_Stats = EMP_Stats,
                    JDI_Stats = JDI_Stats,
                    SCI_Stats = SCI_Stats
                };

                IMyEntity ent = MyAPIGateway.Entities.GetEntityById(missile.LauncherId);

                Vector3? vel;
                if (ent is IMySlimBlock)
                {
                    vel = (ent as IMySlimBlock).CubeGrid.LinearVelocity;
                }
                else if (ent is IMyCubeBlock)
                {
                    vel = (ent as IMyCubeBlock).CubeGrid.LinearVelocity;
                }
                else
                {
                    vel = ent?.Physics?.LinearVelocity;
                }
                Vector3 velocity = Vector3.Zero;
                if (vel != null)
                {
                    velocity = vel.Value;
                }

                Beams.Beams.GenerateBeam(CurrentPosition, missile.WorldMatrix.Forward, velocity, ref def, missile);

                if (missile != null && !missile.MarkedForClose)
                {
                    missile.Close();
                    missile.LinearVelocity = Vector3.Zero;
                    missile.SetPosition(Vector3.Zero);
                }
            }

            if (GL_Stats != null)
            {
                if (GL_Stats.Value.GL_HomingPiecewisePolynomialFunction != null)
                {
                    if (GL_Stats.Value.GL_HomingPiecewisePolynomialFunction.Count == 0)
                    {
                        GL_Stats = new GuidanceLock_Logic
                        {
                            GL_HomingPiecewisePolynomialFunction = null,
                            GL_DecoyPercentChanceToCauseRetarget = GL_Stats.Value.GL_DecoyPercentChanceToCauseRetarget,
                            GL_DecoyRetargetRadius = GL_Stats.Value.GL_DecoyRetargetRadius
                        };
                    }
                }

                GL_Target = GL_GetTarget();
                SyncTarget = true;

                if (GL_Target != null)
                {
                    GL_PrevTargetPos = GL_Target.GetPosition();

                    MatrixD WorldMatrix = missile.WorldMatrix;
                    GL_Direction = WorldMatrix.Forward;

                    GL_PrevLeadPos = GL_Target.GetPosition();
                    GL_UpdateDesiredDirection(GL_TargetPrediction());

                    UpdateVelocity();
                }
            }


            if (AmmoDefinition.NeedsAPHEFix && missile.ExplosionDamage != 0 && missile.HealthPool != 0)
            {
                NeedsAPHEFix = true;

                APHE_ExplosionDamage = missile.ExplosionDamage;
                APHE_ExplosionRadius = missileAmmoDefinition.MissileExplosionRadius;
                missile.ExplosionDamage = 0;
            }


            if (TimeToArm > 0)
            {
                IsDisarmed = true;
                missile.ExplosionDamage = 0;
                missile.HealthPool = 0;
                APHE_ExplosionDamage = 0;
                APHE_ExplosionRadius = 0;
            }


            if (ProxyId == -1)
            {
                BoundingSphere sphere = new BoundingSphere(missile.PositionComp.GetPosition(), 1.0f);
                BoundingBox result;
                BoundingBox.CreateFromSphere(ref sphere, out result);
                ProxyId = MissileLogic.MissileTree.AddProxy(ref result, this, 0u);
            }

            LastWorldMatrix = missile.WorldMatrix;
        }


        #endregion

        #region Update & General Functions

        private bool IsValidTarget(List<long> Owners)
        {
            IMyLargeTurretBase launcher = MyAPIGateway.Entities.GetEntityById(missile.LauncherId) as IMyLargeTurretBase;


            if (launcher == null)
            {
                foreach (long id in Owners)
                {
                    if (!missile.IsCharacterIdFriendly(id))
                        return true;
                }
                return false;
            }

            foreach (long id in Owners)
            {
                if (launcher.OwnerId == id)
                    return false;
            }


            if (launcher.GetValue<bool>("TargetFriends"))
            {
                foreach (long id in Owners)
                {
                    if (launcher.GetUserRelationToOwner(id) == MyRelationsBetweenPlayerAndBlock.Friends)
                        return true;
                }
            }

            if (launcher.TargetNeutrals)
            {
                foreach (long id in Owners)
                {
                    if (launcher.GetUserRelationToOwner(id) == MyRelationsBetweenPlayerAndBlock.Neutral || launcher.GetUserRelationToOwner(id) == MyRelationsBetweenPlayerAndBlock.NoOwnership)
                        return true;
                }
            }

            if (launcher.TargetEnemies)
            {
                foreach (long id in Owners)
                {
                    if (launcher.GetUserRelationToOwner(id) == MyRelationsBetweenPlayerAndBlock.Enemies)
                        return true;
                }
            }
            return false;
        }
        public bool Update()
        {
            IsDamaged = false;
            collided = false;

            if (missile == null || missile.MarkedForClose || missile.Closed || IsClosed)
                return true;

            CurrentPosition = missile.GetPosition();
            MissileVelocity = missile.LinearVelocity;
            MissileForwardDirection = missile.WorldMatrix.Forward;

            if (missileHP <= 0)
            {
                Close();
                return true;
            }

            if (CustomVisualEffects != null && !MyAPIGateway.Utilities.IsDedicated)
            {
                RenderMissileEffects();
            }

            time += ticktime;
            ticks += 1;

            if (IsDisarmed && TimeToArm < time)
            {
                IsDisarmed = false;

                
                missile.HealthPool = missileAmmoDefinition.MissileHealthPool;

                if (NeedsAPHEFix)
                {
                    APHE_ExplosionDamage = missileAmmoDefinition.MissileExplosionDamage;
                    APHE_ExplosionRadius = missileAmmoDefinition.MissileExplosionRadius;
                }
                else
                {
                    missile.ExplosionDamage = missileAmmoDefinition.MissileExplosionDamage;
                }
            }

            if (ticks % 2 == 0)
                Update2();
            if (ticks % 5 == 0)
                Update5();
            if (ticks % 10 == 0)
                Update10();


            if (PD_Stats != null)
            {

                if (PD_Stats.Value.PD_DetonationRadius > 0)
                {
                    PD_DetectHostiles(PD_Stats.Value.PD_AntiMissileDamage > 0);
                }
            }
            
            if (GL_Stats != null && (GL_Target != null || GL_GPSTargetPos != null))
            {
                UpdateVelocity();
                UpdateDirection();
            }

            LastWorldMatrix = missile.WorldMatrix;
            return false;
        }

        private void RenderMissileEffects()
        {
            MatrixD renderMatrix = missile.WorldMatrix;

            renderMatrix.Forward = MissileVelocity.Normalized();

            foreach (SimpleObjectDefinition def in CustomVisualEffects.DrawnObjects)
            {
                if (def.TicksPerSpawn == 0) def.TicksPerSpawn = 1;
                if (ticks % (def.TicksPerSpawn) != 0) continue;

                if (def.TimeRendered > 1)
                {
                    if (def is LineDefinition)
                    {
                        FXRenderer.ObjectsToRender.Add(new LineRenderer(def as LineDefinition, renderMatrix, MissileVelocity));
                    }
                    else if (def is SphereDefinition)
                    {
                        FXRenderer.ObjectsToRender.Add(new SphereRenderer(def as SphereDefinition, renderMatrix, MissileVelocity));
                    }
                    else if (def is TrailDefinition)
                    {
                        FXRenderer.ObjectsToRender.Add(new LineRenderer(def as TrailDefinition, renderMatrix, MissileVelocity, LastWorldMatrix));
                    }
                }
                else
                {
                    if (def is LineDefinition)
                    {
                        MySimpleObjectDraw.DrawLine(Vector3D.Transform(def.Pos1, renderMatrix), Vector3D.Transform((def as LineDefinition).Pos2, renderMatrix), def.Material, ref def.Color, def.Thickness, def.BlendType);
                    }
                    else if (def is SphereDefinition)
                    {
                        Color c = def.Color;

                        Vector3D v = Vector3D.Transform(def.Pos1, renderMatrix);

                        MatrixD m = renderMatrix;
                        m.Translation = v;

                        MySimpleObjectDraw.DrawTransparentSphere(ref m, (def as SphereDefinition).Radius, ref c, (def as SphereDefinition).Rasterizer,
                            (def as SphereDefinition).wireDivideRatio, def.Material, def.Material, (def as SphereDefinition).Thickness, -1, null, def.BlendType);
                    }
                    else if (def is TrailDefinition)
                    {
                        MySimpleObjectDraw.DrawLine(Vector3D.Transform(def.Pos1, LastWorldMatrix), Vector3D.Transform(def.Pos1, renderMatrix), def.Material, ref def.Color, def.Thickness, def.BlendType);
                    }
                }
            }
        }

        public int GetTicks()
        {
            return ticks;
        }

        private void Update2()
        {
            if (GL_Stats != null)
            {
                if (GL_Target != null)
                    GL_UpdateDesiredDirection(GL_TargetPrediction());
                else if (GL_GPSTargetPos != null)
                    GL_UpdateDesiredDirection(GL_GPSTargetPos.Value);
            }
        }

        private void Update5()
        {

        }

        private void Update10()
        {
            if (GL_Stats != null && GL_Stats.Value.GL_HomingPiecewisePolynomialFunction != null && MyAPIGateway.Multiplayer.IsServer)
                GL_RetargetOntoFlare();
        }

        private void UpdateVelocity()
        {
            if (GL_Direction == null || GL_Direction.X == float.NaN)
            {
                GL_Direction = missile.GetPosition() - GL_Target.GetPosition();
                GL_Direction.Normalize();
            }

            float DesiredSpeed = Math.Min(missileAmmoDefinition.MissileInitialSpeed + missileAmmoDefinition.MissileAcceleration * time, missileAmmoDefinition.DesiredSpeed);
            MissileVelocity = GL_Direction * DesiredSpeed;

            missile.LinearVelocity = MissileVelocity;
        }

        private void UpdateDirection()
        {
            MatrixD WorldMatrix = missile.WorldMatrix;
            WorldMatrix.Forward = GL_Direction;
            missile.WorldMatrix = WorldMatrix;
        }

        public void Close()
        {
            if (IsClosed)
                return;
            IsClosed = true;
            missile.LinearVelocity = Vector3.Zero;

            if (ProxyId != -1)
            {
                MissileLogic.MissileTree.RemoveProxy(ProxyId);
                ProxyId = -1;
            }

            if (EMP_Stats != null && missile != null && MyAPIGateway.Multiplayer.IsServer)
                FrameworkUtilities.EMP(CurrentPosition, EMP_Stats.Value.EMP_Radius, EMP_Stats.Value.EMP_TimeDisabled);
            (MyEntities.GetEntityById(missile.LauncherId) as IMyMissileGunObject)?.RemoveMissile(missile.EntityId); // needs more funny testing required

            missile.Close();
        }

        public void APHE_DetonateMissile(MyEntity HitEntity)
        {
            BoundingSphereD sphere = new BoundingSphereD(missile.GetPosition(), APHE_ExplosionRadius);

            CreateExplosion(missile, sphere, APHE_ExplosionDamage, false, HitEntity);
        }

        private void CreateExplosion(IMyMissile Missile, BoundingSphereD sphere, float damage, bool doExplosionFX, MyEntity HitEntity = null)
        {
            
            MyEntity OwningEntity = (MyEntity)MyAPIGateway.Entities.GetEntityById(missile.LauncherId);

            if (HitEntity == null)
            {
                List<IMyEntity> entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
                entities.AddList(MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere));

                for (int i = 0; i < entities.Count; i++)
                {
                    if (entities[i] is IMyCubeGrid)
                    {
                        HitEntity = (MyEntity)entities[0];
                        break;
                    }
                }

                if (HitEntity == null && entities.Count >= 1)
                {
                    HitEntity = (MyEntity)entities[0];
                }
            }

            float interference;
            var gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(sphere.Center, out interference);

            if (gravity.LengthSquared() == 0)
                MyAPIGateway.Physics.CalculateArtificialGravityAt(sphere.Center, interference);

            Vector3D direction = gravity.LengthSquared() > 0 ? Vector3D.Normalize(-gravity) : Missile.WorldMatrix.Backward;


            MyExplosionInfo explosionInfo = new MyExplosionInfo
            {
                PlayerDamage = damage,
                Damage = damage,
                ExplosionType = (missileAmmoDefinition.EndOfLifeEffect == null && missileAmmoDefinition.EndOfLifeSound == null) ? MyExplosionTypeEnum.MISSILE_EXPLOSION : MyExplosionTypeEnum.CUSTOM,
                ExplosionSphere = sphere,
                StrengthImpulse = 0,
                LifespanMiliseconds = 700,
                HitEntity = HitEntity,
                ParticleScale = (float)sphere.Radius / 12f,
                OwnerEntity = (MyEntity)Missile,
                CustomEffect = doExplosionFX ? missileAmmoDefinition.EndOfLifeEffect : "NullParticle",
                CustomSound = doExplosionFX ? missileAmmoDefinition.EndOfLifeSound : new MySoundPair("NullSound"),
                ShouldDetonateAmmo = true,
                ExcludedEntity = OwningEntity is IMyCubeBlock ? (MyEntity)(OwningEntity as IMyCubeBlock).CubeGrid : OwningEntity,
                EffectHitAngle = MyObjectBuilder_MaterialPropertiesDefinition.EffectHitAngle.DeflectUp,
                Direction = (Missile.PositionComp.GetPosition() - missile.Origin).Normalized(),
                DirectionNormal = direction,
                VoxelExplosionCenter = sphere.Center,
                Velocity = Vector3.Zero,

                VoxelCutoutScale = 0.3f,
                PlaySound = true,
                ApplyForceAndDamage = true,
                OriginEntity = Missile.EntityId,
                KeepAffectedBlocks = true,
                IgnoreFriendlyFireSetting = false,
                CheckIntersections = false,
            };

            explosionInfo.ExplosionFlags = missileAmmoDefinition.ExplosionFlags.HasValue ? missileAmmoDefinition.ExplosionFlags.Value : FrameworkUtilities.CastHax(explosionInfo.ExplosionFlags, 1006); // probably fixed tmr by keen but idc

            MyExplosions.AddExplosion(ref explosionInfo);
        }

        

        public void DetonateSelf(float damage, bool doExplosionFX)
        {
            DetonateMissile(missile, CurrentPosition, missileAmmoDefinition.MissileExplosionRadius, damage, doExplosionFX);
        }
        private void DetonateMissile(IMyMissile missile, Vector3D CurrentPosition, float radius, float damage, bool doExplosionFX, MyEntity HitEntity = null)
        {

            BoundingSphereD sphere = new BoundingSphereD(CurrentPosition, radius);

            CreateExplosion(missile, sphere, damage, doExplosionFX, HitEntity);
            Close();
        }
        #endregion

        #region Proximity Detonation Functions
        private void PD_DetectHostiles(bool detectMissiles)
        {
            BoundingSphereD sphere = new BoundingSphereD(CurrentPosition, PD_Stats.Value.PD_DetonationRadius);
            BoundingSphereD behind = new BoundingSphereD(CurrentPosition - (missile.LinearVelocity * 1 / 120), PD_Stats.Value.PD_DetonationRadius);

            bool destroySelf = false;

            List<IMyEntity> detectedEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            detectedEntities.AddList(MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere));



            if (detectedEntities == null)
            {
                detectedEntities = new List<IMyEntity>();
            }

            bool checkInBetween = missile.LinearVelocity.Length() / 60 > PD_Stats.Value.PD_DetonationRadius;

            if (checkInBetween)
            {
                detectedEntities.AddList(MyAPIGateway.Entities.GetEntitiesInSphere(ref behind));
                detectedEntities.AddList(MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref behind));
            }


            IMyEntity HitGrid = CheckCubegridsInList(sphere, ref destroySelf, detectedEntities);

            if (detectMissiles)
            {
                CheckMissilesInSphere(sphere, ref destroySelf);

                if (checkInBetween)
                {
                    CheckMissilesInSphere(behind, ref destroySelf);
                }
            }


            if (destroySelf)
            {
                DetonateMissile(missile, CurrentPosition, missileAmmoDefinition.MissileExplosionRadius, missile.ExplosionDamage, true, (MyEntity)HitGrid);
            }

            detectedEntities.Clear();
        }

        private void CheckMissilesInSphere(BoundingSphere sphere, ref bool destroySelf)
        {
            MissileLogic.MissileTree.OverlapAllBoundingSphere(ref sphere, detectedMissiles, false);

            foreach (VPFMissile VPFMissileObject in detectedMissiles)
            {
                if (VPFMissileObject.missile != null && !VPFMissileObject.missile.MarkedForClose && !VPFMissileObject.missile.Closed && 
                    Vector3.DistanceSquared(VPFMissileObject.missile.GetPosition(), CurrentPosition) <= PD_Stats.Value.PD_DetonationRadius * PD_Stats.Value.PD_DetonationRadius * 2 
                    && IsValidTarget(new List<long>() { VPFMissileObject.missile.Owner }))
                {
                    destroySelf = true;
                    if (!VPFMissileObject.IsDamaged && missile != VPFMissileObject.missile)
                    {
                        VPFMissileObject.IsDamaged = true;
                        VPFMissileObject.missileHP -= PD_Stats.Value.PD_AntiMissileDamage;
                        if (VPFMissileObject.missileHP <= 0)
                            VPFMissileObject.Close();
                    }
                }
            }
            detectedMissiles.Clear();
        }

        private IMyEntity CheckCubegridsInList(BoundingSphereD sphere, ref bool destroySelf, List<IMyEntity> detectedEntities)
        {
            foreach (IMyEntity entity in detectedEntities)
            {
                if (entity == null) continue;

                if (entity is IMyCubeGrid)
                {
                    IMyCubeGrid gridNearby = (IMyCubeGrid)entity;
                    List<long> majorityOwners = new List<long>(gridNearby.BigOwners);



                    if (IsValidTarget(majorityOwners))
                    {
                        List<IMySlimBlock> blocks = gridNearby.GetBlocksInsideSphere(ref sphere);

                        if (blocks.Count > 0)
                        {
                            destroySelf = true;
                            majorityOwners.Clear();
                            return entity;
                        }
                    }
                    majorityOwners.Clear();
                }
                //else if (entity is IMyCharacter)
                //{
                //    IMyCharacter character = entity as IMyCharacter;

                //    if (!character.IsDead && IsValidTarget(new List<long>() { character.EntityId }))
                //    {
                //        destroySelf = true;

                //        return;
                //    }
                //}
            }
            return null;
        }

        #endregion

        #region Missile Guidance Functions

        /// <summary>
        /// parse in blocks a new List<IMySlimBlock>
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="blocks"></param>
        /// <returns></returns>
        public void GetAllBlocks(IMyCubeGrid grid, List<IMySlimBlock> blocks)
        {
            if (grid  == null) return;

            List<IMySlimBlock> blocks2 = new List<IMySlimBlock>();
            grid.GetBlocks(blocks2);

            blocks.AddList(blocks2);

            foreach (IMyPistonBase subgrid in grid.GetFatBlocks<IMyPistonBase>())
            {
                if (subgrid.Top?.SlimBlock != null)
                {
                    if (!blocks.Contains(subgrid.Top.SlimBlock))
                    {
                        GetAllBlocks(subgrid.Top.CubeGrid, blocks);
                    }
                }
            }
            foreach (IMyPistonTop subgrid in grid.GetFatBlocks<IMyPistonTop>())
            {
                if (subgrid.Base?.SlimBlock != null)
                {
                    if (!blocks.Contains(subgrid.Base.SlimBlock))
                    {
                        GetAllBlocks(subgrid.Base.CubeGrid, blocks);
                    }
                }
            }
            foreach (IMyMotorBase subgrid in grid.GetFatBlocks<IMyMotorBase>())
            {
                if (subgrid.Top?.SlimBlock != null)
                {
                    if (!blocks.Contains(subgrid.Top.SlimBlock))
                    {
                        GetAllBlocks(subgrid.Top.CubeGrid, blocks);
                    }
                }
            }

            foreach (IMyMotorAdvancedRotor subgrid in grid.GetFatBlocks<IMyMotorAdvancedRotor>())
            {
                if (subgrid.Base?.SlimBlock != null)
                {
                    if (!blocks.Contains(subgrid.Base.SlimBlock))
                    {
                        GetAllBlocks(subgrid.Base.CubeGrid, blocks);
                    }
                }
            }

            foreach (IMyMotorRotor subgrid in grid.GetFatBlocks<IMyMotorRotor>())
            {
                if (subgrid.Base?.SlimBlock != null)
                {
                    if (!blocks.Contains(subgrid.Base.SlimBlock))
                    {
                        GetAllBlocks(subgrid.Base.CubeGrid, blocks);
                    }
                }
            }
        }

        private IMyEntity GL_GetTarget()
        {
            if (GL_Stats.Value.GL_HomingPiecewisePolynomialFunction == null)
            {
                return null;
            }
            else if (GL_Stats.Value.GL_HomingPiecewisePolynomialFunction.Count == 0)
            {
                return null;
            }

            IMyEntity Target = null;

            if (Target == null && GL_Stats.Value.GL_AllowedGuidanceTypes.HasFlag(GuidanceType.TurretTarget))
            {
                IMyEntity MissileLauncher = MyAPIGateway.Entities.GetEntityById(missile.LauncherId);
                if (MissileLauncher != null && MissileLauncher is IMyLargeTurretBase)
                {
                    IMyLargeTurretBase LargeTurretLauncher = (IMyLargeTurretBase)MissileLauncher;


                    if (LargeTurretLauncher.HasTarget)
                    {
                        Target = LargeTurretLauncher.Target;


                    }
                }
            }

            
            if (Target == null && GL_Stats.Value.GL_AllowedGuidanceTypes.HasFlag(GuidanceType.LockOn))
            {
                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                GetAllBlocks((MyAPIGateway.Entities.GetEntityById(missile.LauncherId) as IMyCubeBlock)?.CubeGrid, blocks);
                if (blocks != null)
                {
                    List<IMyDefensiveCombatBlock> defensiveCombatBlocks = new List<IMyDefensiveCombatBlock>();
                    foreach (IMySlimBlock b in blocks)
                    {
                        if (b.FatBlock != null && b.FatBlock is IMyDefensiveCombatBlock)
                            defensiveCombatBlocks.Add(b.FatBlock as IMyDefensiveCombatBlock);
                    }

                    foreach (IMyDefensiveCombatBlock Block in defensiveCombatBlocks)
                    {
                        Target = Block.Components?.Get<MyTargetLockingBlockComponent>()?.TargetEntity;
                        if (Target != null)
                            break;
                    }

                    List<IMyOffensiveCombatBlock> offensiveCombatBlocks = new List<IMyOffensiveCombatBlock>();
                    foreach (IMySlimBlock b in blocks)
                    {
                        if (b.FatBlock != null && b.FatBlock is IMyOffensiveCombatBlock)
                            offensiveCombatBlocks.Add(b.FatBlock as IMyOffensiveCombatBlock);
                    }

                    foreach (IMyOffensiveCombatBlock Block in offensiveCombatBlocks)
                    {
                        Target = Block.Components?.Get<MyTargetLockingBlockComponent>()?.TargetEntity;
                        if (Target != null)
                            break;
                    }
                }
            }

            List<IMyPlayer> Players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(Players);

            IMyPlayer Owner = null;

            foreach (IMyPlayer player in Players)
            {
                if (player.IdentityId == missile.Owner || player.Controller?.ControlledEntity?.Entity?.EntityId == missile.LauncherId)
                {
                    Owner = player;
                    break;
                }
            }

            if (Owner != null && Target == null && GL_Stats.Value.GL_AllowedGuidanceTypes.HasFlag(GuidanceType.LockOn))
            {
                MyTargetLockingComponent comp = Owner.Character?.Components?.Get<MyTargetLockingComponent>();
                if (comp != null && comp.TargetEntity != null && comp.LockingTimeRemainingMilliseconds == 0)
                {
                    Target = comp.TargetEntity;
                }

            }

            //if (Target == null && GL_Stats.Value.GL_AllowedGuidanceTypes.HasFlag(GuidanceType.DesignatedPosition))
            //{
            //    IMyCubeBlock block = MyAPIGateway.Entities.GetEntityById(missile.LauncherId) as IMyCubeBlock;
            //    IMyCubeGrid LaunchingGrid = block.CubeGrid;

            //    if (LaunchingGrid != )
            //}

            if (Target == null && GL_Stats.Value.GL_AllowedGuidanceTypes.HasFlag(GuidanceType.OneTimeRaycast))
            {
                List<IHitInfo> HitEntities = new List<IHitInfo>();

                Matrix Matrix1 = Matrix.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(missile.WorldMatrix.Up, 0.00872665f));
                Matrix Matrix2 = Matrix.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(missile.WorldMatrix.Up, -0.00872665f));
                Matrix Matrix3 = Matrix.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(missile.WorldMatrix.Left, 0.00872665f));
                Matrix Matrix4 = Matrix.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(missile.WorldMatrix.Left, -0.00872665f));

                Vector3 _Vector1 = Vector3.Transform(MissileForwardDirection, Matrix1);
                Vector3 _Vector2 = Vector3.Transform(MissileForwardDirection, Matrix2);
                Vector3 _Vector3 = Vector3.Transform(MissileForwardDirection, Matrix3);
                Vector3 _Vector4 = Vector3.Transform(MissileForwardDirection, Matrix4);

                _Vector1.Normalize();
                _Vector2.Normalize();
                _Vector3.Normalize();
                _Vector4.Normalize();

                MyAPIGateway.Physics.CastRay(CurrentPosition, CurrentPosition + MissileForwardDirection * missileAmmoDefinition.MaxTrajectory, HitEntities);
                MyAPIGateway.Physics.CastRay(CurrentPosition, CurrentPosition + _Vector1 * missileAmmoDefinition.MaxTrajectory, HitEntities);
                MyAPIGateway.Physics.CastRay(CurrentPosition, CurrentPosition + _Vector2 * missileAmmoDefinition.MaxTrajectory, HitEntities);
                MyAPIGateway.Physics.CastRay(CurrentPosition, CurrentPosition + _Vector3 * missileAmmoDefinition.MaxTrajectory, HitEntities);
                MyAPIGateway.Physics.CastRay(CurrentPosition, CurrentPosition + _Vector4 * missileAmmoDefinition.MaxTrajectory, HitEntities);

                double dist = double.MaxValue;
                IHitInfo ClosestHitCubeGrid = null;

                foreach (IHitInfo HitEntity in HitEntities)
                {
                    if ((HitEntity.HitEntity is IMyCubeGrid) || (HitEntity.HitEntity is IMyPlayer) && (Vector3.DistanceSquared(HitEntity.HitEntity.GetPosition(), CurrentPosition) < dist))
                    {
                        dist = Vector3.DistanceSquared(HitEntity.HitEntity.GetPosition(), CurrentPosition);
                        ClosestHitCubeGrid = HitEntity;
                    }
                }

                if (ClosestHitCubeGrid != null)
                {
                    Target = ClosestHitCubeGrid.HitEntity;
                }
            }



            IMyCubeGrid TargetGrid;
            IMyEntity TargetBlock = null;
            if (Target is IMyCubeGrid)
            {
                TargetGrid = (IMyCubeGrid)Target;
            }
            else if (Target is IMySlimBlock)
            {
                TargetGrid = (Target as IMySlimBlock).CubeGrid;
            }
            else if (Target is IMyCubeBlock)
            {
                TargetGrid = (Target as IMyCubeBlock).CubeGrid;
            }
            else
            {
                return Target;
            }

            if (TargetGrid != null)
            {

                while (TargetGrid.Parent != null && TargetGrid.Parent is IMyCubeGrid)
                {
                    TargetGrid = (IMyCubeGrid)TargetGrid.Parent;
                }
                if (TargetGrid.GetFatBlocks<IMyTerminalBlock>().Count() > 0)
                {
                    TargetBlock = TargetGrid.GetFatBlocks<IMyTerminalBlock>().ElementAt((int)MissileLogic.Random.GetRandomFloat(0,
                        TargetGrid.GetFatBlocks<IMyTerminalBlock>().Count()));

                }
            }

            return TargetBlock;
        }

        private void GL_RetargetOntoFlare()
        {
            if (GL_Target is IMyMissile || (GL_Target == null && GL_GPSTargetPos == null) || missile == null || MaximumRetargetRadius <= 0)
                return;



            BoundingSphere FlareDetectionSphere = new BoundingSphere(GL_Target.GetPosition(), MaximumRetargetRadius);

            List<VPFMissile> missiles = new List<VPFMissile>();
            MissileLogic.MissileTree.OverlapAllBoundingSphere(ref FlareDetectionSphere, missiles);

            foreach (VPFMissile m in missiles)
            {
                if (m.GL_Stats == null)
                    continue;

                if (Vector3.DistanceSquared(m.missile.GetPosition(), GL_Target.GetPosition()) <= m.GL_Stats.Value.GL_DecoyRetargetRadius * m.GL_Stats.Value.GL_DecoyRetargetRadius)
                {
                    float RandomFloat = MissileLogic.Random.GetRandomFloat(0, 100);

                    if (RandomFloat < m.GL_Stats.Value.GL_DecoyPercentChanceToCauseRetarget)
                    {
                        GL_Target = m.missile;
                        SyncTarget = true;
                        return;
                    }
                }
            }
        }


        private Vector3 GL_TargetPrediction()
        {
            if (GL_Target.MarkedForClose)
            {
                if (GL_Target is IMyTerminalBlock)
                {
                    IMyCubeGrid retargetGrid = GL_Target.Parent as IMyCubeGrid;

                    if (retargetGrid != null && retargetGrid.GetFatBlocks<IMyTerminalBlock>().Count() > 0)
                    {
                        GL_Target = retargetGrid.GetFatBlocks<IMyTerminalBlock>().ElementAt(MyUtils.GetRandomInt(0, retargetGrid.GetFatBlocks<IMyTerminalBlock>().Count()));
                        SyncTarget = true;
                    }
                    else
                        return GL_PrevLeadPos;
                }
                else
                    return GL_PrevLeadPos;
            }

            Vector3 TargetPosition = GL_Target.GetPosition();
            Vector3 TargetVelocity = Vector3.Zero;


            MyPhysicsComponentBase targetPhysics = null;

            if (GL_Target is IMyFunctionalBlock)
            {
                if (GL_Target.Physics != null)
                    targetPhysics = GL_Target.Physics;

                if (targetPhysics == null && GL_Target.Parent != null && GL_Target.Parent.Physics != null)
                    targetPhysics = GL_Target.Parent.Physics;

                if (targetPhysics != null)
                {
                    TargetVelocity = targetPhysics.LinearVelocity;
                }
            }
            else if (GL_Target is IMyMissile)
            {
                IMyMissile target = GL_Target as IMyMissile;
                TargetVelocity = target.LinearVelocity;
            }
            else
            {
                TargetVelocity = (TargetPosition - GL_PrevTargetPos) * 60;
            }

            Vector3 RelativePosition = TargetPosition - CurrentPosition;
            Vector3 RelativeVelocity = TargetVelocity - MissileVelocity;

            float TimeToIntercept = RelativePosition.Length() / RelativeVelocity.Length();

            Vector3 LeadPosition = TargetPosition + TargetVelocity * TimeToIntercept;

            GL_PrevLeadPos = LeadPosition;

            GL_PrevTargetPos = GL_Target.GetPosition();

            return LeadPosition;
        }

        private void GL_UpdateDesiredDirection(Vector3 TargetPositon)
        {
            Vector3 DesiredDirection = TargetPositon - CurrentPosition;
            DesiredDirection.Normalize();

            double AngleDifference = MyMath.AngleBetween(GL_Direction, DesiredDirection);

            Vector3 RotAxis = Vector3.Cross(GL_Direction, DesiredDirection);
            RotAxis.Normalize();

            if (!GL_Target.MarkedForClose && (GL_Target.Parent != null || GL_Target is IMyMissile))
            {
                double AimSpeed = DefinitionTools.FunctionOutput(time, GL_Stats.Value.GL_HomingPiecewisePolynomialFunction) * 0.000581776417;

                Matrix RotationMatrix = Matrix.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(RotAxis, (float)Math.Max(Math.Min(AngleDifference, AimSpeed), -AimSpeed)));
                GL_Direction = Vector3.Transform(GL_Direction, RotationMatrix);
                GL_Direction.Normalize();
            }
        }
        #endregion
    }
}
