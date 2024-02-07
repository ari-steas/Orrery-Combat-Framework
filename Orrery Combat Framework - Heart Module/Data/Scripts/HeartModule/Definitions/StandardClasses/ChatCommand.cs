using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heart_Module.Data.Scripts.HeartModule.Definitions.StandardClasses
{
    [ProtoContract]
    public class ChatCommand
    {
        public string modName;
        public Action<string[]> action;

        public ChatCommand(string modName, Action<string[]> action)
        {
            this.modName = modName;
            this.action = action;
        }
    }
}
