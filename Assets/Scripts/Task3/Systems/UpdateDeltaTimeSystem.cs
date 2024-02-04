using Task3.AuthoringAndComponents;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Task3.Systems
{
    //Used for testing as a device to query entities whether they should update current frame
    //I didn't include this in solution as it produced no noticeable effect and instead produced
    //a substantial overhead on its own, sometimes it is more efficient to perform some little
    //demanding calculations en masse than to block the command buffer with structural changes
    //I didn't remove the code as a testimony that I did experiment with suggested solutions
    //as presented in the task description.
    
    [DisableAutoCreation]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(UpdateChangedRangeSystem))]
    public partial struct UpdateDeltaTimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var setDeltaTimeQuery = SystemAPI.QueryBuilder().WithAll<RangeComponent>().WithAllRW<RangeDeltaTimeComponent>()
                .WithDisabled<ChangedRangeComponent>().Build();

            var incrementDeltaTimeQuery = SystemAPI.QueryBuilder().WithDisabledRW<RangeDeltaTimeComponent>().WithDisabled<ChangedRangeComponent>()
                .Build();
            
            var changedRangeDeltaTimeDisables = SystemAPI.QueryBuilder().WithAll<ChangedRangeComponent>().WithDisabledRW<RangeDeltaTimeComponent>().Build();

            var deltaTime = SystemAPI.Time.DeltaTime;

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var asParallelWriter = ecb.AsParallelWriter();

            var handle1 = new SetDeltaTimeJob()
            {
                Ecb = asParallelWriter,
                DeltaTime = deltaTime
            }.ScheduleParallel(setDeltaTimeQuery, state.Dependency);
            
            var handle2 = new IncrementDeltaTimeJob()
            {
                Ecb = asParallelWriter,
                DeltaTime = deltaTime
            }.ScheduleParallel(incrementDeltaTimeQuery, handle1);

            state.Dependency = handle2;

            var handle3 = new ChangedRangeDeltaTimeCorrectionJob()
            {
                Ecb = asParallelWriter,
                DeltaTime = deltaTime
            }.Schedule(changedRangeDeltaTimeDisables, handle2);

            state.Dependency = handle3;
            setDeltaTimeQuery.AddDependency(state.Dependency);
            incrementDeltaTimeQuery.AddDependency(state.Dependency);
            changedRangeDeltaTimeDisables.AddDependency(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct ChangedRangeDeltaTimeCorrectionJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public float DeltaTime;

        public void Execute([EntityIndexInQuery] int sortKey, in Entity entity, ref RangeDeltaTimeComponent rangeDeltaTime, in ChangedRangeComponent change)
        {
            var newUpdateFrame = math.min(change.ChangedRangeIndex, 3);
            rangeDeltaTime.DeltaTime += DeltaTime;
            rangeDeltaTime.UpdateFrame = newUpdateFrame;
            rangeDeltaTime.CurrentFrame++;
            
            if (rangeDeltaTime.CurrentFrame >= newUpdateFrame)
            {
                //we are all set, just add delta and allow earlier update:
                Ecb.SetComponentEnabled<RangeDeltaTimeComponent>(sortKey, entity, true);
            }
        }
    }

    [BurstCompile]
    public partial struct SetDeltaTimeJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public float DeltaTime;

        public void Execute([EntityIndexInQuery] int sortKey, in Entity entity, in RangeComponent range, ref RangeDeltaTimeComponent rangeDeltaTime)
        {
            rangeDeltaTime.CurrentFrame = 0;
            rangeDeltaTime.DeltaTime = DeltaTime;
            rangeDeltaTime.UpdateFrame = math.min(range.CurrentRangeIndex, 3);

            if (range.CurrentRangeIndex == 0)
            {
                //we don't need to disable, delta time is passed as is
                return;
            }
            
            Ecb.SetComponentEnabled<RangeDeltaTimeComponent>(sortKey, entity, false);
        }
    }

    [BurstCompile]
    public partial struct IncrementDeltaTimeJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public float DeltaTime;

        public void Execute([EntityIndexInQuery] int sortKey, in Entity entity, ref RangeDeltaTimeComponent rangeDeltaTime)
        {
            if (rangeDeltaTime.UpdateFrame > rangeDeltaTime.CurrentFrame)
            {
                rangeDeltaTime.CurrentFrame++;
                rangeDeltaTime.DeltaTime += DeltaTime;
            }
            else
            {
                Ecb.SetComponentEnabled<RangeDeltaTimeComponent>(sortKey, entity, true);
            }
        }
    }
}