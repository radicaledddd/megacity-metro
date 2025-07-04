using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.Extensions;
using Unity.Physics;
using UnityEngine;
using static Unity.Entities.SystemAPI;

namespace Unity.MegacityMetro.Gameplay
{

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerLaserBeamTriggerEventSystem : ISystem
    {
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<SimulationSingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var networkTime = GetSingleton<NetworkTime>()




            ;

            using var laserBeamsExploded = new NativeList<Entity>(Allocator.TempJob);
            var triggerJob = new ServerLaserBeamTriggerEventJob()
            {
                LaserBeamsExploded = laserBeamsExploded,
                VehicleHealthLookup = GetComponentLookup<VehicleHealth>(),
                PlayerScoreLookup = GetComponentLookup<PlayerScore>(),
                LaserBeamLookup = GetComponentLookup<LaserBeam>(true),
                ImmunityLookup = GetComponentLookup<Immunity>(true),
                VehicleLaserLookup = GetComponentLookup<VehicleLaser>(true),
                PlayerNameLookup = GetComponentLookup<PlayerName>(true),
                GhostOwnerLookup = GetComponentLookup<GhostOwner>(true),
            };
            state.Dependency = triggerJob.Schedule(GetSingleton<SimulationSingleton>(), state.Dependency);
            state.Dependency.Complete();
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
                    Debug.Log($"{logMsg} at tick {networkTime.ServerTick}");
                }
                state.EntityManager.DestroyEntity(arr);
                arr.Dispose();
            }
        }
    }
}