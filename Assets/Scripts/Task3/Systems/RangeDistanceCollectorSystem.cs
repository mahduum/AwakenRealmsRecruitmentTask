using Task3.AuthoringAndComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Task3.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct RangeDistanceCollectorSystem : ISystem
    {
        [ReadOnly]
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<RangeReferenceDistanceSqComponent> _distanceSqLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerComponent>();

            _transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            _distanceSqLookup = SystemAPI.GetComponentLookup<RangeReferenceDistanceSqComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);
            _distanceSqLookup.Update(ref state);
            var player = SystemAPI.GetSingletonEntity<PlayerComponent>();
            var playerPosition = SystemAPI.GetComponentRO<LocalTransform>(player).ValueRO.Position;
            
            var distanceCollectionQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, RangeSettingsShared>()
                .WithAllRW<RangeReferenceDistanceSqComponent>().Build();

            var distanceCollectionJob = new DistanceToReferencePointCollectionJob()
            {
                PlayerPosition = playerPosition
            };

            state.Dependency = distanceCollectionJob.ScheduleParallel(distanceCollectionQuery, state.Dependency);
        }
    }
    
    [BurstCompile]
    public partial struct DistanceToReferencePointCollectionJob : IJobEntity
    {
        public float3 PlayerPosition;
        
        private void Execute(ref RangeReferenceDistanceSqComponent distanceToViewerSq,
            in LocalTransform localTransform)
        {
            distanceToViewerSq.Value = math.distancesq(PlayerPosition, localTransform.Position);
        }
    }
}