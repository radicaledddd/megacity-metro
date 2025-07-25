using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.MegacityMetro.Gameplay
{
    public partial struct ClientLaserBeamTriggerEventJob : ITriggerEventsJob
    {
        internal NativeList<(Entity, LocalTransform)> LaserBeamsExploded;
        internal ComponentLookup<LaserBeam> LaserBeamLookup;
        [ReadOnly]
        internal ComponentLookup<VehicleHealth> VehicleHealthLookup;
        [ReadOnly]
        internal ComponentLookup<GhostOwner> GhostOwnerLookup;
        [ReadOnly]
        internal ComponentLookup<LocalTransform> LocalTransformLookup;
        
        public void Execute(TriggerEvent triggerEvent)
        {
            // This jobs handles laser collisions
            // This assumes an entity cannot have a LaserBeam and a VehicleHealth component at the same time
            Entity sourceLaserBeamEntity = default;
            if (LaserBeamLookup.TryGetComponent(triggerEvent.EntityA, out var sourceLaserBeam))
            {
                sourceLaserBeamEntity = triggerEvent.EntityA;
            }
            else
            {
                if (LaserBeamLookup.TryGetComponent(triggerEvent.EntityB, out sourceLaserBeam))
                {
                    sourceLaserBeamEntity = triggerEvent.EntityB;
                }
                else
                {
                    // This jobs only deals with lasers
                    return;
                }
            }

            if (sourceLaserBeam.Exploded)
            {
                Debug.LogWarning("Abort due to isServer isExploded");
                // We already handled hiding the entity on the client
                return;
            }

            Entity targetVehicleEntity = default;
            if (VehicleHealthLookup.TryGetComponent(triggerEvent.EntityA, out var targetHealth))
            {
                targetVehicleEntity = triggerEvent.EntityA;
            }
            else
            {
                if(VehicleHealthLookup.TryGetComponent(triggerEvent.EntityB, out targetHealth))
                {
                    targetVehicleEntity = triggerEvent.EntityB;
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
                if (triggerEvent.EntityA == triggerEvent.EntityB)
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
            LaserBeamLookup[sourceLaserBeamEntity] = sourceLaserBeam;
            LocalTransformLookup.TryGetComponent(sourceLaserBeamEntity, out var laserBeamTransform);
            LaserBeamsExploded.Add((sourceLaserBeamEntity, laserBeamTransform));
        }
    }
}