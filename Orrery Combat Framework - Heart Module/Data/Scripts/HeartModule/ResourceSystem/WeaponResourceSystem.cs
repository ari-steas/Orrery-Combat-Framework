using System;
using System.Collections.Generic;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.ResourceSystem
{
    internal class WeaponResourceSystem
    {
        private readonly WeaponDefinitionBase _weaponDefinition;
        private readonly SorterWeaponLogic _weaponLogic;
        private Dictionary<string, float> _resources;

        public WeaponResourceSystem(WeaponDefinitionBase weaponDefinition, SorterWeaponLogic weaponLogic)
        {
            _weaponDefinition = weaponDefinition;
            _weaponLogic = weaponLogic;
            _resources = new Dictionary<string, float>();

            // Initialize resources with maximum storage capacity
            foreach (var resource in _weaponDefinition.Loading.Resources)
            {
                _resources[resource.ResourceType] = resource.ResourceStorage;
            }
        }

        public bool CanShoot()
        {
            // Check if there are enough resources for at least one shot
            foreach (var resource in _weaponDefinition.Loading.Resources)
            {
                if (_resources[resource.ResourceType] < resource.MinResourceBeforeFire)
                {
                    ShowNotification($"Insufficient {resource.ResourceType} to fire. Current {resource.ResourceType} count: {_resources[resource.ResourceType]}", 1000); // Show notification for insufficient resources
                    return false;
                }
            }
            return true;
        }

        public void ConsumeResources()
        {
            // Consume resources per shot
            foreach (var resource in _weaponDefinition.Loading.Resources)
            {
                _resources[resource.ResourceType] -= resource.ResourcePerShot;
                if (_resources[resource.ResourceType] < 0) _resources[resource.ResourceType] = 0;
            }

            // Update resource status after consumption
            ShowResourceStatus();
        }

        public void RegenerateResources(float deltaTime)
        {
            // Regenerate resources over time
            foreach (var resource in _weaponDefinition.Loading.Resources)
            {
                _resources[resource.ResourceType] += resource.ResourceGeneration * deltaTime;
                if (_resources[resource.ResourceType] > resource.ResourceStorage)
                    _resources[resource.ResourceType] = resource.ResourceStorage;
            }

            // Update resource status after regeneration
            ShowResourceStatus();
        }

        // Call this method every update tick
        public void Update(float deltaTime)
        {
            RegenerateResources(deltaTime);
        }

        private void ShowResourceStatus()
        {
            // Display the current count of each resource
            foreach (var resource in _weaponDefinition.Loading.Resources)
            {
                ShowNotification($"Current {resource.ResourceType} count: {_resources[resource.ResourceType]}", 1000);
            }
        }

        private void ShowNotification(string message, int duration)
        {
            MyAPIGateway.Utilities.ShowNotification(message, duration);
        }
    }
}
