using Unity.Entities;
using Unity.MegacityMetro.Gameplay;
using UnityEngine;

namespace Unity.MegacityMetro.Authoring
{
    public class LaserBeamAuthoring : MonoBehaviour
    {
        public GameObject LaserBeamPrefab;
        public GameObject ExplosionPrefab;

        public class LaserBeamAuthoringBaker : Baker<LaserBeamAuthoring>
        {
            
            public override void Bake(LaserBeamAuthoring authoring)
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