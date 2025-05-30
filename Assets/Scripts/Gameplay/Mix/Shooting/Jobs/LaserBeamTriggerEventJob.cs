using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode.Extensions;
using Unity.Physics;
using UnityEngine;

namespace Unity.MegacityMetro.Gameplay
{
    public partial struct LaserBeamTriggerEventJob : ITriggerEventsJob
    {
        internal bool IsServer;
        internal NativeList<Entity> LaserBeamsExploded;
        internal ComponentLookup<VehicleHealth> VehicleHealthLookup;
        internal ComponentLookup<PlayerScore> PlayerScoreLookup;
        [ReadOnly]
        internal ComponentLookup<LaserBeam> LaserBeamLookup;
        [ReadOnly]
        internal ComponentLookup<Immunity> ImmunityLookup;
        [ReadOnly]
        internal ComponentLookup<VehicleLaser> VehicleLaserLookup;
        [ReadOnly]
        internal ComponentLookup<PlayerName> PlayerNameLookup;
        
        public void Execute(TriggerEvent collisionEvent)
        {
            // This jobs handles laser collisions
            // This assumes an entity cannot have a LaserBeam and a VehicleHealth component at the same time
            Entity sourceLaserBeamEntity = default;
            if (LaserBeamLookup.TryGetComponent(collisionEvent.EntityA, out var sourceLaserBeam))
            {
                sourceLaserBeamEntity = collisionEvent.EntityA;
            }
            else
            {
                if (LaserBeamLookup.TryGetComponent(collisionEvent.EntityB, out sourceLaserBeam))
                {
                    sourceLaserBeamEntity = collisionEvent.EntityB;
                }
                else
                {
                    // This job only deals with laser
                    return;
                }
            }

            Entity targetVehicleEntity = default;
            if (VehicleHealthLookup.TryGetComponent(collisionEvent.EntityA, out var targetHealth))
            {
                targetVehicleEntity = collisionEvent.EntityA;
            }
            else
            {
                targetVehicleEntity = collisionEvent.EntityA;
                if(VehicleHealthLookup.TryGetComponent(collisionEvent.EntityB, out targetHealth))
                {
                    targetVehicleEntity = collisionEvent.EntityB;
                }
            }

            // If the laser hit his own vehicle, ignore it
            if (sourceLaserBeam.PlayerSource == targetVehicleEntity)
            {
                return;
            }

            // If the laser hits anything, destroy it.
            LaserBeamsExploded.Add(sourceLaserBeamEntity);

            // Only the server deals the damage and kill confirmation
            if (!IsServer)
            {
                return;
            }
            
            ImmunityLookup.TryGetComponent(targetVehicleEntity, out var targetImmunity);
            
            // If the target is valid and not immune
            if (targetVehicleEntity == default || // Not target vehicle found
                targetHealth.Value == 0 || // Target is already dead
                targetImmunity.Counter > 0f || // Has some immunity left
                !VehicleLaserLookup.TryGetComponent(sourceLaserBeam.PlayerSource, out var sourceVehicleLaser) ||
                !PlayerScoreLookup.TryGetComponent(sourceLaserBeam.PlayerSource, out var sourceScore) ||
                !PlayerScoreLookup.TryGetComponent(targetVehicleEntity, out var targetScore) ||
                !PlayerNameLookup.TryGetComponent(sourceLaserBeam.PlayerSource, out var sourceName) ||
                !PlayerNameLookup.TryGetComponent(targetVehicleEntity, out var targetName))
                return;
            
            Debug.LogWarning("Laser beam hit a vehicle !");
            // Damage the target
            
            targetHealth.Value -= sourceVehicleLaser.Damage;
            sourceScore.Value += sourceVehicleLaser.Damage;

            // Detects Kill
            if (math.abs(targetHealth.Value) < 0.01f)
            {
                targetHealth.Value = 0;
                sourceScore.Kills += 1;
                sourceScore.KilledPlayer = targetName.Name;
                targetScore.KillerName = sourceName.Name;
                PlayerScoreLookup[targetVehicleEntity] = targetScore; 
            }
            
            VehicleHealthLookup[targetVehicleEntity] = targetHealth;
            PlayerScoreLookup[sourceLaserBeam.PlayerSource] = sourceScore; 
        }
    }
}