using Unity.Entities;
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

    public struct PlayerComponent : IComponentData
    {
        //public float TranslationDelta;
    }

    public struct NeedsCameraPositionUpdate : IComponentData, IEnableableComponent//todo?
    {
        
    }
}