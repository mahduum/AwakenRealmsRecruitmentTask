using Task3.AuthoringAndComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Task3.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(DetectRangeChangeSystem))]
    public partial struct RangeDistanceCollectorSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerComponent>();
            _transformLookup = state.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var player = SystemAPI.GetSingletonEntity<PlayerComponent>();
            _transformLookup.Update(ref state);
            
            var distanceCollectionQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, RangeSettingsShared>()
                .WithAllRW<RangeReferenceDistanceSqComponent>().Build();

            var distanceCollectionJob = new DistanceToReferencePointCollectionJob()
            {
                TransformsLookup = _transformLookup,
                Player = player
            };

            state.Dependency = distanceCollectionJob.ScheduleParallel(distanceCollectionQuery, state.Dependency);
            
            /*This is the only place I decided to leave as example the requested in the task 3 procedure of skipping updates and caching deltas.
             This particular use does not require accumulated delta time but it is available. I also decided to leave in code the range tags from
             High to Lowest precision as they don't produce any noticeable overhead and provide the means to implement additional layers of logic
             in other possible systems in which it might be reasonable to assume that less important and more distant ranges could require less
             and less complex logic for the entities or no logic at all.

             I would like to underline that in this case - keeping account of the distance from the viewer/player with less
             frequent update based on the precision range - there was not visible improvement and with or without this additional logic
             the time needed for the system with 100 000 moving entities was on average 0.03 ms on my machine. In case the person who reads
             this would want to test it, all is needed is to uncomment the code below (and the respective code in `RangeDeltaTimeSystem.cs`
             and `RangeObjectAuthoring.cs` and comment out the code above concerning `distanceCollectionQuery`*/
            
            // var rangeDeltaTimesEntity = SystemAPI.GetSingletonEntity<RangeDeltaTimes>();
            // var rangeDeltaTimes = state.EntityManager.GetComponentData<RangeDeltaTimes>(rangeDeltaTimesEntity);
            // var distanceCollectionHighQuery = SystemAPI.QueryBuilder().WithAll<HighPrecisionRange, LocalTransform, RangeSettingsShared>()
            //     .WithAllRW<RangeReferenceDistanceSqComponent>().Build();
            // var distanceCollectionMediumQuery = SystemAPI.QueryBuilder().WithAll<MediumPrecisionRange, LocalTransform, RangeSettingsShared>()
            //     .WithAllRW<RangeReferenceDistanceSqComponent>().Build();
            // var distanceCollectionLowQuery = SystemAPI.QueryBuilder().WithAll<LowPrecisionRange, LocalTransform, RangeSettingsShared>()
            //     .WithAllRW<RangeReferenceDistanceSqComponent>().Build();
            // var distanceCollectionLowestQuery = SystemAPI.QueryBuilder().WithAll<LowestPrecisionRange, LocalTransform, RangeSettingsShared>()
            //     .WithAllRW<RangeReferenceDistanceSqComponent>().Build();
            // state.Dependency = new DistanceToReferencePointCollectionJob()
            // {
            //     TransformsLookup = _transformLookup,
            //     Player = player
            // }.ScheduleParallel(distanceCollectionHighQuery, state.Dependency);
            // if (rangeDeltaTimes.ShouldUpdateEverySecondFrame)
            // {
            //     state.Dependency = new DistanceToReferencePointCollectionJob()
            //     {
            //         TransformsLookup = _transformLookup,
            //         Player = player
            //
            //     }.ScheduleParallel(distanceCollectionMediumQuery, state.Dependency);
            // }
            //
            // if (rangeDeltaTimes.ShouldUpdateEveryThirdFrame)
            // {
            //     state.Dependency = new DistanceToReferencePointCollectionJob()
            //     {
            //         TransformsLookup = _transformLookup,
            //         Player = player
            //     }.ScheduleParallel(distanceCollectionLowQuery, state.Dependency);
            // }
            //
            // if (rangeDeltaTimes.ShouldUpdateEveryFourthFrame)
            // {
            //     state.Dependency = new DistanceToReferencePointCollectionJob()
            //     {
            //         TransformsLookup = _transformLookup,
            //         Player = player
            //
            //     }.ScheduleParallel(distanceCollectionLowestQuery, state.Dependency);
            // }
        }
    }
    
    [BurstCompile]
    public partial struct DistanceToReferencePointCollectionJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformsLookup;
        public Entity Player;
        
        [BurstCompile]
        private void Execute(ref RangeReferenceDistanceSqComponent distanceToViewerSq,
            in LocalTransform localTransform)
        {
            distanceToViewerSq.Value = math.distancesq(TransformsLookup[Player].Position, localTransform.Position);
        }
    }
}