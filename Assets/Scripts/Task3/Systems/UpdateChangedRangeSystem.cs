using System;
using Task3.AuthoringAndComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Task3.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [BurstCompile]
    public partial class UpdateChangedRangeSystem : SystemBase
    {
        private ComponentLookup<HighPrecisionRange> _highPrecisionLookup;
        private ComponentLookup<MediumPrecisionRange> _mediumPrecisionLookup;
        private ComponentLookup<LowPrecisionRange> _lowPrecisionLookup;
        private ComponentLookup<LowestPrecisionRange> _lowestPrecisionLookup;

        [BurstCompile]
        protected override void OnCreate()
        {
            _highPrecisionLookup = GetComponentLookup<HighPrecisionRange>();
            _mediumPrecisionLookup = GetComponentLookup<MediumPrecisionRange>();
            _lowPrecisionLookup = GetComponentLookup<LowPrecisionRange>();
            _lowestPrecisionLookup = GetComponentLookup<LowestPrecisionRange>();
        }
        
        [BurstCompile]
        protected override void OnUpdate()
        {
            _highPrecisionLookup.Update(this);
            _mediumPrecisionLookup.Update(this);
            _lowPrecisionLookup.Update(this);
            _lowestPrecisionLookup.Update(this);
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
                
                var endSimSystem = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>();
                var endSimBuffer = endSimSystem.CreateCommandBuffer(World.Unmanaged);

                ref var rangeGroupColors = ref rangeGroup.RangesColors.Value.Values;
                 NativeArray<float4> rangeGroupColorsCopy = new NativeArray<float4>(rangeGroupColors.Length, Allocator.TempJob);
                
                 for (int i = 0; i < rangeGroupColorsCopy.Length; i++)
                 {
                     rangeGroupColorsCopy[i] = rangeGroupColors[i];
                 }

                 var processingJobHandle = new ProcessChangeRangeJob
                 {
                     EcbParallel = endSimBuffer.AsParallelWriter(),
                     RangeGroupColors = rangeGroupColorsCopy,
                     HighPrecisionLookup = _highPrecisionLookup,
                     MediumPrecisionLookup = _mediumPrecisionLookup,
                     LowPrecisionLookup = _lowPrecisionLookup,
                     LowestPrecisionLookup = _lowestPrecisionLookup,
                 };

                Dependency = processingJobHandle.ScheduleParallel(query, Dependency);
                
                query.AddDependency(Dependency);
                query.ResetFilter();
            }
            
            rangeTypeGroups.Dispose();
        }
        
        [BurstCompile]
        private partial struct ProcessChangeRangeJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter EcbParallel;
            
            [DeallocateOnJobCompletion, ReadOnly]
            public NativeArray<float4> RangeGroupColors;
            
            [NativeDisableContainerSafetyRestriction, NativeDisableParallelForRestriction]
            public ComponentLookup<HighPrecisionRange> HighPrecisionLookup;
            [NativeDisableContainerSafetyRestriction, NativeDisableParallelForRestriction]
            public ComponentLookup<MediumPrecisionRange> MediumPrecisionLookup;
            [NativeDisableContainerSafetyRestriction, NativeDisableParallelForRestriction]
            public ComponentLookup<LowPrecisionRange> LowPrecisionLookup;
            [NativeDisableContainerSafetyRestriction, NativeDisableParallelForRestriction]
            public ComponentLookup<LowestPrecisionRange> LowestPrecisionLookup;

            [BurstCompile]
            public void Execute([EntityIndexInQuery] int sortKey, Entity entity, in ChangedRangeComponent change, ref RangeComponent range, ref HDRPMaterialPropertyBaseColor material)
            {
                HighPrecisionLookup.SetComponentEnabled(entity, change.ChangedRangeIndex == 0);
                MediumPrecisionLookup.SetComponentEnabled(entity, change.ChangedRangeIndex == 1);
                LowPrecisionLookup.SetComponentEnabled(entity, change.ChangedRangeIndex == 2);
                LowestPrecisionLookup.SetComponentEnabled(entity, change.ChangedRangeIndex >= 3);
                
                EcbParallel.SetComponentEnabled<ChangedRangeComponent>(sortKey, entity, false);
                range.CurrentRangeIndex = change.ChangedRangeIndex;
                material.Value = RangeGroupColors[change.ChangedRangeIndex];
            }
        }
    }
}