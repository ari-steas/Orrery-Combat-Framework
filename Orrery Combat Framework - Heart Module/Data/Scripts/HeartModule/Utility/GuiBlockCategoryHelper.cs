using Sandbox.Definitions;
using System.Collections.Generic;
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
                IsBlockCategory = true,
            };
            MyDefinitionManager.Static.GetCategories().Add(Name, category);
        }

        public void AddBlock(string subtypeId)
        {
            if (!category.ItemIds.Contains(subtypeId))
                category.ItemIds.Add(subtypeId);

            //foreach (var _cat in MyDefinitionManager.Static.GetCategories().Values)
            //{
            //    HeartData.I.Log.Log("Category " + _cat.Name);
            //    foreach (var _id in _cat.ItemIds)
            //        HeartData.I.Log.Log($"   \"{_id}\"");
            //}
        }
    }
}
