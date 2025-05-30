using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.MegacityMetro.Gameplay
{
    /// <summary>
    /// Job to simulate the laser.
    /// </summary>
    [WithAll(typeof(PlayerVehicleInput))]
    
    [BurstCompile]
    public partial struct ShootLaserJob : IJobEntity
    {
        [ReadOnly]
        public float DeltaTime;
        [ReadOnly]
        public ComponentLookup<VehicleHealth> HealthLookup;
        
        [BurstCompile]
        private void Execute(
            ref VehicleLaser laser,
            in Entity entity, 
            in PlayerVehicleInput input)
        {
            HealthLookup.TryGetComponent(entity, out VehicleHealth ownerHealth);
            if (ownerHealth.IsDead == 1)
            {
                laser.Shoots = false;
                return;
            }
            laser.Energy = math.min(100, laser.Energy + (laser.RechargeRate * DeltaTime));
            laser.LaserCooldown = math.min(laser.FireRate, laser.LaserCooldown + DeltaTime);
            // If the laser has cooldown (max fire rate), has enough energy and the player is shooting, then shoot
            laser.Shoots = laser.LaserCooldown >= laser.FireRate && laser.Energy > laser.DrainRate && input.Shoot;
            if (laser.Shoots)
            {
                // Energy drain when shooting
                laser.Energy -= laser.DrainRate;
                laser.LaserCooldown = 0;
            }
        }
    }
}