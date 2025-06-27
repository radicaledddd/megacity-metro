using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.Extensions;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

namespace Unity.MegacityMetro.Gameplay
{
    
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct LaserBeamTriggerEventSystem : ISystem
    {
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<SimulationSingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            using var laserBeamsExploded = new NativeList<Entity>( Allocator.TempJob);
            var networkTime = GetSingleton<NetworkTime>();
            var isServer = state.WorldUnmanaged.IsServer();
            var triggerJob = new LaserBeamTriggerEventJob()
            {
                IsServer = isServer,
                LaserBeamsExploded = laserBeamsExploded,
                VehicleHealthLookup = GetComponentLookup<VehicleHealth>(),
                PlayerScoreLookup =  GetComponentLookup<PlayerScore>(),
                LaserBeamLookup =  GetComponentLookup<LaserBeam>(true),
                ImmunityLookup =  GetComponentLookup<Immunity>(true),
                VehicleLaserLookup =  GetComponentLookup<VehicleLaser>(true),
                PlayerNameLookup =  GetComponentLookup<PlayerName>(true),
                GhostOwnerLookup = GetComponentLookup<GhostOwner>(true),
            };
            state.Dependency = triggerJob.Schedule(GetSingleton<SimulationSingleton>(),state.Dependency);
            state.Dependency.Complete();
            if (isServer)
            {
                if (!networkTime.IsFirstTimeFullyPredictingTick)
                {
                    Debug.LogWarning("It was needed");
                }
                if (laserBeamsExploded.Length > 0)
                {
                    Debug.LogWarning($"Disposing {laserBeamsExploded.Length} entities server side");
                    var arr = laserBeamsExploded.ToArray(Allocator.TempJob);
                    state.EntityManager.DestroyEntity(arr);
                    arr.Dispose();
                }
            }
            else
            {
                if (networkTime.IsFirstTimeFullyPredictingTick)
                {
                    if(laserBeamsExploded.Length > 0)
                        Debug.LogWarning($"Hiding {laserBeamsExploded.Length} entities client side");
                    var commandBuffer = new EntityCommandBuffer(state.WorldUpdateAllocator);
                    foreach (var laserBeamExploded in laserBeamsExploded)
                    {
                        commandBuffer.AddComponent<DisableRendering>(laserBeamExploded);
                    }
                    commandBuffer.Playback(state.EntityManager);
                }
            }
        }
    }
}