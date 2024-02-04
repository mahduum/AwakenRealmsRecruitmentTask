using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Task3.AuthoringAndComponents
{
    public class PlayerAuthoring : MonoBehaviour
    {
        private class PlayerBaker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PlayerComponent());
            }
        }
    }

    [WriteGroup(typeof(LocalTransform))]
    public struct PlayerComponent : IComponentData
    {
    }
}