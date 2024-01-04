using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding
{
    // For more info about the gamelogic comp see https://github.com/THDigi/SE-ModScript-Examples/blob/master/Data/Scripts/Examples/BasicExample_GameLogicAndSession/GameLogic.cs
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "TestWeapon")]
    public class SorterWeaponLogic : MyGameLogicComponent
    {
        IMyConveyorSorter SorterWep;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            SorterWeaponTerminalControls.DoOnce(ModContext);

            SorterWep = (IMyConveyorSorter)Entity;
            if (SorterWep.CubeGrid?.Physics == null)
                return; // ignore ghost/projected grids

            // stuff and things
        }

        // these are going to be set or retrieved by the terminal controls (as seen in the terminal control's Getter and Setter).

        // as mentioned in the other .cs file, the terminal stuff are only GUI.
        // if you want the values to persist over world reloads and be sent to clients you'll need to implement that yourself.
        // see: https://github.com/THDigi/SE-ModScript-Examples/wiki/Save-&-Sync-ways

        public bool Terminal_ExampleToggle
        {
            get
            {
                return SorterWep?.Enabled ?? false;
            }
            set
            {
                if (SorterWep != null)
                    SorterWep.Enabled = value;
            }
        }

        public float Terminal_ExampleFloat { get; set; }
    }
}