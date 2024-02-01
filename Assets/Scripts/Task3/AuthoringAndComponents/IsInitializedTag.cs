using System;
using Unity.Entities;
using Unity.Transforms;

namespace Task3.AuthoringAndComponents
{
    [Serializable]
    [WriteGroup(typeof(LocalToWorld))]
    public struct IsUninitializedTag : IComponentData
    {
    }
}