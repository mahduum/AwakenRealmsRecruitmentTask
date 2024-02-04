using System;
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
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateAfter(typeof(UpdateRangesSystem))]
    [BurstCompile]
    public partial class ChangedRangeProcessingSystem : SystemBase
    {
        [BurstCompile]
        protected override void OnUpdate()
        {
            var endSimSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var endSimBuffer = endSimSystem.CreateCommandBuffer(World.Unmanaged);
            var query = SystemAPI.QueryBuilder().WithAll<ChangedRangeComponent, RangeSettingsShared>().WithAllRW<RangeComponent, HDRPMaterialPropertyBaseColor>().Build();
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

                foreach (var entity in entities)//todo convert to jobs!!!
                {
                    var rangeComponent = EntityManager.GetComponentData<RangeComponent>(entity);
                    var changedRangeComponent = EntityManager.GetComponentData<ChangedRangeComponent>(entity);
                    SwapRangeTagComponents(rangeComponent.CurrentRangeIndex, changedRangeComponent.ChangedRangeIndex, endSimBuffer, entity);
                    EntityManager.SetComponentEnabled<ChangedRangeComponent>(entity, false);
                    EntityManager.SetComponentData(
                        entity,
                        new RangeComponent(){CurrentRangeIndex = changedRangeComponent.ChangedRangeIndex});
                    EntityManager.SetComponentData(
                        entity,
                        new HDRPMaterialPropertyBaseColor() { Value = rangeGroupColors[changedRangeComponent.ChangedRangeIndex]});
                }
                
                query.AddDependency(Dependency);
                query.ResetFilter();
            }
            
            rangeTypeGroups.Dispose();
        }

        public void SwapRangeTagComponents (int changeFromIndex, int changeToIndex, EntityCommandBuffer ecb, Entity entity)
        {
            switch (changeFromIndex)
            {
                case 0:
                    ecb.RemoveComponent<HighPrecisionRange>(entity);
                    break;
                case 1:
                    ecb.RemoveComponent<MediumPrecisionRange>(entity);
                    break;
                case 2:
                    ecb.RemoveComponent<LowPrecisionRange>(entity);
                    break;
                default:
                    ecb.RemoveComponent<LowestPrecisionRange>(entity);
                    break;
            }
            
            switch (changeToIndex)
            {
                case 0:
                    ecb.AddComponent<HighPrecisionRange>(entity);
                    break;
                case 1:
                    ecb.AddComponent<MediumPrecisionRange>(entity);
                    break;
                case 2:
                    ecb.AddComponent<LowPrecisionRange>(entity);
                    break;
                default:
                    ecb.AddComponent<LowestPrecisionRange>(entity);
                    break;
            }
        }
    }
}