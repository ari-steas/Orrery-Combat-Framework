using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game;
using Sandbox.Game.Entities;

namespace Heart_Module.Data.Scripts.HeartModule
{
    public class HeartUtils
    {
        public static MyRelationsBetweenPlayers GetRelationsBetweenPlayers(long playerIdentity1, long playeIdentity2) // From Digi in the KSH Discord
        {
            if (playerIdentity1 == playeIdentity2)
                return MyRelationsBetweenPlayers.Self;

            var faction1 = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerIdentity1);
            var faction2 = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playeIdentity2);

            if (faction1 == null || faction2 == null)
                return MyRelationsBetweenPlayers.Enemies;

            if (faction1 == faction2)
                return MyRelationsBetweenPlayers.Allies;

            if (MyAPIGateway.Session.Factions.GetRelationBetweenFactions(faction1.FactionId, faction2.FactionId) == MyRelationsBetweenFactions.Neutral)
                return MyRelationsBetweenPlayers.Neutral;

            return MyRelationsBetweenPlayers.Enemies;
        }

        public static IMyIdentity GetGridOwner(IMyCubeGrid grid)
        {
            List<long> owners = grid.BigOwners;
            if (owners.Count == 0)
                return null;
            IMyIdentity targetOwner = null;
            MyAPIGateway.Players.GetAllIdentites(null, (ident) => { if (ident.IdentityId == owners[0]) targetOwner = ident; return false; });

            return targetOwner;
        }

        public static IMyPlayer GetPlayerFromSteamId(ulong id)
        {
            foreach (var player in HeartData.I.Players)
                if (player.SteamUserId == id)
                    return player;
            return null;
        }

        public static MyRelationsBetweenPlayerAndBlock MapPlayerRelationsToBlock(MyRelationsBetweenPlayers relations)
        {
            switch (relations)
            {
                case MyRelationsBetweenPlayers.Self:
                    return MyRelationsBetweenPlayerAndBlock.Owner;
                case MyRelationsBetweenPlayers.Neutral:
                    return MyRelationsBetweenPlayerAndBlock.Neutral;
                case MyRelationsBetweenPlayers.Allies:
                    return MyRelationsBetweenPlayerAndBlock.Friends;
                case MyRelationsBetweenPlayers.Enemies:
                    return MyRelationsBetweenPlayerAndBlock.Enemies;
            }
            return MyRelationsBetweenPlayerAndBlock.NoOwnership;
        }

        public static MyRelationsBetweenPlayerAndBlock GetRelationsBetweeenGrids(IMyCubeGrid ownGrid, IMyCubeGrid targetGrid)
        {
            if (targetGrid.BigOwners.Count == 0 || ownGrid.BigOwners.Count == 0)
                return MyRelationsBetweenPlayerAndBlock.NoOwnership;
            long targetOwner = targetGrid.BigOwners[0];
            long owner = ownGrid.BigOwners[0];

            return MapPlayerRelationsToBlock(GetRelationsBetweenPlayers(owner, targetOwner));
        }
    }
}
