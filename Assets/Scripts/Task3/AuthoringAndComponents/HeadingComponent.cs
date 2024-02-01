using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Task3.AuthoringAndComponents
{
    [Serializable]
    public struct HeadingComponent : IComponentData
    {
        public float3 Value;
    }
}