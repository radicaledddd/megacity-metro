using MegacityMetro.Pooling;
using Unity.Entities;
using Unity.Mathematics;
using Unity.MegacityMetro.Gameplay;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.LocalSimulation)]
public partial struct VehicleFXSystem : ISystem
{
    private static int ID_FXParam_BeamLength = 0;
    private static int ID_FXParam_HitEffectActive = 0;
    private static int ID_FXParam_ShieldLifetime = 0;

    public void OnCreate(ref SystemState state)
    {
        ID_FXParam_BeamLength = Shader.PropertyToID("Length");
        ID_FXParam_HitEffectActive = Shader.PropertyToID("HitEffect");
        ID_FXParam_ShieldLifetime = Shader.PropertyToID("Lifetime");
    }

    public void OnUpdate(ref SystemState state)
    {
        ComponentLookup<DynamicInstanceLinkCleanup> instanceLinkCleanupLookup = SystemAPI.GetComponentLookup<DynamicInstanceLinkCleanup>(true);

        foreach (var (vehicleSettings, moveState, velocity, vehicleHealth, laser, ltw, immunity) in SystemAPI.Query<
                     RefRO<PlayerVehicleSettings>,
                     RefRO<VehicleMovementState>,
                     RefRO<PhysicsVelocity>,
                     RefRW<VehicleHealth>,
                     RefRO<VehicleLaser>,
                     RefRO<LocalToWorld>,
                     RefRW<Immunity>>())
        {
            if (GameObjectPool.GetPooledElement(vehicleSettings.ValueRO.VehicleFX, ref instanceLinkCleanupLookup, out GameObjectPoolElement vehicleFXElement))
            {
                ShipFXManager fxManager = vehicleFXElement.GameObject.GetComponent<ShipFXManager>();
                if (fxManager != null)
                {
                    fxManager.transform.SetPositionAndRotation(ltw.ValueRO.Position, ltw.ValueRO.Rotation);
                    // Laser;
                    if (laser.ValueRO.Shoots)
                    {
                        if (!fxManager.VFXLaserMuzzle.enabled)
                            fxManager.VFXLaserMuzzle.enabled = true;
                    }else
                    {
                        if (fxManager.VFXLaserMuzzle.enabled)
                            fxManager.VFXLaserMuzzle.enabled = false;
                    }
                    
                    // Car movement sound
                    {
                        if (vehicleHealth.ValueRO.IsDead == 0)
                        {
                            float speedFactor = math.saturate(math.length(velocity.ValueRO.Linear) / 30f);
                            fxManager.SFXCarSound.pitch = math.lerp(1f, 2f, speedFactor);
                        }
                        else
                        {
                            fxManager.SFXCarSound.pitch = 0f;
                        }
                    }

                    // Shield
                    {
                        if (immunity.ValueRO.Counter > 0)
                        {
                            if (immunity.ValueRW._immunityFXState == 0)
                            {
                                if (fxManager.VFXShield.HasFloat(ID_FXParam_ShieldLifetime))
                                {
                                    fxManager.VFXShield.SetFloat(ID_FXParam_ShieldLifetime, immunity.ValueRO.Duration);
                                }
                                fxManager.VFXShield.Play();
                                immunity.ValueRW._immunityFXState = 1;
                            }
                        }
                        else
                        {
                            immunity.ValueRW._immunityFXState = 0;
                        }
                    }

                    // Damage/Death
                    {
                        // Sparks
                        if (vehicleHealth.ValueRO.Value > 50f)
                        {
                            // Remove
                            if (fxManager.VFXSparks.enabled)
                            {
                                fxManager.VFXSparks.enabled = false;
                            }
                        }
                        else
                        {
                            // Activate
                            if (!fxManager.VFXSparks.enabled)
                            {
                                fxManager.VFXSparks.enabled = true;
                            }
                        }

                        // Smoke
                        if (vehicleHealth.ValueRO.Value > 20f)
                        {
                            // Remove
                            if (fxManager.VFXSmoke.enabled)
                            {
                                fxManager.VFXSmoke.enabled = false;
                            }
                        }
                        else
                        {
                            // Activate
                            if (!fxManager.VFXSmoke.enabled)
                            {
                                fxManager.VFXSmoke.enabled = true;
                            }
                        }

                        // Death
                        if (vehicleHealth.ValueRO.IsDead == 1)
                        {
                            // Play
                            if (vehicleHealth.ValueRO._DeathFXState == 0)
                            {
                                vehicleHealth.ValueRW._DeathFXState = 1;
                                fxManager.SFXDeath.Play();
                                fxManager.VFXExplosion.Play();
                                fxManager.VFXElecArcs.enabled = true;
                            }
                        }
                        else
                        {
                            if (vehicleHealth.ValueRW._DeathFXState == 1)
                            {
                                vehicleHealth.ValueRW._DeathFXState = 0;
                                fxManager.VFXElecArcs.enabled = false;
                            }
                        }
                    }
                }
            }
        }
    }
}