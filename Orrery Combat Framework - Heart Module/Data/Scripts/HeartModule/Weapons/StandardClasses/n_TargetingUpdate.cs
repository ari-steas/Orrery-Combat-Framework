using Heart_Module.Data.Scripts.HeartModule.Network;
using Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses
{
    // TODO complete
    [ProtoContract]
    internal class n_TargetingUpdate : PacketBase
    {
        [ProtoMember(1)] long EntityId;
        [ProtoMember(2)] long TargetEntityId;

        public override void Received(ulong SenderSteamId)
        {
            IMyEntity thisEntity = MyAPIGateway.Entities.GetEntityById(EntityId);
            if (thisEntity == null)
                return;

            IMyEntity targetEntity = MyAPIGateway.Entities.GetEntityById(TargetEntityId);

            if (thisEntity is IMyCubeGrid)
            {
                WeaponManagerAi.I.GetTargeting((IMyCubeGrid) thisEntity).SetPrimaryTarget((IMyCubeGrid) targetEntity);
            }
            else if (thisEntity is IMyConveyorSorter)
            {
                SorterTurretLogic weapon = WeaponManager.I.GetWeapon(EntityId) as SorterTurretLogic;

                weapon?.SetTarget(targetEntity);
            }
        }
    }
}
