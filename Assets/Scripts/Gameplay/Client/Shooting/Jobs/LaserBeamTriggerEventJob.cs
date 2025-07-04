using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;

namespace Unity.MegacityMetro.Gameplay
{
    public partial struct ClientLaserBeamTriggerEventJob : ITriggerEventsJob
    {
        internal NativeList<Entity> LaserBeamsExploded;
        [ReadOnly]
        internal ComponentLookup<VehicleHealth> VehicleHealthLookup;
        internal ComponentLookup<LaserBeam> LaserBeamLookup;
        [ReadOnly]
        internal ComponentLookup<GhostOwner> GhostOwnerLookup;
        
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
                    // This jobs only deals with lasers
                    return;
                }
            }

            if (sourceLaserBeam.Exploded)
            {
                Debug.LogWarning("Abort due to isServer isExplosed");
                // We already handled hiding the entity on the client
                return;
            }

            Entity targetVehicleEntity = default;
            if (VehicleHealthLookup.TryGetComponent(collisionEvent.EntityA, out var targetHealth))
            {
                targetVehicleEntity = collisionEvent.EntityA;
            }
            else
            {
                if(VehicleHealthLookup.TryGetComponent(collisionEvent.EntityB, out targetHealth))
                {
                    targetVehicleEntity = collisionEvent.EntityB;
                }
            }

            GhostOwner targetGhostOwner = default;

            if (targetVehicleEntity != default && !GhostOwnerLookup.TryGetComponent(targetVehicleEntity, out targetGhostOwner))
            {
                Debug.LogError("GhostOwner lookup not found");
                return;
            }

            if (targetVehicleEntity != default && targetVehicleEntity == sourceLaserBeamEntity)
            {
                if (collisionEvent.EntityA == collisionEvent.EntityB)
                {
                    Debug.LogError("they are really equal !!!");
                }
                Debug.LogError("Source and target are equal ??? ignoring");
                return;
            }
            
            // If the laser hit his own vehicle, ignore it
            if (targetVehicleEntity != default && sourceLaserBeam.NetworkId == targetGhostOwner.NetworkId)
            {
                Debug.Log($"Player {sourceLaserBeam.NetworkId} it is own vehicle {targetGhostOwner.NetworkId}, ignoring");
                return;
            }

            sourceLaserBeam.Exploded = true;
            LaserBeamsExploded.Add(sourceLaserBeamEntity);
        }
    }
}