using Unity.Entities;
using Unity.MegacityMetro.Gameplay;
using UnityEngine;

namespace Unity.MegacityMetro.Authoring
{
    public class LaserBeamSpawnerAuthoring : MonoBehaviour
    {
        public GameObject LaserBeamPrefab;
        public GameObject ExplosionPrefab;

        [BakingVersion("megacity-metro", 1)]
        public class Baker : Baker<LaserBeamSpawnerAuthoring>
        {
            public override void Bake(LaserBeamSpawnerAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(entity, new LaserBeamSpawner
                {
                    LaserBeamPrefab = GetEntity(authoring.LaserBeamPrefab, TransformUsageFlags.Dynamic),
                    ExplosionPrefab = GetEntity(authoring.ExplosionPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}