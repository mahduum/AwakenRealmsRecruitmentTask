using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Task3.AuthoringAndComponents
{
    [DisallowMultipleComponent]
    public class CameraInfoAuthoring : MonoBehaviour
    {
        class CameraInfoBaker : Baker<CameraInfoAuthoring>
        {
            public override void Bake(CameraInfoAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CameraInfoComponent());
            }
        }
    }
    
    public struct CameraInfoComponent : IComponentData
    {
        public float3 Position;
    }
}