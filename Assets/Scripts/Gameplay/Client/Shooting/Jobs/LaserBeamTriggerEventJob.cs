using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace Unity.MegacityMetro.Gameplay
{
    public partial struct LaserBeamTriggerEventJob : ITriggerEventsJob
    {
        internal ComponentLookup<VehicleHealth> VehicleHealthLookup;
        internal ComponentLookup<LaserBeam> LaserBeamLookup;
        
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
            sourceLaserBeam.Exploded = true;
        }
    }
}