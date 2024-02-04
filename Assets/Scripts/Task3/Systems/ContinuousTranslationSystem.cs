using Task3.AuthoringAndComponents;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Task3.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct ContinuousTranslationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var translationQuery = SystemAPI.QueryBuilder().WithAll<LocalToWorld, TranslationBoundsSharedComponent>()
                .WithAllRW<LocalTransform, HeadingComponent>().Build();

            var dt = SystemAPI.Time.DeltaTime;
            var translationJob = new TranslationJob()
            {
                DeltaTime = dt,
            }.ScheduleParallel(translationQuery, state.Dependency);

            state.Dependency = translationJob;
        }
    }
    
    [BurstCompile]
    public partial struct TranslationJob : IJobEntity
    {
        public float DeltaTime;
        public void Execute(in LocalToWorld localToWorld, ref LocalTransform localTransform,
            ref HeadingComponent heading, in TranslationBoundsSharedComponent bounds)
        {
            ref var direction = ref heading.Value;
            ref var position = ref localTransform.Position;
            localTransform.Rotation = localToWorld.Rotation;
            position = localToWorld.Position + direction * DeltaTime * 10f;
            
            const int vectorLength = 3;

            for (int i = 0; i < vectorLength; i++)
            {
                if (IsOutOfBounds(i, localTransform, bounds))
                {
                    direction[i] *= -1;
                }
            }
        }
        
        private bool IsOutOfBounds(int axisIndex, LocalTransform localTransform, TranslationBoundsSharedComponent bounds)
        {
            var origin = bounds.Origin[axisIndex];
            var extent = bounds.BoundsExtents[axisIndex];
            var position = localTransform.Position[axisIndex];
            return position > origin + extent || position < origin - extent;
        } 
    }
}