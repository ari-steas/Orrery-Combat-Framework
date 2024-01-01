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
using VRage.Scripting;
using System.Net.Sockets;
using VanillaPlusFramework.TemplateClasses;
using VanillaPlusFramework.Networking;
using VanillaPlusFramework.Utilities;
using VanillaPlusFramework;
using Sandbox.Engine.Multiplayer;
using System.Security.Cryptography;
using VanillaPlusFramework.Beams;
using VRage.Game.ObjectBuilders.Components;
using Sandbox.Game.SessionComponents;
using VanillaPlusFramework.FX;
using VRage.Library.Utils;
using Sandbox.Game.World;
using static VRage.Game.MyObjectBuilder_SessionComponentMission;
using VRage.Serialization;

namespace VanillaPlusFramework.Missiles
{

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation)]
    public class MissileLogic : MySessionComponentBase
    {
        public static List<VPFAmmoDefinition> Definitions = new List<VPFAmmoDefinition>();
        public static Dictionary<long, VPFMissile> Missiles = new Dictionary<long, VPFMissile>();
        public static Dictionary<string, MyTuple<float, double>> RetargetChances = new Dictionary<string, MyTuple<float, double>>();

        public static MyRandom Random = new MyRandom(-210412);

        public static VPFAmmoDefinition DefaultAmmoDefinition = new VPFAmmoDefinition
        {
            VPF_MissileHitpoints = 1,
            subtypeName = null,
            FXsubtypeName = null,

            EMP_Stats = null,
            JDI_Stats = null,
            PD_Stats = null,
            BWT_Stats = null,
            GL_Stats = null,
            SCI_Stats = null,
        };

        public static readonly MyDynamicAABBTree MissileTree = new MyDynamicAABBTree(Vector3.One, 1);

        private bool firstFrame = true;
        private int LastMissilesCount = 0;
        private int ticks = 0;

        public bool DEBUG = false;

        public ulong MultiplayerId;

        #region Netcode
        public static List<Packet> SyncRequest(PacketType type)
        {
            List<Packet> retvals = new List<Packet>();

            if (type == PacketType.Missiles)
            {
                List<long> missiles = new List<long>();
                List<long> targets = new List<long>();

                foreach (KeyValuePair<long, VPFMissile> pair in Missiles)
                {
                    if (pair.Value.GL_Target != null)
                    {
                        missiles.Add(pair.Key);
                        targets.Add(pair.Value.GL_Target?.EntityId ?? 0);
                    }
                }
                if (missiles.Count > 0)
                {
                    retvals.Add(new SyncMissileTarget(missiles.ToArray(), targets.ToArray()));
                }

                return retvals;
            }
            else
            {
                foreach (VPFAmmoDefinition def in Definitions)
                {
                    retvals.Add(new SendDefinition(def, PacketType.Definitions));
                }

                return retvals;
            }
        }

        public static void SyncData(Packet data)
        {
            //MyLog.Default.WriteLine($"Message recieved. Message: {data}");

            if (data is SyncMissileTarget)
            {
                SyncMissileTarget sync = data as SyncMissileTarget;

                if (sync.missileIDs == null) return;


                for (int i = 0; i < sync.missileIDs.Length; i++)
                {
                    VPFMissile missile;

                    if (Missiles.TryGetValue(sync.missileIDs[i], out missile))
                    {
                        if (sync.targetIDs[i] == 0)
                        {
                            missile.GL_Target = null;
                            return;
                        }

                        missile.GL_Target = MyAPIGateway.Entities.GetEntityById(sync.targetIDs[i]);
                    }

                }
            }
            else if (data is SendDefinition)
            {
                SendDefinition data2 = data as SendDefinition;
                VPFAmmoDefinition definition = data2.Data;

                if (!Definitions.Contains(definition))
                {
                    Definitions.Add(definition);
                }
                else
                {
                    return;
                }


                if (definition.GL_Stats != null)
                {
                    if (definition.GL_Stats.Value.GL_DecoyPercentChanceToCauseRetarget > 0 && !RetargetChances.ContainsKey(definition.subtypeName))
                    {
                        RetargetChances.Add(definition.subtypeName, new MyTuple<float, double>(definition.GL_Stats.Value.GL_DecoyRetargetRadius, definition.GL_Stats.Value.GL_DecoyPercentChanceToCauseRetarget));
                        if (definition.GL_Stats.Value.GL_DecoyRetargetRadius > VPFMissile.MaximumRetargetRadius)
                        {
                            VPFMissile.MaximumRetargetRadius = definition.GL_Stats.Value.GL_DecoyRetargetRadius;
                        }
                    }
                }

                MyLog.Default.WriteLine($"Vanilla+ Framework API: Added {definition.subtypeName} Definition. Total: {Definitions.Count}");
            }
        }

        #endregion

        #region Event & Mod Message Handlers
        private void OnMissileAdded(IMyMissile missile)
        {
            VPFAmmoDefinition def = null;

            foreach (VPFAmmoDefinition Definition in Definitions)
            {
                if (missile.AmmoDefinition.Id.SubtypeName == Definition.subtypeName)
                {
                    def = Definition;
                    break;
                }
            }

            if (def == null)
            {
                Missiles.Add(missile.EntityId, new VPFMissile(missile, DefaultAmmoDefinition, null));
                return;
            }

            VPFVisualEffectsDefinition visualeffect = null;

            if (def.FXsubtypeName != null)
            {
                foreach (VPFVisualEffectsDefinition Definition in FXRenderer.Definitions)
                {
                    if (def.FXsubtypeName == Definition.subtypeName)
                    {
                        visualeffect = Definition;
                        break;
                    }
                }
            }



            Missiles.Add(missile.EntityId, new VPFMissile(missile, def, visualeffect));
        }

        private void OnProjectileAdded(ref MyProjectileInfo projectile, int index)
        {
            try
            {
                VPFAmmoDefinition def = null;

                foreach (VPFAmmoDefinition Definition in Definitions)
                {
                    if (projectile.ProjectileAmmoDefinition.Id.SubtypeName == Definition.subtypeName)
                    {
                        def = Definition;
                        break;
                    }
                }

                if (def != null && def.BWT_Stats != null)
                {
                    MyProjectileAmmoDefinition ProjectileAmmoDef = projectile.ProjectileAmmoDefinition as MyProjectileAmmoDefinition;

                    float distance = ProjectileAmmoDef.MaxTrajectory * ((MyWeaponDefinition)projectile.WeaponDefinition).RangeMultiplier;

                    Vector3? vel;
                    if (projectile.OwnerEntity is IMySlimBlock)
                    {
                        vel = (projectile.OwnerEntity as IMySlimBlock).CubeGrid.LinearVelocity;
                    }
                    else if (projectile.OwnerEntity is IMyCubeGrid)
                    {
                        vel = (projectile.OwnerEntity as IMyCubeGrid).LinearVelocity;
                    }
                    else
                    {
                        vel = projectile.OwnerEntity?.Physics?.LinearVelocity;
                    }
                    Vector3 velocity = Vector3.Zero;
                    if (vel != null)
                    {
                        velocity = vel.Value;
                    }
                    BeamDefinition beamdef = new BeamDefinition
                    {
                        MaxTrajectory = distance,

                        MaxReflectChance = ProjectileAmmoDef.ProjectileTrailColor.X,
                        MinReflectChance = -1,
                        MaxReflectAngle = ProjectileAmmoDef.ProjectileTrailColor.Y,
                        MinReflectAngle = -1,
                        ReflectDamage = ProjectileAmmoDef.ProjectileTrailColor.Z,

                        PenetrationDamage = ProjectileAmmoDef.ProjectileMassDamage * ((MyWeaponDefinition)projectile.WeaponDefinition).DamageMultiplier,
                        PlayerDamage = ProjectileAmmoDef.ProjectileHealthDamage * ((MyWeaponDefinition)projectile.WeaponDefinition).DamageMultiplier,
                        ExplosiveDamage = ProjectileAmmoDef.ProjectileExplosionDamage * ((MyWeaponDefinition)projectile.WeaponDefinition).DamageMultiplier,
                        ExplosiveRadius = ProjectileAmmoDef.ProjectileExplosionRadius,

                        ExplosionFlags = 1006,
                        EndOfLifeEffect = ProjectileAmmoDef.EndOfLifeEffect,
                        EndOfLifeSound = ProjectileAmmoDef.EndOfLifeSound,

                        BWT_Stats = def.BWT_Stats.Value,
                        PD_Stats = def.PD_Stats,
                        EMP_Stats = def.EMP_Stats,
                        JDI_Stats = def.JDI_Stats,
                        SCI_Stats = def.SCI_Stats
                    };

                    Beams.Beams.GenerateBeam(projectile.Position, projectile.Velocity.Normalized(), velocity, ref beamdef, projectile.OwnerEntity);

                    MyAPIGateway.Projectiles.MarkProjectileForDestroy(index);
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(ex);
            }

        }

        private void OnMissileMoved(IMyMissile missile, ref Vector3 Velocity)
        {
            VPFMissile m;
            if (Missiles.TryGetValue(missile.EntityId, out m))
            {
                if (m.ProxyId != -1)
                {
                    BoundingSphere sphere = new BoundingSphere(missile.PositionComp.GetPosition(), 1.0f);
                    BoundingBox result;
                    BoundingBox.CreateFromSphere(ref sphere, out result);
                    MissileTree.MoveProxy(m.ProxyId, ref result, Velocity);
                }
            }
        }
        private void OnMissileRemoved(IMyMissile missile)
        {
            VPFMissile VPFMissileStat;
            Missiles.TryGetValue(missile.EntityId, out VPFMissileStat);

            if (VPFMissileStat.ProxyId != -1)
            {
                MissileTree.RemoveProxy(VPFMissileStat.ProxyId);
                VPFMissileStat.ProxyId = -1;
            }

            if (VPFMissileStat.NeedsAPHEFix)
            {
                VPFMissileStat.APHE_DetonateMissile(VPFMissileStat.LastHitEntity);
            }
            VPFMissileStat.Close();

            Missiles.Remove(missile.EntityId);

        }



        private void OnMissileCollided(IMyMissile missile)
        {
            VPFMissile VPFMissileStat;
            Missiles.TryGetValue(missile.EntityId, out VPFMissileStat);

            if (VPFMissileStat.BWT_Stats != null || VPFMissileStat.collided)
            {
                return;
            }
            VPFMissileStat.collided = true;

            JumpDriveInhibition_Logic? JDI_Stats = VPFMissileStat.JDI_Stats;

            if (JDI_Stats != null)
            {
                FrameworkUtilities.JDI_Hit(missile.CollidedEntity, JDI_Stats.Value.JDI_PowerDrainInW, JDI_Stats.Value.JDI_DistributePower);
            }

            List<SpecialComponentryInteraction_Logic> SCI_Stats = VPFMissileStat.SCI_Stats;

            if (SCI_Stats != null)
            {
                FrameworkUtilities.SCI_Hit(missile.CollidedEntity, SCI_Stats, missile.PositionComp.GetPosition());
            }

            if (VPFMissileStat.NeedsAPHEFix)
            {
                VPFMissileStat.LastHitEntity = missile.CollidedEntity;
            }
        }


        // Gets definitions from dependencies
        public static void OnDefinitionRecieved(VPFAmmoDefinition def)
        {
            if (def.subtypeName == "" || def.subtypeName == null)
            {
                MyLog.Default.WriteLineAndConsole($"Error. Specified subtype in {def} is null or empty.");
                return;
            }
            if (!Definitions.Contains(def))
                Definitions.Add(def);
            else return;

            if (def.VPF_MissileHitpoints != -1 && def.GL_Stats != null ? (def.GL_Stats.Value.GL_DecoyPercentChanceToCauseRetarget > 0) : false && !RetargetChances.ContainsKey(def.subtypeName))
            {
                RetargetChances.Add(def.subtypeName, new MyTuple<float, double>(def.GL_Stats.Value.GL_DecoyRetargetRadius, def.GL_Stats.Value.GL_DecoyPercentChanceToCauseRetarget));
                if (def.GL_Stats.Value.GL_DecoyRetargetRadius > VPFMissile.MaximumRetargetRadius)
                {
                    VPFMissile.MaximumRetargetRadius = def.GL_Stats.Value.GL_DecoyRetargetRadius;
                }
            }

            MyLog.Default.WriteLineAndConsole($"Definition {def} loaded");
        }

        public void OnNetworkMessageRecieved(ushort ChannelId, Packet packet, ulong SenderId, bool IsServer)
        {
            if (packet is Request)
            {
                List<Packet> packets = SyncRequest(packet.DataType);

                foreach (Packet m in packets)
                {
                    CommunicationTools.SendMessageTo(m, CommunicationTools.MessageHandlerId, SenderId);
                }

                MyLog.Default.WriteLine($"Sync request recieved from {SenderId}, of {packet.DataType}");
            }
            else
            {
                SyncData(packet);
            }
        }
        #endregion

        #region Missile Chat Commands


        public void ChatCommand_ShowLoadedDefinitions(ulong SenderId, string[] SplitText)
        {
            if (SplitText.Length >= 2)
            {
                if (SplitText[1] == "ShowFlares")
                {
                    foreach (KeyValuePair<string, MyTuple<float, double>> pair in RetargetChances)
                    {
                        VPFChatCommands.ShowMessage($"Flare Definition: {pair.Key} - Radius: {pair.Value.Item1} - {pair.Value.Item2}% Chance", SenderId, true);
                    }
                }
                else
                {
                    foreach (VPFAmmoDefinition def in Definitions)
                    {
                        if (def.subtypeName == SplitText[1])
                        {
                            VPFChatCommands.ShowMessage(def.ToString(), SenderId, true);
                            break;
                        }
                        else if (SplitText[1] == "List")
                        {
                            VPFChatCommands.ShowMessage(def.subtypeName, SenderId, true);
                        }
                    }
                }
                return;
            }

            VPFChatCommands.ShowMessage($"Loaded {Definitions.Count} Ammo Definitions. Definitions:", SenderId, true);
            foreach (VPFAmmoDefinition def in Definitions)
            {
                VPFChatCommands.ShowMessage($"{def.subtypeName}", SenderId, true);
            }
        }

        public void ChatCommand_KillAllMissiles(ulong SenderId, string[] SplitText)
        {
            if (VPFChatCommands.IsAdmin(SenderId))
            {
                foreach (KeyValuePair<long, VPFMissile> missile in Missiles)
                {
                    missile.Value.Close();
                }
            }
            VPFChatCommands.ShowMessage($"Killed {Missiles.Count} missiles!", SenderId, false);
            Missiles.Clear();
        }

        #endregion

        #region Load/Unload & Update
        public override void BeforeStart()
        {
            MyAPIGateway.Missiles.OnMissileAdded += OnMissileAdded;
            MyAPIGateway.Missiles.OnMissileRemoved += OnMissileRemoved;
            MyAPIGateway.Missiles.OnMissileCollided += OnMissileCollided;
            MyAPIGateway.Missiles.OnMissileMoved += OnMissileMoved;
            CommunicationTools.OnMessageReceived += OnNetworkMessageRecieved;
            MyAPIGateway.Projectiles.OnProjectileAdded += OnProjectileAdded;

            VPFChatCommands.AddChatCommand("/ShowLoadedAmmoDefinitions", ChatCommand_ShowLoadedDefinitions);
            VPFChatCommands.AddChatCommand("/KillAllMissiles", ChatCommand_KillAllMissiles);

            MultiplayerId = MyAPIGateway.Multiplayer.MyId;

            if (!MyAPIGateway.Session.IsServer)
            {
                Request request = new Request();

                CommunicationTools.SendMessageToServer(request, CommunicationTools.MessageHandlerId);
            }
        }



        protected override void UnloadData()
        {
            MyAPIGateway.Missiles.OnMissileAdded -= OnMissileAdded;
            MyAPIGateway.Missiles.OnMissileRemoved -= OnMissileRemoved;
            MyAPIGateway.Missiles.OnMissileCollided -= OnMissileCollided;
            MyAPIGateway.Missiles.OnMissileMoved -= OnMissileMoved;
            CommunicationTools.OnMessageReceived -= OnNetworkMessageRecieved;
            MyAPIGateway.Projectiles.OnProjectileAdded -= OnProjectileAdded;

            RetargetChances.Clear();
            Definitions.Clear();
            Missiles.Clear();
        }

        private void FirstFrameInit()
        {
            foreach (VPFAmmoDefinition definition in Definitions)
            {
                if (definition.GL_Stats != null)
                {
                    if (definition.GL_Stats.Value.GL_DecoyPercentChanceToCauseRetarget > 0 && !RetargetChances.ContainsKey(definition.subtypeName))
                    {
                        RetargetChances.Add(definition.subtypeName, new MyTuple<float, double>(definition.GL_Stats.Value.GL_DecoyRetargetRadius, definition.GL_Stats.Value.GL_DecoyPercentChanceToCauseRetarget));
                        if (definition.GL_Stats.Value.GL_DecoyRetargetRadius > VPFMissile.MaximumRetargetRadius)
                        {
                            VPFMissile.MaximumRetargetRadius = definition.GL_Stats.Value.GL_DecoyRetargetRadius;
                        }
                    }
                }
            }

            MyAPIGateway.Utilities.ShowMessage("Vanilla+ Framework API", $"Loaded {Definitions.Count} Ammo Definitions.");
            firstFrame = false;
        }

        private void UpdateMissiles()
        {
            for (int i = 0; i < Missiles.Count; i++)
            {
                if (Missiles.ElementAt(i).Value.Update())
                    Missiles.Remove(Missiles.ElementAt(i).Key);
            }
        }


        private void UpdateDisabledBlocks()
        {
            for (int i = 0; i < VanillaPlusFramework.DisabledBlocks.Count; i++)
            {
                IMyEntity entity = MyAPIGateway.Entities.GetEntityById(VanillaPlusFramework.DisabledBlocks.Keys.ElementAt(i));

                if (VanillaPlusFramework.DisabledBlocks.Values.ElementAt(i).Update(entity != null ? (IMyFunctionalBlock)entity : null))
                    VanillaPlusFramework.DisabledBlocks.Remove(VanillaPlusFramework.DisabledBlocks.ElementAt(i).Key);
            }
        }
        public bool ShouldSync(VPFMissile VPFmissile)
        {
            if (VPFmissile == null || VPFmissile.missile == null || VPFmissile.missile.MarkedForClose || VPFmissile.GL_Target == null) return false;

            if (VPFmissile.SyncTarget)
            {
                VPFmissile.SyncTarget = false;
                return true;
            }

            return false;
        }
        int cooldown = 0;
        private void DoNetcode()
        {
            if (!(LastMissilesCount == 0 && 0 == Missiles.Count) && cooldown < ticks)
            {
                List<long> missiles = new List<long>();
                List<long> targets = new List<long>();

                foreach (KeyValuePair<long, VPFMissile> pair in Missiles)
                {
                    if (ShouldSync(pair.Value))
                    {
                        missiles.Add(pair.Key);
                        targets.Add(pair.Value.GL_Target?.EntityId ?? 0);

                        if (missiles.Count > 10)
                        {
                            cooldown = ticks += 60;
                            break;
                        }
                    }
                }
                if (missiles.Count > 0)
                    CommunicationTools.SendMessageToClients(new SyncMissileTarget(missiles.ToArray(), targets.ToArray()), CommunicationTools.MessageHandlerId, true, MultiplayerId);
            }
            if (ticks % 100 == 0)
            {
                // sync RNG
            }

            LastMissilesCount = Missiles.Count;
        }
        public override void UpdateBeforeSimulation()
        {
            if (firstFrame)
            {
                FirstFrameInit();
            }

            UpdateMissiles();
            if (MyAPIGateway.Multiplayer.IsServer)
                UpdateDisabledBlocks();
        }
        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                DoNetcode();
            }
        }

        #endregion


    }
}
