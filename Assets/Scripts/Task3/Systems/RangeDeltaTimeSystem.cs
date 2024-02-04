using Task3.AuthoringAndComponents;
using Unity.Burst;
using Unity.Entities;

namespace Task3.Systems
{
    /*System used for testing/profiling as part of the task, detailed description in RangeDistanceCollectorSystem.cs*/
    // [UpdateInGroup(typeof(InitializationSystemGroup))]
    // public partial struct RangeDeltaTimeSystem : ISystem
    // {
    //     [BurstCompile]
    //     public void OnCreate(ref SystemState state)
    //     {
    //         state.EntityManager.AddComponentData(state.SystemHandle, new RangeDeltaTimes());
    //     }
    //
    //     [BurstCompile]
    //     public void OnUpdate(ref SystemState state)
    //     {
    //         var deltaTimes = state.EntityManager.GetComponentDataRW<RangeDeltaTimes>(state.SystemHandle);
    //         var deltaTime = SystemAPI.Time.DeltaTime;
    //
    //         if ((deltaTimes.ValueRW.EverySecondFrameCounter %= 2) == 0)
    //         {
    //             deltaTimes.ValueRW.EverySecondFrameDeltaTime = 0;
    //         }
    //         else
    //         {
    //             deltaTimes.ValueRW.EverySecondFrameDeltaTime += deltaTime;
    //         }
    //         deltaTimes.ValueRW.EverySecondFrameCounter++;
    //         deltaTimes.ValueRW.ShouldUpdateEverySecondFrame = deltaTimes.ValueRW.EverySecondFrameCounter % 2 == 0;
    //         
    //         if ((deltaTimes.ValueRW.EveryThirdFrameCounter %= 3) == 0)
    //         {
    //             deltaTimes.ValueRW.EveryThirdFrameDeltaTime = 0;
    //         }
    //         else
    //         {
    //             deltaTimes.ValueRW.EveryThirdFrameDeltaTime += deltaTime;
    //
    //         }
    //         deltaTimes.ValueRW.EveryThirdFrameCounter++;
    //         deltaTimes.ValueRW.ShouldUpdateEveryThirdFrame = deltaTimes.ValueRW.EveryThirdFrameCounter % 3 == 0;
    //
    //         
    //         if ((deltaTimes.ValueRW.EveryFourthFrameCounter %= 4) == 0)
    //         {
    //             deltaTimes.ValueRW.EveryFourthFrameDeltaTime = 0;
    //         }
    //         else
    //         {
    //             deltaTimes.ValueRW.EveryFourthFrameDeltaTime += deltaTime;
    //         }
    //         deltaTimes.ValueRW.EveryFourthFrameCounter++;
    //         deltaTimes.ValueRW.ShouldUpdateEveryFourthFrame = deltaTimes.ValueRW.EveryFourthFrameCounter % 4 == 0;
    //     }
    // }
}