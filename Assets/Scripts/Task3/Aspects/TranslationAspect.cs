using Task3.AuthoringAndComponents;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Task3.Aspects
{
    public readonly partial struct TranslationAspect : IAspect
    {
        private readonly TranslationBoundsSharedComponent _bounds;
        private readonly RefRO<LocalToWorld> _localToWorld;
        private readonly RefRW<LocalTransform> _localTransform;
        private readonly RefRW<HeadingComponent> _headingComponent;

        public void Translate(float deltaTime)
        {
            ref var direction = ref _headingComponent.ValueRW.Value;
            ref var position = ref _localTransform.ValueRW.Position;
            _localTransform.ValueRW.Rotation = _localToWorld.ValueRO.Rotation;//todo write only the first time
            position = _localToWorld.ValueRO.Position + direction * deltaTime * 10f;

            _localTransform.ValueRW.Position = position;
            
            const int vectorLength = 3;

            for (int i = 0; i < vectorLength; i++)
            {
                if (IsOutOfBounds(i))
                {
                    direction[i] *= -1;
                }
            }
        }

        private bool IsOutOfBounds(int axisIndex)
        {
            var origin = _bounds.Origin[axisIndex];
            var extent = _bounds.BoundsExtents[axisIndex];
            var position = _localTransform.ValueRO.Position[axisIndex];
            return position > origin + extent || position < origin - extent;
        } 
    }

    public struct TranslationBoundsSharedComponent : ISharedComponentData
    {
        public float3 BoundsExtents;
        public float3 Origin;
    }
}