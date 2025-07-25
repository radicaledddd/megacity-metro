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

    public partial struct LaserBeamExplosion : IComponentData
    {
        public double DeleteMeAt;
    }

[UpdateInGroup(typeof(SimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.LocalSimulation)]
public partial struct ClientLaserBeamTriggerEventSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<LaserBeamSpawner>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var networkTime = GetSingleton<NetworkTime>();

            using var laserBeamsExploded = new NativeList<(Entity,LocalTransform)>(Allocator.TempJob);
            var triggerJob = new ClientLaserBeamTriggerEventJob()
            {
                LaserBeamsExploded = laserBeamsExploded,
                LaserBeamLookup =  GetComponentLookup<LaserBeam>(),
                VehicleHealthLookup = GetComponentLookup<VehicleHealth>(true),
                LocalTransformLookup= GetComponentLookup<LocalTransform>(true),
                GhostOwnerLookup = GetComponentLookup<GhostOwner>(true),
            };
            state.Dependency = triggerJob.Schedule(GetSingleton<SimulationSingleton>(), state.Dependency);
            state.Dependency.Complete();

            if (laserBeamsExploded.Length == 0)
                return;
            
            // Log each laser beam hidden with its Entity ID and NetworkID (if available)
            foreach (var laserBeam in laserBeamsExploded)
            {
                string logMsg = $"Laser beam hidden: Entity={laserBeam.Item1.Index} V{laserBeam.Item1.Version}";
                if (state.EntityManager.HasComponent<GhostOwner>(laserBeam.Item1))
                {
                    var ghostOwner = state.EntityManager.GetComponentData<GhostOwner>(laserBeam.Item1);
                    logMsg += $", NetworkID={ghostOwner.NetworkId}";
                }
                //Debug.LogWarning($"{logMsg} hidden at tick {networkTime.ServerTick},{networkTime.InterpolationTick}");
            }
            var laserBeamSpawner = GetSingleton<LaserBeamSpawner>();
            var commandBuffer = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var laserBeamExploded in laserBeamsExploded)
            {
                var explosion = commandBuffer.Instantiate(laserBeamSpawner.ExplosionPrefab);
                var deleteAt = state.WorldUnmanaged.Time.ElapsedTime + 2;
                commandBuffer.AddComponent(explosion, laserBeamExploded.Item2);
                commandBuffer.AddComponent(explosion, new LaserBeamExplosion {DeleteMeAt = deleteAt});
                commandBuffer.AddComponent(laserBeamExploded.Item1, new DisableRendering(){});
            }
            commandBuffer.Playback(state.EntityManager);
        }
    }
}