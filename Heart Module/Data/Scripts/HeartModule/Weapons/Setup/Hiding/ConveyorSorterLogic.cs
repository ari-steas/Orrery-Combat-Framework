using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Hiding
{
    // For more info about the gamelogic comp see https://github.com/THDigi/SE-ModScript-Examples/blob/master/Data/Scripts/Examples/BasicExample_GameLogicAndSession/GameLogic.cs
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "TestWeapon")]
    public class ConveyorSorterLogic : MyGameLogicComponent
    {
        IMyConveyorSorter Sorter;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            HideControlsExample.DoOnce();

            Sorter = (IMyConveyorSorter)Entity;

            if (Sorter.CubeGrid?.Physics == null)
                return; // ignore ghost/projected grids

            // the bonus part, enforcing it to stay a specific value.
            if (MyAPIGateway.Multiplayer.IsServer) // serverside only to avoid network spam
            {
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (Sorter.DrainAll)
            {
                Sorter.DrainAll = false;
            }
            //MyAPIGateway.Utilities.ShowNotification("is this even working");
        }
    }
}