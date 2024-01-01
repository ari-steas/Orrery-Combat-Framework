using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VanillaPlusFramework.Utilities;
using VanillaPlusFramework.TemplateClasses;
using VRage.Game.Components;
using VanillaPlusFramework.Missiles;
using VRage.Utils;
using VanillaPlusFramework.Networking;
using VanillaPlusFramework.Turrets;
using VanillaPlusFramework.FX;
using Sandbox.Game;
using System.Security.Principal;
using VRage.Game.Entity;
using VRage.Noise.Patterns;
using Ingame = VRage.Game.ModAPI.Ingame;
using Sandbox.Game.Entities;
using VRage.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game;
using VRageMath;
using Sandbox.Game.Weapons;
using VRage;
using System.Linq;

namespace VanillaPlusFramework
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation)]
    public class VanillaPlusFramework : MySessionComponentBase
    {
        public static Dictionary<long, DisabledBlock> DisabledBlocks = new Dictionary<long, DisabledBlock>();
                             
        public override void LoadData()
        {
            MyAPIUtilities.Static.RegisterMessageHandler(DefinitionTools.ModMessageID, OnModMessageRecieved);
            MyExplosions.OnExplosion += OnExplosion;
            CommunicationTools.Load();
        }


        private void OnExplosion(ref MyExplosionInfo explosionInfo)
        {
            if (explosionInfo.HitEntity == null)
            {
                List<MyEntity> entities = new List<MyEntity>();
                MyGamePruningStructure.GetAllEntitiesInSphere(ref explosionInfo.ExplosionSphere, entities);
                for (int i = 0; i < entities.Count; i++)
                {
                    if (entities[i] is IMyCubeGrid)
                    {
                        explosionInfo.HitEntity = entities[i];
                        break;
                    }
                    else if (entities[i] is IMyDestroyableObject)
                    {
                        explosionInfo.HitEntity = entities[i];
                        break;
                    }
                }

                if (explosionInfo.HitEntity == null && entities.Count >= 1)
                {
                    explosionInfo.HitEntity = entities[0];
                }
            }

            /*
            if (explosionInfo.ExplosionType == MyExplosionTypeEnum.ProjectileExplosion)
            {
                explosionInfo.ExplosionType = MyExplosionTypeEnum.MISSILE_EXPLOSION;
                explosionInfo.StrengthImpulse = 0;
                explosionInfo.EffectHitAngle = MyObjectBuilder_MaterialPropertiesDefinition.EffectHitAngle.DeflectUp;
                explosionInfo.PlayerDamage = 0;
                explosionInfo.ExcludedEntity = explosionInfo.OwnerEntity is IMyCubeBlock ? (MyEntity)(explosionInfo.OwnerEntity as IMyCubeBlock).CubeGrid : explosionInfo.OwnerEntity;
                //explosionInfo.ExplosionSphere.Center = explosionInfo.ExplosionSphere.Center - *explosionInfo.Direction.Value;

                float interference;
                var gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(explosionInfo.ExplosionSphere.Center, out interference);

                if (gravity.LengthSquared() == 0)
                    MyAPIGateway.Physics.CalculateArtificialGravityAt(explosionInfo.ExplosionSphere.Center, interference);

                Vector3D direction = gravity.LengthSquared() > 0 ? Vector3D.Normalize(-gravity) : (explosionInfo.VoxelExplosionCenter - explosionInfo.ExplosionSphere.Center).Normalized();

                explosionInfo.DirectionNormal = direction;


                explosionInfo.ExplosionSphere.Center += -0.5f * explosionInfo.Direction.Value;
            }*/

            if (explosionInfo.HitEntity is IMyCubeBlock)
            {
                explosionInfo.HitEntity = (MyEntity)(explosionInfo.HitEntity as IMyCubeBlock).CubeGrid;
            }
            else if (explosionInfo.HitEntity is IMySlimBlock)
            {
                explosionInfo.HitEntity = (MyEntity)(explosionInfo.HitEntity as IMySlimBlock).CubeGrid;
            }



            explosionInfo.KeepAffectedBlocks = true;
            //explosionInfo.CheckIntersections = false;
        }

        protected override void UnloadData()
        {
            MyAPIUtilities.Static.UnregisterMessageHandler(DefinitionTools.ModMessageID, OnModMessageRecieved);
            MyExplosions.OnExplosion -= OnExplosion;
            CommunicationTools.Unload();
        }

        private void OnModMessageRecieved(object message)
        {
            VPFDefinition def = null;

            try
            {
                def = MyAPIGateway.Utilities.SerializeFromBinary<VPFDefinition>(message as byte[]);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("Error occured during deserialization. The mod that was loaded may need to update their side of Vanilla+ Framework.");
                MyLog.Default.Error(e.ToString());
            }

            if (def != null)
            {
                if (def is VPFAmmoDefinition)
                {
                    MissileLogic.OnDefinitionRecieved(def as VPFAmmoDefinition);
                }
                else if (def is VPFTurretDefinition)
                {
                    TurretLogic.OnDefinitionRecieved(def as VPFTurretDefinition);
                }
                else if (def is VPFVisualEffectsDefinition)
                {
                    FXRenderer.OnDefinitionRecieved(def as VPFVisualEffectsDefinition);
                }
            }
        }
    }
}
