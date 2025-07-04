using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.NetCode.Extensions;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.MegacityMetro.Gameplay
{
    public partial struct LaserBeamTriggerEventJob : ITriggerEventsJob
    {
        internal bool IsServer;
        internal NativeList<Entity> LaserBeamsExploded;
        internal ComponentLookup<VehicleHealth> VehicleHealthLookup;
        internal ComponentLookup<PlayerScore> PlayerScoreLookup;
        internal ComponentLookup<LaserBeam> LaserBeamLookup;
        [ReadOnly]
        internal ComponentLookup<Immunity> ImmunityLookup;
        [ReadOnly]
        internal ComponentLookup<VehicleLaser> VehicleLaserLookup;
        [ReadOnly]
        internal ComponentLookup<PlayerName> PlayerNameLookup;
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

            if (!IsServer && sourceLaserBeam.Exploded)
            {
                Debug.LogWarning($"Abort due to isServer {IsServer}, isExplosed {sourceLaserBeam.Exploded}");
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

            // If the laser hits anything, destroy it.
            sourceLaserBeam.Exploded = true;
            LaserBeamsExploded.Add(sourceLaserBeamEntity);

            // Only the server deals the damage and kill confirmation
            if (!IsServer)
            {
                Debug.LogWarning("Hit a vehicle with client, ignoring");
                return;
            }
            Debug.LogWarning("Hit a vehicle with server, proceeding");
            
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
            {
                Debug.LogWarning("Laser beam hit something else, destroying it");
                return;
            }
            
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