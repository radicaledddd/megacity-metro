using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.Extensions;
using Unity.Physics;
using static Unity.Entities.SystemAPI;

namespace Unity.MegacityMetro.Gameplay
{
    
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct LaserBeamTriggerEventSystem : ISystem
    {
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var triggerJob = new LaserBeamTriggerEventJob()
            {
                VehicleHealthLookup = GetComponentLookup<VehicleHealth>(),
                LaserBeamLookup =  GetComponentLookup<LaserBeam>(),
            };
            state.Dependency = triggerJob.Schedule(GetSingleton<SimulationSingleton>(),state.Dependency);
            state.Dependency.Complete();
        }
    }
}