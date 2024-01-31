using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Heart_Module.Data.Scripts.HeartModule.Utility
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class CommandHandler : MySessionComponentBase
    {

      //  private bool _isInitialized = false;
      //
      //  public override void UpdateBeforeSimulation()
      //  {
      //      base.UpdateBeforeSimulation();
      //
      //      if (_isInitialized) return;
      //      if (MyAPIGateway.Session == null) return;
      //
      //      MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
      //      _isInitialized = true;
      //  }
      //
      //  private void OnMessageEntered(string messageText, ref bool sendToOthers)
      //  {
      //      sendToOthers = false; // Prevents the message from being broadcasted to other players
      //
      //      if (!messageText.StartsWith("/OCF ", StringComparison.OrdinalIgnoreCase)) return;
      //
      //      var args = messageText.Split(' ');
      //      if (args.Length < 2) return;
      //
      //      switch (args[1].ToLower())
      //      {
      //          case "fillammo":
      //              FillAmmoCommand(args.Skip(2).ToArray());
      //              break;
      //              // You can add more commands here
      //      }
      //  }
      //
      //  private void FillAmmoCommand()
      //  {
      //      var aimedBlock = GetAimedBlock();
      //      if (aimedBlock == null)
      //      {
      //          MyAPIGateway.Utilities.ShowNotification("No targeted block.", 2000, "Red");
      //          return;
      //      }
      //
      //      var grid = aimedBlock.CubeGrid;
      //      var sorters = new List<IMyTerminalBlock>();
      //      MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid).GetBlocksOfType<IMyConveyorSorter>(sorters);
      //
      //      foreach (var sorter in sorters)
      //      {
      //          var inventory = sorter.GetInventory();
      //          // Assume the sorter has a WeaponLogic_Magazines component attached or accessible
      //          var ammoDefinition = ProjectileDefinitionManager.GetDefinition(GetSelectedAmmo(sorter)); // Implement YourMethodToGetSelectedAmmo
      //          string magazineItem = ammoDefinition.Ungrouped.MagazineItemToConsume;
      //
      //          if (!string.IsNullOrWhiteSpace(magazineItem))
      //          {
      //              var itemToConsume = new MyDefinitionId(typeof(MyObjectBuilder_Component), magazineItem);
      //              int amountNeeded = YourMethodToDetermineAmountNeeded(sorter); // Implement YourMethodToDetermineAmountNeeded
      //
      //              // Attempt to add the ammo items to the sorter's inventory
      //              AddItemsToInventory(inventory, itemToConsume, amountNeeded);
      //              MyAPIGateway.Utilities.ShowNotification($"Filled {sorter.CustomName} with {magazineItem}.", 2000, "Green");
      //          }
      //          else
      //          {
      //              MyAPIGateway.Utilities.ShowNotification($"{sorter.CustomName} does not require specific ammo.", 2000, "Blue");
      //          }
      //      }
      //  }
      //
      //  private void AddItemsToInventory(IMyInventory inventory, MyDefinitionId itemToConsume, int amountNeeded)
      //  {
      //      // Implement logic to add the specified amount of the item to the inventory
      //      // This is a placeholder implementation
      //      var content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(itemToConsume);
      //      inventory.AddItems(amountNeeded, content);
      //  }
      //
      //
      //  protected override void UnloadData()
      //  {
      //      base.UnloadData();
      //      MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
      //  }
      //
    }
}
