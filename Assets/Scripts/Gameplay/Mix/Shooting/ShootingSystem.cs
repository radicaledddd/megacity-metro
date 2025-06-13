using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

namespace Unity.MegacityMetro.Gameplay
{
    /// <summary>
    /// Handles shooting
    /// </summary>
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct ShootingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VehicleLaser>();
            state.RequireForUpdate<NetworkTime>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var shootLaserJob = new ShootLaserJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                HealthLookup = GetComponentLookup<VehicleHealth>(true),
            };
            state.Dependency = shootLaserJob.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
            
            // Only spawn entities once
            var networkTime = GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstTimeFullyPredictingTick)
                return;
            using var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (vehicleLaser, localToWorld, physicsVelocity, ghostOwner, entity)in Query<RefRO<VehicleLaser>, RefRO<LocalToWorld>, RefRO<PhysicsVelocity>, RefRO<GhostOwner>>().WithEntityAccess())
            {
                // If vehicle is shooting, spawn a laser beam
                if (vehicleLaser.ValueRO.Shoots)
                {
                    var laserBeam = cmdBuffer.Instantiate(vehicleLaser.ValueRO.LaserBeamPrefab);
                    cmdBuffer.SetName(laserBeam, "LaserBeam");
                    cmdBuffer.SetComponent(laserBeam, new LocalTransform()
                    {
                        // Place the entity at the front of the vehicle's laser plus an offset to avoid colliding with it as it spawns.
                        Scale = 1,
                        Position = localToWorld.ValueRO.Position + math.mul(localToWorld.ValueRO.Rotation.value.xyz, vehicleLaser.ValueRO.LocalLaserStartPoint),
                        Rotation = localToWorld.ValueRO.Rotation
                    });
                    cmdBuffer.AddComponent(laserBeam, PhysicsMass.CreateDynamic(MassProperties.UnitSphere, 1f));
                    cmdBuffer.AddComponent(laserBeam, new PhysicsVelocity()
                    {
                        // Calculate the velocity of the laser beam taking the forward velocity of the vehicle into account.
                        Linear = vehicleLaser.ValueRO.LaserBeamSpeed * localToWorld.ValueRO.Forward + physicsVelocity.ValueRO.Linear
                    });
                    
                    cmdBuffer.AddComponent(laserBeam, new LaserBeam
                    {
                        PlayerSource = entity,
                        Exploded = false
                    });
                    cmdBuffer.AddComponent(laserBeam, new GhostOwner {NetworkId = ghostOwner.ValueRO.NetworkId});
                }
            }
            cmdBuffer.Playback(state.EntityManager);
        }
    }
}