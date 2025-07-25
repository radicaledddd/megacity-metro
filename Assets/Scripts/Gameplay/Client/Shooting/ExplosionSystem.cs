using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Unity.MegacityMetro.Gameplay
{
 
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ClientLaserBeamTriggerEventSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.LocalSimulation)]
    public partial struct ExplosionSystem : ISystem
    {
        private EntityQuery m_ExplosionsQuery;

        public void OnCreate(ref SystemState state)
        {
            m_ExplosionsQuery = state.GetEntityQuery(typeof(LaserBeamExplosion));
        }

        public void OnUpdate(ref SystemState state)
        {
            var currentTime = state.WorldUnmanaged.Time.ElapsedTime;
            using var explosionsEntities = m_ExplosionsQuery.ToEntityArray(Allocator.Temp);
            using var laserBeamExplosions = m_ExplosionsQuery.ToComponentDataArray<LaserBeamExplosion>(Allocator.Temp);
            using var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);
            for (var i = 0; i < explosionsEntities.Length; i++)
            {
                if (currentTime >= laserBeamExplosions[i].DeleteMeAt)
                {
                    cmdBuffer.DestroyEntity(explosionsEntities[i]);
                }
            }
            cmdBuffer.Playback(state.EntityManager);
        }
    }
}