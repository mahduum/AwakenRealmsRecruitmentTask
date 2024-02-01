using Task3.AuthoringAndComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Task3.Systems
{
    [BurstCompile]
    public partial class ChangedRangeProcessingSystem : SystemBase
    {
        [BurstCompile]
        protected override void OnUpdate()
        {
            var query = SystemAPI.QueryBuilder().WithAll<RangeComponent, RangeSettingsShared>().WithAllRW<ChangedRangeComponent, HDRPMaterialPropertyBaseColor>().Build();
            
            var world = World.Unmanaged;
            EntityManager.GetAllUniqueSharedComponents(out NativeList<RangeSettingsShared> rangeTypeGroups, world.UpdateAllocator.ToAllocator);
            
            foreach (var rangeGroup in rangeTypeGroups)
            {
                query.AddSharedComponentFilter(rangeGroup);

                var rangersCount = query.CalculateEntityCount();
                if (rangersCount == 0)
                {
                    query.ResetFilter();
                    continue;
                }

                ref var rangeGroupColors = ref rangeGroup.RangesColors.Value.Values;

                var entities = query.ToEntityArray(World.Unmanaged.UpdateAllocator.Handle);

                foreach (var entity in entities)
                {
                    var rangeComponent = EntityManager.GetComponentData<RangeComponent>(entity);
                    EntityManager.SetComponentEnabled<ChangedRangeComponent>(entity, false);
                    EntityManager.SetComponentData(
                        entity,
                        new HDRPMaterialPropertyBaseColor() { Value = rangeGroupColors[rangeComponent.CurrentRangeIndex]});
                }
                
                query.AddDependency(Dependency);
                query.ResetFilter();
            }
            
            rangeTypeGroups.Dispose();
        }
    }
}