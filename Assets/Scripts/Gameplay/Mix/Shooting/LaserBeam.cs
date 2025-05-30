using Unity.Entities;
using Unity.NetCode;

namespace Unity.MegacityMetro.Gameplay
{
    public struct LaserBeam : IComponentData
    {
        public Entity PlayerSource;
        public bool Exploded;
    }
}