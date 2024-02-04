using JetBrains.Annotations;
using Task3.AuthoringAndComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Task3.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateAfter(typeof(RangeDistanceCollectorSystem))]
    public partial struct DetectRangeChangeSystem : ISystem
    {
        private ComponentLookup<ChangedRangeComponent> _changedRangeLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _changedRangeLookup = state.GetComponentLookup<ChangedRangeComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _changedRangeLookup.Update(ref state);
            var rangeSettingsQuery = SystemAPI.QueryBuilder().WithAll<RangeComponent, RangeSettingsShared, RangeReferenceDistanceSqComponent>().Build();
            
            var world = state.WorldUnmanaged;
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<RangeSettingsShared> rangeTypeGroups, world.UpdateAllocator.ToAllocator);

            foreach (var rangeGroup in rangeTypeGroups)
            {
                rangeSettingsQuery.AddSharedComponentFilter(rangeGroup);

                var rangersCount = rangeSettingsQuery.CalculateEntityCount();
                if (rangersCount == 0)
                {
                    rangeSettingsQuery.ResetFilter();
                    continue;
                }
                
                var updateRangeJob = new UpdateRangeJob()
                {
                    ChangedRangeLookup = _changedRangeLookup,
                    RangeGroupSettings = rangeGroup,
                };
                
                state.Dependency = updateRangeJob.ScheduleParallel(rangeSettingsQuery, state.Dependency);
                
                rangeSettingsQuery.AddDependency(state.Dependency);
                rangeSettingsQuery.ResetFilter();
            }

            rangeTypeGroups.Dispose();
        }
    }

    [WithDisabled(typeof(ChangedRangeComponent))]
    [BurstCompile]
    public partial struct UpdateRangeJob : IJobEntity
    {
        [NativeDisableContainerSafetyRestriction, NativeDisableParallelForRestriction] 
        public ComponentLookup<ChangedRangeComponent> ChangedRangeLookup;
        public RangeSettingsShared RangeGroupSettings;

        [UsedImplicitly]
        private void Execute(Entity entity, in RangeComponent range, in RangeReferenceDistanceSqComponent distanceSqComponent)
        {
            var distanceSq = distanceSqComponent.Value;
            ref var rangesLimitsData = ref RangeGroupSettings.Ranges.Value.Values;

            //first check the distance within its own recorded range and if it is outside then exclude the index from next check
            if (IsWithinRange(distanceSq, rangesLimitsData[range.CurrentRangeIndex]))
            {
                return;
            }
                    
            //if we are here that means that object changed range
            for (int i = rangesLimitsData.Length - 1; i >= 0; i--)
            {
                if (distanceSq >= math.lengthsq(rangesLimitsData[i].Min))
                {
                    ChangedRangeLookup.GetRefRW(entity).ValueRW.ChangedRangeIndex = i;
                    ChangedRangeLookup.SetComponentEnabled(entity, true);
                    break;
                }
            }
        }
        
        private static bool IsWithinRange(float distanceSq, RangeLimits rangeLimits)
        {
            return distanceSq >= math.lengthsq(rangeLimits.Min) && distanceSq < math.lengthsq(rangeLimits.Max);
        } 

    }
}