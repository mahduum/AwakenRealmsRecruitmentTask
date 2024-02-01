using Task3.Aspects;
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
        //[BurstCompile]
        // public void OnCreate(ref SystemState state)
        // {
        //     
        // }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var aspect in SystemAPI.Query<TranslationAspect>().WithOptions(EntityQueryOptions.FilterWriteGroup))//WithNone<IsUninitializedTag>())
            {
                aspect.Translate(SystemAPI.Time.DeltaTime);
            }
        }
    }
}