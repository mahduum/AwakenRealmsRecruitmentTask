using Unity.Entities;
using Unity.Mathematics;

namespace Task3.AuthoringAndComponents
{
    public struct TranslationBoundsSharedComponent : ISharedComponentData
    {
        public float3 BoundsExtents;
        public float3 Origin;
    }
}