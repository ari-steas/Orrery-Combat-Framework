using System;
using System.Collections.Generic;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
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
                    return false;
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
        }

        // Call this method every update tick
        public void Update(float deltaTime)
        {
            RegenerateResources(deltaTime);
        }
    }
}
