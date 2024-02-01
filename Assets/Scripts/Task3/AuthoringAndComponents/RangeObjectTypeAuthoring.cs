using Task3.Aspects;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.Serialization;

namespace Task3.AuthoringAndComponents
{
    public class RangeObjectTypeAuthoring : MonoBehaviour
    {
        [SerializeField] GameObject _prefab;
        [SerializeField] int _count;
        [SerializeField] float3 _extents;
        
        private class Baker : Baker<RangeObjectTypeAuthoring>
        {
            public override void Bake(RangeObjectTypeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                
                AddComponent(entity, new RangeObjectTypeGroup()
                {
                    Prefab = GetEntity(authoring._prefab, TransformUsageFlags.Dynamic),
                    Count = authoring._count,
                    Extents = authoring._extents,
                    PrefabRadius = authoring._prefab.GetComponent<MeshFilter>().sharedMesh.bounds.extents.magnitude
                });
            }
        }
    }
    
    public struct RangeObjectTypeGroup : IComponentData
    {
        public Entity Prefab;
        public int Count;
        public float3 Extents;
        public float PrefabRadius;
    }
}