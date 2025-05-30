using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace Unity.MegacityMetro.Gameplay
{
    /// <summary>
    ///  Vehicle laser settings
    /// </summary>
    public struct VehicleLaser : IComponentData
    {
        public float LaserCooldown;
        public float FireRate;
        public float DrainRate;
        public float RechargeRate;
        public float LaserBeamSpeed;
        public float Damage;
        public float3 LocalLaserStartPoint;
        public Entity LaserBeamPrefab;
        
        public float3 VFXLaserStartNode;
        public float3 VFXLaserEndNode;
        
        [GhostField(Quantization = 1000)] 
        public float Energy;
        [GhostField] 
        public bool Shoots;
        

        public readonly float3 CalculateLaserStartPoint(float3 vehiclePosition, quaternion vehicleRotation)
        {
            return math.transform(float4x4.TRS(vehiclePosition, vehicleRotation, 1f), LocalLaserStartPoint);
        }
        
        public readonly float3 CalculateLaserEndPoint(float3 vehiclePosition, quaternion vehicleRotation, ref ComponentLookup<LocalToWorld> localToWorldLookup)
        {
            return CalculateLaserStartPoint(vehiclePosition, vehicleRotation) + math.mul(vehicleRotation, math.forward() * 150);
        }
    }
}