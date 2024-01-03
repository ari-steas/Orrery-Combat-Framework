using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;
using VRage.Game;
using VRage.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Definitions;
using VRage.Input;
using VRage;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using System;

[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
public class MissileSpawnerScript : MySessionComponentBase
{
    private bool isRPressedLastFrame = false;

    public override void UpdateBeforeSimulation()
    {
        try
        {
            var controlledObject = MyAPIGateway.Session?.ControlledObject;
            var character = controlledObject?.Entity as IMyCharacter;

            if (character != null)
            {
                var input = MyAPIGateway.Input;
                if (input != null)
                {
                    if (input.IsNewKeyPressed(MyKeys.R) && !isRPressedLastFrame)
                    {
                        SpawnMissile(character);
                    }

                    isRPressedLastFrame = input.IsKeyPress(MyKeys.R);
                }
            }
        }
        catch (Exception e)
        {
            MyAPIGateway.Utilities.ShowNotification("Error: " + e.ToString(), 10000, MyFontEnum.Red);
        }
    }

    private void SpawnMissile(IMyCharacter character)
    {
        try
        {
            var matrix = character.WorldMatrix;
            var forward = matrix.Forward;
            var spawnPosition = matrix.Translation + forward * 10; // Spawn 10m in front of the player

            // Define the weapon and ammo definition IDs
            MyDefinitionId weaponDefId = new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "InvalidTestTurretProjectile");
            MyDefinitionId ammoDefId = new MyDefinitionId(typeof(MyObjectBuilder_AmmoDefinition), "InvalidProjectile");

            // Retrieve the actual definition objects using the definition IDs
            var weaponDefinition = MyDefinitionManager.Static.GetWeaponDefinition(weaponDefId);
            var ammoDefinition = MyDefinitionManager.Static.GetAmmoDefinition(ammoDefId);

            if (weaponDefinition == null || ammoDefinition == null)
            {
                MyAPIGateway.Utilities.ShowNotification("One or more definitions not found.", 10000, MyFontEnum.Red);
                return;
            }

            // Set the properties for the new projectile
            Vector3D origin = spawnPosition;
            Vector3 initialVelocity = character.Physics?.LinearVelocity ?? Vector3.Zero; // Ensure a valid velocity
            Vector3 directionNormalized = forward;

            // Assuming the character is the owner of this projectile for simplicity
            MyEntity owningEntity = character as MyEntity;

            // Add the projectile using the IMyProjectiles interface
            MyAPIGateway.Projectiles.Add(
                weaponDefinition, // MyDefinitionBase object
                ammoDefinition,   // MyDefinitionBase object
                origin,
                initialVelocity,
                directionNormalized,
                owningEntity,
                owningEntity,
                null,
                null,
                false,
                0UL
            );

            //add a missile
            MyAPIGateway.Utilities.ShowNotification("Missile spawned!", 2000, MyFontEnum.Green);
        }
        catch (Exception e)
        {
            MyAPIGateway.Utilities.ShowNotification("Error spawning missile: " + e.ToString(), 10000, MyFontEnum.Red);
        }
    }



    protected override void UnloadData()
    {
        // Unload any data or unsubscribe from events here
    }
}
