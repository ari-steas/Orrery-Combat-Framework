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
using System.Collections.Concurrent;
using VRage.Serialization;
using VanillaPlusFramework.TemplateClasses;
using static VanillaPlusFramework.TemplateClasses.TargetFlags;
using static VanillaPlusFramework.TemplateClasses.FuelType;
using SpaceEngineers.Game.ModAPI;
using Sandbox.Game.Weapons.Guns;
using Sandbox.Game.Gui;
using Ingame = VRage.Game.ModAPI.Ingame;
using VRage.Input;
using VRage.Game.ModAPI.Network;
using VRage.Sync;
using VanillaPlusFramework.Utilities;
using Sandbox.Game.Entities.Cube;

namespace VanillaPlusFramework.Turrets
{
    public class Turret : MyGameLogicComponent
    {
        public IMyLargeTurretBase self;
        public IMyInventory turretInventory;

        public TurretAI_Logic? TAI_Stats;
        public AmmoGeneration_Logic? AG_Stats;

        private float defaultFuelDraw;
        private bool usingDefaultFuelDraw = true;
        private MyDefinitionId fuelId;
        public float FuelPerSecond;

        public float AG_Capacitor = 0;
        private bool IsCharged = false;
        private float AmmoVolume;
        private bool MakeNoAmmo = false;

        public MyResourceSinkComponent sink;

        Random random = new Random();

        uint ticks = 0;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            bool? IsServer = MyAPIGateway.Session?.IsServer;
            if (IsServer == null)
                return;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            self = Entity as IMyLargeTurretBase;

            if (self?.CubeGrid?.Physics == null)
                return;

            foreach (var def in TurretLogic.Definitions)
            {
                if (def.subtypeName == self.BlockDefinition.SubtypeName)
                {
                    TAI_Stats = def.TAI_Stats;
                    AG_Stats = def.AG_Stats;
                    break;
                }
            }

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
            sink = self.Components.Get<MyResourceSinkComponent>();
            turretInventory = self.GetInventory(0);
            TurretLogic.Turrets.Add(this);
            
            if (AG_Stats != null)
            {
                defaultFuelDraw = AG_Stats.Value.AG_FuelType == POWER ? sink.MaxRequiredInputByType(fuelId) : 0;

                FuelPerSecond = AG_Stats.Value.AG_AmmoCost * (AG_Stats.Value.AG_FuelType == POWER ? 3600 : 1) / (AG_Stats.Value.AG_GenerationTime);
                if (AG_Stats.Value.AG_AmmoDefinitionName != "")
                {
                    AmmoVolume = MyDefinitionManager.Static.GetAmmoMagazineDefinition(MyDefinitionId.Parse("MyObjectBuilder_AmmoMagazine/" + AG_Stats.Value.AG_AmmoDefinitionName)).Volume;
                }
                else AmmoVolume = -10; // always need input
                AmmoVolume *= AG_Stats.Value.AG_NumberGenerated;
                MakeNoAmmo = AG_Stats.Value.AG_AmmoDefinitionName == "";
                fuelId = EnumToId(AG_Stats.Value.AG_FuelType);

                self.EnabledChanged += EnabledChanged;
            }

            
        }

        public override void OnBeforeRemovedFromContainer()
        {
            if (AG_Stats != null)
                self.EnabledChanged -= EnabledChanged;
        }


        public override void UpdateBeforeSimulation()
        {
            ticks++;
            if (TAI_Stats != null)
            {
                DoTurretAILogic();
            }

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                self.SyncAzimuth();
                self.SyncElevation();

                if (AG_Stats != null && self.Enabled)
                {
                    DoAmmoGenerationLogic();
                }
            }
            else if (MakeNoAmmo && self.Enabled)
            {
                DoAmmoGenerationLogic();
            }
        }
        public override void UpdateBeforeSimulation10()
        {
            
        }

        public void DoTurretAILogic()
        {
            
            
            if (self.HasTarget)
            {
                if (TAI_Stats.Value.TAI_MinimumRange != 0)
                {
                    if (Vector3.DistanceSquared(self.Target.PositionComp.GetPosition(), self.PositionComp.GetPosition()) < TAI_Stats.Value.TAI_MinimumRange * TAI_Stats.Value.TAI_MinimumRange)
                    {
                        RemoveTarget();
                    }
                }
                if (TAI_Stats.Value.TAI_ForceMaximumRange != -1)
                {
                    if (Vector3.DistanceSquared(self.Target.PositionComp.GetPosition(), self.PositionComp.GetPosition()) > TAI_Stats.Value.TAI_ForceMaximumRange * TAI_Stats.Value.TAI_ForceMaximumRange)
                    {
                        RemoveTarget();
                    }
                }

                if (TAI_Stats.Value.TAI_ShootWhenTargetAquired && self.CanActiveToolShoot())
                {
                    self.ShootOnce();
                }
            }
            else if (MyAPIGateway.Multiplayer.IsServer && TAI_Stats.Value.TAI_ResponseTime != 100 && ticks % TAI_Stats.Value.TAI_ResponseTime == 0)
            {
                //AttemptFindNewTarget();
            }

            if (TAI_Stats.Value.TAI_DisableTargetingFlags != None)
            {
                ForceSetTargetOptions(TAI_Stats.Value.TAI_DisableTargetingFlags, false);
            }
            if (TAI_Stats.Value.TAI_ForceEnableTargetingFlags != None)
            {
                ForceSetTargetOptions(TAI_Stats.Value.TAI_ForceEnableTargetingFlags, true);
            }
            
        }

        public bool IsValidTargetFromRelation(MyRelationsBetweenPlayerAndBlock relations)
        {
            if (relations.HasFlag(MyRelationsBetweenPlayerAndBlock.Enemies) && self.TargetEnemies) return true;
            else if (relations.HasFlag(MyRelationsBetweenPlayerAndBlock.Neutral) && self.TargetNeutrals) return true;
            else if (relations.HasFlag(MyRelationsBetweenPlayerAndBlock.Friends) && self.GetValue<bool>("TargetFriends")) return true;
            else return false;
            
        }

        public void AttemptFindNewTarget()
        {
            float min = TAI_Stats.Value.TAI_MinimumRange;
            float max = TAI_Stats.Value.TAI_ForceMaximumRange;

            BoundingSphereD sphere = new BoundingSphereD(self.GetPosition(), max);

            List<MyEntity> result = new List<MyEntity>();

            MyGamePruningStructure.GetAllEntitiesInSphere(ref sphere, result);
            
            for (int i = 0; i < result.Count; i++)
            {
                if (Vector3.DistanceSquared(result[i].PositionComp.GetPosition(), self.GetPosition()) <= min * min)
                {
                    result.Remove(result[i]);
                    continue;
                }

                if (result is IMyMeteor) continue;

                else if (result is IMyMissile)
                {
                    if (!IsValidTargetFromRelation(self.GetUserRelationToOwner((result as IMyMissile).Owner)))
                    {
                        result.Remove(result[i]);
                    }
                }
                else if (result is IMyCubeGrid)
                {
                    if ((result as IMyCubeGrid).BigOwners.Count == 0)
                    {
                        result.Remove(result[i]);
                        continue;
                    }

                    bool remove = true;

                    foreach (var owner in (result as IMyCubeGrid).BigOwners)
                    {
                        if (IsValidTargetFromRelation(self.GetUserRelationToOwner(owner)))
                        {
                            remove = false;
                            break;
                        }
                    }

                    if ((result as IMyCubeGrid).IsStatic && !self.TargetStations) remove = true;
                    else if ((result as IMyCubeGrid).GridSizeEnum == MyCubeSize.Large && !self.TargetLargeGrids) remove = true;
                    else if ((result as IMyCubeGrid).GridSizeEnum == MyCubeSize.Small && !self.TargetSmallGrids) remove = true;

                    if (remove)
                    {
                        result.Remove(result[i]);
                        continue;
                    }
                }
                //else if (result is IMyCharacter)
                //{
                //    if (IsValidTargetFromRelation(self.GetUserRelationToOwner((result as IMyCharacter).EntityId)))
                //}
            }
            
            
            self.TrackTarget(self);
            self.ResetTargetingToDefault();
        }

        public void ForceSetTargetOptions(TargetFlags flags, bool type)
        {
            if (flags.HasFlag(ManualControl))
            {
                if (self.IsUnderControl)
                {
                    self.SetManualAzimuthAndElevation((float)(random.NextDouble() * 6.28), (float)((random.NextDouble() * 6.28) - 3.14));
                }
            }

            if (flags.HasFlag(AIControl))
            {
                if (self.AIEnabled)
                {
                    self.Range = 0;
                    RemoveTarget();
                }
            }

            if (flags.HasFlag(ToggleShooting))
            {
                self.Shoot = type;
            }

            if (flags.HasFlag(MeteorTargeting))
            {
                self.TargetMeteors = type;
            }
            
            if (flags.HasFlag(MissileTargeting))
            {
                self.TargetMissiles = type;
            }

            if (flags.HasFlag(SmallShipTargeting))
            {
                self.TargetSmallGrids = type;
            }

            if (flags.HasFlag(LargeShipTargeting))
            {
                self.TargetLargeGrids = type;
            }

            if (flags.HasFlag(CharacterTargeting))
            {
                self.TargetCharacters = type;
            }

            if (flags.HasFlag(StationTargeting))
            {
                self.TargetStations = type;
            }

            if (flags.HasFlag(FriendlyTargeting))
            {
                if (self.GetValue<bool>("TargetFriends") != type)
                {
                    self.SetValue("TargetFriends", type);
                }
            }

            if (flags.HasFlag(NeutralTargeting))
            {
                if (self.TargetNeutrals != type)
                {
                    self.TargetNeutrals = type;
                }
            }

            if (flags.HasFlag(HostileTargeting))
            {
                if (self.TargetEnemies != type)
                {
                    self.TargetEnemies = type;
                }
            }
        }

        public void RemoveTarget() { self.ResetTargetingToDefault(); }

        private static MyDefinitionId EnumToId(FuelType type)
        {
            switch (type)
            {
                case POWER:
                    return MyResourceDistributorComponent.ElectricityId;
                case HYDROGEN:
                    return MyResourceDistributorComponent.HydrogenId;
                default:
                    return MyResourceDistributorComponent.OxygenId;
            }
        }

        private void EnabledChanged(IMyTerminalBlock obj)
        {
            if (!self.Enabled && AG_Stats != null)
            {
                sink.SetMaxRequiredInputByType(fuelId, 0);
                sink.SetRequiredInputFuncByType(fuelId, () => 0);
                usingDefaultFuelDraw = true;
                sink.Update();
            }
        }

        public void DoAmmoGenerationLogic()
        {
            if (self.Enabled && IsCharged && AmmoVolume + (float)turretInventory.CurrentVolume <= (float)turretInventory.MaxVolume)
            {
                if (MakeNoAmmo)
                {
                    AG_Capacitor = 0;
                    IsCharged = false;
                    return;
                }

                MyObjectBuilder_PhysicalObject item = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_AmmoMagazine>(AG_Stats.Value.AG_AmmoDefinitionName);

                AG_Capacitor = 0;
                IsCharged = false;
                turretInventory.AddItems(AG_Stats.Value.AG_NumberGenerated, item);

                StoreFuel();
            }
            else if (self.Enabled && !IsCharged)
            {
                if (usingDefaultFuelDraw)
                {
                    if (!sink.AcceptedResources.Contains(fuelId))
                    {
                        MyResourceSinkInfo info = new MyResourceSinkInfo()
                        {
                            ResourceTypeId = fuelId,
                            MaxRequiredInput = FuelPerSecond,
                            RequiredInputFunc = () => FuelPerSecond,
                        };
                        sink.AddType(ref info);
                    }
                    sink.SetMaxRequiredInputByType(fuelId, FuelPerSecond);
                    sink.SetRequiredInputFuncByType(fuelId, () => FuelPerSecond);
                    usingDefaultFuelDraw = false;
                    sink.Update();
                }
                StoreFuel();
            }
            else if (!usingDefaultFuelDraw)
            {
                sink.SetMaxRequiredInputByType(fuelId, defaultFuelDraw);
                sink.SetRequiredInputFuncByType(fuelId, () => defaultFuelDraw);
                usingDefaultFuelDraw = true;
                sink.Update();
            }
        }

        private void StoreFuel()
        {
            if (MakeNoAmmo)
            {
                return;
            }

                float Input = sink.CurrentInputByType(fuelId) / (AG_Stats.Value.AG_FuelType == POWER ? 216000 : 60); // 1/216000 is MW to MWh over 1 tick, 1/60 is L to liters over 1 tick
            AG_Capacitor = Math.Min(AG_Capacitor + Input, AG_Stats.Value.AG_AmmoCost);
            
            if (AG_Capacitor == AG_Stats.Value.AG_AmmoCost)
            {
                IsCharged = true;
            }
        }

        public void OnTurretFire()
        {
            
        }

        public override void Close()
        {
            if (TurretLogic.Turrets.Contains(this))
            {
                TurretLogic.Turrets.Remove(this);
            }
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeGatlingTurret), false)]
    public class Turret_Gatling : Turret { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), false)]
    public class Turret_Missile : Turret { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_InteriorTurret), false)]
    public class Turret_Interior : Turret { }
}
