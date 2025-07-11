using Unity.Entities;

namespace Unity.MegacityMetro.Gameplay
{
    public struct LaserBeamSpawner : IComponentData
    {
        public Entity LaserBeamPrefab;
        public Entity ExplosionPrefab;
    }
}