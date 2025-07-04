using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.Extensions;
using Unity.Physics;
using Unity.Physics.Systems;
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
            var networkTime = GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstTimeFullyPredictingTick)
                return;

            using var laserBeamsExploded = new NativeList<Entity>(Allocator.TempJob);
            var isServer = state.WorldUnmanaged.IsServer();
            var triggerJob = new LaserBeamTriggerEventJob()
            {
                IsServer = isServer,
                LaserBeamsExploded = laserBeamsExploded,
                VehicleHealthLookup = GetComponentLookup<VehicleHealth>(),
                PlayerScoreLookup =  GetComponentLookup<PlayerScore>(),
                LaserBeamLookup =  GetComponentLookup<LaserBeam>(),
                ImmunityLookup =  GetComponentLookup<Immunity>(true),
                VehicleLaserLookup =  GetComponentLookup<VehicleLaser>(true),
                PlayerNameLookup =  GetComponentLookup<PlayerName>(true),
                GhostOwnerLookup = GetComponentLookup<GhostOwner>(true),
            };
            state.Dependency = triggerJob.Schedule(GetSingleton<SimulationSingleton>(),state.Dependency);
            state.Dependency.Complete();
            if (isServer)
            {
                if (laserBeamsExploded.Length > 0)
                {
                    // Log each laser beam destroyed with its Entity ID and NetworkID (if available)
                    var arr = laserBeamsExploded.ToArray(Allocator.TempJob);
                    foreach (var laserBeam in arr)
                    {
                        string logMsg = $"Laser beam destroyed: Entity={laserBeam.Index}";
                        if (state.EntityManager.HasComponent<GhostOwner>(laserBeam))
                        {
                            var ghostOwner = state.EntityManager.GetComponentData<GhostOwner>(laserBeam);
                            logMsg += $", NetworkID={ghostOwner.NetworkId}";
                        }
                        Debug.Log($"{logMsg} at tick {networkTime.ServerTick},{networkTime.InterpolationTick}");
                    }
                    state.EntityManager.DestroyEntity(arr);
                    arr.Dispose();
                }
            }
            else
            {
                if (laserBeamsExploded.Length > 0)
                {
                    // Log each laser beam hidden with its Entity ID and NetworkID (if available)
                    foreach (var laserBeam in laserBeamsExploded)
                    {
                        string logMsg = $"Laser beam hidden: Entity={laserBeam.Index}";
                        if (state.EntityManager.HasComponent<GhostOwner>(laserBeam))
                        {
                            var ghostOwner = state.EntityManager.GetComponentData<GhostOwner>(laserBeam);
                            logMsg += $", NetworkID={ghostOwner.NetworkId}";
                        }
                        Debug.Log($"{logMsg} at tick {networkTime.ServerTick},{networkTime.InterpolationTick}");
                    }
                }
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