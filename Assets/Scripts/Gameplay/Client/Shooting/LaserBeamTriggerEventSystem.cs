using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.Extensions;
using Unity.Physics;
using Unity.Rendering;
using UnityEngine;
using static Unity.Entities.SystemAPI;

namespace Unity.MegacityMetro.Gameplay
{

[UpdateInGroup(typeof(PresentationSystemGroup))]
 [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.LocalSimulation)]
public partial struct ClientLaserBeamTriggerEventSystem : ISystem
    {
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<SimulationSingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var networkTime = GetSingleton<NetworkTime>();

            using var laserBeamsExploded = new NativeList<Entity>(Allocator.TempJob);
            var triggerJob = new ClientLaserBeamTriggerEventJob()
            {
                LaserBeamsExploded = laserBeamsExploded,
                VehicleHealthLookup = GetComponentLookup<VehicleHealth>(true),
                LaserBeamLookup =  GetComponentLookup<LaserBeam>(),
                GhostOwnerLookup = GetComponentLookup<GhostOwner>(true),
            };
            state.Dependency = triggerJob.Schedule(GetSingleton<SimulationSingleton>(),state.Dependency);
            state.Dependency.Complete();

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