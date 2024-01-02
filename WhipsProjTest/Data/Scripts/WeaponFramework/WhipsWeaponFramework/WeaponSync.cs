using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using ProtoBuf;

namespace Whiplash.WeaponFramework
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class WeaponSync : MySessionComponentBase
    {
        bool scriptInit = false;

        public override void UpdateBeforeSimulation()
        {
            if (scriptInit == false)
            {
                scriptInit = true;

                MyAPIGateway.Multiplayer.RegisterMessageHandler(FrameworkConstants.NETID_FIRE_SYNC, ProcessFireSyncData);
            }
        }

        public static void SendToClients(WeaponFireSyncData data)
        {
            //Below, save your data to string or some other serializable type
            byte[] sendData = MyAPIGateway.Utilities.SerializeToBinary(data);

            //Send the message to the ID you registered in Setup, and specify the user via SteamId
            bool sendStatus = MyAPIGateway.Multiplayer.SendMessageToOthers(FrameworkConstants.NETID_FIRE_SYNC, sendData);

            if (!MyAPIGateway.Utilities.IsDedicated) // Send to self if in local session
            {
                WeaponSession.CreateShadow(data);
            }
        }

        public static void ProcessFireSyncData(byte[] data)
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            //This converts your data back to the original type you had before you sent
            //Depending on what you sent, you may need to parse it back into something usable
            try
            {
                var receivedData = MyAPIGateway.Utilities.SerializeFromBinary<WeaponFireSyncData>(data);
                WeaponSession.CreateShadow(receivedData);
            }
            catch (Exception e)
            {
                /* TODO: Do something later */
            } 
        }

        protected override void UnloadData()
        {
            //Unregister the Message Handler on Unload. I guess these persist?
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(FrameworkConstants.NETID_FIRE_SYNC, ProcessFireSyncData);
        }
    }

    [ProtoContract]
    public struct WeaponFireSyncData
    {
        [ProtoMember(1)]
        public Vector3 Origin;
        [ProtoMember(2)]
        public Vector3 Direction;
        [ProtoMember(3)]
        public Vector3 ShooterVelocity;
        [ProtoMember(4)]
        public Vector3 TracerColor;
        [ProtoMember(5)]
        public bool DrawTrails;
        [ProtoMember(6)]
        public float ProjectileTrailScale;
        [ProtoMember(7)]
        public float TrailDecayRatio;
        [ProtoMember(8)]
        public float MuzzleVelocity;
        [ProtoMember(9)]
        public float MaxRange;
        [ProtoMember(10)]
        public float ArtGravityMult;
        [ProtoMember(11)]
        public float NatGravityMult;
        [ProtoMember(12)]
        public long ShooterID;
        [ProtoMember(13)]
        public bool DrawImpactSprite;
        [ProtoMember(14)]
        public bool ShouldProximityDetonate;
        [ProtoMember(15)]
        public float ProximityDetonationRange;
        [ProtoMember(16)]
        public float ProximityDetonationArmingRange;
    }
}