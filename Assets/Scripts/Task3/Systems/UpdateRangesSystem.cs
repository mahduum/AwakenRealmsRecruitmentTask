using Task3.AuthoringAndComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Task3.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateAfter(typeof(RangeDistanceCollectorSystem))]
    public partial struct UpdateRangesSystem : ISystem//todo make job set ranges that will run only once per newly spawned entity, for entities without range component, it will be added
    {
        private ComponentLookup<ChangedRangeComponent> _changedRangeLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _changedRangeLookup = state.GetComponentLookup<ChangedRangeComponent>();//TODO move to separate system that detects change
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _changedRangeLookup.Update(ref state);
            //todo with hashmap grid the ranges sq component will not be necessary
            var rangeSettingsQuery = SystemAPI.QueryBuilder().WithAll<RangeSettingsShared, RangeReferenceDistanceSqComponent>().WithAllRW<RangeComponent>().Build();
            //add different queries for range components etc.
            //just fill the hash with locations, it can be made only after the first time
            
            var world = state.WorldUnmanaged;
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<RangeSettingsShared> rangeTypeGroups, world.UpdateAllocator.ToAllocator);

            float deltaTime = SystemAPI.Time.DeltaTime;

            //todo add or activate component crossing range that will activate glow effect
            foreach (var rangeGroup in rangeTypeGroups)
            {
                rangeSettingsQuery.AddSharedComponentFilter(rangeGroup);

                var rangersCount = rangeSettingsQuery.CalculateEntityCount();
                if (rangersCount == 0)
                {
                    rangeSettingsQuery.ResetFilter();
                    continue;
                }
                
                //todo later, hashmap with cells by radius
                //var hashMap = new NativeParallelMultiHashMap<int, int>(rangersCount, world.UpdateAllocator.ToAllocator);

                var entities = rangeSettingsQuery.ToEntityArray(Allocator.Temp);

                foreach (var entity in entities)
                {
                    var distanceSq = state.EntityManager.GetComponentData<RangeReferenceDistanceSqComponent>(entity).Value;
                    var range = state.EntityManager.GetComponentData<RangeComponent>(entity);
                    ref var rangesLimitsData = ref rangeGroup.Ranges.Value.Values;

                    //first check the distance within its own recorded range and if it is outside then exclude the index from next check
                    if (IsWithinRange(distanceSq, rangesLimitsData[range.CurrentRangeIndex]))//todo return out index to omit in next search
                    {
                            continue;
                    }
                    
                    //if we are here that means that object changed range
                    for (int i = 0; i < rangesLimitsData.Length; i++)
                    {
                        if (IsWithinRange(distanceSq, rangesLimitsData[i]))
                        {
                            //is in range save limits, indices and compare whether it changed or not
                            state.EntityManager.SetComponentData(entity, new RangeComponent(){CurrentRangeIndex = i});
                            _changedRangeLookup.SetComponentEnabled(entity, true);
                            break;
                        }
                    }
                }
                //...
                
                rangeSettingsQuery.AddDependency(state.Dependency);
                rangeSettingsQuery.ResetFilter();
            }

            rangeTypeGroups.Dispose();
        }

        private static bool IsWithinRange(float distanceSq, RangeLimits rangeLimits)
        {
            return distanceSq >= math.pow(rangeLimits.Min, 2) && distanceSq < math.pow(rangeLimits.Max, 2);
        } 

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    public partial struct UpdateRangeJob : IJobEntity
    {
        
    }
}