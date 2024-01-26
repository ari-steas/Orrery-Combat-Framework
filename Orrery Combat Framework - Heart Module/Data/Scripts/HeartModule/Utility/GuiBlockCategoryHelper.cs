using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;

namespace Heart_Module.Data.Scripts.HeartModule.Utility
{
    public class GuiBlockCategoryHelper
    {
        MyGuiBlockCategoryDefinition category;

        public GuiBlockCategoryHelper(string Name, string Id)
        {
            category = new MyGuiBlockCategoryDefinition
            {
                Id = new MyDefinitionId(typeof(MyObjectBuilder_GuiBlockCategoryDefinition), Id),
                Name = Name,
                DisplayNameString = Name,
                ItemIds = new HashSet<string>(),
            };
            MyDefinitionManager.Static.GetCategories().Add(Name, category);
        }

        public void AddBlock(string subtypeId)
        {
            if (!category.ItemIds.Contains(subtypeId))
                category.ItemIds.Add(subtypeId);
            //foreach (var _cat in MyDefinitionManager.Static.GetCategories().Values)
            //{
            //    MyAPIGateway.Utilities.ShowMessage("", _cat.DisplayNameString);
            //    _cat.ItemIds.Add(subtypeId);
            //}
        }
    }
}
