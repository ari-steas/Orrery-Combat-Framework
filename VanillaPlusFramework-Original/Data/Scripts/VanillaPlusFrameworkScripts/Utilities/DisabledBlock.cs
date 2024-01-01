using Sandbox.ModAPI;
using ProtoBuf;

namespace VanillaPlusFramework.Utilities
{
    [ProtoContract]
    public class DisabledBlock
    {
        [ProtoMember(1)]
        public int TicksDisabled;
        [ProtoMember(2)]
        public bool previousState;
        public DisabledBlock(bool state, int TicksDisabled)
        {
            this.TicksDisabled = TicksDisabled;
            previousState = state;
        }
        public DisabledBlock() { }
        public bool Update(IMyFunctionalBlock block)
        {
            if (block == null)
                return true;

            if (TicksDisabled <= 0)
            {
                block.Enabled = previousState;
                return true;
            }

            block.Enabled = false;

            TicksDisabled--;
            return false;
        }
    }
}
