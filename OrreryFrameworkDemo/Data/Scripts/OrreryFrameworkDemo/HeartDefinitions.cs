namespace OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication
{
    partial class HeartDefinitions
    {
        internal HeartDefinitions()
        {
            LoadWeaponDefinitions(ExampleTurretWeapon, ExampleFixedProjWeapon, ExampleFixedBeamWeapon, ExampleFixedMissileWeapon);         //todo tell the user that they forgot to add stuff here when they get an error
            LoadAmmoDefinitions(ExampleAmmoProjectile, ExampleAmmoMissile, ExampleAmmoBeam);
        }
    }
}
