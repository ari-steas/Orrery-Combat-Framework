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
using System.Security.Cryptography;

namespace VanillaPlusFramework.Turrets
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation)]
    public class TurretLogic : MySessionComponentBase
    {
        public static List<VPFTurretDefinition> Definitions = new List<VPFTurretDefinition>();
        public static List<Turret> Turrets = new List<Turret>();
        bool IsFirstFrame = true;
        public override void BeforeStart()
        {
            MyAPIGateway.Projectiles.OnProjectileAdded += OnProjectileAdded;
            MyAPIGateway.Missiles.OnMissileAdded += OnMissileAdded;
            
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Projectiles.OnProjectileAdded -= OnProjectileAdded;
            MyAPIGateway.Missiles.OnMissileAdded -= OnMissileAdded;

            Definitions.Clear();
            Turrets.Clear();
        }

        private void FirstFrameInit()
        {
            MyAPIGateway.Utilities.ShowMessage("Vanilla+ Framework API", $"Loaded {Definitions.Count} Turret Definitions.");
            IsFirstFrame = false;
        }

        public override void UpdateBeforeSimulation()
        {
            if (IsFirstFrame)
            {
                FirstFrameInit();
            }
        }

        public static void OnDefinitionRecieved(VPFTurretDefinition def)
        {
            if (def.subtypeName == "" || def.subtypeName == null)
            {
                MyLog.Default.WriteLineAndConsole($"Error. Specified subtype in {def} is null or empty.");
                return;
            }
            if (!Definitions.Contains(def))
                Definitions.Add(def);
            else return;


            MyLog.Default.WriteLineAndConsole($"Definition {def} loaded");
        }
        private void OnMissileAdded(IMyMissile obj)
        {
            IMyEntity owner = MyAPIGateway.Entities.GetEntityById(obj.Owner);

            CallOnTurretFire(owner);
        }

        private static void CallOnTurretFire(IMyEntity owner)
        {
            if (owner != null && owner is IMyLargeTurretBase)
            {
                owner.Components.Get<Turret>()?.OnTurretFire();
            }
        }

        private void OnProjectileAdded(ref MyProjectileInfo projectile, int index)
        {
            IMyEntity owner = projectile.OwnerEntity;

            CallOnTurretFire(owner);
        }
    }
}
